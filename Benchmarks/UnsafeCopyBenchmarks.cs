using BenchmarkDotNet.Attributes;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Benchmarks
{
    /*
        | Method            | N    | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
        |------------------ |----- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
        | Buffer_MemoryCopy | 10   |  1.328 ns | 0.0180 ns | 0.0160 ns |  1.335 ns |  1.00 |    0.02 |         - |          NA |
        | Unsafe_CopyBlock  | 10   |  1.133 ns | 0.0195 ns | 0.0163 ns |  1.128 ns |  0.85 |    0.02 |         - |          NA |
        | NativeMemory_Copy | 10   |  1.379 ns | 0.0468 ns | 0.0856 ns |  1.331 ns |  1.04 |    0.06 |         - |          NA |
        |                   |      |           |           |           |           |       |         |           |             |
        | Buffer_MemoryCopy | 100  |  1.863 ns | 0.0149 ns | 0.0132 ns |  1.859 ns |  1.00 |    0.01 |         - |          NA |
        | Unsafe_CopyBlock  | 100  |  1.704 ns | 0.0180 ns | 0.0168 ns |  1.703 ns |  0.92 |    0.01 |         - |          NA |
        | NativeMemory_Copy | 100  |  1.869 ns | 0.0242 ns | 0.0226 ns |  1.862 ns |  1.00 |    0.01 |         - |          NA |
        |                   |      |           |           |           |           |       |         |           |             |
        | Buffer_MemoryCopy | 1000 | 10.254 ns | 0.0967 ns | 0.0904 ns | 10.269 ns |  1.00 |    0.01 |         - |          NA |
        | Unsafe_CopyBlock  | 1000 | 10.113 ns | 0.1074 ns | 0.1005 ns | 10.100 ns |  0.99 |    0.01 |         - |          NA |
        | NativeMemory_Copy | 1000 | 10.291 ns | 0.1068 ns | 0.0999 ns | 10.273 ns |  1.00 |    0.01 |         - |          NA |
    */

    [MemoryDiagnoser]
    public class UnsafeCopyBenchmarks
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

        [Benchmark(Baseline = true)]
        public unsafe void Buffer_MemoryCopy()
        {
            fixed (byte* sourcePtr = _source)
            fixed (byte* destPtr = _destination)
            {
                var byteCount = (uint)BlockLength;
                Buffer.MemoryCopy(sourcePtr, destPtr + 3, byteCount, byteCount);
            }
        }

        [Benchmark]
        public unsafe void Unsafe_CopyBlock()
        {
            fixed (byte* sourcePtr = _source)
            fixed (byte* destPtr = _destination)
            {
                Unsafe.CopyBlock(destPtr + 3, sourcePtr, (uint)BlockLength);
            }
        }

        [Benchmark]
        public unsafe void NativeMemory_Copy()
        {
            fixed (byte* sourcePtr = _source)
            fixed (byte* destPtr = _destination)
            {
                NativeMemory.Copy(sourcePtr, destPtr + 3, (uint)BlockLength);
            }
        }
    }
}
