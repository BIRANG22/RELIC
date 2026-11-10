using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArabellaSpawnPresentation : MonoBehaviour
{
    public const string FirstDialogueKey = "battle.boss.arabella.appear.01";
    public const string SecondDialogueKey = "battle.boss.arabella.appear.02";
    public const string FirstDialogueFallback = "들리시나요? 이토록 아름다운 기도가...";
    public const string SecondDialogueFallback = "고통은 잠시뿐입니다. 구원은 영원하니까요";

    private const string Appear01StateName = "Appear01";
    private const string Appear02StateName = "Appear02";

    public enum SequenceAction
    {
        CameraMove,
        CameraShake,
        Wait,
        ShowFirstDialogue,
        ShowSecondDialogue,
        HideDialogue,
        PlayAppear01,
        PlayAppear02,
        RestoreCamera,
        PlayIdle
    }

    [Serializable]
    public class PresentationStep
    {
        public string label;
        public SequenceAction action;
        [Tooltip("CameraMove: 이동 시간 / CameraShake: 흔들림 시간 / Wait: 대기 시간 / Animation: 0이면 클립 길이, 양수이면 지정 시간")]
        [Min(0f)] public float duration = 0.35f;

        [Header("CameraMove")]
        public Vector3 targetOffset = new Vector3(0f, 2f, -20f);

        [Header("CameraShake")]
        [Min(0f)] public float shakeStrength = 0.18f;
        [Min(0f)] public float shakeFrequency = 28f;
        public bool fadeOutShake = true;
    }

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private BattleUnitAnimator unitAnimator;
    [SerializeField] private AnimationClip appear01Clip;
    [SerializeField] private AnimationClip appear02Clip;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private RectTransform dialogueRect;
    [SerializeField] private Vector3 dialogueLocalPosition = new Vector3(0f, 16f, 0f);
    [SerializeField] private Vector2 dialogueSize = new Vector2(650f, 104f);
    [SerializeField, Min(0f)] private float firstDialogueDuration;
    [SerializeField, Min(0f)] private float secondDialogueDuration;

    [Header("Animation Timing")]
    [SerializeField, Min(0.01f)] private float animationSpeed = 1f;

    [Header("Sequential Presentation (비어 있으면 기본 등장 연출)")]
    [Tooltip("위에서 아래로 순차 실행합니다. CameraMove의 X/Y는 아라벨라 위치에서의 오프셋이며 Z는 카메라의 월드 Z 좌표입니다. 애니메이션 단계는 실행 후 duration 동안 기다립니다.")]
    [SerializeField] private PresentationStep[] sequenceSteps = new PresentationStep[0];

    private bool hasPlayed;
    private bool ownsIntroCamera;
    private Camera introCamera;
    private Coroutine cameraBlendRoutine;
    private Vector3 cameraRestorePosition;
    private Quaternion cameraRestoreRotation;
    private float cameraRestoreOrthographicSize;
    private Vector3 cameraBasePosition;
    private float cameraBaseSize;

    public bool HasPlayed => hasPlayed;
    public static bool ShouldRestoreCameraAfterSequence => false;

    public bool CanPlay =>
        animator != null &&
        animator.runtimeAnimatorController != null &&
        unitAnimator != null &&
        appear01Clip != null &&
        appear02Clip != null &&
        dialogueRoot != null &&
        dialogueText != null &&
        dialogueRect != null;

    private void Awake()
    {
        ApplyDialogueLayout();
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Start()
    {
        PrepareFirstFrameIfIntroPlaying();
    }

    private void OnDisable()
    {
        CancelPresentation();
    }

    private void PrepareFirstFrameIfIntroPlaying()
    {
        if (!BattleSceneController.IsBattleRoomIntroPlaying || !CanPlay)
            return;

        PlayAnimatorState(Appear01StateName);
        animator.speed = 0f;
    }

    public IEnumerator Play()
    {
        if (hasPlayed)
            yield break;

        hasPlayed = true;
        if (!CanPlay)
        {
            Debug.LogWarning("[ArabellaSpawnPresentation] 등장 연출 참조가 완전하지 않아 연출을 건너뜁니다.", this);
            yield break;
        }

        ApplyDialogueLayout();
        animator.speed = Mathf.Max(0.01f, animationSpeed);
        if (sequenceSteps != null && Array.Exists(sequenceSteps, step => step != null && (step.action == SequenceAction.CameraMove || step.action == SequenceAction.CameraShake)))
            BeginIntroCamera();

        if (sequenceSteps != null && sequenceSteps.Length > 0)
        {
            foreach (PresentationStep step in sequenceSteps)
            {
                if (step == null)
                    continue;
                yield return ExecuteStep(step);
            }
        }
        else
        {
            ShowDialogue(FirstDialogueKey, FirstDialogueFallback);
            PlayAnimatorState(Appear01StateName);
            yield return WaitForDuration(ResolvePresentationDuration(appear01Clip, firstDialogueDuration));

            ShowDialogue(SecondDialogueKey, SecondDialogueFallback);
            PlayAnimatorState(Appear02StateName);
            yield return WaitForDuration(ResolvePresentationDuration(appear02Clip, secondDialogueDuration));
        }

        ReleaseIntroCameraKeepingCurrentPose();

        dialogueRoot.SetActive(false);
        animator.speed = 1f;
        unitAnimator.PlayIdle();
    }

    private IEnumerator ExecuteStep(PresentationStep step)
    {
        switch (step.action)
        {
            case SequenceAction.CameraMove:
                if (ownsIntroCamera)
                {
                    StopCameraBlend();
                    Vector3 target = new Vector3(
                        transform.position.x + step.targetOffset.x,
                        transform.position.y + step.targetOffset.y,
                        step.targetOffset.z);
                    yield return BlendCameraRoutine(cameraBasePosition, target,
                        cameraBaseSize, cameraBaseSize, step.duration);
                }
                break;
            case SequenceAction.CameraShake:
                if (ownsIntroCamera)
                    yield return ShakeCameraRoutine(step.duration, step.shakeStrength,
                        step.shakeFrequency, step.fadeOutShake);
                break;
            case SequenceAction.Wait:
                yield return WaitForDuration(step.duration);
                break;
            case SequenceAction.ShowFirstDialogue:
                ShowDialogue(FirstDialogueKey, FirstDialogueFallback);
                break;
            case SequenceAction.ShowSecondDialogue:
                ShowDialogue(SecondDialogueKey, SecondDialogueFallback);
                break;
            case SequenceAction.HideDialogue:
                dialogueRoot.SetActive(false);
                break;
            case SequenceAction.PlayAppear01:
                PlayAnimatorState(Appear01StateName);
                yield return WaitForDuration(ResolvePresentationDuration(appear01Clip, step.duration));
                break;
            case SequenceAction.PlayAppear02:
                PlayAnimatorState(Appear02StateName);
                yield return WaitForDuration(ResolvePresentationDuration(appear02Clip, step.duration));
                break;
            case SequenceAction.RestoreCamera:
                yield return RestoreIntroCameraRoutine(step.duration);
                break;
            case SequenceAction.PlayIdle:
                unitAnimator.PlayIdle();
                break;
        }
    }

    private void ShowDialogue(string key, string fallback)
    {
        dialogueRoot.SetActive(true);
        dialogueText.text = GameLocalization.Get(key, fallback);
    }

    public void CancelPresentation()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
        if (animator != null)
            animator.speed = 1f;

        StopCameraBlend();
        RestoreIntroCameraImmediate();
    }

    private void ApplyDialogueLayout()
    {
        if (dialogueRect == null)
            return;
        dialogueRect.localPosition = dialogueLocalPosition;
        dialogueRect.sizeDelta = dialogueSize;
    }

    private void PlayAnimatorState(string stateName)
    {
        animator.Play(stateName, 0, 0f);
        animator.Update(0f);
    }

    private float ResolvePresentationDuration(AnimationClip clip, float overrideDuration)
    {
        if (overrideDuration > 0f)
            return overrideDuration;
        float clipLength = clip != null ? clip.length : 0f;
        return clipLength / Mathf.Max(0.01f, animationSpeed);
    }

    private static IEnumerator WaitForDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0f, duration))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void BeginIntroCamera()
    {
        if (BattleCameraController.Instance == null ||
            !BattleCameraController.Instance.TryBeginCinematicCamera())
            return;

        introCamera = Camera.main;
        if (introCamera == null)
        {
            BattleCameraController.Instance.RestoreCinematicCameraImmediate();
            return;
        }

        ownsIntroCamera = true;
        cameraRestorePosition = introCamera.transform.position;
        cameraRestoreRotation = introCamera.transform.rotation;
        cameraRestoreOrthographicSize = introCamera.orthographicSize;
        cameraBasePosition = cameraRestorePosition;
        cameraBaseSize = cameraRestoreOrthographicSize;
    }

    private IEnumerator RestoreIntroCameraRoutine(float duration = 0.35f)
    {
        if (!ownsIntroCamera || introCamera == null)
            yield break;

        StopCameraBlend();
        yield return BlendCameraRoutine(cameraBasePosition, cameraRestorePosition,
            cameraBaseSize, cameraRestoreOrthographicSize, duration);

        BattleCameraController.Instance?.EndCinematicCameraKeepingCurrentPose();
        ownsIntroCamera = false;
        introCamera = null;
    }

    private void ReleaseIntroCameraKeepingCurrentPose()
    {
        if (!ownsIntroCamera)
            return;

        StopCameraBlend();
        BattleCameraController.Instance?.EndCinematicCameraKeepingCurrentPose();
        ownsIntroCamera = false;
        introCamera = null;
    }

    private void StopCameraBlend()
    {
        if (cameraBlendRoutine == null)
            return;
        StopCoroutine(cameraBlendRoutine);
        cameraBlendRoutine = null;
        if (ownsIntroCamera)
            ApplyCameraPose(cameraBasePosition, cameraBaseSize);
    }

    private IEnumerator BlendCameraRoutine(Vector3 startPosition, Vector3 targetPosition,
        float startSize, float targetSize, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);
        do
        {
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            ApplyCameraPose(Vector3.Lerp(startPosition, targetPosition, t),
                Mathf.Lerp(startSize, targetSize, t));
            if (t >= 1f)
                break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        while (true);
        cameraBlendRoutine = null;
    }

    private IEnumerator ShakeCameraRoutine(float duration, float strength, float frequency, bool fadeOut)
    {
        Vector3 fixedBase = cameraBasePosition;
        float size = cameraBaseSize;
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);
        while (elapsed < duration)
        {
            float envelope = fadeOut ? 1f - Mathf.Clamp01(elapsed / duration) : 1f;
            float phase = elapsed * Mathf.Max(0f, frequency);
            // 두 축의 서로 다른 주파수로 반복 패턴을 줄이고, 마지막에는 원래 위치로 복구.
            Vector3 offset = new Vector3(Mathf.Sin(phase * 2.17f),
                Mathf.Sin(phase * 2.91f + 1.3f), 0f) * (strength * envelope);
            ApplyCameraPose(fixedBase + offset, size, false);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        ApplyCameraPose(fixedBase, size);
    }

    private void ApplyCameraPose(Vector3 position, float orthographicSize, bool updateBase = true)
    {
        if (!ownsIntroCamera)
            return;

        if (updateBase)
        {
            cameraBasePosition = position;
            cameraBaseSize = orthographicSize;
        }

        if (BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.ApplyCinematicCameraPose(
                position, cameraRestoreRotation, orthographicSize,
                true, false, true);
            return;
        }

        if (introCamera == null)
            return;
        introCamera.transform.position = position;
        if (introCamera.orthographic)
            introCamera.orthographicSize = Mathf.Max(0.01f, orthographicSize);
    }

    private void RestoreIntroCameraImmediate()
    {
        if (!ownsIntroCamera)
            return;

        StopCameraBlend();
        if (BattleCameraController.Instance != null)
            BattleCameraController.Instance.RestoreCinematicCameraImmediate();
        else if (introCamera != null)
        {
            introCamera.transform.SetPositionAndRotation(cameraRestorePosition, cameraRestoreRotation);
            if (introCamera.orthographic)
                introCamera.orthographicSize = cameraRestoreOrthographicSize;
        }

        ownsIntroCamera = false;
        introCamera = null;
    }
}
