using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DomsUtils.DataStructures.CircularBuffer.Base;

/// <summary>
/// A fixed-size circular buffer optimized for high-performance and low memory usage.
/// Used for storing a collection where items can be added or removed efficiently.
/// </summary>
[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
public sealed class CircularBuffer<T> : IEnumerable<T>
    where T : notnull
{
    /// <summary>
    /// Internal buffer array that stores the elements in the circular buffer.
    /// The length of this array represents the total capacity of the buffer,
    /// and its indices are accessed using bitwise operations for wrap-around behavior.
    /// </summary>
    private readonly T[] _buffer;

    /// <summary>
    /// Holds a bit-mask used for efficient wrap-around operations in the circular buffer.
    /// The mask value is derived from the buffer's capacity, which is always a power of two.
    /// </summary>
    private readonly int  _mask;

    /// <summary>
    /// Represents the index of the first element in the circular buffer (the head).
    /// This value is used to track the position of the oldest element and is updated
    /// when elements are dequeued or overwritten in the buffer.
    /// </summary>
    private int _head, _tail, _count;

    /// <summary>
    /// Gets the number of elements currently stored in the circular buffer.
    /// </summary>
    /// <remarks>
    /// The value of this property is in the range [0, Capacity], where Capacity is the
    /// total size of the buffer. This property is updated dynamically as elements are
    /// added or removed from the buffer.
    /// </remarks>
    /// <value>
    /// The number of elements currently stored in the buffer.
    /// </value>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    /// <summary>
    /// Gets the total capacity of the circular buffer. The capacity is the maximum number of elements
    /// the buffer can hold before needing to overwrite or reject new items.
    /// </summary>
    /// <remarks>
    /// The capacity of this buffer must always be a power of two. This constraint allows for efficient
    /// indexing using a bitwise mask operation, which enhances performance when wrapping around the buffer.
    /// </remarks>
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Length;
    }

    /// <summary>
    /// A high-performance, low-memory-footprint circular buffer implementation.
    /// Optimized for scenarios where capacity is a power of two, enabling fast
    /// bitwise wrapping of indices.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the circular buffer. Must be non-nullable.</typeparam>
    public CircularBuffer(int capacity)
    {
        if (capacity < 1 || (capacity & (capacity - 1)) != 0)
            throw new ArgumentException("Capacity must be a power of two", nameof(capacity));

        _buffer = new T[capacity];
        _mask   = capacity - 1;
    }

    /// <summary>
    /// Attempts to enqueue an item into the circular buffer.
    /// Returns false if the buffer is full.
    /// </summary>
    /// <param name="item">The item to enqueue into the buffer.</param>
    /// <returns>
    /// True if the item was successfully enqueued; false if the buffer is full.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(T item)
    {
        int count = _count;
        int cap   = _mask + 1;
        if (count == cap) return false;

        int tail = _tail;
        _buffer[tail] = item;
        _tail = (tail + 1) & _mask;
        _count = count + 1;
        return true;
    }


    /// <summary>
    /// Adds an element to the buffer, overwriting the oldest element if the buffer is full.
    /// </summary>
    /// <param name="item">The element to be added to the buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnqueueOverwrite(T item)
    {
        int tail = _tail;
        _buffer[tail] = item;
        _tail = (tail + 1) & _mask;

        if (_count == _buffer.Length)
            _head = (_head + 1) & _mask;  // drop oldest
        else
            _count++;
    }

    /// <summary>
    /// Attempts to remove and return the oldest item from the circular buffer.
    /// </summary>
    /// <param name="item">The item removed from the buffer, or the default value of <typeparamref name="T"/> if the operation fails.</param>
    /// <returns><c>true</c> if an item was successfully dequeued; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out T item)
    {
        int cnt = _count;
        if (cnt == 0)
        {
            item = default;
            return false;
        }

        int head = _head;
        item = _buffer[head];
        _head = (head + 1) & _mask;
        _count = cnt - 1;
        return true;
    }

    /// <summary>
    /// Attempts to retrieve the item at the front of the buffer without removing it.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the element at the front of the buffer,
    /// if the operation is successful; otherwise, the default value of <typeparamref name="T"/>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the operation was successful and the buffer is not empty;
    /// otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out T item)
    {
        if (_count == 0)
        {
            item = default;
            return false;
        }
        item = _buffer[_head];
        return true;
    }

    /// <summary>
    /// Resets the circular buffer by clearing all items and setting the head, tail, and count to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(bool zeroMemory = false)
    {
        if (zeroMemory)
            Array.Clear(_buffer, 0, _buffer.Length);
        _head = _tail = _count = 0;
    }

    // Struct‐based, allocation‐free enumerator
    /// <summary>
    /// Returns an enumerator that iterates through the elements of the circular buffer.
    /// </summary>
    /// <returns>An enumerator for the circular buffer.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// A struct-based, allocation-free enumerator for iterating over the elements
    /// of a <see cref="CircularBuffer{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        /// <summary>
        /// Represents the backing array holding the elements of the circular buffer.
        /// Used internally by the enumerator to traverse the buffer without creating additional allocations.
        /// </summary>
        private readonly T[] _buf;

        /// <summary>
        /// A bitmask value used to optimize index computations within the circular buffer.
        /// It ensures indices wrap correctly using bitwise operations, as the buffer's capacity
        /// is always a power of two.
        /// </summary>
        private readonly int  _mask;

        /// <summary>
        /// Index used to track the current position within the buffer during enumeration.
        /// </summary>
        private int _idx, _left;

        /// <summary>
        /// Provides an allocation-free enumerator for the <see cref="CircularBuffer{T}"/>.
        /// </summary>
        internal Enumerator(CircularBuffer<T> buffer)
        {
            _buf   = buffer._buffer;
            _mask  = buffer._mask;
            _idx   = buffer._head;
            _left  = buffer._count;
            Current = default!;
        }

        /// <summary>
        /// Advances the enumerator to the next element in the circular buffer.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator successfully advanced to the next element; otherwise, <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            if (_left <= 0) return false;
            Current = _buf[_idx];
            _idx = (_idx + 1) & _mask;
            _left--;
            return true;
        }

        /// <summary>
        /// Gets the element in the circular buffer at the current enumerator position.
        /// This property reflects the most recently accessed element after a successful
        /// call to <see cref="MoveNext"/>.
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        /// <remarks>
        /// This property returns the current element in the collection being enumerated.
        /// It is intended to be called after <see cref="MoveNext"/> has been called and returned true.
        /// If the enumerator is positioned before the first element or after the last element of the collection,
        /// the value of this property is undefined.
        /// </remarks>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Releases all resources used by the enumerator.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Resets the enumerator to its initial position, which is before the first element
        /// in the collection. This operation is not supported for CircularBuffer enumerators
        /// and will throw a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown as resetting the enumerator is not supported.
        /// </exception>
        public void Reset() => throw new NotSupportedException();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}