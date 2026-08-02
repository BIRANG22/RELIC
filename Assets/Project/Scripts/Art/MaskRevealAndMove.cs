using System.Collections;
using UnityEngine;

public class MaskRevealAndMove : MonoBehaviour
{
    [Header("마스크 설정")]
    [Tooltip("높이를 변경할 RectMask2D 오브젝트의 RectTransform")]
    [SerializeField] private RectTransform maskRect;

    [Tooltip("연출 시작 시 마스크 높이")]
    [SerializeField] private float startMaskHeight = 0f;

    [Tooltip("연출 완료 시 마스크 높이")]
    [SerializeField] private float targetMaskHeight = 500f;

    [Tooltip("마스크 높이가 변경되는 시간")]
    [SerializeField] private float maskDuration = 1f;

    [Header("이동 오브젝트 설정")]
    [Tooltip("이동시킬 UI 오브젝트")]
    [SerializeField] private RectTransform movingObject;

    [Tooltip("이동 시작 위치")]
    [SerializeField] private Vector2 startPosition;

    [Tooltip("이동 완료 위치")]
    [SerializeField] private Vector2 targetPosition;

    [Tooltip("오브젝트가 이동하는 시간")]
    [SerializeField] private float moveDuration = 1f;

    [Header("공통 설정")]
    [Tooltip("오브젝트 활성화 후 연출이 시작되기까지의 대기 시간")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("마스크 높이 변화에 적용할 곡선")]
    [SerializeField]
    private AnimationCurve maskCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("오브젝트 이동에 적용할 곡선")]
    [SerializeField]
    private AnimationCurve moveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Time.timeScale이 0이어도 연출을 실행할지 여부")]
    [SerializeField] private bool useUnscaledTime = false;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (maskRect != null)
        {
            // 마스크의 위쪽을 고정하고 아래쪽으로 높이가 변하도록 설정
            maskRect.pivot = new Vector2(maskRect.pivot.x, 1f);
        }
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    /// <summary>
    /// 마스크 및 이동 연출을 처음부터 재생합니다.
    /// </summary>
    public void PlayAnimation()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        StopAnimation();
        animationCoroutine = StartCoroutine(AnimationRoutine());
    }

    /// <summary>
    /// 연출을 정지하고 시작 상태로 되돌립니다.
    /// </summary>
    public void ResetAnimation()
    {
        StopAnimation();
        SetInitialState();
    }

    private void StopAnimation()
    {
        if (animationCoroutine == null)
        {
            return;
        }

        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
    }

    private IEnumerator AnimationRoutine()
    {
        SetInitialState();

        if (startDelay > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }
            else
            {
                yield return new WaitForSeconds(startDelay);
            }
        }

        float elapsedTime = 0f;
        float totalDuration = Mathf.Max(maskDuration, moveDuration);

        while (elapsedTime < totalDuration)
        {
            elapsedTime += GetDeltaTime();

            UpdateMask(elapsedTime);
            UpdateMovingObject(elapsedTime);

            yield return null;
        }

        // 마지막 프레임에서 정확한 완료 값으로 보정
        SetMaskHeight(targetMaskHeight);

        if (movingObject != null)
        {
            movingObject.anchoredPosition = targetPosition;
        }

        animationCoroutine = null;
    }

    private void SetInitialState()
    {
        SetMaskHeight(startMaskHeight);

        if (movingObject != null)
        {
            movingObject.anchoredPosition = startPosition;
        }
    }

    private void UpdateMask(float elapsedTime)
    {
        if (maskRect == null)
        {
            return;
        }

        float progress = maskDuration <= 0f
            ? 1f
            : Mathf.Clamp01(elapsedTime / maskDuration);

        float curvedProgress = maskCurve.Evaluate(progress);

        float currentHeight = Mathf.LerpUnclamped(
            startMaskHeight,
            targetMaskHeight,
            curvedProgress
        );

        SetMaskHeight(currentHeight);
    }

    private void UpdateMovingObject(float elapsedTime)
    {
        if (movingObject == null)
        {
            return;
        }

        float progress = moveDuration <= 0f
            ? 1f
            : Mathf.Clamp01(elapsedTime / moveDuration);

        float curvedProgress = moveCurve.Evaluate(progress);

        movingObject.anchoredPosition = Vector2.LerpUnclamped(
            startPosition,
            targetPosition,
            curvedProgress
        );
    }

    private void SetMaskHeight(float height)
    {
        if (maskRect == null)
        {
            return;
        }

        maskRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(0f, height)
        );
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }
}