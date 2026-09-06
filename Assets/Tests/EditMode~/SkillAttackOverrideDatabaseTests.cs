using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class SkillAttackOverrideDatabaseTests
{
    [Test]
    public void TryGetAttackIndex_ReturnsConfiguredAttackSlotForCharacterSkillPair()
    {
        SkillAttackOverrideDatabase database =
            ScriptableObject.CreateInstance<SkillAttackOverrideDatabase>();

        try
        {
            SetEntries(database, new List<SkillAttackOverrideEntry>
            {
                new()
                {
                    CharacterId = "C_Elise",
                    SkillId = "S_HeavySlash",
                    AttackSlot = SkillAttackSlot.Attack3
                }
            });

            bool found = database.TryGetAttackIndex(
                " C_Elise ",
                " S_HeavySlash ",
                out int attackIndex);

            Assert.That(found, Is.True);
            Assert.That(attackIndex, Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void PlaySkillAction_UsesSkillAttackOverrideBeforeRandomAttackSelection()
    {
        GameObject owner = new("SkillAttackOverrideOwner");
        GameObject attack1Prefab = new("OverrideAttack1Vfx");
        GameObject attack2Prefab = new("OverrideAttack2Vfx");
        GameObject attack3Prefab = new("OverrideAttack3Vfx");
        SkillAttackOverrideDatabase database =
            ScriptableObject.CreateInstance<SkillAttackOverrideDatabase>();

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Elise",
                MaxHP = 10,
                CurrentHP = 10
            });

            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                attack1 = CreatePresentation(attack1Prefab),
                attack2 = CreatePresentation(attack2Prefab),
                attack3 = CreatePresentation(attack3Prefab)
            });
            SetEntries(database, new List<SkillAttackOverrideEntry>
            {
                new()
                {
                    CharacterId = "C_Elise",
                    SkillId = "S_HeavySlash",
                    AttackSlot = SkillAttackSlot.Attack2
                }
            });
            SetPrivateField(animator, "skillAttackOverrideDatabase", database);

            animator.PlaySkillAction(new SkillMasterData
            {
                SkillId = "S_HeavySlash",
                SkillType = SkillType.Attack
            });

            Assert.That(owner.transform.Find("OverrideAttack2Vfx(Clone)"), Is.Not.Null);
            Assert.That(owner.transform.Find("OverrideAttack1Vfx(Clone)"), Is.Null);
            Assert.That(owner.transform.Find("OverrideAttack3Vfx(Clone)"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(attack3Prefab);
            Object.DestroyImmediate(attack2Prefab);
            Object.DestroyImmediate(attack1Prefab);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void PlaySkillAction_UsesExistingAttackSelectionWhenNoOverrideExists()
    {
        GameObject owner = new("SkillAttackOverrideFallbackOwner");
        GameObject attack3Prefab = new("FallbackAttack3Vfx");
        SkillAttackOverrideDatabase database =
            ScriptableObject.CreateInstance<SkillAttackOverrideDatabase>();

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Elise",
                MaxHP = 10,
                CurrentHP = 10
            });

            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                attack3 = CreatePresentation(attack3Prefab)
            });
            SetPrivateField(animator, "skillAttackOverrideDatabase", database);

            animator.PlaySkillAction(new SkillMasterData
            {
                SkillId = "S_UnmappedAttack",
                SkillType = SkillType.Attack
            });

            Assert.That(owner.transform.Find("FallbackAttack3Vfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(attack3Prefab);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void PlaySkillAction_ReplaysOverriddenAttackPresentationForEachHit()
    {
        GameObject owner = new("SkillAttackOverrideMultiHitOwner");
        GameObject attack2Prefab = new("OverrideMultiHitAttack2Vfx");
        SkillAttackOverrideDatabase database =
            ScriptableObject.CreateInstance<SkillAttackOverrideDatabase>();

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Elise",
                MaxHP = 10,
                CurrentHP = 10
            });

            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                attack2 = CreatePresentation(attack2Prefab)
            });
            SetEntries(database, new List<SkillAttackOverrideEntry>
            {
                new()
                {
                    CharacterId = "C_Elise",
                    SkillId = "S_HeavySlash",
                    AttackSlot = SkillAttackSlot.Attack2
                }
            });
            SetPrivateField(animator, "skillAttackOverrideDatabase", database);

            SkillMasterData skillData = new()
            {
                SkillId = "S_HeavySlash",
                SkillType = SkillType.Attack
            };

            animator.PlaySkillAction(skillData, 0);
            animator.PlaySkillAction(skillData, 1);

            int spawnedCount = 0;
            foreach (Transform child in owner.transform)
            {
                if (child.name == "OverrideMultiHitAttack2Vfx(Clone)")
                    spawnedCount++;
            }

            Assert.That(spawnedCount, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(attack2Prefab);
            Object.DestroyImmediate(owner);
        }
    }

    private static BattleUnitActionPresentation CreatePresentation(GameObject prefab)
    {
        return new BattleUnitActionPresentation
        {
            stateName = "",
            vfx = new BattleVfxEntry
            {
                prefab = prefab,
                flipType = VfxFlipType.None
            }
        };
    }

    private static void SetEntries(
        SkillAttackOverrideDatabase database,
        List<SkillAttackOverrideEntry> entries)
    {
        FieldInfo field = typeof(SkillAttackOverrideDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(database, entries);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
