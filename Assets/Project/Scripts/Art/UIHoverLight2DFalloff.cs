using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class UIHoverLight2DFalloff : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Light 2D")]
    [Tooltip("Falloff Strength 값을 변경할 Light 2D입니다.")]
    [SerializeField] private Light2D targetLight;

    [Header("Falloff Strength")]
    [Tooltip("마우스를 호버하지 않았을 때 사용할 Falloff Strength 값입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float normalFalloffStrength = 1f;

    [Tooltip("마우스를 호버했을 때 도달할 Falloff Strength 값입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float hoverFalloffStrength = 0f;

    [Tooltip("두 Falloff Strength 값 사이를 변화하는 속도입니다.")]
    [Min(0f)]
    [SerializeField] private float changeSpeed = 3f;

    [Header("Hover Detection")]
    [Tooltip("UI 오브젝트라면 RectTransform 영역을 직접 검사하여 Raycast Target 설정과 관계없이 호버를 감지합니다.")]
    [SerializeField] private bool detectUIRectHover = true;

    [Header("Option")]
    [Tooltip("마우스가 벗어났을 때 시작 Falloff Strength 값으로 부드럽게 돌아갑니다.")]
    [SerializeField] private bool restoreOnPointerExit = true;

    [Tooltip("게임이 일시정지되어도 효과가 작동하도록 합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private float targetFalloffStrength;
    private bool isChanging;
    private bool isHovered;
    private bool wasRectHovered;

    private void Awake()
    {
        FindReferences();
        SetNormalValueImmediately();
    }

    private void OnEnable()
    {
        FindReferences();
        SetNormalValueImmediately();
        isHovered = false;
        wasRectHovered = false;
    }

    private void Update()
    {
        DetectUIHoverDirectly();
        UpdateFalloffStrength();
    }

    /// <summary>
    /// UI EventSystem을 통한 호버 진입입니다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverState(true);
    }

    /// <summary>
    /// UI EventSystem을 통한 호버 종료입니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverState(false);
    }

    /// <summary>
    /// Collider가 붙은 일반 오브젝트에서도 사용할 수 있도록 지원합니다.
    /// </summary>
    private void OnMouseEnter()
    {
        SetHoverState(true);
    }

    /// <summary>
    /// Collider가 붙은 일반 오브젝트에서도 사용할 수 있도록 지원합니다.
    /// </summary>
    private void OnMouseExit()
    {
        SetHoverState(false);
    }

    /// <summary>
    /// UI 오브젝트의 RectTransform 영역 안에 마우스가 있는지 직접 검사합니다.
    /// Graphic의 Raycast Target 여부와 관계없이 동작합니다.
    /// </summary>
    private void DetectUIHoverDirectly()
    {
        if (!detectUIRectHover || rectTransform == null)
        {
            return;
        }

        Camera uiCamera = GetUICamera();

        bool rectHovered = RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            Input.mousePosition,
            uiCamera
        );

        if (rectHovered == wasRectHovered)
        {
            return;
        }

        wasRectHovered = rectHovered;
        SetHoverState(rectHovered);
    }

    /// <summary>
    /// 현재 호버 상태에 따라 목표 Falloff Strength를 지정합니다.
    /// </summary>
    private void SetHoverState(bool hovered)
    {
        if (targetLight == null)
        {
            return;
        }

        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;

        if (hovered)
        {
            targetFalloffStrength = hoverFalloffStrength;
            isChanging = true;
            return;
        }

        if (restoreOnPointerExit)
        {
            targetFalloffStrength = normalFalloffStrength;
            isChanging = true;
        }
    }

    /// <summary>
    /// 현재 Falloff Strength를 목표값까지 부드럽게 이동시킵니다.
    /// </summary>
    private void UpdateFalloffStrength()
    {
        if (!isChanging || targetLight == null)
        {
            return;
        }

        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        targetLight.falloffIntensity = Mathf.MoveTowards(
            targetLight.falloffIntensity,
            targetFalloffStrength,
            changeSpeed * deltaTime
        );

        if (Mathf.Approximately(targetLight.falloffIntensity, targetFalloffStrength))
        {
            targetLight.falloffIntensity = targetFalloffStrength;
            isChanging = false;
        }
    }

    /// <summary>
    /// 필요한 참조를 찾습니다.
    /// </summary>
    private void FindReferences()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light2D>();
        }

        rectTransform = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Canvas 렌더 모드에 맞는 UI 카메라를 반환합니다.
    /// Screen Space - Overlay에서는 null을 사용합니다.
    /// </summary>
    private Camera GetUICamera()
    {
        if (parentCanvas == null)
        {
            return null;
        }

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return parentCanvas.worldCamera;
    }

    /// <summary>
    /// 시작 Falloff Strength 값을 즉시 적용합니다.
    /// </summary>
    public void SetNormalValueImmediately()
    {
        if (targetLight == null)
        {
            return;
        }

        targetLight.falloffIntensity = normalFalloffStrength;
        targetFalloffStrength = normalFalloffStrength;
        isChanging = false;
    }

    /// <summary>
    /// 호버 Falloff Strength 값을 즉시 적용합니다.
    /// </summary>
    public void SetHoverValueImmediately()
    {
        if (targetLight == null)
        {
            return;
        }

        targetLight.falloffIntensity = hoverFalloffStrength;
        targetFalloffStrength = hoverFalloffStrength;
        isChanging = false;
    }
}
