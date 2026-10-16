using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    };

    [Test]
    public void LocalizationWorkbook_ContainsBattlePresentationKeysForEveryLocale()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = rows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .GroupBy(row => row[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

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
        })
        {
            Assert.That(combined, Does.Not.Contain(forbidden), forbidden);
        }
    }
}
