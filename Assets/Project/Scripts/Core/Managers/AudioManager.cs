using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

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
    BattleTimelineSlotRotate,
    BattleEndButtonHover,
    BattleTimelineSlotSlide,
    BattleMapNodeCheckAnimation,

    RelicChoiceAcquire,
    BattleRewardRemnantAcquire,
    BattleRewardRelicSkillAcquire,

    SkillListPanelOpen = 21,
    SkillListPanelClose = 22,

    // 기존 프로젝트에 저장된 enum 번호를 유지한다.
    BoxOpen = 23,

    CharacterSettingAreaAppear = 24,
    CharacterSettingAreaExit = 25,

    BagOpen = 26,
    BagClose = 27,
    InventoryOpen = 28,
    InventoryClose = 29
}

[System.Serializable]
public class BgmData
{
    public BgmType type;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}

[System.Serializable]
public class SfxData
{
    public SfxType type;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}

[System.Serializable]
public class SfxIdData
{
    public string id;
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
    [SerializeField] private List<BgmData> bgmList = new();

    [Header("Common SFX")]
    [SerializeField] private List<SfxData> commonSfxList = new();

    [Header("Player SFX")]
    [SerializeField] private List<SfxData> playerSfxList = new();

    [Header("Monster SFX")]
    [SerializeField] private List<SfxData> monsterSfxList = new();

    [Header("UI SFX")]
    [SerializeField] private List<SfxData> uiSfxList = new();

    [Header("Battle SFX")]
    [SerializeField] private List<SfxData> battleSfxList = new();

    [Header("Reward SFX")]
    [SerializeField] private List<SfxData> rewardSfxList = new();

    [Header("VFX SFX ID List")]
    [SerializeField] private List<SfxIdData> vfxSfxIdList = new();

    private Dictionary<BgmType, List<BgmData>> bgmDict;
    private Dictionary<SfxType, SfxData> sfxDict;
    private Dictionary<string, SfxIdData> sfxIdDict;
    private readonly List<AudioSource> bgmLayerSources = new();
    private readonly List<RoutedSfxSource> routedSfxSources = new();
    private IReadOnlyList<BgmData> activeBgmLayers;
    private Coroutine pendingBgmRoutine;

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
        bgmDict = new Dictionary<BgmType, List<BgmData>>();
        sfxDict = new Dictionary<SfxType, SfxData>();
        sfxIdDict = new Dictionary<string, SfxIdData>(System.StringComparer.Ordinal);

        foreach (BgmData data in bgmList)
        {
            if (data == null || data.clip == null)
                continue;

            data.volume = Mathf.Clamp01(data.volume);

            if (!bgmDict.TryGetValue(data.type, out List<BgmData> layers))
            {
                layers = new List<BgmData>();
                bgmDict.Add(data.type, layers);
            }

            layers.Add(data);
        }

