using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

public sealed class SoundUsageScanOptions
{
    public const string DefaultSoundDatabasePath = "Assets/DB/SoundDatabase.asset";
    public const string DefaultSkillVfxDatabasePath = "Assets/DB/SkillVfxDatabase.asset";
    public const string DefaultReportPath = "AI_Docs/sound-usage-audit.md";

    public string SoundDatabasePath = DefaultSoundDatabasePath;
    public string SkillVfxDatabasePath = DefaultSkillVfxDatabasePath;
    public string[] PrefabSearchRoots =
    {
        "Assets/Project/PrefabsR",
        "Assets/Project/Art/VFX"
    };
    public string[] SourceSearchRoots =
    {
        "Assets/Project/Scripts"
    };

    public SoundDatabase Database;
    public IEnumerable<GameObject> Prefabs;
    public IEnumerable<SkillVfxEntry> SkillVfxEntries;
}

public sealed class SoundUsageReport
{
    public List<SoundUsageDatabaseEntry> DatabaseEntries { get; } = new();
    public List<SoundUsageReference> References { get; } = new();
    public List<EmbeddedAudioSourceUsage> EmbeddedAudioSources { get; } = new();
    public List<string> MissingDatabaseEntryIds { get; } = new();
    public List<string> UnusedDatabaseEntryIds { get; } = new();
    public List<SoundUsageVfxSoundEntry> VfxSoundEntries { get; } = new();
    public List<string> MissingVfxSoundPrefabPaths { get; } = new();

    public IReadOnlyList<SoundUsageReference> GetReferences(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId))
            return Array.Empty<SoundUsageReference>();

        string normalized = soundId.Trim();
        return References
            .Where(reference => reference.SoundId == normalized)
            .ToArray();
    }
}

public sealed class SoundUsageVfxSoundEntry
{
    public SoundUsageVfxSoundEntry(
        string group,
        string vfxPath,
        string vfxName,
        int cueCount,
        string clipNames)
    {
        Group = group ?? "";
        VfxPath = vfxPath ?? "";
        VfxName = vfxName ?? "";
        CueCount = cueCount;
        ClipNames = clipNames ?? "";
    }

    public string Group { get; }
    public string VfxPath { get; }
    public string VfxName { get; }
    public int CueCount { get; }
    public string ClipNames { get; }
}

public sealed class SoundUsageDatabaseEntry
{
    public SoundUsageDatabaseEntry(
        SoundCategory category,
        string id,
        string clipName,
        float volume,
        float pitch,
        bool loop,
        SoundData data)
    {
        Category = category;
        Id = Normalize(id);
        ClipName = clipName ?? "";
        Volume = volume;
        Pitch = pitch;
        Loop = loop;
        Data = data;
    }

