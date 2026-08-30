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
    private const string DefaultSoundDatabasePath = "Assets/DB/SoundDatabase.asset";
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

        SoundDatabase soundDatabase = LoadDefaultSoundDatabase();
        List<SkillVfxAudioAuditResult> results = new();
        results.AddRange(ScanEntries(database.Entries, soundDatabase));
        results.AddRange(ScanCharacterPresentationPrefabs(soundDatabase));
        results.AddRange(ScanMonsterPresentationPrefabs(soundDatabase));
        WriteMarkdownReport(results, DefaultReportPath);
        UnityAssetDatabase.Refresh();
        Debug.Log($"[SkillVfxAudioAudit] Report written: {DefaultReportPath}");
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanEntries(
        IEnumerable<SkillVfxEntry> entries)
    {
        return ScanEntries(entries, LoadDefaultSoundDatabase());
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanEntries(
        IEnumerable<SkillVfxEntry> entries,
        SoundDatabase soundDatabase)
    {
        if (entries == null)
            return Array.Empty<SkillVfxAudioAuditResult>();

        List<SkillVfxAudioAuditResult> results = new();

        foreach (SkillVfxEntry entry in entries)
        {
            if (entry == null)
                continue;

            results.Add(ScanEntry(entry.SkillId, entry.Vfx, soundDatabase));
        }

        return results;
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanCharacterPresentationPrefabs()
    {
        return ScanCharacterPresentationPrefabs(LoadDefaultSoundDatabase());
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanCharacterPresentationPrefabs(
        SoundDatabase soundDatabase)
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
                AddAnimatorPresentationResults(results, path, animator, soundDatabase);
        }

        return results;
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanMonsterPresentationPrefabs()
    {
        return ScanMonsterPresentationPrefabs(LoadDefaultSoundDatabase());
    }

    public static IReadOnlyList<SkillVfxAudioAuditResult> ScanMonsterPresentationPrefabs(
        SoundDatabase soundDatabase)
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
                AddMonsterPresentationResults(results, path, animator, soundDatabase);
        }

        return results;
    }

    public static SkillVfxAudioAuditResult ScanEntry(string skillId, BattleVfxEntry vfx)
    {
        return ScanEntry(skillId, vfx, LoadDefaultSoundDatabase());
    }

    public static SkillVfxAudioAuditResult ScanEntry(
        string skillId,
        BattleVfxEntry vfx,
        SoundDatabase soundDatabase)
    {
        GameObject prefab = vfx != null ? vfx.prefab : null;
        return ScanEntry(skillId, prefab, soundDatabase);
    }

    public static SkillVfxAudioAuditResult ScanEntry(
        string skillId,
        GameObject prefab,
        SoundDatabase soundDatabase)
    {
        AudioSource[] audioSources = prefab != null
            ? prefab.GetComponentsInChildren<AudioSource>(true)
            : Array.Empty<AudioSource>();

        VfxSoundData soundData = ResolveVfxSoundData(soundDatabase, prefab);

        return new SkillVfxAudioAuditResult(
            skillId,
            prefab,
            soundData,
            audioSources);
    }

    private static void AddAnimatorPresentationResults(
        List<SkillVfxAudioAuditResult> results,
        string prefabPath,
        BattleUnitAnimator animator,
        SoundDatabase soundDatabase)
    {
        BattleUnitPlayerSkillPresentations presentations =
            GetPrivateField<BattleUnitPlayerSkillPresentations>(animator, "playerSkillPresentations");

        if (presentations == null)
            return;

        AddPresentationResult(results, prefabPath, "power", presentations.power, soundDatabase);
        AddPresentationResult(results, prefabPath, "attack1", presentations.attack1, soundDatabase);
        AddPresentationResult(results, prefabPath, "attack2", presentations.attack2, soundDatabase);
        AddPresentationResult(results, prefabPath, "attack3", presentations.attack3, soundDatabase);
        AddPresentationResult(results, prefabPath, "skill", presentations.skill, soundDatabase);
    }

    private static void AddPresentationResult(
        List<SkillVfxAudioAuditResult> results,
        string prefabPath,
        string slotName,
        BattleUnitActionPresentation presentation,
        SoundDatabase soundDatabase)
    {
        if (presentation == null || presentation.vfx == null || presentation.vfx.prefab == null)
            return;

        results.Add(ScanEntry($"{prefabPath}:{slotName}", presentation.vfx, soundDatabase));
    }

    private static void AddMonsterPresentationResults(
        List<SkillVfxAudioAuditResult> results,
        string prefabPath,
        BattleUnitAnimator animator,
        SoundDatabase soundDatabase)
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
                results.Add(ScanEntry($"{prefix}:vfx", presentation.vfx, soundDatabase));

            BattleProjectileVfxEntry projectile = presentation.projectileVfx;
            if (projectile == null)
                continue;

            if (projectile.missilePrefab != null)
                results.Add(ScanEntry($"{prefix}:missile", projectile.missilePrefab, soundDatabase));

            if (projectile.impactPrefab != null)
                results.Add(ScanEntry($"{prefix}:impact", projectile.impactPrefab, soundDatabase));
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
        builder.AppendLine("| SkillId | VFX Prefab | DB VFX Clips | Embedded AudioSources | Status |");
        builder.AppendLine("|---|---|---|---:|---|");

        foreach (SkillVfxAudioAuditResult result in results.OrderBy(r => r.SkillId, StringComparer.Ordinal))
        {
            builder.Append("| ");
            builder.Append(Escape(result.SkillId));
            builder.Append(" | ");
            builder.Append(Escape(result.PrefabPath));
            builder.Append(" | ");
            builder.Append(Escape(result.DatabaseClipName));
            builder.Append(" | ");
            builder.Append(result.EmbeddedAudioSourceCount);
            builder.Append(" | ");
            builder.Append(result.Status);
            builder.AppendLine(" |");
        }

        return builder.ToString();
    }

    private static VfxSoundData ResolveVfxSoundData(
        SoundDatabase soundDatabase,
        GameObject prefab)
    {
        if (soundDatabase == null || prefab == null)
            return null;

        return soundDatabase.TryGetSkillVfxSfx(prefab, out VfxSoundData data)
            ? data
            : null;
    }

    private static SoundDatabase LoadDefaultSoundDatabase()
    {
        return UnityAssetDatabase.LoadAssetAtPath<SoundDatabase>(DefaultSoundDatabasePath);
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
        VfxSoundData soundData,
        IReadOnlyList<AudioSource> embeddedAudioSources)
    {
        SkillId = string.IsNullOrWhiteSpace(skillId) ? "" : skillId.Trim();
        Prefab = prefab;
        PrefabPath = prefab != null ? UnityAssetDatabase.GetAssetPath(prefab) : "";
        DatabaseClipNames = GetDatabaseClipNames(soundData);
        DatabaseClipName = string.Join(", ", DatabaseClipNames);
        HasDatabaseMapping = soundData != null;
        EmbeddedAudioSourceCount = embeddedAudioSources != null ? embeddedAudioSources.Count : 0;
    }

    public string SkillId { get; }
    public GameObject Prefab { get; }
    public string PrefabPath { get; }
    public string DatabaseClipName { get; }
    public IReadOnlyList<string> DatabaseClipNames { get; }
    public bool HasDatabaseMapping { get; }
    public bool HasDatabaseVfxSound => DatabaseClipNames.Count > 0;
    public int EmbeddedAudioSourceCount { get; }
    public bool RequiresMigration => EmbeddedAudioSourceCount > 0;
    public string Status
    {
        get
        {
            if (EmbeddedAudioSourceCount > 0)
                return "Needs embedded AudioSource cleanup";

            if (HasDatabaseVfxSound)
                return "OK";

            return HasDatabaseMapping ? "No playable DB VFX SFX" : "No DB VFX SFX";
        }
    }

    private static IReadOnlyList<string> GetDatabaseClipNames(VfxSoundData soundData)
    {
        List<string> names = new();

        if (soundData == null || soundData.Cues == null)
            return names;

        for (int i = 0; i < soundData.Cues.Count; i++)
        {
            AudioClip clip = soundData.Cues[i]?.clip;
            if (clip != null)
                names.Add(clip.name);
        }

        return names;
    }
}
