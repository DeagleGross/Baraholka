using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

/*
 * BenchmarkDotNet v0.15.6, Windows 11 (10.0.26100.8390/24H2/2024Update/HudsonValley)
 * AMD Ryzen 9 7950X3D 4.20GHz, 1 CPU, 32 logical and 16 physical cores
 * .NET SDK 10.0.204
 *   [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v4
 *   DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v4
 *
 * | Method                            | Input           |       Mean |  Ratio | Allocated |
 * |---------------------------------- |---------------- |-----------:|-------:|----------:|
 * | SequenceEqualChain                | Connection      |   0.853 ns |   1.00 |         - |
 * | FrozenSetString_NameAlreadyString | Connection      |   2.451 ns |   2.88 |         - |
 * | FrozenSetBytes                    | Connection      |  10.170 ns |  11.96 |         - |
 * | FrozenSetString_FromBytes         | Connection      |  24.536 ns |  28.86 |      48 B |
 * |                                   |                 |            |        |           |
 * | SequenceEqualChain                | Upgrade         |   1.178 ns |   1.00 |         - |
 * | FrozenSetString_NameAlreadyString | Upgrade         |   1.989 ns |   1.69 |         - |
 * | FrozenSetBytes                    | Upgrade         |   6.635 ns |   5.63 |         - |
 * | FrozenSetString_FromBytes         | Upgrade         |  12.756 ns |  10.83 |      40 B |
 * |                                   |                 |            |        |           |
 * | SequenceEqualChain                | TE-close        |   1.712 ns |   1.00 |         - |
 * | FrozenSetString_NameAlreadyString | TE-close        |   2.449 ns |   1.43 |         - |
 * | FrozenSetBytes                    | TE-close        |   6.222 ns |   3.64 |         - |
 * | FrozenSetString_FromBytes         | TE-close        |  12.518 ns |   7.32 |      32 B |
 * |                                   |                 |            |        |           |
 * | SequenceEqualChain                | TE-trailers     |   2.026 ns |   1.00 |         - |
 * | FrozenSetString_NameAlreadyString | TE-trailers     |   2.516 ns |   1.24 |         - |
 * | FrozenSetBytes                    | TE-trailers     |   5.956 ns |   2.94 |         - |
 * | FrozenSetString_FromBytes         | TE-trailers     |  11.220 ns |   5.54 |      32 B |
 * |                                   |                 |            |        |           |
 * | SequenceEqualChain                | Content-Length  |   1.435 ns |   1.00 |         - |
 * | FrozenSetString_NameAlreadyString | Content-Length  |   2.273 ns |   1.59 |         - |
 * | FrozenSetBytes                    | Content-Length  |   7.827 ns |   5.46 |         - |
 * | FrozenSetString_FromBytes         | Content-Length  |  11.762 ns |   8.21 |      56 B |
 * |                                   |                 |            |        |           |
 * | SequenceEqualChain                | X-Custom-Header |   1.206 ns |   1.00 |         - |
 * | FrozenSetString_NameAlreadyString | X-Custom-Header |   1.688 ns |   1.41 |         - |
 * | FrozenSetBytes                    | X-Custom-Header |   7.441 ns |   6.20 |         - |
 * | FrozenSetString_FromBytes         | X-Custom-Header |  10.738 ns |   8.94 |      56 B |
 * 
 */
[MemoryDiagnoser]
public class FrozenSetVsSequenceEqualBenchmark
{
    // ------------------------------------------------------------------
    // Header name byte literals (as in the original code).
    // ------------------------------------------------------------------
    private static readonly byte[] ConnectionBytes = "Connection"u8.ToArray();
    private static readonly byte[] TransferEncodingBytes = "Transfer-Encoding"u8.ToArray();
    private static readonly byte[] KeepAliveBytes = "Keep-Alive"u8.ToArray();
    private static readonly byte[] ProxyConnectionBytes = "Proxy-Connection"u8.ToArray();
    private static readonly byte[] UpgradeBytes = "Upgrade"u8.ToArray();
    private static readonly byte[] TeBytes = "TE"u8.ToArray();
    private static readonly byte[] TrailersBytes = "trailers"u8.ToArray();

