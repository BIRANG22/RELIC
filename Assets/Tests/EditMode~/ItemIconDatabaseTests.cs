using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class ItemIconDatabaseTests
{
    [Test]
    public void TryGetIcon_ReturnsConfiguredIngredientIcon()
    {
        ItemIconDatabase database = ScriptableObject.CreateInstance<ItemIconDatabase>();
        Texture2D texture = new(1, 1); Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
        FieldInfo field = typeof(ItemIconDatabase).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(database, new List<ItemIconEntry> { new() { ItemId = "Item_001", Icon = sprite } });

        Assert.That(database.TryGetIcon("Item_001", out Sprite result), Is.True);
        Assert.That(result, Is.SameAs(sprite));

        Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); Object.DestroyImmediate(database);
    }
}
