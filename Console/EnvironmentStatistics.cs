using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace Orleans.Statistics;

// This struct is intentionally 'packed' in order to avoid extra padding.
// This will be created very frequently, so we reduce stack size and lower the serialization cost.
// As more fields are added to this, they could be placed in such a manner that it may result in a lot of 'empty' space.
/// <summary>
/// Contains statistics about the current process and its execution environment.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct EnvironmentStatistics
{
    /// <summary>
    /// The system CPU usage.
    /// <br/>
    /// Applies Kalman filtering to smooth out short-term fluctuations.
    /// See <see href="https://en.wikipedia.org/wiki/Kalman_filter"/>;
    /// <see href="https://www.ledjonbehluli.com/posts/orleans_resource_placement_kalman/"/>
    /// </summary>
    /// <remarks>Ranges from 0.0-100.0.</remarks>    
    public readonly float CpuUsagePercentage;

    /// <summary>
    /// The amount of managed memory currently consumed by the process.
    /// <br/>
    /// Applies Kalman filtering to smooth out short-term fluctuations.
    /// See <see href="https://en.wikipedia.org/wiki/Kalman_filter"/>;
    /// <see href="https://www.ledjonbehluli.com/posts/orleans_resource_placement_kalman/"/>
    /// </summary>
    /// <remarks>
    /// Includes fragmented memory, which is the unused memory between objects on the managed heaps.
    /// </remarks>
    public readonly long MemoryUsageBytes;

    /// <summary>
    /// The amount of memory currently available for allocations to the process.
    /// <br/>
    /// Applies Kalman filtering to smooth out short-term fluctuations.
    /// See <see href="https://en.wikipedia.org/wiki/Kalman_filter"/>;
    /// <see href="https://www.ledjonbehluli.com/posts/orleans_resource_placement_kalman/"/>
    /// </summary>
    /// <remarks>
    /// Includes the currently available memory of the process and the system.
    /// </remarks>
    public readonly long AvailableMemoryBytes;

    /// <summary>
    /// The maximum amount of memory available to the process.
    /// </summary>
    /// <remarks>
    /// This value is computed as the lower of two amounts:
    /// <list type="bullet">
    ///   <item><description>The amount of memory after which the garbage collector will begin aggressively collecting memory, defined by <see cref="GCMemoryInfo.HighMemoryLoadThresholdBytes"/>.</description></item>
    ///   <item><description>The process' configured memory limit, defined by <see cref="GCMemoryInfo.TotalAvailableMemoryBytes"/>.</description></item>
    /// </list>
    /// Memory limits are common in containerized environments. For more information on configuring memory limits, see <see href="https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector#heap-limit"/>
    /// </remarks>
    public readonly long MaximumAvailableMemoryBytes;

    /// <summary>
    /// The system CPU usage.
    /// </summary>
    /// <remarks>Ranges from 0.0-100.0.</remarks>
    public readonly float RawCpuUsagePercentage;

    /// <summary>
    /// The amount of managed memory currently consumed by the process.
    /// </summary>
    public readonly long RawMemoryUsageBytes;

    /// <summary>
    /// The amount of memory currently available for allocations to the process.
    /// </summary>
    public readonly long RawAvailableMemoryBytes;

    internal EnvironmentStatistics(
        float cpuUsagePercentage,
        float rawCpuUsagePercentage,
        long memoryUsageBytes,
        long rawMemoryUsageBytes,
        long availableMemoryBytes,
        long rawAvailableMemoryBytes,
        long maximumAvailableMemoryBytes)
    {
        CpuUsagePercentage = cpuUsagePercentage;
        RawCpuUsagePercentage = rawCpuUsagePercentage;
        MemoryUsageBytes = memoryUsageBytes;
        RawMemoryUsageBytes = rawMemoryUsageBytes;
        AvailableMemoryBytes = availableMemoryBytes;
        RawAvailableMemoryBytes = rawAvailableMemoryBytes;
        MaximumAvailableMemoryBytes = maximumAvailableMemoryBytes;
    }
}


#nullable enable
public sealed class EnvironmentStatisticsProvider : IDisposable
{
    private const float OneKiloByte = 1024f;

    private long _availableMemoryBytes;
    private long _maximumAvailableMemoryBytes;

    private readonly EventCounterListener _eventCounterListener = new();

    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used for memory-dump debugging.")]
    private readonly ObservableCounter<long> _availableMemoryCounter;

    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used for memory-dump debugging.")]
    private readonly ObservableCounter<long> _maximumAvailableMemoryCounter;

    private readonly DualModeKalmanFilter _cpuUsageFilter = new();
    private readonly DualModeKalmanFilter _memoryUsageFilter = new();
    private readonly DualModeKalmanFilter _availableMemoryFilter = new();

    public EnvironmentStatisticsProvider()
    {
        GC.Collect(0, GCCollectionMode.Forced, true); // we make sure the GC structure wont be empty, also performing a blocking GC guarantees immediate collection.
    }

