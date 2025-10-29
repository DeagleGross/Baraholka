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


        | Method                                                   | _bytesToFillCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
        |--------------------------------------------------------- |------------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
        | Default                                                  | 3                 |  8.424 ns | 0.1099 ns | 0.0974 ns |  1.00 |    0.02 | 0.0005 |      40 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 3                 | 40.227 ns | 0.6520 ns | 0.5445 ns |  4.78 |    0.08 | 0.0014 |     112 B |        2.80 |
        | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 3                 | 28.407 ns | 0.5723 ns | 0.5073 ns |  3.37 |    0.07 | 0.0015 |     112 B |        2.80 |
        | Span_ArrayBufferWriter                                   | 3                 | 16.804 ns | 0.2511 ns | 0.2226 ns |  1.99 |    0.03 | 0.0009 |      72 B |        1.80 |
        | Span_PooledArrayBufferWriter                             | 3                 | 29.140 ns | 0.5138 ns | 0.4555 ns |  3.46 |    0.07 | 0.0004 |      32 B |        0.80 |
        | Span_MyPooledArrayBufferWriter                           | 3                 | 18.045 ns | 0.3825 ns | 0.3578 ns |  2.14 |    0.05 | 0.0004 |      32 B |        0.80 |
        | FullyManualWithPooling                                   | 3                 |  9.629 ns | 0.2024 ns | 0.2165 ns |  1.14 |    0.03 |      - |         - |        0.00 |
        |                                                          |                   |           |           |           |       |         |        |           |             |
        | Default                                                  | 10                |  9.474 ns | 0.2099 ns | 0.2246 ns |  1.00 |    0.03 | 0.0006 |      48 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 10                | 40.266 ns | 0.8209 ns | 0.6855 ns |  4.25 |    0.12 | 0.0015 |     120 B |        2.50 |
        | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 10                | 27.201 ns | 0.5620 ns | 0.5257 ns |  2.87 |    0.08 | 0.0015 |     120 B |        2.50 |
        | Span_ArrayBufferWriter                                   | 10                | 17.012 ns | 0.3612 ns | 0.6035 ns |  1.80 |    0.08 | 0.0010 |      80 B |        1.67 |
        | Span_PooledArrayBufferWriter                             | 10                | 27.529 ns | 0.2640 ns | 0.2470 ns |  2.91 |    0.07 | 0.0004 |      32 B |        0.67 |
        | Span_MyPooledArrayBufferWriter                           | 10                | 17.739 ns | 0.2138 ns | 0.2000 ns |  1.87 |    0.05 | 0.0004 |      32 B |        0.67 |
        | FullyManualWithPooling                                   | 10                |  9.432 ns | 0.2066 ns | 0.3277 ns |  1.00 |    0.04 |      - |         - |        0.00 |
        |                                                          |                   |           |           |           |       |         |        |           |             |
        | Default                                                  | 25                |  9.376 ns | 0.2081 ns | 0.3241 ns |  1.00 |    0.05 | 0.0008 |      64 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 25                | 43.886 ns | 0.8983 ns | 1.7940 ns |  4.69 |    0.24 | 0.0017 |     136 B |        2.12 |
        | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 25                | 30.065 ns | 0.5481 ns | 0.5127 ns |  3.21 |    0.12 | 0.0017 |     136 B |        2.12 |
        | Span_ArrayBufferWriter                                   | 25                | 18.440 ns | 0.3860 ns | 0.5778 ns |  1.97 |    0.09 | 0.0012 |      96 B |        1.50 |
        | Span_PooledArrayBufferWriter                             | 25                | 28.576 ns | 0.5783 ns | 1.0280 ns |  3.05 |    0.15 | 0.0004 |      32 B |        0.50 |
        | Span_MyPooledArrayBufferWriter                           | 25                | 18.233 ns | 0.3761 ns | 0.4024 ns |  1.95 |    0.08 | 0.0004 |      32 B |        0.50 |
        | FullyManualWithPooling                                   | 25                | 10.016 ns | 0.2239 ns | 0.3863 ns |  1.07 |    0.05 |      - |         - |        0.00 |
        |                                                          |                   |           |           |           |       |         |        |           |             |
        | Default                                                  | 50                | 11.589 ns | 0.2526 ns | 0.4289 ns |  1.00 |    0.05 | 0.0011 |      88 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter   | 50                | 43.670 ns | 0.7217 ns | 0.8591 ns |  3.77 |    0.15 | 0.0020 |     160 B |        1.82 |
        | DefaultInterfaceImplementation_MyPooledArrayBufferWriter | 50                | 33.066 ns | 0.6679 ns | 0.7147 ns |  2.86 |    0.12 | 0.0021 |     160 B |        1.82 |
        | Span_ArrayBufferWriter                                   | 50                | 18.964 ns | 0.3590 ns | 0.3842 ns |  1.64 |    0.07 | 0.0015 |     120 B |        1.36 |
        | Span_PooledArrayBufferWriter                             | 50                | 30.593 ns | 0.5678 ns | 0.6973 ns |  2.64 |    0.11 | 0.0004 |      32 B |        0.36 |
        | Span_MyPooledArrayBufferWriter                           | 50                | 18.503 ns | 0.3598 ns | 0.3189 ns |  1.60 |    0.06 | 0.0004 |      32 B |        0.36 |
        | FullyManualWithPooling                                   | 50                |  9.924 ns | 0.2149 ns | 0.3650 ns |  0.86 |    0.04 |      - |         - |        0.00 |
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
            using var arrayBufferWriter = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _simpleProtector.Protect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

        [Benchmark]
        public int DefaultInterfaceImplementation_MyPooledArrayBufferWriter()
        {
            using var arrayBufferWriter = new MyPooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
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