    public SoundCategory Category { get; }
    public string Id { get; }
    public string ClipName { get; }
    public float Volume { get; }
    public float Pitch { get; }
    public bool Loop { get; }
    public SoundData Data { get; }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public sealed class SoundUsageReference
{
    public SoundUsageReference(
        string soundId,
        SoundCategory category,
        string context,
        string assetPath,
        string memberPath)
    {
        SoundId = Normalize(soundId);
        Category = category;
        Context = context ?? "";
        AssetPath = assetPath ?? "";
        MemberPath = memberPath ?? "";
    }

    public string SoundId { get; }
    public SoundCategory Category { get; }
    public string Context { get; }
    public string AssetPath { get; }
    public string MemberPath { get; }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public sealed class EmbeddedAudioSourceUsage
{
    public EmbeddedAudioSourceUsage(
        string assetPath,
        string ownerName,
        string memberPath,
        string clipName,
        bool enabled,
        bool playOnAwake,
        float volume,
        float pitch,
        bool loop)
    {
        AssetPath = assetPath ?? "";
        OwnerName = ownerName ?? "";
        MemberPath = memberPath ?? "";
        ClipName = clipName ?? "";
        Enabled = enabled;
        PlayOnAwake = playOnAwake;
        Volume = volume;
        Pitch = pitch;
        Loop = loop;
    }

    public string AssetPath { get; }
    public string OwnerName { get; }
    public string MemberPath { get; }
    public string ClipName { get; }
    public bool Enabled { get; }
    public bool PlayOnAwake { get; }
    public float Volume { get; }
    public float Pitch { get; }
    public bool Loop { get; }
}

public static class SoundUsageScanner
{
    [MenuItem("Relic/Audio/Generate Sound Usage Report")]
    public static void GenerateDefaultReport()
    {
        SoundUsageReport report = Scan(new SoundUsageScanOptions());
        WriteMarkdownReport(report, SoundUsageScanOptions.DefaultReportPath);
        UnityAssetDatabase.Refresh();
        Debug.Log($"[SoundUsageScanner] Report written: {SoundUsageScanOptions.DefaultReportPath}");
    }

    public static SoundUsageReport Scan(SoundUsageScanOptions options = null)
    {
        options ??= new SoundUsageScanOptions();

        SoundUsageReport report = new();
        SoundDatabase database = options.Database != null
            ? options.Database
            : UnityAssetDatabase.LoadAssetAtPath<SoundDatabase>(options.SoundDatabasePath);

        AddDatabaseEntries(report, database);
        AddSourceCodeReferences(report, options);
        AddVfxSoundEntries(report, database);
        AddSkillVfxMappingDiagnostics(report, database, options);
        AddPrefabReferences(report, options);
        ClassifyReferences(report);

        return report;
    }

    public static void WriteMarkdownReport(SoundUsageReport report, string reportPath)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
            reportPath = SoundUsageScanOptions.DefaultReportPath;

        string directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(reportPath, BuildMarkdown(report), Encoding.UTF8);
    }

    public static string BuildMarkdown(SoundUsageReport report)
    {
        report ??= new SoundUsageReport();

        StringBuilder builder = new();
        builder.AppendLine("# Sound Usage Audit");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Database entries: {report.DatabaseEntries.Count}");
        builder.AppendLine($"- References: {report.References.Count}");
        builder.AppendLine($"- Missing database entries: {report.MissingDatabaseEntryIds.Count}");
        builder.AppendLine($"- Unused database entries: {report.UnusedDatabaseEntryIds.Count}");
        builder.AppendLine($"- VFX sound mappings: {report.VfxSoundEntries.Count}");
        builder.AppendLine($"- Skill VFX without playable DB sound: {report.MissingVfxSoundPrefabPaths.Count}");
        builder.AppendLine($"- Embedded AudioSources: {report.EmbeddedAudioSources.Count}");
        builder.AppendLine();

        AppendDatabaseEntries(builder, report);
        AppendVfxSoundMappings(builder, report);
        AppendUsageBySoundId(builder, report);
        AppendIdList(builder, "Missing Database Entries", report.MissingDatabaseEntryIds);
        AppendIdList(builder, "Unused Database Entries", report.UnusedDatabaseEntryIds);
        AppendIdList(builder, "Skill VFX Without Playable DB Sound", report.MissingVfxSoundPrefabPaths);
        AppendEmbeddedAudioSources(builder, report);

        return builder.ToString();
    }

    private static void AddDatabaseEntries(SoundUsageReport report, SoundDatabase database)
    {
        if (database == null)
            return;

        AddDatabaseEntries(report, SoundCategory.Bgm, database.BgmEntries);
        AddDatabaseEntries(report, SoundCategory.Sfx, database.SfxEntries);
    }

    private static void AddDatabaseEntries(
        SoundUsageReport report,
        SoundCategory category,
        IReadOnlyList<SoundData> entries)
    {
        if (entries == null)
            return;

        foreach (SoundData data in entries)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.id))
                continue;

