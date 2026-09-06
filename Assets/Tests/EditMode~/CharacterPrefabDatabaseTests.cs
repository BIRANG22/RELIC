using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class CharacterPrefabDatabaseTests
{
    [Test]
    public void TryGetRestPrefab_ReturnsConfiguredRestPrefab()
    {
        CharacterPrefabDatabase database = ScriptableObject.CreateInstance<CharacterPrefabDatabase>();
        GameObject restPrefab = new("RestPrefab");

        try
        {
            SetEntries(database, new List<CharacterPrefabEntry>
            {
                new()
                {
                    CharacterId = "Char_01",
                    RestPrefab = restPrefab
                }
            });

            bool found = database.TryGetRestPrefab("Char_01", out GameObject loadedPrefab);

            Assert.That(found, Is.True);
            Assert.That(loadedPrefab, Is.SameAs(restPrefab));
        }
        finally
        {
            Object.DestroyImmediate(restPrefab);
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void TryGetRestPrefab_DoesNotFallBackToBattleEventPrefab()
    {
        CharacterPrefabDatabase database = ScriptableObject.CreateInstance<CharacterPrefabDatabase>();
        GameObject battleEventPrefab = new("BattleEventPrefab");

        try
        {
            SetEntries(database, new List<CharacterPrefabEntry>
            {
                new()
                {
                    CharacterId = "Char_04",
                    BattleEventWorldPrefab = battleEventPrefab
                }
            });

            bool found = database.TryGetRestPrefab("Char_04", out GameObject loadedPrefab);

            Assert.That(found, Is.False);
            Assert.That(loadedPrefab, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(battleEventPrefab);
            Object.DestroyImmediate(database);
        }
    }

    private static void SetEntries(
        CharacterPrefabDatabase database,
        List<CharacterPrefabEntry> entries)
    {
        FieldInfo field = typeof(CharacterPrefabDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        field.SetValue(database, entries);
    }
}
