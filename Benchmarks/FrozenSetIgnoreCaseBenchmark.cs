using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

/*
 * BenchmarkDotNet v0.15.6, Windows 11 (10.0.26100.8390/24H2/2024Update/HudsonValley)
 * AMD Ryzen 9 7950X3D 4.20GHz, 1 CPU, 32 logical and 16 physical cores
 * .NET SDK 10.0.204
 *   [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v4
 *   DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v4
 *
 * | Method            | Input          |       Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
 * |------------------ |--------------- |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
 * | Ordinal           | ExactMatch     |   1.454 ns | 0.0198 ns | 0.0185 ns |  1.00 |    0.02 |         - |          NA |
 * | OrdinalIgnoreCase | ExactMatch     |   1.476 ns | 0.0373 ns | 0.0383 ns |  1.02 |    0.03 |         - |          NA |
 * |                   |                |            |           |           |       |         |           |             |
 * | Ordinal           | Miss           |   1.350 ns | 0.0308 ns | 0.0257 ns |  1.00 |    0.03 |         - |          NA |
 * | OrdinalIgnoreCase | Miss           |   1.329 ns | 0.0314 ns | 0.0262 ns |  0.98 |    0.03 |         - |          NA |
 * |                   |                |            |           |           |       |         |           |             |
 * | Ordinal           | MixedCaseMatch |   5.028 ns | 0.1134 ns | 0.1260 ns |  1.00 |    0.03 |         - |          NA |
 * | OrdinalIgnoreCase | MixedCaseMatch |  12.312 ns | 0.2660 ns | 0.2488 ns |  2.45 |    0.08 |         - |          NA |
 * |                   |                |            |           |           |       |         |           |             |
 * | Ordinal           | UpperCaseMatch |   5.043 ns | 0.0798 ns | 0.1038 ns |  1.00 |    0.03 |         - |          NA |
 * | OrdinalIgnoreCase | UpperCaseMatch |  12.519 ns | 0.2676 ns | 0.3082 ns |  2.48 |    0.08 |         - |          NA |
 */
[MemoryDiagnoser]
public class FrozenSetIgnoreCaseBenchmark
{
    private static readonly string[] Headers =
    {
        "Connection",
        "Transfer-Encoding",
        "Keep-Alive",
        "Proxy-Connection",
        "Upgrade",
        "TE",
    };

    private static readonly FrozenSet<string> OrdinalSet =
        Headers.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> IgnoreCaseSet =
        Headers.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> InputNames => new[]
    {
        "ExactMatch",       // "Connection"
        "MixedCaseMatch",   // "connection"
        "UpperCaseMatch",   // "CONNECTION"
        "Miss",             // "Content-Length"
    };

    [ParamsSource(nameof(InputNames))]
    public string Input { get; set; } = "ExactMatch";

    private string _name = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _name = Input switch
        {
            "ExactMatch"     => "Connection",
            "MixedCaseMatch" => "connection",
            "UpperCaseMatch" => "CONNECTION",
            "Miss"           => "Content-Length",
            _ => throw new ArgumentOutOfRangeException(nameof(Input)),
        };
    }

    [Benchmark(Baseline = true)]
    public bool Ordinal() => OrdinalSet.Contains(_name);

    [Benchmark]
    public bool OrdinalIgnoreCase() => IgnoreCaseSet.Contains(_name);
}
