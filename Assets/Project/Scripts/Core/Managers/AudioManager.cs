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
    Skill,
    NormalButtonHover,
    NormalButtonClick,
    MoveButtonHover,
    MoveButtonClick,
    SceneTransition,
    LobbyPanelTransition,
    BattleActionReserveText,
    BattleProgressText,
    RelicChoiceAcquire,
    BattleRewardRemnantAcquire,
    BattleRewardRelicSkillAcquire
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

    [Range(0f, 1f)]
    public float volume = 1f;
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
    private Dictionary<SfxType, SfxData> sfxDict;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;

        InitializeDictionary();
    }

    public void Initialize()
    {
        InitializeDictionary();
        ApplyVolumes();
    }

    private void InitializeDictionary()
    {
        bgmDict = new Dictionary<BgmType, AudioClip>();
        sfxDict = new Dictionary<SfxType, SfxData>();

        foreach (BgmData data in bgmList)
        {
            if (data == null || data.clip == null)
                continue;

            if (!bgmDict.ContainsKey(data.type))
                bgmDict.Add(data.type, data.clip);
        }

        foreach (SfxData data in sfxList)
        {
            if (data == null || data.clip == null)
                continue;

            data.volume = Mathf.Clamp01(data.volume);

            if (!sfxDict.ContainsKey(data.type))
                sfxDict.Add(data.type, data);
        }
    }

    public void PlayBgm(BgmType type, bool loop = true)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] BGM Source is not assigned.");
            return;
        }

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
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySfx(SfxType type)
    {
        PlaySfx(type, 1f);
    }

    public void PlaySfx(SfxType type, float volumeMultiplier)
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] SFX Source is not assigned.");
            return;
        }

        if (!sfxDict.TryGetValue(type, out SfxData data) || data == null || data.clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX not found: {type}");
            return;
        }

        float volume = Mathf.Clamp01(data.volume) * Mathf.Clamp01(volumeMultiplier);
        sfxSource.PlayOneShot(data.clip, volume);
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

    public void SetSfxVolume(SfxType type, float volume)
    {
        if (!sfxDict.TryGetValue(type, out SfxData data) || data == null)
        {
            Debug.LogWarning($"[AudioManager] SFX not found: {type}");
            return;
        }

        data.volume = Mathf.Clamp01(volume);
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

    public float GetSfxVolume(SfxType type)
    {
        if (!sfxDict.TryGetValue(type, out SfxData data) || data == null)
            return 1f;

        return Mathf.Clamp01(data.volume);
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
