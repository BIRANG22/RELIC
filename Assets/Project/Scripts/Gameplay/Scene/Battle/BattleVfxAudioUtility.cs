using System.Collections;
using UnityEngine;

public static class BattleVfxAudioUtility
{
    public static void PlayAndStripEmbeddedAudioSources(
        GameObject vfx,
        BattleVfxSfxEntry settings,
        MonoBehaviour coroutineHost)
    {
        PlayConfiguredSfx(settings, coroutineHost);

        if (vfx == null)
            return;

        AudioSource[] sources = vfx.GetComponentsInChildren<AudioSource>(true);

        if (ShouldRouteEmbeddedAudioSources(settings))
            PlayEmbeddedAudioSources(sources, settings);

        if (ShouldRemoveEmbeddedAudioSources(settings))
            RemoveEmbeddedAudioSources(sources);
    }

    private static void PlayConfiguredSfx(BattleVfxSfxEntry settings, MonoBehaviour coroutineHost)
    {
        if (settings == null || !settings.playSfx)
            return;

        if (settings.delay > 0f && coroutineHost != null && coroutineHost.isActiveAndEnabled)
        {
            coroutineHost.StartCoroutine(PlayConfiguredSfxDelayed(settings));
            return;
        }

        PlayConfiguredSfxNow(settings);
    }

    private static IEnumerator PlayConfiguredSfxDelayed(BattleVfxSfxEntry settings)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, settings.delay));
        PlayConfiguredSfxNow(settings);
    }

    private static void PlayConfiguredSfxNow(BattleVfxSfxEntry settings)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(settings.sfxId, settings.volumeMultiplier);
    }

    private static bool ShouldRouteEmbeddedAudioSources(BattleVfxSfxEntry settings)
    {
        if (settings == null)
            return true;

        return settings.routeEmbeddedAudioSourcesThroughAudioManager && !settings.playSfx;
    }

    private static bool ShouldRemoveEmbeddedAudioSources(BattleVfxSfxEntry settings)
    {
        return settings == null || settings.removeEmbeddedAudioSources;
    }

    private static void PlayEmbeddedAudioSources(
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

            AudioManager.Instance.PlaySfxClip(source.clip, source.volume * entryVolume);
        }
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
