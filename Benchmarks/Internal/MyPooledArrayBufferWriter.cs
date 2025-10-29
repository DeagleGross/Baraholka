using System;
using System.Buffers;
using System.Reflection;

namespace Benchmarks.Internal
{
    internal sealed class MyPooledArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private T[] _rentedBuffer;
        private int _index;

        public MyPooledArrayBufferWriter(int initialCapacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);

            _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
            _index = 0;
        }

        public int WrittenCount
        {
            get
            {
                CheckIfDisposed();
                return _index;
            }
        }

        public void Advance(int count)
        {
            CheckIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (_index > _rentedBuffer.Length - count)
            {
                throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {_rentedBuffer.Length}.");
            }
            _index += count;
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            CheckIfDisposed();
            return _rentedBuffer.AsMemory(_index);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckIfDisposed();
            return _rentedBuffer.AsSpan(_index);
        }

        public void Dispose()
        {
            if (_rentedBuffer == null)
            {
                return;
            }

            ArrayPool<T>.Shared.Return(_rentedBuffer);
            _rentedBuffer = null!;
        }

        private void CheckIfDisposed()
        {
            if (_rentedBuffer == null)
            {
                throw new ObjectDisposedException(nameof(ArrayBufferWriter<T>));
            }
        }
    }
}
