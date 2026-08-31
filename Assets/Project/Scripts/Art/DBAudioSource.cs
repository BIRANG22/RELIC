using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// AudioSource처럼 오브젝트에 붙여 사용하되,
/// AudioClip을 직접 참조하지 않고 AudioManager에 등록된 SFX ID를 재생합니다.
/// 필요하면 오브젝트 활성화 / 호버 / 클릭 시 DB SFX를 각각 재생할 수 있습니다.
/// </summary>
public class DBAudioSource : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sound")]
    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string soundId = "event..09.Skylight";

    [SerializeField, Range(0f, 1f)]
    private float volume = 1f;

    [Header("Playback")]
    [Tooltip("사운드를 재생하기 전 대기 시간입니다. 0이면 즉시 재생합니다.")]
    [SerializeField, Min(0f)]
    private float playDelay = 0f;

    [Tooltip("오브젝트가 활성화될 때 기본 Sound의 사운드를 자동으로 재생합니다.")]
    [SerializeField]
    private bool playOnEnable = true;

    [Header("Hover Sound")]
    [Tooltip("이 오브젝트에 포인터가 올라왔을 때 사운드를 재생합니다.")]
    [SerializeField]
    private bool playOnHover = false;

    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string hoverSoundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float hoverVolume = 1f;

    [Header("Click Sound")]
    [Tooltip("이 오브젝트를 클릭했을 때 사운드를 재생합니다.")]
    [SerializeField]
    private bool playOnClick = false;

    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string clickSoundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float clickVolume = 1f;

    private Coroutine delayedPlayCoroutine;

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        if (delayedPlayCoroutine != null)
        {
            StopCoroutine(delayedPlayCoroutine);
            delayedPlayCoroutine = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playOnHover)
            return;

        PlaySound(hoverSoundId, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playOnClick)
            return;

        PlaySound(clickSoundId, clickVolume);
    }

    /// <summary>
    /// 인스펙터에 설정된 기본 볼륨으로 기본 SFX를 재생합니다.
    /// UnityEvent에서도 직접 호출할 수 있습니다.
    /// </summary>
    public void Play()
    {
        Play(1f);
    }

    /// <summary>
    /// 기본 볼륨에 추가 배율을 곱해 기본 SFX를 재생합니다.
    /// </summary>
    public void Play(float volumeMultiplier)
    {
        PlaySound(soundId, Mathf.Clamp01(volume) * Mathf.Clamp01(volumeMultiplier));
    }

    private void PlaySound(string targetSoundId, float targetVolume)
    {
        if (string.IsNullOrWhiteSpace(targetSoundId))
            return;

        if (playDelay <= 0f)
        {
            PlayImmediate(targetSoundId, targetVolume);
            return;
        }

        if (delayedPlayCoroutine != null)
            StopCoroutine(delayedPlayCoroutine);

        delayedPlayCoroutine = StartCoroutine(PlayDelayedRoutine(targetSoundId, targetVolume));
    }

    private IEnumerator PlayDelayedRoutine(string targetSoundId, float targetVolume)
    {
        yield return new WaitForSecondsRealtime(playDelay);
        delayedPlayCoroutine = null;
        PlayImmediate(targetSoundId, targetVolume);
    }

    private void PlayImmediate(string targetSoundId, float targetVolume)
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(DBAudioSource)}] AudioManager.Instance를 찾지 못했습니다. Object: {name}", this);
            return;
        }

        AudioManager.Instance.PlaySfx(targetSoundId, Mathf.Clamp01(targetVolume));
    }

    /// <summary>
    /// 런타임에서 기본 재생용 DB 사운드 ID를 교체합니다.
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
