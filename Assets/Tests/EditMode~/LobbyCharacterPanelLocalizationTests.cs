using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

public class LobbyCharacterPanelLocalizationTests
{
    private const string WorkbookPath = "Assets/ExcelSource/Localization.xlsx";
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string SharedDataPath = "Assets/Language/Text Shared Data.asset";
    private const string EnglishTablePath = "Assets/Language/Text_en.asset";
    private const string ChineseTablePath = "Assets/Language/Text_zh-Hans.asset";
    private const string JapaneseTablePath = "Assets/Language/Text_ja.asset";
    private const string KoreanTablePath = "Assets/Language/Text_ko.asset";
    private const string SpanishTablePath = "Assets/Language/Text_es.asset";
    private const string RuneSettingPanelPath =
        "Assets/Project/Scripts/Gameplay/Scene/Lobby/RuneSettingPanel.cs";
    private const string CharacterStatTooltipTargetPath =
        "Assets/Project/Scripts/CharacterStatTooltipTarget.cs";

    private static readonly LocalizationEntryCase[] NewEntries =
    {
        new(
            "lobby.skill_info.method_label",
            "방식 : ",
            "Method: ",
            "方式：",
            "方式：",
            "Método: "),
        new(
            "lobby.skill_info.cost_label",
            "소모 :",
            "Cost:",
            "消耗：",
            "消費：",
            "Coste:"),
        new(
            "lobby.rune_slot_unlock_level",
            "캐릭터 {0}레벨에 오픈됩니다.",
            "Opens at character level {0}.",
            "角色达到{0}级时开放。",
            "キャラクターがレベル{0}になると開放されます。",
            "Se abre en el nivel {0} del personaje."),
        new(
            "lobby.stat.hp.description",
            "캐릭터의 체력입니다.\\n체력이 0이 되면 전투불능 상태가 됩니다.",
            "Character health.\\nAt 0 HP, the character becomes unable to fight.",
            "角色的生命值。\\n生命值降至0时，角色会无法战斗。",
            "キャラクターの体力です。\\nHPが0になると戦闘不能になります。",
            "La salud del personaje.\\nCon 0 PV, no puede combatir."),
        new(
            "lobby.stat.cost.description",
            "스킬을 사용할 때 소모하는 자원입니다.\\n현재 코스트가 부족하면 스킬을 사용할 수 없습니다.",
            "A resource spent to use skills.\\nSkills cannot be used without enough current Cost.",
            "使用技能时消耗的资源。\\n当前费用不足时无法使用技能。",
            "スキル使用時に消費するリソースです。\\n現在のコストが足りないとスキルを使用できません。",
            "Recurso que se gasta al usar habilidades.\\nNo puedes usar habilidades sin suficiente coste actual."),
        new(
            "lobby.stat.recovery.description",
            "턴이 시작될 때 회복되는 코스트 수치입니다.\\n회복량이 높을수록 한 턴에 사용할 수 있는 스킬 선택지가 늘어납니다.",
            "Cost restored at the start of the turn.\\nHigher recovery gives more skill options each turn.",
            "回合开始时恢复的费用数值。\\n恢复量越高，每回合可选择的技能越多。",
            "ターン開始時に回復するコスト量です。\\n回復量が高いほど、そのターンに使えるスキルの選択肢が増えます。",
            "Coste recuperado al inicio del turno.\\nUna mayor recuperación ofrece más opciones de habilidad cada turno."),
        new(
            "lobby.stat.move.description",
            "전투 중 이동스킬 사용 시 1칸 이동에 1코스트를 소모합니다.\\n이동력이 50 이상이면 2칸 이동에 1코스트를 소모합니다.",
            "Movement skills spend 1 Cost per tile during battle.\\nAt 50 or more Move Points, 1 Cost moves 2 tiles.",
            "战斗中使用移动技能时，每移动1格消耗1点费用。\\n移动力达到50以上时，每1点费用可移动2格。",
            "戦闘中に移動スキルを使うと、1マス移動するごとに1コスト消費します。\\n移動力が50以上なら、1コストで2マス移動します。",
            "Las habilidades de movimiento gastan 1 coste por casilla en combate.\\nCon 50 o más puntos de movimiento, 1 coste mueve 2 casillas."),
        new(
            "lobby.stat.base_value",
            "기본 수치 {0}",
            "Base value {0}",
            "基础数值 {0}",
            "基本値 {0}",
            "Valor base {0}"),
        new(
            "lobby.stat.rune_bonus",
            "룬 보정 {0}",
            "Rune bonus {0}",
            "符文修正 {0}",
            "ルーン補正 {0}",
            "Bonificación de runa {0}"),
    };

    private static readonly SceneTextCase[] SceneTexts =
    {
        new("1968573378", "lobby.skill_info.method_label"),
        new("643678573", "lobby.skill_info.cost_label"),
    };

