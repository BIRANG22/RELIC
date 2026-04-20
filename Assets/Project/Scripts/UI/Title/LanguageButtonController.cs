using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageButtonController : MonoBehaviour
{
    public void SetKorean()
    {
        StartCoroutine(SetLocale("ko"));
    }

    public void SetEnglish()
    {
        StartCoroutine(SetLocale("en"));
    }

    public void SetChineseSimplified()
    {
        StartCoroutine(SetLocale("zh-Hans"));
    }

    public void SetJapanese()
    {
        StartCoroutine(SetLocale("ja"));
    }

    public void SetSpanish()
    {
        StartCoroutine(SetLocale("es"));
    }

    private IEnumerator SetLocale(string localeCode)
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;

        foreach (var locale in locales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                PlayerPrefs.SetString("SelectedLanguage", localeCode);
                PlayerPrefs.Save();
                yield break;
            }
        }

        Debug.LogWarning($"Locale not found: {localeCode}");
    }
}