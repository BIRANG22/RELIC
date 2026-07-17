using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleHitImpactFeedback : MonoBehaviour
{
    private const string AutoInstanceName = "BattleHitImpactFeedback_Auto";

    private static BattleHitImpactFeedback instance;

    [Header("Damage Hit Push")]
    [SerializeField] private bool enableDamageHitPush = true;
    [SerializeField] private float damageHitPushDistance = 0.5f;           //밀리는거리
    [SerializeField] private float attackerPushMultiplier = 0.65f;
    [SerializeField] private float targetPushMultiplier = 1f;
    [SerializeField] private float damageHitPushOutDuration = 0.8f;        //밀리는시간
    [SerializeField] private float damageHitPushHoldDuration = 0.1f;        //정지시간
    [SerializeField] private float damageHitPushReturnDuration = 0.06f;     //돌아오는 시간
    [SerializeField] private AnimationCurve damageHitPushCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Buff Debuff Pulse")]
    [SerializeField] private bool enableStatusPulse = true;
    [SerializeField] private float statusPulseScale = 1.12f;
    [SerializeField] private float statusPulseOutDuration = 0.055f;
    [SerializeField] private float statusPulseHoldDuration = 0.1f;
    [SerializeField] private float statusPulseReturnDuration = 0.1f;
    [SerializeField] private AnimationCurve statusPulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Camera")]
    [SerializeField] private bool playDamageCameraImpact = true;

    private readonly Dictionary<Transform, TransformVectorState> activePositionStates = new();
    private readonly Dictionary<Transform, TransformVectorState> activeScaleStates = new();
    private readonly Dictionary<Transform, Coroutine> statusPulseRoutines = new();

    private sealed class TransformVectorState
    {
        public Vector3 Value;
        public int RefCount;
    }

    private struct MoveEntry
    {
        public Transform Target;
        public Vector3 OriginalPosition;
        public Vector3 Offset;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
            return;

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static IEnumerator PlayDamageHitFeedback(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        int fallbackHorizontalDirection)
    {
        BattleHitImpactFeedback feedback = GetOrCreateInstance();

        if (feedback == null)
        {
            if (BattleCameraController.Instance != null)
                yield return BattleCameraController.Instance.PlayDamageImpact();

            yield break;
        }

        yield return feedback.PlayDamageHitInternal(attacker, targets, fallbackHorizontalDirection);
    }

    public static void PlayStatusHitFeedback(Transform target)
    {
        BattleHitImpactFeedback feedback = GetOrCreateInstance();

        if (feedback == null)
            return;

        feedback.StartStatusPulse(target);
    }

    public static int ResolveHorizontalDirection(
        Transform attacker,
        Transform target,
        int fallbackHorizontalDirection)
    {
        int fallback = NormalizeHorizontalDirection(fallbackHorizontalDirection);

        if (attacker == null || target == null)
            return fallback;

        float deltaX = target.position.x - attacker.position.x;

        if (Mathf.Abs(deltaX) <= 0.001f)
            return fallback;

        return deltaX > 0f ? 1 : -1;
    }

    private static BattleHitImpactFeedback GetOrCreateInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<BattleHitImpactFeedback>(FindObjectsInactive.Include);

        if (instance != null)
            return instance;

        GameObject gameObject = new(AutoInstanceName);
        instance = gameObject.AddComponent<BattleHitImpactFeedback>();
        return instance;
    }

    private IEnumerator PlayDamageHitInternal(
    Transform attacker,
    IReadOnlyList<Transform> targets,
    int fallbackHorizontalDirection)
    {
        // 실제 피해 적중 시 BattleEffect의 두 Plane 회전 연출을 실행합니다.
        BattleEffectPlaneRotation.PlayHitRotationFeedback();

        IEnumerator cameraRoutine =
            playDamageCameraImpact && BattleCameraController.Instance != null
                ? BattleCameraController.Instance.PlayDamageImpact()
                : null;

        IEnumerator pushRoutine = enableDamageHitPush
            ? PlayDamagePush(
                attacker,
                targets,
                fallbackHorizontalDirection
            )
            : null;

        yield return RunTogether(
            cameraRoutine,
            pushRoutine
        );
    }

    private IEnumerator RunTogether(params IEnumerator[] routines)
    {
        bool[] completed = new bool[routines.Length];
        int remaining = 0;

        for (int i = 0; i < routines.Length; i++)
        {
            if (routines[i] == null)
            {
                completed[i] = true;
                continue;
            }

            remaining++;
            int routineIndex = i;
            StartCoroutine(RunAndMarkCompleted(routines[i], () =>
            {
                if (completed[routineIndex])
                    return;

                completed[routineIndex] = true;
                remaining--;
            }));
        }

        while (remaining > 0)
            yield return null;
    }

    private static IEnumerator RunAndMarkCompleted(IEnumerator routine, System.Action onCompleted)
    {
        yield return routine;
        onCompleted?.Invoke();
    }

    private IEnumerator PlayDamagePush(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        int fallbackHorizontalDirection)
    {
        List<MoveEntry> entries = BuildDamageMoveEntries(attacker, targets, fallbackHorizontalDirection);

        if (entries.Count <= 0)
            yield break;

        try
        {
            yield return AnimateDamagePush(entries);
        }
        finally
        {
            ReleaseMoveEntries(entries);
        }
    }

    private List<MoveEntry> BuildDamageMoveEntries(
        Transform attacker,
        IReadOnlyList<Transform> targets,
        int fallbackHorizontalDirection)
    {
        List<MoveEntry> entries = new();
        HashSet<Transform> added = new();

        Transform firstTarget = GetFirstValidTarget(targets);
        int horizontalDirection = ResolveHorizontalDirection(attacker, firstTarget, fallbackHorizontalDirection);
        Vector3 baseOffset = Vector3.right * horizontalDirection * Mathf.Max(0f, damageHitPushDistance);

        AddMoveEntry(entries, added, attacker, baseOffset * Mathf.Max(0f, attackerPushMultiplier));

        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
                AddMoveEntry(entries, added, targets[i], baseOffset * Mathf.Max(0f, targetPushMultiplier));
        }

        return entries;
    }

    private static Transform GetFirstValidTarget(IReadOnlyList<Transform> targets)
    {
        if (targets == null)
            return null;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
                return targets[i];
        }

        return null;
    }

    private void AddMoveEntry(
        List<MoveEntry> entries,
        HashSet<Transform> added,
        Transform target,
        Vector3 offset)
    {
        if (target == null || added.Contains(target))
            return;

        added.Add(target);
        entries.Add(new MoveEntry
        {
            Target = target,
            OriginalPosition = AcquireOriginalValue(activePositionStates, target, target.position),
            Offset = offset
        });
    }

    private IEnumerator AnimateDamagePush(List<MoveEntry> entries)
    {
        yield return AnimatePositions(entries, 0f, 1f, damageHitPushOutDuration);
        yield return WaitUnscaledWithVfxPause(damageHitPushHoldDuration);
        yield return AnimatePositions(entries, 1f, 0f, damageHitPushReturnDuration);
    }

    private IEnumerator AnimatePositions(
        List<MoveEntry> entries,
        float from,
        float to,
        float duration)
    {
        if (duration <= 0f)
        {
            ApplyDamageMove(entries, to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EvaluateCurve(damageHitPushCurve, t);
            ApplyDamageMove(entries, Mathf.Lerp(from, to, eased));
            yield return null;
        }

        ApplyDamageMove(entries, to);
    }

    private void ApplyDamageMove(List<MoveEntry> entries, float amount)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            MoveEntry entry = entries[i];

            if (entry.Target == null)
                continue;

            entry.Target.position = entry.OriginalPosition + entry.Offset * amount;
        }
    }

    private void ReleaseMoveEntries(List<MoveEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Transform target = entries[i].Target;
            ReleaseOriginalValue(activePositionStates, target, value => target.position = value);
        }
    }

    private void StartStatusPulse(Transform target)
    {
        if (!enableStatusPulse || target == null || !isActiveAndEnabled)
            return;

        if (statusPulseRoutines.TryGetValue(target, out Coroutine routine) && routine != null)
        {
            StopCoroutine(routine);
            ReleaseOriginalValue(activeScaleStates, target, value => target.localScale = value);
        }

        statusPulseRoutines[target] = StartCoroutine(PlayStatusPulse(target));
    }

    private IEnumerator PlayStatusPulse(Transform target)
    {
        if (target == null)
            yield break;

        Vector3 originalScale = AcquireOriginalValue(activeScaleStates, target, target.localScale);
        Vector3 peakScale = originalScale * Mathf.Max(0.01f, statusPulseScale);

        try
        {
            yield return AnimateScale(target, originalScale, peakScale, statusPulseOutDuration);
            yield return WaitUnscaledWithVfxPause(statusPulseHoldDuration);
            yield return AnimateScale(target, peakScale, originalScale, statusPulseReturnDuration);
        }
        finally
        {
            statusPulseRoutines.Remove(target);
            ReleaseOriginalValue(activeScaleStates, target, value => target.localScale = value);
        }
    }

    private IEnumerator AnimateScale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EvaluateCurve(statusPulseCurve, t);
            target.localScale = Vector3.Lerp(from, to, eased);
            yield return null;
        }

        if (target != null)
            target.localScale = to;
    }

    private static IEnumerator WaitUnscaled(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitUnscaledWithVfxPause(float duration)
    {
        if (duration <= 0f)
            yield break;

        BattleVfxPlaybackPauseController.PauseAll();

        try
        {
            yield return WaitUnscaled(duration);
        }
        finally
        {
            BattleVfxPlaybackPauseController.ResumeAll();
        }
    }

    private static Vector3 AcquireOriginalValue(
        Dictionary<Transform, TransformVectorState> states,
        Transform target,
        Vector3 currentValue)
    {
        if (target == null)
            return currentValue;

        if (!states.TryGetValue(target, out TransformVectorState state))
        {
            state = new TransformVectorState
            {
                Value = currentValue,
                RefCount = 0
            };
            states[target] = state;
        }

        state.RefCount++;
        return state.Value;
    }

    private static void ReleaseOriginalValue(
        Dictionary<Transform, TransformVectorState> states,
        Transform target,
        System.Action<Vector3> restore)
    {
        if (object.ReferenceEquals(target, null))
            return;

        if (!states.TryGetValue(target, out TransformVectorState state))
            return;

        state.RefCount = Mathf.Max(0, state.RefCount - 1);

        if (state.RefCount > 0)
            return;

        if (target != null)
            restore?.Invoke(state.Value);

        states.Remove(target);
    }

    private static int NormalizeHorizontalDirection(int direction)
    {
        return direction < 0 ? -1 : 1;
    }

    private static float EvaluateCurve(AnimationCurve curve, float t)
    {
        return curve != null ? curve.Evaluate(t) : t * t * (3f - 2f * t);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        damageHitPushDistance = Mathf.Max(0f, damageHitPushDistance);
        attackerPushMultiplier = Mathf.Max(0f, attackerPushMultiplier);
        targetPushMultiplier = Mathf.Max(0f, targetPushMultiplier);
        damageHitPushOutDuration = Mathf.Max(0f, damageHitPushOutDuration);
        damageHitPushHoldDuration = Mathf.Max(0f, damageHitPushHoldDuration);
        damageHitPushReturnDuration = Mathf.Max(0f, damageHitPushReturnDuration);
        statusPulseScale = Mathf.Max(0.01f, statusPulseScale);
        statusPulseOutDuration = Mathf.Max(0f, statusPulseOutDuration);
        statusPulseHoldDuration = Mathf.Max(0f, statusPulseHoldDuration);
        statusPulseReturnDuration = Mathf.Max(0f, statusPulseReturnDuration);
    }
#endif
}
