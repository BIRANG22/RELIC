using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SoundIdAttribute))]
public sealed class SoundIdDrawer : PropertyDrawer
{
    private const string EmptyOption = "(None)";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SoundIdAttribute soundId = (SoundIdAttribute)attribute;

        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.HelpBox(
                position,
                "[SoundId] can only be used on string fields.",
                MessageType.Error);
            return;
        }

        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(soundId.DatabasePath);
        if (database == null)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        IReadOnlyList<string> ids = GetSoundIds(database, soundId.Category);
        DrawSoundIdPopup(position, property, label, ids);
    }

    internal static IReadOnlyList<string> GetSoundIdsForTest(
        SoundDatabase database,
        SoundCategory category)
    {
        return GetSoundIds(database, category);
    }

    private static IReadOnlyList<string> GetSoundIds(
        SoundDatabase database,
        SoundCategory category)
    {
        if (database == null)
            return Array.Empty<string>();

        IReadOnlyList<SoundData> entries = category switch
        {
            SoundCategory.Bgm => database.BgmEntries,
            SoundCategory.SkillSfx => database.SkillSfxEntries,
            _ => database.SfxEntries
        };

        if (entries == null)
            return Array.Empty<string>();

        return entries
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.id))
            .Select(entry => entry.id.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    private static void DrawSoundIdPopup(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        IReadOnlyList<string> ids)
    {
        string currentValue = property.stringValue ?? string.Empty;
        List<string> options = new() { EmptyOption };
        options.AddRange(ids);

        int selectedIndex = string.IsNullOrEmpty(currentValue)
            ? 0
            : options.FindIndex(option => option == currentValue);

        if (selectedIndex < 0)
        {
            options.Add($"Missing: {currentValue}");
            selectedIndex = options.Count - 1;
        }

        int nextIndex = EditorGUI.Popup(position, label.text, selectedIndex, options.ToArray());
        if (nextIndex == selectedIndex)
            return;

        property.stringValue = nextIndex == 0 ? string.Empty : options[nextIndex];
    }
}
