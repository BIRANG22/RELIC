using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// LocalizationIgnore가 붙은 반복 변경 TMP의 마지막 한국어 원문/키를 보관하고,
/// 값 변경 및 언어 변경 시 현재 로케일 문자열을 다시 적용합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class DynamicLocalizedTMPText : MonoBehaviour
{
    private TMP_Text target;
    private DynamicLocalizationSourceState sourceState;
    private readonly LocalizationRefreshGate refreshGate = new();

    public bool IsApplyingLocalization { get; private set; }

    public void Configure(LocalizationBindingResolver sourceResolver, string koreanSource)
    {
        target ??= GetComponent<TMP_Text>();
        sourceState = new DynamicLocalizationSourceState(sourceResolver);
        if (sourceState.UpdateSource(koreanSource))
            RefreshImmediatelyAndWhenReady(LocalizationSettings.SelectedLocale);
    }

    private void OnEnable()
    {
        target ??= GetComponent<TMP_Text>();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        if (sourceState != null && !string.IsNullOrWhiteSpace(sourceState.LocalizationKey))
            RefreshImmediatelyAndWhenReady(LocalizationSettings.SelectedLocale);
    }

    private void OnDisable()
    {
        refreshGate.Invalidate();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        if (!isActiveAndEnabled || sourceState == null || string.IsNullOrWhiteSpace(sourceState.LocalizationKey))
            return;

        RefreshImmediatelyAndWhenReady(locale);
    }

    private void RefreshImmediatelyAndWhenReady(UnityEngine.Localization.Locale requestedLocale)
    {
        RefreshForLocale(requestedLocale);
        RefreshWhenReady(requestedLocale);
    }

    private async void RefreshWhenReady(UnityEngine.Localization.Locale requestedLocale)
    {
        int requestVersion = refreshGate.Begin();
        try
        {
            await LocalizationSettings.InitializationOperation.Task;
            if (!CanApplyRefresh(requestVersion))
                return;

            requestedLocale ??= LocalizationSettings.SelectedLocale;
            if (requestedLocale == null)
                return;

            await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync(
                    GameLocalization.TableName,
                    sourceState.LocalizationKey,
                    requestedLocale,
                    UnityEngine.Localization.Settings.FallbackBehavior.DontUseFallback)
                .Task;
        }
        catch
        {
            // 아래 Refresh가 선택 언어의 표준 미번역 표기를 적용합니다.
        }

        if (CanApplyRefresh(requestVersion) &&
            (requestedLocale == null ||
             LocalizationSettings.SelectedLocale?.Identifier == requestedLocale.Identifier))
            RefreshForLocale(requestedLocale);
    }

    public void RefreshForLocale(UnityEngine.Localization.Locale locale)
    {
        target ??= GetComponent<TMP_Text>();
        if (target == null ||
            target.GetComponentInParent<LocalizationAutoBindingIgnore>(true) != null ||
            sourceState == null ||
            string.IsNullOrWhiteSpace(sourceState.LocalizationKey))
            return;

        IsApplyingLocalization = true;
        try
        {
            target.text = GameLocalization.GetForLocale(
                sourceState.LocalizationKey,
                sourceState.KoreanSource,
                locale);
        }
        finally
        {
            IsApplyingLocalization = false;
        }
    }

    private bool CanApplyRefresh(int requestVersion) =>
        refreshGate.IsCurrent(requestVersion) &&
        isActiveAndEnabled &&
        target != null &&
        sourceState != null &&
        !string.IsNullOrWhiteSpace(sourceState.LocalizationKey);
}
