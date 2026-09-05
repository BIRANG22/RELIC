using UnityEngine;

public class Settings : Singleton<Settings>
{
    public float MasterVolume = 1f;
    public float BGMVolume = 1f;
    public float SFXVolume = 1f;
    public float Brightness = 0.5f;

    public void Load()
    {
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
