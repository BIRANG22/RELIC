using UnityEngine;

[DisallowMultipleComponent]
public class GridEffectHoverTarget : MonoBehaviour
{
    [SerializeField] private string gridEffectId;

    private BoxCollider2D hoverCollider;
    private GridEffectTooltipUI tooltipUI;
    private Camera mainCamera;
    private bool isHovered;
    private bool warnedMissingCollider;

    public static GridEffectHoverTarget Attach(
        GameObject target,
        string effectId,
        Vector2? fallbackSize = null)
    {
        if (target == null)
            return null;

        GridEffectHoverTarget hover = target.GetComponent<GridEffectHoverTarget>();
        if (hover == null)
            hover = target.AddComponent<GridEffectHoverTarget>();

        hover.Bind(effectId);
        return hover;
    }

    public void Bind(string effectId)
    {
        gridEffectId = effectId?.Trim() ?? string.Empty;
        FindManualCollider();
    }

    private void Awake()
    {
        FindManualCollider();
    }

    private void OnEnable()
    {
        FindManualCollider();
        isHovered = false;
    }

    private void Update()
    {
        if (string.IsNullOrWhiteSpace(gridEffectId))
        {
            SetHovered(false);
            return;
        }

        if (hoverCollider == null)
            FindManualCollider();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || hoverCollider == null || !hoverCollider.enabled)
        {
            SetHovered(false);
            return;
        }

        bool hovered = TryGetMouseWorldPoint(out Vector2 mouseWorldPoint) &&
                       hoverCollider.OverlapPoint(mouseWorldPoint);

        SetHovered(hovered);

        if (isHovered && tooltipUI != null)
            tooltipUI.SetPosition(Input.mousePosition);
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    private void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
            return;

        isHovered = hovered;

        if (!isHovered)
        {
            HideTooltip();
            return;
        }

        tooltipUI = GridEffectTooltipUI.GetOrCreate();
        tooltipUI?.Show(this, gridEffectId, Input.mousePosition);
    }

    private void HideTooltip()
    {
        isHovered = false;

        if (tooltipUI != null)
            tooltipUI.Hide(this);
    }

    private bool TryGetMouseWorldPoint(out Vector2 worldPoint)
    {
        worldPoint = default;

        if (mainCamera == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new(Vector3.forward, transform.position);

        if (!plane.Raycast(ray, out float enter))
            return false;

        Vector3 point = ray.GetPoint(enter);
        worldPoint = new Vector2(point.x, point.y);
        return true;
    }

    /// <summary>
    /// 프리팹에 사용자가 직접 설정한 BoxCollider2D를 찾습니다.
    /// 콜라이더를 자동 생성하거나 Size / Offset 값을 변경하지 않습니다.
    /// </summary>
    private void FindManualCollider()
    {
        hoverCollider = GetComponent<BoxCollider2D>();

        if (hoverCollider != null)
        {
            warnedMissingCollider = false;
            return;
        }

        if (!warnedMissingCollider)
        {
            Debug.LogWarning(
                $"[GridEffectHoverTarget] '{name}'에 BoxCollider2D가 없습니다. " +
                "그리드 효과 호버 범위로 사용할 BoxCollider2D를 프리팹에 직접 추가하고 Size / Offset을 설정해주세요.",
                this);

            warnedMissingCollider = true;
        }
    }
}
