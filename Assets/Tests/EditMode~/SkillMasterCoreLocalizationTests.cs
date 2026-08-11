using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

public class SkillMasterCoreLocalizationTests
{
    private const string WorkbookPath = "Assets/ExcelSource/Localization.xlsx";
    private const string SharedDataPath = "Assets/Language/Text Shared Data.asset";

    private static readonly string[] LocaleTablePaths =
    {
        "Assets/Language/Text_ko.asset",
        "Assets/Language/Text_en.asset",
        "Assets/Language/Text_zh-Hans.asset",
        "Assets/Language/Text_ja.asset",
        "Assets/Language/Text_es.asset",
    };

    private static readonly string[] SkillTextFields =
    {
        "name",
        "tooltip",
        "details",
    };

    [Test]
    public void LocalizationWorkbook_ContainsMigratedCorePublicSkillRows()
    {
        Dictionary<string, IReadOnlyList<string>> rowsByKey = ReadWorkbookRowsByKey();

        foreach (CorePublicSkillKeyPair pair in EnumerateCorePublicSkillKeyPairs())
        {
            Assert.That(rowsByKey.ContainsKey(pair.TargetKey), Is.True, pair.TargetKey);
        }
    }

    [Test]
    public void MigratedCorePublicSkillRows_CopyLegacyPublicSkillTranslations()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");
        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int firstLocaleIndex = FindHeader(headers, "Korean(ko)");

        Dictionary<string, IReadOnlyList<string>> rowsByKey = rows
            .Skip(1)
            .Where(row => !string.IsNullOrWhiteSpace(GetValue(row, keyIndex)))
            .ToDictionary(row => GetValue(row, keyIndex), row => row);

        foreach (CorePublicSkillKeyPair pair in EnumerateCorePublicSkillKeyPairs())
        {
            Assert.That(rowsByKey.ContainsKey(pair.SourceKey), Is.True, pair.SourceKey);
            Assert.That(rowsByKey.ContainsKey(pair.TargetKey), Is.True, pair.TargetKey);

            IReadOnlyList<string> sourceRow = rowsByKey[pair.SourceKey];
            IReadOnlyList<string> targetRow = rowsByKey[pair.TargetKey];

            for (int column = firstLocaleIndex; column < headers.Count; column++)
            {
                Assert.That(
                    GetValue(targetRow, column),
                    Is.EqualTo(GetValue(sourceRow, column)),
                    $"{pair.SourceKey} -> {pair.TargetKey} column {headers[column]}");
            }
        }
    }

    [Test]
    public void TextStringTables_ContainMigratedCorePublicSkillEntries()
    {
        Dictionary<string, string> sharedIds = ReadSharedIds();
        Dictionary<string, string> localeTables = LocaleTablePaths.ToDictionary(
            path => path,
            File.ReadAllText);

        foreach (CorePublicSkillKeyPair pair in EnumerateCorePublicSkillKeyPairs())
        {
            Assert.That(sharedIds.ContainsKey(pair.TargetKey), Is.True, pair.TargetKey);
            string id = sharedIds[pair.TargetKey];

            foreach (KeyValuePair<string, string> localeTable in localeTables)
            {
                Assert.That(
                    Regex.IsMatch(
                        localeTable.Value,
                        @"m_Id: " + Regex.Escape(id) + @"\s+m_Localized: "),
                    Is.True,
                    $"{pair.TargetKey} missing from {localeTable.Key}");
            }
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> ReadWorkbookRowsByKey()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");
        int keyIndex = FindHeader(rows[0], "Key");

        return rows
            .Skip(1)
            .Where(row => !string.IsNullOrWhiteSpace(GetValue(row, keyIndex)))
            .ToDictionary(row => GetValue(row, keyIndex), row => row);
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

    private static IEnumerable<CorePublicSkillKeyPair> EnumerateCorePublicSkillKeyPairs()
    {
        for (int number = 1; number <= 20; number++)
        {
            int coreNumber = number + 60;
            foreach (string field in SkillTextFields)
            {
                yield return new CorePublicSkillKeyPair(
                    $"data.skill_master.s_public_{number:00}.{field}",
                    $"data.skill_master.s_core_{coreNumber:00}.{field}");
            }
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

    private readonly struct CorePublicSkillKeyPair
    {
        public CorePublicSkillKeyPair(string sourceKey, string targetKey)
        {
            SourceKey = sourceKey;
            TargetKey = targetKey;
        }

        public string SourceKey { get; }
        public string TargetKey { get; }
    }
}
