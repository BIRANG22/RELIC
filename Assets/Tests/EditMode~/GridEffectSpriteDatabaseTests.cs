using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GridEffectSpriteDatabaseTests
{
    [Test]
    public void TryGetSprite_ReturnsSpriteByGridEffectId()
    {
        GridEffectSpriteDatabase database = ScriptableObject.CreateInstance<GridEffectSpriteDatabase>();
        Texture2D texture = new(1, 1);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

        try
        {
            SetEntries(database, new List<GridEffectSpriteDatabase.Entry>
            {
                new GridEffectSpriteDatabase.Entry
                {
                    gridEffectId = "GR_thorn",
                    sprite = sprite
                }
            });

            bool found = database.TryGetSprite("GR_thorn", out Sprite loadedSprite);

            Assert.That(found, Is.True);
            Assert.That(loadedSprite, Is.SameAs(sprite));
        }
        finally
        {
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void TryGetSprite_TrimsGridEffectId()
    {
        GridEffectSpriteDatabase database = ScriptableObject.CreateInstance<GridEffectSpriteDatabase>();
        Texture2D texture = new(1, 1);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

        try
        {
            SetEntries(database, new List<GridEffectSpriteDatabase.Entry>
            {
                new GridEffectSpriteDatabase.Entry
                {
                    gridEffectId = "GR_helmet",
                    sprite = sprite
                }
            });

            bool found = database.TryGetSprite(" GR_helmet ", out Sprite loadedSprite);

            Assert.That(found, Is.True);
            Assert.That(loadedSprite, Is.SameAs(sprite));
        }
        finally
        {
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(database);
        }
    }

    private static void SetEntries(
        GridEffectSpriteDatabase database,
        List<GridEffectSpriteDatabase.Entry> entries)
    {
        FieldInfo field = typeof(GridEffectSpriteDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        field.SetValue(database, entries);
    }
}
