using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class FatigueEffectTargetingTests
{
    private GameObject playerObject;
    private GameObject monsterObject;
    private BattleCharacter player;
    private MonsterUnit monster;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("FatigueCaster");
        player = playerObject.AddComponent<BattleCharacter>();
        player.Initialize(new CharacterRuntimeData
        {
            CharacterId = "Char_03",
            MaxHP = 10,
            CurrentHP = 10
        });

        monsterObject = new GameObject("FatigueTarget");
        monster = monsterObject.AddComponent<MonsterUnit>();
        MonsterRuntimeData monsterRuntime = new MonsterRuntimeData("M:1", null)
        {
            MonsterId = "Mon_Test",
            MaxHP = 10,
            CurrentHP = 10
        };
        monster.Initialize(monsterRuntime);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(monsterObject);
    }

    [Test]
    public void PlayerCasterAndMonsterTarget_AppliesFatigueOnlyToMonster()
    {
        new FatigueEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = player,
            MonsterTarget = monster,
            EffectId = "E_Fatigue",
            Value = 3,
            Count = 1
        });

        Assert.That(player.RuntimeData.StatusEffects.Any(x => x.EffectId == "E_Fatigue"), Is.False);
        Assert.That(monster.RuntimeData.StatusEffects.Single(x => x.EffectId == "E_Fatigue").Stack, Is.EqualTo(3));
    }

    [Test]
    public void ExplicitPlayerTarget_AppliesFatigueToPlayerTarget()
    {
        new FatigueEffect().Execute(new BattleEffectContext
        {
            PlayerTarget = player,
            EffectId = "E_Fatigue",
            Value = 2,
            Count = 1
        });

        Assert.That(player.RuntimeData.StatusEffects.Single(x => x.EffectId == "E_Fatigue").Stack, Is.EqualTo(2));
    }

    [Test]
    public void CasterWithoutExplicitTarget_DoesNotApplyFatigueToCaster()
    {
        new FatigueEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = player,
            EffectId = "E_Fatigue",
            Value = 3,
            Count = 1
        });

        Assert.That(player.RuntimeData.StatusEffects.Any(x => x.EffectId == "E_Fatigue"), Is.False);
    }
}
