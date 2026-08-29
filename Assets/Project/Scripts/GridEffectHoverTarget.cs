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
    private BattleGridEffectController gridEffectController;
    private int gridIndex = -1;
    private int lastDisplayedDuration = int.MinValue;

    public static GridEffectHoverTarget Attach(
        GameObject target,
        string effectId,
        Vector2? fallbackSize = null,
        BattleGridEffectController controller = null,
        int targetGridIndex = -1)
    {
        if (target == null)
            return null;

        GridEffectHoverTarget hover = target.GetComponent<GridEffectHoverTarget>();
        if (hover == null)
            hover = target.AddComponent<GridEffectHoverTarget>();

        if (hover == null)
        {
            Debug.LogWarning(
                $"[GridEffectHoverTarget] '{target.name}'에 GridEffectHoverTarget을 추가하지 못했습니다.",
                target);
            return null;
        }

        hover.Initialize(effectId, fallbackSize, controller, targetGridIndex);
        return hover;
    }

    public void Bind(string effectId)
    {
        gridEffectId = effectId?.Trim() ?? string.Empty;
        EnsureHoverCollider(null, true);
    }

    private void Initialize(
        string effectId,
        Vector2? fallbackSize,
        BattleGridEffectController controller,
        int targetGridIndex)
    {
        gridEffectId = effectId?.Trim() ?? string.Empty;
        gridEffectController = controller;
        gridIndex = targetGridIndex;
        lastDisplayedDuration = int.MinValue;
        EnsureHoverCollider(fallbackSize, true);
    }

    private void Awake()
    {
        // AddComponent 직후 Awake가 먼저 호출될 수 있으므로 여기서는 경고하거나
        // fallback 콜라이더를 만들지 않습니다. Attach.Initialize에서 초기화합니다.
        FindExistingCollider();
    }

    private void OnEnable()
    {
        FindExistingCollider();
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
            FindExistingCollider();

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
        {
            RefreshTooltipIfDurationChanged();
            tooltipUI.SetPosition(Input.mousePosition);
        }
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
        ShowTooltip();
    }

    private void ShowTooltip()
    {
        if (tooltipUI == null || string.IsNullOrWhiteSpace(gridEffectId))
            return;

        Relic.Gameplay.Data.GridEffectDatabase database = DataManager.Instance?.GridEffectDatabase;
        if (database == null ||
            !database.TryGet(gridEffectId.Trim(), out Relic.Gameplay.Data.GridEffectData data) ||
            data == null)
        {
            tooltipUI.Hide(this);
            return;
        }

        int? remainingDuration = null;
        if (TryGetRemainingDuration(out int currentDuration))
        {
            remainingDuration = currentDuration;
            lastDisplayedDuration = currentDuration;
        }
        else
        {
            lastDisplayedDuration = int.MinValue;
        }

        tooltipUI.Show(this, data, Input.mousePosition, remainingDuration);
    }

    private void RefreshTooltipIfDurationChanged()
    {
        if (!TryGetRemainingDuration(out int currentDuration))
            return;

        if (currentDuration == lastDisplayedDuration)
            return;

        ShowTooltip();
    }

    private bool TryGetRemainingDuration(out int remainingDuration)
    {
        remainingDuration = 0;

        return gridEffectController != null &&
               gridIndex >= 0 &&
               gridEffectController.State != null &&
               gridEffectController.State.TryGetRemainingDuration(gridIndex, out remainingDuration);
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
    /// 루트 또는 자식 프리팹에 사용자가 직접 설정한 BoxCollider2D를 찾습니다.
    /// </summary>
    private bool FindExistingCollider()
    {
        if (hoverCollider != null)
            return true;

        hoverCollider = GetComponent<BoxCollider2D>();

        if (hoverCollider == null)
            hoverCollider = GetComponentInChildren<BoxCollider2D>(true);

        if (hoverCollider != null)
        {
            warnedMissingCollider = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 호버 판정은 프리팹 또는 BattleWorldVfxRenderer가 Proxy에 복사한
    /// BoxCollider2D만 사용합니다. GridEffectHoverTarget 자체에서는 임시 콜라이더를 만들지 않습니다.
    /// </summary>
    private void EnsureHoverCollider(Vector2? fallbackSize, bool warnWhenUnavailable)
    {
        if (FindExistingCollider())
            return;

        if (!warnWhenUnavailable || warnedMissingCollider)
            return;

        Debug.LogWarning(
            $"[GridEffectHoverTarget] '{name}'과 자식 오브젝트에 BoxCollider2D가 없습니다. " +
            "IndividualWorldRenderTexture VFX라면 원본 VFX 프리팹의 BoxCollider2D가 Proxy로 복사되는지 확인해주세요.",
            this);

        warnedMissingCollider = true;
    }

}
