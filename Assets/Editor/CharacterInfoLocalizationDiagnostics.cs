using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

public static class CharacterInfoLocalizationDiagnostics
{
    [MenuItem("Tools/Localization/Diagnose Character InfoArea")]
    public static void Diagnose()
    {
        SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene lobby = EditorSceneManager.OpenScene("Assets/Project/Scenes/YDM/Lobby.unity", OpenSceneMode.Single);
            Transform settingPanel = lobby.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(candidate => Path(candidate) == "Canvas/PositionPanel/CharacterSettingPanel");
            if (settingPanel == null) { Debug.LogError("[Character InfoArea Localization] CharacterSettingPanel not found."); return; }

            string[] names = { "InfoText", "Infotext_1", "Infotext_2", "Infotext_3", "Infotext_4", "CharacterInfoText", "CharacterinfoText" };
            foreach (TMP_Text text in settingPanel.GetComponentsInChildren<TMP_Text>(true).Where(candidate => names.Contains(candidate.name)))
            {
                var local = text.GetComponent<LocalizedTMPText>();
                string writers = string.Join(", ", Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .SelectMany(component => ReferenceFields(component, text).Select(field => component.GetType().Name + "." + field)));
                Debug.Log($"[Character InfoArea Localization] Path={Path(text.transform)} | Name={text.name} | TMP={text.GetType().Name} | Text='{text.text}' | Active={text.gameObject.activeSelf}/{text.gameObject.activeInHierarchy} | LocalizedTMPText={local != null} Key='{local?.LocalizationKey}' KoreanSource='{local?.KoreanSource}' | LocalizationIgnore={text.GetComponent<LocalizationIgnore>() != null} | LocalizeStringEvent={text.GetComponent<LocalizeStringEvent>() != null} | WriterReferences=[{writers}] | FinalWriter={(local != null ? "LocalizedTMPText" : text.GetComponent<LocalizeStringEvent>() != null ? "LocalizeStringEvent" : "None")}");
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
    }

    private static System.Collections.Generic.IEnumerable<string> ReferenceFields(MonoBehaviour component, TMP_Text text)
    {
        for (System.Type type = component.GetType(); type != null; type = type.BaseType)
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            if (ReferenceEquals(field.GetValue(component), text))
                yield return field.Name;
    }
    private static string Path(Transform value) => value.parent == null ? value.name : Path(value.parent) + "/" + value.name;
}