    [Test]
    public void LocalizationWorkbook_ContainsLobbyCharacterPanelRows()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");

        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, "Korean(ko)");
        int englishIndex = FindHeader(headers, "English(en)");
        int chineseIndex = FindHeader(headers, "Chinese (Simplified)(zh-Hans)");
        int japaneseIndex = FindHeader(headers, "Japanese(ja)");
        int spanishIndex = FindHeader(headers, "Spanish(es)");

        Dictionary<string, IReadOnlyList<string>> byKey = rows
            .Skip(1)
            .Where(row => keyIndex < row.Count && !string.IsNullOrWhiteSpace(row[keyIndex]))
            .ToDictionary(row => row[keyIndex], row => row);

        foreach (LocalizationEntryCase entry in NewEntries)
        {
            Assert.That(byKey.ContainsKey(entry.Key), Is.True, entry.Key);
            IReadOnlyList<string> row = byKey[entry.Key];
            Assert.That(GetValue(row, koreanIndex), Is.EqualTo(entry.Korean), entry.Key);
            Assert.That(GetValue(row, englishIndex), Is.EqualTo(entry.English), entry.Key);
            Assert.That(GetValue(row, chineseIndex), Is.EqualTo(entry.Chinese), entry.Key);
            Assert.That(GetValue(row, japaneseIndex), Is.EqualTo(entry.Japanese), entry.Key);
            Assert.That(GetValue(row, spanishIndex), Is.EqualTo(entry.Spanish), entry.Key);
        }
    }

    [Test]
    public void TextStringTables_ContainLobbyCharacterPanelEntries()
    {
        string sharedData = File.ReadAllText(SharedDataPath);
        string[] tablePaths =
        {
            KoreanTablePath,
            EnglishTablePath,
            ChineseTablePath,
            JapaneseTablePath,
            SpanishTablePath,
        };

        foreach (LocalizationEntryCase entry in NewEntries)
        {
            string id = FindSharedTableId(sharedData, entry.Key);

            foreach (string tablePath in tablePaths)
            {
                string tableYaml = File.ReadAllText(tablePath);
                Assert.That(
                    Regex.IsMatch(tableYaml, @"m_Id: " + Regex.Escape(id) + @"\s+m_Localized: "),
                    Is.True,
                    $"{entry.Key} missing from {tablePath}");
            }
        }
    }

    [Test]
    public void EnglishStringTable_UsesLobbyCharacterPanelTranslations()
    {
        string sharedData = File.ReadAllText(SharedDataPath);
        string englishTable = File.ReadAllText(EnglishTablePath);

        foreach (LocalizationEntryCase entry in NewEntries)
        {
            string id = FindSharedTableId(sharedData, entry.Key);
            Assert.That(
                Regex.IsMatch(
                    englishTable,
                    @"m_Id: " + Regex.Escape(id) +
                    @"\s+m_Localized: "),
                Is.True,
                entry.Key);
            Assert.That(englishTable, Does.Contain(entry.English), entry.Key);
        }
    }

    [Test]
    public void LobbyScene_SkillInfoLabelsUseLocalizeStringEvents()
    {
        string sceneYaml = File.ReadAllText(LobbyScenePath);

        foreach (SceneTextCase sceneText in SceneTexts)
        {
            string localizerPattern =
                @"MonoBehaviour:\s+.*?m_GameObject: \{fileID: " +
                Regex.Escape(sceneText.GameObjectFileId) +
                @"\}.*?m_Script: \{fileID: 11500000, guid: 56eb0353ae6e5124bb35b17aff880f16, type: 3\}" +
                @".*?m_Key: " +
                Regex.Escape(sceneText.Key);

            Assert.That(
                Regex.IsMatch(sceneYaml, localizerPattern, RegexOptions.Singleline),
                Is.True,
                sceneText.Key);
        }
    }

    [Test]
    public void RuneSlotLockedHover_UsesLocalizedMessages()
    {
        string source = File.ReadAllText(RuneSettingPanelPath);

        Assert.That(source, Does.Contain("GameLocalization.Format(\"lobby.rune_slot_unlock_level\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"lobby.rune_slot_locked\""));
        Assert.That(source, Does.Not.Contain("$\"캐릭터 {requiredLevel}레벨에 오픈됩니다.\""));
    }

    [Test]
    public void CharacterStatTooltipTarget_UsesLocalizedDefaults()
    {
        string source = File.ReadAllText(CharacterStatTooltipTargetPath);

        Assert.That(source, Does.Contain("GameLocalization.Get(\"common.hp\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"common.cost\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"common.recovery\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"common.move_point\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"lobby.stat.hp.description\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"lobby.stat.cost.description\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"lobby.stat.recovery.description\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(\"lobby.stat.move.description\""));
        Assert.That(source, Does.Contain("GameLocalization.Format(\"lobby.stat.base_value\""));
        Assert.That(source, Does.Contain("GameLocalization.Format(\"lobby.stat.rune_bonus\""));
    }

    private static string FindSharedTableId(string sharedData, string key)
    {
        string keyPattern = @"m_Id: (?<id>\d+)\s+m_Key: " + Regex.Escape(key);
        Match keyMatch = Regex.Match(sharedData, keyPattern);
        Assert.That(keyMatch.Success, Is.True, key);
        return keyMatch.Groups["id"].Value;
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

    private readonly struct LocalizationEntryCase
    {
        public LocalizationEntryCase(
            string key,
            string korean,
            string english,
            string chinese,
            string japanese,
            string spanish)
        {
            Key = key;
            Korean = korean;
            English = english;
            Chinese = chinese;
            Japanese = japanese;
            Spanish = spanish;
        }

        public string Key { get; }
        public string Korean { get; }
        public string English { get; }
        public string Chinese { get; }
        public string Japanese { get; }
        public string Spanish { get; }
    }

    private readonly struct SceneTextCase
    {
        public SceneTextCase(string gameObjectFileId, string key)
        {
            GameObjectFileId = gameObjectFileId;
            Key = key;
        }

        public string GameObjectFileId { get; }
        public string Key { get; }
    }
}
