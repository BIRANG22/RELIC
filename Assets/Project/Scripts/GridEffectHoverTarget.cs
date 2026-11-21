using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GridEffectHoverTarget : MonoBehaviour
{
    private const string FallbackColliderName = "TooltipHoverCollider";
    private static readonly List<GridEffectHoverTarget> HoveredTargets = new();
    private static ulong nextHoverOrder;

    [SerializeField] private string gridEffectId;

    private BoxCollider2D hoverCollider;
    private GridEffectTooltipUI tooltipUI;
    private Camera mainCamera;
    private bool isHovered;
    private bool warnedMissingCollider;
    private BattleGridEffectController gridEffectController;
    private int gridIndex = -1;
    private int lastDisplayedDuration = int.MinValue;
    private ulong hoverOrder;

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
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
        {
            SetHovered(false);
            return;
        }

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

        if (isHovered && GetPreferredHoveredTarget() == this)
        {
            EnsurePreferredTooltipVisible();

            if (tooltipUI != null && tooltipUI.IsOwnedBy(this))
            {
                RefreshTooltipIfDurationChanged();
                GetTooltipWorldSideAnchors(out Vector3 leftAnchor, out Vector3 rightAnchor);
                tooltipUI.SetWorldSideAnchors(leftAnchor, rightAnchor, mainCamera);
            }
        }
    }

    private void OnDisable()
    {
        SetHovered(false);
    }

    private void OnDestroy()
    {
        SetHovered(false);
    }

    private void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
            return;

        isHovered = hovered;

        if (!isHovered)
        {
            HoveredTargets.Remove(this);
            hoverOrder = 0;
            HideTooltip();

            GridEffectHoverTarget preferred = GetPreferredHoveredTarget();
            if (preferred != null)
                preferred.EnsurePreferredTooltipVisible();

            return;
        }

        HoveredTargets.Remove(this);
        hoverOrder = ++nextHoverOrder;
        HoveredTargets.Add(this);
        EnsurePreferredTooltipVisible();
    }

    private void EnsurePreferredTooltipVisible()
    {
        if (!isHovered || GetPreferredHoveredTarget() != this ||
            string.IsNullOrWhiteSpace(gridEffectId))
        {
            return;
        }

        if (tooltipUI == null)
            tooltipUI = GridEffectTooltipUI.GetOrCreate();

        if (tooltipUI != null && !tooltipUI.IsOwnedBy(this))
            ShowTooltip();
    }

    private static GridEffectHoverTarget GetPreferredHoveredTarget()
    {
        GridEffectHoverTarget preferred = null;
        ulong preferredOrder = 0;

        for (int i = HoveredTargets.Count - 1; i >= 0; i--)
        {
            GridEffectHoverTarget candidate = HoveredTargets[i];
            if (candidate == null || !candidate.isHovered)
            {
                HoveredTargets.RemoveAt(i);
                continue;
            }

            if (preferred == null || candidate.hoverOrder > preferredOrder)
            {
                preferred = candidate;
                preferredOrder = candidate.hoverOrder;
            }
        }

        return preferred;
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

        GetTooltipWorldSideAnchors(out Vector3 leftAnchor, out Vector3 rightAnchor);
        tooltipUI.ShowWorldAutoSide(this, data, leftAnchor, rightAnchor, mainCamera, remainingDuration);
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

    private void GetTooltipWorldSideAnchors(out Vector3 leftAnchor, out Vector3 rightAnchor)
    {
        if (hoverCollider != null)
        {
            Bounds bounds = hoverCollider.bounds;
            float z = transform.position.z;
            leftAnchor = new Vector3(bounds.min.x, bounds.center.y, z);
            rightAnchor = new Vector3(bounds.max.x, bounds.center.y, z);
            return;
        }

        leftAnchor = transform.position;
        rightAnchor = transform.position;
    }

    private void HideTooltip()
    {
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
    /// Reuses an authored collider as a hover trigger, including colliders disabled by
    /// placement preview interaction setup. Creates a fallback only when none exists.
    /// </summary>
    private void EnsureHoverCollider(Vector2? fallbackSize, bool warnWhenUnavailable)
    {
        if (FindExistingCollider())
        {
            hoverCollider.isTrigger = true;
            hoverCollider.enabled = true;
            return;
        }

        if (fallbackSize.HasValue &&
            fallbackSize.Value.x > 0f &&
            fallbackSize.Value.y > 0f)
        {
            GameObject fallbackObject = new(FallbackColliderName);
            fallbackObject.transform.SetParent(transform, false);

            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            fallbackObject.layer = ignoreRaycastLayer >= 0 ? ignoreRaycastLayer : gameObject.layer;

            hoverCollider = fallbackObject.AddComponent<BoxCollider2D>();
            hoverCollider.size = fallbackSize.Value;
            hoverCollider.isTrigger = true;
            warnedMissingCollider = false;
            return;
        }

        if (!warnWhenUnavailable || warnedMissingCollider)
            return;

        Debug.LogWarning(
            $"[GridEffectHoverTarget] '{name}'과 자식 오브젝트에 BoxCollider2D가 없습니다. " +
            "IndividualWorldRenderTexture VFX라면 원본 VFX 프리팹의 BoxCollider2D가 Proxy로 복사되는지 확인해주세요.",
            this);

        warnedMissingCollider = true;
    }

}
