using System.Collections.Generic;
using System.Reflection;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using NUnit.Framework;
using UnityEngine;

public class StatusEffectDamageModifierTests
{
    [Test]
    public void StrikeEffect_CorrosionOnMonsterCasterAddsStackToDamageDealt()
    {
        BattleCharacter playerTarget = CreatePlayer("Corrosion_Player_Target", 20);
        MonsterUnit monsterCaster = CreateMonster("Corrosion_Monster_Caster", 20);

        try
        {
            monsterCaster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Corrosion", 3));

            StrikeEffect effect = new();
            effect.Execute(new BattleEffectContext
            {
                MonsterCaster = monsterCaster,
                PlayerTarget = playerTarget,
                Value = 5
            });

            Assert.That(playerTarget.RuntimeData.CurrentHP, Is.EqualTo(12));
        }
        finally
        {
            DestroyBattleObject(playerTarget);
            DestroyBattleObject(monsterCaster);
            DestroyIfExists("BattleDamageTextPopupUI_Auto");
            DestroyIfExists("BattleDamageTextCanvas_Auto");
        }
    }

    [Test]
    public void StrikeEffect_GrudgeOnPlayerTargetAddsStackToReceivedDamage()
    {
        BattleCharacter playerTarget = CreatePlayer("Grudge_Player_Target", 20);
        MonsterUnit monsterCaster = CreateMonster("Grudge_Monster_Caster", 20);

        try
        {
            playerTarget.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Grudge", 2));

            StrikeEffect effect = new();
            effect.Execute(new BattleEffectContext
            {
                MonsterCaster = monsterCaster,
                PlayerTarget = playerTarget,
                Value = 5
            });

            Assert.That(playerTarget.RuntimeData.CurrentHP, Is.EqualTo(13));
        }
        finally
        {
            DestroyBattleObject(playerTarget);
            DestroyBattleObject(monsterCaster);
            DestroyIfExists("BattleDamageTextPopupUI_Auto");
            DestroyIfExists("BattleDamageTextCanvas_Auto");
        }
    }

    [Test]
    public void PierceEffect_CorrosionOnMonsterCasterAddsStackToDamageDealt()
    {
        BattleCharacter playerTarget = CreatePlayer("Pierce_Corrosion_Player_Target", 20);
        MonsterUnit monsterCaster = CreateMonster("Pierce_Corrosion_Monster_Caster", 20);

        try
        {
            monsterCaster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Corrosion", 3));

            PierceEffect effect = new();
            effect.Execute(new BattleEffectContext
            {
                MonsterCaster = monsterCaster,
                PlayerTarget = playerTarget,
                Value = 5
            });

            Assert.That(playerTarget.RuntimeData.CurrentHP, Is.EqualTo(12));
        }
        finally
        {
            DestroyBattleObject(playerTarget);
            DestroyBattleObject(monsterCaster);
            DestroyIfExists("BattleDamageTextPopupUI_Auto");
            DestroyIfExists("BattleDamageTextCanvas_Auto");
        }
    }

    [Test]
    public void PierceEffect_GrudgeOnPlayerTargetAddsStackToReceivedDamage()
    {
        BattleCharacter playerTarget = CreatePlayer("Pierce_Grudge_Player_Target", 20);
        MonsterUnit monsterCaster = CreateMonster("Pierce_Grudge_Monster_Caster", 20);

        try
        {
            playerTarget.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Grudge", 2));

            PierceEffect effect = new();
            effect.Execute(new BattleEffectContext
            {
                MonsterCaster = monsterCaster,
                PlayerTarget = playerTarget,
                Value = 5
            });

            Assert.That(playerTarget.RuntimeData.CurrentHP, Is.EqualTo(13));
        }
        finally
        {
            DestroyBattleObject(playerTarget);
            DestroyBattleObject(monsterCaster);
            DestroyIfExists("BattleDamageTextPopupUI_Auto");
            DestroyIfExists("BattleDamageTextCanvas_Auto");
        }
    }

