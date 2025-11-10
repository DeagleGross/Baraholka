using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Exporters;
using Benchmarks.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Buffers;
using System.Formats.Tar;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmarks
{
    /*     
        // * Summary *
        BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.6899)
        AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
        .NET SDK 10.0.100-rc.2.25502.107
          [Host]     : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
          DefaultJob : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

    | Method                                                 | _bytesToFillCount | Mean         | Error      | StdDev      | Median       | Ratio  | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
    |------------------------------------------------------- |------------------ |-------------:|-----------:|------------:|-------------:|-------:|--------:|-------:|-------:|-------:|----------:|------------:|
    | FullyManualWithStackalloc                              | 25                |     5.896 ns |  0.1380 ns |   0.1979 ns |     5.807 ns |   0.65 |    0.03 |      - |      - |      - |         - |        0.00 |
    | FullyManualWithPooling                                 | 25                |     9.515 ns |  0.2092 ns |   0.2932 ns |     9.466 ns |   1.05 |    0.04 |      - |      - |      - |         - |        0.00 |
    | Default                                                | 25                |     9.097 ns |  0.1937 ns |   0.2900 ns |     9.030 ns |   1.00 |    0.04 | 0.0008 |      - |      - |      64 B |        1.00 |
    | DefaultInterfaceImplementation_PooledArrayBufferWriter | 25                |    28.996 ns |  0.6014 ns |   0.7605 ns |    28.639 ns |   3.19 |    0.13 | 0.0018 |      - |      - |     136 B |        2.12 |
    | Span_PooledArrayBufferWriter                           | 25                |    14.886 ns |  0.2476 ns |   0.1933 ns |    14.958 ns |   1.64 |    0.05 | 0.0004 |      - |      - |      32 B |        0.50 |
    | SpanCallingGeneric_PooledArrayBufferWriter             | 25                |    22.462 ns |  0.4678 ns |   1.0655 ns |    22.270 ns |   2.47 |    0.14 | 0.0004 | 0.0000 | 0.0000 |         - |        0.00 |
    | Span_StructPooledArrayBufferWriter                     | 25                |     9.142 ns |  0.1098 ns |   0.0916 ns |     9.127 ns |   1.01 |    0.03 |      - |      - |      - |         - |        0.00 |
    | Span_PooledArrayBufferWriter_Parallel                  | 25                | 1,453.068 ns | 37.1612 ns | 109.5706 ns | 1,445.777 ns | 159.89 |   12.95 | 0.0305 |      - |      - |    2391 B |       37.36 |
    | Span_StructPooledArrayBufferWriter_Parallel            | 25                | 1,428.240 ns | 26.0955 ns |  50.2772 ns | 1,429.562 ns | 157.16 |    7.27 | 0.0267 |      - |      - |    2104 B |       32.88 |
    |                                                        |                   |              |            |             |              |        |         |        |        |        |           |             |
    | FullyManualWithStackalloc                              | 80                |     5.770 ns |  0.1370 ns |   0.2212 ns |     5.694 ns |   0.46 |    0.03 |      - |      - |      - |         - |        0.00 |
    | FullyManualWithPooling                                 | 80                |     9.982 ns |  0.2174 ns |   0.3511 ns |     9.923 ns |   0.79 |    0.05 |      - |      - |      - |         - |        0.00 |
    | Default                                                | 80                |    12.608 ns |  0.2736 ns |   0.6865 ns |    12.338 ns |   1.00 |    0.08 | 0.0015 |      - |      - |     120 B |        1.00 |
    | DefaultInterfaceImplementation_PooledArrayBufferWriter | 80                |    32.962 ns |  0.6472 ns |   1.0075 ns |    32.472 ns |   2.62 |    0.16 | 0.0024 |      - |      - |     192 B |        1.60 |
    | Span_PooledArrayBufferWriter                           | 80                |    15.792 ns |  0.3328 ns |   0.5083 ns |    15.667 ns |   1.26 |    0.08 | 0.0005 | 0.0000 | 0.0000 |         - |        0.00 |
    | SpanCallingGeneric_PooledArrayBufferWriter             | 80                |    21.997 ns |  0.2738 ns |   0.2138 ns |    21.956 ns |   1.75 |    0.09 | 0.0004 |      - |      - |      32 B |        0.27 |
    | Span_StructPooledArrayBufferWriter                     | 80                |    10.503 ns |  0.1617 ns |   0.1350 ns |    10.459 ns |   0.84 |    0.04 |      - |      - |      - |         - |        0.00 |
    | Span_PooledArrayBufferWriter_Parallel                  | 80                | 1,461.830 ns | 28.8943 ns |  51.3596 ns | 1,466.782 ns | 116.27 |    7.24 | 0.0305 |      - |      - |    2432 B |       20.27 |
    | Span_StructPooledArrayBufferWriter_Parallel            | 80                | 1,431.181 ns | 28.4001 ns |  66.9423 ns | 1,434.755 ns | 113.83 |    7.91 | 0.0267 |      - |      - |    2110 B |       17.58 |
    |                                                        |                   |              |            |             |              |        |         |        |        |        |           |             |
    | FullyManualWithStackalloc                              | 120               |     7.389 ns |  0.1269 ns |   0.1060 ns |     7.371 ns |   0.49 |    0.01 |      - |      - |      - |         - |        0.00 |
    | FullyManualWithPooling                                 | 120               |     9.884 ns |  0.2154 ns |   0.2480 ns |     9.809 ns |   0.66 |    0.02 |      - |      - |      - |         - |        0.00 |
    | Default                                                | 120               |    14.966 ns |  0.2725 ns |   0.2128 ns |    14.981 ns |   1.00 |    0.02 | 0.0021 |      - |      - |     160 B |        1.00 |
    | DefaultInterfaceImplementation_PooledArrayBufferWriter | 120               |    38.251 ns |  0.7945 ns |   1.5682 ns |    37.868 ns |   2.56 |    0.11 | 0.0030 |      - |      - |     232 B |        1.45 |
    | Span_PooledArrayBufferWriter                           | 120               |    16.561 ns |  0.3532 ns |   0.6092 ns |    16.318 ns |   1.11 |    0.04 | 0.0004 |      - |      - |      32 B |        0.20 |
    | SpanCallingGeneric_PooledArrayBufferWriter             | 120               |    23.765 ns |  0.4960 ns |   0.6621 ns |    23.568 ns |   1.59 |    0.05 | 0.0004 |      - |      - |      32 B |        0.20 |
    | Span_StructPooledArrayBufferWriter                     | 120               |    11.796 ns |  0.1637 ns |   0.1278 ns |    11.786 ns |   0.79 |    0.01 |      - |      - |      - |         - |        0.00 |
    | Span_PooledArrayBufferWriter_Parallel                  | 120               | 1,515.167 ns | 30.0954 ns |  55.7838 ns | 1,524.555 ns | 101.26 |    3.94 | 0.0572 |      - |      - |    2412 B |       15.07 |
    | Span_StructPooledArrayBufferWriter_Parallel            | 120               | 1,468.083 ns | 29.1282 ns |  65.1493 ns | 1,465.638 ns |  98.11 |    4.52 | 0.0267 |      - |      - |    2129 B |       13.31 |

     */

    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class DIMvsDedicatedImplementation
    {
        private int _parallelThreadCount = 8;

        [Params(25, 80, 120)]
        public int _bytesToFillCount;
        private byte[] _bytesToFill;

        private byte[] _plainText;

        private IMyDataProtector _simpleProtector;
        private ISpanMyDataProtector _spanProtector;

        [GlobalSetup]
        public void Setup()
        {
            _bytesToFill = new byte[_bytesToFillCount];
            RandomNumberGenerator.Fill(_bytesToFill);            

            _simpleProtector = new SimpleProtector(_bytesToFill);
            _spanProtector = new SpanProtector(_bytesToFill);

            _plainText = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        }

        // whatever i can do without any underlying types
        [Benchmark]
        public unsafe int FullyManualWithStackalloc()
        {
            Span<byte> rent = stackalloc byte[_plainText.Length + _bytesToFill.Length];
            _plainText.CopyTo(rent);
            _bytesToFill.CopyTo(rent.Slice(_plainText.Length));
            return rent.Length;
        }

        // whatever i can do without any underlying types
        [Benchmark]
        public int FullyManualWithPooling()
        {
            var rent = ArrayPool<byte>.Shared.Rent(_plainText.Length + _bytesToFillCount);
            try
            {
                Span<byte> span = rent.AsSpan(0, _plainText.Length + _bytesToFillCount);
                _plainText.CopyTo(span);
                _bytesToFill.CopyTo(span.Slice(_plainText.Length));
                return span.Length;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rent);
            }
        }

        // original implementation
        [Benchmark(Baseline = true)]
        public int Default()
        {
            var result = _simpleProtector.Protect(_plainText);
            return result.Length;
        }

        [Benchmark]
        public int DefaultInterfaceImplementation_PooledArrayBufferWriter()
        {
            using var buffer = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _simpleProtector.Protect(_plainText, buffer);
            return buffer.WrittenCount;
        }

        // an improved interface using Span<byte> with PooledArrayBufferWriter
        [Benchmark]
        public int Span_PooledArrayBufferWriter()
        {
            using var buffer = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _spanProtector.SpanProtectNormal(_plainText, buffer);
            return buffer.WrittenCount;
        }

        // an improved interface using Span<byte> with PooledArrayBufferWriter
        [Benchmark]
        public int SpanCallingGeneric_PooledArrayBufferWriter()
        {
            var buffer = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            try
            {
                _spanProtector.SpanProtectGeneric(_plainText, ref buffer);
                return buffer.WrittenCount;
            }
            finally
            {
                buffer.Dispose();
            }
        }

        // an improved interface using Span<byte> with StructPooledArrayBufferWriter
        [Benchmark]
        public int Span_StructPooledArrayBufferWriter()
        {
            Span<byte> bytes = stackalloc byte[_plainText.Length + _bytesToFillCount];
            var buffer = new StructArrayBufferWriter<byte>(bytes);
            try
            {
                _spanProtector.SpanProtectGeneric(_plainText, ref buffer);
                return buffer.WrittenSpan.Length;
            }
            finally
            {
                buffer.Dispose();
            }
        }

        [Benchmark]
        public int Span_PooledArrayBufferWriter_Parallel()
        {
            int totalCount = 0;
            Parallel.For(0, _parallelThreadCount, _ =>
            {
                using var buffer = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
                _spanProtector.SpanProtectNormal(_plainText, buffer);
                Interlocked.Add(ref totalCount, buffer.WrittenCount);
            });
            return totalCount;
        }

        [Benchmark]
        public int Span_StructPooledArrayBufferWriter_Parallel()
        {
            int totalCount = 0;
            Parallel.For(0, _parallelThreadCount, _ =>
            {
                Span<byte> bytes = stackalloc byte[_plainText.Length + _bytesToFillCount];
                var buffer = new StructArrayBufferWriter<byte>(bytes);
                try
                {
                    _spanProtector.SpanProtectGeneric(_plainText, ref buffer);
                    Interlocked.Add(ref totalCount, buffer.WrittenSpan.Length);
                }
                finally
                {
                    buffer.Dispose();
                }
            });
            return totalCount;
        }
    }

    interface IMyDataProtector
    {
        byte[] Protect(byte[] plaintext);

        void Protect(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination)
        {
            var result = Protect(plaintext.ToArray());
            var span = destination.GetSpan(result.Length);
            result.CopyTo(span);
            destination.Advance(span.Length);
        }
    }

    interface ISpanMyDataProtector : IMyDataProtector
    {
        void SpanProtectGeneric<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct;

        void SpanProtectNormal(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination);
    }

    class SimpleProtector(byte[] _bytesToFill) : IMyDataProtector
    {
        public byte[] Protect(byte[] plaintext)
        {
            // if we do `return [..plainText, .._bytesToFill]` then it will do span copies.
            // we assume that default protect implementation allocates the output array and return it with inner copy of data

            var result = new byte[plaintext.Length + _bytesToFill.Length];
            Array.Copy(plaintext, 0, result, 0, plaintext.Length);
            Array.Copy(_bytesToFill, 0, result, plaintext.Length, _bytesToFill.Length);

            return result;
        }
    }

    class SpanProtector(byte[] _bytesToFill) : ISpanMyDataProtector
    {
        public byte[] Protect(byte[] plaintext) => throw new NotImplementedException();

        public void SpanProtectGeneric<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
        {
            var span = destination.GetSpan(sizeHint: plaintext.Length + _bytesToFill.Length);
            plaintext.CopyTo(span);
            _bytesToFill.CopyTo(span.Slice(plaintext.Length));
            destination.Advance(plaintext.Length + _bytesToFill.Length);
        }

        public void SpanProtectNormal(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination)
        {
            var span = destination.GetSpan(sizeHint: plaintext.Length + _bytesToFill.Length);
            plaintext.CopyTo(span);
            _bytesToFill.CopyTo(span.Slice(plaintext.Length));
            destination.Advance(plaintext.Length + _bytesToFill.Length);
        }
    }
}

internal class Config : ManualConfig
{
    public Config()
    {
        AddExporter(MarkdownExporter.GitHub);
        // Don't add CsvExporter
    }
}
