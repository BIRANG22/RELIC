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

    public bool IsApplyingLocalization { get; private set; }

    public void Configure(LocalizationBindingResolver sourceResolver, string koreanSource)
    {
        target ??= GetComponent<TMP_Text>();
        sourceState = new DynamicLocalizationSourceState(sourceResolver);
        if (sourceState.UpdateSource(koreanSource))
            Refresh();
    }

    private void OnEnable()
    {
        target ??= GetComponent<TMP_Text>();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private async void OnLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        if (!isActiveAndEnabled || sourceState == null || string.IsNullOrWhiteSpace(sourceState.LocalizationKey))
            return;

        Refresh();
        try
        {
            await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync(GameLocalization.TableName, sourceState.LocalizationKey)
                .Task;
            if (isActiveAndEnabled && LocalizationSettings.SelectedLocale?.Identifier == locale.Identifier)
                Refresh();
        }
        catch
        {
            // 즉시 적용한 표준 fallback을 유지합니다.
        }
    }

    private void Refresh()
    {
        if (target == null || sourceState == null || string.IsNullOrWhiteSpace(sourceState.LocalizationKey))
            return;

        IsApplyingLocalization = true;
        try
        {
            target.text = GameLocalization.Get(sourceState.LocalizationKey, sourceState.KoreanSource);
        }
        finally
        {
            IsApplyingLocalization = false;
        }
    }
}
