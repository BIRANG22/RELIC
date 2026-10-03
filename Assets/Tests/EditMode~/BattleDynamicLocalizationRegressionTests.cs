using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class BattleDynamicLocalizationRegressionTests
{
    private static readonly string[] SkillTypeKeys =
    {
        "common.move",
        "common.passive",
        "common.attack",
        "common.buff",
        "common.debuff",
    };

    [Test]
    public void HudSlot_LocalizesCharacterNameAndRefreshesAfterLocaleTableReady()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/HUDSlot.cs");

        Assert.That(source, Does.Contain("GameDataLocalization.CharacterName(data)"));
        Assert.That(source, Does.Contain("LocalizationRuntimeRefreshCoordinator.LocaleTableReady"));
    }

    [Test]
    public void BattleSkillTooltip_UsesLocalizedTypeKeysInsteadOfKoreanLiterals()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/BattleCharacterPanelUI.cs");
        int methodStart = source.IndexOf("GetSkillTooltipTypeDisplayName", StringComparison.Ordinal);
        int methodEnd = source.IndexOf("private void ShowSkillInfo", methodStart, StringComparison.Ordinal);
        string method = source.Substring(methodStart, methodEnd - methodStart);

        foreach (string key in SkillTypeKeys)
            Assert.That(method, Does.Contain($"GameLocalization.Get(\"{key}\")"), key);

        foreach (string koreanLiteral in new[] { "\"이동\"", "\"패시브\"", "\"공격\"", "\"버프\"", "\"디버프\"" })
            Assert.That(method, Does.Not.Contain(koreanLiteral), koreanLiteral);
    }

    [Test]
    public void ShopGoodsPlate_UsesDynamicLocalizedLabelsAndRefreshesAfterLocaleTableReady()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/GoodsIconItem.cs");

        Assert.That(source, Does.Contain("GameLocalization.Get(\"common.memory\")"));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"common.relic\")"));
        Assert.That(source, Does.Contain("ProtectDynamicLocalizedTexts"));
        Assert.That(source, Does.Contain("LocalizationRuntimeRefreshCoordinator.LocaleTableReady"));
    }

    [Test]
    public void LocalizationWorkbook_ContainsCompleteBattleSkillTypeLabels()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet("Assets/ExcelSource/Localization.xlsx", "Text");
        Dictionary<string, IReadOnlyList<string>> byKey = rows.Skip(1)
            .Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .GroupBy(row => row[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (string key in SkillTypeKeys)
        {
            Assert.That(byKey, Does.ContainKey(key), key);
            Assert.That(byKey[key].Count, Is.GreaterThan(6), key);
            for (int localeColumn = 2; localeColumn <= 6; localeColumn++)
                Assert.That(byKey[key][localeColumn], Is.Not.Empty, $"{key}: column {localeColumn}");
        }
    }
}
