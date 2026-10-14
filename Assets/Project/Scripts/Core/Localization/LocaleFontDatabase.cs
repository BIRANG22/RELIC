using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "LocaleFontDatabase", menuName = "RELIC/Localization/Locale Font Database")]
public sealed class LocaleFontDatabase : ScriptableObject
{
    public const string BodyRoleId = "body";
    public const string TitleRoleId = "title";
    public const string NumberRoleId = "number";

    [SerializeField] private LocaleFontProfile[] profiles = Array.Empty<LocaleFontProfile>();

    public IReadOnlyList<LocaleFontProfile> Profiles => profiles;

    public bool TryResolve(string localeCode, string roleId, out TMP_FontAsset font)
    {
        font = null;
        string normalizedLocale = Normalize(localeCode);
        string normalizedRole = Normalize(roleId);
        if (string.IsNullOrEmpty(normalizedLocale))
            return false;

        LocaleFontProfile exact = FindProfile(normalizedLocale);
        string parentCode = GetParentLocaleCode(normalizedLocale);
        LocaleFontProfile parent = string.IsNullOrEmpty(parentCode) ? null : FindProfile(parentCode);

        if (TryResolveRole(exact, normalizedRole, out font) ||
            TryResolveRole(parent, normalizedRole, out font))
        {
            return true;
        }

        if (exact != null && exact.DefaultFont != null)
        {
            font = exact.DefaultFont;
            return true;
        }

        if (parent != null && parent.DefaultFont != null)
        {
            font = parent.DefaultFont;
            return true;
        }

        return false;
    }

    public string[] GetRoleIds()
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRole(BodyRoleId, result, seen);
        AddRole(TitleRoleId, result, seen);
        AddRole(NumberRoleId, result, seen);

        for (int i = 0; i < profiles.Length; i++)
        {
            LocaleFontProfile profile = profiles[i];
            if (profile?.RoleFonts == null)
                continue;

            for (int roleIndex = 0; roleIndex < profile.RoleFonts.Count; roleIndex++)
                AddRole(profile.RoleFonts[roleIndex]?.RoleId, result, seen);
        }

        return result.ToArray();
    }

    public ValidationIssue[] Validate()
    {
        var issues = new List<ValidationIssue>();
        var locales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < profiles.Length; i++)
        {
            LocaleFontProfile profile = profiles[i];
            if (profile == null)
            {
                issues.Add(new ValidationIssue("null-profile", $"Locale profile {i} is null."));
                continue;
            }

            string locale = Normalize(profile.LocaleCode);
            if (string.IsNullOrEmpty(locale))
                issues.Add(new ValidationIssue("empty-locale", $"Locale profile {i} has an empty code."));
            else if (!locales.Add(locale))
                issues.Add(new ValidationIssue("duplicate-locale", $"Locale code '{locale}' is duplicated."));

            var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<RoleFontEntry> entries = profile.RoleFonts;
            for (int roleIndex = 0; roleIndex < entries.Count; roleIndex++)
            {
                RoleFontEntry entry = entries[roleIndex];
                if (entry == null)
                {
                    issues.Add(new ValidationIssue("null-role", $"Locale '{locale}' role {roleIndex} is null."));
                    continue;
                }

                string role = Normalize(entry.RoleId);
                if (string.IsNullOrEmpty(role))
                    issues.Add(new ValidationIssue("empty-role", $"Locale '{locale}' has an empty role ID."));
                else if (!roles.Add(role))
                    issues.Add(new ValidationIssue("duplicate-role", $"Locale '{locale}' role '{role}' is duplicated."));

                if (entry.Font == null)
                    issues.Add(new ValidationIssue("missing-font", $"Locale '{locale}' role '{role}' has no font."));
            }

            AddMissingInitialRoleIssue(locale, BodyRoleId, roles, issues);
            AddMissingInitialRoleIssue(locale, TitleRoleId, roles, issues);
            AddMissingInitialRoleIssue(locale, NumberRoleId, roles, issues);
        }

        return issues.ToArray();
    }

    private LocaleFontProfile FindProfile(string localeCode)
    {
        for (int i = 0; i < profiles.Length; i++)
        {
            LocaleFontProfile profile = profiles[i];
            if (profile != null &&
                string.Equals(Normalize(profile.LocaleCode), localeCode, StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return null;
    }

    private static bool TryResolveRole(LocaleFontProfile profile, string roleId, out TMP_FontAsset font)
    {
        font = null;
        if (profile == null || string.IsNullOrEmpty(roleId))
            return false;

        IReadOnlyList<RoleFontEntry> entries = profile.RoleFonts;
        for (int i = 0; i < entries.Count; i++)
        {
            RoleFontEntry entry = entries[i];
            if (entry != null && entry.Font != null &&
                string.Equals(Normalize(entry.RoleId), roleId, StringComparison.OrdinalIgnoreCase))
            {
                font = entry.Font;
                return true;
            }
        }

        return false;
    }

    private static string GetParentLocaleCode(string localeCode)
    {
        int separator = localeCode.LastIndexOf('-');
        if (separator <= 0 || separator >= localeCode.Length - 1)
            return string.Empty;

        string suffix = localeCode.Substring(separator + 1);
        bool regionCode = suffix.Length == 2;
        if (!regionCode && suffix.Length == 3)
        {
            regionCode = true;
            for (int i = 0; i < suffix.Length; i++)
                regionCode &= char.IsDigit(suffix[i]);
        }

        return regionCode ? localeCode.Substring(0, separator) : string.Empty;
    }

    private static void AddRole(string roleId, List<string> result, HashSet<string> seen)
    {
        string normalized = Normalize(roleId);
        if (!string.IsNullOrEmpty(normalized) && seen.Add(normalized))
            result.Add(normalized);
    }

    private static void AddMissingInitialRoleIssue(
        string locale,
        string roleId,
        HashSet<string> roles,
        List<ValidationIssue> issues)
    {
        if (!roles.Contains(roleId))
            issues.Add(new ValidationIssue("missing-role", $"Locale '{locale}' has no '{roleId}' mapping; defaultFont will be used."));
    }

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    [Serializable]
    public sealed class LocaleFontProfile
    {
        [SerializeField] private string localeCode;
        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private RoleFontEntry[] roleFonts = Array.Empty<RoleFontEntry>();

        public string LocaleCode => localeCode;
        public TMP_FontAsset DefaultFont => defaultFont;
        public IReadOnlyList<RoleFontEntry> RoleFonts => roleFonts ?? Array.Empty<RoleFontEntry>();
    }

    [Serializable]
    public sealed class RoleFontEntry
    {
        [SerializeField, LocaleFontRole] private string roleId = BodyRoleId;
        [SerializeField] private TMP_FontAsset font;

        public string RoleId => roleId;
        public TMP_FontAsset Font => font;
    }

    public readonly struct ValidationIssue
    {
        public ValidationIssue(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }
    }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class LocaleFontRoleAttribute : PropertyAttribute
{
}
