using TMPro;
using UnityEngine;

/// <summary>
/// 체력을 가진 그리드 효과의 현재 체력/최대 체력을 화면 UI로 표시합니다.
/// 표시 위치는 해당 그리드 효과에 직접 설정된 BoxCollider2D의 아래쪽을 기준으로 합니다.
/// </summary>
public class GridEffectHpUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private CanvasGroup canvasGroup;

    private BattleGridEffectController controller;
    private BoxCollider2D targetCollider;
    private Canvas targetCanvas;
    private RectTransform canvasRect;
    private int gridIndex = -1;
    private float worldYOffset;
    private int lastCurrentHp = int.MinValue;
    private int lastMaxHp = int.MinValue;

    public void Bind(
        BattleGridEffectController owner,
        int targetGridIndex,
        BoxCollider2D collider,
        Canvas canvas,
        float yOffset)
    {
        controller = owner;
        gridIndex = targetGridIndex;
        targetCollider = collider;
        targetCanvas = canvas;
        canvasRect = targetCanvas != null ? targetCanvas.transform as RectTransform : null;
        worldYOffset = yOffset;

        if (rootRect == null)
            rootRect = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        SetVisible(false);
        RefreshHp(true);
        RefreshPosition();
    }

    private void LateUpdate()
    {
        if (controller == null || targetCollider == null || targetCanvas == null || canvasRect == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!controller.TryGetEffectHitPoints(gridIndex, out _, out _))
        {
            Destroy(gameObject);
            return;
        }

        RefreshHp(false);
        RefreshPosition();
        RefreshHoverVisibility();
    }


    private void RefreshHoverVisibility()
    {
        if (targetCollider == null || !targetCollider.enabled)
        {
            SetVisible(false);
            return;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            SetVisible(false);
            return;
        }

        Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new(Vector3.forward, targetCollider.transform.position);

        if (!plane.Raycast(ray, out float enter))
        {
            SetVisible(false);
            return;
        }

        Vector3 worldPoint = ray.GetPoint(enter);
        bool hovered = targetCollider.OverlapPoint(new Vector2(worldPoint.x, worldPoint.y));
        SetVisible(hovered);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void RefreshHp(bool force)
    {
        if (controller == null || hpText == null)
            return;

        if (!controller.TryGetEffectHitPoints(gridIndex, out int currentHp, out int maxHp))
            return;

        if (!force && currentHp == lastCurrentHp && maxHp == lastMaxHp)
            return;

        lastCurrentHp = currentHp;
        lastMaxHp = maxHp;
        hpText.text = $"{Mathf.Max(0, currentHp)}/{Mathf.Max(0, maxHp)}";
    }

    private void RefreshPosition()
    {
        if (rootRect == null || targetCollider == null || targetCanvas == null || canvasRect == null)
            return;

        Bounds bounds = targetCollider.bounds;
        Vector3 worldPosition = new(
            bounds.center.x,
            bounds.min.y - worldYOffset,
            bounds.center.z);

        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera != null
                ? targetCanvas.worldCamera
                : Camera.main;

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0f)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            rootRect.anchoredPosition = localPoint;
        }
    }
}
