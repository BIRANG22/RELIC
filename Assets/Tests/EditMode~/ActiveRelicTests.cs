using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class ActiveRelicTests
{
    [Test]
    public void DataRowMapper_MapsActiveRelicColumns()
    {
        Dictionary<string, string> row = new()
        {
            ["FragmentId"] = "Relic_11",
            ["Name"] = "Adrenaline",
            ["Type"] = "Active",
            ["Durability"] = "3",
            ["EffectIds"] = "E_Value",
            ["EffectDesc"] = "Damage boost"
        };

        RelicData relic = DataRowMapper.Map<RelicData>(row);

        Assert.That(relic.FragmentId, Is.EqualTo("Relic_11"));
        Assert.That(relic.Type, Is.EqualTo("Active"));
        Assert.That(relic.Durability, Is.EqualTo(3));
    }

    [Test]
    public void DataRowMapper_MapsCharacterStartingRelicColumn()
    {
        Dictionary<string, string> row = new()
        {
            ["CharacterId"] = "Char_01",
            ["Name"] = "Knight",
            ["Relic"] = "Relic_11"
        };

        CharacterMasterData character = DataRowMapper.Map<CharacterMasterData>(row);

        Assert.That(character.CharacterId, Is.EqualTo("Char_01"));
        Assert.That(character.Relic, Is.EqualTo("Relic_11"));
    }

    [Test]
    public void CharacterStartingRelicUtility_EquipsMasterRelicIntoFirstSlot()
    {
        CharacterMasterData master = new()
        {
            CharacterId = "Char_01",
            Relic = "Relic_11"
        };

        string[] slots = CharacterStartingRelicUtility.CreateStartingRelicSlots(master);

        Assert.That(slots, Has.Length.EqualTo(5));
        Assert.That(slots[0], Is.EqualTo("Relic_11"));
        Assert.That(slots[1], Is.Null.Or.Empty);
    }

    [Test]
    public void CharacterStartingRelicUtility_FillsEmptyExistingFirstSlot()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_01",
            EquippedRelicIds = new string[5]
        };
        CharacterMasterData master = new()
        {
            CharacterId = "Char_01",
            Relic = "Relic_11"
        };

        bool changed = CharacterStartingRelicUtility.EnsureStartingRelicEquippedIfEmpty(
            runtime,
            master,
            null);

        Assert.That(changed, Is.True);
        Assert.That(runtime.EquippedRelicIds[0], Is.EqualTo("Relic_11"));
    }

    [Test]
    public void CharacterStartingRelicUtility_DoesNotOverwriteExistingFirstSlot()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_01",
            EquippedRelicIds = new[] { "Relic_03", null, null, null, null }
        };
        CharacterMasterData master = new()
        {
            CharacterId = "Char_01",
            Relic = "Relic_11"
        };

        bool changed = CharacterStartingRelicUtility.EnsureStartingRelicEquippedIfEmpty(
            runtime,
            master,
            null);

        Assert.That(changed, Is.False);
        Assert.That(runtime.EquippedRelicIds[0], Is.EqualTo("Relic_03"));
    }

    [Test]
    public void ActiveRelicRuntimeUtility_InitializesAndConsumesUsesFromDurability()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_01",
            EquippedRelicIds = new[] { "Relic_11", null, null, null, null }
        };
        RelicData relic = new()
        {
            FragmentId = "Relic_11",
            Type = "Active",
            Durability = 2
        };

        Assert.That(ActiveRelicRuntimeUtility.GetRemainingUses(runtime, relic), Is.EqualTo(2));
        Assert.That(ActiveRelicRuntimeUtility.TryConsumeUse(runtime, relic), Is.True);
        Assert.That(ActiveRelicRuntimeUtility.GetRemainingUses(runtime, relic), Is.EqualTo(1));
    }

    [Test]
    public void ActiveRelicEffectResolver_UsesSpecificFallbackForCurrentExcelPlaceholders()
    {
        RelicData relic = new()
        {
            FragmentId = "Relic_13",
            Type = "Active",
            Durability = 3,
            EffectIds = "E_Value"
        };

        Assert.That(
            ActiveRelicEffectResolver.ResolveEffectId(relic),
            Is.EqualTo(ActiveRelicEffectIds.MoveToGrid));
    }
}
