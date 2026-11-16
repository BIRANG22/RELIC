using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BondageEffectTargetingTests
{
    private GameObject playerObject;
    private GameObject monsterObject;
    private BattleCharacter player;
    private MonsterUnit monster;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("BondageCaster");
        player = playerObject.AddComponent<BattleCharacter>();
        player.Initialize(new CharacterRuntimeData
        {
            CharacterId = "Char_03",
            MaxHP = 10,
            CurrentHP = 10
        });

        monsterObject = new GameObject("BondageTarget");
        monster = monsterObject.AddComponent<MonsterUnit>();
        monster.Initialize(new MonsterRuntimeData("M:Bondage", null)
        {
            MonsterId = "Mon_Test",
            MaxHP = 10,
            CurrentHP = 10
        });
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(monsterObject);
    }

    [Test]
    public void PlayerCasterAndMonsterTarget_AppliesBondageOnlyToMonster()
    {
        new BondageEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = player,
            MonsterTarget = monster,
            EffectId = "E_Bondage",
            Value = 1,
            Count = 1
        });

        Assert.That(player.RuntimeData.StatusEffects.Any(x => x.EffectId == "E_Bondage"), Is.False);
        Assert.That(monster.RuntimeData.StatusEffects.Single(x => x.EffectId == "E_Bondage").Stack, Is.EqualTo(1));
    }

    [Test]
    public void ExplicitPlayerTarget_AppliesBondageToPlayerTarget()
    {
        new BondageEffect().Execute(new BattleEffectContext
        {
            PlayerTarget = player,
            EffectId = "E_Bondage",
            Value = 2,
            Count = 1
        });

        Assert.That(player.RuntimeData.StatusEffects.Single(x => x.EffectId == "E_Bondage").Stack, Is.EqualTo(2));
    }

    [Test]
    public void CasterWithoutExplicitTarget_DoesNotApplyBondageToCaster()
    {
        new BondageEffect().Execute(new BattleEffectContext
        {
            PlayerCaster = player,
            EffectId = "E_Bondage",
            Value = 1,
            Count = 1
        });

        Assert.That(player.RuntimeData.StatusEffects.Any(x => x.EffectId == "E_Bondage"), Is.False);
    }

    [Test]
    public void TryBlockGridMovement_BondageConsumesOneStackAndBlocksMovement()
    {
        List<StatusEffectRuntimeData> statuses = new()
        {
            new StatusEffectRuntimeData("E_Bondage", 2)
        };
        EffectMasterData bondage = new EffectMasterData
        {
            EffectId = "E_Bondage",
            EndTurn = EndTurn.DecreaseOnTrigger
        };

        bool blocked = BattleStatusEffectService.TryBlockGridMovement(
            statuses,
            _ => bondage);

        Assert.That(blocked, Is.True);
        Assert.That(statuses.Single().Stack, Is.EqualTo(1));
    }

    [Test]
    public void TryBlockGridMovement_WithoutBondageAllowsMovement()
    {
        bool blocked = BattleStatusEffectService.TryBlockGridMovement(
            new List<StatusEffectRuntimeData>(),
            _ => null);

        Assert.That(blocked, Is.False);
    }
}
