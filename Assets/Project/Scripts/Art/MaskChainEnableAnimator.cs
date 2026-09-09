using System.Collections;
using UnityEngine;

public class MaskChainEnableAnimator : MonoBehaviour
{
    [Header("Mask")]
    [SerializeField] private RectTransform mask;
    [SerializeField] private Vector2 maskPositionA;
    [SerializeField] private Vector2 maskPositionB;
    [SerializeField] private Vector2 maskPositionC;

    [Header("Chain Under")]
    [SerializeField] private RectTransform chainUnder;
    [SerializeField] private Vector2 chainUnderPositionA;
    [SerializeField] private Vector2 chainUnderPositionB;
    [SerializeField] private Vector2 chainUnderPositionC;

    [Header("Start Delay")]
    [Tooltip("Mask가 A 위치에서 B 위치로 이동하기 전 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float maskStartDelay = 0f;

    [Tooltip("Chain Under가 A 위치에서 B 위치로 이동하기 전 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float chainStartDelay = 0f;

    [Header("A To B")]
    [Tooltip("Mask와 Chain Under가 A에서 B로 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float aToBDuration = 0.5f;

    [Header("Mask B To C")]
    [Tooltip("Mask가 B에 도착한 뒤, C 방향으로 천천히 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float maskBToCDelay = 0f;

    [Tooltip("대기 시간 동안 B에서 C까지 거리 중 몇 %만큼 천천히 이동할지 지정합니다. 0.15 = 15%.")]
    [Range(0f, 1f)]
    [SerializeField] private float maskBToCDriftRatio = 0.15f;

    [Tooltip("천천히 이동한 지점에서 C까지 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float maskBToCDuration = 0.5f;

    [Header("Chain B To C")]
    [Tooltip("Chain Under가 B에 도착한 뒤, C 방향으로 천천히 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float chainBToCDelay = 0f;

    [Tooltip("대기 시간 동안 B에서 C까지 거리 중 몇 %만큼 천천히 이동할지 지정합니다. 0.15 = 15%.")]
    [Range(0f, 1f)]
    [SerializeField] private float chainBToCDriftRatio = 0.15f;

    [Tooltip("천천히 이동한 지점에서 C까지 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float chainBToCDuration = 0.5f;

    [Header("Deactivate")]
    [Tooltip("정방향 애니메이션이 모두 끝난 뒤 이 GameObject를 비활성화하기 전 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float deactivateDelay = 0f;

    [Header("Animation")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine sequenceCoroutine;
    private bool maskPhaseFinished;
    private bool chainPhaseFinished;

    private void OnEnable()
    {
        ResetToStartState();

        if (sequenceCoroutine != null)
            StopCoroutine(sequenceCoroutine);

        sequenceCoroutine = StartCoroutine(PlayFullSequence());
    }

    private void OnDisable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
    }

    private IEnumerator PlayFullSequence()
    {
        maskPhaseFinished = false;
        chainPhaseFinished = false;

        StartCoroutine(PlayMaskForward());
        StartCoroutine(PlayChainForward());

        while (!maskPhaseFinished || !chainPhaseFinished)
            yield return null;

        if (deactivateDelay > 0f)
            yield return Wait(deactivateDelay);

        sequenceCoroutine = null;
        gameObject.SetActive(false);
    }

    private IEnumerator PlayMaskForward()
    {
        if (maskStartDelay > 0f)
            yield return Wait(maskStartDelay);

        // A -> B
        yield return AnimateRectPosition(mask, maskPositionA, maskPositionB, aToBDuration);

        // B에서 멈추지 않고, 딜레이 시간 동안 C 방향으로 조금씩 이동합니다.
        Vector2 driftEnd = Vector2.Lerp(maskPositionB, maskPositionC, maskBToCDriftRatio);
        if (maskBToCDelay > 0f)
            yield return AnimateRectPosition(mask, maskPositionB, driftEnd, maskBToCDelay, false);
        else
            SetRectPosition(mask, driftEnd);

        // 드리프트가 끝난 위치 -> C
        yield return AnimateRectPosition(mask, driftEnd, maskPositionC, maskBToCDuration);

        maskPhaseFinished = true;
    }

    private IEnumerator PlayChainForward()
    {
        if (chainStartDelay > 0f)
            yield return Wait(chainStartDelay);

        // A -> B
        yield return AnimateRectPosition(chainUnder, chainUnderPositionA, chainUnderPositionB, aToBDuration);

        // B에서 멈추지 않고, 딜레이 시간 동안 C 방향으로 조금씩 이동합니다.
        Vector2 driftEnd = Vector2.Lerp(chainUnderPositionB, chainUnderPositionC, chainBToCDriftRatio);
        if (chainBToCDelay > 0f)
            yield return AnimateRectPosition(chainUnder, chainUnderPositionB, driftEnd, chainBToCDelay, false);
        else
            SetRectPosition(chainUnder, driftEnd);

        // 드리프트가 끝난 위치 -> C
        yield return AnimateRectPosition(chainUnder, driftEnd, chainUnderPositionC, chainBToCDuration);

        chainPhaseFinished = true;
    }

    private IEnumerator AnimateRectPosition(
        RectTransform target,
        Vector2 fromPosition,
        Vector2 toPosition,
        float duration,
        bool useCurve = true)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            SetRectPosition(target, toPosition);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float evaluatedT = useCurve ? EvaluateCurve(t) : t;

            SetRectPosition(target, Vector2.LerpUnclamped(fromPosition, toPosition, evaluatedT));
            yield return null;
        }

        SetRectPosition(target, toPosition);
    }

    private IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += DeltaTime();
            yield return null;
        }
    }

    private void ResetToStartState()
    {
        SetRectPosition(mask, maskPositionA);
        SetRectPosition(chainUnder, chainUnderPositionA);
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private float EvaluateCurve(float t)
    {
        return moveCurve != null ? moveCurve.Evaluate(t) : t;
    }

    private void SetRectPosition(RectTransform target, Vector2 position)
    {
        if (target != null)
            target.anchoredPosition = position;
    }
}
