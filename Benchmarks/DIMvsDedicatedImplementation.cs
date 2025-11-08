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

namespace Benchmarks
{
    /*     
        // * Summary *
        BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.6899)
        AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
        .NET SDK 10.0.100-rc.2.25502.107
          [Host]     : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
          DefaultJob : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

        | Method                                                 | _bytesToFillCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
        |------------------------------------------------------- |------------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
        | FullyManualWithStackalloc                              | 25                |  4.159 ns | 0.0957 ns | 0.0895 ns |  0.43 |    0.01 |      - |         - |        0.00 |
        | FullyManualWithPooling                                 | 25                |  9.008 ns | 0.1983 ns | 0.2715 ns |  0.93 |    0.03 |      - |         - |        0.00 |
        | Default                                                | 25                |  9.724 ns | 0.2137 ns | 0.1999 ns |  1.00 |    0.03 | 0.0031 |      64 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 25                | 30.462 ns | 0.6220 ns | 0.7866 ns |  3.13 |    0.10 | 0.0017 |     136 B |        2.12 |
        | Span_PooledArrayBufferWriter                           | 25                | 14.928 ns | 0.2911 ns | 0.2431 ns |  1.54 |    0.04 | 0.0004 |      32 B |        0.50 |
        | Span_StructPooledArrayBufferWriter                     | 25                |  9.171 ns | 0.0721 ns | 0.0674 ns |  0.94 |    0.02 |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |       |         |        |           |             |
        | FullyManualWithStackalloc                              | 50                |  4.857 ns | 0.1037 ns | 0.1645 ns |  0.40 |    0.02 |      - |         - |        0.00 |
        | FullyManualWithPooling                                 | 50                |  9.222 ns | 0.1716 ns | 0.1521 ns |  0.76 |    0.04 |      - |         - |        0.00 |
        | Default                                                | 50                | 12.100 ns | 0.2629 ns | 0.5660 ns |  1.00 |    0.06 | 0.0011 |      88 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 50                | 33.515 ns | 0.6683 ns | 0.6251 ns |  2.78 |    0.13 | 0.0021 |     160 B |        1.82 |
        | Span_PooledArrayBufferWriter                           | 50                | 16.003 ns | 0.3255 ns | 0.2886 ns |  1.33 |    0.06 | 0.0004 |      32 B |        0.36 |
        | Span_StructPooledArrayBufferWriter                     | 50                |  9.338 ns | 0.1039 ns | 0.0921 ns |  0.77 |    0.04 |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |       |         |        |           |             |
        | FullyManualWithStackalloc                              | 120               |  6.511 ns | 0.0726 ns | 0.0606 ns |  0.35 |    0.01 |      - |         - |        0.00 |
        | FullyManualWithPooling                                 | 120               |  9.329 ns | 0.1948 ns | 0.1626 ns |  0.50 |    0.01 |      - |         - |        0.00 |
        | Default                                                | 120               | 18.769 ns | 0.3851 ns | 0.3602 ns |  1.00 |    0.03 | 0.0021 |     160 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 120               | 43.345 ns | 0.8264 ns | 1.3807 ns |  2.31 |    0.09 | 0.0030 |     232 B |        1.45 |
        | Span_PooledArrayBufferWriter                           | 120               | 16.928 ns | 0.3292 ns | 0.4927 ns |  0.90 |    0.03 | 0.0004 |      32 B |        0.20 |
        | Span_StructPooledArrayBufferWriter                     | 120               | 11.022 ns | 0.2392 ns | 0.2754 ns |  0.59 |    0.02 |      - |         - |        0.00 |

     */

    [MemoryDiagnoser]
    [Config(typeof(Config))]
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
