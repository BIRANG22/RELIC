using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

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

    [Serializable]
    public sealed class VfxActivation
    {
        [Tooltip("활성화할 VFX 오브젝트를 지정합니다. 프리팹의 비활성 자식 오브젝트를 연결할 수 있습니다.")]
        public GameObject vfxObject;
        [Tooltip("해당 Step이 시작된 시점부터 VFX를 활성화하기까지의 대기 시간(초)")]
        [Min(0f)] public float delay = 0f;
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
        [Header("VFX 활성화 (Step 시작 기준)")]
        public List<VfxActivation> vfxObjects = new List<VfxActivation>();
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
    [Tooltip("마지막 단계와 카메라 복귀가 모두 끝나면 이 오브젝트를 비활성화합니다. Loop가 켜져 있으면 반복이 끝나기 전까지 비활성화하지 않습니다.")]
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
    private readonly List<Coroutine> activeVfxCoroutines = new List<Coroutine>();
    private int pendingVfxCount;

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
        StopVfxActivations();
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
                StartStepVfx(steps[0]);
                if (HasCameraMotions(steps[0]))
                    yield return PlayCameraMotions(steps[0]);
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
                StartStepVfx(step);

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
            }
        }
        while (loop && isActiveAndEnabled);

        // 마지막 Step에서 예약한 VFX가 아직 대기 중이라면 활성화될 때까지 기다립니다.
        while (pendingVfxCount > 0)
            yield return null;
        activeVfxCoroutines.Clear();

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

    private void StartStepVfx(TransformStep step)
    {
        if (step == null || step.vfxObjects == null)
            return;

        foreach (VfxActivation entry in step.vfxObjects)
        {
            if (entry == null || entry.vfxObject == null)
                continue;

            // 코루틴을 시작하기 전에 수를 증가시켜, Delay가 0이어도 정확히 집계합니다.
            pendingVfxCount++;
            Coroutine coroutine = StartCoroutine(ActivateVfxAfterDelay(entry.vfxObject, entry.delay));
            if (coroutine != null)
                activeVfxCoroutines.Add(coroutine);
        }
    }

    private IEnumerator ActivateVfxAfterDelay(GameObject vfxObject, float delay)
    {
        float remaining = Mathf.Max(0f, delay);
        while (remaining > 0f)
        {
            yield return null;
            remaining -= DeltaTime;
        }

        if (vfxObject != null)
            vfxObject.SetActive(true);

        pendingVfxCount = Mathf.Max(0, pendingVfxCount - 1);
    }

    private void StopVfxActivations()
    {
        foreach (Coroutine coroutine in activeVfxCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeVfxCoroutines.Clear();
        pendingVfxCount = 0;
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
