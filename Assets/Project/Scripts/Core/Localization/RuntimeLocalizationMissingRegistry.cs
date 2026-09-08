using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class RuntimeLocalizationMissingRegistry
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const string PlayerPrefsKey = "Localization.RuntimeMissing.v1";
    private static readonly HashSet<int> ReportedTextInstances = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private static void OnTextChanged(UnityEngine.Object changedObject)
    {
        if (changedObject is not TMP_Text text ||
            !LocalizationTextRules.IsKoreanPlayerText(text.text) ||
            text.GetComponent<LocalizedTMPText>() != null ||
            !ReportedTextInstances.Add(text.GetInstanceID()))
            return;

        string scene = text.gameObject.scene.IsValid() ? text.gameObject.scene.name : "Unknown";
        string record = $"{scene}|{text.gameObject.name}|{text.GetType().Name}|{text.text}";
        string previous = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (!previous.Contains(record, StringComparison.Ordinal))
        {
            PlayerPrefs.SetString(PlayerPrefsKey, previous + record + "\n");
            PlayerPrefs.Save();
            Debug.LogWarning($"[Runtime Localization Missing] Scene: {scene}, Object: {text.gameObject.name}, Text: {text.text}");
        }
    }
#endif
}

public static class LocalizationTextRules
{
    public static bool IsKoreanPlayerText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (char character in value)
        {
            if (character >= 0xAC00 && character <= 0xD7A3)
                return true;
        }

        return false;
    }
}
