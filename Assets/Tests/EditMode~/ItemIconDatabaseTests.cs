using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class ItemIconDatabaseTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void TryGetResearchResultIcon_UsesSeparateIconWithoutChangingDefaultIcon()
    {
        ItemIconDatabase database = ScriptableObject.CreateInstance<ItemIconDatabase>();
        createdObjects.Add(database);

        Sprite defaultIcon = CreateSprite("DefaultItemIcon");
        Sprite researchResultIcon = CreateSprite("ResearchResultItemIcon");

        SetEntries(database, new List<ItemIconEntry>
        {
            new()
            {
                ItemId = "Item_001",
                Icon = defaultIcon,
                ResearchResultIcon = researchResultIcon
            }
        });

        Assert.That(database.TryGetIcon("Item_001", out Sprite resolvedDefaultIcon), Is.True);
        Assert.That(resolvedDefaultIcon, Is.SameAs(defaultIcon));

        Assert.That(database.TryGetResearchResultIcon("Item_001", out Sprite resolvedResearchResultIcon), Is.True);
        Assert.That(resolvedResearchResultIcon, Is.SameAs(researchResultIcon));
    }

    [Test]
    public void TryGetResearchResultIcon_ReturnsFalseWhenItemIdIsMissing()
    {
        ItemIconDatabase database = ScriptableObject.CreateInstance<ItemIconDatabase>();
        createdObjects.Add(database);

        SetEntries(database, new List<ItemIconEntry>());

        Assert.That(database.TryGetResearchResultIcon("Missing_Item", out Sprite icon), Is.False);
        Assert.That(icon, Is.Null);
    }

    private Sprite CreateSprite(string objectName)
    {
        Texture2D texture = new(1, 1)
        {
            name = objectName + "_Texture"
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        createdObjects.Add(texture);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f));
        sprite.name = objectName;
        createdObjects.Add(sprite);
        return sprite;
    }

    private static void SetEntries(ItemIconDatabase database, List<ItemIconEntry> entries)
    {
        FieldInfo entriesField = typeof(ItemIconDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(entriesField, Is.Not.Null);
        entriesField.SetValue(database, entries);
    }
}
