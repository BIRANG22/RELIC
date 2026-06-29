using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillRewardAndInventoryTests
{
    [Test]
    public void SkillRarityUtility_AllowsUpgradeOnlyForAbilityPublicAndCore()
    {
        Assert.That(SkillRarityUtility.CanUpgrade(CreateSkill("S_Ability_01", Category.Ability)), Is.True);
        Assert.That(SkillRarityUtility.CanUpgrade(CreateSkill("S_Public_01", Category.Public)), Is.True);
        Assert.That(SkillRarityUtility.CanUpgrade(CreateSkill("S_Core_01", Category.Core)), Is.True);

        Assert.That(SkillRarityUtility.CanUpgrade(CreateSkill("S_Move_1", Category.Move)), Is.False);
        Assert.That(SkillRarityUtility.CanUpgrade(CreateSkill("S_Passive_01", Category.Passive)), Is.False);
        Assert.That(SkillRarityUtility.CanUpgrade(CreateSkill("S_Unique_01", Category.Unique)), Is.False);
    }

    [Test]
    public void SkillRarityUtility_AllowsUnequipForPublicAndCoreOnly()
    {
        Assert.That(SkillRarityUtility.CanUnequip(CreateSkill("S_Public_01", Category.Public)), Is.True);
        Assert.That(SkillRarityUtility.CanUnequip(CreateSkill("S_Core_01", Category.Core)), Is.True);

        Assert.That(SkillRarityUtility.CanUnequip(CreateSkill("S_Move_1", Category.Move)), Is.False);
        Assert.That(SkillRarityUtility.CanUnequip(CreateSkill("S_Passive_01", Category.Passive)), Is.False);
        Assert.That(SkillRarityUtility.CanUnequip(CreateSkill("S_Unique_01", Category.Unique)), Is.False);
        Assert.That(SkillRarityUtility.CanUnequip(CreateSkill("S_Ability_01", Category.Ability)), Is.False);
    }

    [Test]
    public void SkillRarityUtility_GetsBaseAndUpgradePairIds()
    {
        Assert.That(
            SkillRarityUtility.TryGetPairedVariantId("S_Core_01", out string upgradeId),
            Is.True);
        Assert.That(upgradeId, Is.EqualTo("S_Core_02"));

        Assert.That(
            SkillRarityUtility.TryGetPairedVariantId("S_Core_10", out string baseId),
            Is.True);
        Assert.That(baseId, Is.EqualTo("S_Core_09"));
    }

    [Test]
    public void SkillRewardRoller_RollsOnceAndSelectsBaseCoreSkillByRarity()
    {
        BattleMapData mapData = new()
        {
            BattleMapId = "Battlemap_Test",
            SkillDropChance = 1f,
            CoreCommonChance = 0f,
            CoreRareChance = 1f,
            CoreEpicChance = 0f
        };

        List<SkillMasterData> skills = new()
        {
            CreateSkill("S_Core_01", Category.Core, SkillRarity.CoreCommon),
            CreateSkill("S_Core_09", Category.Core, SkillRarity.CoreRare),
            CreateSkill("S_Core_10", Category.Core, SkillRarity.CoreRare),
            CreateSkill("S_Core_15", Category.Core, SkillRarity.CoreEpic),
            CreateSkill("S_Public_01", Category.Public, SkillRarity.Shared)
        };

        SequenceSkillRewardRandom random = new(new[] { 0f, 0f }, new[] { 0 });

        bool rolled = SkillRewardRoller.TryRoll(
            mapData,
            skills,
            random,
            out SkillMasterData reward);

        Assert.That(rolled, Is.True);
        Assert.That(reward.SkillId, Is.EqualTo("S_Core_09"));
    }

    [Test]
    public void SkillRewardRoller_DoesNotDropWhenChanceFails()
    {
        BattleMapData mapData = new()
        {
            BattleMapId = "Battlemap_Test",
            SkillDropChance = 0.25f,
            CoreCommonChance = 1f
        };

        SequenceSkillRewardRandom random = new(new[] { 0.9f }, Array.Empty<int>());

        bool rolled = SkillRewardRoller.TryRoll(
            mapData,
            new[] { CreateSkill("S_Core_01", Category.Core, SkillRarity.CoreCommon) },
            random,
            out SkillMasterData reward);

        Assert.That(rolled, Is.False);
        Assert.That(reward, Is.Null);
    }

    [Test]
    public void SkillInventoryEquipService_EquipsInventorySkillAndReturnsPreviousUnequippableSkill()
    {
        CharacterRuntimeStore characterStore = new();
        CharacterRuntimeData character = CreateCharacter("Char_01");
        character.EquippedSkillIds[2] = "S_Public_01";
        characterStore.AddOrUpdate(character);

        BattleRuntimeData battleRuntime = new()
        {
            SkillInventoryIds = new List<string> { "S_Core_01" }
        };

        Dictionary<string, SkillMasterData> skills = new()
        {
            ["S_Public_01"] = CreateSkill("S_Public_01", Category.Public, SkillRarity.Shared),
            ["S_Core_01"] = CreateSkill("S_Core_01", Category.Core, SkillRarity.CoreCommon)
        };

        SkillInventoryEquipService service = new(
            characterStore,
            battleRuntime,
            id => skills.TryGetValue(id, out SkillMasterData skill) ? skill : null);

        bool equipped = service.EquipInventorySkillToSlot("Char_01", 2, "S_Core_01");

        Assert.That(equipped, Is.True);
        Assert.That(character.EquippedSkillIds[2], Is.EqualTo("S_Core_01"));
        Assert.That(battleRuntime.SkillInventoryIds, Does.Not.Contain("S_Core_01"));
        Assert.That(battleRuntime.SkillInventoryIds, Does.Contain("S_Public_01"));
    }

    [Test]
    public void SkillInventoryEquipService_UnequipsCoreSkillBackToInventory()
    {
        CharacterRuntimeStore characterStore = new();
        CharacterRuntimeData character = CreateCharacter("Char_01");
        character.EquippedSkillIds[2] = "S_Core_01";
        characterStore.AddOrUpdate(character);

        BattleRuntimeData battleRuntime = new()
        {
            SkillInventoryIds = new List<string>()
        };

        Dictionary<string, SkillMasterData> skills = new()
        {
            ["S_Core_01"] = CreateSkill("S_Core_01", Category.Core, SkillRarity.CoreCommon)
        };

        SkillInventoryEquipService service = new(
            characterStore,
            battleRuntime,
            id => skills.TryGetValue(id, out SkillMasterData skill) ? skill : null);

        bool unequipped = service.UnequipSkillFromSlot("Char_01", 2);

        Assert.That(unequipped, Is.True);
        Assert.That(character.EquippedSkillIds[2], Is.Empty);
        Assert.That(battleRuntime.SkillInventoryIds, Does.Contain("S_Core_01"));
    }

    [Test]
    public void SkillInventoryPanelUI_UsesExistingGridLayoutAsSingleVerticalColumn()
    {
        GameObject panelObject = new("SkillInventoryPanel");
        GameObject contentObject = new("Content");
        contentObject.transform.SetParent(panelObject.transform);
        GridLayoutGroup grid = contentObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        SkillInventoryPanelUI panel = panelObject.AddComponent<SkillInventoryPanelUI>();

        typeof(SkillInventoryPanelUI)
            .GetField("inventoryContent", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(panel, contentObject.transform);

        MethodInfo method = typeof(SkillInventoryPanelUI)
            .GetMethod("EnsureInventoryVerticalLayout", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        Assert.DoesNotThrow(() => method.Invoke(panel, null));
        Assert.That(grid.enabled, Is.True);
        Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
        Assert.That(grid.constraintCount, Is.EqualTo(1));
        Assert.That(contentObject.GetComponent<VerticalLayoutGroup>(), Is.Null);

        UnityEngine.Object.DestroyImmediate(panelObject);
    }

    [Test]
    public void SkillInventoryPanelUI_LocksSkillEditingWhenBattleRoomLoaderIsActive()
    {
        GameObject battleRoomObject = new("BattleRoom");
        GameObject panelObject = new("SkillInventoryPanel");

        try
        {
            battleRoomObject.AddComponent<BattleRoomLoader>();
            SkillInventoryPanelUI panel = panelObject.AddComponent<SkillInventoryPanelUI>();

            Assert.That(panel.IsSkillEditLocked(), Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
            UnityEngine.Object.DestroyImmediate(battleRoomObject);
        }
    }

    [Test]
    public void SkillInventoryNotificationUI_ShowsButtonChildTextAndClearsOnClick()
    {
        GameObject buttonObject = new("InventoryButton");
        GameObject textObject = new("Text (TMP)");

        try
        {
            Button button = buttonObject.AddComponent<Button>();
            textObject.transform.SetParent(buttonObject.transform);
            textObject.AddComponent<TextMeshProUGUI>();
            textObject.SetActive(false);

            SkillInventoryNotificationUI notification =
                buttonObject.AddComponent<SkillInventoryNotificationUI>();

            notification.ShowNotice();

            Assert.That(textObject.activeSelf, Is.True);

            button.onClick.Invoke();

            Assert.That(textObject.activeSelf, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buttonObject);
        }
    }

    [Test]
    public void SkillUpgradePanel_ConfiguresContentGridUsingPrefabSizeAndWraps()
    {
        GameObject panelObject = new("SkillUpgradePanel");
        GameObject contentObject = new("Content", typeof(RectTransform));
        GameObject prefabObject = new("SkillUpgradeIconPrefab", typeof(RectTransform));

        try
        {
            contentObject.transform.SetParent(panelObject.transform);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(150f, 100f);

            RectTransform prefabRect = prefabObject.GetComponent<RectTransform>();
            prefabRect.sizeDelta = new Vector2(40f, 40f);

            SkillUpgradePanel panel = panelObject.AddComponent<SkillUpgradePanel>();
            SkillUpgradeIconItem prefab = prefabObject.AddComponent<SkillUpgradeIconItem>();

            typeof(SkillUpgradePanel)
                .GetField("contentRoot", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(panel, contentObject.transform);
            typeof(SkillUpgradePanel)
                .GetField("iconPrefab", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(panel, prefab);

            MethodInfo method = typeof(SkillUpgradePanel)
                .GetMethod("ConfigureContentLayout", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            Assert.DoesNotThrow(() => method.Invoke(panel, null));

            GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.cellSize, Is.EqualTo(new Vector2(40f, 40f)));
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(3));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
            UnityEngine.Object.DestroyImmediate(prefabObject);
        }
    }

    [Test]
    public void SkillUpgradePanel_ResetRestRoomUpgradeLimitClearsUpgradeLock()
    {
        GameObject panelObject = new("SkillUpgradePanel");

        try
        {
            SkillUpgradePanel panel = panelObject.AddComponent<SkillUpgradePanel>();

            MethodInfo method = typeof(SkillUpgradePanel)
                .GetMethod("CompleteUpgrade", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            Assert.DoesNotThrow(() => method.Invoke(panel, null));
            Assert.That(panel.HasUpgradedThisRestRoom, Is.True);

            panel.ResetRestRoomUpgradeLimit();

            Assert.That(panel.HasUpgradedThisRestRoom, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    private static SkillMasterData CreateSkill(
        string skillId,
        Category category,
        SkillRarity rarity = SkillRarity.None)
    {
        return new SkillMasterData
        {
            SkillId = skillId,
            Name = skillId,
            Category = category,
            Rarity = rarity
        };
    }

    private static CharacterRuntimeData CreateCharacter(string characterId)
    {
        return new CharacterRuntimeData
        {
            CharacterId = characterId,
            EquippedSkillIds = new string[4]
        };
    }

    private sealed class SequenceSkillRewardRandom : ISkillRewardRandom
    {
        private readonly Queue<float> values;
        private readonly Queue<int> ranges;

        public SequenceSkillRewardRandom(IEnumerable<float> values, IEnumerable<int> ranges)
        {
            this.values = new Queue<float>(values);
            this.ranges = new Queue<int>(ranges);
        }

        public float Value()
        {
            return values.Count > 0 ? values.Dequeue() : 0f;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (ranges.Count <= 0)
                return minInclusive;

            int value = ranges.Dequeue();

            if (value < minInclusive)
                return minInclusive;

            if (value >= maxExclusive)
                return maxExclusive - 1;

            return value;
        }
    }
}
