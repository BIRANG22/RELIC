using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

public static class BgmCrossfadePolicy
{
    public static void Evaluate(float elapsed, float fadeOutDuration, float fadeInDuration,
        out float outgoingMultiplier, out float incomingMultiplier)
    {
        outgoingMultiplier = fadeOutDuration <= 0f
            ? 0f
            : 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
        incomingMultiplier = fadeInDuration <= 0f
            ? 1f
            : Mathf.Clamp01(elapsed / fadeInDuration);
    }
}

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Database")]
    [SerializeField] private SoundDatabase soundDatabase;

    [Header("BGM Transition")]
    [SerializeField, Min(0f)] private float bgmFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float bgmFadeInDuration = 0.5f;

    [System.Serializable]
    public sealed class ClipRegion
    {
        public AudioClip clip;
        public bool enabled = true;
        [Min(0f)] public float playbackStart;
        [Min(0f)] public float playbackEnd; // 0 = original clip end
        public bool loop = true;
        [Min(0f)] public float loopStart;
        [Min(0f)] public float loopEnd; // 0 = playback end
    }

    [Header("Waveform BGM Regions (edit using Open Waveform Editor)")]
    [SerializeField] private List<ClipRegion> bgmClipRegions = new();

    private ClipRegion FindBgmRegion(AudioClip clip)
    {
        if (clip == null) return null;
        for (int i = 0; i < bgmClipRegions.Count; ++i)
        {
            ClipRegion region = bgmClipRegions[i];
            if (region != null && region.enabled && region.clip == clip) return region;
        }
        return null;
    }

    private static void GetRegionSamples(ClipRegion region, AudioClip clip,
        out int playbackStart, out int playbackEnd, out int loopStart, out int loopEnd)
    {
        int max = Mathf.Max(1, clip.samples);
        int hz = clip.frequency;
        playbackStart = Mathf.Clamp(Mathf.RoundToInt(region.playbackStart * hz), 0, max - 1);
        playbackEnd = region.playbackEnd <= 0f ? max :
            Mathf.Clamp(Mathf.RoundToInt(region.playbackEnd * hz), playbackStart + 1, max);
        loopStart = Mathf.Clamp(Mathf.RoundToInt(region.loopStart * hz), playbackStart, playbackEnd - 1);
        loopEnd = region.loopEnd <= 0f ? playbackEnd :
            Mathf.Clamp(Mathf.RoundToInt(region.loopEnd * hz), loopStart + 1, playbackEnd);
    }

    private Dictionary<string, SoundData> sfxIdDict;
    private List<AudioSource> bgmLayerSources = new();
    private AudioSource standbyBgmSource;
    private List<AudioSource> standbyBgmLayerSources = new();
    private readonly List<RoutedSfxSource> routedSfxSources = new();
    private readonly List<RoutedSfxSource> routedAmbienceSources = new();
    private IReadOnlyList<BgmClipData> activeBgmLayers;
    private IReadOnlyList<BgmClipData> standbyBgmLayers;
    private BgmState? activeBgmState;
    private Coroutine bgmTransitionRoutine;
    private float bgmFadeMultiplier = 1f;
    private float standbyBgmFadeMultiplier;

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
        sfxIdDict = new Dictionary<string, SoundData>(System.StringComparer.Ordinal);

        RegisterSoundDatabase();
    }

    private void RegisterSoundDatabase()
    {
        if (soundDatabase == null)
            return;

        soundDatabase.Initialize();

        foreach (SoundData data in soundDatabase.SfxEntries)
            RegisterSfxId(data);

        foreach (SoundData data in soundDatabase.EventSfxEntries)
            RegisterSfxId(data);
    }

    private void RegisterSfxId(SoundData data)
    {
        if (data == null || data.clip == null)
            return;

        RegisterSfxId(data.id, data);

        if (data.aliases == null)
            return;

        foreach (string alias in data.aliases)
            RegisterSfxId(alias, data);
    }

    private void RegisterSfxId(string id, SoundData data)
    {
        if (data == null || data.clip == null || string.IsNullOrWhiteSpace(id))
            return;

        id = id.Trim();
        if (!sfxIdDict.ContainsKey(id))
            sfxIdDict.Add(id, data);
        else
            Debug.LogWarning($"[AudioManager] Duplicate SFX ID: {id}");
    }

    public void PlayBgm(BgmState state)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] BGM Source is not assigned.");
            return;
        }

        if (soundDatabase == null || !soundDatabase.TryGetBgm(state, out BgmData data))
        {
            Debug.LogWarning($"[AudioManager] BGM state is not configured: {state}");
            return;
        }

        List<BgmClipData> layers = GetBgmLayers(data);
        if (layers.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] BGM state has no playable entries: {state}");
            return;
        }

        if (activeBgmState == state && IsBgmAlreadyPlaying(layers, true))
        {
            activeBgmLayers = layers;
            ApplyVolumes();
            return;
        }

        if (!isActiveAndEnabled || (bgmFadeOutDuration <= 0f && bgmFadeInDuration <= 0f))
        {
            CancelBgmTransition();
            bgmFadeMultiplier = 1f;
            PlayBgmImmediate(state, layers);
            return;
        }

        CancelBgmTransition();
        bgmTransitionRoutine = StartCoroutine(BgmTransitionRoutine(state, layers));
    }

    public bool IsBgmConfigured(BgmState state)
    {
        return soundDatabase != null &&
            soundDatabase.TryGetBgm(state, out BgmData data) &&
            data?.mainClip?.clip != null;
    }

    private void Update()
    {
        MaintainLoopRegion(bgmSource, 0);
        for (int i = 0; i < bgmLayerSources.Count; i++)
            MaintainLoopRegion(bgmLayerSources[i], i + 1);
        MaintainLoopRegion(standbyBgmSource, 0, standbyBgmLayers);
        for (int i = 0; i < standbyBgmLayerSources.Count; i++)
            MaintainLoopRegion(standbyBgmLayerSources[i], i + 1, standbyBgmLayers);
    }

    private void MaintainLoopRegion(AudioSource source, int layerIndex)
    {
        MaintainLoopRegion(source, layerIndex, activeBgmLayers);
    }

    private void MaintainLoopRegion(AudioSource source, int layerIndex, IReadOnlyList<BgmClipData> layers)
    {
        if (source == null || !source.isPlaying || layers == null ||
            layerIndex < 0 || layerIndex >= layers.Count)
            return;

        BgmClipData data = layers[layerIndex];
        if (data == null || data.clip == null) return;
        ClipRegion region = FindBgmRegion(data.clip);
        if (region != null)
        {
            GetRegionSamples(region, data.clip, out int playbackStart, out int playbackEnd,
                out int loopStart, out int loopEnd);
            int boundary = region.loop ? loopEnd : playbackEnd;
            if (source.timeSamples >= boundary)
            {
                if (region.loop)
                    source.timeSamples = loopStart;
                else
                    source.Stop();
            }
            return;
        }
        if (!data.TryGetLoopSamples(out int start, out int end)) return;
        int current = source.timeSamples;
        if (current >= end)
            source.timeSamples = BgmClipData.WrapLoopSample(current, start, end);
    }

    public void PlayBgmState(BgmState state)
    {
        PlayBgm(state);
    }

    private IEnumerator BgmTransitionRoutine(BgmState state, IReadOnlyList<BgmClipData> layers)
    {
        PrepareStandbyBgmBank(layers);
        float outgoingStart = HasPlayingBgm(bgmSource, bgmLayerSources) ? bgmFadeMultiplier : 0f;
        standbyBgmFadeMultiplier = bgmFadeInDuration <= 0f ? 1f : 0f;
        ApplyVolumes();

        float elapsed = 0f;
        float duration = Mathf.Max(bgmFadeOutDuration, bgmFadeInDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            BgmCrossfadePolicy.Evaluate(elapsed, bgmFadeOutDuration, bgmFadeInDuration,
                out float outgoing, out float incoming);
            bgmFadeMultiplier = outgoingStart * outgoing;
            standbyBgmFadeMultiplier = incoming;
            ApplyVolumes();
            yield return null;
        }

        bgmFadeMultiplier = 0f;
        standbyBgmFadeMultiplier = 1f;
        PromoteStandbyBgmBank(state);
        ApplyVolumes();

        bgmTransitionRoutine = null;
    }

    private void PlayBgmImmediate(BgmState state, IReadOnlyList<BgmClipData> layers)
    {
        if (layers == null || layers.Count == 0)
            return;

        StopBgmPlaybackSources();
        activeBgmLayers = layers;
        activeBgmState = state;

        PlayBgmClip(bgmSource, layers[0]);
        EnsureBgmLayerSourceCount(layers.Count - 1);

        for (int i = 1; i < layers.Count; i++)
        {
            AudioSource layerSource = bgmLayerSources[i - 1];
            PlayBgmClip(layerSource, layers[i]);
        }

        StopUnusedBgmLayerSources(layers.Count - 1);
        ApplyVolumes();
    }

    private bool HasPlayingBgm()
    {
        return HasPlayingBgm(bgmSource, bgmLayerSources) ||
            HasPlayingBgm(standbyBgmSource, standbyBgmLayerSources);
    }

    private static bool HasPlayingBgm(AudioSource mainSource, List<AudioSource> layerSources)
    {
        if (mainSource != null && mainSource.isPlaying)
            return true;
        for (int i = 0; i < layerSources.Count; i++)
            if (layerSources[i] != null && layerSources[i].isPlaying)
                return true;
        return false;
    }

    private void CancelBgmTransition()
    {
        if (bgmTransitionRoutine == null)
            return;

        StopCoroutine(bgmTransitionRoutine);
        bgmTransitionRoutine = null;

        if (standbyBgmFadeMultiplier > bgmFadeMultiplier)
            PromoteStandbyBgmBank(activeBgmState);
        else
            StopBgmBank(standbyBgmSource, standbyBgmLayerSources);

        standbyBgmLayers = null;
        standbyBgmFadeMultiplier = 0f;
        ApplyVolumes();
    }

    private static List<BgmClipData> GetBgmLayers(BgmData data)
    {
        List<BgmClipData> layers = new();

        if (data == null || data.mainClip == null || data.mainClip.clip == null)
            return layers;

        layers.Add(data.mainClip);

        if (data.ambienceClips == null)
            return layers;

        foreach (BgmClipData ambienceClip in data.ambienceClips)
        {
            if (ambienceClip != null && ambienceClip.clip != null)
                layers.Add(ambienceClip);
        }

        return layers;
    }

    public void StopBgm()
    {
        CancelBgmTransition();
        bgmFadeMultiplier = 1f;
        activeBgmState = null;
        StopBgmPlaybackSources();
        StopBgmBank(standbyBgmSource, standbyBgmLayerSources);
        standbyBgmLayers = null;
        standbyBgmFadeMultiplier = 0f;
        ApplyVolumes();
    }

    private void StopBgmPlaybackSources()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        activeBgmLayers = null;
        StopUnusedBgmLayerSources(0);
    }

    private void PrepareStandbyBgmBank(IReadOnlyList<BgmClipData> layers)
    {
        EnsureStandbyBgmSource();
        StopBgmBank(standbyBgmSource, standbyBgmLayerSources);
        standbyBgmLayers = layers;
        PlayBgmClip(standbyBgmSource, layers[0]);
        EnsureBgmLayerSourceCount(standbyBgmLayerSources, standbyBgmSource,
            layers.Count - 1, "BGM Standby Layer");
        for (int i = 1; i < layers.Count; i++)
            PlayBgmClip(standbyBgmLayerSources[i - 1], layers[i]);
        StopUnusedBgmLayerSources(standbyBgmLayerSources, layers.Count - 1);
    }

    private void EnsureStandbyBgmSource()
    {
        if (standbyBgmSource != null)
            return;

        GameObject sourceObject = new("BGM Standby");
        sourceObject.transform.SetParent(transform, false);
        standbyBgmSource = sourceObject.AddComponent<AudioSource>();
        CopyBgmSourceSettings(bgmSource, standbyBgmSource);
    }

    private void PromoteStandbyBgmBank(BgmState? state)
    {
        StopBgmBank(bgmSource, bgmLayerSources);
        (bgmSource, standbyBgmSource) = (standbyBgmSource, bgmSource);
        (bgmLayerSources, standbyBgmLayerSources) =
            (standbyBgmLayerSources, bgmLayerSources);
        activeBgmLayers = standbyBgmLayers;
        standbyBgmLayers = null;
        activeBgmState = state;
        bgmFadeMultiplier = standbyBgmFadeMultiplier;
        standbyBgmFadeMultiplier = 0f;
    }

    private static void StopBgmBank(AudioSource mainSource, List<AudioSource> layerSources)
    {
        if (mainSource != null)
        {
            mainSource.Stop();
            mainSource.clip = null;
        }

        StopUnusedBgmLayerSources(layerSources, 0);
    }

    public void PlaySfx(string id)
    {
        PlaySfx(id, 1f);
    }

    public void PlaySfx(string id, float volumeMultiplier)
    {
        if (!TryGetSfxData(id, out SoundData data))
            return;

        Vector3 worldPosition = sfxSource != null ? sfxSource.transform.position : transform.position;
        Quaternion worldRotation = sfxSource != null ? sfxSource.transform.rotation : transform.rotation;

        AudioSourcePlaybackSettings settings =
            AudioSourcePlaybackSettings.From(data, worldPosition, worldRotation, sfxSource);

        PlaySfxClip(settings, volumeMultiplier);
    }

    public bool TryGetSfxData(string id, out SoundData data)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(id))
            return false;

        string trimmedId = id.Trim();

        if (sfxIdDict != null &&
            sfxIdDict.TryGetValue(trimmedId, out data) &&
            data != null &&
            data.clip != null)
        {
            return true;
        }

        Debug.LogWarning($"[AudioManager] SFX ID not found: {trimmedId}");
        data = null;
        return false;
    }

    public bool TryGetAmbienceData(string id, out SoundData data)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(id) || soundDatabase == null)
            return false;

        if (soundDatabase.TryGetAmbience(id, out data) && data != null && data.clip != null)
            return true;

        Debug.LogWarning($"[AudioManager] Ambience ID not found: {id.Trim()}");
        data = null;
        return false;
    }

    public bool TryGetSkillVfxSfx(GameObject vfxPrefab, out VfxSoundData data)
    {
        data = null;

        return soundDatabase != null &&
            soundDatabase.TryGetSkillVfxSfx(vfxPrefab, out data);
    }

    public AudioSource PlayVfxSfxCue(
        VfxSoundCue cue,
        Vector3 worldPosition,
        Quaternion worldRotation,
        float volumeMultiplier = 1f,
        float pitchMultiplier = 1f)
    {
        if (cue == null || cue.clip == null)
            return null;

        if (cue.loop)
        {
            AudioSourcePlaybackSettings settings =
                AudioSourcePlaybackSettings.From(cue, worldPosition, worldRotation, sfxSource);

            settings?.MultiplyPitch(pitchMultiplier);

            return PlaySfxClip(settings, volumeMultiplier);
        }

        AudioSourcePlaybackSettings oneShotSettings =
            AudioSourcePlaybackSettings.From(cue, worldPosition, worldRotation, sfxSource);

        oneShotSettings?.MultiplyPitch(pitchMultiplier);

        return PlaySfxClip(oneShotSettings, volumeMultiplier);
    }

    public void PlaySfxClip(AudioClip clip)
    {
        PlaySfxClip(clip, 1f);
    }

    public void PlaySfxClip(AudioClip clip, float volumeMultiplier)
    {
        PlaySfxClip(clip, volumeMultiplier, 1f);
    }

    private void PlaySfxClip(AudioClip clip, float volumeMultiplier, float pitch)
    {
        if (clip == null)
            return;

        if (sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] SFX Source is not assigned.");
            return;
        }

        AudioSourcePlaybackSettings settings = AudioSourcePlaybackSettings.From(sfxSource);
        if (settings == null)
            return;

        settings.SetClipAndPitch(clip, SoundData.ClampPlaybackPitch(pitch));
        PlaySfxClip(settings, volumeMultiplier);
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

    public AudioSource PlaySfxSource(
        string id,
        Vector3 worldPosition,
        Quaternion worldRotation,
        float volumeMultiplier)
    {
        if (!TryGetSfxData(id, out SoundData data))
            return null;

        AudioSourcePlaybackSettings settings =
            AudioSourcePlaybackSettings.From(data, worldPosition, worldRotation, sfxSource);

        return PlaySfxClip(settings, volumeMultiplier);
    }

    private AudioSource PlayAmbienceClip(AudioSourcePlaybackSettings settings, float volumeMultiplier)
    {
        if (settings == null || settings.Clip == null)
            return null;

        GameObject sourceObject = new($"{settings.Clip.name}_RoutedAmbience");
        sourceObject.transform.SetParent(transform, false);
        sourceObject.transform.SetPositionAndRotation(settings.WorldPosition, settings.WorldRotation);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        settings.ApplyTo(source, Mathf.Clamp01(volumeMultiplier));
        float baseVolume = source.volume;
        routedAmbienceSources.Add(new RoutedSfxSource { source = source, baseVolume = baseVolume });
        ApplyRoutedAmbienceVolume(source, baseVolume);
        source.Play();
        return source;
    }

    public AudioSource PlayAmbienceSource(string id, float volumeMultiplier)
    {
        if (!TryGetAmbienceData(id, out SoundData data))
            return null;

        AudioSourcePlaybackSettings settings =
            AudioSourcePlaybackSettings.From(data, transform.position, transform.rotation, sfxSource);
        settings.Force2DLoop();

        return PlayAmbienceClip(settings, volumeMultiplier);
    }

    public void StopRoutedSfxSource(AudioSource routedSource)
    {
        if (routedSource == null)
            return;

        UnregisterRoutedSfxSource(routedSource);
        UnregisterRoutedAmbienceSource(routedSource);

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

    public void SetSfxVolume(string id, float volume)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        string trimmedId = id.Trim();
        if (sfxIdDict == null ||
            !sfxIdDict.TryGetValue(trimmedId, out SoundData data) ||
            data == null)
        {
            Debug.LogWarning($"[AudioManager] SFX not found: {trimmedId}");
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

    public float GetSfxVolume(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return 1f;

        string trimmedId = id.Trim();
        if (sfxIdDict == null ||
            !sfxIdDict.TryGetValue(trimmedId, out SoundData data) ||
            data == null)
            return 1f;

        return Mathf.Clamp01(data.volume);
    }

    private void ApplyVolumes()
    {
        float master = GetMasterVolumeOrDefault();
        float bgm = GetBgmVolumeOrDefault();
        float sfx = GetSfxVolumeOrDefault();

        ApplyBgmBankVolumes(bgmSource, bgmLayerSources, activeBgmLayers,
            master * bgm * bgmFadeMultiplier);
        ApplyBgmBankVolumes(standbyBgmSource, standbyBgmLayerSources, standbyBgmLayers,
            master * bgm * standbyBgmFadeMultiplier);

        if (sfxSource != null)
            sfxSource.volume = master * sfx;

        ApplyRoutedSfxVolumes();
        ApplyRoutedAmbienceVolumes();
    }

    private bool IsBgmAlreadyPlaying(IReadOnlyList<BgmClipData> layers, bool loop)
    {
        if (layers == null || layers.Count == 0)
            return false;

        if (!IsBgmSourcePlayingClip(bgmSource, layers[0].clip,
            FindBgmRegion(layers[0].clip) == null && UsesAudioSourceLoop(layers[0])))
            return false;

        for (int i = 1; i < layers.Count; i++)
        {
            int layerIndex = i - 1;

            if (layerIndex >= bgmLayerSources.Count)
                return false;

            if (!IsBgmSourcePlayingClip(bgmLayerSources[layerIndex], layers[i].clip,
                FindBgmRegion(layers[i].clip) == null && UsesAudioSourceLoop(layers[i])))
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

    private void PlayBgmClip(AudioSource source, BgmClipData data)
    {
        if (source == null || data == null || data.clip == null)
            return;

        if (source.clip != data.clip)
        {
            source.Stop();
            source.clip = data.clip;
        }

        ClipRegion region = FindBgmRegion(data.clip);
        source.loop = region != null ? false : UsesAudioSourceLoop(data);
        source.pitch = data.GetPlaybackPitch();
        source.Play();
        if (region != null)
        {
            GetRegionSamples(region, data.clip, out int start, out _, out _, out _);
            source.timeSamples = start;
        }
    }

    private static bool UsesAudioSourceLoop(BgmClipData data)
    {
        return data != null && data.loop && !data.TryGetLoopSamples(out _, out _);
    }

    private void EnsureBgmLayerSourceCount(int count)
    {
        EnsureBgmLayerSourceCount(bgmLayerSources, bgmSource, count, "BGM Layer");
    }

    private void EnsureBgmLayerSourceCount(List<AudioSource> sources, AudioSource template,
        int count, string objectName)
    {
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            if (sources[i] == null)
                sources.RemoveAt(i);
        }

        while (sources.Count < count)
        {
            GameObject layerObject = new($"{objectName} {sources.Count + 1}");
            layerObject.transform.SetParent(transform, false);

            AudioSource layerSource = layerObject.AddComponent<AudioSource>();
            CopyBgmSourceSettings(template, layerSource);
            sources.Add(layerSource);
        }
    }

    private void StopUnusedBgmLayerSources(int activeCount)
    {
        StopUnusedBgmLayerSources(bgmLayerSources, activeCount);
    }

    private static void StopUnusedBgmLayerSources(List<AudioSource> sources, int activeCount)
    {
        for (int i = 0; i < sources.Count; i++)
        {
            AudioSource layerSource = sources[i];

            if (layerSource == null || i < activeCount)
                continue;

            layerSource.Stop();
            layerSource.clip = null;
        }
    }

    private static void ApplyBgmBankVolumes(AudioSource mainSource, List<AudioSource> layerSources,
        IReadOnlyList<BgmClipData> layers, float volume)
    {
        if (mainSource != null)
            mainSource.volume = volume * GetBgmLayerVolume(layers, 0);

        for (int i = layerSources.Count - 1; i >= 0; i--)
        {
            AudioSource layerSource = layerSources[i];

            if (layerSource == null)
            {
                layerSources.RemoveAt(i);
                continue;
            }

            layerSource.volume = volume * GetBgmLayerVolume(layers, i + 1);
        }
    }

    private static float GetBgmLayerVolume(IReadOnlyList<BgmClipData> layers, int layerIndex)
    {
        if (layers == null || layerIndex < 0 || layerIndex >= layers.Count)
            return 1f;

        BgmClipData data = layers[layerIndex];
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

    private void ApplyRoutedAmbienceVolumes()
    {
        for (int i = routedAmbienceSources.Count - 1; i >= 0; i--)
        {
            RoutedSfxSource routed = routedAmbienceSources[i];
            if (routed == null || routed.source == null)
            {
                routedAmbienceSources.RemoveAt(i);
                continue;
            }
            ApplyRoutedAmbienceVolume(routed.source, routed.baseVolume);
        }
    }

    private void ApplyRoutedAmbienceVolume(AudioSource source, float baseVolume)
    {
        if (source != null)
            source.volume = Mathf.Clamp01(baseVolume) * GetAmbienceOutputVolumeMultiplier();
    }

    private void UnregisterRoutedAmbienceSource(AudioSource source)
    {
        for (int i = routedAmbienceSources.Count - 1; i >= 0; i--)
            if (routedAmbienceSources[i].source == source)
                routedAmbienceSources.RemoveAt(i);
    }

    private static float GetAmbienceOutputVolumeMultiplier()
    {
        return GetMasterVolumeOrDefault() * GetBgmVolumeOrDefault();
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

    public static AudioSourcePlaybackSettings From(
        SoundData data,
        Vector3 worldPosition,
        Quaternion worldRotation,
        AudioSource template)
    {
        if (data == null || data.clip == null)
            return null;

        return new AudioSourcePlaybackSettings
        {
            Clip = data.clip,
            OutputAudioMixerGroup = template != null ? template.outputAudioMixerGroup : null,
            WorldPosition = worldPosition,
            WorldRotation = worldRotation,
            BypassEffects = template != null && template.bypassEffects,
            BypassListenerEffects = template != null && template.bypassListenerEffects,
            BypassReverbZones = template != null && template.bypassReverbZones,
            Loop = data.loop,
            Priority = template != null ? template.priority : 128,
            Volume = Mathf.Clamp01(data.volume),
            Pitch = data.GetPlaybackPitch(),
            PanStereo = template != null ? template.panStereo : 0f,
            SpatialBlend = template != null ? template.spatialBlend : 0f,
            ReverbZoneMix = template != null ? template.reverbZoneMix : 1f,
            DopplerLevel = template != null ? template.dopplerLevel : 1f,
            Spread = template != null ? template.spread : 0f,
            RolloffMode = template != null ? template.rolloffMode : AudioRolloffMode.Logarithmic,
            MinDistance = template != null ? template.minDistance : 1f,
            MaxDistance = template != null ? template.maxDistance : 500f,
            IgnoreListenerPause = template != null && template.ignoreListenerPause,
            IgnoreListenerVolume = template != null && template.ignoreListenerVolume,
            Spatialize = template != null && template.spatialize,
            SpatializePostEffects = template != null && template.spatializePostEffects,
            VelocityUpdateMode = template != null
                ? template.velocityUpdateMode
                : AudioVelocityUpdateMode.Auto,
            customRolloffCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.CustomRolloff))
                : null,
            spatialBlendCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.SpatialBlend))
                : null,
            reverbZoneMixCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix))
                : null,
            spreadCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.Spread))
                : null
        };
    }

    public static AudioSourcePlaybackSettings From(
        VfxSoundCue cue,
        Vector3 worldPosition,
        Quaternion worldRotation,
        AudioSource template)
    {
        if (cue == null || cue.clip == null)
            return null;

        return new AudioSourcePlaybackSettings
        {
            Clip = cue.clip,
            OutputAudioMixerGroup = template != null ? template.outputAudioMixerGroup : null,
            WorldPosition = worldPosition,
            WorldRotation = worldRotation,
            BypassEffects = template != null && template.bypassEffects,
            BypassListenerEffects = template != null && template.bypassListenerEffects,
            BypassReverbZones = template != null && template.bypassReverbZones,
            Loop = cue.loop,
            Priority = template != null ? template.priority : 128,
            Volume = Mathf.Clamp01(cue.volume),
            Pitch = cue.GetPlaybackPitch(),
            PanStereo = template != null ? template.panStereo : 0f,
            SpatialBlend = cue.overrideSpatialBlend
                ? Mathf.Clamp01(cue.spatialBlend)
                : template != null ? template.spatialBlend : 0f,
            ReverbZoneMix = template != null ? template.reverbZoneMix : 1f,
            DopplerLevel = template != null ? template.dopplerLevel : 1f,
            Spread = template != null ? template.spread : 0f,
            RolloffMode = template != null ? template.rolloffMode : AudioRolloffMode.Logarithmic,
            MinDistance = template != null ? template.minDistance : 1f,
            MaxDistance = template != null ? template.maxDistance : 500f,
            IgnoreListenerPause = template != null && template.ignoreListenerPause,
            IgnoreListenerVolume = template != null && template.ignoreListenerVolume,
            Spatialize = template != null && template.spatialize,
            SpatializePostEffects = template != null && template.spatializePostEffects,
            VelocityUpdateMode = template != null
                ? template.velocityUpdateMode
                : AudioVelocityUpdateMode.Auto,
            customRolloffCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.CustomRolloff))
                : null,
            spatialBlendCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.SpatialBlend))
                : null,
            reverbZoneMixCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix))
                : null,
            spreadCurve = template != null
                ? CopyCurve(template.GetCustomCurve(AudioSourceCurveType.Spread))
                : null
        };
    }

    public void SetClipAndPitch(AudioClip clip, float pitch)
    {
        Clip = clip;
        Loop = false;
        Pitch = SoundData.ClampPlaybackPitch(pitch);
    }

    public void MultiplyPitch(float multiplier)
    {
        Pitch = BattleVfxAudioUtility.CalculatePlaybackPitch(Pitch, multiplier);
    }

    public void Force2DLoop()
    {
        Loop = true;
        SpatialBlend = 0f;
        DopplerLevel = 0f;
        Spatialize = false;
        SpatializePostEffects = false;
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
