using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private GameStateType firstState = GameStateType.Title;
    [SerializeField] private string defaultLanguageCode = "ko";

    private IEnumerator Start()
    {
        // 1. Settings Load
        Settings.Instance.Load();

        // 2. SaveSystem Init
        SaveSystem.Instance.Initialize();

        // 3. EventBus Init
        EventBus.Instance.Initialize();

        // 4. Data Load
        DataManager.Instance.Initialize();

        // 5. Audio Init
        AudioManager.Instance.Initialize();

        // 6. Input Init
        InputManager.Instance.Initialize();

        // 7. UIManager Init
        var uiManager = UIManager.Instance;

        // 8. GameManager Init
        GameManager.Instance.Initialize();

        // 9. Localization Init
        yield return InitializeLanguage();

        yield return null;

        yield return ChangeFirstState();
    }

    private IEnumerator ChangeFirstState()
    {
        var task = GameManager.Instance.StateMachine.ChangeState(firstState);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            Debug.LogException(task.Exception);
        }
    }
    private IEnumerator InitializeLanguage()
    {
        yield return LocalizationSettings.InitializationOperation;

        string savedLanguageCode = PlayerPrefs.GetString("SelectedLanguage", defaultLanguageCode);

        var locales = LocalizationSettings.AvailableLocales.Locales;
        foreach (var locale in locales)
        {
            if (locale.Identifier.Code == savedLanguageCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                yield break;
            }
        }

        Debug.LogWarning($"Saved locale not found: {savedLanguageCode}");

        foreach (var locale in locales)
        {
            if (locale.Identifier.Code == defaultLanguageCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                yield break;
            }
        }
    }
}