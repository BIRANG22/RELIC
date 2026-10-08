using System;
using System.Collections.Generic;
using System.Linq;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SkillRewardIdAttribute))]
public sealed class SkillRewardIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        IReadOnlyList<string> ids = SkillRewardIdCatalog.GetIds();
        if (ids.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string current = property.stringValue?.Trim() ?? string.Empty;
        List<string> options = new(ids.Count + 2) { "(None)" };
        options.AddRange(ids);

        int selectedIndex = string.IsNullOrEmpty(current)
            ? 0
            : options.FindIndex(id => string.Equals(id, current, StringComparison.Ordinal));

        if (selectedIndex < 0)
        {
            options.Add($"Missing: {current}");
            selectedIndex = options.Count - 1;
        }

        int nextIndex = EditorGUI.Popup(position, label.text, selectedIndex, options.ToArray());
        if (nextIndex != selectedIndex)
            property.stringValue = nextIndex == 0 ? string.Empty : options[nextIndex];
    }
}

[CustomEditor(typeof(SkillRewardPoolDatabase))]
public sealed class SkillRewardPoolDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        SkillRewardPoolDatabase database = (SkillRewardPoolDatabase)target;
        if (!database.RestrictRewards)
            return;

        IReadOnlyList<string> knownIds = SkillRewardIdCatalog.GetIds();
        if (knownIds.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "GameDataRuntime에서 스킬 ID 목록을 읽지 못했습니다. 문자열을 직접 확인하세요.",
                MessageType.Warning);
            return;
        }

        HashSet<string> known = new(knownIds, StringComparer.Ordinal);
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<string> missing = new();
        List<string> duplicates = new();

        foreach (string rawId in database.AllowedSkillIds)
        {
            if (string.IsNullOrWhiteSpace(rawId))
                continue;

            string id = rawId.Trim();
            if (!known.Contains(id))
                missing.Add(id);
            if (!seen.Add(id))
                duplicates.Add(id);
        }

        if (missing.Count > 0)
            EditorGUILayout.HelpBox($"존재하지 않거나 보상 대상이 아닌 ID: {string.Join(", ", missing.Distinct())}", MessageType.Error);

        if (duplicates.Count > 0)
            EditorGUILayout.HelpBox($"중복 ID: {string.Join(", ", duplicates.Distinct())}", MessageType.Warning);

        if (database.AllowedSkillIds.Count == 0)
            EditorGUILayout.HelpBox("제한이 켜져 있지만 허용 ID가 없어 스킬 보상이 생성되지 않습니다.", MessageType.Warning);
    }
}

internal static class SkillRewardIdCatalog
{
    private const string RuntimeDataPath = "Assets/Resources/Data/GameDataRuntime.csv";
    private static Hash128 cachedHash;
    private static IReadOnlyList<string> cachedIds = Array.Empty<string>();

    public static IReadOnlyList<string> GetIds()
    {
        Hash128 currentHash = UnityEditor.AssetDatabase.GetAssetDependencyHash(RuntimeDataPath);
        if (currentHash == cachedHash && cachedIds.Count > 0)
            return cachedIds;

        TextAsset data = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(RuntimeDataPath);
        if (data == null)
            return Array.Empty<string>();

        try
        {
            Dictionary<string, List<Dictionary<string, string>>> workbook =
                ExcelWorkbookReader.Read(data.bytes);

            cachedIds = SkillCsvLoader.LoadSkills(workbook)
                .Where(skill =>
                    skill != null &&
                    skill.Category == Category.Core &&
                    !string.IsNullOrWhiteSpace(skill.SkillId) &&
                    SkillRarityUtility.IsBaseSkillVariant(skill.SkillId))
                .Select(skill => skill.SkillId.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
            cachedHash = currentHash;
            return cachedIds;
        }
        catch (Exception)
        {
            cachedIds = Array.Empty<string>();
            cachedHash = currentHash;
            return cachedIds;
        }
    }
}