    // ------------------------------------------------------------------
    // FrozenSet keyed by string (case-insensitive — what HTTP requires).
    // Strings need to be materialized from the byte span on each call.
    // ------------------------------------------------------------------
    private static readonly FrozenSet<string> ConnectionHeadersStringSet =
        new[]
        {
            "Connection",
            "Transfer-Encoding",
            "Keep-Alive",
            "Proxy-Connection",
            "Upgrade",
            "TE",
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // ------------------------------------------------------------------
    // FrozenSet keyed by byte[] with a custom byte-sequence comparer.
    // Avoids the string allocation but pays a hashing cost.
    // ------------------------------------------------------------------
    private static readonly FrozenSet<byte[]> ConnectionHeadersByteSet =
        new[]
        {
            ConnectionBytes,
            TransferEncodingBytes,
            KeepAliveBytes,
            ProxyConnectionBytes,
            UpgradeBytes,
            TeBytes,
        }.ToFrozenSet(ByteArrayEqualityComparer.Instance);

    // ------------------------------------------------------------------
    // Inputs exercised by [ParamsSource]. Stored as fields populated in
    // GlobalSetup so the benchmark methods can branch on a single index.
    // ------------------------------------------------------------------
    public static IEnumerable<string> InputNames => new[]
    {
        "Connection",       // 1st branch hit
        "Upgrade",          // 5th branch hit
        "TE-trailers",      // TE special case, returns false
        "TE-close",         // TE special case, returns true
        "Content-Length",   // full miss
        "X-Custom-Header",  // full miss, longer
    };

    [ParamsSource(nameof(InputNames))]
    public string Input { get; set; } = "Connection";

    private byte[] _name = Array.Empty<byte>();
    private byte[] _value = Array.Empty<byte>();
    private string _nameString = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        (string headerName, string headerValue) = Input switch
        {
            "Connection" => ("Connection", "keep-alive"),
            "Upgrade" => ("Upgrade", "websocket"),
            "TE-trailers" => ("TE", "trailers"),
            "TE-close" => ("TE", "deflate"),
            "Content-Length" => ("Content-Length", "42"),
            "X-Custom-Header" => ("X-Custom-Header", "some-value"),
            _ => throw new ArgumentOutOfRangeException(nameof(Input)),
        };

        _name = Encoding.ASCII.GetBytes(headerName);
        _value = Encoding.ASCII.GetBytes(headerValue);
        _nameString = headerName;
    }

    // ------------------------------------------------------------------
    // 1) Chain of SequenceEqual on byte spans — the original approach.
    // ------------------------------------------------------------------
    [Benchmark(Baseline = true)]
    public bool SequenceEqualChain()
    {
        ReadOnlySpan<byte> name = _name;
        ReadOnlySpan<byte> value = _value;

        return name.SequenceEqual(ConnectionBytes)
            || name.SequenceEqual(TransferEncodingBytes)
            || name.SequenceEqual(KeepAliveBytes)
            || name.SequenceEqual(ProxyConnectionBytes)
            || name.SequenceEqual(UpgradeBytes)
            || (name.SequenceEqual(TeBytes) && !value.SequenceEqual(TrailersBytes));
    }

    // ------------------------------------------------------------------
    // 2) FrozenSet<string>.Contains with on-the-fly UTF-8 -> string decode.
    //    This is what you'd write if you stored only strings in the set
    //    but received bytes on the hot path.
    // ------------------------------------------------------------------
    [Benchmark]
    public bool FrozenSetString_FromBytes()
    {
        ReadOnlySpan<byte> name = _name;
        ReadOnlySpan<byte> value = _value;

        // Allocates a string per call. Conservative comparison.
        string nameStr = Encoding.ASCII.GetString(name);
        if (!ConnectionHeadersStringSet.Contains(nameStr))
        {
            return false;
        }

        if (nameStr.Equals("TE", StringComparison.OrdinalIgnoreCase))
        {
            return !value.SequenceEqual(TrailersBytes);
        }

        return true;
    }

    // ------------------------------------------------------------------
    // 3) FrozenSet<string>.Contains where the name is already a string.
    //    Models the case where you've moved string conversion out of the
    //    hot path (e.g. headers are already represented as strings).
    // ------------------------------------------------------------------
    [Benchmark]
    public bool FrozenSetString_NameAlreadyString()
    {
        string nameStr = _nameString;
        ReadOnlySpan<byte> value = _value;

        if (!ConnectionHeadersStringSet.Contains(nameStr))
        {
            return false;
        }

        if (nameStr.Equals("TE", StringComparison.OrdinalIgnoreCase))
        {
            return !value.SequenceEqual(TrailersBytes);
        }

        return true;
    }

    // ------------------------------------------------------------------
    // 4) FrozenSet<byte[]>.Contains with a custom byte-sequence comparer.
    //    Avoids the string allocation but FrozenSet<T> doesn't expose a
    //    span-based lookup, so we have to provide a byte[] key.
    // ------------------------------------------------------------------
    [Benchmark]
    public bool FrozenSetBytes()
    {
        byte[] name = _name;
        ReadOnlySpan<byte> value = _value;

        if (!ConnectionHeadersByteSet.Contains(name))
        {
            return false;
        }

        if (name.AsSpan().SequenceEqual(TeBytes))
        {
            return !value.SequenceEqual(TrailersBytes);
        }

        return true;
    }

    // ------------------------------------------------------------------
    // Custom comparer that hashes/compares byte arrays by content.
    // ------------------------------------------------------------------
    private sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Instance = new();

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            // Simple FNV-1a — adequate for tiny header-name keys.
            const uint OffsetBasis = 2166136261;
            const uint Prime = 16777619;
            uint hash = OffsetBasis;
            foreach (byte b in obj)
            {
                hash ^= b;
                hash *= Prime;
            }
            return (int)hash;
        }
    }
}
