using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine.Localization.Settings;

public static class GameLocalization
{
    public const string TableName = "Text";

    public static string BuildDataKey(string category, string stableId, string field)
    {
        return $"data.{NormalizeKeySegment(category)}.{NormalizeKeySegment(stableId)}.{NormalizeKeySegment(field)}";
    }

    public static string GetData(
        string category,
        string stableId,
        string field,
        string fallback)
    {
        return Get(BuildDataKey(category, stableId, field), fallback);
    }

    /// <summary>Runtime UI/system copy must identify a workbook key and must not embed Korean fallback text.</summary>
    public static string Get(string key, params object[] arguments)
    {
        return Get(key, ResolveMissingTranslation("en"), arguments);
    }

    public static string Format(string key, params object[] arguments)
    {
        string template = Get(key);
        try
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                template,
                arguments ?? Array.Empty<object>());
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public static string Format(string key, string fallback, params object[] arguments)
    {
        string template = Get(key, fallback);
        try
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                template,
                arguments ?? Array.Empty<object>());
        }
        catch (FormatException)
        {
            return fallback ?? string.Empty;
        }
    }

    public static string Get(string key, string fallback, params object[] arguments)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback ?? string.Empty;

        try
        {
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString(
                TableName,
                key,
                arguments ?? Array.Empty<object>());

            if (string.IsNullOrEmpty(localized) || IsMissingTranslationResult(localized))
                return ResolveMissingTranslation(
                    LocalizationSettings.SelectedLocale?.Identifier.Code,
                    fallback);

            return localized;
        }
        catch (Exception)
        {
            return fallback ?? string.Empty;
        }
    }

    public static string ResolveMissingTranslation(string localeCode, string koreanFallback = null)
    {
        if (!string.IsNullOrWhiteSpace(localeCode) &&
            localeCode.StartsWith("ko", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(koreanFallback))
        {
            return koreanFallback;
        }

        if (string.IsNullOrWhiteSpace(localeCode))
            return "미번역";

        if (localeCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return "Untranslated";
        if (localeCode.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            return "未翻訳";
        if (localeCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return "未翻译";
        if (localeCode.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            return "Sin traducir";

        return "미번역";
    }

    private static bool IsMissingTranslationResult(string localized)
    {
        if (string.IsNullOrWhiteSpace(localized))
            return true;

        string trimmed = localized.TrimStart();
        return trimmed.StartsWith("No translation found for", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeKeySegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        string snakeCase = Regex.Replace(value.Trim(), "([a-z0-9])([A-Z])", "$1_$2");
        snakeCase = Regex.Replace(snakeCase, "[^A-Za-z0-9]+", "_");
        return snakeCase.Trim('_').ToLowerInvariant();
    }
}
