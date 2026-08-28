using System.Collections;
using UnityEngine;

/// <summary>
/// AudioSource처럼 오브젝트에 붙여 사용하되,
/// AudioClip을 직접 참조하지 않고 AudioManager에 등록된 SFX ID를 재생합니다.
/// </summary>
public class DBAudioSource : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string soundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float volume = 1f;

    [Header("Playback")]
    [Tooltip("오브젝트가 활성화될 때 자동으로 재생합니다.")]
    [SerializeField]
    private bool playOnEnable = true;

    [Tooltip("활성화된 같은 프레임보다 한 프레임 뒤에 재생합니다. AudioManager 초기화 순서가 필요한 경우에 유용합니다.")]
    [SerializeField]
    private bool playNextFrameOnEnable = true;

    private Coroutine playOnEnableCoroutine;

    private void OnEnable()
    {
        if (!playOnEnable)
            return;

        if (playNextFrameOnEnable)
        {
            playOnEnableCoroutine = StartCoroutine(PlayNextFrameRoutine());
            return;
        }

        Play();
    }

    private void OnDisable()
    {
        if (playOnEnableCoroutine == null)
            return;

        StopCoroutine(playOnEnableCoroutine);
        playOnEnableCoroutine = null;
    }

    private IEnumerator PlayNextFrameRoutine()
    {
        yield return null;
        playOnEnableCoroutine = null;

        if (isActiveAndEnabled)
            Play();
    }

    /// <summary>
    /// 인스펙터에 설정된 볼륨으로 SFX를 재생합니다.
    /// UnityEvent에서도 직접 호출할 수 있습니다.
    /// </summary>
    public void Play()
    {
        Play(1f);
    }

    /// <summary>
    /// 인스펙터 볼륨에 추가 배율을 곱해 SFX를 재생합니다.
    /// </summary>
    public void Play(float volumeMultiplier)
    {
        if (string.IsNullOrWhiteSpace(soundId))
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(DBAudioSource)}] AudioManager.Instance를 찾지 못했습니다. Object: {name}", this);
            return;
        }

        float finalVolume = Mathf.Clamp01(volume) * Mathf.Clamp01(volumeMultiplier);
        AudioManager.Instance.PlaySfx(soundId, finalVolume);
    }

    /// <summary>
    /// 런타임에서 재생할 DB 사운드 ID를 교체합니다.
    /// </summary>
    public void SetSoundId(string id)
    {
        soundId = id;
    }

    /// <summary>
    /// 런타임에서 기본 볼륨을 변경합니다.
    /// </summary>
    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
    }
}
