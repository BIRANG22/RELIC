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

    private static bool IsBlurReplica(Transform transform)
    {
        // 블러 복제본은 원본의 번역 결과를 그대로 복사하므로 누락 검사 대상이 아닙니다.
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name.EndsWith("_BlurReplica", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static void OnTextChanged(UnityEngine.Object changedObject)
    {
        if (!RuntimeTMPTextAutoLocalizer.ShouldProcessTextChange(Application.isPlaying) ||
            changedObject is not TMP_Text text ||
            IsBlurReplica(text.transform) ||
            !LocalizationTextRules.IsKoreanPlayerText(text.text) ||
            !LocalizedTMPText.ShouldManageText(text) ||
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
    private static readonly string[] MissingTranslationMarkers =
    {
        "미번역",
        "Untranslated",
        "未翻訳",
        "未翻译",
        "Sin traducir",
    };

    public static bool IsKoreanPlayerText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (IsMissingTranslationMarker(value))
            return false;

        foreach (char character in value)
        {
            if (character >= 0xAC00 && character <= 0xD7A3)
                return true;
        }

        return false;
    }

    public static bool IsMissingTranslationMarker(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        foreach (string marker in MissingTranslationMarkers)
            if (string.Equals(normalized, marker, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
