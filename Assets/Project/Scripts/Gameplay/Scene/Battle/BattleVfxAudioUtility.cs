using System.Collections;
using UnityEngine;

public static class BattleVfxAudioUtility
{
    public static void PlayAndStripEmbeddedAudioSources(
        GameObject vfx,
        GameObject sourcePrefab,
        MonoBehaviour coroutineHost)
    {
        PlayDatabaseVfxSfx(vfx, sourcePrefab, coroutineHost);

        if (vfx == null)
            return;

        AudioSource[] sources = vfx.GetComponentsInChildren<AudioSource>(true);
        RemoveEmbeddedAudioSources(sources);
    }

    private static void PlayDatabaseVfxSfx(
        GameObject vfx,
        GameObject sourcePrefab,
        MonoBehaviour coroutineHost)
    {
        if (AudioManager.Instance == null || sourcePrefab == null)
            return;

        if (!AudioManager.Instance.TryGetSkillVfxSfx(sourcePrefab, out VfxSoundData data) ||
            data == null ||
            data.Cues == null)
        {
            return;
        }

        for (int i = 0; i < data.Cues.Count; i++)
        {
            VfxSoundCue cue = data.Cues[i];

            if (cue == null)
                continue;

            PlayDatabaseVfxSfxCue(vfx, cue, coroutineHost);
        }
    }

    private static void PlayDatabaseVfxSfxCue(
        GameObject vfx,
        VfxSoundCue cue,
        MonoBehaviour coroutineHost)
    {
        if (cue == null || cue.clip == null)
            return;

        float delay = Mathf.Max(0f, cue.delay);
        if (delay > 0f && coroutineHost != null && coroutineHost.isActiveAndEnabled)
        {
            coroutineHost.StartCoroutine(PlayDatabaseVfxSfxDelayed(
                vfx,
                cue,
                delay));
            return;
        }

        PlayDatabaseVfxSfxNow(vfx, cue);
    }

    private static IEnumerator PlayDatabaseVfxSfxDelayed(
        GameObject vfx,
        VfxSoundCue cue,
        float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        PlayDatabaseVfxSfxNow(vfx, cue);
    }

    private static void PlayDatabaseVfxSfxNow(
        GameObject vfx,
        VfxSoundCue cue)
    {
        if (AudioManager.Instance == null || cue == null || cue.clip == null)
            return;

        Transform vfxTransform = vfx != null ? vfx.transform : null;
        AudioSource routedSource = AudioManager.Instance.PlayVfxSfxCue(
            cue,
            vfxTransform != null ? vfxTransform.position : Vector3.zero,
            vfxTransform != null ? vfxTransform.rotation : Quaternion.identity);

        if (routedSource != null && routedSource.loop)
            TrackLoopedRoutedAudioSource(vfx, routedSource);
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
