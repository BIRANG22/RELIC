using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GridEffectSpriteDatabaseTests
{
    [Test]
    public void TryGetPrefab_ReturnsPrefabByGridEffectId()
    {
        GridEffectSpriteDatabase database = ScriptableObject.CreateInstance<GridEffectSpriteDatabase>();
        GameObject prefab = new("GridEffectPrefab");

        try
        {
            SetEntries(database, new List<GridEffectSpriteDatabase.Entry>
            {
                new GridEffectSpriteDatabase.Entry
                {
                    gridEffectId = "GR_thorn",
                    prefab = prefab
                }
            });

            bool found = database.TryGetPrefab("GR_thorn", out GameObject loadedPrefab);

            Assert.That(found, Is.True);
            Assert.That(loadedPrefab, Is.SameAs(prefab));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void TryGetPrefab_TrimsGridEffectId()
    {
        GridEffectSpriteDatabase database = ScriptableObject.CreateInstance<GridEffectSpriteDatabase>();
        GameObject prefab = new("GridEffectPrefab");

        try
        {
            SetEntries(database, new List<GridEffectSpriteDatabase.Entry>
            {
                new GridEffectSpriteDatabase.Entry
                {
                    gridEffectId = "GR_helmet",
                    prefab = prefab
                }
            });

            bool found = database.TryGetPrefab(" GR_helmet ", out GameObject loadedPrefab);

            Assert.That(found, Is.True);
            Assert.That(loadedPrefab, Is.SameAs(prefab));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
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
