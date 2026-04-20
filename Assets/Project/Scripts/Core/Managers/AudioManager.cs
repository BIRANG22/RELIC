using UnityEngine;
using System.Collections.Generic;

public enum BgmType
{
    Title,
    Lobby,
    Battle
}

public enum SfxType
{
    Click,
    Cancel,
    Confirm,
    Attack,
    Hit,
    Skill
}

[System.Serializable]
public class BgmData
{
    public BgmType type;
    public AudioClip clip;
}

[System.Serializable]
public class SfxData
{
    public SfxType type;
    public AudioClip clip;
}

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM List")]
    [SerializeField] private List<BgmData> bgmList = new List<BgmData>();

    [Header("SFX List")]
    [SerializeField] private List<SfxData> sfxList = new List<SfxData>();

    private Dictionary<BgmType, AudioClip> bgmDict;
    private Dictionary<SfxType, AudioClip> sfxDict;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;

        InitializeDictionary();
    }

    public void Initialize()
    {
        Debug.Log("AudioManager Initialized");
        ApplyVolumes();
    }

    private void InitializeDictionary()
    {
        bgmDict = new Dictionary<BgmType, AudioClip>();
        sfxDict = new Dictionary<SfxType, AudioClip>();

        foreach (var data in bgmList)
        {
            if (data == null || data.clip == null)
                continue;

            if (!bgmDict.ContainsKey(data.type))
                bgmDict.Add(data.type, data.clip);
        }

        foreach (var data in sfxList)
        {
            if (data == null || data.clip == null)
                continue;

            if (!sfxDict.ContainsKey(data.type))
                sfxDict.Add(data.type, data.clip);
        }
    }

    public void PlayBgm(BgmType type, bool loop = true)
    {
        if (!bgmDict.TryGetValue(type, out AudioClip clip))
        {
            Debug.LogWarning($"[AudioManager] BGM not found: {type}");
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();

        ApplyVolumes();
    }

    public void StopBgm()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySfx(SfxType type)
    {
        if (!sfxDict.TryGetValue(type, out AudioClip clip))
        {
            Debug.LogWarning($"[AudioManager] SFX not found: {type}");
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    public void SetMasterVolume(float volume)
    {
        Settings.Instance.MasterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetBgmVolume(float volume)
    {
        Settings.Instance.BGMVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        Settings.Instance.SFXVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public float GetMasterVolume()
    {
        return Settings.Instance.MasterVolume;
    }

    public float GetBgmVolume()
    {
        return Settings.Instance.BGMVolume;
    }

    public float GetSfxVolume()
    {
        return Settings.Instance.SFXVolume;
    }

    private void ApplyVolumes()
    {
        float master = Settings.Instance.MasterVolume;
        float bgm = Settings.Instance.BGMVolume;
        float sfx = Settings.Instance.SFXVolume;

        if (bgmSource != null)
            bgmSource.volume = master * bgm;

        if (sfxSource != null)
            sfxSource.volume = master * sfx;
    }
}