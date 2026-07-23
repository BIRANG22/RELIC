using System.Collections;
using UnityEngine;

/// <summary>
/// 일반 스프라이트 오브젝트에 마우스를 올렸을 때
/// 지정한 대상의 위치와 스케일을 부드럽게 변경하고,
/// 호버 중에만 별도의 오브젝트를 활성화합니다.
/// </summary>
public class SpriteHoverScale : MonoBehaviour
{
    [Header("변경 대상")]
    [Tooltip("호버 시 위치와 스케일을 변경할 오브젝트입니다. 비워두면 현재 오브젝트를 사용합니다.")]
    [SerializeField] private Transform targetImage;

    [Header("호버 스케일")]
    [Tooltip("마우스를 올렸을 때 적용할 스케일 배율입니다.")]
    [Min(0f)]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;

    [Header("호버 위치")]
    [Tooltip("기본 위치에서 이동할 거리입니다.")]
    [SerializeField] private Vector3 hoverPositionOffset = Vector3.zero;

    [Header("변경 시간")]
    [Tooltip("위치와 스케일이 변경되는 데 걸리는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float transitionDuration = 0.15f;

    [Tooltip("위치와 스케일 변화에 적용할 곡선입니다.")]
    [SerializeField]
    private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("호버 표시 오브젝트")]
    [Tooltip("평소에는 꺼져 있다가 호버 중에만 켜질 오브젝트입니다.")]
    [SerializeField] private GameObject hoverOnlyObject;

    [Header("복구 설정")]
    [Tooltip("이 컴포넌트가 비활성화될 때 원래 상태로 복구합니다.")]
    [SerializeField] private bool resetOnDisable = true;

    // 최초 또는 RefreshOriginalTransform 호출 시 저장한 고정 기준값입니다.
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;

    private Coroutine transitionCoroutine;
    private bool initialized;
    private bool isHovering;

    private void Awake()
    {
        Initialize();

        if (hoverOnlyObject != null)
            hoverOnlyObject.SetActive(false);
    }

    /// <summary>
    /// 대상 오브젝트와 현재 상태를 고정 기준값으로 저장합니다.
    /// </summary>
    private void Initialize()
    {
        if (targetImage == null)
            targetImage = transform;

        originalLocalPosition = targetImage.localPosition;
        originalLocalScale = targetImage.localScale;
        initialized = true;
    }

    private void OnMouseEnter()
    {
        if (UIPanelButton.IsMenuPanelOpen)
        {
            ResetHoverImmediate();
            return;
        }

        if (!initialized)
            Initialize();

        if (targetImage == null || isHovering)
            return;

        isHovering = true;

        // 현재 전환 중인 값이 아니라 고정된 원래 값을 기준으로 계산합니다.
        Vector3 hoverTargetPosition =
            originalLocalPosition + hoverPositionOffset;

        Vector3 hoverTargetScale =
            originalLocalScale * hoverScaleMultiplier;

        StartTransition(hoverTargetPosition, hoverTargetScale);

        if (hoverOnlyObject != null)
            hoverOnlyObject.SetActive(true);
    }

    private void Update()
    {
        // 메뉴가 열린 순간 이미 적용 중이던 월드 호버 효과도 즉시 해제합니다.
        if (isHovering && UIPanelButton.IsMenuPanelOpen)
            ResetHoverImmediate();
    }

    private void OnMouseExit()
    {
        if (!isHovering)
            return;

        isHovering = false;

        StartTransition(originalLocalPosition, originalLocalScale);

        if (hoverOnlyObject != null)
            hoverOnlyObject.SetActive(false);
    }

    private void StartTransition(
        Vector3 targetPosition,
        Vector3 targetScale)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(
            TransitionCoroutine(targetPosition, targetScale));
    }

    private IEnumerator TransitionCoroutine(
        Vector3 targetPosition,
        Vector3 targetScale)
    {
        if (targetImage == null)
        {
            transitionCoroutine = null;
            yield break;
        }

        Vector3 startPosition = targetImage.localPosition;
        Vector3 startScale = targetImage.localScale;

        if (transitionDuration <= 0f)
        {
            targetImage.localPosition = targetPosition;
            targetImage.localScale = targetScale;
            transitionCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsedTime / transitionDuration);

            float curvedTime = transitionCurve.Evaluate(normalizedTime);

            targetImage.localPosition = Vector3.LerpUnclamped(
                startPosition,
                targetPosition,
                curvedTime);

            targetImage.localScale = Vector3.LerpUnclamped(
                startScale,
                targetScale,
                curvedTime);

            yield return null;
        }

        targetImage.localPosition = targetPosition;
        targetImage.localScale = targetScale;
        transitionCoroutine = null;
    }

    /// <summary>
    /// 진행 중인 호버 전환을 중지하고 저장된 기본 위치와 스케일로 즉시 복구합니다.
    /// </summary>
    private void ResetHoverImmediate()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (initialized && targetImage != null)
        {
            targetImage.localPosition = originalLocalPosition;
            targetImage.localScale = originalLocalScale;
        }

        if (hoverOnlyObject != null)
            hoverOnlyObject.SetActive(false);

        isHovering = false;
    }

    private void OnDisable()
    {
        if (resetOnDisable)
        {
            ResetHoverImmediate();
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (hoverOnlyObject != null)
            hoverOnlyObject.SetActive(false);

        isHovering = false;
    }

    /// <summary>
    /// 현재 위치와 스케일을 새로운 기본 상태로 저장합니다.
    /// 반드시 호버가 끝난 상태에서 호출해야 합니다.
    /// </summary>
    public void RefreshOriginalTransform()
    {
        if (targetImage == null)
            targetImage = transform;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        originalLocalPosition = targetImage.localPosition;
        originalLocalScale = targetImage.localScale;
        initialized = true;
        isHovering = false;
    }
}
