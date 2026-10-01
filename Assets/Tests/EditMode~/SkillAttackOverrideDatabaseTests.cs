using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class SkillAttackOverrideDatabaseTests
{
    private SkillAttackOverrideDatabase database;

    [SetUp]
    public void SetUp()
    {
        database = ScriptableObject.CreateInstance<SkillAttackOverrideDatabase>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(database);
    }

    [Test]
    public void TryGetPresentationSlot_UsesSharedEntryForAnyCharacter()
    {
        SetEntries(new SkillAttackOverrideEntry
        {
            ApplyToAllCharacters = true,
            SkillId = "Skill_Common",
            AttackSlot = SkillAttackSlot.Attack1
        });

        bool found = database.TryGetPresentationSlot(
            "Char_Any",
            "Skill_Common",
            out SkillAttackSlot slot);

        Assert.That(found, Is.True);
        Assert.That(slot, Is.EqualTo(SkillAttackSlot.Attack1));
    }

    [Test]
    public void TryGetPresentationSlot_PrefersCharacterEntryOverSharedEntry()
    {
        SetEntries(
            new SkillAttackOverrideEntry
            {
                ApplyToAllCharacters = true,
                SkillId = "Skill_Common",
                AttackSlot = SkillAttackSlot.Attack1
            },
            new SkillAttackOverrideEntry
            {
                CharacterId = "Char_01",
                SkillId = "Skill_Common",
                AttackSlot = SkillAttackSlot.Attack3
            });

        bool found = database.TryGetPresentationSlot(
            "Char_01",
            "Skill_Common",
            out SkillAttackSlot slot);

        Assert.That(found, Is.True);
        Assert.That(slot, Is.EqualTo(SkillAttackSlot.Attack3));
    }

    [Test]
    public void TryGetRepeatPresentationSlot_UsesSharedEntry()
    {
        SetEntries(new SkillAttackOverrideEntry
        {
            ApplyToAllCharacters = true,
            SkillId = "Skill_Repeat",
            AttackSlot = SkillAttackSlot.Attack1,
            RepeatAttackSlots = new List<SkillAttackSlot>
            {
                SkillAttackSlot.Attack2
            }
        });

        bool found = database.TryGetRepeatPresentationSlot(
            "Char_Any",
            "Skill_Repeat",
            SkillAttackSlot.Attack1,
            out SkillAttackSlot slot);

        Assert.That(found, Is.True);
        Assert.That(slot, Is.EqualTo(SkillAttackSlot.Attack2));
    }

    private void SetEntries(params SkillAttackOverrideEntry[] entries)
    {
        FieldInfo entriesField = typeof(SkillAttackOverrideDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(entriesField, Is.Not.Null);
        entriesField.SetValue(database, new List<SkillAttackOverrideEntry>(entries));
        database.Initialize();
    }
}
