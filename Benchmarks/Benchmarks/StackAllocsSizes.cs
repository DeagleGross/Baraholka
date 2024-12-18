using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    /*
        | Method            | Mean       | Error     | StdDev    | Allocated |
        |------------------ |-----------:|----------:|----------:|----------:|
        | OneBig            |  6.2626 ns | 0.0571 ns | 0.0506 ns |         - |
        | OneBigWithSkip    |  0.3302 ns | 0.0116 ns | 0.0108 ns |         - |
        | TwoSmall          | 11.6515 ns | 0.2354 ns | 0.2418 ns |         - |
        | TwoSmallWithSkip  |  0.6846 ns | 0.0089 ns | 0.0083 ns |         - |
        | FourMicro         |  5.3126 ns | 0.0318 ns | 0.0282 ns |         - |
        | FourMicroWithSkip |  1.6459 ns | 0.0437 ns | 0.0409 ns |         - |
     */

    [MemoryDiagnoser]
    public unsafe class StackAllocsSizes
    {
        [Benchmark]
        public int OneBig()
        {
            var arr = stackalloc byte[1024];
            return arr[1];
        }

        [Benchmark, SkipLocalsInit]
        public int OneBigWithSkip()
        {
            var arr = stackalloc byte[1024];
            return arr[1];
        }

        [Benchmark]
        public int TwoSmall()
        {
            var arr1 = stackalloc byte[512];
            var arr2 = stackalloc byte[512];

            return arr1[1] + arr2[1];
        }

        [Benchmark, SkipLocalsInit]
        public int TwoSmallWithSkip()
        {
            var arr1 = stackalloc byte[512];
            var arr2 = stackalloc byte[512];

            return arr1[1] + arr2[1];
        }

        [Benchmark]
        public int FourMicro()
        {
            var arr1 = stackalloc byte[256];
            var arr2 = stackalloc byte[256];
            var arr3 = stackalloc byte[256];
            var arr4 = stackalloc byte[256];

            return arr1[1] + arr2[1] + arr3[1] + arr4[1];
        }

        [Benchmark, SkipLocalsInit]
        public int FourMicroWithSkip()
        {
            var arr1 = stackalloc byte[256];
            var arr2 = stackalloc byte[256];
            var arr3 = stackalloc byte[256];
            var arr4 = stackalloc byte[256];

            return arr1[1] + arr2[1] + arr3[1] + arr4[1];
        }
    }
}
