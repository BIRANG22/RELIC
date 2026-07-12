using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 로비의 이전/다음 화살표 버튼에 마우스를 올렸을 때
/// 지정한 화살표 이미지를 좌우로 천천히 반복 이동시킵니다.
/// </summary>
public class LobbyArrowHoverMoveEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum MoveDirection
    {
        Left = -1,
        Right = 1
    }

    [Header("Move Target")]
    [Tooltip("실제로 움직일 화살표 이미지의 RectTransform입니다. 버튼 자체가 아닌 자식 이미지를 연결하는 것을 권장합니다.")]
    [SerializeField] private RectTransform moveTarget;

    [Header("Hover Move")]
    [Tooltip("이전 버튼은 Left, 다음 버튼은 Right로 설정합니다.")]
    [SerializeField] private MoveDirection moveDirection = MoveDirection.Right;

    [Tooltip("원래 위치에서 이동할 최대 거리입니다.")]
    [SerializeField, Min(0f)] private float moveDistance = 10f;

    [Tooltip("원래 위치에서 이동한 뒤 다시 돌아오기까지의 한 번 왕복 시간입니다.")]
    [SerializeField, Min(0.01f)] private float roundTripDuration = 0.8f;

    [Tooltip("마우스가 빠졌을 때 원래 위치로 돌아오는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float returnDuration = 0.15f;

    private Vector2 originalAnchoredPosition;
    private Coroutine moveRoutine;
    private bool isPointerInside;
    private bool hasCachedOriginalPosition;

    private RectTransform Target
    {
        get
        {
            if (moveTarget == null)
                moveTarget = transform as RectTransform;

            return moveTarget;
        }
    }

    private void Awake()
    {
        CacheOriginalPosition();
    }

    private void OnEnable()
    {
        CacheOriginalPosition();
        ResetPositionImmediate();
    }

    private void OnDisable()
    {
        StopMoveRoutine();
        isPointerInside = false;
        ResetPositionImmediate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        StartMoveRoutine();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        StartMoveRoutine();
    }

    private void CacheOriginalPosition()
    {
        RectTransform target = Target;

        if (target == null)
            return;

        originalAnchoredPosition = target.anchoredPosition;
        hasCachedOriginalPosition = true;
    }

    private void StartMoveRoutine()
    {
        StopMoveRoutine();

        if (!gameObject.activeInHierarchy || Target == null)
            return;

        if (!hasCachedOriginalPosition)
            CacheOriginalPosition();

        moveRoutine = StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        RectTransform target = Target;

        if (target == null)
            yield break;

        float phase = 0f;
        float safeRoundTripDuration = Mathf.Max(0.01f, roundTripDuration);
        float direction = (float)moveDirection;

        while (isPointerInside)
        {
            phase += Time.unscaledDeltaTime * Mathf.PI / safeRoundTripDuration;

            if (phase >= Mathf.PI)
                phase -= Mathf.PI;

            float offset = Mathf.Sin(phase) * moveDistance * direction;
            target.anchoredPosition = originalAnchoredPosition + new Vector2(offset, 0f);
            yield return null;
        }

        Vector2 startPosition = target.anchoredPosition;
        float safeReturnDuration = Mathf.Max(0.01f, returnDuration);
        float elapsed = 0f;

        while (elapsed < safeReturnDuration && !isPointerInside)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeReturnDuration);
            t = t * t * (3f - 2f * t);
            target.anchoredPosition = Vector2.LerpUnclamped(startPosition, originalAnchoredPosition, t);
            yield return null;
        }

        if (!isPointerInside)
            target.anchoredPosition = originalAnchoredPosition;

        moveRoutine = null;
    }

    private void StopMoveRoutine()
    {
        if (moveRoutine == null)
            return;

        StopCoroutine(moveRoutine);
        moveRoutine = null;
    }

    private void ResetPositionImmediate()
    {
        RectTransform target = Target;

        if (target != null && hasCachedOriginalPosition)
            target.anchoredPosition = originalAnchoredPosition;
    }
}
