using System.Collections;
using UnityEngine;

/// <summary>
/// IntroSequenceController에서 특정 연출 오브젝트가 활성화될 때
/// 지정한 시간 후 AudioManager의 사운드 DB에 등록된 SFX를 재생합니다.
///
/// 사용 방법:
/// 1. IntroSequenceController의 특정 Object Action 대상 또는 그 자식 오브젝트에 이 컴포넌트를 붙입니다.
/// 2. IntroSequenceController에서 해당 오브젝트를 활성화하도록 설정합니다.
/// 3. Effect Sound Delay를 해당 연출이 끝나는 시점에 맞게 설정합니다.
/// </summary>
public class IntroSequenceEffectAudioSource : MonoBehaviour
{
    [Header("Sound")]
    [Tooltip("연출 후 재생할 SFX입니다. AudioManager의 사운드 DB에서 선택합니다.")]
    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string soundId = AudioIds.Sfx.NormalButtonClick;

    [Tooltip("재생 볼륨입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float volume = 1f;

    [Header("Playback")]
    [Tooltip("오브젝트가 활성화될 때 자동으로 사운드 예약을 시작합니다.")]
    [SerializeField]
    private bool playOnEnable = true;

    [Tooltip("연출 시작 후 사운드를 재생하기까지의 시간입니다. 연출 Duration과 맞추면 연출 종료 후 사운드가 재생됩니다.")]
    [SerializeField, Min(0f)]
    private float effectSoundDelay = 0f;

    [Tooltip("활성화된 같은 프레임이 아니라 한 프레임 뒤부터 딜레이를 계산합니다.")]
    [SerializeField]
    private bool startNextFrame = false;

    [Tooltip("이미 사운드 재생이 예약되어 있을 때 Play를 다시 호출하면 기존 예약을 취소하고 처음부터 다시 예약합니다.")]
    [SerializeField]
    private bool restartIfAlreadyScheduled = true;

    private Coroutine playCoroutine;

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        CancelScheduledPlay();
    }

    /// <summary>
    /// 인스펙터에 지정된 딜레이 후 SFX를 재생합니다.
    /// UnityEvent에서도 직접 호출할 수 있습니다.
    /// </summary>
    public void Play()
    {
        Play(1f);
    }

    /// <summary>
    /// 기본 볼륨에 추가 배율을 곱해 딜레이 후 SFX를 재생합니다.
    /// </summary>
    public void Play(float volumeMultiplier)
    {
        if (playCoroutine != null)
        {
            if (!restartIfAlreadyScheduled)
                return;

            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        playCoroutine = StartCoroutine(PlayRoutine(volumeMultiplier));
    }

    /// <summary>
    /// 예약된 사운드 재생을 취소합니다.
    /// </summary>
    public void CancelScheduledPlay()
    {
        if (playCoroutine == null)
            return;

        StopCoroutine(playCoroutine);
        playCoroutine = null;
    }

    private IEnumerator PlayRoutine(float volumeMultiplier)
    {
        if (startNextFrame)
            yield return null;

        if (effectSoundDelay > 0f)
            yield return new WaitForSecondsRealtime(effectSoundDelay);

        playCoroutine = null;
        PlayImmediate(volumeMultiplier);
    }

    private void PlayImmediate(float volumeMultiplier)
    {
        if (string.IsNullOrWhiteSpace(soundId))
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(IntroSequenceEffectAudioSource)}] AudioManager.Instance를 찾지 못했습니다. Object: {name}", this);
            return;
        }

        float finalVolume = Mathf.Clamp01(volume) * Mathf.Clamp01(volumeMultiplier);
        AudioManager.Instance.PlaySfx(soundId, finalVolume);
    }

    /// <summary>
    /// 런타임에서 재생할 DB 사운드 ID를 변경합니다.
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

    /// <summary>
    /// 런타임에서 연출 후 재생 딜레이를 변경합니다.
    /// </summary>
    public void SetEffectSoundDelay(float value)
    {
        effectSoundDelay = Mathf.Max(0f, value);
    }
}
