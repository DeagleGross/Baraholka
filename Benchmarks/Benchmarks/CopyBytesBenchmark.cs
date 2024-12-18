using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    /*
        BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4460/23H2/2023Update/SunValley3)
        AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
        .NET SDK 9.0.100-rc.2.24474.11
          [Host]     : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
          DefaultJob : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


        | Method           | N    | Mean      | Error     | StdDev    | Allocated |
        |----------------- |----- |----------:|----------:|----------:|----------:|
        | Array_Copy       | 10   |  2.402 ns | 0.0181 ns | 0.0142 ns |         - |
        | Buffer_BlockCopy | 10   |  3.475 ns | 0.0232 ns | 0.0217 ns |         - |
        | Span_CopyTo      | 10   |  1.126 ns | 0.0139 ns | 0.0124 ns |         - |
        | Array_Copy       | 100  |  3.755 ns | 0.0313 ns | 0.0277 ns |         - |
        | Buffer_BlockCopy | 100  |  4.188 ns | 0.0419 ns | 0.0392 ns |         - |
        | Span_CopyTo      | 100  |  2.660 ns | 0.0196 ns | 0.0183 ns |         - |
        | Array_Copy       | 1000 | 10.202 ns | 0.1154 ns | 0.0964 ns |         - |
        | Buffer_BlockCopy | 1000 | 12.023 ns | 0.1411 ns | 0.1251 ns |         - |
        | Span_CopyTo      | 1000 |  8.986 ns | 0.1847 ns | 0.1814 ns |         - |
     */

    [MemoryDiagnoser]
    public class CopyBytesBenchmark
    {
        private byte[] _source;
        private byte[] _destination;
        
        [Params(10, 100, 1000)]
        public int N;

        public int BlockLength => N - 3;

        [GlobalSetup]
        public void Setup()
        {
            _source = new byte[N];
            RandomNumberGenerator.Fill(_source);

            _destination = new byte[N + 5];
            RandomNumberGenerator.Fill(_destination);
        }

        [Benchmark]
        public void Array_Copy()
        {
            Array.Copy(_source, 0, _destination, 3, BlockLength);
        }

        [Benchmark(Baseline = true)]
        public void Buffer_BlockCopy()
        {
            Buffer.BlockCopy(_source, 0, _destination, 3, BlockLength);
        }

        [Benchmark]
        public void Span_CopyTo()
        {
            var sourceSpan = _source.AsSpan();
            var destinationSpan = _destination.AsSpan();

            sourceSpan.CopyTo(destinationSpan.Slice(3));
        }
    }
}
