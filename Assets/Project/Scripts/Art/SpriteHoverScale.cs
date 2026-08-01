using System.Collections;
using UnityEngine;

/// <summary>
/// 지정한 마우스 감지 영역에 마우스를 올렸을 때
/// 대상 오브젝트의 위치와 스케일을 부드럽게 변경합니다.
///
/// Hover Only Objects는 마우스 호버 중이거나,
/// 연결된 LobbyPanelTransitionButton의 패널이 열려 있는 동안
/// 계속 활성화됩니다.
/// </summary>
public class SpriteHoverScale : MonoBehaviour
{
    [Header("마우스 감지 영역")]
    [Tooltip("마우스 호버를 감지할 오브젝트입니다. 비워두면 이 스크립트가 붙은 오브젝트를 사용합니다. Collider 또는 Collider2D가 필요합니다.")]
    [SerializeField] private GameObject hoverDetectionObject;

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
    [Tooltip("호버 중이거나 연결된 패널이 열려 있을 때 켜질 오브젝트들입니다.")]
    [SerializeField] private GameObject[] hoverOnlyObjects;

    [Header("패널 선택 표시 유지")]
    [Tooltip("연결된 버튼의 대상 패널이 열려 있으면 호버 표시 오브젝트를 계속 활성화합니다.")]
    [SerializeField] private bool keepHoverObjectsWhilePanelOpen = true;

    [Tooltip("비워두면 같은 오브젝트에서 LobbyPanelTransitionButton을 자동으로 찾습니다.")]
    [SerializeField]
    private LobbyPanelTransitionButton panelTransitionButton;

    [Header("복구 설정")]
    [Tooltip("이 컴포넌트가 비활성화될 때 원래 상태로 복구합니다.")]
    [SerializeField] private bool resetOnDisable = true;

    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;

    private Coroutine transitionCoroutine;
    private SpriteHoverAreaRelay hoverAreaRelay;

    private bool initialized;
    private bool isHovering;
    private bool wasTargetPanelOpen;

    private void Awake()
    {
        Initialize();
        BindHoverDetectionObject();
        ResolvePanelTransitionButton();

        wasTargetPanelOpen = IsTargetPanelOpen();
        RefreshHoverOnlyObjects();
    }

    private void OnEnable()
    {
        if (initialized)
            BindHoverDetectionObject();

        ResolvePanelTransitionButton();

        wasTargetPanelOpen = IsTargetPanelOpen();
        RefreshHoverOnlyObjects();
    }

    private void Initialize()
    {
        if (targetImage == null)
            targetImage = transform;

        originalLocalPosition = targetImage.localPosition;
        originalLocalScale = targetImage.localScale;

        initialized = true;
    }

    /// <summary>
    /// 지정한 감지 오브젝트에 마우스 이벤트 전달 컴포넌트를 연결합니다.
    /// </summary>
    private void BindHoverDetectionObject()
    {
        GameObject detectionObject =
            hoverDetectionObject != null
                ? hoverDetectionObject
                : gameObject;

        if (hoverAreaRelay != null &&
            hoverAreaRelay.gameObject != detectionObject)
        {
            hoverAreaRelay.RemoveOwner(this);
            hoverAreaRelay = null;
        }

        hoverAreaRelay =
            detectionObject.GetComponent<SpriteHoverAreaRelay>();

        if (hoverAreaRelay == null)
        {
            hoverAreaRelay =
                detectionObject.AddComponent<SpriteHoverAreaRelay>();
        }

        hoverAreaRelay.SetOwner(this);
    }

    /// <summary>
    /// 감지 영역에 마우스가 들어왔을 때 호출됩니다.
    /// </summary>
    public void HandleHoverEnter()
    {
        if (!isActiveAndEnabled)
            return;

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

        Vector3 hoverTargetPosition =
            originalLocalPosition + hoverPositionOffset;

        Vector3 hoverTargetScale =
            originalLocalScale * hoverScaleMultiplier;

        StartTransition(
            hoverTargetPosition,
            hoverTargetScale);

        RefreshHoverOnlyObjects();
    }

    /// <summary>
    /// 감지 영역에서 마우스가 나갔을 때 호출됩니다.
    /// </summary>
    public void HandleHoverExit()
    {
        if (!isHovering)
            return;

        isHovering = false;

        // 위치와 크기는 원래 상태로 돌아갑니다.
        StartTransition(
            originalLocalPosition,
            originalLocalScale);

        // 패널이 열려 있다면 Hover Only Objects는 꺼지지 않습니다.
        RefreshHoverOnlyObjects();
    }

