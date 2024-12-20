using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    public class StackAllocCost
    {
        public int Size16 { get; set; } = 16;
        public int Size64 { get; set; } = 64;
        public int Size128 { get; set; } = 128;
        public int Size256 { get; set; } = 256;

        [Benchmark] public void FixedSize_Init_16() => DoNothing(stackalloc byte[16]);
        [Benchmark] public void FixedSize_Init_64() => DoNothing(stackalloc byte[64]);
        [Benchmark] public void FixedSize_Init_128() => DoNothing(stackalloc byte[128]);
        [Benchmark] public void FixedSize_Init_256() => DoNothing(stackalloc byte[256]);

        [Benchmark, SkipLocalsInit] public void FixedSize_SkipInit_16() => DoNothing(stackalloc byte[16]);
        [Benchmark, SkipLocalsInit] public void FixedSize_SkipInit_64() => DoNothing(stackalloc byte[64]);
        [Benchmark, SkipLocalsInit] public void FixedSize_SkipInit_128() => DoNothing(stackalloc byte[128]);
        [Benchmark, SkipLocalsInit] public void FixedSize_SkipInit_256() => DoNothing(stackalloc byte[256]);

        [Benchmark] public void VariableSize_Init_16() => DoNothing(stackalloc byte[Size16]);
        [Benchmark] public void VariableFixedSize_Init_64() => DoNothing(stackalloc byte[Size64]);
        [Benchmark] public void VariableFixedSize_Init_128() => DoNothing(stackalloc byte[Size128]);
        [Benchmark] public void VariableFixedSize_Init_256() => DoNothing(stackalloc byte[Size256]);

        [Benchmark, SkipLocalsInit] public void VariableSize_SkipInit_16() => DoNothing(stackalloc byte[Size16]);
        [Benchmark, SkipLocalsInit] public void VariableSize_SkipInit_64() => DoNothing(stackalloc byte[Size64]);
        [Benchmark, SkipLocalsInit] public void VariableSize_SkipInit_128() => DoNothing(stackalloc byte[Size128]);
        [Benchmark, SkipLocalsInit] public void VariableSize_SkipInit_256() => DoNothing(stackalloc byte[Size256]);


        private static int DoNothing(ReadOnlySpan<byte> bytes) => bytes.Length;
    }
}
