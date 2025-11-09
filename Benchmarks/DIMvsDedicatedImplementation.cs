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

        | Method                                                 | _bytesToFillCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
        |------------------------------------------------------- |------------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|------------:|
        | FullyManualWithStackalloc                              | 25                |  3.976 ns | 0.0686 ns | 0.0608 ns |  3.965 ns |  0.43 |    0.02 |      - |      - |      - |         - |        0.00 |
        | FullyManualWithPooling                                 | 25                |  8.327 ns | 0.1823 ns | 0.1522 ns |  8.334 ns |  0.89 |    0.03 |      - |      - |      - |         - |        0.00 |
        | Default                                                | 25                |  9.365 ns | 0.2024 ns | 0.3325 ns |  9.283 ns |  1.00 |    0.05 | 0.0008 |      - |      - |      64 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 25                | 27.528 ns | 0.5651 ns | 0.7922 ns | 27.264 ns |  2.94 |    0.13 | 0.0018 |      - |      - |     136 B |        2.12 |
        | Span_PooledArrayBufferWriter                           | 25                | 13.389 ns | 0.2149 ns | 0.1794 ns | 13.377 ns |  1.43 |    0.05 | 0.0004 | 0.0000 | 0.0000 |         - |        0.00 |
        | SpanCallingGeneric_PooledArrayBufferWriter             | 25                | 20.196 ns | 0.4176 ns | 0.6121 ns | 20.202 ns |  2.16 |    0.10 | 0.0004 |      - |      - |      32 B |        0.50 |
        | Span_StructPooledArrayBufferWriter                     | 25                |  8.281 ns | 0.1821 ns | 0.3091 ns |  8.142 ns |  0.89 |    0.04 |      - |      - |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |           |       |         |        |        |        |           |             |
        | FullyManualWithStackalloc                              | 80                |  6.523 ns | 0.1458 ns | 0.1497 ns |  6.509 ns |  0.54 |    0.03 |      - |      - |      - |         - |        0.00 |
        | FullyManualWithPooling                                 | 80                |  8.774 ns | 0.1957 ns | 0.5190 ns |  8.643 ns |  0.73 |    0.05 |      - |      - |      - |         - |        0.00 |
        | Default                                                | 80                | 12.103 ns | 0.2625 ns | 0.5594 ns | 12.025 ns |  1.00 |    0.06 | 0.0015 |      - |      - |     120 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 80                | 31.685 ns | 0.6420 ns | 1.0366 ns | 31.530 ns |  2.62 |    0.14 | 0.0024 |      - |      - |     192 B |        1.60 |
        | Span_PooledArrayBufferWriter                           | 80                | 14.531 ns | 0.3087 ns | 0.2888 ns | 14.449 ns |  1.20 |    0.06 | 0.0004 |      - |      - |      32 B |        0.27 |
        | SpanCallingGeneric_PooledArrayBufferWriter             | 80                | 20.784 ns | 0.4354 ns | 0.8492 ns | 20.542 ns |  1.72 |    0.10 | 0.0004 |      - |      - |      32 B |        0.27 |
        | Span_StructPooledArrayBufferWriter                     | 80                | 12.061 ns | 0.2540 ns | 0.2252 ns | 12.019 ns |  1.00 |    0.05 |      - |      - |      - |         - |        0.00 |
        |                                                        |                   |           |           |           |           |       |         |        |        |        |           |             |
        | FullyManualWithStackalloc                              | 120               |  5.600 ns | 0.1029 ns | 0.0912 ns |  5.593 ns |  0.39 |    0.02 |      - |      - |      - |         - |        0.00 |
        | FullyManualWithPooling                                 | 120               |  8.279 ns | 0.1584 ns | 0.1404 ns |  8.206 ns |  0.58 |    0.02 |      - |      - |      - |         - |        0.00 |
        | Default                                                | 120               | 14.289 ns | 0.3062 ns | 0.5599 ns | 14.118 ns |  1.00 |    0.05 | 0.0021 |      - |      - |     160 B |        1.00 |
        | DefaultInterfaceImplementation_PooledArrayBufferWriter | 120               | 33.767 ns | 0.6961 ns | 0.9759 ns | 33.638 ns |  2.37 |    0.11 | 0.0030 |      - |      - |     232 B |        1.45 |
        | Span_PooledArrayBufferWriter                           | 120               | 15.020 ns | 0.3026 ns | 0.3363 ns | 14.972 ns |  1.05 |    0.05 | 0.0004 |      - |      - |      32 B |        0.20 |
        | SpanCallingGeneric_PooledArrayBufferWriter             | 120               | 22.212 ns | 0.4434 ns | 0.5766 ns | 22.104 ns |  1.56 |    0.07 | 0.0004 |      - |      - |      32 B |        0.20 |
        | Span_StructPooledArrayBufferWriter                     | 120               |  9.752 ns | 0.1902 ns | 0.1779 ns |  9.779 ns |  0.68 |    0.03 |      - |      - |      - |         - |        0.00 |

     */

    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class DIMvsDedicatedImplementation
    {
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
