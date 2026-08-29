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

            EditorGUILayout.EndScrollView();
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

            EditorGUILayout.LabelField(selectedId, EditorStyles.boldLabel);
            DrawDatabaseEntryEditor(selectedId);
            DrawReferences(selectedId);
            DrawEmbeddedAudioSources();

            EditorGUILayout.EndScrollView();
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
            SoundCategory.SkillSfx => "skillSfxList",
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

    private static void DrawChild(SerializedProperty parent, string childName)
    {
        SerializedProperty child = parent.FindPropertyRelative(childName);
        if (child != null)
            EditorGUILayout.PropertyField(child, true);
    }
}
