using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageDropdownUI : MonoBehaviour
{
    [Header("Dropdown")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Language Preview Objects")]
    [SerializeField] private GameObject krObject;
    [SerializeField] private GameObject enObject;
    [SerializeField] private GameObject scObject;
    [SerializeField] private GameObject jpObject;
    [SerializeField] private GameObject spObject;

    [Header("Template Sorting")]
    [SerializeField] private int dropdownSortingOrderOffset = 50;

    private const string SaveKey = "SelectedLanguage";

    private readonly List<string> localeCodes = new()
    {
        "ko",
        "en",
        "zh-Hans",
        "ja",
        "es"
    };

    private bool isInitialized;

    private void Awake()
    {
        DirectTemplateDropdown.Attach(languageDropdown)
            ?.Configure(dropdownSortingOrderOffset);
    }

    private async void Start()
    {
        await LocalizationSettings.InitializationOperation.Task;

        SetupDropdownOptions();
        SyncDropdownFromCurrentLocale();
        RefreshLanguagePreview(languageDropdown.value);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }
    }

    private void SetupDropdownOptions()
    {
        if (languageDropdown == null)
        {
            Debug.LogError("[LanguageDropdownUI] languageDropdown is null.");
            return;
        }

        languageDropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>
        {
            new("한국어"),
            new("English"),
            new("中文"),
            new("日本語"),
            new("Español")
        };

        languageDropdown.AddOptions(options);
    }

    private void SyncDropdownFromCurrentLocale()
    {
        string currentCode = LocalizationSettings.SelectedLocale.Identifier.Code;
        int index = localeCodes.IndexOf(currentCode);

        if (index < 0)
        {
            index = 0;
        }

        languageDropdown.SetValueWithoutNotify(index);
    }

    private void OnLanguageChanged(int index)
    {
        if (!isInitialized)
            return;

        if (index < 0 || index >= localeCodes.Count)
        {
            Debug.LogWarning($"[LanguageDropdownUI] Invalid dropdown index: {index}");
            return;
        }

        string localeCode = localeCodes[index];
        ApplyLanguage(localeCode);
        RefreshLanguagePreview(index);
    }

    private async void ApplyLanguage(string localeCode)
    {
        await LocalizationSettings.InitializationOperation.Task;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                PlayerPrefs.SetString(SaveKey, localeCode);
                PlayerPrefs.Save();

                Debug.Log($"[LanguageDropdownUI] Language changed: {localeCode}");
                return;
            }
        }

        Debug.LogWarning($"[LanguageDropdownUI] Locale not found: {localeCode}");
    }

    private void RefreshLanguagePreview(int index)
    {
        if (krObject != null) krObject.SetActive(index == 0);
        if (enObject != null) enObject.SetActive(index == 1);
        if (scObject != null) scObject.SetActive(index == 2);
        if (jpObject != null) jpObject.SetActive(index == 3);
        if (spObject != null) spObject.SetActive(index == 4);
    }

}
