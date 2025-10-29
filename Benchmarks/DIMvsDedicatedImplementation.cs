using BenchmarkDotNet.Attributes;
using Benchmarks.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Buffers;
using System.Security.Cryptography;

namespace Benchmarks
{
    /*     
        // * Summary *
        BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.6899)
        AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
        .NET SDK 10.0.100-rc.2.25502.107
          [Host]     : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
          DefaultJob : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


            | Method                                                   | _bytesToFillCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
            |--------------------------------------------------------- |------------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
            | Default                                                  | 3                 |  7.792 ns | 0.1694 ns | 0.2142 ns |  7.741 ns |  1.00 |    0.04 | 0.0005 |      40 B |        1.00 |
            | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 3                 | 63.777 ns | 2.1516 ns | 6.2079 ns | 61.547 ns |  8.19 |    0.82 | 0.0083 |     648 B |       16.20 |
            | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 3                 | 26.080 ns | 0.5247 ns | 0.4651 ns | 26.003 ns |  3.35 |    0.11 | 0.0020 |     152 B |        3.80 |
            | Span_ArrayBufferWriter                                   | 3                 | 14.908 ns | 0.2770 ns | 0.5270 ns | 14.745 ns |  1.91 |    0.08 | 0.0009 |      72 B |        1.80 |
            | Span_PooledArrayBufferWriter                             | 3                 | 23.997 ns | 0.3884 ns | 0.3443 ns | 23.922 ns |  3.08 |    0.09 | 0.0005 |      32 B |        0.80 |
            | Span_MyPooledArrayBufferWriter                           | 3                 | 15.445 ns | 0.1976 ns | 0.1751 ns | 15.382 ns |  1.98 |    0.06 | 0.0004 |      32 B |        0.80 |
            | FullyManualWithPooling                                   | 3                 |  8.200 ns | 0.0771 ns | 0.0683 ns |  8.181 ns |  1.05 |    0.03 |      - |         - |        0.00 |
            |                                                          |                   |           |           |           |           |       |         |        |           |             |
            | Default                                                  | 10                |  7.890 ns | 0.1617 ns | 0.1797 ns |  7.873 ns |  1.00 |    0.03 | 0.0006 |      48 B |        1.00 |
            | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 10                | 55.255 ns | 0.9995 ns | 1.1510 ns | 55.478 ns |  7.01 |    0.21 | 0.0085 |     656 B |       13.67 |
            | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 10                | 32.510 ns | 2.0679 ns | 6.0648 ns | 29.462 ns |  4.12 |    0.77 | 0.0023 |     176 B |        3.67 |
            | Span_ArrayBufferWriter                                   | 10                | 17.508 ns | 0.3669 ns | 0.8576 ns | 17.568 ns |  2.22 |    0.12 | 0.0010 |      80 B |        1.67 |
            | Span_PooledArrayBufferWriter                             | 10                | 29.300 ns | 0.6164 ns | 1.3399 ns | 28.853 ns |  3.72 |    0.19 | 0.0004 |      32 B |        0.67 |
            | Span_MyPooledArrayBufferWriter                           | 10                | 18.144 ns | 0.3362 ns | 0.4488 ns | 18.028 ns |  2.30 |    0.07 | 0.0004 |      32 B |        0.67 |
            | FullyManualWithPooling                                   | 10                |  9.487 ns | 0.1713 ns | 0.1602 ns |  9.491 ns |  1.20 |    0.03 |      - |         - |        0.00 |
            |                                                          |                   |           |           |           |           |       |         |        |           |             |
            | Default                                                  | 25                |  9.799 ns | 0.2177 ns | 0.4824 ns |  9.785 ns |  1.00 |    0.07 | 0.0008 |      64 B |        1.00 |
            | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 25                | 68.288 ns | 1.3850 ns | 3.0978 ns | 67.756 ns |  6.99 |    0.46 | 0.0087 |     672 B |       10.50 |
            | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 25                | 33.950 ns | 0.7055 ns | 1.6629 ns | 33.745 ns |  3.47 |    0.24 | 0.0029 |     224 B |        3.50 |
            | Span_ArrayBufferWriter                                   | 25                | 18.411 ns | 0.3896 ns | 0.6616 ns | 18.259 ns |  1.88 |    0.11 | 0.0012 |      96 B |        1.50 |
            | Span_PooledArrayBufferWriter                             | 25                | 27.746 ns | 0.5726 ns | 0.6127 ns | 27.653 ns |  2.84 |    0.15 | 0.0004 |      32 B |        0.50 |
            | Span_MyPooledArrayBufferWriter                           | 25                | 18.351 ns | 0.3696 ns | 0.4540 ns | 18.300 ns |  1.88 |    0.10 | 0.0004 |      32 B |        0.50 |
            | FullyManualWithPooling                                   | 25                |  9.476 ns | 0.2054 ns | 0.2198 ns |  9.430 ns |  0.97 |    0.05 |      - |         - |        0.00 |
            |                                                          |                   |           |           |           |           |       |         |        |           |             |
            | Default                                                  | 50                | 11.202 ns | 0.2432 ns | 0.3076 ns | 11.186 ns |  1.00 |    0.04 | 0.0011 |      88 B |        1.00 |
            | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 50                | 70.741 ns | 1.4438 ns | 2.1163 ns | 70.444 ns |  6.32 |    0.25 | 0.0091 |     696 B |        7.91 |
            | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 50                | 34.291 ns | 0.6810 ns | 0.6688 ns | 34.274 ns |  3.06 |    0.10 | 0.0032 |     248 B |        2.82 |
            | Span_ArrayBufferWriter                                   | 50                | 18.447 ns | 0.3507 ns | 0.3281 ns | 18.369 ns |  1.65 |    0.05 | 0.0015 |     120 B |        1.36 |
            | Span_PooledArrayBufferWriter                             | 50                | 29.052 ns | 0.5933 ns | 1.0698 ns | 29.158 ns |  2.60 |    0.12 | 0.0004 |      32 B |        0.36 |
            | Span_MyPooledArrayBufferWriter                           | 50                | 18.865 ns | 0.3760 ns | 0.4023 ns | 18.798 ns |  1.69 |    0.06 | 0.0004 |      32 B |        0.36 |
            | FullyManualWithPooling                                   | 50                |  9.565 ns | 0.2093 ns | 0.1747 ns |  9.505 ns |  0.85 |    0.03 |      - |         - |        0.00 |
     */

