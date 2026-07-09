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