        RegisterSfxList(commonSfxList);
        RegisterSfxList(playerSfxList);
        RegisterSfxList(monsterSfxList);
        RegisterSfxList(uiSfxList);
        RegisterSfxList(battleSfxList);
        RegisterSfxList(rewardSfxList);
        RegisterSfxIdList(vfxSfxIdList);
    }

    private void RegisterSfxList(List<SfxData> list)
    {
        if (list == null)
            return;

        foreach (SfxData data in list)
        {
            if (data == null || data.clip == null)
                continue;

            data.volume = Mathf.Clamp01(data.volume);

            if (!sfxDict.ContainsKey(data.type))
                sfxDict.Add(data.type, data);
            else
                Debug.LogWarning($"[AudioManager] Duplicate SFX Type: {data.type}");
        }
    }

    private void RegisterSfxIdList(List<SfxIdData> list)
    {
        if (list == null)
            return;

        foreach (SfxIdData data in list)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.id) || data.clip == null)
                continue;

            string id = data.id.Trim();
            data.volume = Mathf.Clamp01(data.volume);

            if (!sfxIdDict.ContainsKey(id))
                sfxIdDict.Add(id, data);
            else
                Debug.LogWarning($"[AudioManager] Duplicate SFX ID: {id}");
        }
    }

    public void PlayBgm(BgmType type, bool loop = true)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] BGM Source is not assigned.");
            return;
        }

        if (!bgmDict.TryGetValue(type, out List<BgmData> layers) || layers == null || layers.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] BGM not found: {type}");
            return;
        }

        if (IsBgmAlreadyPlaying(layers, loop))
        {
            activeBgmLayers = layers;
            ApplyVolumes();
            return;
        }

        activeBgmLayers = layers;

        PlayBgmClip(bgmSource, layers[0].clip, loop);
        EnsureBgmLayerSourceCount(layers.Count - 1);

        for (int i = 1; i < layers.Count; i++)
        {
            AudioSource layerSource = bgmLayerSources[i - 1];
            PlayBgmClip(layerSource, layers[i].clip, loop);
        }

        StopUnusedBgmLayerSources(layers.Count - 1);

        ApplyVolumes();
    }

    public void PlayBgmDelayed(BgmType type, bool loop = true)
    {
        if (!isActiveAndEnabled)
        {
            PlayBgm(type, loop);
            return;
        }

        if (pendingBgmRoutine != null)
            StopCoroutine(pendingBgmRoutine);

        pendingBgmRoutine = StartCoroutine(PlayBgmDelayedRoutine(type, loop));
    }

    private IEnumerator PlayBgmDelayedRoutine(BgmType type, bool loop)
    {
        yield return null;

        pendingBgmRoutine = null;
        PlayBgm(type, loop);
    }

    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
        activeBgmLayers = null;

        StopUnusedBgmLayerSources(0);
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

    public void PlaySfx(string id)
    {
        PlaySfx(id, 1f);
    }

    public void PlaySfx(string id, float volumeMultiplier)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        string trimmedId = id.Trim();

        if (sfxIdDict != null &&
            sfxIdDict.TryGetValue(trimmedId, out SfxIdData data) &&
            data != null &&
            data.clip != null)
        {
            PlaySfxClip(data.clip, Mathf.Clamp01(data.volume) * Mathf.Clamp01(volumeMultiplier));
            return;
        }

        if (System.Enum.TryParse(trimmedId, out SfxType type))
        {
            PlaySfx(type, volumeMultiplier);
            return;
        }

        Debug.LogWarning($"[AudioManager] SFX ID not found: {trimmedId}");
    }

    public void PlaySfxClip(AudioClip clip)
    {
        PlaySfxClip(clip, 1f);
    }

    public void PlaySfxClip(AudioClip clip, float volumeMultiplier)
    {
        if (clip == null)
            return;

        if (sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] SFX Source is not assigned.");
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeMultiplier));
    }

    public AudioSource PlaySfxClip(AudioSource source)
    {
        return PlaySfxClip(source, 1f);
    }

    public AudioSource PlaySfxClip(AudioSource source, float volumeMultiplier)
    {
        AudioSourcePlaybackSettings settings = AudioSourcePlaybackSettings.From(source);
        return PlaySfxClip(settings, volumeMultiplier);
    }

    public AudioSource PlaySfxClip(
        AudioSourcePlaybackSettings sourceSettings,
        float volumeMultiplier)
    {
        if (sourceSettings == null || sourceSettings.Clip == null)
            return null;

        GameObject sourceObject = new($"{sourceSettings.Clip.name}_RoutedSfx");
        sourceObject.transform.SetParent(transform, false);
        sourceObject.transform.SetPositionAndRotation(
            sourceSettings.WorldPosition,
            sourceSettings.WorldRotation);

        AudioSource routedSource = sourceObject.AddComponent<AudioSource>();
        sourceSettings.ApplyTo(routedSource, Mathf.Clamp01(volumeMultiplier));

        float baseVolume = routedSource.volume;
        RegisterRoutedSfxSource(routedSource, baseVolume);
        ApplyRoutedSfxVolume(routedSource, baseVolume);
        routedSource.Play();

        if (!routedSource.loop && isActiveAndEnabled)
            StartCoroutine(DestroyRoutedSfxWhenFinished(routedSource));

        return routedSource;
    }

    public void StopRoutedSfxSource(AudioSource routedSource)
    {
        if (routedSource == null)
            return;

        UnregisterRoutedSfxSource(routedSource);

        GameObject sourceObject = routedSource.gameObject;
        routedSource.Stop();

        if (Application.isPlaying)
            Destroy(sourceObject);
        else
            DestroyImmediate(sourceObject);
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
        float master = GetMasterVolumeOrDefault();
        float bgm = GetBgmVolumeOrDefault();
        float sfx = GetSfxVolumeOrDefault();

        if (bgmSource != null)
            bgmSource.volume = master * bgm * GetBgmLayerVolume(0);

        ApplyBgmLayerVolumes(master * bgm);

        if (sfxSource != null)
            sfxSource.volume = master * sfx;

        ApplyRoutedSfxVolumes();
    }

    private bool IsBgmAlreadyPlaying(IReadOnlyList<BgmData> layers, bool loop)
    {
        if (layers == null || layers.Count == 0)
            return false;

        if (!IsBgmSourcePlayingClip(bgmSource, layers[0].clip, loop))
            return false;

        for (int i = 1; i < layers.Count; i++)
        {
            int layerIndex = i - 1;

            if (layerIndex >= bgmLayerSources.Count)
                return false;

            if (!IsBgmSourcePlayingClip(bgmLayerSources[layerIndex], layers[i].clip, loop))
                return false;
        }

        return true;
    }

    private static bool IsBgmSourcePlayingClip(AudioSource source, AudioClip clip, bool loop)
    {
        return source != null &&
            source.clip == clip &&
            source.loop == loop &&
            source.isPlaying;
    }

    private static void PlayBgmClip(AudioSource source, AudioClip clip, bool loop)
    {
        if (source == null || clip == null)
            return;

        if (source.clip != clip)
        {
            source.Stop();
            source.clip = clip;
        }

        source.loop = loop;
        source.Play();
    }

    private void EnsureBgmLayerSourceCount(int count)
    {
        for (int i = bgmLayerSources.Count - 1; i >= 0; i--)
        {
            if (bgmLayerSources[i] == null)
                bgmLayerSources.RemoveAt(i);
        }

        while (bgmLayerSources.Count < count)
        {
            GameObject layerObject = new($"BGM Layer {bgmLayerSources.Count + 1}");
            layerObject.transform.SetParent(transform, false);

            AudioSource layerSource = layerObject.AddComponent<AudioSource>();
            CopyBgmSourceSettings(bgmSource, layerSource);
            bgmLayerSources.Add(layerSource);
        }
    }

    private void StopUnusedBgmLayerSources(int activeCount)
    {
        for (int i = 0; i < bgmLayerSources.Count; i++)
        {
            AudioSource layerSource = bgmLayerSources[i];

            if (layerSource == null || i < activeCount)
                continue;

            layerSource.Stop();
            layerSource.clip = null;
        }
    }

    private void ApplyBgmLayerVolumes(float volume)
    {
        for (int i = bgmLayerSources.Count - 1; i >= 0; i--)
        {
            AudioSource layerSource = bgmLayerSources[i];

            if (layerSource == null)
            {
                bgmLayerSources.RemoveAt(i);
                continue;
            }

            layerSource.volume = volume * GetBgmLayerVolume(i + 1);
        }
    }

    private float GetBgmLayerVolume(int layerIndex)
    {
        if (activeBgmLayers == null || layerIndex < 0 || layerIndex >= activeBgmLayers.Count)
            return 1f;

        BgmData data = activeBgmLayers[layerIndex];
        return data != null ? Mathf.Clamp01(data.volume) : 1f;
    }

    private static void CopyBgmSourceSettings(AudioSource source, AudioSource target)
    {
        if (source == null || target == null)
            return;

        target.outputAudioMixerGroup = source.outputAudioMixerGroup;
        target.bypassEffects = source.bypassEffects;
        target.bypassListenerEffects = source.bypassListenerEffects;
        target.bypassReverbZones = source.bypassReverbZones;
        target.playOnAwake = false;
        target.priority = source.priority;
        target.pitch = source.pitch;
        target.panStereo = source.panStereo;
        target.spatialBlend = source.spatialBlend;
        target.reverbZoneMix = source.reverbZoneMix;
        target.dopplerLevel = source.dopplerLevel;
        target.spread = source.spread;
        target.rolloffMode = source.rolloffMode;
        target.minDistance = source.minDistance;
        target.maxDistance = source.maxDistance;
        target.ignoreListenerPause = source.ignoreListenerPause;
        target.ignoreListenerVolume = source.ignoreListenerVolume;
        target.spatialize = source.spatialize;
        target.spatializePostEffects = source.spatializePostEffects;
        target.velocityUpdateMode = source.velocityUpdateMode;
    }

    private IEnumerator DestroyRoutedSfxWhenFinished(AudioSource routedSource)
    {
        if (routedSource == null || routedSource.clip == null)
            yield break;

        float pitch = Mathf.Abs(routedSource.pitch);
        float duration = routedSource.clip.length / Mathf.Max(0.01f, pitch);
        yield return new WaitForSeconds(duration + 0.05f);

        StopRoutedSfxSource(routedSource);
    }

    private void RegisterRoutedSfxSource(AudioSource source, float baseVolume)
    {
        if (source == null)
            return;

        routedSfxSources.Add(new RoutedSfxSource
        {
            source = source,
            baseVolume = Mathf.Clamp01(baseVolume)
        });
    }

    private void UnregisterRoutedSfxSource(AudioSource source)
    {
        if (source == null)
            return;

        for (int i = routedSfxSources.Count - 1; i >= 0; i--)
        {
            if (routedSfxSources[i].source == source)
                routedSfxSources.RemoveAt(i);
        }
    }

    private void ApplyRoutedSfxVolumes()
    {
        for (int i = routedSfxSources.Count - 1; i >= 0; i--)
        {
            RoutedSfxSource routed = routedSfxSources[i];

            if (routed == null || routed.source == null)
            {
                routedSfxSources.RemoveAt(i);
                continue;
            }

            ApplyRoutedSfxVolume(routed.source, routed.baseVolume);
        }
    }

    private void ApplyRoutedSfxVolume(AudioSource source, float baseVolume)
    {
        if (source == null)
            return;

        source.volume = Mathf.Clamp01(baseVolume) * GetSfxOutputVolumeMultiplier();
    }

    private static float GetSfxOutputVolumeMultiplier()
    {
        return GetMasterVolumeOrDefault() * GetSfxVolumeOrDefault();
    }

    private static float GetMasterVolumeOrDefault()
    {
        return Settings.Instance != null ? Mathf.Clamp01(Settings.Instance.MasterVolume) : 1f;
    }

    private static float GetBgmVolumeOrDefault()
    {
        return Settings.Instance != null ? Mathf.Clamp01(Settings.Instance.BGMVolume) : 1f;
    }

    private static float GetSfxVolumeOrDefault()
    {
        return Settings.Instance != null ? Mathf.Clamp01(Settings.Instance.SFXVolume) : 1f;
    }

    private sealed class RoutedSfxSource
    {
        public AudioSource source;
        public float baseVolume;
    }
}

