using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class LocalizationProjectScanner
{
    private static readonly Regex NonKeyCharacters = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static string BuildSuggestedKey(string assetPath, string objectName)
    {
        return BuildSuggestedKey(assetPath, objectName, string.Empty);
    }

    public static string BuildSuggestedKey(string assetPath, string objectName, string koreanSource)
    {
        string path = (assetPath ?? string.Empty).Replace('\\', '/');
        const string prefix = "Assets/Project/PrefabsR/";
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            path = path.Substring(prefix.Length);
        else if (path.StartsWith("Assets/Project/Scenes/", StringComparison.OrdinalIgnoreCase))
            path = path.Substring("Assets/Project/Scenes/".Length);

        path = Path.ChangeExtension(path, null) ?? string.Empty;
        string[] parts = (path + "/" + (objectName ?? string.Empty)).Split('/');
        var normalized = new List<string>();
        foreach (string part in parts)
        {
            string value = NonKeyCharacters.Replace(part.ToLowerInvariant(), "_").Trim('_');
            if (!string.IsNullOrWhiteSpace(value) && value != "ydm")
                normalized.Add(value);
        }

        string keyPrefix = normalized.Count == 0 ? "ui.unknown.text" : "ui." + string.Join(".", normalized);
        return string.IsNullOrEmpty(koreanSource) ? keyPrefix : keyPrefix + "." + BuildStableSuffix(koreanSource);
    }

    public static bool IsLocalizableKoreanText(string value)
    {
        return LocalizationTextRules.IsKoreanPlayerText(value);
    }

    public static IReadOnlyList<LocalizationSourceEntry> FindExplicitLocalizationSources(string source)
    {
        string code = RemoveComments(source ?? string.Empty);
        var results = new List<LocalizationSourceEntry>();
        const string literal = "\"(?<value>(?:\\\\.|[^\"\\\\])*)\"";
        var koreanOnly = new Regex(
            @"GameLocalization\.(?:Get|FormatWithFallback)\s*\(\s*" + literal.Replace("value", "key") +
            @"\s*,\s*" + literal.Replace("value", "korean"),
            RegexOptions.Multiline);
        foreach (Match match in koreanOnly.Matches(code))
            results.Add(new LocalizationSourceEntry(
                DecodeCSharpLiteral(match.Groups["key"].Value),
                DecodeCSharpLiteral(match.Groups["korean"].Value)));

        return results
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    /// <summary>
    /// 번역 키 없이 동적 출력 API에 직접 전달된 한국어 리터럴을 찾습니다.
    /// 일반 로그/내부 문자열은 자동 등록하지 않습니다.
    /// </summary>
    public static IReadOnlyList<string> FindDynamicDisplayLiterals(string source)
    {
        string code = RemoveComments(source ?? string.Empty);
        const string literal = "\"(?<value>(?:\\\\.|[^\"\\\\])*)\"";
        var sinkCall = new Regex(
            @"(?:BattleWarningUI\.ShowMessage|SettingWarningUI\.ShowMessage|ShowBattleWarning|ShowWarning|[A-Za-z_]\w*\.Show|BattleMapIntroText\.ShowMessage(?:AndWait)?)\s*\(\s*" + literal,
            RegexOptions.Multiline);

        var directTmpWriter = new Regex(
            @"(?:\b\w+\.text\s*=|\b\w+\.SetText\s*\()\s*" + literal,
            RegexOptions.Multiline);

        var formattedReturn = new Regex(
            @"\breturn\s+string\.Format\s*\(\s*" + literal,
            RegexOptions.Multiline);

        return sinkCall.Matches(code)
            .Cast<Match>()
            .Concat(directTmpWriter.Matches(code).Cast<Match>())
            .Concat(formattedReturn.Matches(code).Cast<Match>())
            .Select(match => DecodeCSharpLiteral(match.Groups["value"].Value))
            .Where(IsLocalizableKoreanText)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>인스펙터에 직렬화되어 플레이어에게 표시되는 한국어 기본값을 찾습니다.</summary>
    public static IReadOnlyList<string> FindSerializedPlayerTextLiterals(string source)
    {
        string code = RemoveComments(source ?? string.Empty);
        const string literal = "\"(?<value>(?:\\\\.|[^\"\\\\])*)\"";
        var serializedPlayerText = new Regex(
            @"\[SerializeField[^\]]*\][^;\r\n]*\b(?:\w*(?:Text|Message|Title|Label|Description|DisplayName|DisplayNames))\b\s*=\s*" + literal,
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        return serializedPlayerText.Matches(code)
            .Cast<Match>()
            .Select(match => DecodeCSharpLiteral(match.Groups["value"].Value))
            .Where(IsLocalizableKoreanText)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> FindUnityYamlTmpTexts(string source)
    {
        return Regex.Matches(
                source ?? string.Empty,
                "(?ms)^[ \\t]*m_text:[ \\t]*(?<value>\\\"(?:\\\\.|[^\\\"\\\\])*\\\"|[^\\r\\n]*)")
            .Cast<Match>()
            .Select(match => Regex.Replace(match.Groups["value"].Value.Trim(), @"\r?\n\s+", " "))
            .Select(DecodeUnityYamlText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    public static IReadOnlyList<string> FindUnityYamlPlayerTextFields(string source)
    {
        return Regex.Matches(
                source ?? string.Empty,
                @"(?m)^\s*(?:displayName):\s*(?<value>.*)$")
            .Cast<Match>()
            .Select(match => DecodeUnityYamlText(match.Groups["value"].Value))
            .Where(IsLocalizableKoreanText)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string DecodeCSharpLiteral(string value)
    {
        try { return Regex.Unescape(value ?? string.Empty); }
        catch (ArgumentException) { return value ?? string.Empty; }
    }

    /// <summary>Unity YAML에 직렬화된 TMP 문자열을 스캔용 원문으로 복원합니다.</summary>
    public static string DecodeUnityYamlText(string serializedValue)
    {
        if (string.IsNullOrWhiteSpace(serializedValue))
            return string.Empty;

        string value = serializedValue.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            value = value.Substring(1, value.Length - 2);

        try
        {
            return NormalizeKnownXmlControls(Regex.Unescape(value));
        }
        catch (ArgumentException)
        {
            // 손상된 YAML 이스케이프는 원문 그대로 두어 스캔 실패가 예외로 번지지 않게 합니다.
            return value;
        }
    }

    /// <summary>
    /// 코드 리터럴은 실제 표시 대상인지 안전하게 판별할 수 없습니다.
    /// 자동 키 추가는 프리팹/씬/게임데이터의 명시적인 원문에만 허용합니다.
    /// </summary>
    public static bool IsAutomaticScriptLiteralCandidate(string value)
    {
        return false;
    }

    public static bool IsPlayerFacingGameDataColumn(string header)
    {
        return IsPlayerFacingGameDataColumn(string.Empty, header);
    }

    public static bool IsPlayerFacingGameDataColumn(string sheetName, string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return false;

        string compactHeader = header.Replace(" ", string.Empty).Replace("\t", string.Empty);
        if (string.Equals(sheetName, "SkillMaster", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(compactHeader, "효과", StringComparison.Ordinal) ||
             string.Equals(compactHeader, "Details", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return header.Contains("이름", StringComparison.Ordinal) ||
               header.Contains("제목", StringComparison.Ordinal) ||
               header.Contains("설명", StringComparison.Ordinal) ||
               header.Contains("툴팁", StringComparison.Ordinal) ||
               header.Contains("소개", StringComparison.Ordinal) ||
               header.Contains("레어도", StringComparison.Ordinal) ||
               header.Contains("타임라인표기", StringComparison.Ordinal) ||
               header.Contains("특수행동", StringComparison.Ordinal) ||
               header.Contains("선택지", StringComparison.Ordinal) ||
               compactHeader.Contains("선택불가", StringComparison.Ordinal) ||
               header.Contains("실패결과", StringComparison.Ordinal);
    }

    public static string BuildGameDataKey(string sheetName, string stableId, string header)
    {
        return GameLocalization.BuildDataKey(
            sheetName,
            stableId,
            GetDataFieldName(sheetName, header));
    }

    public static string BuildUniqueGameDataKey(string sheetName, string stableId, string header, string koreanSource)
    {
        return BuildGameDataKey(sheetName, stableId, header) + "." + BuildStableSuffix(koreanSource);
    }

    /// <summary>Event 시트의 반복 선택지를 EventId와 선택 순서로 구분하는 런타임 키입니다.</summary>
    public static string BuildEventChoiceKey(string eventId, int choiceOrder, string header)
    {
        string field = GetDataFieldName("Event", header);
        if (!field.StartsWith("choice_", StringComparison.Ordinal) &&
            field != "disabled_choice_description" &&
            field != "failure_description")
        {
            return BuildGameDataKey("Event", eventId, header);
        }

        int normalizedOrder = Math.Max(0, choiceOrder);
        string choiceField = field switch
        {
            "choice_name" => $"choice_{normalizedOrder}_name",
            "choice_description" => $"choice_{normalizedOrder}_description",
            "disabled_choice_description" => $"choice_{normalizedOrder}_disabled_description",
            "failure_description" => $"choice_{normalizedOrder}_failure_description",
            _ => field,
        };
        return GameLocalization.BuildDataKey("Event", eventId, choiceField);
    }

    public static bool IsEventChoiceField(string header)
    {
        string field = GetDataFieldName("Event", header);
        return field == "choice_name" ||
               field == "choice_description" ||
               field == "disabled_choice_description" ||
               field == "failure_description";
    }

    private static string GetDataFieldName(string sheetName, string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return "text";

        string compactHeader = header.Replace(" ", string.Empty).Replace("\t", string.Empty);
        if (string.Equals(sheetName, "SkillMaster", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(compactHeader, "효과", StringComparison.Ordinal) ||
             string.Equals(compactHeader, "Details", StringComparison.OrdinalIgnoreCase)))
        {
            return "details";
        }
        if (compactHeader.Contains("선택지이름", StringComparison.Ordinal))
            return "choice_name";
        if (compactHeader.Contains("선택지내용", StringComparison.Ordinal))
            return "choice_description";
        if (compactHeader.Contains("선택불가", StringComparison.Ordinal))
            return "disabled_choice_description";
        if (compactHeader.Contains("실패결과", StringComparison.Ordinal))
            return "failure_description";
        if (compactHeader.Contains("이름", StringComparison.Ordinal))
            return "name";
        if (compactHeader.Contains("제목", StringComparison.Ordinal))
            return "description";
        if (compactHeader.Contains("소개", StringComparison.Ordinal))
            return "introduction";
        if (compactHeader.Contains("Regeneration", StringComparison.OrdinalIgnoreCase) ||
            compactHeader.Contains("카르마획득", StringComparison.Ordinal))
            return "regeneration";
        if (compactHeader.Contains("효과설명", StringComparison.Ordinal))
            return "effect_description";
        if (compactHeader.Contains("툴팁", StringComparison.Ordinal))
            return "tooltip";
        if (compactHeader.Contains("레어도", StringComparison.Ordinal))
            return "rarity";
        if (compactHeader.Contains("특수행동", StringComparison.Ordinal))
            return "special_action";
        if (compactHeader.Contains("타임라인", StringComparison.Ordinal))
            return "timeline_label";
        if (compactHeader == "타입")
            return "type";
        return "description";
    }

    public static string ReadScriptText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // 프로젝트의 기존 CP949 소스는 Unity에서 정상 지원합니다.
            return Encoding.GetEncoding(949).GetString(bytes);
        }
    }

    private static string BuildStableSuffix(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char character in value)
            {
                hash ^= character;
                hash *= 16777619;
            }
            return hash.ToString("x8");
        }
    }

    /// <summary>문자열 리터럴은 보존하고 C# 한 줄/블록 주석만 제거합니다.</summary>
    public static string RemoveComments(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;

        var result = new StringBuilder(source.Length);
        bool inString = false;
        bool inLineComment = false;
        bool inBlockComment = false;
        bool escaped = false;
        for (int index = 0; index < source.Length; index++)
        {
            char current = source[index];
            char next = index + 1 < source.Length ? source[index + 1] : '\0';
            if (inLineComment)
            {
                if (current == '\n') { inLineComment = false; result.Append(current); }
                continue;
            }
            if (inBlockComment)
            {
                if (current == '*' && next == '/') { inBlockComment = false; index++; }
                continue;
            }
            if (!inString && current == '/' && next == '/') { inLineComment = true; index++; continue; }
            if (!inString && current == '/' && next == '*') { inBlockComment = true; index++; continue; }
            result.Append(current);
            if (current == '"' && !escaped) inString = !inString;
            escaped = current == '\\' && !escaped;
            if (current != '\\') escaped = false;
        }
        return result.ToString();
    }

    public static string NormalizeKnownXmlControls(string value)
    {
        return (value ?? string.Empty).Replace('\v', '\n');
    }
}

public sealed class LocalizationSourceEntry
{
    public string Key { get; }
    public string Korean { get; }

    public LocalizationSourceEntry(string key, string korean)
    {
        Key = key ?? string.Empty;
        Korean = korean ?? string.Empty;
    }
}
