using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

public class PreparedLocalizationTranslationsTests
{
    private const string WorkbookPath = "Assets/ExcelSource/Localization.xlsx";
    private const string SharedDataPath = "Assets/Language/Text Shared Data.asset";
    private const string EnglishTablePath = "Assets/Language/Text_en.asset";
    private const string ChineseTablePath = "Assets/Language/Text_zh-Hans.asset";
    private const string JapaneseTablePath = "Assets/Language/Text_ja.asset";
    private const string SpanishTablePath = "Assets/Language/Text_es.asset";

    private static readonly PreparedTranslationCase[] TranslationCases =
    {
        new("가까운 캐릭터에게\\n이동합니다.", "Move to the\\nnearest character.", "移动至\\n最近的角色。", "最も近いキャラクターへ\\n移動します。", "Muévete hacia el\\npersonaje más cercano."),
        new("결속", "Bond", "羁绊", "結束", "Vínculo"),
        new("간파", "Insight", "洞察", "看破", "Perspicacia"),
        new("결심", "Resolve", "决心", "決意", "Determinación"),
        new("격려", "Encouragement", "鼓舞", "激励", "Ánimo"),
        new("강철 피부", "Steel Skin", "钢铁皮肤", "鋼鉄の皮膚", "Piel de Acero"),
        new("가시갑옷", "Thorn Armor", "荆棘铠甲", "棘の鎧", "Armadura de Espinas"),
        new("가시 돋친 갑옷을 둘러 자신에게 가호 2를 부여합니다.", "Surround yourself with thorny armor, granting yourself 2 Ward.", "以带刺的铠甲护住自身，使自己获得2层庇护。", "棘の生えた鎧をまとい、自身に加護を2付与します。", "Cúbrete con una armadura espinosa y obtén 2 de Protección."),
        new("검에 기를 둘러 적에게 7의 피해를 줍니다.", "Channel energy into your sword to deal 7 damage to an enemy.", "将气注入剑中，对敌人造成7点伤害。", "剣に気をまとわせ、敵に7ダメージを与えます。", "Imbuye la espada con energía para infligir 7 de daño a un enemigo."),
        new("가까운 캐릭터를 향해 1칸 이동합니다.", "Move 1 tile toward the nearest character.", "向最近的角色移动1格。", "最も近いキャラクターに向かって1マス移動します。", "Muévete 1 casilla hacia el personaje más cercano."),
        new("거미줄 갑주", "Web Armor", "蛛网甲胄", "蜘蛛糸の甲冑", "Armadura de Telaraña"),
        new("가까운 캐릭터에게 이동합니다.", "Move to the nearest character.", "移动至最近的角色。", "最も近いキャラクターへ移動します。", "Muévete hacia el personaje más cercano."),
        new("검심", "Swordheart", "剑心", "剣心", "Corazón de Espada", preserveSuffix: true),
        new("각인·불안정한 원천", "Engraving·Unstable Source", "刻印·不稳定之源", "刻印・不安定な源", "Grabado·Fuente Inestable"),
        new("각인·메마른 고동", "Engraving·Withered Pulse", "刻印·枯竭脉动", "刻印・乾いた鼓動", "Grabado·Pulso Marchito"),
        new("각인·굳어진 육신", "Engraving·Hardened Flesh", "刻印·硬化之躯", "刻印・固まった肉体", "Grabado·Cuerpo Endurecido"),
        new("각인·잃어버린 숨결", "Engraving·Lost Breath", "刻印·遗失吐息", "刻印・失われた息吹", "Grabado·Aliento Perdido"),
        new("각인·뒤틀린 격류", "Engraving·Twisted Torrent", "刻印·扭曲激流", "刻印・歪んだ激流", "Grabado·Torrente Retorcido"),
        new("가벼운 발걸음", "Light Step", "轻盈步伐", "軽やかな足取り", "Paso Ligero"),
        new("강렬한 파동", "Powerful Wave", "强烈波动", "強烈な波動", "Onda Intensa"),
        new("가시", "Thorns", "荆棘", "棘", "Espinas"),
        new("거미알", "Spider Egg", "蜘蛛卵", "蜘蛛の卵", "Huevo de Araña"),
        new("거미줄", "Web", "蛛网", "蜘蛛の糸", "Telaraña"),
        new("강타", "Smash", "猛击", "強打", "Golpe Fuerte"),
        new("가호", "Ward", "庇护", "加護", "Protección"),
        new("고유스킬 사용 가능 최소 고유자원 기준을 수치만큼 변화시킨다.", "Changes the minimum Unique Resource required to use a Unique Skill by the specified amount.", "使施放固有技能所需的最低固有资源要求改变指定数值。", "固有スキルを使用可能になる最低固有リソース条件を指定値分変更する。", "Cambia en la cantidad indicada el mínimo de Recurso Único necesario para usar una Habilidad Única."),
        new("고유스킬 수치 퍼센트 변화", "Unique Skill Value Percentage Change", "固有技能数值百分比变化", "固有スキル数値割合変化", "Cambio Porcentual del Valor de Habilidad Única"),
        new("고유스킬 수치를 퍼센트로 변화시킨다.", "Changes the Unique Skill value by a percentage.", "按百分比改变固有技能数值。", "固有スキルの数値を割合で変更する。", "Cambia porcentualmente el valor de la Habilidad Única."),
        new("고유스킬 처치 집중", "Unique Skill Kill Focus", "固有技能击杀集中", "固有スキル撃破集中", "Concentración de Eliminación de Habilidad Única"),
        new("강제이동 면역", "Forced Movement Immunity", "强制移动免疫", "強制移動無効", "Inmunidad al Movimiento Forzado"),
        new("가시 장판 생성", "Create Thorn Field", "生成荆棘区域", "棘エリア生成", "Crear Campo de Espinas"),
        new("거미알 생성", "Create Spider Egg", "生成蜘蛛卵", "蜘蛛の卵生成", "Crear Huevo de Araña"),
        new("거미줄 생성", "Create Web", "生成蛛网", "蜘蛛の糸生成", "Crear Telaraña"),
        new("강철 닻", "Steel Anchor", "钢铁之锚", "鋼鉄の錨", "Ancla de Acero"),
        new("감염된 바늘", "Infected Needle", "感染之针", "感染した針", "Aguja Infectada"),
        new("경화제 앰플", "Hardening Ampoule", "硬化剂安瓿", "硬化剤アンプル", "Ampolla Endurecedora"),
        new("각성제", "Stimulant", "兴奋剂", "覚醒剤", "Estimulante"),
        new("간이 바리게이트", "Makeshift Barricade", "简易路障", "簡易バリケード", "Barricada Improvisada"),
        new("고대 조각", "Ancient Fragment", "远古碎片", "古代の欠片", "Fragmento Antiguo"),
        new("강대한 존재를 쓰러뜨린 증표다. 판매 시 100 더스티움을 얻는다.", "Proof of defeating a mighty being. Sell it to gain 100 Dustium.", "击败强大存在的证明。出售后可获得100尘晶。", "強大な存在を倒した証。売却すると100ダスティウムを獲得する。", "Prueba de haber derrotado a un ser poderoso. Al venderlo, obtienes 100 Dustium."),
        new("거래하지 않는다", "Do Not Trade", "不可交易", "取引しない", "No comerciar"),
        new("고블린 토벌", "Goblin Hunt", "哥布林讨伐", "ゴブリン討伐", "Caza de Goblins"),
        new("고블린을 10마리 처치하라", "Defeat 10 Goblins.", "击败10只哥布林。", "ゴブリンを10体倒せ。", "Derrota a 10 Goblins.")
    };

