using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedTMPText : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string localizationKey;
    [SerializeField, TextArea] private string koreanSource;
    [SerializeField] private bool automaticallyRegistered;

    public string LocalizationKey => localizationKey;
    public string KoreanSource => koreanSource;

    public static bool ShouldManageText(TMP_Text text)
    {
        // Dropdown의 Caption/Item은 TMP_Dropdown이 선택값과 목록값을 직접 작성합니다.
        // 여기에 정적 LocalizedTMPText가 붙으면 선택 언어명이 번역 키 값으로 덮어써집니다.
        return text != null && text.GetComponentInParent<TMP_Dropdown>() == null;
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

        if (!ShouldManageText(target))
            return;

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        if (LocalizationSettings.InitializationOperation.IsDone)
            Refresh();
    }

    private async void Start()
    {
        if (!ShouldManageText(target))
            return;

        await LocalizationSettings.InitializationOperation.Task;
        if (isActiveAndEnabled)
            Refresh();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!ShouldManageText(target) || !LocalizationSettings.InitializationOperation.IsDone)
            return;

        target.text = ResolveText(localizationKey, koreanSource);
    }
}
