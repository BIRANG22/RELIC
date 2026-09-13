using System;
using System.Collections.Generic;
using System.IO;
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
            return Regex.Unescape(value);
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
        if (string.IsNullOrWhiteSpace(header))
            return false;

        return header.Contains("이름", StringComparison.Ordinal) ||
               header.Contains("제목", StringComparison.Ordinal) ||
               header.Contains("설명", StringComparison.Ordinal) ||
               header.Contains("툴팁", StringComparison.Ordinal) ||
               header.Contains("소개", StringComparison.Ordinal) ||
               header.Contains("레어도", StringComparison.Ordinal) ||
               header.Contains("타임라인표기", StringComparison.Ordinal) ||
               header.Contains("특수행동", StringComparison.Ordinal) ||
               header.Contains("선택지", StringComparison.Ordinal) ||
               header.Contains("실패결과", StringComparison.Ordinal);
    }

    public static string BuildGameDataKey(string sheetName, string stableId, string header)
    {
        return $"data.{NormalizeDataSegment(sheetName)}.{NormalizeDataSegment(stableId)}.{GetDataFieldName(header)}";
    }

    public static string BuildUniqueGameDataKey(string sheetName, string stableId, string header, string koreanSource)
    {
        return BuildGameDataKey(sheetName, stableId, header) + "." + BuildStableSuffix(koreanSource);
    }

    /// <summary>Event 시트의 반복 선택지를 EventId와 선택 순서로 구분하는 런타임 키입니다.</summary>
    public static string BuildEventChoiceKey(string eventId, int choiceOrder, string header)
    {
        string field = GetDataFieldName(header);
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
        return BuildGameDataKey("Event", eventId, choiceField);
    }

    private static string GetDataFieldName(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return "text";

        string compactHeader = header.Replace(" ", string.Empty).Replace("\t", string.Empty);
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
        if (compactHeader.Contains("선택지이름", StringComparison.Ordinal))
            return "choice_name";
        if (compactHeader.Contains("선택지내용", StringComparison.Ordinal))
            return "choice_description";
        if (compactHeader.Contains("선택불가", StringComparison.Ordinal))
            return "disabled_choice_description";
        if (compactHeader.Contains("실패결과", StringComparison.Ordinal))
            return "failure_description";
        if (compactHeader == "타입")
            return "type";
        return "description";
    }

    private static string NormalizeDataSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        string normalized = NonKeyCharacters.Replace(value.ToLowerInvariant(), "_").Trim('_');
        return string.IsNullOrWhiteSpace(normalized) ? "unknown" : normalized;
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
}
