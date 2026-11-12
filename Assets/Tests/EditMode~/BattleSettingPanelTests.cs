using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class BattleSettingPanelTests
{
    [Test]
    public void ResourceHoverState_LateExitPreservesNewerHover()
    {
        GameObject regenObject = new GameObject("Regen", typeof(RectTransform));
        GameObject manaObject = new GameObject("Mana", typeof(RectTransform));
        try
        {
            RectTransform regen = regenObject.GetComponent<RectTransform>();
            RectTransform mana = manaObject.GetComponent<RectTransform>();
            BattleSettingPanelUI.ResourceHoverState state = new BattleSettingPanelUI.ResourceHoverState();

            state.Enter(regen, BattleSettingPanelUI.TooltipKind.Recovery);
            state.Enter(mana, BattleSettingPanelUI.TooltipKind.Mana);
            state.Exit(regen);

            Assert.That(state.Target, Is.SameAs(mana));
            Assert.That(state.Kind, Is.EqualTo(BattleSettingPanelUI.TooltipKind.Mana));
        }
        finally
        {
            Object.DestroyImmediate(manaObject);
            Object.DestroyImmediate(regenObject);
        }
    }

    [Test]
    public void ResourceHoverState_CurrentExitClearsHover()
    {
        GameObject karmaObject = new GameObject("Karma", typeof(RectTransform));
        try
        {
            RectTransform karma = karmaObject.GetComponent<RectTransform>();
            BattleSettingPanelUI.ResourceHoverState state = new BattleSettingPanelUI.ResourceHoverState();

            state.Enter(karma, BattleSettingPanelUI.TooltipKind.Karma);
            state.Exit(karma);

            Assert.That(state.Target, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(karmaObject);
        }
    }

    [Test]
    public void ResolveEventCanvas_UsesTopLevelCanvasForNestedWorldSpaceCanvas()
    {
        GameObject rootObject = new GameObject("RootCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject nestedObject = new GameObject("NestedCanvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            nestedObject.transform.SetParent(rootObject.transform, false);
            Canvas root = rootObject.GetComponent<Canvas>();
            Canvas nested = nestedObject.GetComponent<Canvas>();
            root.renderMode = RenderMode.ScreenSpaceOverlay;
            nested.renderMode = RenderMode.WorldSpace;
            nested.overrideSorting = true;

            Assert.That(BattleSettingPanelUI.ResolveEventCanvas(nested), Is.SameAs(root));
        }
        finally
        {
            Object.DestroyImmediate(nestedObject);
            Object.DestroyImmediate(rootObject);
        }
    }

    [TestCase("Mana", BattleSettingPanelUI.TooltipKind.Mana)]
    [TestCase("Regen", BattleSettingPanelUI.TooltipKind.Recovery)]
    [TestCase("Karma", BattleSettingPanelUI.TooltipKind.Karma)]
    public void GetResourceTooltipKind_UsesExplicitSectionMapping(
        string sectionName,
        BattleSettingPanelUI.TooltipKind expected)
    {
        Assert.That(BattleSettingPanelUI.GetResourceTooltipKind(sectionName), Is.EqualTo(expected));
    }

    [Test]
    public void ModalRules_StayAboveMenuAndPreserveOtherPause()
    {
        Assert.That(BattleSettingPanelUI.GetModalSortingOrder(30003), Is.EqualTo(30004));
        Assert.That(BattleSettingPanelUI.ResolveResumeTimeScale(1f, false), Is.EqualTo(1f));
        Assert.That(BattleSettingPanelUI.ResolveResumeTimeScale(1f, true), Is.EqualTo(0f));
        Assert.That(BattleSettingPanelUI.ResolveResumeTimeScale(0f, false), Is.EqualTo(0f));
    }

    [TestCase(BattleSettingPanelUI.TooltipKind.Skill, "Skill_01", true)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Rune, "Rune_01", true)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Relic, "Relic_01", true)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Compound, "Compound_01", true)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Skill, "", false)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Rune, null, false)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Mana, null, true)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Recovery, null, true)]
    [TestCase(BattleSettingPanelUI.TooltipKind.Karma, null, true)]
    public void CanShowTooltip_RequiresIdsOnlyForDataBackedKinds(
        BattleSettingPanelUI.TooltipKind kind,
        string dataId,
        bool expected)
    {
        Assert.That(BattleSettingPanelUI.CanShowTooltip(kind, dataId), Is.EqualTo(expected));
    }

    [TestCase(BattleSettingPanelUI.TooltipKind.Mana, "common.mana", "마나")]
    [TestCase(BattleSettingPanelUI.TooltipKind.Recovery, "common.recovery", "마나재생량")]
    [TestCase(BattleSettingPanelUI.TooltipKind.Karma, "resource.karma", "카르마")]
    public void GetStatTooltipText_ReturnsLocalizationBinding(
        BattleSettingPanelUI.TooltipKind kind,
        string expectedKey,
        string expectedFallback)
    {
        BattleSettingPanelUI.StatTooltipText result = BattleSettingPanelUI.GetStatTooltipText(kind);

        Assert.That(result.LocalizationKey, Is.EqualTo(expectedKey));
        Assert.That(result.Fallback, Is.EqualTo(expectedFallback));
    }

    [TestCase(Category.Passive, SkillType.Attack, 0, SkillRarity.Common, "common.passive")]
    [TestCase(Category.Core, SkillType.Attack, 0, SkillRarity.Common, "common.attack")]
    [TestCase(Category.Core, SkillType.Buff, 1, SkillRarity.Common, "common.fast_buff")]
    [TestCase(Category.Core, SkillType.Debuff, 1, SkillRarity.Common, "common.fast_debuff")]
    [TestCase(Category.Move, SkillType.Attack, 0, SkillRarity.Move, "common.move")]
    public void GetSkillTypeLocalizationKey_MatchesBattleSkillTooltip(
        Category category,
        SkillType skillType,
        int isFastSkill,
        SkillRarity rarity,
        string expected)
    {
        SkillMasterData skill = new SkillMasterData
        {
            Category = category,
            SkillType = skillType,
            IsFastSkill = isFastSkill,
            Rarity = rarity
        };

        Assert.That(BattleSettingPanelUI.GetSkillTypeLocalizationKey(skill), Is.EqualTo(expected));
    }

    [Test]
    public void FormatMana_UsesCompactSingleLineFormat()
    {
        Assert.That(BattleSettingPanelUI.FormatMana(14, 14), Is.EqualTo("14/14"));
        Assert.That(BattleSettingPanelUI.FormatMana(99, 99), Is.EqualTo("99/99"));
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void GetNextPanelActiveState_InvertsCurrentState(bool current, bool expected)
    {
        Assert.That(BattleSettingPanelUI.GetNextPanelActiveState(current), Is.EqualTo(expected));
    }

    [Test]
    public void BuildSnapshot_MapsCurrentBattleEquipmentWithoutMutatingRuntime()
    {
        CharacterRuntimeData runtime = new CharacterRuntimeData
        {
            CharacterId = "Character_01",
            CurrentHP = 37,
            MaxHP = 50,
            CurrentCost = 7,
            MaxCost = 12,
            CostRecovery = 2,
            BonusCostRecovery = 1,
            CurrentResource = 4,
            PassiveSkillId = "Passive_01",
            UniqueSkillId = "Ultimate_01",
            AbilitySkillId = "Memory_Fallback",
            EquippedSkillIds = new[] { "Passive_01", "Memory_01", "Memory_02", "Memory_03" },
            EquippedRuneIds = new[] { "Rune_01", "Rune_02", "Rune_03", "Rune_04", "Rune_05", "Rune_06" },
            EquippedRelicIds = new[] { "Relic_Active", "Artifact_01", "Artifact_02", "Artifact_03", "Artifact_04", "Artifact_05", "Artifact_06" }
        };

        BattleSettingPanelUI.CharacterSnapshot snapshot = BattleSettingPanelUI.BuildSnapshot(runtime);

        Assert.That(snapshot.CharacterId, Is.EqualTo("Character_01"));
        Assert.That(snapshot.CurrentHP, Is.EqualTo(37));
        Assert.That(snapshot.CurrentCost, Is.EqualTo(7));
        Assert.That(snapshot.MaxCost, Is.EqualTo(12));
        Assert.That(snapshot.CostRecovery, Is.EqualTo(3));
        Assert.That(snapshot.CurrentResource, Is.EqualTo(4));
        Assert.That(snapshot.PassiveSkillId, Is.EqualTo("Passive_01"));
        Assert.That(snapshot.UniqueSkillId, Is.EqualTo("Ultimate_01"));
        Assert.That(snapshot.MemorySkillIds, Is.EqualTo(new[] { "Memory_01", "Memory_02", "Memory_03" }));
        Assert.That(snapshot.ActiveRelicId, Is.EqualTo("Relic_Active"));
        Assert.That(snapshot.ArtifactIds, Is.EqualTo(new[] { "Artifact_01", "Artifact_02", "Artifact_03", "Artifact_04", "Artifact_05", "Artifact_06" }));
        Assert.That(snapshot.RuneIds, Is.EqualTo(runtime.EquippedRuneIds));
        Assert.That(runtime.CurrentHP, Is.EqualTo(37));
    }

    [Test]
    public void BuildSnapshot_UsesAbilitySkillForEmptyFirstMemorySlot()
    {
        CharacterRuntimeData runtime = new CharacterRuntimeData
        {
            AbilitySkillId = "Memory_Ability",
            EquippedSkillIds = new string[4]
        };

        BattleSettingPanelUI.CharacterSnapshot snapshot = BattleSettingPanelUI.BuildSnapshot(runtime);

        Assert.That(snapshot.MemorySkillIds[0], Is.EqualTo("Memory_Ability"));
    }

    [Test]
    public void ResolveJobIcon_UsesCharacterIconDatabaseMarkAndClearsMissingEntry()
    {
        CharacterIconDatabase database = ScriptableObject.CreateInstance<CharacterIconDatabase>();
        Texture2D texture = new Texture2D(2, 2);
        Sprite expected = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        try
        {
            FieldInfo entriesField = typeof(CharacterIconDatabase).GetField(
                "entries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(entriesField, Is.Not.Null);
            entriesField.SetValue(database, new List<CharacterIconEntry>
            {
                new CharacterIconEntry { CharacterId = "Character_Job", Mark = expected }
            });

            Assert.That(BattleSettingPanelUI.ResolveJobIcon(database, "Character_Job"), Is.SameAs(expected));
            Assert.That(BattleSettingPanelUI.ResolveJobIcon(database, "Missing"), Is.Null);
            Assert.That(BattleSettingPanelUI.ResolveJobIcon(null, "Character_Job"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(expected);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(database);
        }
    }
}

