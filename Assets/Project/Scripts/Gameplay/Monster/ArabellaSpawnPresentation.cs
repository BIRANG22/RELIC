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

    [Header("Intro Camera")]
    [SerializeField] private bool useIntroCamera = true;
    [SerializeField] private Vector2 cameraTargetOffset = new Vector2(0f, 2f);
    [SerializeField, Min(0.01f)] private float cameraOrthographicSize = 6f;
    [SerializeField, Min(0f)] private float cameraBlendInDuration = 0.35f;
    [SerializeField, Min(0f)] private float cameraBlendOutDuration = 0.35f;

    private bool hasPlayed;
    private bool ownsIntroCamera;
    private Camera introCamera;
    private Coroutine cameraBlendRoutine;
    private Vector3 cameraRestorePosition;
    private Quaternion cameraRestoreRotation;
    private float cameraRestoreOrthographicSize;

    public bool HasPlayed => hasPlayed;

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
        BeginIntroCamera();

        dialogueRoot.SetActive(true);
        dialogueText.text = GameLocalization.Get(FirstDialogueKey, FirstDialogueFallback);
        PlayAnimatorState(Appear01StateName);
        yield return WaitForDuration(ResolvePresentationDuration(
            appear01Clip,
            firstDialogueDuration));

        dialogueText.text = GameLocalization.Get(SecondDialogueKey, SecondDialogueFallback);
        PlayAnimatorState(Appear02StateName);
        yield return WaitForDuration(ResolvePresentationDuration(
            appear02Clip,
            secondDialogueDuration));

        yield return RestoreIntroCameraRoutine();

        dialogueRoot.SetActive(false);
        animator.speed = 1f;
        unitAnimator.PlayIdle();
    }

    public void CancelPresentation()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        if (animator != null)
            animator.speed = 1f;

        if (cameraBlendRoutine != null)
        {
            StopCoroutine(cameraBlendRoutine);
            cameraBlendRoutine = null;
        }

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
        duration = Mathf.Max(0f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void BeginIntroCamera()
    {
        if (!useIntroCamera || BattleCameraController.Instance == null ||
            !BattleCameraController.Instance.TryBeginCinematicCamera())
        {
            return;
        }

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

        Vector3 targetPosition = new Vector3(
            transform.position.x + cameraTargetOffset.x,
            transform.position.y + cameraTargetOffset.y,
            cameraRestorePosition.z);

        cameraBlendRoutine = StartCoroutine(BlendCameraRoutine(
            cameraRestorePosition,
            targetPosition,
            cameraRestoreOrthographicSize,
            cameraOrthographicSize,
            cameraBlendInDuration));
    }

    private IEnumerator RestoreIntroCameraRoutine()
    {
        if (!ownsIntroCamera || introCamera == null)
            yield break;

        if (cameraBlendRoutine != null)
        {
            StopCoroutine(cameraBlendRoutine);
            cameraBlendRoutine = null;
        }

        yield return BlendCameraRoutine(
            introCamera.transform.position,
            cameraRestorePosition,
            introCamera.orthographicSize,
            cameraRestoreOrthographicSize,
            cameraBlendOutDuration);

        BattleCameraController.Instance?.EndCinematicCameraKeepingCurrentPose();
        ownsIntroCamera = false;
        introCamera = null;
    }

    private IEnumerator BlendCameraRoutine(
        Vector3 startPosition,
        Vector3 targetPosition,
        float startSize,
        float targetSize,
        float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);

        do
        {
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            ApplyCameraPose(
                Vector3.Lerp(startPosition, targetPosition, t),
                Mathf.Lerp(startSize, targetSize, t));

            if (t >= 1f)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        while (true);

        cameraBlendRoutine = null;
    }

    private void ApplyCameraPose(Vector3 position, float orthographicSize)
    {
        if (BattleCameraController.Instance != null)
        {
            BattleCameraController.Instance.ApplyCinematicCameraPose(
                position,
                cameraRestoreRotation,
                orthographicSize,
                true,
                false,
                true);
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

        if (cameraBlendRoutine != null)
        {
            StopCoroutine(cameraBlendRoutine);
            cameraBlendRoutine = null;
        }

        if (BattleCameraController.Instance != null)
            BattleCameraController.Instance.RestoreCinematicCameraImmediate();
        else if (introCamera != null)
        {
            introCamera.transform.SetPositionAndRotation(
                cameraRestorePosition,
                cameraRestoreRotation);
            if (introCamera.orthographic)
                introCamera.orthographicSize = cameraRestoreOrthographicSize;
        }

        ownsIntroCamera = false;
        introCamera = null;
    }
}
