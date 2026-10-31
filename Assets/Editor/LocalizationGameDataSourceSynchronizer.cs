using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public static class LocalizationGameDataSourceSynchronizer
{
    private const string GameDataWorkbook = "Assets/ExcelSource/GameData.xlsx";
    private static readonly string[] SheetNames =
    {
        "Character", "Monster", "SkillMaster", "MonsterSkill", "GridEffect", "Effect",
        "Map", "BattleMap", "Event", "SkillRange", "Rune", "Relic", "Compound", "Item", "Erosion",
    };

    public static readonly ReadOnlyCollection<string> ManagedSheets = Array.AsReadOnly(SheetNames);

    public readonly struct SyncResult
    {
        public int SourceUpdates { get; }
        public int Additions { get; }
        public int Removals { get; }

        public SyncResult(int sourceUpdates, int additions, int removals)
        {
            SourceUpdates = sourceUpdates;
            Additions = additions;
            Removals = removals;
        }
    }

    public static SyncResult Audit()
    {
        List<LocalizationWorkbookEntry> sources = CollectSources();
        IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(
            LocalizationExcelImporter.WorkbookPath,
            LocalizationExcelImporter.WorksheetName);
        Dictionary<string, string> koreanByKey = ReadKoreanByKey(rows);
        HashSet<string> activeKeys = sources.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

        int updates = sources.Count(entry => koreanByKey.TryGetValue(entry.Key, out string current) &&
            !string.Equals(LocalizationBindingResolver.Normalize(current),
                LocalizationBindingResolver.Normalize(entry.Korean), StringComparison.Ordinal));
        int additions = sources.Count(entry => !koreanByKey.ContainsKey(entry.Key));
        int removals = FindRemovedManagedKeys(koreanByKey.Keys, activeKeys).Length;
        return new SyncResult(updates, additions, removals);
    }

    public static SyncResult Synchronize()
    {
        List<LocalizationWorkbookEntry> sources = CollectSources();
        HashSet<string> activeKeys = sources.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(
            LocalizationExcelImporter.WorkbookPath,
            LocalizationExcelImporter.WorksheetName);
        Dictionary<string, string> koreanByKey = ReadKoreanByKey(rows);

        LocalizationWorkbookEntry[] updates = sources.Where(entry =>
            koreanByKey.TryGetValue(entry.Key, out string current) &&
            !string.Equals(LocalizationBindingResolver.Normalize(current),
                LocalizationBindingResolver.Normalize(entry.Korean), StringComparison.Ordinal)).ToArray();
        LocalizationWorkbookEntry[] additions = sources.Where(entry => !koreanByKey.ContainsKey(entry.Key)).ToArray();
        string[] removals = FindRemovedManagedKeys(koreanByKey.Keys, activeKeys);

        LocalizationWorkbookWriter.SynchronizeEntries(
            LocalizationExcelImporter.WorkbookPath,
            updates,
            additions,
            Array.Empty<string>()); // 구형 키의 참조/ID가 남아 있을 수 있어 자동 삭제하지 않습니다.
        return new SyncResult(updates.Length, additions.Length, 0);
    }

    public static string[] FindRemovedManagedKeys(
        IEnumerable<string> existingKeys,
        IEnumerable<string> activeKeys)
    {
        var active = new HashSet<string>(activeKeys ?? Array.Empty<string>(), StringComparer.Ordinal);
        return (existingKeys ?? Array.Empty<string>())
            .Where(key => IsManagedGameDataKey(key) && !active.Contains(key))
            .ToArray();
    }

    private static List<LocalizationWorkbookEntry> CollectSources()
    {
        var result = new List<LocalizationWorkbookEntry>();
        var scannedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sheet in ManagedSheets)
        {
            IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(GameDataWorkbook, sheet);
            if (rows.Count < 3)
                continue;

            IReadOnlyList<string> headers = rows[0];
            int choiceOrderColumn = FindHeaderIndex(headers, "ChoiceOrder", "선택지 순서");
            // Row 2 contains the runtime/schema field names, not game data.
            for (int rowIndex = 2; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string> row = rows[rowIndex];
                string stableId = row.Count > 0 ? row[0] : string.Empty;
                if (string.IsNullOrWhiteSpace(stableId))
                    continue;

                for (int column = 1; column < headers.Count; column++)
                {
                    string header = headers[column];
                    string korean = column < row.Count ? row[column] : string.Empty;
                    bool isMonsterSkillType = string.Equals(sheet, "MonsterSkill", StringComparison.Ordinal) &&
                                              string.Equals(header.Trim(), "타입", StringComparison.Ordinal);
                    if ((!LocalizationProjectScanner.ShouldCollectGameDataLocalizationValue(sheet, header, korean) &&
                         !isMonsterSkillType) ||
                        (isMonsterSkillType && !LocalizationProjectScanner.IsLocalizableKoreanText(korean)))
                        continue;

                    string sharedKey = LocalizationProjectScanner.GetSharedGameDataKey(sheet, stableId, header, korean);
                    if (!string.IsNullOrEmpty(sharedKey))
                    {
                        if (scannedKeys.Add(sharedKey))
                            result.Add(new LocalizationWorkbookEntry(sharedKey, korean.Trim()));
                        continue;
                    }
                    string stableKey = BuildGameDataKey(sheet, stableId, row, choiceOrderColumn, header);
                    string key = scannedKeys.Add(stableKey)
                        ? stableKey
                        : LocalizationProjectScanner.BuildUniqueGameDataKey(sheet, stableId, header, korean);
                    result.Add(new LocalizationWorkbookEntry(key, korean));
                }
            }
        }
        return result;
    }

    private static string BuildGameDataKey(string sheet, string stableId, IReadOnlyList<string> row,
        int choiceOrderColumn, string header)
    {
        if (string.Equals(sheet, "Event", StringComparison.Ordinal) &&
            choiceOrderColumn >= 0 && LocalizationProjectScanner.IsEventChoiceField(header))
        {
            int choiceOrder = choiceOrderColumn < row.Count && int.TryParse(row[choiceOrderColumn], out int parsed)
                ? parsed : 0;
            return LocalizationProjectScanner.BuildEventChoiceKey(stableId, choiceOrder, header);
        }
        return LocalizationProjectScanner.BuildGameDataKey(sheet, stableId, header);
    }

    private static Dictionary<string, string> ReadKoreanByKey(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        int keyIndex = rows[0].ToList().FindIndex(value => value == "Key");
        int koreanIndex = rows[0].ToList().FindIndex(value =>
            value == LocalizationWorkbookWriter.KoreanSourceHeader);
        if (koreanIndex < 0)
            koreanIndex = rows[0].ToList().FindIndex(value => value == "Korean(ko)");
        return rows.Skip(1)
            .Where(row => keyIndex >= 0 && keyIndex < row.Count && !string.IsNullOrWhiteSpace(row[keyIndex]))
            .ToDictionary(row => row[keyIndex], row => koreanIndex >= 0 && koreanIndex < row.Count ? row[koreanIndex] : "",
                StringComparer.Ordinal);
    }

    private static bool IsManagedGameDataKey(string key)
    {
        return ManagedSheets.Any(sheet => key.StartsWith(
            GameLocalization.BuildDataKey(sheet, "", "").Replace("unknown.unknown", ""),
            StringComparison.Ordinal));
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, params string[] names)
    {
        for (int i = 0; i < headers.Count; i++)
            if (names.Any(name => string.Equals(headers[i], name, StringComparison.OrdinalIgnoreCase)))
                return i;
        return -1;
    }
}