            report.DatabaseEntries.Add(new SoundUsageDatabaseEntry(
                category,
                data.id,
                data.clip != null ? data.clip.name : "",
                data.volume,
                data.pitch,
                data.loop,
                data));
        }
    }

    private static void AddVfxSoundEntries(
        SoundUsageReport report,
        SoundDatabase database)
    {
        if (database == null)
            return;

        AddVfxSoundEntries(report, "Player", database.PlayerSkillVfxSfxEntries);
        AddVfxSoundEntries(report, "Monster", database.MonsterSkillVfxSfxEntries);
    }

    private static void AddVfxSoundEntries(
        SoundUsageReport report,
        string group,
        IReadOnlyList<VfxSoundData> entries)
    {
        if (entries == null)
            return;

        foreach (VfxSoundData entry in entries)
        {
            if (entry == null || entry.vfxPrefab == null)
                continue;

            IReadOnlyList<VfxSoundCue> cues = entry.Cues;
            List<string> clipNames = new();

            for (int i = 0; i < cues.Count; i++)
            {
                AudioClip clip = cues[i]?.clip;
                if (clip != null)
                    clipNames.Add(clip.name);
            }

            report.VfxSoundEntries.Add(new SoundUsageVfxSoundEntry(
                group,
                GetAssetPathOrName(entry.vfxPrefab),
                entry.vfxPrefab.name,
                clipNames.Count,
                string.Join(", ", clipNames)));
        }
    }

    private static void AddSkillVfxMappingDiagnostics(
        SoundUsageReport report,
        SoundDatabase soundDatabase,
        SoundUsageScanOptions options)
    {
        IEnumerable<SkillVfxEntry> entries = options.SkillVfxEntries;
        if (entries == null)
        {
            SkillVfxDatabase database =
                UnityAssetDatabase.LoadAssetAtPath<SkillVfxDatabase>(options.SkillVfxDatabasePath);
            entries = database != null ? database.Entries : Array.Empty<SkillVfxEntry>();
        }

        foreach (SkillVfxEntry entry in entries)
        {
            if (entry == null || entry.Vfx == null)
                continue;

            GameObject prefab = entry.Vfx.prefab;
            if (prefab == null)
                continue;

            if (soundDatabase != null &&
                soundDatabase.TryGetSkillVfxSfx(prefab, out VfxSoundData data) &&
                data != null &&
                data.HasPlayableCue)
            {
                continue;
            }

            AddMissingVfxSoundPrefab(report, prefab);
        }
    }

    private static void AddSourceCodeReferences(
        SoundUsageReport report,
        SoundUsageScanOptions options)
    {
        if (options.SourceSearchRoots == null || options.SourceSearchRoots.Length == 0)
            return;

        Dictionary<string, (string Id, SoundCategory Category)> constants = LoadAudioIdConstants();
        Regex directSfxCall = new(@"AudioManager\.Instance\.PlaySfx\s*\(\s*""([^""]+)""", RegexOptions.Compiled);
        Regex audioIdUse = new(@"AudioIds\.(Bgm|Sfx)\.([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        foreach (string root in options.SourceSearchRoots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalizedPath = NormalizeAssetPath(path);
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    foreach (Match match in directSfxCall.Matches(line))
                    {
                        AddReference(
                            report,
                            match.Groups[1].Value,
                            SoundCategory.Sfx,
                            "Source:AudioManager.PlaySfx",
                            normalizedPath,
                            $"line {i + 1}");
                    }

                    foreach (Match match in audioIdUse.Matches(line))
                    {
                        string key = $"{match.Groups[1].Value}.{match.Groups[2].Value}";
                        if (!constants.TryGetValue(key, out var constant))
                            continue;

                        AddReference(
                            report,
                            constant.Id,
                            constant.Category,
                            $"Source:AudioIds.{key}",
                            normalizedPath,
                            $"line {i + 1}");
                    }
                }
            }
        }
    }

    private static Dictionary<string, (string Id, SoundCategory Category)> LoadAudioIdConstants()
    {
        Dictionary<string, (string Id, SoundCategory Category)> constants =
            new(StringComparer.Ordinal);
        string path = "Assets/Project/Scripts/Core/AudioIds.cs";
        if (!File.Exists(path))
            return constants;

        string currentGroup = "";
        Regex group = new(@"public\s+static\s+class\s+(Bgm|Sfx)", RegexOptions.Compiled);
        Regex constant = new(@"public\s+const\s+string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*""([^""]+)""", RegexOptions.Compiled);

        foreach (string line in File.ReadAllLines(path))
        {
            Match groupMatch = group.Match(line);
            if (groupMatch.Success)
            {
                currentGroup = groupMatch.Groups[1].Value;
                continue;
            }

            Match constantMatch = constant.Match(line);
            if (!constantMatch.Success || string.IsNullOrWhiteSpace(currentGroup))
                continue;

            SoundCategory category = currentGroup == "Bgm"
                ? SoundCategory.Bgm
                : SoundCategory.Sfx;
            constants[$"{currentGroup}.{constantMatch.Groups[1].Value}"] =
                (constantMatch.Groups[2].Value, category);
        }

        return constants;
    }

    private static void AddPrefabReferences(
        SoundUsageReport report,
        SoundUsageScanOptions options)
    {
        foreach (GameObject prefab in GetPrefabs(options))
        {
            if (prefab == null)
                continue;

            string assetPath = GetAssetPathOrName(prefab);
            AddSoundIdAttributeReferences(report, prefab, assetPath);
            AddEmbeddedAudioSources(report, prefab, assetPath);
        }
    }

    private static IEnumerable<GameObject> GetPrefabs(SoundUsageScanOptions options)
    {
        if (options.Prefabs != null)
            return options.Prefabs.Where(prefab => prefab != null);

        if (options.PrefabSearchRoots == null || options.PrefabSearchRoots.Length == 0)
            return Array.Empty<GameObject>();

        List<GameObject> prefabs = new();
        string[] guids = UnityAssetDatabase.FindAssets("t:Prefab", options.PrefabSearchRoots);
        foreach (string guid in guids)
        {
            string path = UnityAssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityAssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        return prefabs;
    }

    private static void AddSoundIdAttributeReferences(
        SoundUsageReport report,
        GameObject prefab,
        string assetPath)
    {
        foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
                continue;

            VisitSerializableObject(
                report,
                component,
                component.GetType().Name,
                $"Prefab:{prefab.name}",
                assetPath,
                new HashSet<object>(ReferenceEqualityComparer.Instance));
        }
    }

    private static void VisitSerializableObject(
        SoundUsageReport report,
        object target,
        string path,
        string context,
        string assetPath,
        HashSet<object> visited)
    {
        if (target == null)
            return;

        Type type = target.GetType();
        if (target is UnityEngine.Object unityObject &&
            target is not Component &&
            target is not GameObject)
        {
            return;
        }

        if (!type.IsValueType && !visited.Add(target))
            return;

        foreach (FieldInfo field in GetSerializableFields(type))
        {
            object value = field.GetValue(target);
            SoundIdAttribute attribute = field.GetCustomAttribute<SoundIdAttribute>();
            string fieldPath = $"{path}.{field.Name}";

            if (attribute != null && field.FieldType == typeof(string))
            {
                AddReference(
                    report,
                    value as string,
                    attribute.Category,
                    context,
                    assetPath,
                    fieldPath);
                continue;
            }

            if (value == null || IsLeafType(field.FieldType))
                continue;

            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                int index = 0;
                foreach (object item in enumerable)
                {
                    VisitSerializableObject(
                        report,
                        item,
                        $"{fieldPath}[{index}]",
                        context,
                        assetPath,
                        visited);
                    index++;
                }

                continue;
            }

            if (IsSerializableDataType(field.FieldType))
            {
                VisitSerializableObject(
                    report,
                    value,
                    fieldPath,
                    context,
                    assetPath,
                    visited);
            }
        }
    }

    private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        while (type != null && type != typeof(MonoBehaviour) && type != typeof(Component))
        {
            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (field.IsStatic)
                    continue;

                if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                    yield return field;
            }

            type = type.BaseType;
        }
    }

    private static bool IsLeafType(Type type)
    {
        return type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(Vector2) ||
            type == typeof(Vector3) ||
            type == typeof(Vector4) ||
            type == typeof(Quaternion) ||
            type == typeof(Color) ||
            typeof(UnityEngine.Object).IsAssignableFrom(type);
    }

    private static bool IsSerializableDataType(Type type)
    {
        if (type == null || IsLeafType(type))
            return false;

        return type.GetCustomAttribute<SerializableAttribute>() != null ||
            type == typeof(BattleVfxEntry) ||
            type == typeof(BattleProjectileVfxEntry);
    }

    private static void AddEmbeddedAudioSources(
        SoundUsageReport report,
        GameObject prefab,
        string assetPath)
    {
        foreach (AudioSource source in prefab.GetComponentsInChildren<AudioSource>(true))
        {
            if (source == null)
                continue;

            report.EmbeddedAudioSources.Add(new EmbeddedAudioSourceUsage(
                assetPath,
                source.gameObject.name,
                GetTransformPath(source.transform, prefab.transform),
                source.clip != null ? source.clip.name : "",
                source.enabled,
                source.playOnAwake,
                source.volume,
                source.pitch,
                source.loop));
        }
    }

    private static void AddReference(
        SoundUsageReport report,
        string id,
        SoundCategory category,
        string context,
        string assetPath,
        string memberPath)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        report.References.Add(new SoundUsageReference(
            id,
            category,
            context,
            assetPath,
            memberPath));
    }

    private static void ClassifyReferences(SoundUsageReport report)
    {
        HashSet<string> databaseIds = report.DatabaseEntries
            .Select(entry => entry.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> referencedIds = report.References
            .Select(reference => reference.SoundId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        report.MissingDatabaseEntryIds.AddRange(
            referencedIds
                .Where(id => !databaseIds.Contains(id))
                .OrderBy(id => id, StringComparer.Ordinal));

        report.UnusedDatabaseEntryIds.AddRange(
            databaseIds
                .Where(id => !referencedIds.Contains(id))
                .OrderBy(id => id, StringComparer.Ordinal));
    }

    private static void AddMissingVfxSoundPrefab(
        SoundUsageReport report,
        GameObject prefab)
    {
        string path = GetAssetPathOrName(prefab);
        if (string.IsNullOrWhiteSpace(path) ||
            report.MissingVfxSoundPrefabPaths.Contains(path, StringComparer.Ordinal))
        {
            return;
        }

        report.MissingVfxSoundPrefabPaths.Add(path);
    }

    private static void AppendDatabaseEntries(StringBuilder builder, SoundUsageReport report)
    {
        builder.AppendLine("## Database Entries");
        builder.AppendLine();
        builder.AppendLine("| Category | ID | Clip | Volume | Pitch | Loop | Uses |");
        builder.AppendLine("|---|---|---|---:|---:|---|---:|");

        foreach (SoundUsageDatabaseEntry entry in report.DatabaseEntries
            .OrderBy(entry => entry.Category)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal))
        {
            builder.AppendLine(
                $"| {entry.Category} | {Escape(entry.Id)} | {Escape(entry.ClipName)} | {entry.Volume:0.###} | {entry.Pitch:0.###} | {entry.Loop} | {report.GetReferences(entry.Id).Count} |");
        }

        builder.AppendLine();
    }

    private static void AppendVfxSoundMappings(StringBuilder builder, SoundUsageReport report)
    {
        builder.AppendLine("## VFX Sound Mappings");
        builder.AppendLine();
        builder.AppendLine("| Group | VFX | Clips | Cue Count |");
        builder.AppendLine("|---|---|---|---:|");

        foreach (SoundUsageVfxSoundEntry entry in report.VfxSoundEntries
            .OrderBy(entry => entry.Group, StringComparer.Ordinal)
            .ThenBy(entry => entry.VfxPath, StringComparer.Ordinal))
        {
            builder.AppendLine(
                $"| {Escape(entry.Group)} | {Escape(entry.VfxPath)} | {Escape(entry.ClipNames)} | {entry.CueCount} |");
        }

        if (report.VfxSoundEntries.Count == 0)
            builder.AppendLine("|  |  |  | 0 |");

        builder.AppendLine();
    }

    private static void AppendUsageBySoundId(StringBuilder builder, SoundUsageReport report)
    {
        builder.AppendLine("## Usage By Sound ID");
        builder.AppendLine();
        builder.AppendLine("| ID | Category | Context | Asset | Member |");
        builder.AppendLine("|---|---|---|---|---|");

        foreach (SoundUsageReference reference in report.References
            .OrderBy(reference => reference.SoundId, StringComparer.Ordinal)
            .ThenBy(reference => reference.Context, StringComparer.Ordinal))
        {
            builder.AppendLine(
                $"| {Escape(reference.SoundId)} | {reference.Category} | {Escape(reference.Context)} | {Escape(reference.AssetPath)} | {Escape(reference.MemberPath)} |");
        }

        builder.AppendLine();
    }

    private static void AppendIdList(StringBuilder builder, string title, IReadOnlyList<string> ids)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();

        if (ids == null || ids.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        foreach (string id in ids.OrderBy(id => id, StringComparer.Ordinal))
            builder.AppendLine($"- `{id}`");

        builder.AppendLine();
    }

    private static void AppendEmbeddedAudioSources(StringBuilder builder, SoundUsageReport report)
    {
        builder.AppendLine("## Embedded AudioSources");
        builder.AppendLine();
        builder.AppendLine("| Asset | Object | Path | Clip | Enabled | Play On Awake | Volume | Pitch | Loop |");
        builder.AppendLine("|---|---|---|---|---|---|---:|---:|---|");

        foreach (EmbeddedAudioSourceUsage source in report.EmbeddedAudioSources
            .OrderBy(source => source.AssetPath, StringComparer.Ordinal)
            .ThenBy(source => source.MemberPath, StringComparer.Ordinal))
        {
            builder.AppendLine(
                $"| {Escape(source.AssetPath)} | {Escape(source.OwnerName)} | {Escape(source.MemberPath)} | {Escape(source.ClipName)} | {source.Enabled} | {source.PlayOnAwake} | {source.Volume:0.###} | {source.Pitch:0.###} | {source.Loop} |");
        }

        builder.AppendLine();
    }

    private static string GetAssetPathOrName(GameObject gameObject)
    {
        if (gameObject == null)
            return "";

        string path = UnityAssetDatabase.GetAssetPath(gameObject);
        return string.IsNullOrWhiteSpace(path) ? gameObject.name : path;
    }

    private static string NormalizeAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        return path.Replace('\\', '/');
    }

    private static string GetTransformPath(Transform target, Transform root)
    {
        if (target == null)
            return "";

        Stack<string> names = new();
        Transform current = target;
        while (current != null)
        {
            names.Push(current.name);
            if (current == root)
                break;

            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static string Escape(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace("|", "\\|");
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
