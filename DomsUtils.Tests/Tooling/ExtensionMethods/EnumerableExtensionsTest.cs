using DomsUtils.Tooling.ExtensionMethods;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace DomsUtils.Tests.Tooling.ExtensionMethods;

[TestClass]
[TestSubject(typeof(EnumerableExtensions))]
public class EnumerableExtensionsTest
{
    [TestMethod]
    public void IsNullOrEmpty_ShouldReturnTrue_ForNullEnumerable()
    {
        IEnumerable<int>? source = null;
        Assert.IsTrue(source.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_ShouldReturnTrue_ForEmptyEnumerable()
    {
        IEnumerable<int> source = Array.Empty<int>();
        Assert.IsTrue(source.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_ShouldReturnFalse_ForNonEmptyEnumerable()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 3 };
        Assert.IsFalse(source.IsNullOrEmpty());
    }

    [TestMethod]
    public void HasItems_ShouldReturnFalse_ForNullEnumerable()
    {
        IEnumerable<int>? source = null;
        Assert.IsFalse(source.HasItems());
    }

    [TestMethod]
    public void HasItems_ShouldReturnFalse_ForEmptyEnumerable()
    {
        IEnumerable<int> source = Array.Empty<int>();
        Assert.IsFalse(source.HasItems());
    }

    [TestMethod]
    public void HasItems_ShouldReturnTrue_ForNonEmptyEnumerable()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 3 };
        Assert.IsTrue(source.HasItems());
    }

    [TestMethod]
    public void ForEach_ShouldExecuteAction_ForEachElement()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 3 };
        List<int> result = new();
        source.ForEach(i => result.Add(i));
        CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ForEach_ShouldThrowArgumentNullException_ForNullSource()
    {
        IEnumerable<int> source = null!;
        source.ForEach(i => { });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ForEach_ShouldThrowArgumentNullException_ForNullAction()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 3 };
        source.ForEach(null!);
    }

    [TestMethod]
    public void WhereNotNull_ShouldReturnNonNullElements_ForNullableReferenceTypes()
    {
        IEnumerable<string?> source = new List<string?> { null, "a", null, "b" };
        IEnumerable<string> result = source.WhereNotNull();
        CollectionAssert.AreEqual(new List<string> { "a", "b" }, result.ToList());
    }

    [TestMethod]
    public void WhereNotNull_ShouldReturnNonNullElements_ForNullableValueTypes()
    {
        IEnumerable<int?> source = new List<int?> { null, 1, null, 2 };
        IEnumerable<int> result = source.WhereNotNull();
        CollectionAssert.AreEqual(new List<int> { 1, 2 }, result.ToList());
    }

    [TestMethod]
    public void FirstOrDefaultSafe_ShouldReturnDefault_ForNullSource()
    {
        IEnumerable<int>? source = null;
        Assert.AreEqual(default, source.FirstOrDefaultSafe());
    }

    [TestMethod]
    public void FirstOrDefaultSafe_ShouldReturnFirstElement_ForNonEmptyEnumerable()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 3 };
        Assert.AreEqual(1, source.FirstOrDefaultSafe());
    }

    [TestMethod]
    public void FirstOrDefaultSafe_ShouldReturnDefault_ForEmptyEnumerable()
    {
        IEnumerable<int> source = new List<int>();
        Assert.AreEqual(default, source.FirstOrDefaultSafe());
    }

    [TestMethod]
    public void LastOrDefaultSafe_ShouldReturnDefault_ForNullSource()
    {
        IEnumerable<int>? source = null;
        Assert.AreEqual(default, source.LastOrDefaultSafe());
    }

    [TestMethod]
    public void LastOrDefaultSafe_ShouldReturnLastElement_ForNonEmptyEnumerable()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 3 };
        Assert.AreEqual(3, source.LastOrDefaultSafe());
    }

    [TestMethod]
    public void LastOrDefaultSafe_ShouldReturnDefault_ForEmptyEnumerable()
    {
        IEnumerable<int> source = new List<int>();
        Assert.AreEqual(default, source.LastOrDefaultSafe());
    }

    [TestMethod]
    public void ToHashSetSafe_ShouldReturnEmptyHashSet_ForNullSource()
    {
        IEnumerable<int>? source = null;
        HashSet<int> result = source.ToHashSetSafe();
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ToHashSetSafe_ShouldReturnHashSet_WithElementsOfSource()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 2, 3 };
        HashSet<int> result = source.ToHashSetSafe();
        CollectionAssert.AreEquivalent(new List<int> { 1, 2, 3 }, result.ToList());
    }

    [TestMethod]
    public void DistinctSafe_ShouldReturnEmptyEnumerable_ForNullSource()
    {
        IEnumerable<int>? source = null;
        IEnumerable<int> result = source.DistinctSafe();
        Assert.IsFalse(result.HasItems());
    }

    [TestMethod]
    public void DistinctSafe_ShouldReturnDistinctElements_ForSourceWithDuplicates()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 2, 3 };
        IEnumerable<int> result = source.DistinctSafe();
        CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, result.ToList());
    }
}