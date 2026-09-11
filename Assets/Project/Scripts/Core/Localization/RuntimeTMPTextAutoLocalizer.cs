using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>
/// 데이터 바인딩이나 런타임 프리팹이 TMP 문자열을 직접 대입해도,
/// Text 표의 한국어 원문과 일치하면 즉시 LocalizedTMPText로 전환합니다.
/// </summary>
public static class RuntimeTMPTextAutoLocalizer
{
    private static LocalizationBindingResolver resolver = new(Array.Empty<LocalizationBindingEntry>());
    private static readonly HashSet<int> ProcessingTextIds = new();
    private static readonly HashSet<int> SceneStartupTextIds = new();
    private static bool isReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static async void Initialize()
    {
        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            if (text != null)
                SceneStartupTextIds.Add(text.GetInstanceID());

        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);

        await LocalizationSettings.InitializationOperation.Task;
        var koreanLocale = LocalizationSettings.AvailableLocales.Locales
            .FirstOrDefault(locale => locale.Identifier.Code.StartsWith("ko", StringComparison.OrdinalIgnoreCase));
        if (koreanLocale == null)
            return;

        StringTable koreanTable = await LocalizationSettings.StringDatabase
            .GetTableAsync(GameLocalization.TableName, koreanLocale).Task;
        if (koreanTable == null)
            return;

        resolver = new LocalizationBindingResolver(koreanTable.Values
            .Select(entry => new LocalizationBindingEntry(entry.Key, NormalizeKoreanSource(entry.Value))));

        isReady = true;
    }

    private static void OnTextChanged(UnityEngine.Object changedObject)
    {
        if (changedObject is not TMP_Text text)
            return;

        if (SceneStartupTextIds.Contains(text.GetInstanceID()))
            return;

        TryAttach(text);
    }

    private static void TryAttach(TMP_Text text)
    {
        if (text == null)
            return;

        if (!isReady)
            return;

        if (!LocalizedTMPText.ShouldManageText(text))
            return;

        if (text.GetComponent<LocalizedTMPText>() != null)
            return;

        int instanceId = text.GetInstanceID();
        if (!ProcessingTextIds.Add(instanceId))
            return;

        try
        {
            string koreanSource = NormalizeKoreanSource(text.text);
            LocalizationKeyResolution resolution = resolver.ResolveSource(koreanSource);
            if (!LocalizationTextRules.IsKoreanPlayerText(koreanSource) || !resolution.IsUnique)
                return;

            LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
            if (localizer == null)
                localizer = text.gameObject.AddComponent<LocalizedTMPText>();

            if (!string.Equals(localizer.LocalizationKey, resolution.Key, StringComparison.Ordinal) ||
                !string.Equals(localizer.KoreanSource, koreanSource, StringComparison.Ordinal))
                localizer.Configure(resolution.Key, koreanSource, true);
        }
        finally
        {
            ProcessingTextIds.Remove(instanceId);
        }
    }

    public static string NormalizeKoreanSource(string source)
    {
        return string.IsNullOrWhiteSpace(source)
            ? string.Empty
            : source.Replace("\r\n", "\n").Trim();
    }

}
