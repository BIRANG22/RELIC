using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleDebugKillAllPlayersTests
{
    private GameObject characterObject;
    private GameObject monsterObject;

    [TearDown]
    public void TearDown()
    {
        if (characterObject != null)
            Object.DestroyImmediate(characterObject);

        if (monsterObject != null)
            Object.DestroyImmediate(monsterObject);
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

    [Test]
    public void AreAllMonstersDead_AwakenedArabellaIsAlive_ReturnsFalse()
    {
        monsterObject = new GameObject("AwakenedArabella");
        var monster = monsterObject.AddComponent<Relic.Gameplay.Monster.MonsterUnit>();
        monster.Initialize(new MonsterRuntimeData(
            "runtime-arabella",
            new MonsterMasterData
            {
                MonsterId = "Mon_12_2",
                Phase = 2,
                Name = "Awakened Arabella",
                HP = 80
            }));

        MethodInfo method = typeof(BattleDebugKillAllMonsters).GetMethod(
            "AreAllMonstersDead",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        Assert.That(method.Invoke(null, null), Is.False);
    }
}
