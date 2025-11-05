using BenchmarkDotNet.Attributes;
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


        | Method                                                       | _bytesToFillCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
        |------------------------------------------------------------- |------------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
        | FullyManualWithPooling                                       | 25                |  8.264 ns | 0.0725 ns | 0.0678 ns |  8.246 ns |  0.85 |    0.03 |      - |         - |        0.00 |
        | Default                                                      | 25                |  9.723 ns | 0.2092 ns | 0.2864 ns |  9.645 ns |  1.00 |    0.04 | 0.0008 |      64 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter       | 25                | 28.454 ns | 0.2663 ns | 0.2491 ns | 28.453 ns |  2.93 |    0.09 | 0.0017 |     136 B |        2.12 |
        | Span_PooledArrayBufferWriter                                 | 25                | 19.735 ns | 0.1750 ns | 0.1551 ns | 19.698 ns |  2.03 |    0.06 | 0.0004 |      32 B |        0.50 |
        | DefaultInterfaceImplementation_StructPooledArrayBufferWriter | 25                | 41.285 ns | 0.2528 ns | 0.2241 ns | 41.259 ns |  4.25 |    0.12 | 0.0030 |     232 B |        3.62 |
        | Span_StructPooledArrayBufferWriter                           | 25                | 25.356 ns | 0.1393 ns | 0.1235 ns | 25.367 ns |  2.61 |    0.08 | 0.0012 |      96 B |        1.50 |
        |                                                              |                   |           |           |           |           |       |         |        |           |             |
        | FullyManualWithPooling                                       | 50                |  8.307 ns | 0.0600 ns | 0.0562 ns |  8.295 ns |  0.72 |    0.01 |      - |         - |        0.00 |
        | Default                                                      | 50                | 11.539 ns | 0.1166 ns | 0.1091 ns | 11.540 ns |  1.00 |    0.01 | 0.0011 |      88 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter       | 50                | 32.660 ns | 0.3762 ns | 0.3519 ns | 32.819 ns |  2.83 |    0.04 | 0.0021 |     160 B |        1.82 |
        | Span_PooledArrayBufferWriter                                 | 50                | 19.897 ns | 0.1647 ns | 0.1541 ns | 19.920 ns |  1.72 |    0.02 | 0.0004 |      32 B |        0.36 |
        | DefaultInterfaceImplementation_StructPooledArrayBufferWriter | 50                | 42.274 ns | 0.5359 ns | 0.5013 ns | 42.213 ns |  3.66 |    0.05 | 0.0033 |     256 B |        2.91 |
        | Span_StructPooledArrayBufferWriter                           | 50                | 25.879 ns | 0.5382 ns | 1.3203 ns | 25.069 ns |  2.24 |    0.12 | 0.0013 |      96 B |        1.09 |
        |                                                              |                   |           |           |           |           |       |         |        |           |             |
        | FullyManualWithPooling                                       | 120               | 10.618 ns | 0.0431 ns | 0.0382 ns | 10.610 ns |  0.63 |    0.01 |      - |         - |        0.00 |
        | Default                                                      | 120               | 16.847 ns | 0.1960 ns | 0.1738 ns | 16.853 ns |  1.00 |    0.01 | 0.0021 |     160 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter       | 120               | 39.425 ns | 0.7963 ns | 1.4561 ns | 39.438 ns |  2.34 |    0.09 | 0.0030 |     232 B |        1.45 |
        | Span_PooledArrayBufferWriter                                 | 120               | 21.815 ns | 0.4096 ns | 0.3198 ns | 21.723 ns |  1.29 |    0.02 | 0.0004 |      32 B |        0.20 |
        | DefaultInterfaceImplementation_StructPooledArrayBufferWriter | 120               | 49.387 ns | 0.9224 ns | 1.3806 ns | 49.273 ns |  2.93 |    0.09 | 0.0042 |     328 B |        2.05 |
        | Span_StructPooledArrayBufferWriter                           | 120               | 25.923 ns | 0.1797 ns | 0.1501 ns | 25.918 ns |  1.54 |    0.02 | 0.0012 |      96 B |        0.60 |
     */

    [MemoryDiagnoser]
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

        // default interface invocation with StructPooledArrayBufferWriter
        [Benchmark]
        public int DefaultInterfaceImplementation_StructPooledArrayBufferWriter()
        {
            using var arrayBufferWriter = new StructArrayBufferWriter(initialCapacity: _plainText.Length + _bytesToFillCount);
            _simpleProtector.Protect(_plainText, arrayBufferWriter);
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
