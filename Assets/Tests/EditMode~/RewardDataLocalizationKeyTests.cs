using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class RewardDataLocalizationKeyTests
{
    private const string EquipPanelPrefabPath = "Assets/Project/PrefabsR/Equip_panel.prefab";

    [Test]
    public void EquipPanelPrefab_RuntimeItemTextsHaveNoStaticLocalizationWriter()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(EquipPanelPrefabPath);
        try
        {
            BattleRewardEquipPanelUI panel = root.GetComponentInChildren<BattleRewardEquipPanelUI>(true);
            Assert.That(panel, Is.Not.Null);

            foreach (string fieldName in new[] { "itemNameText", "itemRarityText", "itemEffectText" })
            {
                TMP_Text text = typeof(BattleRewardEquipPanelUI)
                    .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(panel) as TMP_Text;

                Assert.That(text, Is.Not.Null, fieldName);
                Assert.That(text.GetComponent<LocalizationIgnore>(), Is.Not.Null, fieldName);
                Assert.That(text.GetComponent<LocalizedTMPText>(), Is.Null, fieldName);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void SkillMasterLocalizationWorkbook_CoversEveryRuntimeNameAndDetailsKey()
    {
        IReadOnlyList<IReadOnlyList<string>> gameRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/GameData.xlsx", "SkillMaster");
        IReadOnlyList<IReadOnlyList<string>> localizationRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");

        HashSet<string> localizationKeys = localizationRows
            .Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .Select(row => row[0])
            .ToHashSet(System.StringComparer.Ordinal);

        for (int rowIndex = 2; rowIndex < gameRows.Count; rowIndex++)
        {
            IReadOnlyList<string> row = gameRows[rowIndex];
            if (row.Count <= 14 || string.IsNullOrWhiteSpace(row[0]))
                continue;

            string normalizedId = row[0].Trim().ToLowerInvariant();
            if (LocalizationProjectScanner.IsLocalizableKoreanText(row[1]))
                Assert.That(localizationKeys, Does.Contain($"data.skill_master.{normalizedId}.name"), row[0]);
            if (LocalizationProjectScanner.IsLocalizableKoreanText(row[14]))
                Assert.That(localizationKeys, Does.Contain($"data.skill_master.{normalizedId}.details"), row[0]);
        }

        Assert.That(localizationKeys.Any(key => key.StartsWith("data.skillmaster.", System.StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void SkillMasterStringTables_HaveOneLocaleEntryForEveryCanonicalKey()
    {
        string shared = File.ReadAllText("Assets/Language/Text Shared Data.asset");
        var requiredIds = Regex.Matches(
                shared,
                @"(?m)^  - m_Id: (?<id>\d+)\r?\n    m_Key: data\.skill_master\.[^\r\n]+\.(?:name|details)$")
            .Cast<Match>()
            .Select(match => match.Groups["id"].Value)
            .ToArray();

        Assert.That(requiredIds, Is.Not.Empty);
        Assert.That(requiredIds.Distinct().Count(), Is.EqualTo(requiredIds.Length));

        foreach (string locale in new[] { "ko", "en", "zh-Hans", "ja", "es" })
        {
            string table = File.ReadAllText($"Assets/Language/Text_{locale}.asset");
            string[] localeIds = Regex.Matches(table, @"(?m)^  - m_Id: (?<id>\d+)$")
                .Cast<Match>()
                .Select(match => match.Groups["id"].Value)
                .ToArray();

            Assert.That(localeIds.Distinct().Count(), Is.EqualTo(localeIds.Length), locale);
            foreach (string requiredId in requiredIds)
                Assert.That(localeIds.Count(id => id == requiredId), Is.EqualTo(1), $"{locale}:{requiredId}");
        }
    }

    [Test]
    public void EquipPanel_DynamicItemTextsDisableStaticLocalizationWriters()
    {
        GameObject panelObject = new GameObject("EquipPanel");
        panelObject.SetActive(false);

        try
        {
            BattleRewardEquipPanelUI panel = panelObject.AddComponent<BattleRewardEquipPanelUI>();
            string[] fieldNames =
            {
                "itemNameText",
                "itemRarityText",
                "itemEffectText",
                "itemCostValueText",
            };

            foreach (string fieldName in fieldNames)
            {
                GameObject textObject = new GameObject(fieldName);
                textObject.transform.SetParent(panelObject.transform);
                TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
                LocalizedTMPText staticWriter = textObject.AddComponent<LocalizedTMPText>();
                DynamicLocalizedTMPText dynamicWriter = textObject.AddComponent<DynamicLocalizedTMPText>();

                typeof(BattleRewardEquipPanelUI)
                    .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(panel, text);

                Assert.That(staticWriter.enabled, Is.True, $"{fieldName}의 사전 조건이 잘못되었습니다.");
                Assert.That(dynamicWriter.enabled, Is.True, $"{fieldName}의 동적 작성자 사전 조건이 잘못되었습니다.");
            }

            typeof(BattleRewardEquipPanelUI)
                .GetMethod("ProtectDynamicItemTexts", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(panel, null);

            foreach (string fieldName in fieldNames)
            {
                Transform textTransform = panelObject.transform.Find(fieldName);
                Assert.That(textTransform.GetComponent<LocalizationIgnore>(), Is.Not.Null, fieldName);
                Assert.That(textTransform.GetComponent<LocalizationAutoBindingIgnore>(), Is.Not.Null, fieldName);
                Assert.That(textTransform.GetComponent<LocalizedTMPText>().enabled, Is.False, fieldName);
                Assert.That(textTransform.GetComponent<DynamicLocalizedTMPText>().enabled, Is.False, fieldName);
            }
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [TestCase("SkillMaster", "S_Core_01", "이름", "data.skill_master.s_core_01.name")]
    [TestCase("MonsterSkill", "S_Monster_01", "효과설명", "data.monster_skill.s_monster_01.effect_description")]
    public void GameDataScanner_UsesTheSameCamelCaseNormalizationAsRuntime(
        string sheet,
        string stableId,
        string header,
        string expected)
    {
        Assert.That(
            LocalizationProjectScanner.BuildGameDataKey(sheet, stableId, header),
            Is.EqualTo(expected));
    }

    [TestCase("효과")]
    [TestCase("Details")]
    public void SkillMasterDetailsColumn_IsPlayerFacingAndUsesDetailsField(string header)
    {
        Assert.That(
            LocalizationProjectScanner.IsPlayerFacingGameDataColumn("SkillMaster", header),
            Is.True);
        Assert.That(
            LocalizationProjectScanner.BuildGameDataKey("SkillMaster", "S_Core_01", header),
            Is.EqualTo("data.skill_master.s_core_01.details"));
    }

    [TestCase("Common", "common.rarity.common")]
    [TestCase("Rare", "common.rarity.rare")]
    [TestCase("Epic", "common.rarity.epic")]
    [TestCase("Unique", "common.rarity.unique")]
    [TestCase("언커먼", "common.rarity.rare")]
    public void RelicRarity_UsesSharedSemanticLocalizationKey(string rawRarity, string expectedKey)
    {
        Assert.That(RelicRarityUtility.GetLocalizationKey(rawRarity), Is.EqualTo(expectedKey));
    }

    [Test]
    public void RelicRarity_UnknownValueDoesNotSelectAnUnrelatedKey()
    {
        Assert.That(RelicRarityUtility.GetLocalizationKey("UnknownTier"), Is.Empty);
    }

    [TestCase(Category.Passive, SkillRarity.Exclusive, "본능 기억")]
    [TestCase(Category.Unique, SkillRarity.Exclusive, "발현 기억")]
    [TestCase(Category.Ability, SkillRarity.Exclusive, "구현 기억")]
    [TestCase(Category.Core, SkillRarity.Rare, "레어 기억")]
    public void SkillRarity_HasExplicitKoreanFallback(
        Category category,
        SkillRarity rarity,
        string expected)
    {
        var skill = new SkillMasterData { Category = category, Rarity = rarity };

        Assert.That(SkillRarityUtility.GetKoreanFallbackName(skill), Is.EqualTo(expected));
    }

    [Test]
    public void EquipPanel_ResolvesSkillIconFromDatabaseWhenRewardCacheIsEmpty()
    {
        Texture2D texture = new Texture2D(2, 2);
        Sprite databaseIcon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
        SkillIconDatabase database = ScriptableObject.CreateInstance<SkillIconDatabase>();

        try
        {
            typeof(SkillIconDatabase)
                .GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(database, new List<SkillIconEntry>
                {
                    new SkillIconEntry { SkillId = "S_Core_01", Icon = databaseIcon },
                });
            database.Initialize();

            MethodInfo resolver = typeof(BattleRewardEquipPanelUI).GetMethod(
                "ResolveSkillRewardIcon",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolver, Is.Not.Null);

            Sprite result = resolver.Invoke(
                null,
                new object[]
                {
                    new BattleRewardData { RewardId = "S_Core_01", Icon = null },
                    new SkillMasterData { SkillId = "S_Core_01", Icon = null },
                    database,
                }) as Sprite;

            Assert.That(result, Is.SameAs(databaseIcon));
        }
        finally
        {
            Object.DestroyImmediate(databaseIcon);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(database);
        }
    }

    [TestCase(BattleRewardType.Item, "I_001")]
    [TestCase(BattleRewardType.Relic, "R_001")]
    [TestCase(BattleRewardType.Skill, "S_Core_01")]
    public void RewardPanel_IdBackedRewardAlwaysRefreshesPresentation(
        BattleRewardType rewardType,
        string rewardId)
    {
        MethodInfo policy = typeof(BattleRewardPanelUI).GetMethod(
            "ShouldPopulateRewardPresentation",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(policy, Is.Not.Null);

        var reward = new BattleRewardData
        {
            Type = rewardType,
            RewardId = rewardId,
            Name = "미번역",
            Icon = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                Vector2.zero),
        };

        try
        {
            Assert.That(policy.Invoke(null, new object[] { reward }), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(reward.Icon);
        }
    }

    [TestCase(SkillRarity.None, SkillType.Attack, EventChoiceSkillRewardFilter.Attack, false)]
    [TestCase(SkillRarity.Common, SkillType.Attack, EventChoiceSkillRewardFilter.Attack, true)]
    [TestCase(SkillRarity.Rare, SkillType.Buff, EventChoiceSkillRewardFilter.Buff, true)]
    [TestCase(SkillRarity.Epic, SkillType.Debuff, EventChoiceSkillRewardFilter.Debuff, true)]
    public void EventSkillRewardFilter_RejectsNonDropRarityCandidates(
        SkillRarity rarity,
        SkillType skillType,
        EventChoiceSkillRewardFilter filter,
        bool expected)
    {
        MethodInfo matcher = typeof(EventRoomController).GetMethod(
            "MatchesSkillRewardFilter",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(matcher, Is.Not.Null);

        var skill = new SkillMasterData
        {
            SkillId = "S_Core_Test",
            Category = Category.Core,
            Rarity = rarity,
            SkillType = skillType,
        };

        Assert.That(matcher.Invoke(null, new object[] { skill, filter }), Is.EqualTo(expected));
    }
}
