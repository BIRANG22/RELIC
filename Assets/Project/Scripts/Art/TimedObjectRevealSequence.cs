using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedObjectRevealSequence : MonoBehaviour, IBattleRoomIntroSequence
{
    [Serializable]
    public class RevealTarget
    {
        [Tooltip("지정한 시간에 나타날 오브젝트")]
        public GameObject targetObject;

        [Tooltip("연출 시작 후 오브젝트가 나타나는 시간")]
        [Min(0f)]
        public float revealTime = 0f;

        [Header("등장 효과음")]
        [Tooltip("오브젝트가 등장할 때 재생할 효과음")]
        public AudioClip revealSound;

        [Range(0f, 1f)]
        [Tooltip("효과음 볼륨")]
        public float soundVolume = 1f;

        [Range(0.1f, 3f)]
        [Tooltip("효과음 재생 속도와 음높이")]
        public float soundPitch = 1f;

        [Header("등장 시 카메라 흔들림")]
        [Tooltip("이 오브젝트가 나타날 때 카메라를 흔듭니다.")]
        public bool useCameraShake = true;

        [Tooltip("카메라 흔들림 지속 시간")]
        [Min(0f)]
        public float shakeDuration = 0.2f;

        [Tooltip("카메라 흔들림 세기")]
        [Min(0f)]
        public float shakeStrength = 0.15f;

        [Tooltip("카메라 흔들림 속도")]
        [Min(0f)]
        public float shakeFrequency = 30f;
    }

    [Header("순차 등장 오브젝트")]
    [Tooltip("각 오브젝트와 등장 시간을 설정합니다.")]
    [SerializeField]
    private List<RevealTarget> revealTargets = new();

    [Header("효과음 설정")]
    [Tooltip("등장 효과음을 재생할 Audio Source")]
    [SerializeField]
    private AudioSource audioSource;

    [Tooltip("Audio Source가 없으면 현재 오브젝트에 자동으로 생성")]
    [SerializeField]
    private bool createAudioSourceAutomatically = true;

    [Header("카메라 흔들림")]
    [Tooltip("메인 카메라의 CameraShakeController")]
    [SerializeField]
    private CameraShakeController cameraShakeController;

    [Header("연출 종료 후 활성화")]
    [Tooltip("전체 연출이 끝난 뒤 활성화할 오브젝트들")]
    [SerializeField]
    private List<GameObject> objectsToActivateAfterFinish = new();

    [Tooltip("게임 시작 시 종료 후 활성화할 오브젝트들을 숨김")]
    [SerializeField]
    private bool hideFinishObjectsOnAwake = true;

    [Tooltip("연출을 실행할 때 종료 후 활성화할 오브젝트들을 다시 숨김")]
    [SerializeField]
    private bool hideFinishObjectsOnPlay = true;

    [Header("종료 설정")]
    [Tooltip("모든 등장 오브젝트가 나타난 뒤 유지되는 시간")]
    [Min(0f)]
    [SerializeField]
    private float finalHoldTime = 1f;

    [Tooltip("유지 시간이 끝나면 등장 오브젝트들을 모두 숨김")]
    [SerializeField]
    private bool hideRevealTargetsAfterFinish = true;

    [Tooltip("연출 종료 후 이 스크립트가 붙은 오브젝트를 비활성화")]
    [SerializeField]
    private bool disableSequenceObjectAfterFinish = false;

    [Header("실행 설정")]
    [Tooltip("게임 시작 시 등장 오브젝트들을 모두 숨김")]
    [SerializeField]
    private bool hideRevealTargetsOnAwake = true;

    [Tooltip("이 오브젝트가 활성화될 때 자동 실행")]
    [SerializeField]
    private bool playOnEnable = false;

    [Tooltip("Time Scale이 0이어도 연출을 진행")]
    [SerializeField]
    private bool useUnscaledTime = false;

    private Coroutine sequenceCoroutine;

    public bool IsPlaying => sequenceCoroutine != null;
    public bool IsCompleted { get; private set; } = true;
    public event Action Completed;

    private void Awake()
    {
        PrepareAudioSource();

        if (hideRevealTargetsOnAwake)
        {
            SetRevealTargetsActive(false);
        }

        if (hideFinishObjectsOnAwake)
        {
            SetFinishObjectsActive(false);
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void PrepareAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null &&
            createAudioSourceAutomatically)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }

    public void Play()
    {
        StopSequence();
        IsCompleted = false;

        SetRevealTargetsActive(false);

        if (hideFinishObjectsOnPlay)
        {
            SetFinishObjectsActive(false);
        }

        sequenceCoroutine = StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        if (revealTargets == null ||
            revealTargets.Count == 0)
        {
            Debug.LogWarning(
                "[TimedObjectRevealSequence] " +
                "Reveal Targets가 지정되지 않았습니다.",
                this
            );

            SetFinishObjectsActive(true);
            sequenceCoroutine = null;
            MarkCompleted();
            yield break;
        }

        List<RevealTarget> sortedTargets =
            new List<RevealTarget>(revealTargets);

        sortedTargets.Sort(CompareRevealTime);

        float elapsedTime = 0f;
        int nextTargetIndex = 0;

        while (nextTargetIndex < sortedTargets.Count)
        {
            RevealTarget revealTarget =
                sortedTargets[nextTargetIndex];

            if (revealTarget == null ||
                revealTarget.targetObject == null)
            {
                nextTargetIndex++;
                continue;
            }

            float targetRevealTime =
                Mathf.Max(0f, revealTarget.revealTime);

            while (elapsedTime < targetRevealTime)
            {
                elapsedTime += GetDeltaTime();
                yield return null;
            }

            // 오브젝트 등장
            revealTarget.targetObject.SetActive(true);

            // 등장 효과음 재생
            PlayRevealSound(revealTarget);

            // 등장 카메라 흔들림
            PlayCameraShake(revealTarget);

            nextTargetIndex++;
        }

        if (finalHoldTime > 0f)
        {
            yield return WaitRoutine(finalHoldTime);
        }

        if (hideRevealTargetsAfterFinish)
        {
            SetRevealTargetsActive(false);
        }

        SetFinishObjectsActive(true);

        sequenceCoroutine = null;
        MarkCompleted();

        if (disableSequenceObjectAfterFinish)
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayRevealSound(RevealTarget revealTarget)
    {
        if (revealTarget.revealSound == null)
            return;

        if (audioSource == null)
        {
            PrepareAudioSource();
        }

        if (audioSource == null)
        {
            Debug.LogWarning(
                "[TimedObjectRevealSequence] " +
                "효과음을 재생할 Audio Source가 없습니다.",
                this
            );

            return;
        }

        audioSource.pitch =
            Mathf.Clamp(revealTarget.soundPitch, 0.1f, 3f);

        audioSource.PlayOneShot(
            revealTarget.revealSound,
            Mathf.Clamp01(revealTarget.soundVolume)
        );
    }

    private void PlayCameraShake(RevealTarget revealTarget)
    {
        if (!revealTarget.useCameraShake)
            return;

        if (cameraShakeController == null)
            return;

        cameraShakeController.Shake(
            revealTarget.shakeDuration,
            revealTarget.shakeStrength,
            revealTarget.shakeFrequency
        );
    }

    private int CompareRevealTime(
        RevealTarget first,
        RevealTarget second)
    {
        float firstTime =
            first != null ? first.revealTime : 0f;

        float secondTime =
            second != null ? second.revealTime : 0f;

        return firstTime.CompareTo(secondTime);
    }

    private IEnumerator WaitRoutine(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }

    private void SetRevealTargetsActive(bool isActive)
    {
        if (revealTargets == null)
            return;

        for (int i = 0; i < revealTargets.Count; i++)
        {
            RevealTarget revealTarget = revealTargets[i];

            if (revealTarget == null ||
                revealTarget.targetObject == null)
            {
                continue;
            }

            revealTarget.targetObject.SetActive(isActive);
        }
    }

    private void SetFinishObjectsActive(bool isActive)
    {
        if (objectsToActivateAfterFinish == null)
            return;

        for (int i = 0;
             i < objectsToActivateAfterFinish.Count;
             i++)
        {
            GameObject target =
                objectsToActivateAfterFinish[i];

            if (target != null)
            {
                target.SetActive(isActive);
            }
        }
    }

    public void StopSequence()
    {
        if (sequenceCoroutine == null)
            return;

        StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = null;
    }

    public void ResetSequence()
    {
        StopSequence();
        IsCompleted = true;

        SetRevealTargetsActive(false);
        SetFinishObjectsActive(false);
    }

    public void ActivateFinishObjectsImmediately()
    {
        SetFinishObjectsActive(true);
        MarkCompleted();
    }

    public void HideFinishObjectsImmediately()
    {
        SetFinishObjectsActive(false);
    }

    private void MarkCompleted()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        Completed?.Invoke();
    }
}