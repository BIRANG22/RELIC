using TMPro;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedTMPText : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string localizationKey;
    [SerializeField, TextArea] private string koreanSource;
    [SerializeField] private bool automaticallyRegistered;
    private readonly LocalizationRefreshGate refreshGate = new();

    public string LocalizationKey => localizationKey;
    public string KoreanSource => koreanSource;

    public static bool ShouldManageText(TMP_Text text)
    {
        // Dropdown의 Caption/Item은 TMP_Dropdown이 선택값과 목록값을 직접 작성합니다.
        // 여기에 정적 LocalizedTMPText가 붙으면 선택 언어명이 번역 키 값으로 덮어써집니다.
        return text != null &&
               text.GetComponentInParent<TMP_Dropdown>() == null &&
               text.GetComponentInParent<LocalizationIgnore>(true) == null;
    }

    public static string ResolveText(string key, string koreanFallback)
    {
        return string.IsNullOrWhiteSpace(key)
            ? koreanFallback ?? string.Empty
            : GameLocalization.Get(key, koreanFallback);
    }

    public void Configure(string key, string fallback, bool registeredAutomatically)
    {
        localizationKey = key ?? string.Empty;
        koreanSource = fallback ?? string.Empty;
        automaticallyRegistered = registeredAutomatically;
        if (isActiveAndEnabled)
            RefreshWhenReady(LocalizationSettings.SelectedLocale);
        else
            Refresh();
    }

    private void Reset()
    {
        target = GetComponent<TMP_Text>();
        koreanSource = target != null ? target.text : string.Empty;
    }

    private void OnEnable()
    {
        if (target == null)
            target = GetComponent<TMP_Text>();

        DisableLegacyWriter();

        if (!ShouldManageText(target))
            return;

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshWhenReady(LocalizationSettings.SelectedLocale);
    }

    private void OnDisable()
    {
        refreshGate.Invalidate();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (!ShouldManageText(target) || !isActiveAndEnabled || string.IsNullOrWhiteSpace(localizationKey))
            return;

        RefreshWhenReady(locale);
    }

    private async void RefreshWhenReady(Locale requestedLocale)
    {
        int requestVersion = refreshGate.Begin();

        try
        {
            await LocalizationSettings.InitializationOperation.Task;
            if (!CanApplyRefresh(requestVersion))
                return;

            requestedLocale ??= LocalizationSettings.SelectedLocale;
            if (string.IsNullOrWhiteSpace(localizationKey))
                return;

            await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync(
                    GameLocalization.TableName,
                    localizationKey,
                    requestedLocale,
                    FallbackBehavior.DontUseFallback)
                .Task;
        }
        catch
        {
            // Refresh below applies the selected locale's missing-translation marker.
        }

        if (CanApplyRefresh(requestVersion) &&
            (requestedLocale == null ||
             LocalizationSettings.SelectedLocale?.Identifier == requestedLocale.Identifier))
            RefreshForLocale(requestedLocale);
    }

    public void Refresh()
    {
        RefreshForLocale(LocalizationSettings.SelectedLocale);
    }

    public void RefreshForLocale(Locale locale)
    {
        if (target == null)
            target = GetComponent<TMP_Text>();

        if (!ShouldManageText(target))
            return;

        target.text = string.IsNullOrWhiteSpace(localizationKey)
            ? koreanSource ?? string.Empty
            : GameLocalization.GetForLocale(localizationKey, koreanSource, locale);
    }

    private void DisableLegacyWriter()
    {
        LocalizeStringEvent legacyWriter = GetComponent<LocalizeStringEvent>();
        if (legacyWriter != null && legacyWriter.enabled)
            legacyWriter.enabled = false;
    }

    private bool CanApplyRefresh(int requestVersion) =>
        refreshGate.IsCurrent(requestVersion) &&
        isActiveAndEnabled &&
        ShouldManageText(target);
}

public sealed class LocalizationRefreshGate
{
    private int version;

    public int Begin() => ++version;

    public void Invalidate() => version++;

    public bool IsCurrent(int requestVersion) => requestVersion == version;
}

/// <summary>
/// 언어 변경 이벤트를 놓쳤거나 같은 프레임에 다른 UI 작성자가 덮어쓴 TMP까지
/// 선택 로케일의 최종 표시 상태로 다시 맞춥니다.
/// </summary>
public static class LocalizationRuntimeRefreshCoordinator
{
    private static int refreshVersion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        refreshVersion++;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private static async void OnLocaleChanged(Locale locale)
    {
        int requestVersion = ++refreshVersion;

        try
        {
            await LocalizationSettings.InitializationOperation.Task;
            locale ??= LocalizationSettings.SelectedLocale;
            await LocalizationSettings.StringDatabase
                .GetTableAsync(GameLocalization.TableName, locale)
                .Task;
        }
        catch (Exception)
        {
            // 개별 로컬라이저가 요청 로케일의 표준 미번역 표기를 적용합니다.
        }

        if (!CanApply(requestVersion, locale))
            return;

        RefreshAllNow(locale);

        // 동일 프레임의 UI presenter가 한국어 원문을 다시 쓴 경우까지 최종 정리합니다.
        await Task.Yield();
        if (CanApply(requestVersion, locale))
            RefreshAllNow(locale);
    }

    public static void RefreshAllNow(Locale locale)
    {
        foreach (LocalizedTMPText localizer in UnityEngine.Object.FindObjectsByType<LocalizedTMPText>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (localizer != null)
                localizer.RefreshForLocale(locale);
        }

        foreach (DynamicLocalizedTMPText localizer in UnityEngine.Object.FindObjectsByType<DynamicLocalizedTMPText>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (localizer != null)
                localizer.RefreshForLocale(locale);
        }
    }

    private static bool CanApply(int requestVersion, Locale locale)
    {
        return requestVersion == refreshVersion &&
               (locale == null || LocalizationSettings.SelectedLocale?.Identifier == locale.Identifier);
    }
}