    /// <inheritdoc />
    public EnvironmentStatistics GetEnvironmentStatistics()
    {
        var memoryInfo = GC.GetGCMemoryInfo();

        var cpuUsage = _eventCounterListener.CpuUsage;
        var memoryUsage = GC.GetTotalMemory(false) + memoryInfo.FragmentedBytes;

        var committedOfLimit = memoryInfo.TotalAvailableMemoryBytes - memoryInfo.TotalCommittedBytes;
        var unusedLoad = memoryInfo.HighMemoryLoadThresholdBytes - memoryInfo.MemoryLoadBytes;
        var systemAvailable = Math.Max(0, Math.Min(committedOfLimit, unusedLoad));
        var processAvailable = memoryInfo.TotalCommittedBytes - memoryInfo.HeapSizeBytes;
        var availableMemory = systemAvailable + processAvailable;
        var maxAvailableMemory = Math.Min(memoryInfo.TotalAvailableMemoryBytes, memoryInfo.HighMemoryLoadThresholdBytes);

        var filteredCpuUsage = _cpuUsageFilter.Filter(cpuUsage);
        var filteredMemoryUsage = (long)_memoryUsageFilter.Filter(memoryUsage);
        var filteredAvailableMemory = (long)_availableMemoryFilter.Filter(availableMemory);
        // no need to filter 'maxAvailableMemory' as it will almost always be a steady value.

        _availableMemoryBytes = filteredAvailableMemory;
        _maximumAvailableMemoryBytes = maxAvailableMemory;

        return new(
            filteredCpuUsage, cpuUsage,
            filteredMemoryUsage, memoryUsage,
            filteredAvailableMemory, availableMemory,
            maxAvailableMemory);
    }

    public void Dispose() => _eventCounterListener.Dispose();

    private sealed class EventCounterListener : EventListener
    {
        public float CpuUsage { get; private set; } = 0f;

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("System.Runtime"))
            {
                Dictionary<string, string?>? refreshInterval = new() { ["EventCounterIntervalSec"] = "1" };
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if ("EventCounters".Equals(eventData.EventName) && eventData.Payload is { } payload)
            {
                for (var i = 0; i < payload.Count; i++)
                {
                    if (payload[i] is IDictionary<string, object?> eventPayload
                        && eventPayload.TryGetValue("Name", out var name)
                        && "cpu-usage".Equals(name)
                        && eventPayload.TryGetValue("Mean", out var mean)
                        && mean is double value)
                    {
                        CpuUsage = (float)value;
                        break;
                    }
                }
            }
        }
    }

    // See: https://www.ledjonbehluli.com/posts/orleans_resource_placement_kalman/

    // The rationale behind using a cooperative dual-mode KF, is that we want the input signal to follow a trajectory that
    // decays with a slower rate than the original one, but also tracks the signal in case of signal increases
    // (which represent potential of overloading). Both are important, but they are inversely correlated to each other.
    private sealed class DualModeKalmanFilter
    {
        private const float SlowProcessNoiseCovariance = 0f;
        private const float FastProcessNoiseCovariance = 0.01f;

        private KalmanFilter _slowFilter = new();
        private KalmanFilter _fastFilter = new();

        private FilterRegime _regime = FilterRegime.Slow;

        private enum FilterRegime
        {
            Slow,
            Fast
        }

        public float Filter(float measurement)
        {
            float slowEstimate = _slowFilter.Filter(measurement, SlowProcessNoiseCovariance);
            float fastEstimate = _fastFilter.Filter(measurement, FastProcessNoiseCovariance);

            if (measurement > slowEstimate)
            {
                if (_regime == FilterRegime.Slow)
                {
                    _regime = FilterRegime.Fast;
                    _fastFilter.SetState(measurement, 0f);
                    fastEstimate = _fastFilter.Filter(measurement, FastProcessNoiseCovariance);
                }

                return fastEstimate;
            }
            else
            {
                if (_regime == FilterRegime.Fast)
                {
                    _regime = FilterRegime.Slow;
                    _slowFilter.SetState(_fastFilter.PriorEstimate, _fastFilter.PriorErrorCovariance);
                    slowEstimate = _slowFilter.Filter(measurement, SlowProcessNoiseCovariance);
                }

                return slowEstimate;
            }
        }

        private struct KalmanFilter()
        {
            public float PriorEstimate { get; private set; } = 0f;
            public float PriorErrorCovariance { get; private set; } = 1f;

            public void SetState(float estimate, float errorCovariance)
            {
                PriorEstimate = estimate;
                PriorErrorCovariance = errorCovariance;
            }

            public float Filter(float measurement, float processNoiseCovariance)
            {
                float estimate = PriorEstimate;
                float errorCovariance = PriorErrorCovariance + processNoiseCovariance;

                float gain = errorCovariance / (errorCovariance + 1f);
                float newEstimate = estimate + gain * (measurement - estimate);
                float newErrorCovariance = (1f - gain) * errorCovariance;

                PriorEstimate = newEstimate;
                PriorErrorCovariance = newErrorCovariance;

                return newEstimate;
            }
        }
    }
}