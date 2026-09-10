using System;
using System.Collections.Generic;
using System.Linq;

public enum LocalizationBindingStatus
{
    Valid,
    Fixed,
    MissingKey,
    SourceMismatch,
    AmbiguousKey,
    MissingComponent,
    UnexpectedComponent,
    DynamicTextWithStaticLocalization,
    DuplicateBinding,
    ManualConflict,
}

public readonly struct LocalizationBindingEntry
{
    public LocalizationBindingEntry(string key, string korean)
    {
        Key = key ?? string.Empty;
        Korean = korean ?? string.Empty;
    }

    public string Key { get; }
    public string Korean { get; }
}

public readonly struct LocalizationKeyResolution
{
    public LocalizationKeyResolution(LocalizationBindingStatus status, string key, IReadOnlyList<string> candidates)
    {
        Status = status;
        Key = key ?? string.Empty;
        Candidates = candidates ?? Array.Empty<string>();
    }

    public LocalizationBindingStatus Status { get; }
    public string Key { get; }
    public IReadOnlyList<string> Candidates { get; }
    public bool IsUnique => Status == LocalizationBindingStatus.Valid && !string.IsNullOrWhiteSpace(Key);
}

/// <summary>Workbook Korean source/key relations. Duplicate Korean copy never selects an arbitrary key.</summary>
public sealed class LocalizationBindingResolver
{
    private readonly Dictionary<string, string> koreanByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> keysByKorean = new(StringComparer.Ordinal);

    public LocalizationBindingResolver(IEnumerable<LocalizationBindingEntry> entries)
    {
        foreach (LocalizationBindingEntry entry in entries ?? Array.Empty<LocalizationBindingEntry>())
        {
            string key = Normalize(entry.Key);
            string korean = Normalize(entry.Korean);
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(korean))
                koreanByKey[key] = korean;
        }

        foreach (IGrouping<string, KeyValuePair<string, string>> group in koreanByKey.GroupBy(pair => pair.Value, StringComparer.Ordinal))
            keysByKorean[group.Key] = group.Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    public LocalizationKeyResolution ResolveSource(string koreanSource)
    {
        string normalizedSource = Normalize(koreanSource);
        if (string.IsNullOrWhiteSpace(normalizedSource) || !keysByKorean.TryGetValue(normalizedSource, out string[] candidates))
            return new LocalizationKeyResolution(LocalizationBindingStatus.MissingKey, string.Empty, Array.Empty<string>());

        return candidates.Length == 1
            ? new LocalizationKeyResolution(LocalizationBindingStatus.Valid, candidates[0], candidates)
            : new LocalizationKeyResolution(LocalizationBindingStatus.AmbiguousKey, string.Empty, candidates);
    }

    public LocalizationBindingStatus Validate(string koreanSource, string key)
    {
        string normalizedSource = Normalize(koreanSource);
        string normalizedKey = Normalize(key);
        if (string.IsNullOrWhiteSpace(normalizedKey) || !koreanByKey.TryGetValue(normalizedKey, out string tableKorean))
            return LocalizationBindingStatus.MissingKey;
        return string.Equals(normalizedSource, tableKorean, StringComparison.Ordinal)
            ? LocalizationBindingStatus.Valid
            : LocalizationBindingStatus.SourceMismatch;
    }

    public bool TryGetKorean(string key, out string korean) => koreanByKey.TryGetValue(Normalize(key), out korean);

    public static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : value.Replace("\r\n", "\n").Trim();
}
