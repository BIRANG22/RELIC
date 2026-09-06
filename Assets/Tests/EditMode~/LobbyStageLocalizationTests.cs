using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

public class LobbyStageLocalizationTests
{
    private const string WorkbookPath = "Assets/ExcelSource/Localization.xlsx";
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string SharedDataPath = "Assets/Language/Text Shared Data.asset";
    private const string EnglishTablePath = "Assets/Language/Text_en.asset";

    private static readonly StageTextCase[] StageTexts =
    {
        new(
            "1433460218",
            "lobby.stage_select.area_3",
            "제 3구역 : 붉은 폐허",
            "Area 3: Red Ruins"),
        new(
            "139910395",
            "lobby.stage_select.area_7",
            "제 7구역 : 잿빛 수로",
            "Area 7: Ashen Waterway"),
        new(
            "829871923",
            "lobby.stage_select.area_12",
            "제 12구역 : 침식 동굴",
            "Area 12: Erosion Cave"),
    };

    [Test]
    public void LocalizationWorkbook_ContainsLobbyStageSelectRows()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");

        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, "Korean(ko)");
        int englishIndex = FindHeader(headers, "English(en)");

        Dictionary<string, IReadOnlyList<string>> byKey = rows
            .Skip(1)
            .Where(row => keyIndex < row.Count && !string.IsNullOrWhiteSpace(row[keyIndex]))
            .ToDictionary(row => row[keyIndex], row => row);

        foreach (StageTextCase stageText in StageTexts)
        {
            Assert.That(byKey.ContainsKey(stageText.Key), Is.True, stageText.Key);
            IReadOnlyList<string> row = byKey[stageText.Key];
            Assert.That(GetValue(row, koreanIndex), Is.EqualTo(stageText.Korean));
            Assert.That(GetValue(row, englishIndex), Is.EqualTo(stageText.English));
        }
    }

    [Test]
    public void TextStringTables_ContainLobbyStageSelectEntries()
    {
        string sharedData = File.ReadAllText(SharedDataPath);
        string englishTable = File.ReadAllText(EnglishTablePath);

        foreach (StageTextCase stageText in StageTexts)
        {
            string keyPattern = @"m_Id: (?<id>\d+)\s+m_Key: " +
                                Regex.Escape(stageText.Key);
            Match keyMatch = Regex.Match(sharedData, keyPattern);

            Assert.That(keyMatch.Success, Is.True, stageText.Key);
            string id = keyMatch.Groups["id"].Value;

            Assert.That(
                Regex.IsMatch(
                    englishTable,
                    @"m_Id: " + Regex.Escape(id) +
                    @"\s+m_Localized: " + Regex.Escape(stageText.English)),
                Is.True,
                stageText.Key);
        }
    }

    [Test]
    public void LobbyScene_StageSelectTextsUseLocalizeStringEvents()
    {
        string sceneYaml = File.ReadAllText(LobbyScenePath);

        Assert.That(sceneYaml, Does.Contain("applyStageDisplayNamesToTexts: 0"));

        foreach (StageTextCase stageText in StageTexts)
        {
            string localizerPattern =
                @"MonoBehaviour:\s+.*?m_GameObject: \{fileID: " +
                Regex.Escape(stageText.GameObjectFileId) +
                @"\}.*?m_Script: \{fileID: 11500000, guid: 56eb0353ae6e5124bb35b17aff880f16, type: 3\}" +
                @".*?m_Key: " +
                Regex.Escape(stageText.Key);

            Assert.That(
                Regex.IsMatch(sceneYaml, localizerPattern, RegexOptions.Singleline),
                Is.True,
                stageText.Key);
        }
    }

    private static int FindHeader(IReadOnlyList<string> headers, string name)
    {
        for (int index = 0; index < headers.Count; index++)
        {
            if (headers[index] == name)
                return index;
        }

        Assert.Fail($"Missing header: {name}");
        return -1;
    }

    private static string GetValue(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] : string.Empty;

    private readonly struct StageTextCase
    {
        public StageTextCase(
            string gameObjectFileId,
            string key,
            string korean,
            string english)
        {
            GameObjectFileId = gameObjectFileId;
            Key = key;
            Korean = korean;
            English = english;
        }

        public string GameObjectFileId { get; }
        public string Key { get; }
        public string Korean { get; }
        public string English { get; }
    }
}