    private void Update()
    {
        // 메뉴가 열리면 월드 오브젝트의 크기 및 위치 호버 효과는 해제합니다.
        // 단, 해당 버튼의 패널이 열려 있으면 표시 오브젝트는 유지됩니다.
        if (isHovering && UIPanelButton.IsMenuPanelOpen)
            ResetHoverImmediate();

        bool isTargetPanelOpen = IsTargetPanelOpen();

        // 패널의 활성 상태가 변경되었을 때만 표시 상태를 갱신합니다.
        if (wasTargetPanelOpen != isTargetPanelOpen)
        {
            wasTargetPanelOpen = isTargetPanelOpen;
            RefreshHoverOnlyObjects();
        }
    }

    private void StartTransition(
        Vector3 targetPosition,
        Vector3 targetScale)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(
            TransitionCoroutine(
                targetPosition,
                targetScale));
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
                Mathf.Clamp01(
                    elapsedTime / transitionDuration);

            float curvedTime =
                transitionCurve.Evaluate(normalizedTime);

            targetImage.localPosition =
                Vector3.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    curvedTime);

            targetImage.localScale =
                Vector3.LerpUnclamped(
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
    /// 연결할 LobbyPanelTransitionButton을 찾습니다.
    /// </summary>
    private void ResolvePanelTransitionButton()
    {
        if (panelTransitionButton != null)
            return;

        // 먼저 현재 오브젝트에서 찾습니다.
        panelTransitionButton =
            GetComponent<LobbyPanelTransitionButton>();

        if (panelTransitionButton != null)
            return;

        // 현재 오브젝트에 없으면 부모에서 찾습니다.
        panelTransitionButton =
            GetComponentInParent<LobbyPanelTransitionButton>();
    }

    /// <summary>
    /// 연결된 버튼의 대상 패널이 열려 있는지 확인합니다.
    /// </summary>
    private bool IsTargetPanelOpen()
    {
        if (!keepHoverObjectsWhilePanelOpen)
            return false;

        ResolvePanelTransitionButton();

        return panelTransitionButton != null &&
               panelTransitionButton.IsTargetPanelOpen;
    }

    /// <summary>
    /// 현재 호버 상태와 패널 상태를 함께 확인해
    /// 호버 표시 오브젝트를 갱신합니다.
    /// </summary>
    private void RefreshHoverOnlyObjects()
    {
        bool shouldShow =
            isHovering || IsTargetPanelOpen();

        SetHoverOnlyObjectsActive(shouldShow);
    }

    private void SetHoverOnlyObjectsActive(bool active)
    {
        if (hoverOnlyObjects == null)
            return;

        for (int i = 0; i < hoverOnlyObjects.Length; i++)
        {
            GameObject hoverObject = hoverOnlyObjects[i];

            if (hoverObject != null &&
                hoverObject.activeSelf != active)
            {
                hoverObject.SetActive(active);
            }
        }
    }

    private void ResetHoverImmediate()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (initialized && targetImage != null)
        {
            targetImage.localPosition =
                originalLocalPosition;

            targetImage.localScale =
                originalLocalScale;
        }

        isHovering = false;

        // 패널이 열려 있다면 표시 오브젝트는 유지됩니다.
        RefreshHoverOnlyObjects();
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (resetOnDisable &&
            initialized &&
            targetImage != null)
        {
            targetImage.localPosition =
                originalLocalPosition;

            targetImage.localScale =
                originalLocalScale;
        }

        isHovering = false;

        // 컴포넌트 또는 버튼 자체가 비활성화될 때는 표시를 끕니다.
        SetHoverOnlyObjectsActive(false);
    }

    private void OnDestroy()
    {
        if (hoverAreaRelay != null)
            hoverAreaRelay.RemoveOwner(this);
    }

    /// <summary>
    /// 인스펙터에서 감지 오브젝트를 변경한 뒤
    /// 연결을 새로 갱신할 때 호출합니다.
    /// </summary>
    public void RefreshHoverDetectionObject()
    {
        BindHoverDetectionObject();
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

        originalLocalPosition =
            targetImage.localPosition;

        originalLocalScale =
            targetImage.localScale;

        initialized = true;
        isHovering = false;

        RefreshHoverOnlyObjects();
    }
}

/// <summary>
/// 지정된 감지 영역의 OnMouseEnter와 OnMouseExit 이벤트를
/// SpriteHoverScale로 전달하는 내부용 컴포넌트입니다.
/// </summary>
public class SpriteHoverAreaRelay : MonoBehaviour
{
    private SpriteHoverScale owner;

    public void SetOwner(SpriteHoverScale newOwner)
    {
        owner = newOwner;
    }

    public void RemoveOwner(SpriteHoverScale currentOwner)
    {
        if (owner == currentOwner)
            owner = null;
    }

    private void OnMouseEnter()
    {
        if (owner != null)
            owner.HandleHoverEnter();
    }

    private void OnMouseExit()
    {
        if (owner != null)
            owner.HandleHoverExit();
    }

    private void OnDisable()
    {
        if (owner != null)
            owner.HandleHoverExit();
    }
}