public sealed class AudioSourcePlaybackSettings
{
    private AnimationCurve customRolloffCurve;
    private AnimationCurve spatialBlendCurve;
    private AnimationCurve reverbZoneMixCurve;
    private AnimationCurve spreadCurve;

    public AudioClip Clip { get; private set; }
    public AudioMixerGroup OutputAudioMixerGroup { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public Quaternion WorldRotation { get; private set; }
    public bool BypassEffects { get; private set; }
    public bool BypassListenerEffects { get; private set; }
    public bool BypassReverbZones { get; private set; }
    public bool Loop { get; private set; }
    public int Priority { get; private set; }
    public float Volume { get; private set; }
    public float Pitch { get; private set; }
    public float PanStereo { get; private set; }
    public float SpatialBlend { get; private set; }
    public float ReverbZoneMix { get; private set; }
    public float DopplerLevel { get; private set; }
    public float Spread { get; private set; }
    public AudioRolloffMode RolloffMode { get; private set; }
    public float MinDistance { get; private set; }
    public float MaxDistance { get; private set; }
    public bool IgnoreListenerPause { get; private set; }
    public bool IgnoreListenerVolume { get; private set; }
    public bool Spatialize { get; private set; }
    public bool SpatializePostEffects { get; private set; }
    public AudioVelocityUpdateMode VelocityUpdateMode { get; private set; }

    public static AudioSourcePlaybackSettings From(AudioSource source)
    {
        if (source == null)
            return null;

        return new AudioSourcePlaybackSettings
        {
            Clip = source.clip,
            OutputAudioMixerGroup = source.outputAudioMixerGroup,
            WorldPosition = source.transform.position,
            WorldRotation = source.transform.rotation,
            BypassEffects = source.bypassEffects,
            BypassListenerEffects = source.bypassListenerEffects,
            BypassReverbZones = source.bypassReverbZones,
            Loop = source.loop,
            Priority = source.priority,
            Volume = source.volume,
            Pitch = source.pitch,
            PanStereo = source.panStereo,
            SpatialBlend = source.spatialBlend,
            ReverbZoneMix = source.reverbZoneMix,
            DopplerLevel = source.dopplerLevel,
            Spread = source.spread,
            RolloffMode = source.rolloffMode,
            MinDistance = source.minDistance,
            MaxDistance = source.maxDistance,
            IgnoreListenerPause = source.ignoreListenerPause,
            IgnoreListenerVolume = source.ignoreListenerVolume,
            Spatialize = source.spatialize,
            SpatializePostEffects = source.spatializePostEffects,
            VelocityUpdateMode = source.velocityUpdateMode,
            customRolloffCurve = CopyCurve(source.GetCustomCurve(AudioSourceCurveType.CustomRolloff)),
            spatialBlendCurve = CopyCurve(source.GetCustomCurve(AudioSourceCurveType.SpatialBlend)),
            reverbZoneMixCurve = CopyCurve(source.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix)),
            spreadCurve = CopyCurve(source.GetCustomCurve(AudioSourceCurveType.Spread))
        };
    }

