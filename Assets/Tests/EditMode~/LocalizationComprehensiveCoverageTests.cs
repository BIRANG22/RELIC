using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public sealed class LocalizationComprehensiveCoverageTests
{
    private static readonly Regex LocalizationTokenRegex = new Regex(
        @"\{[^{}]+\}",
        RegexOptions.Compiled);

    private static readonly HashSet<string> StructuredSentencePrefixExceptions =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "skill.sentence.special.missing_hp",
            "skill.sentence.special.armor",
        };

    private static readonly string[] RequiredRuntimeKeys =
    {
        "battle.action.move", "battle.action.damage", "battle.action.push", "battle.action.pull",
        "battle.action.grudge", "battle.action.erosion", "battle.action.spawn_spider_egg",
        "battle.action.spawn_web", "battle.action.veil", "common.character", "common.info",
        "common.memory", "event.memory.none_upgradable", "event.memory.remove_acquired_failed",
        "event.memory.remove_failed", "event.memory.remove_failed_unavailable",
        "event.memory.select_awaken", "event.memory.select_upgrade",
        "event.memory.select_upgrade_required", "event.memory.selection_panel_invalid",
        "event.memory.selection_panel_missing", "event.memory.upgrade_unavailable",
        "event.relic.none_available", "event.relic.none_equipped", "event.relic.select_exchange",
        "event.relic.select_remove", "event.relic.selection_panel_invalid",
        "event.relic.selection_panel_unavailable", "event.result.end", "event.result.failed",
        "event.result.shop_opened", "event.reward.none_to_confirm", "lobby.quest.first_expedition",
        "lobby.quest.setup", "system.continue.missing", "ui.exploration.record",
        "warning.timeline.combined_action_limit", "event.dice.roll", "event.dice.confirm",
        "event.dice.success", "event.dice.failure", "event.dice.judgement_success",
        "event.dice.judgement_failure",
        "lobby.world_object.workbench", "lobby.world_object.statue",
        "lobby.world_object.stela", "lobby.world_object.researcher_elric",
        "lobby.world_object.storage", "lobby.character.hilt", "lobby.character.kaya",
        "lobby.character.haze", "lobby.character.ines",
    };

    [Test]
    public void LocalizationWorkbook_ContainsRequiredRuntimeKeysWithEnglishText()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = rows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .GroupBy(row => row[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (string key in RequiredRuntimeKeys)
        {
            Assert.That(byKey, Does.ContainKey(key), key);
            Assert.That(byKey[key].Count, Is.GreaterThan(3), key);
            Assert.That(byKey[key][3], Is.Not.Empty, key);
        }
    }

    [Test]
    public void LocalizationWorkbook_KoreanEntriesHaveEnglishTranslationsWithMatchingTokens()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");

        foreach (IReadOnlyList<string> row in rows.Skip(1))
        {
            if (row.Count <= 3 || string.IsNullOrWhiteSpace(row[0]) || string.IsNullOrWhiteSpace(row[2]))
                continue;

            string key = row[0];
            string korean = row[2];
            string english = row[3];

            Assert.That(english, Is.Not.Empty, $"{key}:en");
            if (StructuredSentencePrefixExceptions.Contains(key))
                continue;

            string[] koreanTokens = LocalizationTokenRegex.Matches(korean)
                .Select(match => match.Value)
                .Where(token => !key.StartsWith("skill.sentence.", StringComparison.Ordinal) || token != "{1}")
                .ToArray();
            CollectionAssert.AreEquivalent(
                koreanTokens,
                LocalizationTokenRegex.Matches(english).Select(match => match.Value).ToArray(),
                $"{key}의 영어 번역은 한국어 원문의 치환 토큰을 동일하게 보존해야 합니다.");
        }
    }

    [Test]
    public void BattleRelicExtractionButton_HasEnglishTranslation()
    {
        const string extractionButtonKey =
            "ui.battle.canvas.relic_panel.extractbutton.text_tmp.b5d536cd";
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        IReadOnlyList<string> row = rows.Skip(1).Single(candidate =>
            candidate.Count > 0 && candidate[0] == extractionButtonKey);

        Assert.That(row.Count, Is.GreaterThan(3));
        Assert.That(row[3], Is.EqualTo("Extract"));
    }

    [Test]
    public void RuntimeSkillSentenceCatalog_CoversEverySentenceKeyGeneratedByCurrentSkills()
    {
        IReadOnlyList<IReadOnlyList<string>> gameRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/GameData.xlsx", "SkillMaster");
        HashSet<string> catalogKeys = LocalizationRuntimeSourceCatalog.SkillSentenceEntries
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string key in CollectGeneratedSkillSentenceKeys(gameRows))
            Assert.That(catalogKeys, Does.Contain(key), key);
    }

    [Test]
    public void LocalizationWorkbook_RuntimeSkillSentenceKeysHaveKoreanAndEnglishTemplates()
    {
        IReadOnlyList<IReadOnlyList<string>> localizationRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = localizationRows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .ToDictionary(row => row[0], row => row, StringComparer.Ordinal);

        foreach (LocalizationWorkbookEntry entry in LocalizationRuntimeSourceCatalog.SkillSentenceEntries)
        {
            Assert.That(byKey, Does.ContainKey(entry.Key), entry.Key);
            Assert.That(byKey[entry.Key].Count, Is.GreaterThan(3), entry.Key);
            Assert.That(byKey[entry.Key][2], Is.Not.Empty, $"{entry.Key}:ko");
            Assert.That(byKey[entry.Key][3], Is.Not.Empty, $"{entry.Key}:en");
        }
    }

    [Test]
    public void ActiveCoreSkills_HaveKoreanAndEnglishNameAndDetails()
    {
        IReadOnlyList<IReadOnlyList<string>> gameRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/GameData.xlsx", "SkillMaster");
        IReadOnlyList<IReadOnlyList<string>> localizationRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = localizationRows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .GroupBy(row => row[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (IReadOnlyList<string> row in gameRows.Skip(2))
        {
            if (row.Count <= 14 || string.IsNullOrWhiteSpace(row[0]))
                continue;

            string id = row[0].Trim().ToLowerInvariant();
            foreach (string field in new[] { "name", "details" })
            {
                string key = $"data.skill_master.{id}.{field}";
                Assert.That(byKey, Does.ContainKey(key), key);
                Assert.That(byKey[key].Count, Is.GreaterThan(3), key);
                Assert.That(byKey[key][2], Is.Not.Empty, $"{key}:ko");
                Assert.That(byKey[key][3], Is.Not.Empty, $"{key}:en");
            }
        }
    }

    [Test]
    public void TokenOnlySkillDetails_AreCollectedButPlaceholdersAreNot()
    {
        Assert.That(LocalizationProjectScanner.ShouldCollectGameDataLocalizationValue(
            "SkillMaster", "Details", "{E_Strike} {ValueRate1}"), Is.True);
        Assert.That(LocalizationProjectScanner.ShouldCollectGameDataLocalizationValue(
            "SkillMaster", "Details", "-"), Is.False);
        Assert.That(LocalizationProjectScanner.ShouldCollectGameDataLocalizationValue(
            "SkillMaster", "EffectIds", "E_Strike"), Is.False);
    }

    [Test]
    public void BattleFirstTutorial_UsesKeysOnlyAndWorkbookContainsEveryKey()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Tutorial/BattleFirstTutorialController.cs");
        Assert.That(source, Does.Not.Contain("currentFallbackText"));
        Assert.That(LocalizationProjectScanner.RemoveComments(source), Does.Not.Match("[가-힣]"));
        Assert.That(source, Does.Contain("GameLocalization.Get(currentLocalizationKey)"));

        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = rows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .ToDictionary(row => row[0], row => row, StringComparer.Ordinal);
        foreach (string key in LocalizationRuntimeSourceCatalog.BattleFirstTutorialEntries.Select(entry => entry.Key))
        {
            Assert.That(byKey, Does.ContainKey(key), key);
            Assert.That(byKey[key].Count, Is.GreaterThan(3), key);
            Assert.That(byKey[key][2], Is.Not.Empty, $"{key}:ko");
            Assert.That(byKey[key][3], Is.Not.Empty, $"{key}:en");
        }
    }

    [Test]
    public void RuntimeGameData_DoesNotContainMisspelledEffectIds()
    {
        string source = File.ReadAllText("Assets/Resources/Data/GameDataRuntime.csv");
        Assert.That(source, Does.Not.Contain("E_Chargeana"));
        Assert.That(source, Does.Not.Contain("E_RestoreManaPerHitTarge,"));
        Assert.That(source, Does.Contain("E_RestoreManaPerHitTarget"));
    }

    [Test]
    public void RecordCatalogData_HasLocalizationKeysForEveryPlayerFacingField()
    {
        IReadOnlyList<IReadOnlyList<string>> localizationRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        string[] localizationKeys = localizationRows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .Select(row => row[0])
            .ToArray();
        string[] duplicateLocalizationKeys = localizationKeys
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        Assert.That(duplicateLocalizationKeys, Is.Empty,
            "Localization.xlsx에 중복 키가 있으면 기록서가 어떤 번역을 사용할지 결정적이지 않습니다.");

        HashSet<string> keys = localizationKeys.ToHashSet(StringComparer.Ordinal);
        var generatedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (string sheet in new[] { "SkillMaster", "Rune", "Relic" })
        {
            IReadOnlyList<IReadOnlyList<string>> rows =
                LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/GameData.xlsx", sheet);
            IReadOnlyList<string> headers = rows[0];

            foreach (IReadOnlyList<string> row in rows.Skip(2))
            {
                if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0]))
                    continue;

                for (int column = 1; column < headers.Count && column < row.Count; column++)
                {
                    if (!LocalizationProjectScanner.IsPlayerFacingGameDataColumn(sheet, headers[column]) ||
                        !LocalizationProjectScanner.IsLocalizableKoreanText(row[column]))
                    {
                        continue;
                    }

                    string key = LocalizationProjectScanner.BuildGameDataKey(sheet, row[0], headers[column]);
                    Assert.That(generatedKeys.Add(key), Is.True,
                        $"GameData의 안정 ID/필드가 충돌합니다: {sheet}/{row[0]}/{headers[column]} -> {key}");
                    Assert.That(keys, Does.Contain(key), $"{sheet}/{row[0]}/{headers[column]}");
                }
            }
        }
    }

    [Test]
    public void RuntimeGameData_ContainsLatestManaRiseValueRates()
    {
        string[] matchingRows = File.ReadAllLines("Assets/Resources/Data/GameDataRuntime.csv")
            .Where(line => line.StartsWith("S_Unique_05,", StringComparison.Ordinal))
            .ToArray();
        Assert.That(matchingRows, Has.Length.EqualTo(1));

        string[] columns = matchingRows[0].Split(',');
        Assert.That(columns[9], Is.EqualTo("E_Grit;E_Charge"));
        Assert.That(columns[10], Is.EqualTo("1;2"));
        Assert.That(columns[15], Does.Contain("{ValueRate2}"));
    }

    [Test]
    public void EventChoiceCanonicalKeys_CoverEveryLocalizedChoiceField()
    {
        IReadOnlyList<IReadOnlyList<string>> gameRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/GameData.xlsx", "Event");
        IReadOnlyList<IReadOnlyList<string>> localizationRows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        HashSet<string> keys = localizationRows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .Select(row => row[0])
            .ToHashSet(StringComparer.Ordinal);

        IReadOnlyList<string> headers = gameRows[0];
        int orderColumn = FindHeader(headers, "ChoiceOrder", "선택지 순서");
        for (int rowIndex = 1; rowIndex < gameRows.Count; rowIndex++)
        {
            IReadOnlyList<string> row = gameRows[rowIndex];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0]))
                continue;

            int order = orderColumn >= 0 && orderColumn < row.Count && int.TryParse(row[orderColumn], out int parsed)
                ? parsed
                : 0;
            for (int column = 1; column < headers.Count && column < row.Count; column++)
            {
                string header = headers[column];
                if (string.IsNullOrWhiteSpace(row[column]) ||
                    (!header.Contains("선택지", StringComparison.Ordinal) &&
                     !header.Contains("선택 불가", StringComparison.Ordinal) &&
                     !header.Contains("실패 결과", StringComparison.Ordinal)))
                    continue;

                string key = LocalizationProjectScanner.BuildEventChoiceKey(row[0], order, header);
                Assert.That(keys, Does.Contain(key), $"Event row {rowIndex + 1}: {header}");
            }
        }
    }

    [Test]
    public void IdBasedPresenters_UseLocaleTableReadySignal()
    {
        string coordinator = File.ReadAllText("Assets/Project/Scripts/Core/Localization/LocalizedTMPText.cs");
        Assert.That(coordinator, Does.Contain("LocaleTableReady"));

        foreach (string path in new[]
        {
            "Assets/Project/Scripts/BattleRewardEquipPanelUI.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardPanelUI.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventChoiceSlotUI.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventDiceRollPresenter.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventEquippedRelicSelectionPanelUI.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/RuneSettingPanel.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/Setting.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/SkillSettingPanel.cs",
            "Assets/Project/Scripts/LobbyEquipPanelUI.cs",
            "Assets/Project/Scripts/RecordPanelUI.cs",
        })
        {
            Assert.That(File.ReadAllText(path), Does.Contain("LocalizationRuntimeRefreshCoordinator.LocaleTableReady"), path);
        }
    }

    [Test]
    public void PlayerFacingCode_DoesNotUseLegacyRelicRarityKeys()
    {
        foreach (string path in Directory.GetFiles("Assets/Project/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("\"relic.rarity."), path);
        }
    }

    [Test]
    public void DiceRoll_ChangesStateBeforeRefreshingConfirmLabel()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventDiceRollPresenter.cs");
        int routine = source.IndexOf("private IEnumerator RollInteractiveRoutine()", StringComparison.Ordinal);
        int ready = source.IndexOf("interactiveState = InteractiveState.ReadyToConfirm;", routine, StringComparison.Ordinal);
        int refresh = source.IndexOf("RefreshRollButtonLabel();", routine, StringComparison.Ordinal);

        Assert.That(routine, Is.GreaterThanOrEqualTo(0));
        Assert.That(ready, Is.GreaterThan(routine));
        Assert.That(refresh, Is.GreaterThan(ready));
    }

    [Test]
    public void LocaleCoordinator_NotifiesIdPresentersAfterFinalStaticRefresh()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Core/Localization/LocalizedTMPText.cs");
        int secondYield = source.IndexOf("await Task.Yield();", StringComparison.Ordinal);
        int finalRefresh = source.IndexOf("RefreshAllNow(locale);", secondYield, StringComparison.Ordinal);
        int notify = source.IndexOf("NotifyLocaleTableReady(locale);", secondYield, StringComparison.Ordinal);

        Assert.That(secondYield, Is.GreaterThanOrEqualTo(0));
        Assert.That(finalRefresh, Is.GreaterThan(secondYield));
        Assert.That(notify, Is.GreaterThan(finalRefresh));
    }

    [Test]
    public void RelicShop_RefreshesOpenIdBackedDescriptionsAfterLocaleChange()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs");

        Assert.That(source, Does.Contain("LocalizationRuntimeRefreshCoordinator.LocaleTableReady"));
        Assert.That(source, Does.Contain("ProtectDynamicDescriptionTexts"));
    }

    private static IEnumerable<string> CollectGeneratedSkillSentenceKeys(
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        IReadOnlyList<string> headers = rows[1];
        int detailsColumn = FindHeader(headers, "Details");
        int countRateColumn = FindHeader(headers, "CountRate");
        var effectRegex = new Regex(@"\{(?<id>E_[A-Za-z0-9_]+)\}\s+\{ValueRate(?<index>\d+)\}");

        foreach (IReadOnlyList<string> row in rows.Skip(2))
        {
            string details = detailsColumn < row.Count ? row[detailsColumn] : string.Empty;
            string[] counts = countRateColumn < row.Count
                ? row[countRateColumn].Split(';')
                : Array.Empty<string>();
            MatchCollection matches = effectRegex.Matches(details ?? string.Empty);
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                string effectId = matches[matchIndex].Groups["id"].Value;
                int valueIndex = int.Parse(matches[matchIndex].Groups["index"].Value) - 1;
                string count = valueIndex >= 0 && valueIndex < counts.Length
                    ? counts[valueIndex]
                    : counts.FirstOrDefault() ?? "1";
                bool multiple = int.TryParse(Regex.Replace(count, "<[^>]+>", string.Empty), out int parsed) &&
                                parsed > 1;
                string action = ResolveSentenceAction(effectId);
                string unit = effectId == "E_Knockback" || effectId == "E_Grab" ? "tile" : "amount";
                string position = matchIndex == matches.Count - 1 ? "final" : "linked";
                yield return $"skill.sentence.{action}.{unit}.{(multiple ? "multiple" : "single")}.{position}";
            }
        }
    }

    private static string ResolveSentenceAction(string effectId)
    {
        if (effectId == "E_Strike" || effectId == "E_Pierce" || effectId == "E_MissingHPStrike")
            return "deal";
        if (effectId == "E_Knockback" || effectId == "E_Grab")
            return "apply";
        if (effectId == "E_Heal" || effectId == "E_Cure")
            return "restore";
        if (new[] { "E_Boost", "E_Grit", "E_Charge", "E_Smite", "E_Swift", "E_Lifesteal", "E_Karma", "E_Mana" }
            .Contains(effectId))
            return "gain";
        return "grant";
    }

    private static int FindHeader(IReadOnlyList<string> headers, params string[] names)
    {
        for (int index = 0; index < headers.Count; index++)
        {
            if (names.Any(name => string.Equals(headers[index], name, StringComparison.OrdinalIgnoreCase)))
                return index;
        }

        return -1;
    }
}