    [Test]
    public void Workbook_UsesPreparedTranslationsForEveryMatchingKoreanRow()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");

        IReadOnlyList<string> headers = rows[0];
        int koreanIndex = FindHeader(headers, "Korean(ko)");
        int englishIndex = FindHeader(headers, "English(en)");
        int chineseIndex = FindHeader(headers, "Chinese (Simplified)(zh-Hans)");
        int japaneseIndex = FindHeader(headers, "Japanese(ja)");
        int spanishIndex = FindHeader(headers, "Spanish(es)");

        foreach (PreparedTranslationCase translation in TranslationCases)
        {
            List<IReadOnlyList<string>> matches = rows
                .Skip(1)
                .Where(row => MatchesKorean(GetValue(row, koreanIndex), translation))
                .ToList();

            Assert.That(matches, Is.Not.Empty, translation.Korean);
            foreach (IReadOnlyList<string> row in matches)
            {
                string actualKorean = GetValue(row, koreanIndex);
                Assert.That(NormalizeCellLineBreaks(GetValue(row, englishIndex)), Is.EqualTo(GetExpectedValue(translation, actualKorean, translation.English)), translation.Korean);
                Assert.That(NormalizeCellLineBreaks(GetValue(row, chineseIndex)), Is.EqualTo(GetExpectedValue(translation, actualKorean, translation.Chinese)), translation.Korean);
                Assert.That(NormalizeCellLineBreaks(GetValue(row, japaneseIndex)), Is.EqualTo(GetExpectedValue(translation, actualKorean, translation.Japanese)), translation.Korean);
                Assert.That(NormalizeCellLineBreaks(GetValue(row, spanishIndex)), Is.EqualTo(GetExpectedValue(translation, actualKorean, translation.Spanish)), translation.Korean);
            }
        }
    }

    [Test]
    public void TextStringTables_UsePreparedTranslationsForEveryMatchingWorkbookKey()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");

        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, "Korean(ko)");

        Dictionary<string, string> sharedIds = ReadSharedIds();
        string englishTable = File.ReadAllText(EnglishTablePath);
        string chineseTable = File.ReadAllText(ChineseTablePath);
        string japaneseTable = File.ReadAllText(JapaneseTablePath);
        string spanishTable = File.ReadAllText(SpanishTablePath);

        foreach (PreparedTranslationCase translation in TranslationCases)
        {
            List<string> keys = rows
                .Skip(1)
                .Where(row => MatchesKorean(GetValue(row, koreanIndex), translation))
                .Select(row => new WorkbookKeyMatch(GetValue(row, keyIndex), GetValue(row, koreanIndex)))
                .Where(match => !string.IsNullOrWhiteSpace(match.Key))
                .Distinct()
                .ToList();

            Assert.That(keys, Is.Not.Empty, translation.Korean);
            foreach (WorkbookKeyMatch match in keys)
            {
                Assert.That(sharedIds.ContainsKey(match.Key), Is.True, match.Key);
                string id = sharedIds[match.Key];

                AssertLocaleValue(englishTable, id, GetExpectedValue(translation, match.Korean, translation.English), match.Key);
                AssertLocaleValue(chineseTable, id, GetExpectedValue(translation, match.Korean, translation.Chinese), match.Key);
                AssertLocaleValue(japaneseTable, id, GetExpectedValue(translation, match.Korean, translation.Japanese), match.Key);
                AssertLocaleValue(spanishTable, id, GetExpectedValue(translation, match.Korean, translation.Spanish), match.Key);
            }
        }
    }

    private static Dictionary<string, string> ReadSharedIds()
    {
        string sharedData = File.ReadAllText(SharedDataPath);
        var sharedIds = new Dictionary<string, string>();

        foreach (Match match in Regex.Matches(
                     sharedData,
                     @"m_Id: (?<id>\d+)\s+m_Key: (?<key>[^\r\n]+)"))
        {
            sharedIds[match.Groups["key"].Value.Trim()] = match.Groups["id"].Value;
        }

        return sharedIds;
    }

    private static void AssertLocaleValue(string tableYaml, string id, string expected, string key)
    {
        string encodedExpected = EncodeUnityYamlString(expected);
        Assert.That(
            Regex.IsMatch(
                tableYaml,
                @"m_Id: " + Regex.Escape(id) +
                @"\s+m_Localized: " + Regex.Escape(encodedExpected)),
            Is.True,
            key);
    }

    private static bool MatchesKorean(string actual, PreparedTranslationCase expected)
    {
        string actualValue = NormalizeLookupValue(actual);
        string expectedValue = NormalizeLookupValue(expected.Korean);
        return actualValue == expectedValue ||
               expected.PreserveSuffix &&
               actualValue.StartsWith(expectedValue + " ", StringComparison.Ordinal);
    }

    private static string GetExpectedValue(
        PreparedTranslationCase translation,
        string actualKorean,
        string baseValue)
    {
        if (!translation.PreserveSuffix)
            return baseValue;

        string normalizedActual = NormalizeCellLineBreaks(actualKorean).Trim();
        return normalizedActual.StartsWith(translation.Korean + " ", StringComparison.Ordinal)
            ? baseValue + normalizedActual.Substring(translation.Korean.Length)
            : baseValue;
    }

    private static string NormalizeLookupValue(string value) =>
        NormalizeCellLineBreaks(value).Replace("\\n", "\n");

    private static string NormalizeCellLineBreaks(string value) =>
        (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");

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

    private static string EncodeUnityYamlString(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (char character in value ?? string.Empty)
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < 0x20 || character > 0x7e)
                        builder.Append("\\u").Append(((int)character).ToString("X4"));
                    else
                        builder.Append(character);
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private readonly struct WorkbookKeyMatch
    {
        public WorkbookKeyMatch(string key, string korean)
        {
            Key = key;
            Korean = korean;
        }

        public string Key { get; }
        public string Korean { get; }
    }

    private readonly struct PreparedTranslationCase
    {
        public PreparedTranslationCase(
            string korean,
            string english,
            string chinese,
            string japanese,
            string spanish,
            bool preserveSuffix = false)
        {
            Korean = korean;
            English = english;
            Chinese = chinese;
            Japanese = japanese;
            Spanish = spanish;
            PreserveSuffix = preserveSuffix;
        }

        public string Korean { get; }
        public string English { get; }
        public string Chinese { get; }
        public string Japanese { get; }
        public string Spanish { get; }
        public bool PreserveSuffix { get; }
    }
}
