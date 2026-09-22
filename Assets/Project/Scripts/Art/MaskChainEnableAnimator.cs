using System.Collections;
using UnityEngine;

public class MaskChainEnableAnimator : MonoBehaviour
{
    [Header("Mask")]
    [SerializeField] private RectTransform mask;

    [Header("Mask Position")]
    [SerializeField] private Vector2 maskPositionA;
    [SerializeField] private Vector2 maskPositionB;
    [SerializeField] private Vector2 maskPositionC;

    [Header("Position A To B")]
    [Tooltip("A 위치에서 B 위치로 이동하기 전 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float positionAToBDelay = 0f;

    [Tooltip("A 위치에서 B 위치로 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float positionAToBDuration = 0.5f;

    [Header("Position B Hold")]
    [Tooltip("B 위치에 도착한 뒤 멈춰 있는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float positionBHoldDelay = 0f;

    [Header("Position B To C")]
    [Tooltip("B 위치에서 C 위치로 이동하기 전 추가 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float positionBToCDelay = 0f;

    [Tooltip("B 위치에서 C 위치로 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float positionBToCDuration = 0.5f;

    [Header("Mask Height")]
    [Tooltip("시작 시 Mask의 Height입니다.")]
    [Min(0f)]
    [SerializeField] private float maskHeightA = 0f;

    [Tooltip("첫 번째 Height 변화 완료 시 Mask의 Height입니다.")]
    [Min(0f)]
    [SerializeField] private float maskHeightB = 500f;

    [Tooltip("두 번째 Height 변화 완료 시 Mask의 Height입니다.")]
    [Min(0f)]
    [SerializeField] private float maskHeightC = 500f;

    [Header("Height A To B")]
    [Tooltip("Height가 A에서 B로 변하기 전 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float heightAToBDelay = 0f;

    [Tooltip("Height가 A에서 B로 변하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float heightAToBDuration = 0.5f;

    [Header("Height B Hold")]
    [Tooltip("Height B에 도착한 뒤 멈춰 있는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float heightBHoldDelay = 0f;

    [Header("Height B To C")]
    [Tooltip("Height B에서 C로 변하기 전 추가 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float heightBToCDelay = 0f;

    [Tooltip("Height가 B에서 C로 변하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float heightBToCDuration = 0.5f;

    [Header("Deactivate")]
    [Tooltip("Position과 Height 애니메이션이 모두 끝난 뒤 이 GameObject를 비활성화하기 전 대기 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float deactivateDelay = 0f;

    [Header("Animation")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine sequenceCoroutine;
    private bool positionFinished;
    private bool heightFinished;

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
        positionFinished = false;
        heightFinished = false;

        StartCoroutine(PlayPositionSequence());
        StartCoroutine(PlayHeightSequence());

        while (!positionFinished || !heightFinished)
            yield return null;

        if (deactivateDelay > 0f)
            yield return Wait(deactivateDelay);

        sequenceCoroutine = null;
        gameObject.SetActive(false);
    }

    private IEnumerator PlayPositionSequence()
    {
        if (mask == null)
        {
            positionFinished = true;
            yield break;
        }

        if (positionAToBDelay > 0f)
            yield return Wait(positionAToBDelay);

        yield return AnimateLocalPosition(mask, maskPositionA, maskPositionB, positionAToBDuration);

        if (positionBHoldDelay > 0f)
            yield return Wait(positionBHoldDelay);

        if (positionBToCDelay > 0f)
            yield return Wait(positionBToCDelay);

        yield return AnimateLocalPosition(mask, maskPositionB, maskPositionC, positionBToCDuration);

        positionFinished = true;
    }

    private IEnumerator PlayHeightSequence()
    {
        if (mask == null)
        {
            heightFinished = true;
            yield break;
        }

        if (heightAToBDelay > 0f)
            yield return Wait(heightAToBDelay);

        yield return AnimateHeight(mask, maskHeightA, maskHeightB, heightAToBDuration);

        if (heightBHoldDelay > 0f)
            yield return Wait(heightBHoldDelay);

        if (heightBToCDelay > 0f)
            yield return Wait(heightBToCDelay);

        yield return AnimateHeight(mask, maskHeightB, maskHeightC, heightBToCDuration);

        heightFinished = true;
    }

    private IEnumerator AnimateLocalPosition(RectTransform target, Vector2 fromPosition, Vector2 toPosition, float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            SetLocalPosition(target, toPosition);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float evaluatedT = EvaluateCurve(t);

            SetLocalPosition(target, Vector2.LerpUnclamped(fromPosition, toPosition, evaluatedT));
            yield return null;
        }

        SetLocalPosition(target, toPosition);
    }

    private IEnumerator AnimateHeight(RectTransform target, float fromHeight, float toHeight, float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            SetHeight(target, toHeight);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float evaluatedT = EvaluateCurve(t);

            SetHeight(target, Mathf.LerpUnclamped(fromHeight, toHeight, evaluatedT));
            yield return null;
        }

        SetHeight(target, toHeight);
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
        if (mask == null)
            return;

        SetLocalPosition(mask, maskPositionA);
        SetHeight(mask, maskHeightA);
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private float EvaluateCurve(float t)
    {
        return moveCurve != null ? moveCurve.Evaluate(t) : t;
    }

    private void SetLocalPosition(RectTransform target, Vector2 position)
    {
        if (target == null)
            return;

        Vector3 localPosition = target.localPosition;
        localPosition.x = position.x;
        localPosition.y = position.y;
        target.localPosition = localPosition;
    }

    private void SetHeight(RectTransform target, float height)
    {
        if (target == null)
            return;

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, height));
    }
}
