using UnityEngine;

public class Settings : Singleton<Settings>
{
    public float MasterVolume = 1f;
    public float BGMVolume = 0.5f;
    public float SFXVolume = 0.5f;
    public float Brightness = 0.5f;

    public void Load()
    {
        OnboardingToggleDefaults.EnsureInitialized();

        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        BGMVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        Brightness = Mathf.Clamp01(PlayerPrefs.GetFloat("Brightness", 0.5f));
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.SetFloat("BGMVolume", BGMVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.SetFloat("Brightness", Mathf.Clamp01(Brightness));
        PlayerPrefs.Save();
    }
}

public static class OnboardingToggleDefaults
{
    private const string DefaultsVersionPrefsKey = "Dustium.OnboardingToggleDefaultsVersion";
    private const int CurrentDefaultsVersion = 3;

    /// <summary>
    /// 새 온보딩 설정을 처음 적용할 때 인트로와 전체 튜토리얼을 ON 상태로 맞춥니다.
    /// 이후에는 사용자가 변경하거나 1회 진행으로 소비된 값을 그대로 유지합니다.
    /// </summary>
    public static void EnsureInitialized()
    {
        int appliedVersion = PlayerPrefs.GetInt(DefaultsVersionPrefsKey, 0);
        if (appliedVersion >= CurrentDefaultsVersion)
            return;

        if (appliedVersion < 1)
            IntroSettings.SetShouldPlayIntro(true);

        // 기존 TutorialToggle1은 전투 패널 표시 설정이었지만,
        // 버전 3부터는 새 게임에서 전체 튜토리얼을 진행할지 결정하는 설정으로 전환합니다.
        if (appliedVersion < 3)
            TutorialSettings.SetShouldPlayTutorial(true);

        // 기존 빌드에서 IntroSeen 값 때문에 OFF로 시작하던 상태를 한 번 보정합니다.
        if (appliedVersion < 2)
            IntroSettings.SetShouldPlayIntro(true);

        PlayerPrefs.SetInt(DefaultsVersionPrefsKey, CurrentDefaultsVersion);
        PlayerPrefs.Save();
    }
}

public static class TutorialSettings
{
    public const string TutorialSeenPrefsKey = "Dustium.TutorialSeen";

    private const int SeenValue = 1;
    private const int NotSeenValue = 0;

    public static bool HasSeenTutorial =>
        PlayerPrefs.GetInt(TutorialSeenPrefsKey, NotSeenValue) == SeenValue;

    /// <summary>
    /// ON이면 다음 게임 시작에서 전체 튜토리얼을 진행합니다.
    /// 튜토리얼이 실제 시작되면 자동으로 OFF가 됩니다.
    /// </summary>
    public static bool ShouldPlayTutorial => !HasSeenTutorial;

    public static void SetShouldPlayTutorial(bool shouldPlayTutorial)
    {
        PlayerPrefs.SetInt(
            TutorialSeenPrefsKey,
            shouldPlayTutorial ? NotSeenValue : SeenValue);
        PlayerPrefs.Save();
    }

    public static void MarkTutorialSeen()
    {
        PlayerPrefs.SetInt(TutorialSeenPrefsKey, SeenValue);
        PlayerPrefs.Save();
    }

    public static void ResetTutorialSeenState()
    {
        PlayerPrefs.SetInt(TutorialSeenPrefsKey, NotSeenValue);
        PlayerPrefs.Save();
    }

    // 기존 호출부 호환용입니다. 이제 의미는 "전체 튜토리얼을 다음 시작에 진행할지"입니다.
    public static bool ShouldShowTutorial => ShouldPlayTutorial;
    public static void SetShouldShowTutorial(bool shouldShowTutorial) => SetShouldPlayTutorial(shouldShowTutorial);
    public static void MarkTutorialShown() => MarkTutorialSeen();
}

public static class IntroSettings
{
    public const string IntroSeenPrefsKey = "Dustium.IntroSeen";

    private const int SeenValue = 1;
    private const int NotSeenValue = 0;

    public static bool HasSeenIntro =>
        PlayerPrefs.GetInt(IntroSeenPrefsKey, NotSeenValue) == SeenValue;

    public static bool ShouldPlayIntro => !HasSeenIntro;

    public static void SetShouldPlayIntro(bool shouldPlayIntro)
    {
        PlayerPrefs.SetInt(
            IntroSeenPrefsKey,
            shouldPlayIntro ? NotSeenValue : SeenValue);
        PlayerPrefs.Save();
    }

    public static void MarkIntroSeen()
    {
        PlayerPrefs.SetInt(IntroSeenPrefsKey, SeenValue);
        PlayerPrefs.Save();
    }

    public static void ResetIntroSeenState()
    {
        PlayerPrefs.SetInt(IntroSeenPrefsKey, NotSeenValue);
        PlayerPrefs.Save();
    }
}
