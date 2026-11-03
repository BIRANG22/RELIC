using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public readonly struct SkillVfxDatabaseIntegrityIssue
{
    public SkillVfxDatabaseIntegrityIssue(string code, string skillId, string guid, int lineNumber)
    {
        Code = code;
        SkillId = skillId ?? string.Empty;
        Guid = guid ?? string.Empty;
        LineNumber = lineNumber;
    }

    public string Code { get; }
    public string SkillId { get; }
    public string Guid { get; }
    public int LineNumber { get; }

    public override string ToString()
    {
        string owner = string.IsNullOrEmpty(SkillId) ? "<empty>" : SkillId;
        string reference = string.IsNullOrEmpty(Guid) ? string.Empty : $", guid={Guid}";
        return $"[{Code}] line {LineNumber}, SkillId={owner}{reference}";
    }
}

public static class SkillVfxDatabaseIntegrityValidator
{
    public const string DefaultDatabasePath = "Assets/DB/SkillVfxDatabase.asset";

    private static readonly Regex SkillIdPattern = new(
        @"^\s*- SkillId:\s*(.*)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex GuidPattern = new(
        @"\bguid:\s*([0-9a-fA-F]{32})\b",
        RegexOptions.Compiled);

    public static SkillVfxDatabaseIntegrityIssue[] ValidateAsset(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            return new[]
            {
                new SkillVfxDatabaseIntegrityIssue("missing-database", string.Empty, string.Empty, 0)
            };
        }

        return ValidateText(
            File.ReadAllText(fullPath),
            guid => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)));
    }

    public static SkillVfxDatabaseIntegrityIssue[] ValidateText(
        string yaml,
        Func<string, bool> guidExists)
    {
        if (yaml == null)
            throw new ArgumentNullException(nameof(yaml));
        if (guidExists == null)
            throw new ArgumentNullException(nameof(guidExists));

        var issues = new List<SkillVfxDatabaseIntegrityIssue>();
        var firstLineBySkillId = new Dictionary<string, int>(StringComparer.Ordinal);
        string currentSkillId = string.Empty;
        string[] lines = yaml.Replace("\r\n", "\n").Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            int lineNumber = index + 1;
            Match skillMatch = SkillIdPattern.Match(lines[index]);
            if (skillMatch.Success)
            {
                currentSkillId = skillMatch.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(currentSkillId))
                {
                    issues.Add(new SkillVfxDatabaseIntegrityIssue(
                        "empty-skill-id", currentSkillId, string.Empty, lineNumber));
                }
                else if (firstLineBySkillId.ContainsKey(currentSkillId))
                {
                    issues.Add(new SkillVfxDatabaseIntegrityIssue(
                        "duplicate-skill-id", currentSkillId, string.Empty, lineNumber));
                }
                else
                {
                    firstLineBySkillId.Add(currentSkillId, lineNumber);
                }
            }

            foreach (Match guidMatch in GuidPattern.Matches(lines[index]))
            {
                string guid = guidMatch.Groups[1].Value.ToLowerInvariant();
                if (!guidExists(guid))
                {
                    issues.Add(new SkillVfxDatabaseIntegrityIssue(
                        "missing-guid", currentSkillId, guid, lineNumber));
                }
            }
        }

        return issues.ToArray();
    }

    [MenuItem("Tools/RELIC/Data/Validate Skill VFX Database")]
    private static void ValidateFromMenu()
    {
        SkillVfxDatabaseIntegrityIssue[] issues = ValidateAsset(DefaultDatabasePath);
        if (issues.Length == 0)
        {
            Debug.Log("[SkillVfxDatabase] Integrity validation passed.");
            return;
        }

        Debug.LogError(
            $"[SkillVfxDatabase] Integrity validation failed ({issues.Length}).\n" +
            string.Join("\n", issues.Select(issue => issue.ToString())));
    }
}
