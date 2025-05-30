using System;
using DomsUtils.DataStructures.CircularBuffer.Base;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DomsUtils.Tests.DataStructures.CircularBuffer.Base;

[TestClass]
[TestSubject(typeof(CircularBuffer<>))]
public class CircularBufferTest
{
    [TestMethod]
    public void Constructor_InvalidCapacity_ThrowsException()
    {
        Assert.ThrowsException<ArgumentException>(() => new CircularBuffer<int>(0));
        Assert.ThrowsException<ArgumentException>(() => new CircularBuffer<int>(3)); // Not a power of two
    }

    [TestMethod]
    public void Constructor_ValidCapacity_CreatesBuffer()
    {
        var buffer = new CircularBuffer<int>(8);
        Assert.AreEqual(8, buffer.Capacity);
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void TryEnqueue_ToEmptyBuffer_ReturnsTrue()
    {
        var buffer = new CircularBuffer<int>(4);
        bool result = buffer.TryEnqueue(1);

        Assert.IsTrue(result);
        Assert.AreEqual(1, buffer.Count);
    }

    [TestMethod]
    public void TryEnqueue_ToFullBuffer_ReturnsFalse()
    {
        var buffer = new CircularBuffer<int>(2);
        buffer.TryEnqueue(1);
        buffer.TryEnqueue(2);

        bool result = buffer.TryEnqueue(3);

        Assert.IsFalse(result);
        Assert.AreEqual(2, buffer.Count);
    }

    [TestMethod]
    public void EnqueueOverwrite_ToFullBuffer_OverwritesOldest()
    {
        var buffer = new CircularBuffer<int>(2);
        buffer.EnqueueOverwrite(1);
        buffer.EnqueueOverwrite(2);

        Assert.AreEqual(2, buffer.Count);

        buffer.EnqueueOverwrite(3);

        Assert.AreEqual(2, buffer.Count);
        Assert.IsTrue(buffer.TryDequeue(out int oldest));
        Assert.AreEqual(2, oldest);
    }

    [TestMethod]
    public void TryDequeue_FromEmptyBuffer_ReturnsFalse()
    {
        var buffer = new CircularBuffer<int>(4);

        bool result = buffer.TryDequeue(out int item);

        Assert.IsFalse(result);
        Assert.AreEqual(default, item);
    }

    [TestMethod]
    public void TryDequeue_FromNonEmptyBuffer_ReturnsTrueAndDequeuesItem()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.TryEnqueue(1);

        bool result = buffer.TryDequeue(out int item);

        Assert.IsTrue(result);
        Assert.AreEqual(1, item);
        Assert.AreEqual(0, buffer.Count);
    }

    [TestMethod]
    public void TryPeek_FromEmptyBuffer_ReturnsFalse()
    {
        var buffer = new CircularBuffer<int>(4);

        bool result = buffer.TryPeek(out int item);

        Assert.IsFalse(result);
        Assert.AreEqual(default, item);
    }

    [TestMethod]
    public void TryPeek_FromNonEmptyBuffer_ReturnsTrueAndPeeksItem()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.TryEnqueue(1);

        bool result = buffer.TryPeek(out int item);

        Assert.IsTrue(result);
        Assert.AreEqual(1, item);
        Assert.AreEqual(1, buffer.Count); // Ensure the item is not removed
    }

    [TestMethod]
    public void Clear_WithZeroMemory_EmptiesBufferAndClearsData()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.TryEnqueue(1);
        buffer.TryEnqueue(2);

        buffer.Clear(true);

        Assert.AreEqual(0, buffer.Count);
        Assert.IsTrue(buffer.TryEnqueue(1));
    }

    [TestMethod]
    public void Clear_WithoutZeroMemory_EmptiesBuffer()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.TryEnqueue(1);
        buffer.TryEnqueue(2);

        buffer.Clear(false);

        Assert.AreEqual(0, buffer.Count);
        Assert.IsTrue(buffer.TryEnqueue(1));
    }

    [TestMethod]
    public void Enumerator_ReturnsAllItemsInCorrectOrder()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.TryEnqueue(1);
        buffer.TryEnqueue(2);
        buffer.TryEnqueue(3);

        var items = buffer.ToList();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, items);
    }

    [TestMethod]
    public void Enumerator_WithWrapAround_ReturnsCorrectItemsInOrder()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.EnqueueOverwrite(1);
        buffer.EnqueueOverwrite(2);
        buffer.EnqueueOverwrite(3);
        buffer.EnqueueOverwrite(4);
        buffer.EnqueueOverwrite(5);

        var items = buffer.ToList();

        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, items);
    }
}