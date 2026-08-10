using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;

public class RecordPrefabLocalizationTests
{
    private const string RecordPrefabPath = "Assets/Project/PrefabsR/Record.prefab";

    [Test]
    public void RecordInfoNameText_DoesNotHaveStaticLocalizeStringEvent()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RecordPrefabPath);
        Assert.That(prefab, Is.Not.Null);

        RecordPanelUI panel = prefab.GetComponent<RecordPanelUI>();
        Assert.That(panel, Is.Not.Null);

        var serializedPanel = new SerializedObject(panel);
        SerializedProperty nameTextProperty = serializedPanel.FindProperty("nameText");
        TMP_Text nameText = nameTextProperty.objectReferenceValue as TMP_Text;

        Assert.That(nameText, Is.Not.Null);
        Assert.That(
            nameText.GetComponent<LocalizeStringEvent>(),
            Is.Null,
            "Record Info/Name is a dynamic selected-data label; a static Text/common.name localizer overwrites item names.");
    }
}
