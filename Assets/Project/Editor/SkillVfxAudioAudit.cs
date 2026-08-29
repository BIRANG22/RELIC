using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

public static class SkillVfxAudioAudit
{
    private const string DefaultSkillVfxDatabasePath = "Assets/DB/SkillVfxDatabase.asset";
    private const string CharacterPrefabSearchRoot = "Assets/Project/PrefabsR/Character";
    private const string MonsterPrefabSearchRoot = "Assets/Project/PrefabsR/Monster";
    private const string DefaultReportPath = "AI_Docs/skill-vfx-audio-audit.md";

    [MenuItem("Relic/Audio/Generate Skill VFX Audio Audit")]
    public static void GenerateDefaultReport()
    {
        SkillVfxDatabase database =
            UnityAssetDatabase.LoadAssetAtPath<SkillVfxDatabase>(DefaultSkillVfxDatabasePath);

        if (database == null)
        {
            Debug.LogWarning($"[SkillVfxAudioAudit] Missing database: {DefaultSkillVfxDatabasePath}");
            return;
        }

        List<SkillVfxAudioAuditResult> results = new();
        results.AddRange(ScanEntries(database.Entries));
        results.AddRange(ScanCharacterPresentationPrefabs());
        results.AddRange(ScanMonsterPresentationPrefabs());
        WriteMarkdownReport(results, DefaultReportPath);
        UnityAssetDatabase.Refresh();
        Debug.Log($"[SkillVfxAudioAudit] Report written: {DefaultReportPath}");
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanEntries(
        IEnumerable<SkillVfxEntry> entries)
    {
        if (entries == null)
            return Array.Empty<SkillVfxAudioAuditResult>();

        List<SkillVfxAudioAuditResult> results = new();

        foreach (SkillVfxEntry entry in entries)
        {
            if (entry == null)
                continue;

            results.Add(ScanEntry(entry.SkillId, entry.Vfx));
        }

        return results;
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanCharacterPresentationPrefabs()
    {
        List<SkillVfxAudioAuditResult> results = new();
        string[] guids = UnityAssetDatabase.FindAssets("t:Prefab", new[] { CharacterPrefabSearchRoot });

        foreach (string guid in guids)
        {
            string path = UnityAssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityAssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            foreach (BattleUnitAnimator animator in prefab.GetComponentsInChildren<BattleUnitAnimator>(true))
                AddAnimatorPresentationResults(results, path, animator);
        }

        return results;
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanMonsterPresentationPrefabs()
    {
        List<SkillVfxAudioAuditResult> results = new();
        string[] guids = UnityAssetDatabase.FindAssets("t:Prefab", new[] { MonsterPrefabSearchRoot });

        foreach (string guid in guids)
        {
            string path = UnityAssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityAssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            foreach (BattleUnitAnimator animator in prefab.GetComponentsInChildren<BattleUnitAnimator>(true))
                AddMonsterPresentationResults(results, path, animator);
        }

        return results;
    }

    public static SkillVfxAudioAuditResult ScanEntry(string skillId, BattleVfxEntry vfx)
    {
        GameObject prefab = vfx != null ? vfx.prefab : null;
        BattleVfxSfxEntry sfx = vfx != null ? vfx.sfx : null;

        return ScanEntry(skillId, prefab, sfx);
    }

    public static SkillVfxAudioAuditResult ScanEntry(
        string skillId,
        GameObject prefab,
        BattleVfxSfxEntry sfx)
    {
        AudioSource[] audioSources = prefab != null
            ? prefab.GetComponentsInChildren<AudioSource>(true)
            : Array.Empty<AudioSource>();

        return new SkillVfxAudioAuditResult(
            skillId,
            prefab,
            sfx,
            audioSources);
    }

    private static void AddAnimatorPresentationResults(
        List<SkillVfxAudioAuditResult> results,
        string prefabPath,
        BattleUnitAnimator animator)
    {
        BattleUnitPlayerSkillPresentations presentations =
            GetPrivateField<BattleUnitPlayerSkillPresentations>(animator, "playerSkillPresentations");

        if (presentations == null)
            return;

        AddPresentationResult(results, prefabPath, "power", presentations.power);
        AddPresentationResult(results, prefabPath, "attack1", presentations.attack1);
        AddPresentationResult(results, prefabPath, "attack2", presentations.attack2);
        AddPresentationResult(results, prefabPath, "attack3", presentations.attack3);
        AddPresentationResult(results, prefabPath, "skill", presentations.skill);
    }

    private static void AddPresentationResult(
        List<SkillVfxAudioAuditResult> results,
        string prefabPath,
        string slotName,
        BattleUnitActionPresentation presentation)
    {
        if (presentation == null || presentation.vfx == null || presentation.vfx.prefab == null)
            return;

        results.Add(ScanEntry($"{prefabPath}:{slotName}", presentation.vfx));
    }

    private static void AddMonsterPresentationResults(
        List<SkillVfxAudioAuditResult> results,
        string prefabPath,
        BattleUnitAnimator animator)
    {
        BattleUnitActionPresentation[] presentations =
            GetPrivateField<BattleUnitActionPresentation[]>(animator, "monsterActionPresentations");

        if (presentations == null)
            return;

        for (int i = 0; i < presentations.Length; i++)
        {
            BattleUnitActionPresentation presentation = presentations[i];

            if (presentation == null)
                continue;

            string state = string.IsNullOrWhiteSpace(presentation.stateName)
                ? $"action{i + 1}"
                : presentation.stateName.Trim();
            string prefix = $"{prefabPath}:monsterAction{i + 1}:{state}";

            if (presentation.vfx != null && presentation.vfx.prefab != null)
                results.Add(ScanEntry($"{prefix}:vfx", presentation.vfx));

            BattleProjectileVfxEntry projectile = presentation.projectileVfx;
            if (projectile == null)
                continue;

            if (projectile.missilePrefab != null)
                results.Add(ScanEntry($"{prefix}:missile", projectile.missilePrefab, projectile.missileSfx));

            if (projectile.impactPrefab != null)
                results.Add(ScanEntry($"{prefix}:impact", projectile.impactPrefab, projectile.impactSfx));
        }
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        if (target == null)
            return default;

        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        return field != null ? (T)field.GetValue(target) : default;
    }

    public static void WriteMarkdownReport(
        IReadOnlyList<SkillVfxAudioAuditResult> results,
        string reportPath)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
            reportPath = DefaultReportPath;

        string directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(reportPath, BuildMarkdown(results), Encoding.UTF8);
    }

    public static string BuildMarkdown(IReadOnlyList<SkillVfxAudioAuditResult> results)
    {
        results ??= Array.Empty<SkillVfxAudioAuditResult>();

        StringBuilder builder = new();
        builder.AppendLine("# Skill VFX Audio Audit");
        builder.AppendLine();
        builder.AppendLine("| SkillId | VFX Prefab | DB SFX | Embedded AudioSources | Status |");
        builder.AppendLine("|---|---|---|---:|---|");

        foreach (SkillVfxAudioAuditResult result in results.OrderBy(r => r.SkillId, StringComparer.Ordinal))
        {
            builder.Append("| ");
            builder.Append(Escape(result.SkillId));
            builder.Append(" | ");
            builder.Append(Escape(result.PrefabPath));
            builder.Append(" | ");
            builder.Append(Escape(result.DatabaseSfxId));
            builder.Append(" | ");
            builder.Append(result.EmbeddedAudioSourceCount);
            builder.Append(" | ");
            builder.Append(result.Status);
            builder.AppendLine(" |");
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace("|", "\\|");
    }
}

public sealed class SkillVfxAudioAuditResult
{
    public SkillVfxAudioAuditResult(
        string skillId,
        GameObject prefab,
        BattleVfxSfxEntry sfx,
        IReadOnlyList<AudioSource> embeddedAudioSources)
    {
        SkillId = string.IsNullOrWhiteSpace(skillId) ? "" : skillId.Trim();
        Prefab = prefab;
        PrefabPath = prefab != null ? UnityAssetDatabase.GetAssetPath(prefab) : "";
        DatabaseSfxIds = GetDatabaseSfxIds(sfx);
        DatabaseSfxId = string.Join(", ", DatabaseSfxIds);
        EmbeddedAudioSourceCount = embeddedAudioSources != null ? embeddedAudioSources.Count : 0;
    }

    public string SkillId { get; }
    public GameObject Prefab { get; }
    public string PrefabPath { get; }
    public string DatabaseSfxId { get; }
    public IReadOnlyList<string> DatabaseSfxIds { get; }
    public bool HasDatabaseSfxId => DatabaseSfxIds.Count > 0;
    public int EmbeddedAudioSourceCount { get; }
    public bool RequiresMigration => EmbeddedAudioSourceCount > 0;
    public string Status
    {
        get
        {
            if (EmbeddedAudioSourceCount > 0)
                return "Needs migration";

            return HasDatabaseSfxId ? "OK" : "No DB SFX";
        }
    }

    private static IReadOnlyList<string> GetDatabaseSfxIds(BattleVfxSfxEntry sfx)
    {
        List<string> ids = new();

        if (sfx == null)
            return ids;

        if (sfx.playSfx)
            AddSfxId(ids, sfx.sfxId);

        if (sfx.additionalSfx == null)
            return ids;

        foreach (BattleVfxAdditionalSfxEntry cue in sfx.additionalSfx)
            AddSfxId(ids, cue?.sfxId);

        return ids;
    }

    private static void AddSfxId(List<string> ids, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        ids.Add(id.Trim());
    }
}