    public void ApplyTo(AudioSource target, float volumeMultiplier)
    {
        if (target == null)
            return;

        target.clip = Clip;
        target.outputAudioMixerGroup = OutputAudioMixerGroup;
        target.bypassEffects = BypassEffects;
        target.bypassListenerEffects = BypassListenerEffects;
        target.bypassReverbZones = BypassReverbZones;
        target.playOnAwake = false;
        target.loop = Loop;
        target.priority = Priority;
        target.volume = Mathf.Clamp01(Volume * Mathf.Clamp01(volumeMultiplier));
        target.pitch = Pitch;
        target.panStereo = PanStereo;
        target.spatialBlend = SpatialBlend;
        target.reverbZoneMix = ReverbZoneMix;
        target.dopplerLevel = DopplerLevel;
        target.spread = Spread;
        target.rolloffMode = RolloffMode;
        target.minDistance = MinDistance;
        target.maxDistance = MaxDistance;
        target.ignoreListenerPause = IgnoreListenerPause;
        target.ignoreListenerVolume = IgnoreListenerVolume;
        target.spatialize = Spatialize;
        target.spatializePostEffects = SpatializePostEffects;
        target.velocityUpdateMode = VelocityUpdateMode;

        ApplyCurve(target, AudioSourceCurveType.CustomRolloff, customRolloffCurve);
        ApplyCurve(target, AudioSourceCurveType.SpatialBlend, spatialBlendCurve);
        ApplyCurve(target, AudioSourceCurveType.ReverbZoneMix, reverbZoneMixCurve);
        ApplyCurve(target, AudioSourceCurveType.Spread, spreadCurve);
    }

    private static void ApplyCurve(
        AudioSource target,
        AudioSourceCurveType curveType,
        AnimationCurve curve)
    {
        if (target == null || curve == null)
            return;

        target.SetCustomCurve(curveType, CopyCurve(curve));
    }

    private static AnimationCurve CopyCurve(AnimationCurve source)
    {
        if (source == null)
            return null;

        AnimationCurve copy = new(source.keys)
        {
            preWrapMode = source.preWrapMode,
            postWrapMode = source.postWrapMode
        };

        return copy;
    }
}
