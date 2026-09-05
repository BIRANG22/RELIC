using UnityEngine;

public class Settings : Singleton<Settings>
{
    public float MasterVolume = 1f;
    public float BGMVolume = 1f;
    public float SFXVolume = 1f;
    public float Brightness = 0.5f;

    public void Load()
    {
        OnboardingToggleDefaults.EnsureInitialized();

        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        BGMVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
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
    /// 이 버전의 초기 안내 설정을 처음 적용할 때만 튜토리얼/인트로 예약 토글을 ON으로 맞춥니다.
    /// 이후 실행에서는 사용자가 변경하거나 1회 실행으로 소비된 값을 그대로 유지합니다.
    /// </summary>
    public static void EnsureInitialized()
    {
        int appliedVersion = PlayerPrefs.GetInt(DefaultsVersionPrefsKey, 0);
        if (appliedVersion >= CurrentDefaultsVersion)
            return;

        if (appliedVersion < 1)
        {
            TutorialSettings.SetShouldShowTutorial(true);
            IntroSettings.SetShouldPlayIntro(true);
        }

        // 기존 테스트/빌드의 PlayerPrefs가 남아 있더라도 이번 버전에서 한 번만
        // TutorialToggle1과 IntroToggle1을 모두 ON으로 맞춥니다.
        // 이후에는 실제 1회 실행이나 사용자의 설정 변경 결과를 그대로 유지합니다.
        if (appliedVersion < 3)
        {
            TutorialSettings.SetShouldShowTutorial(true);
            IntroSettings.SetShouldPlayIntro(true);
        }

        PlayerPrefs.SetInt(DefaultsVersionPrefsKey, CurrentDefaultsVersion);
        PlayerPrefs.Save();
    }
}

public static class TutorialSettings
{
    public const string ShowTutorialPrefsKey = "Relic.ShowTutorial";

    private const int ShowTutorialValue = 1;
    private const int HideTutorialValue = 0;

    /// <summary>
    /// TutorialToggle1 상태입니다.
    /// ON이면 다음 전투 튜토리얼 자동 표시를 1회 예약하고,
    /// 실제 자동 튜토리얼이 열리면 OFF로 소비됩니다.
    /// </summary>
    public static bool ShouldShowTutorial =>
        PlayerPrefs.GetInt(ShowTutorialPrefsKey, ShowTutorialValue) == ShowTutorialValue;

    public static void SetShouldShowTutorial(bool shouldShowTutorial)
    {
        PlayerPrefs.SetInt(
            ShowTutorialPrefsKey,
            shouldShowTutorial ? ShowTutorialValue : HideTutorialValue);

        PlayerPrefs.Save();
    }

    /// <summary>
    /// 기존 호출 호환용입니다. 자동 튜토리얼 예약을 소비합니다.
    /// </summary>
    public static void MarkTutorialShown()
    {
        SetShouldShowTutorial(false);
    }
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
