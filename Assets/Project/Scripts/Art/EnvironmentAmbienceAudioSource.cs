using UnityEngine;

/// <summary>
/// 환경 프리팹에 붙여 활성 상태 동안 SoundDatabase ambience를 2D 루프로 재생합니다.
/// </summary>
public sealed class EnvironmentAmbienceAudioSource : MonoBehaviour
{
    [SerializeField, SoundId(SoundCategory.Ambience)]
    private string ambienceId;

    [SerializeField, Range(0f, 1f)]
    private float volume = 1f;

    private AudioSource activeSource;

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    public void Play()
    {
        if (activeSource != null || string.IsNullOrWhiteSpace(ambienceId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning($"[{nameof(EnvironmentAmbienceAudioSource)}] AudioManager.Instance를 찾지 못했습니다. Object: {name}", this);
            return;
        }

        activeSource = audioManager.PlayAmbienceSource(ambienceId, volume);
    }

    public void Stop()
    {
        if (activeSource == null)
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
            audioManager.StopRoutedSfxSource(activeSource);
        else
            activeSource.Stop();

        activeSource = null;
    }

    public void SetAmbienceId(string id)
    {
        if (ambienceId == id)
            return;

        Stop();
        ambienceId = id;

        if (isActiveAndEnabled)
            Play();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
    }
}
