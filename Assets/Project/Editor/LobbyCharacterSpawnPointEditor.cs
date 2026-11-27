using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LobbyCharacterSpawnPoint))]
public sealed class LobbyCharacterSpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("위치·회전·크기는 이 오브젝트의 Transform에서 설정합니다. 표시 옵션은 다음 생성 시 적용됩니다. 부모의 Spawn Points 목록에 이 컴포넌트를 연결하세요.", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("database"), new GUIContent("Character Prefab DB"));
        DrawCharacterId();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("flipX"), new GUIContent("Flip X (앞 스프라이트)"));
        DrawSortingLayer();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("frontSortingOrder"), new GUIContent("Front Order in Layer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("backSortingOrder"), new GUIContent("Back Order in Layer"));
        EditorGUILayout.Space();
        var overrideLayer = serializedObject.FindProperty("overrideLayer");
        EditorGUILayout.PropertyField(overrideLayer, new GUIContent("Override GameObject Layer"));
        if (overrideLayer.boolValue)
        {
            var layer = serializedObject.FindProperty("objectLayer");
            layer.intValue = EditorGUILayout.LayerField("GameObject Layer", layer.intValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyLayerToChildren"), new GUIContent("Apply Layer to Children"));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverNameType"), new GUIContent("Hover Name"));
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCharacterId()
    {
        var property = serializedObject.FindProperty("characterId");
        var database = serializedObject.FindProperty("database").objectReferenceValue;
        if (database == null)
        {
            EditorGUILayout.PropertyField(property);
            EditorGUILayout.HelpBox("Character Prefab DB를 연결하면 캐릭터 목록을 선택할 수 있습니다.", MessageType.Warning);
            return;
        }
        var ids = new List<string> { string.Empty };
        var labels = new List<string> { "(선택 안 함)" };
        using (var db = new SerializedObject(database))
        {
            var entries = db.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                string id = entry.FindPropertyRelative("CharacterId").stringValue;
                if (string.IsNullOrWhiteSpace(id) || ids.Contains(id))
                    continue;
                ids.Add(id);
                labels.Add(entry.FindPropertyRelative("LobbyPrefab").objectReferenceValue != null
                    ? id : id + " (LobbyPrefab 없음)");
            }
        }
        int selected = ids.IndexOf(property.stringValue);
        if (selected < 0)
        {
            selected = ids.Count;
            ids.Add(property.stringValue);
            labels.Add(property.stringValue + " (DB에 없음)");
        }
        int next = EditorGUILayout.Popup("Character ID", selected, labels.ToArray());
        if (next != selected)
            property.stringValue = ids[next];
    }

    private void DrawSortingLayer()
    {
        var property = serializedObject.FindProperty("sortingLayerId");
        var ids = new List<int>();
        var names = new List<string>();
        foreach (SortingLayer layer in SortingLayer.layers)
        {
            ids.Add(layer.id);
            names.Add(layer.name);
        }
        int selected = ids.IndexOf(property.intValue);
        if (selected < 0)
        {
            selected = ids.Count;
            ids.Add(property.intValue);
            names.Add($"없는 레이어 ({property.intValue})");
        }
        int next = EditorGUILayout.Popup("Sorting Layer", selected, names.ToArray());
        if (next != selected)
            property.intValue = ids[next];
    }
}
