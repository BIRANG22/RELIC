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
    private static readonly Dictionary<string, string> KeyByKoreanSource = new(StringComparer.Ordinal);
    private static readonly HashSet<int> ProcessingTextIds = new();
    private static bool isReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static async void Initialize()
    {
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

        KeyByKoreanSource.Clear();
        foreach (StringTableEntry entry in koreanTable.Values)
        {
            string koreanSource = NormalizeKoreanSource(entry.Value);
            if (!string.IsNullOrWhiteSpace(koreanSource) && !KeyByKoreanSource.ContainsKey(koreanSource))
                KeyByKoreanSource.Add(koreanSource, entry.Key);
        }

        isReady = true;
        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            TryAttach(text);
    }

    private static void OnTextChanged(UnityEngine.Object changedObject)
    {
        if (changedObject is TMP_Text text)
            TryAttach(text);
    }

    private static void TryAttach(TMP_Text text)
    {
        if (!isReady || text == null || !LocalizedTMPText.ShouldManageText(text))
            return;

        int instanceId = text.GetInstanceID();
        if (!ProcessingTextIds.Add(instanceId))
            return;

        try
        {
            string koreanSource = NormalizeKoreanSource(text.text);
            if (!LocalizationTextRules.IsKoreanPlayerText(koreanSource) ||
                !KeyByKoreanSource.TryGetValue(koreanSource, out string key))
                return;

            LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
            if (localizer == null)
                localizer = text.gameObject.AddComponent<LocalizedTMPText>();

            if (!string.Equals(localizer.LocalizationKey, key, StringComparison.Ordinal) ||
                !string.Equals(localizer.KoreanSource, koreanSource, StringComparison.Ordinal))
                localizer.Configure(key, koreanSource, true);
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
