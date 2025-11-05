using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Benchmarks.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Buffers;
using System.Formats.Tar;
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

        | Method                                                 | _bytesToFillCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
        |------------------------------------------------------- |------------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
        | FullyManualWithPooling                                 | 25                |  8.154 ns | 0.0677 ns | 0.0633 ns |  8.150 ns |  0.85 |    0.01 |      - |         - |        0.00 |
        | Default                                                | 25                |  9.621 ns | 0.1420 ns | 0.1328 ns |  9.632 ns |  1.00 |    0.02 | 0.0008 |      64 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 25                | 31.398 ns | 0.5987 ns | 0.5308 ns | 31.177 ns |  3.26 |    0.07 | 0.0017 |     136 B |        2.12 |
        | Span_PooledArrayBufferWriter                           | 25                | 21.136 ns | 0.3225 ns | 0.3017 ns | 21.141 ns |  2.20 |    0.04 | 0.0004 |      32 B |        0.50 |
        | Span_StructPooledArrayBufferWriter                     | 25                | 13.074 ns | 0.1039 ns | 0.0868 ns | 13.107 ns |  1.36 |    0.02 |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |           |       |         |        |           |             |
        | FullyManualWithPooling                                 | 50                | 13.262 ns | 0.2826 ns | 0.4230 ns | 13.041 ns |  1.34 |    0.05 |      - |         - |        0.00 |
        | Default                                                | 50                |  9.932 ns | 0.1992 ns | 0.1765 ns |  9.897 ns |  1.00 |    0.02 | 0.0011 |      88 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 50                | 29.993 ns | 0.4535 ns | 0.4242 ns | 29.956 ns |  3.02 |    0.07 | 0.0021 |     160 B |        1.82 |
        | Span_PooledArrayBufferWriter                           | 50                | 21.132 ns | 0.2617 ns | 0.2320 ns | 21.141 ns |  2.13 |    0.04 | 0.0012 |      32 B |        0.36 |
        | Span_StructPooledArrayBufferWriter                     | 50                | 14.068 ns | 0.1998 ns | 0.1669 ns | 14.010 ns |  1.42 |    0.03 |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |           |       |         |        |           |             |
        | FullyManualWithPooling                                 | 120               |  9.291 ns | 0.0728 ns | 0.0568 ns |  9.317 ns |  0.65 |    0.01 |      - |         - |        0.00 |
        | Default                                                | 120               | 14.297 ns | 0.2568 ns | 0.2145 ns | 14.277 ns |  1.00 |    0.02 | 0.0021 |     160 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 120               | 40.689 ns | 0.8329 ns | 1.5644 ns | 41.103 ns |  2.85 |    0.12 | 0.0030 |     232 B |        1.45 |
        | Span_PooledArrayBufferWriter                           | 120               | 20.107 ns | 0.2128 ns | 0.1887 ns | 20.146 ns |  1.41 |    0.02 | 0.0004 |      32 B |        0.20 |
        | Span_StructPooledArrayBufferWriter                     | 120               | 12.419 ns | 0.0894 ns | 0.0836 ns | 12.406 ns |  0.87 |    0.01 |      - |         - |        0.00 |

     */

    [MemoryDiagnoser]
    // [EtwProfiler]
    public class DIMvsDedicatedImplementation
    {
        [Params(25, 50, 120)]
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

        // whatever i can do without any underlying types. The maximum speed we can reach.
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

        // default interface invocation with PooledArrayBufferWriter
        [Benchmark]
        public int DefaultInterfaceImplementation_PooledArrayBufferWriter()
        {
            using var arrayBufferWriter = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _simpleProtector.Protect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

        // an improved interface using Span<byte> with PooledArrayBufferWriter
        [Benchmark]
        public int Span_PooledArrayBufferWriter()
        {
            using var arrayBufferWriter = new PooledArrayBufferWriter<byte>(initialCapacity: _plainText.Length + _bytesToFillCount);
            _spanProtector.SpanProtect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
        }

        // an improved interface using Span<byte> with StructPooledArrayBufferWriter
        [Benchmark]
        public int Span_StructPooledArrayBufferWriter()
        {
            using var arrayBufferWriter = new StructArrayBufferWriter(initialCapacity: _plainText.Length + _bytesToFillCount);
            _spanProtector.SpanProtect(_plainText, arrayBufferWriter);
            return arrayBufferWriter.WrittenCount;
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
        void SpanProtect<TWriter>(ReadOnlySpan<byte> plaintext, TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct;
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

        public void SpanProtect<TWriter>(ReadOnlySpan<byte> plaintext, TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
        {
            var span = destination.GetSpan(sizeHint: plaintext.Length + _bytesToFill.Length);
            plaintext.CopyTo(span);
            _bytesToFill.CopyTo(span.Slice(plaintext.Length));
            destination.Advance(plaintext.Length + _bytesToFill.Length);
        }
    }
}
