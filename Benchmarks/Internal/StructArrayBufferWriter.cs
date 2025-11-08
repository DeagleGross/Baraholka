using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Benchmarks.Internal
{
    /// <summary>
    /// A high-performance struct-based IBufferWriter&lt;byte&gt; implementation that uses ArrayPool for allocations.
    /// Designed for zero-allocation scenarios when used with generic methods via `allows ref struct` constraint.
    /// </summary>
    public ref struct StructArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private T[] _rentedBuffer;
        private Span<T> _initialBuffer;
        private int _index;

        private const int MinimumBufferSize = 256;

        public StructArrayBufferWriter(Span<T> initialBuffer)
        {
            _index = 0;
            _initialBuffer = initialBuffer;
            _rentedBuffer = null!;
        }

        public ReadOnlySpan<T> WrittenSpan
        {
            get
            {
                CheckIfDisposed();
                return _initialBuffer.Slice(0, _index);
            }
        }

        public int WrittenCount
        {
            get
            {
                CheckIfDisposed();
                return _index;
            }
        }

        public int Capacity
        {
            get
            {
                CheckIfDisposed();
                return _initialBuffer.Length;
            }
        }

        public int FreeCapacity
        {
            get
            {
                CheckIfDisposed();
                return _initialBuffer.Length - _index;
            }
        }

        public void Clear()
        {
            CheckIfDisposed();
            ClearHelper();
        }

        private void ClearHelper()
        {
            // _initialBuffer.Clear();
            // _index = 0;
        }

        // Returns the rented buffer back to the pool
        public void Dispose()
        {
            ClearHelper();
            //ArrayPool<T>.Shared.Return(_initialBuffer);
            //_initialBuffer = null!;
        }

        private void CheckIfDisposed()
        {
            //if (_initialBuffer == null)
            //{
            //    ThrowObjectDisposedException();
            //}
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(nameof(ArrayBufferWriter<T>));
        }

        public void Advance(int count)
        {
            CheckIfDisposed();

            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (_index > _initialBuffer.Length - count)
            {
                ThrowInvalidOperationException(_initialBuffer.Length);
            }

            _index += count;
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckIfDisposed();

            CheckAndResizeBuffer(sizeHint);
            return _initialBuffer.Slice(_index);
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            throw new NotImplementedException();
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            Debug.Assert(_initialBuffer != null);

            ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

            if (sizeHint == 0)
            {
                sizeHint = MinimumBufferSize;
            }

            var availableSpace = _initialBuffer.Length - _index;

            //if (sizeHint > availableSpace)
            //{
            //    var growBy = Math.Max(sizeHint, _initialBuffer.Length);

            //    var newSize = checked(_initialBuffer.Length + growBy);

            //    var oldBuffer = _initialBuffer;

            //    _initialBuffer = ArrayPool<T>.Shared.Rent(newSize);

            //    Debug.Assert(oldBuffer.Length >= _index);
            //    Debug.Assert(_initialBuffer.Length >= _index);

            //    var previousBuffer = oldBuffer.AsSpan(0, _index);
            //    previousBuffer.CopyTo(_initialBuffer);
            //    previousBuffer.Clear();
            //    ArrayPool<T>.Shared.Return(oldBuffer);
            //}

            Debug.Assert(_initialBuffer.Length - _index > 0);
            Debug.Assert(_initialBuffer.Length - _index >= sizeHint);
        }

        private static void ThrowInvalidOperationException(int capacity)
        {
            throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {capacity}.");
        }
    }
}