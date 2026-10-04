using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

/// <summary>
/// 데이터 바인딩이나 런타임 프리팹이 TMP 문자열을 직접 대입해도,
/// Text 표의 한국어 원문과 일치하면 즉시 LocalizedTMPText로 전환합니다.
/// </summary>
public static class RuntimeTMPTextAutoLocalizer
{
    private static LocalizationBindingResolver resolver = new(Array.Empty<LocalizationBindingEntry>());
    private static readonly HashSet<int> ProcessingTextIds = new();
    private static bool isReady;

    public static bool ShouldProcessTextChange(bool isPlaying) => isPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static async void Initialize()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

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

        // 초기화가 끝나기 전에 값이 쓰인 동적 출력도 현재 언어로 맞춥니다.
        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            TryAttach(text);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isReady || !scene.IsValid())
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                TryAttach(text);
    }

    private static void OnTextChanged(UnityEngine.Object changedObject)
    {
        if (changedObject is not TMP_Text text)
            return;

        TryAttach(text);
    }

    private static void TryAttach(TMP_Text text)
    {
        if (!ShouldProcessTextChange(Application.isPlaying))
            return;

        if (text == null)
            return;

        if (!isReady)
            return;

        if (text.GetComponentInParent<LocalizationAutoBindingIgnore>(true) != null)
            return;

        DynamicLocalizedTMPText dynamicLocalizer = text.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicLocalizer != null && dynamicLocalizer.IsApplyingLocalization)
            return;

        if (text.GetComponentInParent<LocalizationIgnore>(true) != null)
        {
            TryAttachDynamic(text);
            return;
        }

        if (!LocalizedTMPText.ShouldManageText(text))
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

    private static void TryAttachDynamic(TMP_Text text)
    {
        if (text == null || !isReady || text.GetComponentInParent<TMP_Dropdown>() != null)
            return;

        if (text.GetComponentInParent<LocalizationAutoBindingIgnore>(true) != null)
            return;

        if (text.GetComponentInParent<LocalizationIgnore>(true) == null)
            return;

        DynamicLocalizedTMPText localizer = text.GetComponent<DynamicLocalizedTMPText>();
        if (localizer != null && localizer.IsApplyingLocalization)
            return;

        int instanceId = text.GetInstanceID();
        if (!ProcessingTextIds.Add(instanceId))
            return;

        try
        {
            string koreanSource = NormalizeKoreanSource(text.text);
            if (!LocalizationTextRules.IsKoreanPlayerText(koreanSource))
                return;

            if (localizer == null)
                localizer = text.gameObject.AddComponent<DynamicLocalizedTMPText>();

            localizer.Configure(resolver, koreanSource);
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

public sealed class DynamicLocalizationSourceState
{
    private readonly LocalizationBindingResolver resolver;

    public DynamicLocalizationSourceState(LocalizationBindingResolver resolver)
    {
        this.resolver = resolver ?? new LocalizationBindingResolver(Array.Empty<LocalizationBindingEntry>());
    }

    public string LocalizationKey { get; private set; } = string.Empty;
    public string KoreanSource { get; private set; } = string.Empty;

    public bool UpdateSource(string source)
    {
        string normalizedSource = RuntimeTMPTextAutoLocalizer.NormalizeKoreanSource(source);
        LocalizationKeyResolution resolution = resolver.ResolveSource(normalizedSource);
        if (!LocalizationTextRules.IsKoreanPlayerText(normalizedSource) || !resolution.IsUnique)
        {
            LocalizationKey = string.Empty;
            KoreanSource = string.Empty;
            return false;
        }

        LocalizationKey = resolution.Key;
        KoreanSource = normalizedSource;
        return true;
    }
}
