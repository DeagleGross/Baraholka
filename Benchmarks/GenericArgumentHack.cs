using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Benchmarks
{
    /*
        BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.6899)
        AMD Ryzen 9 7950X3D, 1 CPU, 32 logical and 16 physical cores
        .NET SDK 10.0.100-rc.2.25502.107
          [Host]     : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
          DefaultJob : .NET 10.0.0 (10.0.25.50307), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


        | Method                     | Mean      | Error     | StdDev    | Gen0   | Allocated |
        |--------------------------- |----------:|----------:|----------:|-------:|----------:|
        | Basic                      |  7.682 ns | 0.0845 ns | 0.0791 ns | 0.0008 |      64 B |
        | Generic                    | 12.485 ns | 0.0782 ns | 0.0732 ns | 0.0008 |      64 B |
        | GenericStruct              |  7.454 ns | 0.0498 ns | 0.0441 ns | 0.0004 |      32 B |
        | GenericStackAllocWriter    |  5.816 ns | 0.0323 ns | 0.0286 ns |      - |         - |
        | BasicWithArrayBufferWriter | 12.775 ns | 0.2759 ns | 0.7647 ns | 0.0015 |     120 B |
     */

    [MemoryDiagnoser]
    public class GenericArgumentHack
    {
        private byte[] _plaintext;

        IMyWriter _basicWriter;
        IMyGenericWriter _genericWriter;

        [GlobalSetup]
        public void Setup()
        {
            _plaintext = new byte[5];
            RandomNumberGenerator.Fill(_plaintext);

            _basicWriter = new MyBasicWriter();
            _genericWriter = new MyGenericWriter();
        }

        [Benchmark]
        public int Basic()
        {
            var buffer = new ClassWriter(initialCapacity: 5);
            _basicWriter.Protect(_plaintext, buffer);
            return buffer.Written;
        }

        [Benchmark]
        public int Generic()
        {
            var buffer = new ClassWriter(initialCapacity: 5);
            _genericWriter.Protect(_plaintext, buffer);
            return buffer.Written;
        }

        [Benchmark]
        public int GenericStruct()
        {
            var buffer = new StructWriter(initialCapacity: 5);
            _genericWriter.Protect(_plaintext, buffer);
            return buffer.Written;
        }

        // NEW: True zero-allocation benchmark
        [Benchmark]
        public int GenericStackAllocWriter()
        {
            Span<byte> stackBuffer = stackalloc byte[64];
            var buffer = new StackAllocWriter(stackBuffer);
            _genericWriter.Protect(_plaintext, buffer);
            return buffer.Written;
        }

        // For comparison - can't use stackalloc with non-generic interface
        [Benchmark]
        public int BasicWithArrayBufferWriter()
        {
            var buffer = new ArrayBufferWriter<byte>(initialCapacity: 64);
            _basicWriter.Protect(_plaintext, buffer);
            return buffer.WrittenCount;
        }
    }

    interface IMyGenericWriter
    {
        void Protect<TWriter>(ReadOnlySpan<byte> plaintext, TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct;
    }

    interface IMyWriter
    {
        void Protect(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination);
    }

    class MyBasicWriter : IMyWriter
    {
        public void Protect(ReadOnlySpan<byte> plaintext, IBufferWriter<byte> destination)
        {
            destination.Write(plaintext);
        }
    }

    class MyGenericWriter : IMyGenericWriter
    {
        public void Protect<TWriter>(ReadOnlySpan<byte> plaintext, TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
        {
            var span = destination.GetSpan(plaintext.Length);
            plaintext.CopyTo(span);
            destination.Advance(plaintext.Length);
        }
    }

    class ClassWriter : IBufferWriter<byte>
    {
        byte[] _data;
        int _index = 0;

        public int Written => _index;

        public ClassWriter(int initialCapacity)
        {
            _data = new byte[initialCapacity];
        }

        public void Advance(int count) => _index += count;
        public Memory<byte> GetMemory(int sizeHint = 0) => _data.AsMemory(_index);
        public Span<byte> GetSpan(int sizeHint = 0) => _data.AsSpan(_index);
    }

    struct StructWriter : IBufferWriter<byte>
    {
        byte[] _data;
        int _index = 0;

        public int Written => _index;

        public StructWriter(int initialCapacity)
        {
            _data = new byte[initialCapacity];
        }

        public void Advance(int count) => _index += count;
        public Memory<byte> GetMemory(int sizeHint = 0) => _data.AsMemory(_index);
        public Span<byte> GetSpan(int sizeHint = 0) => _data.AsSpan(_index);
    }

    ref struct StackAllocWriter : IBufferWriter<byte>
    {
        private Span<byte> _buffer;
        private int _index;

        public StackAllocWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _index = 0;
        }

        public int Written => _index;

        public void Advance(int count) => _index += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            throw new NotSupportedException("StackAllocWriter doesn't support Memory<byte> - use GetSpan instead");
        }

        public Span<byte> GetSpan(int sizeHint = 0) => _buffer.Slice(_index);
    }
}