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
        /*
            BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4602/23H2/2023Update/SunValley3)
            AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
            .NET SDK 9.0.100-rc.2.24474.11
              [Host]     : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
              DefaultJob : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI 

            | Method                     | Mean      | Error     | StdDev    |
            |--------------------------- |----------:|----------:|----------:|
            | FixedSize_Init_16          | 0.1786 ns | 0.0223 ns | 0.0197 ns |
            | FixedSize_Init_64          | 1.0648 ns | 0.0223 ns | 0.0209 ns |
            | FixedSize_Init_128         | 1.8015 ns | 0.0131 ns | 0.0116 ns |
            | FixedSize_Init_256         | 3.2279 ns | 0.0459 ns | 0.0429 ns |
            | FixedSize_SkipInit_16      | 0.2090 ns | 0.0040 ns | 0.0031 ns |
            | FixedSize_SkipInit_64      | 0.2157 ns | 0.0188 ns | 0.0167 ns |
            | FixedSize_SkipInit_128     | 0.1878 ns | 0.0033 ns | 0.0030 ns |
            | FixedSize_SkipInit_256     | 0.1944 ns | 0.0067 ns | 0.0056 ns |
            | VariableSize_Init_16       | 0.5183 ns | 0.0055 ns | 0.0046 ns |
            | VariableFixedSize_Init_64  | 1.0997 ns | 0.0131 ns | 0.0116 ns |
            | VariableFixedSize_Init_128 | 1.7970 ns | 0.0222 ns | 0.0207 ns |
            | VariableFixedSize_Init_256 | 3.2626 ns | 0.0518 ns | 0.0485 ns |
            | VariableSize_SkipInit_16   | 1.2459 ns | 0.0285 ns | 0.0267 ns |
            | VariableSize_SkipInit_64   | 1.2352 ns | 0.0183 ns | 0.0171 ns |
            | VariableSize_SkipInit_128  | 1.2395 ns | 0.0269 ns | 0.0252 ns |
            | VariableSize_SkipInit_256  | 1.2432 ns | 0.0327 ns | 0.0306 ns |
        */

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
