using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;
using Relic.Gameplay.Data;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private GameStateType firstState = GameStateType.Title;
    [SerializeField] private string defaultLanguageCode = "ko";

    private IEnumerator Start()
    {
        Debug.Log("[Bootstrap] Startup: begin");
        RunStep("Bootstrap.Resolution", ResolutionManager.EnsureInitialized);
        RunStep("Bootstrap.Blur", () => UIBlurBackgroundManager.Instance.name = "SharedBlurRoot");
        RunStep("Bootstrap.Settings", () => { Settings.Instance.Load(); GameBrightnessManager.ApplySavedBrightness(); });
        RunStep("Bootstrap.Save", SaveSystem.Instance.Initialize);
        RunStep("Bootstrap.EventBus", EventBus.Instance.Initialize);
        RunStep("Bootstrap.Data", () => { DataManager.Instance.Initialize(); SaveSystem.Instance.TryLoadProgress(); InitialDefaultPartySetup.TryInitialize(DataManager.Instance); });
        RunStep("Bootstrap.Audio", AudioManager.Instance.Initialize);
        RunStep("Bootstrap.Input", InputManager.Instance.Initialize);
        RunStep("Bootstrap.UI", () => { var unused = UIManager.Instance; });
        RunStep("Bootstrap.GameManager", GameManager.Instance.Initialize);
        StartupDiagnostics.Begin("Bootstrap.Localization");
        yield return InitializeLanguage();
        StartupDiagnostics.Success("Bootstrap.Localization");
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
            StartupDiagnostics.Fail("Bootstrap.FirstState", task.Exception);
            Debug.LogException(task.Exception);
            yield break;
        }

        Debug.Log($"[Bootstrap] Startup: first state entered ({firstState})");
        StartupDiagnostics.Complete($"state={firstState} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    private static void RunStep(string step, System.Action action)
    {
        StartupDiagnostics.Begin(step);
        try { action(); StartupDiagnostics.Success(step); }
        catch (System.Exception exception) { StartupDiagnostics.Fail(step, exception); throw; }
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
