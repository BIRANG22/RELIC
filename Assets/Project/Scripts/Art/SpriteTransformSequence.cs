using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스프라이트 오브젝트의 위치, 회전, 크기, 색상과 반전 상태를 단계별로 연출합니다.
/// 각 단계의 Delay 이후 해당 단계의 목표값까지 Duration 동안 이동합니다.
/// </summary>
public sealed class SpriteTransformSequence : MonoBehaviour
{
    public enum CameraPositionMode { WorldPosition, RelativeToSprite }

    [Serializable]
    public sealed class CameraMotion
    {
        public string name = "Camera Motion";
        public bool enabled = true;
        public CameraPositionMode positionMode = CameraPositionMode.RelativeToSprite;
        [Tooltip("WorldPosition: 카메라 월드 좌표 / RelativeToSprite: 스프라이트 위치 기준 오프셋")]
        public Vector3 position = new Vector3(0f, 0f, -10f);
        public bool animatePosition = true;
        public bool animateRotation = false;
        public Vector3 rotation;
        public bool animateOrthographicSize = true;
        [Min(0.01f)] public float orthographicSize = 5f;
        [Min(0f)] public float delay;
        [Min(0f)] public float duration = 0.5f;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Header("Shake (이 Motion이 진행되는 동안 적용)")]
        public bool shake;
        [Min(0f)] public float shakeStrength = 0.15f;
        [Min(0f)] public float shakeFrequency = 25f;
    }

    public enum BossTitleEntrance { None, SlideUp, ScaleUp }

    [Serializable]
    public sealed class BossTitleSettings
    {
        public bool enabled;
        public string bossName = "BOSS NAME";
        public string subtitle = "";
        [Min(0f)] public float delay = 0f;
        [Min(0f)] public float fadeInDuration = 0.4f;
        [Min(0f)] public float holdDuration = 1.2f;
        [Min(0f)] public float fadeOutDuration = 0.4f;
        public BossTitleEntrance entrance = BossTitleEntrance.SlideUp;
        [Min(0f)] public float entranceDistance = 45f;
        [Min(0.01f)] public float initialScale = 0.85f;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public Color nameColor = new Color(0.95f, 0.9f, 0.82f, 1f);
        public Color subtitleColor = new Color(0.7f, 0.64f, 0.61f, 1f);
        [Min(1f)] public float nameFontSize = 65f;
        [Min(1f)] public float subtitleFontSize = 22f;
        [Tooltip("체크하면 보스 이름 연출이 끝날 때까지 다음 Step으로 이동하지 않습니다.")]
        public bool waitForCompletion = false;
    }

    [Serializable]
    public sealed class TransformStep
    {
        public string name = "Step";
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public bool flipX;
        public bool flipY;
        [Tooltip("체크 시 Delay 이후 해당 단계 시작 즉시 Flip X/Y를 적용합니다. 체크 해제 시 단계 완료 시 적용합니다.")]
        public bool flipAtStart = false;
        public Color color = Color.white;
        [Tooltip("이 단계가 시작될 때 재생할 Animator용 AnimationClip (비워두면 클립 재생 안 함)")]
        public AnimationClip animationClip;
        [Min(0f)] public float delay = 0f;
        [Min(0f)] public float duration = 0.5f;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Header("카메라 연출 (순서대로 재생)")]
        public bool enableCameraMotion;
        public List<CameraMotion> cameraMotions = new List<CameraMotion>();
        [Header("보스 이름 연출")]
        public BossTitleSettings bossTitle = new BossTitleSettings();
    }

    [Header("대상")]
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private bool useLocalTransform = true;

    [Header("씬 카메라 (프리팹 참조 불필요)")]
    [Tooltip("Camera.main으로 씬의 MainCamera 태그 카메라를 자동으로 탐색합니다.")]
    [SerializeField] private bool enableSceneCamera = true;
    [Tooltip("연출을 중단하거나 완전히 끝냈을 때 최초 카메라 위치/회전/줌으로 복구합니다.")]
    [SerializeField] private bool restoreCameraOnFinish = true;
    [Min(0f)][SerializeField] private float cameraRestoreDuration = 0.4f;
    [SerializeField] private AnimationCurve cameraRestoreEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("재생")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool applyFirstStepImmediately = false;
    [Tooltip("마지막 단계, 보스 이름 연출 및 카메라 복귀가 모두 끝나면 이 오브젝트를 비활성화합니다. Loop가 켜져 있으면 반복이 끝나기 전까지 비활성화하지 않습니다.")]
    [SerializeField] private bool deactivateOnFinish = true;
    [Tooltip("모든 연출과 카메라 복귀가 끝난 후 오브젝트를 비활성화하기까지 기다리는 시간(초)입니다.")]
    [Min(0f)][SerializeField] private float deactivateDelay = 0f;

    [Header("단계별 목표값 (Size로 단계 추가)")]
    [SerializeField] private List<TransformStep> steps = new List<TransformStep>();

    private Coroutine runningCoroutine;
    private PlayableGraph clipGraph;
    private Camera sceneCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private float originalCameraSize;
    private bool cameraSnapshotTaken;
    private Canvas titleCanvas;
    private RectTransform titleRoot;
    private CanvasGroup titleGroup;
    private TextMeshProUGUI titleNameText;
    private TextMeshProUGUI titleSubtitleText;
    private Coroutine titleCoroutine;
    private int titleVersion;
    private bool titlePlaying;


    private Transform Target => target != null ? target : transform;

    private SpriteRenderer SpriteTarget => spriteRenderer != null
        ? spriteRenderer
        : Target.GetComponent<SpriteRenderer>();

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    [ContextMenu("Play Sequence")]
    public void Play()
    {
        Stop();
        if (steps == null || steps.Count == 0)
            return;

        runningCoroutine = StartCoroutine(PlayRoutine());
    }

    [ContextMenu("Stop Sequence")]
    public void Stop()
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        StopStepClip();
        CancelBossTitle();
        if (restoreCameraOnFinish)
            RestoreCameraImmediately();
        else
            cameraSnapshotTaken = false;
    }

    [ContextMenu("Apply First Step")]
    public void ApplyFirstStep()
    {
        if (steps != null && steps.Count > 0 && steps[0] != null)
            ApplyStep(steps[0]);
    }

    private IEnumerator PlayRoutine()
    {
        do
        {
            int startIndex = 0;
            if (applyFirstStepImmediately && steps[0] != null)
            {
                ApplyStep(steps[0]);
                PlayStepClip(steps[0]);
                StartBossTitle(steps[0]);
                if (HasCameraMotions(steps[0]))
                    yield return PlayCameraMotions(steps[0]);
                if (WaitForBossTitle(steps[0]))
                    while (titlePlaying) yield return null;
                startIndex = 1;
            }

            for (int i = startIndex; i < steps.Count; i++)
            {
                TransformStep step = steps[i];
                if (step == null)
                    continue;

                float remainingDelay = Mathf.Max(0f, step.delay);
                while (remainingDelay > 0f)
                {
                    yield return null;
                    remainingDelay -= DeltaTime;
                }

                PlayStepClip(step);
                StartBossTitle(step);

                Transform current = Target;
                Vector3 fromPosition = GetPosition(current);
                Quaternion fromRotation = GetRotation(current);
                Vector3 fromScale = current.localScale;
                Quaternion toRotation = Quaternion.Euler(step.rotation);
                SpriteRenderer sprite = SpriteTarget;
                Color fromColor = sprite != null ? sprite.color : Color.white;
                float duration = Mathf.Max(0f, step.duration);

                IEnumerator cameraRoutine = HasCameraMotions(step) ? PlayCameraMotions(step) : null;
                // 스프라이트와 카메라를 같은 프레임에 시작합니다.
                if (duration <= 0f)
                {
                    ApplyStep(step);
                    if (cameraRoutine != null)
                        yield return cameraRoutine;
                    if (WaitForBossTitle(step))
                        while (titlePlaying) yield return null;
                    continue;
                }

                if (step.flipAtStart)
                    ApplyFlip(sprite, step);

                float elapsed = 0f;
                bool cameraRunning = cameraRoutine != null;
                // 별도 코루틴 대신 같은 프레임에서 두 IEnumerator를 진행시켜 동기화합니다.
                while (elapsed < duration || cameraRunning)
                {
                    if (cameraRunning && !cameraRoutine.MoveNext())
                        cameraRunning = false;
                    if (elapsed >= duration)
                    {
                        yield return null;
                        continue;
                    }
                    elapsed += DeltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float eased = step.easing != null && step.easing.length > 0
                        ? step.easing.Evaluate(progress)
                        : progress;

                    SetPosition(current, Vector3.LerpUnclamped(fromPosition, step.position, eased));
                    SetRotation(current, Quaternion.SlerpUnclamped(fromRotation, toRotation, eased));
                    current.localScale = Vector3.LerpUnclamped(fromScale, step.scale, eased);
                    if (sprite != null)
                        sprite.color = Color.LerpUnclamped(fromColor, step.color, eased);
                    yield return null;
                }

                ApplyStep(step);
                if (WaitForBossTitle(step))
                    while (titlePlaying) yield return null;
            }
        }
        while (loop && isActiveAndEnabled);

        // 마지막 Step의 이름 연출은 완료 후 UI가 정리되도록 기다립니다.
        while (titlePlaying) yield return null;
        if (restoreCameraOnFinish && cameraSnapshotTaken)
            yield return RestoreCameraSmoothly();
        else
            cameraSnapshotTaken = false;

        // 마지막 스프라이트 상태를 유지한 채 비활성화 전 대기합니다.
        if (deactivateOnFinish && !loop)
        {
            float remainingDeactivateDelay = Mathf.Max(0f, deactivateDelay);
            while (remainingDeactivateDelay > 0f)
            {
                yield return null;
                remainingDeactivateDelay -= DeltaTime;
            }
        }

        runningCoroutine = null;
        StopStepClip();

        // 연출과 대기 시간이 모두 끝난 후 오브젝트를 비활성화합니다.
        if (deactivateOnFinish && !loop)
            gameObject.SetActive(false);
    }

    private static bool WaitForBossTitle(TransformStep step)
    {
        return step != null && step.bossTitle != null && step.bossTitle.enabled && step.bossTitle.waitForCompletion;
    }

    private void StartBossTitle(TransformStep step)
    {
        BossTitleSettings settings = step != null ? step.bossTitle : null;
        if (settings == null || !settings.enabled) return;
        CancelBossTitle();
        EnsureTitleUI();
        titlePlaying = true;
        titleCoroutine = StartCoroutine(PlayBossTitle(settings, titleVersion));
    }

    private void CancelBossTitle()
    {
        titleVersion++;
        if (titleCoroutine != null)
        {
            StopCoroutine(titleCoroutine);
            titleCoroutine = null;
        }
        titlePlaying = false;
        if (titleCanvas != null)
            Destroy(titleCanvas.gameObject);
        titleCanvas = null;
        titleRoot = null;
        titleGroup = null;
        titleNameText = null;
        titleSubtitleText = null;
    }

    private void EnsureTitleUI()
    {
        GameObject canvasObject = new GameObject("Sequence Boss Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        titleCanvas = canvasObject.GetComponent<Canvas>();
        titleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        titleCanvas.sortingOrder = 30000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.GetComponent<GraphicRaycaster>().enabled = false;

        GameObject rootObject = new GameObject("Boss Title", typeof(RectTransform), typeof(CanvasGroup));
        rootObject.transform.SetParent(canvasObject.transform, false);
        titleRoot = rootObject.GetComponent<RectTransform>();
        titleRoot.anchorMin = new Vector2(0.5f, 0.5f);
        titleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        titleRoot.pivot = new Vector2(0.5f, 0.5f);
        titleRoot.sizeDelta = new Vector2(1500f, 240f);
        titleGroup = rootObject.GetComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        titleGroup.blocksRaycasts = false;
        titleGroup.interactable = false;

        titleNameText = CreateTitleText("Boss Name", titleRoot, new Vector2(0f, 30f), new Vector2(1450f, 125f));
        titleSubtitleText = CreateTitleText("Boss Subtitle", titleRoot, new Vector2(0f, -65f), new Vector2(1450f, 55f));
    }

    private static TextMeshProUGUI CreateTitleText(string label, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(label, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI result = obj.GetComponent<TextMeshProUGUI>();
        result.alignment = TextAlignmentOptions.Center;
        result.textWrappingMode = TextWrappingModes.NoWrap;
        result.overflowMode = TextOverflowModes.Ellipsis;
        result.raycastTarget = false;
        return result;
    }

    private IEnumerator PlayBossTitle(BossTitleSettings settings, int version)
    {
        titleNameText.text = settings.bossName ?? "";
        titleNameText.fontSize = settings.nameFontSize;
        titleNameText.color = settings.nameColor;
        titleSubtitleText.text = settings.subtitle ?? "";
        titleSubtitleText.fontSize = settings.subtitleFontSize;
        titleSubtitleText.color = settings.subtitleColor;
        titleSubtitleText.gameObject.SetActive(!string.IsNullOrEmpty(settings.subtitle));
        titleGroup.alpha = 0f;
        float delay = Mathf.Max(0f, settings.delay);
        while (delay > 0f) { yield return null; delay -= DeltaTime; }

        float fadeIn = Mathf.Max(0f, settings.fadeInDuration);
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += DeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeIn);
            float eased = settings.easing != null && settings.easing.length > 0 ? settings.easing.Evaluate(t) : t;
            titleGroup.alpha = Mathf.Clamp01(t);
            ApplyTitleEntrance(settings, eased);
            yield return null;
        }
        titleGroup.alpha = 1f;
        ApplyTitleEntrance(settings, 1f);
        float hold = Mathf.Max(0f, settings.holdDuration);
        while (hold > 0f) { yield return null; hold -= DeltaTime; }
        float fadeOut = Mathf.Max(0f, settings.fadeOutDuration);
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += DeltaTime;
            titleGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
            yield return null;
        }
        if (version == titleVersion)
        {
            titlePlaying = false;
            titleCoroutine = null;
            if (titleCanvas != null) Destroy(titleCanvas.gameObject);
            titleCanvas = null;
        }
    }

    private void ApplyTitleEntrance(BossTitleSettings settings, float t)
    {
        titleRoot.anchoredPosition = settings.entrance == BossTitleEntrance.SlideUp
            ? new Vector2(0f, Mathf.LerpUnclamped(-settings.entranceDistance, 0f, t)) : Vector2.zero;
        float scale = settings.entrance == BossTitleEntrance.ScaleUp
            ? Mathf.LerpUnclamped(settings.initialScale, 1f, t) : 1f;
        titleRoot.localScale = Vector3.one * scale;
    }

    private static bool HasCameraMotions(TransformStep step)
    {
        if (step == null || !step.enableCameraMotion || step.cameraMotions == null)
            return false;
        foreach (CameraMotion motion in step.cameraMotions)
            if (motion != null && motion.enabled) return true;
        return false;
    }

    private bool AcquireSceneCamera()
    {
        if (!enableSceneCamera) return false;
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            Debug.LogWarning("[SpriteTransformSequence] MainCamera 태그가 설정된 씬 카메라를 찾지 못했습니다.", this);
            return false;
        }
        if (!cameraSnapshotTaken)
        {
            originalCameraPosition = sceneCamera.transform.position;
            originalCameraRotation = sceneCamera.transform.rotation;
            originalCameraSize = sceneCamera.orthographicSize;
            cameraSnapshotTaken = true;
        }
        return true;
    }

    private IEnumerator PlayCameraMotions(TransformStep step)
    {
        if (!AcquireSceneCamera()) yield break;
        foreach (CameraMotion motion in step.cameraMotions)
        {
            if (motion == null || !motion.enabled) continue;
            float delay = Mathf.Max(0f, motion.delay);
            while (delay > 0f)
            {
                yield return null;
                delay -= DeltaTime;
            }

            Transform cam = sceneCamera.transform;
            Vector3 startPosition = cam.position;
            Quaternion startRotation = cam.rotation;
            float startSize = sceneCamera.orthographicSize;
            Vector3 endPosition = motion.positionMode == CameraPositionMode.RelativeToSprite
                ? Target.position + motion.position : motion.position;
            Quaternion endRotation = Quaternion.Euler(motion.rotation);
            float duration = Mathf.Max(0f, motion.duration);
            if (duration <= 0f)
            {
                if (motion.animatePosition) cam.position = endPosition;
                if (motion.animateRotation) cam.rotation = endRotation;
                if (motion.animateOrthographicSize && sceneCamera.orthographic)
                    sceneCamera.orthographicSize = Mathf.Max(0.01f, motion.orthographicSize);
                continue;
            }

            float elapsed = 0f;
            float seed = UnityEngine.Random.Range(0f, 10000f);
            while (elapsed < duration)
            {
                elapsed += DeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = motion.easing != null && motion.easing.length > 0
                    ? motion.easing.Evaluate(normalized) : normalized;
                Vector3 basePosition = motion.animatePosition
                    ? Vector3.LerpUnclamped(startPosition, endPosition, eased) : startPosition;
                if (motion.shake && motion.shakeStrength > 0f)
                {
                    float attenuation = 1f - normalized;
                    float sample = elapsed * motion.shakeFrequency;
                    Vector3 offset = new Vector3(
                        Mathf.PerlinNoise(seed, sample) * 2f - 1f,
                        Mathf.PerlinNoise(seed + 47f, sample) * 2f - 1f,
                        0f) * (motion.shakeStrength * attenuation);
                    cam.position = basePosition + offset;
                }
                else cam.position = basePosition;
                if (motion.animateRotation)
                    cam.rotation = Quaternion.SlerpUnclamped(startRotation, endRotation, eased);
                if (motion.animateOrthographicSize && sceneCamera.orthographic)
                    sceneCamera.orthographicSize = Mathf.Max(0.01f, Mathf.LerpUnclamped(startSize, motion.orthographicSize, eased));
                yield return null;
            }
            cam.position = motion.animatePosition ? endPosition : startPosition;
            if (motion.animateRotation) cam.rotation = endRotation;
            if (motion.animateOrthographicSize && sceneCamera.orthographic)
                sceneCamera.orthographicSize = Mathf.Max(0.01f, motion.orthographicSize);
        }
    }

    private IEnumerator RestoreCameraSmoothly()
    {
        if (sceneCamera == null) { cameraSnapshotTaken = false; yield break; }
        float duration = Mathf.Max(0f, cameraRestoreDuration);
        if (duration <= 0f) { RestoreCameraImmediately(); yield break; }
        Transform cam = sceneCamera.transform;
        Vector3 startPosition = cam.position;
        Quaternion startRotation = cam.rotation;
        float startSize = sceneCamera.orthographicSize;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = cameraRestoreEasing != null && cameraRestoreEasing.length > 0
                ? cameraRestoreEasing.Evaluate(progress) : progress;
            cam.position = Vector3.LerpUnclamped(startPosition, originalCameraPosition, eased);
            cam.rotation = Quaternion.SlerpUnclamped(startRotation, originalCameraRotation, eased);
            if (sceneCamera.orthographic)
                sceneCamera.orthographicSize = Mathf.Max(0.01f, Mathf.LerpUnclamped(startSize, originalCameraSize, eased));
            yield return null;
        }
        RestoreCameraImmediately();
    }

    private void RestoreCameraImmediately()
    {
        if (!cameraSnapshotTaken) return;
        if (sceneCamera != null)
        {
            sceneCamera.transform.position = originalCameraPosition;
            sceneCamera.transform.rotation = originalCameraRotation;
            if (sceneCamera.orthographic) sceneCamera.orthographicSize = originalCameraSize;
        }
        cameraSnapshotTaken = false;
    }

    /// <summary>
    /// 지정한 클립을 Animator의 PlayableGraph로 직접 재생합니다.
    /// Animator Controller에 클립별 State를 미리 만들 필요가 없습니다.
    /// 클립이 비어 있으면 기존 클립을 중지하고 Animator Controller로 돌아갑니다.
    /// </summary>
    private void PlayStepClip(TransformStep step)
    {
        StopStepClip();

        if (step == null || step.animationClip == null)
            return;

        Animator targetAnimator = animator != null ? animator : Target.GetComponent<Animator>();
        if (targetAnimator == null)
        {
            Debug.LogWarning("[SpriteTransformSequence] AnimationClip 재생을 위한 Animator가 없습니다.", this);
            return;
        }

        clipGraph = PlayableGraph.Create("SpriteTransformSequence_StepClip");
        clipGraph.SetTimeUpdateMode(useUnscaledTime ? DirectorUpdateMode.UnscaledGameTime : DirectorUpdateMode.GameTime);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(clipGraph, "StepAnimation", targetAnimator);
        AnimationClipPlayable playable = AnimationClipPlayable.Create(clipGraph, step.animationClip);
        output.SetSourcePlayable(playable);
        clipGraph.Play();
    }

    private void StopStepClip()
    {
        if (clipGraph.IsValid())
            clipGraph.Destroy();
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private Vector3 GetPosition(Transform t) => useLocalTransform ? t.localPosition : t.position;
    private Quaternion GetRotation(Transform t) => useLocalTransform ? t.localRotation : t.rotation;

    private void SetPosition(Transform t, Vector3 value)
    {
        if (useLocalTransform) t.localPosition = value;
        else t.position = value;
    }

    private void SetRotation(Transform t, Quaternion value)
    {
        if (useLocalTransform) t.localRotation = value;
        else t.rotation = value;
    }

    private static void ApplyFlip(SpriteRenderer sprite, TransformStep step)
    {
        if (sprite == null || step == null)
            return;
        sprite.flipX = step.flipX;
        sprite.flipY = step.flipY;
    }

    private void ApplyStep(TransformStep step)
    {
        Transform t = Target;
        SetPosition(t, step.position);
        SetRotation(t, Quaternion.Euler(step.rotation));
        t.localScale = step.scale;

        SpriteRenderer sprite = SpriteTarget;
        if (sprite != null)
        {
            sprite.color = step.color;
            ApplyFlip(sprite, step);
        }
    }
}
