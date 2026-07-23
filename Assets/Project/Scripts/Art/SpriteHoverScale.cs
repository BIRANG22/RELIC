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
    [Tooltip("호버를 시작한 현재 위치에서 이동할 거리입니다.")]
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

    // 호버가 시작되기 직전의 위치와 스케일입니다.
    private Vector3 positionBeforeHover;
    private Vector3 scaleBeforeHover;

    private Coroutine transitionCoroutine;
    private bool initialized;
    private bool isHovering;

    private void Awake()
    {
        Initialize();

        if (hoverOnlyObject != null)
        {
            hoverOnlyObject.SetActive(false);
        }
    }

    /// <summary>
    /// 대상 오브젝트와 현재 상태를 저장합니다.
    /// </summary>
    private void Initialize()
    {
        if (targetImage == null)
        {
            targetImage = transform;
        }

        positionBeforeHover = targetImage.localPosition;
        scaleBeforeHover = targetImage.localScale;

        initialized = true;
    }

    private void OnMouseEnter()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (targetImage == null || isHovering)
        {
            return;
        }

        isHovering = true;

        // 호버가 시작되는 순간의 실제 위치와 스케일을 저장합니다.
        positionBeforeHover = targetImage.localPosition;
        scaleBeforeHover = targetImage.localScale;

        Vector3 hoverTargetPosition =
            positionBeforeHover + hoverPositionOffset;

        Vector3 hoverTargetScale =
            scaleBeforeHover * hoverScaleMultiplier;

        StartTransition(hoverTargetPosition, hoverTargetScale);

        if (hoverOnlyObject != null)
        {
            hoverOnlyObject.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        if (!isHovering)
        {
            return;
        }

        isHovering = false;

        // 저장된 호버 이전 위치와 스케일로 부드럽게 돌아갑니다.
        StartTransition(positionBeforeHover, scaleBeforeHover);

        if (hoverOnlyObject != null)
        {
            hoverOnlyObject.SetActive(false);
        }
    }

    /// <summary>
    /// 기존 전환을 중지하고 새로운 위치 및 스케일 전환을 시작합니다.
    /// </summary>
    private void StartTransition(
        Vector3 targetPosition,
        Vector3 targetScale)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(
            TransitionCoroutine(targetPosition, targetScale)
        );
    }

    /// <summary>
    /// 현재 상태에서 목표 위치와 스케일까지 부드럽게 변경합니다.
    /// </summary>
    private IEnumerator TransitionCoroutine(
        Vector3 targetPosition,
        Vector3 targetScale)
    {
        if (targetImage == null)
        {
            transitionCoroutine = null;
            yield break;
        }

        // 전환이 시작되는 순간의 실제 상태에서 출발합니다.
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

            float curvedTime =
                transitionCurve.Evaluate(normalizedTime);

            targetImage.localPosition = Vector3.LerpUnclamped(
                startPosition,
                targetPosition,
                curvedTime
            );

            targetImage.localScale = Vector3.LerpUnclamped(
                startScale,
                targetScale,
                curvedTime
            );

            yield return null;
        }

        targetImage.localPosition = targetPosition;
        targetImage.localScale = targetScale;

        transitionCoroutine = null;
    }

    private void OnDisable()
    {
        if (!resetOnDisable)
        {
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (initialized && targetImage != null && isHovering)
        {
            targetImage.localPosition = positionBeforeHover;
            targetImage.localScale = scaleBeforeHover;
        }

        if (hoverOnlyObject != null)
        {
            hoverOnlyObject.SetActive(false);
        }

        isHovering = false;
    }

    /// <summary>
    /// 현재 위치와 스케일을 새로운 기본 상태로 저장합니다.
    /// 호버 중이 아닐 때 호출하는 것을 권장합니다.
    /// </summary>
    public void RefreshOriginalTransform()
    {
        if (targetImage == null)
        {
            targetImage = transform;
        }

        positionBeforeHover = targetImage.localPosition;
        scaleBeforeHover = targetImage.localScale;

        initialized = true;
    }
}