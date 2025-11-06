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
        | FullyManualWithPooling                                 | 25                |  8.358 ns | 0.1857 ns | 0.3876 ns |  8.196 ns |  0.92 |    0.07 |      - |         - |        0.00 |
        | Default                                                | 25                |  9.112 ns | 0.2262 ns | 0.6526 ns |  8.877 ns |  1.00 |    0.10 | 0.0008 |      64 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 25                | 28.799 ns | 0.5890 ns | 0.8816 ns | 28.637 ns |  3.18 |    0.23 | 0.0018 |     136 B |        2.12 |
        | Span_PooledArrayBufferWriter                           | 25                | 20.684 ns | 0.4257 ns | 0.8501 ns | 20.437 ns |  2.28 |    0.18 | 0.0004 |      32 B |        0.50 |
        | Span_StructPooledArrayBufferWriter                     | 25                | 12.568 ns | 0.2706 ns | 0.3793 ns | 12.434 ns |  1.39 |    0.10 |      - |         - |        0.00 |
        | RefSpan_StructPooledArrayBufferWriter                  | 25                | 18.984 ns | 0.3924 ns | 0.6556 ns | 18.722 ns |  2.09 |    0.16 |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |           |       |         |        |           |             |
        | FullyManualWithPooling                                 | 50                | 12.144 ns | 0.2375 ns | 0.2221 ns | 12.039 ns |  1.11 |    0.05 |      - |         - |        0.00 |
        | Default                                                | 50                | 10.976 ns | 0.2386 ns | 0.5187 ns | 10.816 ns |  1.00 |    0.07 | 0.0011 |      88 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 50                | 29.892 ns | 0.5503 ns | 0.4595 ns | 29.818 ns |  2.73 |    0.13 | 0.0020 |     160 B |        1.82 |
        | Span_PooledArrayBufferWriter                           | 50                | 20.069 ns | 0.4129 ns | 0.6306 ns | 19.883 ns |  1.83 |    0.10 | 0.0004 |      32 B |        0.36 |
        | Span_StructPooledArrayBufferWriter                     | 50                | 13.079 ns | 0.2822 ns | 0.5300 ns | 12.836 ns |  1.19 |    0.07 |      - |         - |        0.00 |
        | RefSpan_StructPooledArrayBufferWriter                  | 50                | 13.999 ns | 0.2065 ns | 0.1724 ns | 13.969 ns |  1.28 |    0.06 |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |           |       |         |        |           |             |
        | FullyManualWithPooling                                 | 120               |  8.534 ns | 0.1493 ns | 0.1247 ns |  8.500 ns |  0.56 |    0.03 |      - |         - |        0.00 |
        | Default                                                | 120               | 15.311 ns | 0.2985 ns | 0.7598 ns | 15.126 ns |  1.00 |    0.07 | 0.0021 |     160 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 120               | 35.321 ns | 0.6023 ns | 0.7397 ns | 35.183 ns |  2.31 |    0.12 | 0.0030 |     232 B |        1.45 |
        | Span_PooledArrayBufferWriter                           | 120               | 21.018 ns | 0.4332 ns | 0.6616 ns | 20.845 ns |  1.38 |    0.08 | 0.0004 |      32 B |        0.20 |
        | Span_StructPooledArrayBufferWriter                     | 120               | 12.910 ns | 0.2486 ns | 0.2076 ns | 12.859 ns |  0.85 |    0.04 |      - |         - |        0.00 |
        | RefSpan_StructPooledArrayBufferWriter                  | 120               | 13.849 ns | 0.2975 ns | 0.3426 ns | 13.714 ns |  0.91 |    0.05 |      - |         - |        0.00 |

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
        private ISpanRefMyDataProtector _spanRefProtector;

        [GlobalSetup]
        public void Setup()
        {
            _bytesToFill = new byte[_bytesToFillCount];
            RandomNumberGenerator.Fill(_bytesToFill);            

            _simpleProtector = new SimpleProtector(_bytesToFill);
            _spanProtector = new SpanProtector(_bytesToFill);
            _spanRefProtector = new SpanRefProtector(_bytesToFill);

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

        [Benchmark]
        public int RefSpan_StructPooledArrayBufferWriter()
        {
            var buffer = new StructArrayBufferWriter(initialCapacity: _plainText.Length + _bytesToFillCount);
            try
            {
                _spanRefProtector.SpanProtect(_plainText, ref buffer);
                return buffer.WrittenCount;
            }
            finally
            {
                buffer.Dispose();
            }
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

    interface ISpanRefMyDataProtector : IMyDataProtector
    {
        void SpanProtect<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct;
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

    class SpanRefProtector(byte[] _bytesToFill) : ISpanRefMyDataProtector
    {
        public byte[] Protect(byte[] plaintext) => throw new NotImplementedException();

        public void SpanProtect<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
        {
            var span = destination.GetSpan(sizeHint: plaintext.Length + _bytesToFill.Length);
            plaintext.CopyTo(span);
            _bytesToFill.CopyTo(span.Slice(plaintext.Length));
            destination.Advance(plaintext.Length + _bytesToFill.Length);
        }
    }
}
