using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class ReusableComponentPoolTests
{
    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void Prepare_ReusesInactiveItemsWithoutGrowingHierarchy()
    {
        var pool = new ReusableComponentPool<Transform>();
        int createCount = 0;

        Transform CreateItem()
        {
            createCount++;
            var gameObject = new GameObject($"Item_{createCount}");
            createdObjects.Add(gameObject);
            return gameObject.transform;
        }

        bool firstGrowth = pool.Prepare(3, CreateItem);
        pool.DeactivateUnused(1);
        bool secondGrowth = pool.Prepare(3, CreateItem);

        Assert.That(firstGrowth, Is.True);
        Assert.That(secondGrowth, Is.False);
        Assert.That(createCount, Is.EqualTo(3));
        Assert.That(pool[0].gameObject.activeSelf, Is.True);
        Assert.That(pool[1].gameObject.activeSelf, Is.True);
        Assert.That(pool[2].gameObject.activeSelf, Is.True);
    }

    [Test]
    public void DeactivateUnused_HidesOnlyItemsOutsideUsedRange()
    {
        var pool = new ReusableComponentPool<Transform>();

        Transform CreateItem()
        {
            var gameObject = new GameObject("Item");
            createdObjects.Add(gameObject);
            return gameObject.transform;
        }

        pool.Prepare(3, CreateItem);
        pool.DeactivateUnused(1);

        Assert.That(pool[0].gameObject.activeSelf, Is.True);
        Assert.That(pool[1].gameObject.activeSelf, Is.False);
        Assert.That(pool[2].gameObject.activeSelf, Is.False);
    }
}
