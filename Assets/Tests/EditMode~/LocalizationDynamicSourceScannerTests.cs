using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class LocalizationDynamicSourceScannerTests
{
    [Test]
    public void FindUnityYamlTmpTexts_DecodesWrappedQuotedTextAsOneSource()
    {
        const string yaml = @"--- !u!114 &1
MonoBehaviour:
  m_text: ""[\uCE68\uC2DD\uC774 \uC9D9\uC5B4\uC9C8\uC218\uB85D,
    \uB354 \uB9CE\uC740 \uC794\uC7AC\uAC00 \uB0A8\uB294\uB2E4]""
  m_isRightToLeft: 0";

        IReadOnlyList<string> values = LocalizationProjectScanner.FindUnityYamlTmpTexts(yaml);

        Assert.That(values, Is.EqualTo(new[] { "[침식이 짙어질수록, 더 많은 잔재가 남는다]" }));
    }

    [Test]
    public void FindUnityYamlTmpTexts_NormalizesXmlInvalidVerticalTabToLineFeed()
    {
        const string yaml = @"--- !u!114 &1
MonoBehaviour:
  m_text: ""첫 번째 줄\v두 번째 줄""
  m_isRightToLeft: 0";

        IReadOnlyList<string> values = LocalizationProjectScanner.FindUnityYamlTmpTexts(yaml);

        Assert.That(values, Is.EqualTo(new[] { "첫 번째 줄\n두 번째 줄" }));
        Assert.That(values[0], Does.Not.Contain("\v"));
    }

    [Test]
    public void FindUnityYamlTmpTexts_EmptyTextDoesNotConsumeFollowingYamlDocument()
    {
        const string yaml = @"--- !u!114 &1
MonoBehaviour:
  m_text: 
--- !u!1 &2
GameObject:
  m_Name: 다음 오브젝트
--- !u!114 &3
MonoBehaviour:
  m_text: ""정상 문구""
  m_isRightToLeft: 0";

        IReadOnlyList<string> values = LocalizationProjectScanner.FindUnityYamlTmpTexts(yaml);

        Assert.That(values, Is.EqualTo(new[] { "정상 문구" }));
        Assert.That(values, Has.None.Contains("GameObject"));
    }

    [Test]
    public void MergeNewEntries_RejectsUnknownXmlControlWithKeyAndCodePoint()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xlsx");
        string backupPath = tempPath + ".localization-manager.backup";
        File.Copy("Assets/ExcelSource/Localization.xlsx", tempPath);
        try
        {
            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
                LocalizationWorkbookWriter.MergeNewEntries(
                    tempPath,
                    new[] { new LocalizationWorkbookEntry("test.invalid_control", "테스트\0문자") }));

            Assert.That(exception.Message, Does.Contain("test.invalid_control"));
            Assert.That(exception.Message, Does.Contain("U+0000"));
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            if (File.Exists(backupPath)) File.Delete(backupPath);
        }
    }

    [Test]
    public void FindUnityYamlPlayerTextFields_CollectsSerializedDisplayNames()
    {
        const string yaml = "    displayName: \"\\uCE68\\uC2DD\\uB3C4\"\n    internalId: hidden";

        IReadOnlyList<string> values = LocalizationProjectScanner.FindUnityYamlPlayerTextFields(yaml);

        Assert.That(values, Is.EqualTo(new[] { "침식도" }));
    }

    [Test]
    public void DynamicDisplaySinkLiterals_AreDiscoveredButUnrelatedLiteralsAreNot()
    {
        const string source = @"
            ShowBattleWarning(""선택된 캐릭터가 없습니다."");
            BattleMapIntroText.ShowMessage(""전투 시작"");
            Debug.Log(""내부 진단 문구"");";

        IReadOnlyList<string> values = LocalizationProjectScanner.FindDynamicDisplayLiterals(source);

        Assert.That(values, Does.Contain("선택된 캐릭터가 없습니다."));
        Assert.That(values, Does.Contain("전투 시작"));
        Assert.That(values, Does.Not.Contain("내부 진단 문구"));
    }

    [Test]
    public void FindExplicitSources_CollectsKeyedDynamicKoreanSources()
    {
        const string source = @"
            var turn = GameLocalization.FormatWithFallback(""battle.turn_format"", ""턴 {0:D2}"", turnNumber);
            var intro = GameLocalization.Get(""battle.map_intro"", ""제1구역 폐허"");
            BattleWarningUI.ShowMessage(""검사하면 안 되는 임의 문구"");";

        IReadOnlyList<LocalizationSourceEntry> entries =
            LocalizationProjectScanner.FindExplicitLocalizationSources(source);

        Assert.That(entries.Count, Is.EqualTo(2));
        LocalizationSourceEntry turn = entries.Single(entry => entry.Key == "battle.turn_format");
        LocalizationSourceEntry intro = entries.Single(entry => entry.Key == "battle.map_intro");
        Assert.That(turn.Korean, Is.EqualTo("턴 {0:D2}"));
        Assert.That(intro.Korean, Is.EqualTo("제1구역 폐허"));
    }

    [Test]
    public void ProjectDynamicOutputs_AreDiscoverableByExplicitSourceScanner()
    {
        string[] paths =
        {
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs",
            "Assets/Project/Scripts/UI/BattleMapIntroText.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomController.cs",
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs",
            "Assets/Project/Scripts/Core/Managers/UIManager.cs",
            "Assets/Project/Scripts/ErosionDifficultyCatalogUI.cs",
        };
        HashSet<string> keys = paths
            .SelectMany(path => LocalizationProjectScanner.FindExplicitLocalizationSources(File.ReadAllText(path)))
            .Select(entry => entry.Key)
            .ToHashSet();

        Assert.That(keys, Does.Contain("battle.back2.turn_format"));
        Assert.That(keys, Does.Contain("battle.map_intro"));
        Assert.That(keys, Does.Contain("battle.intro.enter_battle_area"));
        Assert.That(keys, Does.Contain("battle.rest.no_upgradable_equipped_memory"));
        Assert.That(keys, Does.Contain("common.rarity.rare"));
        Assert.That(keys, Does.Contain("ui.check.slogan"));
        Assert.That(keys, Does.Contain("lobby.erosion.reward_bonus_format"));
    }
}
