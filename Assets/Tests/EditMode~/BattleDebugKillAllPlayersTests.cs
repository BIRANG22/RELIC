using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleDebugKillAllPlayersTests
{
    private GameObject characterObject;

    [TearDown]
    public void TearDown()
    {
        if (characterObject != null)
            Object.DestroyImmediate(characterObject);
    }

    [Test]
    public void KillPlayersForDebug_SetsHpAndShieldToZero()
    {
        characterObject = new GameObject("DebugCharacter");
        BattleCharacter character = characterObject.AddComponent<BattleCharacter>();
        character.Initialize(new CharacterRuntimeData
        {
            CharacterId = "C_Debug",
            CurrentHP = 12,
            CurrentShield = 5
        });

        MethodInfo killMethod = typeof(BattleDebugKillAllMonsters).GetMethod(
            "KillPlayersForDebug",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(killMethod, Is.Not.Null);
        killMethod.Invoke(null, new object[] { new[] { character } });

        Assert.That(character.RuntimeData.CurrentHP, Is.Zero);
        Assert.That(character.RuntimeData.CurrentShield, Is.Zero);
        Assert.That(character.RuntimeData.IsDead, Is.True);
    }
}
