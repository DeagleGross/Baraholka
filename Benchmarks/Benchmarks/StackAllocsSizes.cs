using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    /*
     | Method    | Mean     | Error     | StdDev    | Allocated |
     |---------- |---------:|----------:|----------:|----------:|
     | OneBig    | 5.819 ns | 0.0585 ns | 0.0547 ns |         - |
     | TwoSmall  | 5.199 ns | 0.0701 ns | 0.0655 ns |         - |
     | FourMicro | 6.102 ns | 0.0222 ns | 0.0197 ns |         - |
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

        [Benchmark]
        public int TwoSmall()
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
    }
}
