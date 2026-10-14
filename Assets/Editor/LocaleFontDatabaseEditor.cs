using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocaleFontDatabase))]
public sealed class LocaleFontDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        LocaleFontDatabase.ValidationIssue[] issues = ((LocaleFontDatabase)target).Validate();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        if (issues.Length == 0)
        {
            EditorGUILayout.HelpBox("Locale and role mappings are valid.", MessageType.Info);
            return;
        }

        for (int i = 0; i < issues.Length; i++)
            EditorGUILayout.HelpBox(issues[i].Message, MessageType.Warning);
    }
}
