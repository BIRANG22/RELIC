using UnityEngine;

public class Settings : Singleton<Settings>
{
    public float MasterVolume = 1f;
    public float BGMVolume = 1f;
    public float SFXVolume = 1f;

    public void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        BGMVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.SetFloat("BGMVolume", BGMVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.Save();
    }
}

public static class TutorialSettings
{
    public const string ShowTutorialPrefsKey = "Relic.ShowTutorial";

    private const int ShowTutorialValue = 1;
    private const int HideTutorialValue = 0;

    public static bool ShouldShowTutorial =>
        PlayerPrefs.GetInt(ShowTutorialPrefsKey, ShowTutorialValue) == ShowTutorialValue;

    public static void SetShouldShowTutorial(bool shouldShowTutorial)
    {
        PlayerPrefs.SetInt(
            ShowTutorialPrefsKey,
            shouldShowTutorial ? ShowTutorialValue : HideTutorialValue);

        PlayerPrefs.Save();
    }

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