    [MemoryDiagnoser]
    public class DIMvsDedicatedImplementation
    {
        [Params(3, 10, 25, 50)]
        public int _bytesToFillCount;
        private byte[] _bytesToFill;

        private IMyDataProtector _simpleProtector;
        private ISpanMyDataProtector _spanProtector;

        private byte[] _plainText;

        [GlobalSetup]
        public void Setup()
        {
            _bytesToFill = new byte[_bytesToFillCount];
            RandomNumberGenerator.Fill(_bytesToFill);            

            _simpleProtector = new SimpleProtector(_bytesToFill);
            _spanProtector = new SpanProtector(_bytesToFill);

            _plainText = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        }

        [Benchmark(Baseline = true)]
        public int Default()
        {
            var result = _simpleProtector.Protect(_plainText);
            return result.Length;
        }

        [Benchmark]
        public int DefaultInterfaceImplementation_PooledArrayBufferWriter()
        {
            var arrayBufferWriter = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _simpleProtector.Protect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

        [Benchmark]
        public int DefaultInterfaceImplementation_MyPooledArrayBufferWriter()
        {
            var arrayBufferWriter = new MyPooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _simpleProtector.Protect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

        [Benchmark]
        public int Span_ArrayBufferWriter()
        {
            // to simulate "no-resize" for bufferWriter
            var arrayBufferWriter = new ArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _spanProtector.SpanProtect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

        [Benchmark]
        public int Span_PooledArrayBufferWriter()
        {
            using var arrayBufferWriter = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _spanProtector.SpanProtect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

        [Benchmark]
        public int Span_MyPooledArrayBufferWriter()
        {
            using var arrayBufferWriter = new MyPooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _spanProtector.SpanProtect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

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
    }

    interface IMyDataProtector
    {
        byte[] Protect(byte[] plaintext);

        void Protect(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination)
        {
            var result = Protect(plaintext.ToArray());
            destination.Write(result);
        }
    }

    interface ISpanMyDataProtector : IMyDataProtector
    {
        void SpanProtect(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination);
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

        public void SpanProtect(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination)
        {
            destination.Write(plaintext);
            destination.Write(_bytesToFill);
        }
    }
}
