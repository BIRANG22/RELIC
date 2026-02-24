using UnityEngine;

public class Settings : Singleton<Settings>
{
    public float BGMVolume = 1f;
    public float SFXVolume = 1f;

    public void Load()
    {
        BGMVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("BGMVolume", BGMVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
    }
}