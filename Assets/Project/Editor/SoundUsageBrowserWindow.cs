using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

public sealed class SoundUsageBrowserWindow : EditorWindow
{
    private SoundDatabase database;
    private SerializedObject serializedDatabase;
    private SoundUsageReport report;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private string selectedId;
    private string filter = "";
    private const string VfxKeyPrefix = "vfx:";
    private const string MissingVfxKeyPrefix = "missing-vfx:";

    [MenuItem("Relic/Audio/Open Sound Usage Browser")]
    public static void Open()
    {
        SoundUsageBrowserWindow window = GetWindow<SoundUsageBrowserWindow>("Sound Usage");
        window.Refresh();
        window.Show();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (report == null)
            Refresh();

        if (database == null)
            EditorGUILayout.HelpBox("SoundDatabase.asset을 찾을 수 없습니다.", MessageType.Warning);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSoundList();
            DrawDetails();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Refresh();

            if (GUILayout.Button("Generate Report", EditorStyles.toolbarButton, GUILayout.Width(120)))
                GenerateReport();

            if (GUILayout.Button("Ping Database", EditorStyles.toolbarButton, GUILayout.Width(110)) && database != null)
                EditorGUIUtility.PingObject(database);

            GUILayout.Space(8);
            GUILayout.Label("Filter", GUILayout.Width(35));
            filter = GUILayout.TextField(filter, EditorStyles.toolbarSearchField);
        }
    }

    private void DrawSoundList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(360)))
        {
            EditorGUILayout.LabelField("Sound IDs", EditorStyles.boldLabel);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            foreach (SoundUsageDatabaseEntry entry in FilterEntries())
            {
                int uses = report.GetReferences(entry.Id).Count;
                string label = $"{entry.Category}  {entry.Id}  ({uses})";
                GUIStyle style = selectedId == entry.Id ? EditorStyles.helpBox : EditorStyles.miniButton;

                if (GUILayout.Button(label, style))
                    selectedId = entry.Id;
            }

            foreach (string missingId in report.MissingDatabaseEntryIds)
            {
                if (!MatchesFilter(missingId))
                    continue;

                GUIStyle style = selectedId == missingId ? EditorStyles.helpBox : EditorStyles.miniButton;
                if (GUILayout.Button($"Missing  {missingId}", style))
                    selectedId = missingId;
            }

            EditorGUILayout.Space(8);
            DrawVfxSoundListGroup("Player", "Player VFX Sounds");
            DrawVfxSoundListGroup("Monster", "Monster VFX Sounds");
            DrawMissingVfxSoundList();

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawVfxSoundListGroup(string group, string title)
    {
        IEnumerable<SoundUsageVfxSoundEntry> entries = report.VfxSoundEntries
            .Where(entry => entry.Group == group)
            .Where(entry => MatchesFilter(entry.VfxPath) || MatchesFilter(entry.ClipNames))
            .OrderBy(entry => entry.VfxPath, StringComparer.Ordinal);

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        foreach (SoundUsageVfxSoundEntry entry in entries)
        {
            string key = GetVfxKey(entry);
            string label = $"{entry.VfxName}  ({entry.CueCount})";
            GUIStyle style = selectedId == key ? EditorStyles.helpBox : EditorStyles.miniButton;

            if (GUILayout.Button(label, style))
                selectedId = key;
        }
    }

    private void DrawMissingVfxSoundList()
    {
        if (report.MissingVfxSoundPrefabPaths.Count == 0)
            return;

        EditorGUILayout.LabelField("Missing VFX Sounds", EditorStyles.boldLabel);

        foreach (string path in report.MissingVfxSoundPrefabPaths.OrderBy(path => path, StringComparer.Ordinal))
        {
            if (!MatchesFilter(path))
                continue;

            string key = MissingVfxKeyPrefix + path;
            GUIStyle style = selectedId == key ? EditorStyles.helpBox : EditorStyles.miniButton;

            if (GUILayout.Button($"Missing  {path}", style))
                selectedId = key;
        }
    }

    private void DrawDetails()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            if (string.IsNullOrWhiteSpace(selectedId))
            {
                EditorGUILayout.HelpBox("왼쪽에서 사운드 ID를 선택하세요.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (selectedId.StartsWith(VfxKeyPrefix, StringComparison.Ordinal))
            {
                DrawVfxSoundDetails(selectedId);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (selectedId.StartsWith(MissingVfxKeyPrefix, StringComparison.Ordinal))
            {
                DrawMissingVfxSoundDetails(selectedId.Substring(MissingVfxKeyPrefix.Length));
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField(selectedId, EditorStyles.boldLabel);
            DrawDatabaseEntryEditor(selectedId);
            DrawReferences(selectedId);
            DrawEmbeddedAudioSources();

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawVfxSoundDetails(string key)
    {
        SoundUsageVfxSoundEntry entry = report.VfxSoundEntries
            .FirstOrDefault(item => GetVfxKey(item) == key);

        if (entry == null)
        {
            EditorGUILayout.HelpBox("VFX 사운드 매핑을 찾지 못했습니다.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"{entry.Group} VFX Sound", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("VFX", entry.VfxPath);
        EditorGUILayout.LabelField("Clips", entry.ClipNames);

        UnityEngine.Object asset = UnityAssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.VfxPath);
        if (asset != null && GUILayout.Button("Ping VFX", GUILayout.Width(90)))
            EditorGUIUtility.PingObject(asset);

        DrawVfxSoundEntryEditor(entry);
        DrawEmbeddedAudioSources();
    }

    private void DrawMissingVfxSoundDetails(string path)
    {
        EditorGUILayout.LabelField("Missing VFX Sound", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("SkillVfxDatabase에서 사용하는 VFX지만 SoundDatabase에 재생 가능한 VFX 사운드 큐가 없습니다.", MessageType.Warning);
        EditorGUILayout.LabelField("VFX", path);

        UnityEngine.Object asset = UnityAssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (asset != null && GUILayout.Button("Ping VFX", GUILayout.Width(90)))
            EditorGUIUtility.PingObject(asset);

        DrawEmbeddedAudioSources();
    }

    private void DrawVfxSoundEntryEditor(SoundUsageVfxSoundEntry entry)
    {
        if (serializedDatabase == null)
        {
            EditorGUILayout.HelpBox("SerializedObject를 만들 수 없습니다.", MessageType.Warning);
            return;
        }

        SerializedProperty entryProperty = FindVfxEntryProperty(entry);
        if (entryProperty == null)
        {
            EditorGUILayout.HelpBox("SoundDatabase 안에서 VFX 매핑 위치를 찾지 못했습니다.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Database VFX Entry", EditorStyles.boldLabel);

        serializedDatabase.Update();
        DrawChild(entryProperty, "vfxPrefab");
        DrawChild(entryProperty, "cues");

        if (serializedDatabase.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            UnityAssetDatabase.SaveAssets();
            Refresh();
        }
    }

    private void DrawDatabaseEntryEditor(string id)
    {
        SoundUsageDatabaseEntry entry = report.DatabaseEntries.FirstOrDefault(e => e.Id == id);
        if (entry == null)
        {
            EditorGUILayout.HelpBox("DB에 없는 ID입니다. 참조 위치를 확인하고 SoundDatabase에 항목을 추가하세요.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Database Entry", EditorStyles.boldLabel);

        if (serializedDatabase == null)
        {
            EditorGUILayout.HelpBox("SerializedObject를 만들 수 없습니다.", MessageType.Warning);
            return;
        }

        SerializedProperty entryProperty = FindEntryProperty(entry);
        if (entryProperty == null)
        {
            EditorGUILayout.HelpBox("SoundDatabase 안에서 항목 위치를 찾지 못했습니다.", MessageType.Warning);
            return;
        }

        serializedDatabase.Update();
        DrawChild(entryProperty, "id");
        DrawChild(entryProperty, "aliases");
        DrawChild(entryProperty, "clip");
        DrawChild(entryProperty, "volume");
        DrawChild(entryProperty, "pitch");
        DrawChild(entryProperty, "loop");
        DrawChild(entryProperty, "useRandomPitch");
        DrawChild(entryProperty, "randomPitchMin");
        DrawChild(entryProperty, "randomPitchMax");

        if (serializedDatabase.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            UnityAssetDatabase.SaveAssets();
            Refresh();
        }
    }

    private void DrawReferences(string id)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);

        IReadOnlyList<SoundUsageReference> references = report.GetReferences(id);
        if (references.Count == 0)
        {
            EditorGUILayout.HelpBox("현재 참조되지 않는 DB 사운드입니다.", MessageType.Warning);
            return;
        }

        foreach (SoundUsageReference reference in references)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(reference.Context);
                EditorGUILayout.LabelField("Asset", reference.AssetPath);
                EditorGUILayout.LabelField("Member", reference.MemberPath);

                UnityEngine.Object asset = UnityAssetDatabase.LoadAssetAtPath<UnityEngine.Object>(reference.AssetPath);
                if (asset != null && GUILayout.Button("Ping Asset", GUILayout.Width(90)))
                    EditorGUIUtility.PingObject(asset);
            }
        }
    }

    private void DrawEmbeddedAudioSources()
    {
        if (report.EmbeddedAudioSources.Count == 0)
            return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Embedded AudioSources", EditorStyles.boldLabel);

        foreach (EmbeddedAudioSourceUsage source in report.EmbeddedAudioSources)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(source.AssetPath);
                EditorGUILayout.LabelField("Object", source.OwnerName);
                EditorGUILayout.LabelField("Path", source.MemberPath);
                EditorGUILayout.LabelField("Clip", source.ClipName);
                EditorGUILayout.LabelField(
                    "State",
                    $"enabled={source.Enabled}, playOnAwake={source.PlayOnAwake}, loop={source.Loop}, volume={source.Volume:0.###}, pitch={source.Pitch:0.###}");
            }
        }
    }

    private IEnumerable<SoundUsageDatabaseEntry> FilterEntries()
    {
        return report.DatabaseEntries
            .Where(entry => MatchesFilter(entry.Id) || MatchesFilter(entry.ClipName))
            .OrderBy(entry => entry.Category)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal);
    }

    private bool MatchesFilter(string value)
    {
        return string.IsNullOrWhiteSpace(filter) ||
            (!string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void Refresh()
    {
        database = UnityAssetDatabase.LoadAssetAtPath<SoundDatabase>(
            SoundUsageScanOptions.DefaultSoundDatabasePath);
        serializedDatabase = database != null ? new SerializedObject(database) : null;
        report = SoundUsageScanner.Scan(new SoundUsageScanOptions { Database = database });

        if (string.IsNullOrWhiteSpace(selectedId) &&
            report.DatabaseEntries.Count > 0)
        {
            selectedId = report.DatabaseEntries[0].Id;
        }
        else if (string.IsNullOrWhiteSpace(selectedId) &&
            report.VfxSoundEntries.Count > 0)
        {
            selectedId = GetVfxKey(report.VfxSoundEntries[0]);
        }

        Repaint();
    }

    private void GenerateReport()
    {
        Refresh();
        SoundUsageScanner.WriteMarkdownReport(report, SoundUsageScanOptions.DefaultReportPath);
        UnityAssetDatabase.Refresh();
        Debug.Log($"[SoundUsageBrowser] Report written: {SoundUsageScanOptions.DefaultReportPath}");
    }

    private SerializedProperty FindEntryProperty(SoundUsageDatabaseEntry entry)
    {
        string listName = entry.Category switch
        {
            SoundCategory.Bgm => "bgmList",
            _ => "sfxList"
        };

        SerializedProperty list = serializedDatabase.FindProperty(listName);
        if (list == null || !list.isArray)
            return null;

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            SerializedProperty id = element.FindPropertyRelative("id");
            if (id != null && id.stringValue == entry.Id)
                return element;
        }

        return null;
    }

    private SerializedProperty FindVfxEntryProperty(SoundUsageVfxSoundEntry entry)
    {
        string listName = entry.Group == "Player"
            ? "playerSkillVfxSfxList"
            : "monsterSkillVfxSfxList";

        SerializedProperty list = serializedDatabase.FindProperty(listName);
        if (list == null || !list.isArray)
            return null;

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            SerializedProperty prefab = element.FindPropertyRelative("vfxPrefab");
            string path = prefab != null && prefab.objectReferenceValue != null
                ? UnityAssetDatabase.GetAssetPath(prefab.objectReferenceValue)
                : "";

            if (path == entry.VfxPath)
                return element;
        }

        return null;
    }

    private static string GetVfxKey(SoundUsageVfxSoundEntry entry)
    {
        return VfxKeyPrefix + entry.Group + ":" + entry.VfxPath;
    }

    private static void DrawChild(SerializedProperty parent, string childName)
    {
        SerializedProperty child = parent.FindPropertyRelative(childName);
        if (child != null)
            EditorGUILayout.PropertyField(child, true);
    }
}
