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
            return string.IsNullOrEmpty(localized) ? fallback ?? string.Empty : localized;
        }
        catch (Exception)
        {
            return fallback ?? string.Empty;
        }
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
