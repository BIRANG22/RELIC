using System.Collections;
using UnityEngine;

public static class BattleVfxAudioUtility
{
    public static void PlayAndStripEmbeddedAudioSources(
        GameObject vfx,
        BattleVfxSfxEntry settings,
        MonoBehaviour coroutineHost)
    {
        PlayConfiguredSfx(vfx, settings, coroutineHost);

        if (vfx == null)
            return;

        AudioSource[] sources = vfx.GetComponentsInChildren<AudioSource>(true);

        if (ShouldRouteEmbeddedAudioSources(settings))
            PlayEmbeddedAudioSources(vfx, sources, settings);

        if (ShouldRemoveEmbeddedAudioSources(settings))
            RemoveEmbeddedAudioSources(sources);
    }

    private static void PlayConfiguredSfx(
        GameObject vfx,
        BattleVfxSfxEntry settings,
        MonoBehaviour coroutineHost)
    {
        if (settings == null)
            return;

        if (settings.playSfx)
        {
            PlayConfiguredSfxCue(
                vfx,
                settings.sfxId,
                settings.delay,
                settings.volumeMultiplier,
                coroutineHost);
        }

        if (settings.additionalSfx == null)
            return;

        for (int i = 0; i < settings.additionalSfx.Count; i++)
        {
            BattleVfxAdditionalSfxEntry cue = settings.additionalSfx[i];

            if (cue == null)
                continue;

            PlayConfiguredSfxCue(
                vfx,
                cue.sfxId,
                cue.delay,
                cue.volumeMultiplier,
                coroutineHost);
        }
    }

    private static void PlayConfiguredSfxCue(
        GameObject vfx,
        string sfxId,
        float delay,
        float volumeMultiplier,
        MonoBehaviour coroutineHost)
    {
        if (string.IsNullOrWhiteSpace(sfxId))
            return;

        if (delay > 0f && coroutineHost != null && coroutineHost.isActiveAndEnabled)
        {
            coroutineHost.StartCoroutine(PlayConfiguredSfxDelayed(
                vfx,
                sfxId,
                delay,
                volumeMultiplier));
            return;
        }

        PlayConfiguredSfxNow(vfx, sfxId, volumeMultiplier);
    }

    private static IEnumerator PlayConfiguredSfxDelayed(
        GameObject vfx,
        string sfxId,
        float delay,
        float volumeMultiplier)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        PlayConfiguredSfxNow(vfx, sfxId, volumeMultiplier);
    }

    private static void PlayConfiguredSfxNow(
        GameObject vfx,
        string sfxId,
        float volumeMultiplier)
    {
        if (AudioManager.Instance == null)
            return;

        if (!AudioManager.Instance.TryGetSfxData(sfxId, out SoundData data))
            return;

        if (data.loop)
        {
            Transform vfxTransform = vfx != null ? vfx.transform : null;
            AudioSource routedSource = AudioManager.Instance.PlaySfxSource(
                sfxId,
                vfxTransform != null ? vfxTransform.position : Vector3.zero,
                vfxTransform != null ? vfxTransform.rotation : Quaternion.identity,
                volumeMultiplier);

            if (routedSource != null)
                TrackLoopedRoutedAudioSource(vfx, routedSource);

            return;
        }

        AudioManager.Instance.PlaySfx(sfxId, volumeMultiplier);
    }

    private static bool ShouldRouteEmbeddedAudioSources(BattleVfxSfxEntry settings)
    {
        if (settings == null)
            return true;

        return settings.routeEmbeddedAudioSourcesThroughAudioManager && !HasConfiguredSfx(settings);
    }

    private static bool HasConfiguredSfx(BattleVfxSfxEntry settings)
    {
        if (settings == null)
            return false;

        if (settings.playSfx && !string.IsNullOrWhiteSpace(settings.sfxId))
            return true;

        if (settings.additionalSfx == null)
            return false;

        for (int i = 0; i < settings.additionalSfx.Count; i++)
        {
            BattleVfxAdditionalSfxEntry cue = settings.additionalSfx[i];

            if (cue != null && !string.IsNullOrWhiteSpace(cue.sfxId))
                return true;
        }

        return false;
    }

    private static bool ShouldRemoveEmbeddedAudioSources(BattleVfxSfxEntry settings)
    {
        return settings == null || settings.removeEmbeddedAudioSources;
    }

    private static void PlayEmbeddedAudioSources(
        GameObject vfx,
        AudioSource[] sources,
        BattleVfxSfxEntry settings)
    {
        if (AudioManager.Instance == null || sources == null)
            return;

        float entryVolume = settings != null ? Mathf.Clamp01(settings.volumeMultiplier) : 1f;

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];

            if (source == null ||
                !source.enabled ||
                source.mute ||
                !source.playOnAwake ||
                source.clip == null)
            {
                continue;
            }

            AudioSource routedSource = AudioManager.Instance.PlaySfxClip(source, entryVolume);

            if (routedSource != null && routedSource.loop)
                TrackLoopedRoutedAudioSource(vfx, routedSource);
        }
    }

    private static void TrackLoopedRoutedAudioSource(GameObject vfx, AudioSource routedSource)
    {
        if (vfx == null || routedSource == null)
            return;

        BattleVfxRoutedAudioCleanup cleanup =
            vfx.GetComponent<BattleVfxRoutedAudioCleanup>();

        if (cleanup == null)
            cleanup = vfx.AddComponent<BattleVfxRoutedAudioCleanup>();

        cleanup.Track(routedSource);
    }

    private static void RemoveEmbeddedAudioSources(AudioSource[] sources)
    {
        if (sources == null)
            return;

        for (int i = sources.Length - 1; i >= 0; i--)
        {
            AudioSource source = sources[i];

            if (source == null)
                continue;

            source.Stop();
            source.enabled = false;
            DestroyUnityObject(source);
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }
}

public sealed class BattleVfxRoutedAudioCleanup : MonoBehaviour
{
    private readonly System.Collections.Generic.List<AudioSource> routedSources = new();

    public void Track(AudioSource source)
    {
        if (source != null && !routedSources.Contains(source))
            routedSources.Add(source);
    }

    private void OnDestroy()
    {
        for (int i = routedSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = routedSources[i];

            if (source == null)
                continue;

            if (AudioManager.Instance != null)
                AudioManager.Instance.StopRoutedSfxSource(source);
            else
                Destroy(source.gameObject);
        }

        routedSources.Clear();
    }
}
