using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class SkillVfxDatabaseTests
{
    [Test]
    public void TryGetVfx_ReturnsConfiguredVfxForSkillId()
    {
        SkillVfxDatabase database = ScriptableObject.CreateInstance<SkillVfxDatabase>();
        GameObject prefab = new("ConfiguredSkillVfx");

        try
        {
            SetEntries(database, new List<SkillVfxEntry>
            {
                new()
                {
                    SkillId = "S_Ability_11",
                    Vfx = new BattleVfxEntry { prefab = prefab, flipType = VfxFlipType.None }
                }
            });

            bool found = database.TryGetVfx(" S_Ability_11 ", out BattleVfxEntry entry);

            Assert.That(found, Is.True);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.prefab, Is.SameAs(prefab));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void PlaySkillAction_SpawnsConfiguredSkillVfxForAbility11()
    {
        GameObject owner = new("SkillVfxOwner");
        GameObject prefab = new("Vfx_SpriteAni_flash_explosion");
        SkillVfxDatabase database = ScriptableObject.CreateInstance<SkillVfxDatabase>();

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetEntries(database, new List<SkillVfxEntry>
            {
                new()
                {
                    SkillId = "S_Ability_11",
                    Vfx = new BattleVfxEntry { prefab = prefab, flipType = VfxFlipType.None }
                }
            });
            SetPrivateField(animator, "skillVfxDatabase", database);

            animator.PlaySkillAction(new SkillMasterData
            {
                SkillId = "S_Ability_11",
                SkillType = SkillType.Attack
            }, 0);

            Assert.That(owner.transform.Find("Vfx_SpriteAni_flash_explosion(Clone)"), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void PlaySkillAction_DoesNotSpawnSkillVfxWhenSkillIdIsUnmapped()
    {
        GameObject owner = new("SkillVfxFallbackOwner");
        GameObject prefab = new("UnmappedSkillVfx");
        SkillVfxDatabase database = ScriptableObject.CreateInstance<SkillVfxDatabase>();

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetEntries(database, new List<SkillVfxEntry>
            {
                new()
                {
                    SkillId = "S_Ability_11",
                    Vfx = new BattleVfxEntry { prefab = prefab, flipType = VfxFlipType.None }
                }
            });
            SetPrivateField(animator, "skillVfxDatabase", database);

            animator.PlaySkillAction(new SkillMasterData
            {
                SkillId = "S_Unmapped",
                SkillType = SkillType.Attack
            }, 0);

            Assert.That(owner.transform.Find("UnmappedSkillVfx(Clone)"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void PlaySkillAction_RepeatsSkillVfxForEachHit()
    {
        GameObject owner = new("SkillVfxMultiHitOwner");
        GameObject prefab = new("MultiHitSkillVfx");
        SkillVfxDatabase database = ScriptableObject.CreateInstance<SkillVfxDatabase>();

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetEntries(database, new List<SkillVfxEntry>
            {
                new()
                {
                    SkillId = "S_Ability_11",
                    Vfx = new BattleVfxEntry { prefab = prefab, flipType = VfxFlipType.None }
                }
            });
            SetPrivateField(animator, "skillVfxDatabase", database);

            SkillMasterData skillData = new()
            {
                SkillId = "S_Ability_11",
                SkillType = SkillType.Attack
            };

            animator.PlaySkillAction(skillData, 0);
            animator.PlaySkillAction(skillData, 1);

            int spawnedCount = 0;
            foreach (Transform child in owner.transform)
            {
                if (child.name == "MultiHitSkillVfx(Clone)")
                    spawnedCount++;
            }

            Assert.That(spawnedCount, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(owner);
        }
    }

    private static void SetEntries(SkillVfxDatabase database, List<SkillVfxEntry> entries)
    {
        FieldInfo field = typeof(SkillVfxDatabase).GetField(
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
