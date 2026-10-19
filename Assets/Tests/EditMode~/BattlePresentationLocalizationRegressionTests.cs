using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class BattlePresentationLocalizationRegressionTests
{
    private static readonly string[] RequiredKeys =
    {
        "battle.effect_info.health_recovery",
        "battle.effect_info.mana_recovery",
        "battle.effect_info.karma_recovery",
        "battle.effect_info.party_karma_recovery",
        "battle.effect_info.collision",
        "battle.effect_info.turn_start_format",
        "battle.reward.acquire_count_format",
        "event.result.red_dustium_acquired_format",
        "event.result.relic_acquired_format",
        "battle.reward.red_dustium",
        "battle.reward.relic_acquired",
        "battle.reward.skill_panel_title",
        "battle.reward.skill_acquired",
        "event.result.memory_acquired_format",
    };

    [Test]
    public void LocalizationWorkbook_ContainsBattlePresentationKeysForEveryLocale()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = rows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .ToDictionary(row => row[0], row => row, StringComparer.Ordinal);

        foreach (string key in RequiredKeys)
        {
            Assert.That(byKey, Does.ContainKey(key), key);
            Assert.That(byKey[key].Count, Is.GreaterThan(6), key);
            for (int localeColumn = 2; localeColumn <= 6; localeColumn++)
                Assert.That(byKey[key][localeColumn], Is.Not.Empty, $"{key}: column {localeColumn}");
        }
    }

    [Test]
    public void LocalizationWorkbook_HasNoMissingEnglishWhenKoreanSourceExists()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");

        foreach (IReadOnlyList<string> row in rows.Skip(1))
        {
            if (row.Count <= 3 || string.IsNullOrWhiteSpace(row[2]))
                continue;

            Assert.That(row[3], Is.Not.Empty, row[0]);
        }
    }

    [Test]
    public void TurnHeading_UsesCompactLocalizedFormatAcrossBattlePresentation()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        IReadOnlyList<string> row = rows.Skip(1)
            .Single(candidate => candidate[0] == "battle.effect_info.turn_start_format");

        Assert.That(row.Skip(2).Take(5), Is.EqualTo(new[]
        {
            "{0}턴",
            "Turn {0}",
            "第{0}回合",
            "{0}ターン",
            "Turno {0}",
        }));

        string introSource = File.ReadAllText("Assets/Project/Scripts/UI/BattleTurnIntroText.cs");
        string executorSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs");

        Assert.That(introSource, Does.Contain("battle.effect_info.turn_start_format"));
        Assert.That(introSource, Does.Not.Contain("LocalizationKeys.Battle.Progress"));
        Assert.That(executorSource, Does.Contain("BattleTurnIntroText.ShowTurnStart(requestedTurn)"));
    }

    [Test]
    public void BattlePresentationSources_DoNotWriteKnownKoreanDisplayLiterals()
    {
        string combined = string.Join("\n", new[]
        {
            File.ReadAllText("Assets/Project/Scripts/BattleEffectInfoFeed.cs"),
            File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardPanelUI.cs"),
            File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardSlotUI.cs"),
            File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs"),
            File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventChoiceExecutionService.cs"),
            File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomController.cs"),
        });

        foreach (string forbidden in new[]
        {
            "feed.Spawn(\"체력 회복\"",
            "feed.Spawn(\"전환\"",
            "feed.Spawn(\"이동\"",
            "feed.Spawn(\"충돌\"",
            "label.text = \"진행\"",
            "$\"레드 더스티움 {safeAmount} 획득\"",
            "$\"유물 획득: {relicName}\"",
            "$\"레드 더스티움 {amount} 획득\"",
            "\"레드 더스티움 {0} 획득\"",
            "\"유물 획득: {0}\"",
            "$\"레드 더스티움\\n{Mathf.Max(0, reward.Amount)}\"",
            "displayText = \"유물 획득\"",
            "displayText = \"기억 발현\"",
            "$\"기억 획득: {GameDataLocalization.SkillName(skill)}\"",
        })
        {
            Assert.That(combined, Does.Not.Contain(forbidden), forbidden);
        }
    }

    [TestCase("data.skill_master.s_core_07.details", "{E_Strike} {ValueRate1} / On miss, lose {ValueRate2} HP")]
    [TestCase("data.skill_master.s_core_10.details", "{E_Strike} {ValueRate1} / On kill, permanently increase damage by {ValueRate2}")]
    public void LocalizationWorkbook_ContainsEnglishRewardSkillDescriptions(string key, string expectedEnglish)
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        IReadOnlyList<string> row = rows.Skip(1).Single(candidate => candidate[0] == key);

        Assert.That(row[3], Is.EqualTo(expectedEnglish));
    }

    [TestCase("skill.sentence.deal.amount.multiple.final", "{0}{1} {2}만큼 {3}회 준다")]
    [TestCase("skill.sentence.deal.amount.single.final", "{0}{1} {2}만큼 준다")]
    [TestCase("skill.sentence.deal.amount.single.linked", "{0}{1} {2}만큼 주고")]
    public void StrikeSentence_DoesNotRepeatDamageNoun(string key, string expectedKorean)
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        IReadOnlyList<string> row = rows.Skip(1).Single(candidate => candidate[0] == key);

        Assert.That(row[2], Is.EqualTo(expectedKorean));

        string shared = File.ReadAllText("Assets/Language/Text Shared Data.asset");
        string id = row[1];
        Assert.That(ReadLocaleValue("Assets/Language/Text_ko.asset", id), Is.EqualTo(expectedKorean));

        string scannerSource = File.ReadAllText("Assets/Editor/LocalizationProjectScanner.cs");
        Assert.That(scannerSource, Does.Contain($"Entry(\"{key}\", \"{expectedKorean}\")"));
        Assert.That(shared, Does.Contain($"m_Key: {key}"));
    }

    [Test]
    public void RewardLocalization_WorkbookAndRuntimeTablesStaySynchronized()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = rows.Skip(1)
            .Where(row => row.Count > 1 && !string.IsNullOrWhiteSpace(row[0]))
            .ToDictionary(row => row[0], row => row, StringComparer.Ordinal);
        string shared = File.ReadAllText("Assets/Language/Text Shared Data.asset");
        (string Path, int Column)[] locales =
        {
            ("Assets/Language/Text_ko.asset", 2),
            ("Assets/Language/Text_en.asset", 3),
            ("Assets/Language/Text_zh-Hans.asset", 4),
            ("Assets/Language/Text_ja.asset", 5),
            ("Assets/Language/Text_es.asset", 6),
        };

        string[] synchronizedKeys = new[] { "battle.effect_info.turn_start_format" }
            .Concat(RequiredKeys.Skip(RequiredKeys.Length - 3))
            .Concat(new[]
            {
                "data.skill_master.s_core_07.details",
                "data.skill_master.s_core_10.details",
            })
            .ToArray();

        foreach (string key in synchronizedKeys)
        {
            IReadOnlyList<string> row = byKey[key];
            string id = row[1];
            string sharedPattern = $@"- m_Id: {Regex.Escape(id)}\r?\n\s+m_Key: {Regex.Escape(key)}(?:\r?\n|$)";
            Assert.That(Regex.Matches(shared, sharedPattern).Count, Is.EqualTo(1), key);

            foreach ((string path, int column) in locales)
                Assert.That(ReadLocaleValue(path, id), Is.EqualTo(row[column]), $"{path}: {key}");
        }
    }

    [Test]
    public void BattleSkillRewardPanel_HasSerializedTitleAndLocaleRefresh()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");
        string source = File.ReadAllText("Assets/Project/Scripts/BattleRewardSkillPanelUI.cs");

        Assert.That(scene, Does.Contain("panelTitleText: {fileID: 416538621}"));
        Assert.That(source, Does.Contain("LocalizationRuntimeRefreshCoordinator.LocaleTableReady += OnLocaleTableReady"));
        Assert.That(source, Does.Contain("RefreshLocalizedText();"));
    }

    private static string ReadLocaleValue(string path, string id)
    {
        string source = File.ReadAllText(path);
        Match match = Regex.Match(
            source,
            $@"- m_Id: {Regex.Escape(id)}\r?\n\s+m_Localized: (?<value>[^\r\n]*)");
        Assert.That(match.Success, Is.True, $"{path}: {id}");

        string value = match.Groups["value"].Value.Trim();
        if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
            return value.Substring(1, value.Length - 2).Replace("''", "'");
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            return Regex.Unescape(value.Substring(1, value.Length - 2));
        return value;
    }
}
