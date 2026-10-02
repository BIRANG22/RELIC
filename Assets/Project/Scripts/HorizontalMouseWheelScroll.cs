using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class HorizontalMouseWheelScroll : MonoBehaviour, IScrollHandler
{
    [Header("Mouse Wheel")]
    [Tooltip("마우스 휠 1칸당 가로 스크롤 이동량입니다.")]
    [SerializeField, Min(0.001f)] private float wheelStep = 0.12f;

    [Tooltip("체크하면 휠 방향을 반대로 사용합니다.")]
    [SerializeField] private bool invertDirection = false;

    [Header("Edge Snap")]
    [Tooltip("끝에 가까워지면 정확히 0 또는 1로 고정합니다.")]
    [SerializeField, Range(0f, 0.2f)] private float edgeSnapThreshold = 0.02f;

    private ScrollRect scrollRect;
    private float originalScrollSensitivity;
    private bool cachedSensitivity;

    private void Awake()
    {
        CacheScrollRect();
        DisableBuiltInWheelScroll();
    }

    private void OnEnable()
    {
        CacheScrollRect();
        DisableBuiltInWheelScroll();
    }

    private void OnDisable()
    {
        if (scrollRect != null && cachedSensitivity)
            scrollRect.scrollSensitivity = originalScrollSensitivity;
    }

    private void CacheScrollRect()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect != null && !cachedSensitivity)
        {
            originalScrollSensitivity = scrollRect.scrollSensitivity;
            cachedSensitivity = true;
        }
    }

    private void DisableBuiltInWheelScroll()
    {
        if (scrollRect == null)
            return;

        // ScrollRect 기본 마우스 휠 이동을 막아 중복 처리를 방지합니다.
        // 스크롤바 핸들 드래그와 콘텐츠 드래그에는 영향이 없습니다.
        scrollRect.scrollSensitivity = 0f;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect == null ||
            !scrollRect.IsActive() ||
            !scrollRect.horizontal ||
            scrollRect.content == null ||
            scrollRect.viewport == null)
        {
            return;
        }

        float wheel = eventData.scrollDelta.y;
        if (Mathf.Approximately(wheel, 0f))
            return;

        // 슬롯이 방금 추가된 프레임에도 실제 Content 크기를 먼저 확정합니다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        Canvas.ForceUpdateCanvases();

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;

        float direction = invertDirection ? -1f : 1f;

        // 휠 위  -> 왼쪽
        // 휠 아래 -> 오른쪽
        float next = scrollRect.horizontalNormalizedPosition
                     - (wheel * wheelStep * direction);

        next = Mathf.Clamp01(next);

        if (next <= edgeSnapThreshold)
            next = 0f;
        else if (next >= 1f - edgeSnapThreshold)
            next = 1f;

        scrollRect.horizontalNormalizedPosition = next;

        // 레이아웃 보정으로 같은 프레임에 다시 밀리는 현상을 방지합니다.
        Canvas.ForceUpdateCanvases();
        scrollRect.horizontalNormalizedPosition = next;
        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        wheelStep = Mathf.Max(0.001f, wheelStep);
        edgeSnapThreshold = Mathf.Clamp(edgeSnapThreshold, 0f, 0.2f);
    }
#endif
}