    [Test]
    public void MonsterDamageText_CorrosionOnCasterAndGrudgeOnTargetShowsModifiedReservedDamage()
    {
        BattleCharacter playerTarget = CreatePlayer("Preview_Grudge_Player_Target", 20);
        MonsterUnit monsterCaster = CreateMonster("Preview_Corrosion_Monster_Caster", 20);

        try
        {
            playerTarget.SetGridIndex(7);
            playerTarget.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Grudge", 2));
            monsterCaster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Corrosion", 3));

            MonsterSkillData skill = CreateMonsterStrikeSkill("M_Status_Modifier_Preview", 5);
            MonsterReservedCommand command = new(monsterCaster.RuntimeData, skill);
            command.SetReservedDamage(5);
            command.SetRangeResult(new List<int> { 7 }, new List<int> { 7 });

            Assert.That(BattleDamageService.GetMonsterDamageText(command), Is.EqualTo("10"));
        }
        finally
        {
            DestroyBattleObject(playerTarget);
            DestroyBattleObject(monsterCaster);
            DestroyIfExists("BattleDamageTextPopupUI_Auto");
            DestroyIfExists("BattleDamageTextCanvas_Auto");
        }
    }

    [Test]
    public void MonsterDashDamage_AppliesCorrosionAndGrudgeToDirectDamage()
    {
        BattleCharacter playerTarget = CreatePlayer("Dash_Grudge_Player_Target", 20);
        MonsterUnit monsterCaster = CreateMonster("Dash_Corrosion_Monster_Caster", 20);

        try
        {
            playerTarget.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Grudge", 2));
            monsterCaster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Corrosion", 3));

            MonsterSkillData skill = CreateMonsterStrikeSkill("S_Monster_07", 5);
            MonsterReservedCommand command = new(monsterCaster.RuntimeData, skill);
            command.SetReservedDamage(5);

            BattleActionRunner runner = new(null);
            MethodInfo method = typeof(BattleActionRunner).GetMethod(
                "ApplyMonsterDashDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            method.Invoke(runner, new object[] { command, monsterCaster, playerTarget });

            Assert.That(playerTarget.RuntimeData.CurrentHP, Is.EqualTo(10));
        }
        finally
        {
            DestroyBattleObject(playerTarget);
            DestroyBattleObject(monsterCaster);
            DestroyIfExists("BattleDamageTextPopupUI_Auto");
            DestroyIfExists("BattleDamageTextCanvas_Auto");
        }
    }

    private static BattleCharacter CreatePlayer(string name, int hp)
    {
        GameObject gameObject = new(name);
        BattleCharacter character = gameObject.AddComponent<BattleCharacter>();
        character.Initialize(new CharacterRuntimeData
        {
            CharacterId = name,
            MaxHP = hp,
            CurrentHP = hp,
            CurrentShield = 0
        });

        return character;
    }

    private static MonsterUnit CreateMonster(string name, int hp)
    {
        GameObject gameObject = new(name);
        MonsterUnit monster = gameObject.AddComponent<MonsterUnit>();
        MonsterMasterData masterData = new()
        {
            MonsterId = name,
            Name = name,
            HP = hp
        };

        monster.Initialize(new MonsterRuntimeData(name, masterData));
        return monster;
    }

    private static MonsterSkillData CreateMonsterStrikeSkill(string skillId, int value)
    {
        MonsterSkillData skill = new()
        {
            SkillId = skillId,
            Target = TargetType.PlayerParty,
            EffectIds = "E_Strike",
            ValueRate = value.ToString(),
            CountRate = "1",
            TimelineNotation = TimelineActionType.Attack
        };

        skill.EffectEntries.Add(new SkillEffectEntry
        {
            EffectId = "E_Strike",
            ValueAmount = value,
            CountAmount = 1
        });

        return skill;
    }

    private static void DestroyBattleObject(BattleCharacter character)
    {
        if (character != null)
            Object.DestroyImmediate(character.gameObject);
    }

    private static void DestroyBattleObject(MonsterUnit monster)
    {
        if (monster != null)
            Object.DestroyImmediate(monster.gameObject);
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject gameObject = GameObject.Find(objectName);

        if (gameObject != null)
            Object.DestroyImmediate(gameObject);
    }
}
