using UnityEngine;

/// <summary>
/// Follows a world target on a UI canvas. When HUDSlot is present, HUDSlot owns
/// positioning and visibility so two LateUpdate methods never move the same RectTransform.
/// </summary>
public class WorldFollowHUD : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float colliderTopCanvasPadding = 4f;

    private Transform target;
    private Collider2D targetCollider2D;
    private Camera worldCamera;
    private RectTransform canvasRect;
    private Camera uiCamera;
    private System.Func<bool> runtimeAlive;
    private HUDSlot hudSlot;
    private CanvasGroup canvasGroup;

    public void Bind(
        Transform target,
        Camera worldCamera,
        RectTransform canvasRect,
        Camera uiCamera = null,
        Collider2D targetCollider2D = null)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        hudSlot = GetComponent<HUDSlot>();
        this.target = target;
        this.targetCollider2D = targetCollider2D;
        this.worldCamera = worldCamera;
        this.canvasRect = canvasRect;

        Canvas canvas = canvasRect != null
            ? canvasRect.GetComponentInParent<Canvas>()
            : null;
        this.uiCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : uiCamera;

        // Never reset anchoredPosition here: the old (0,0) reset could be rendered
        // for one frame and could fight HUDSlot's screen-space follow logic.
        if (hudSlot == null)
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            UpdatePosition();
        }
    }

    public void SetRuntimeAliveProvider(System.Func<bool> provider)
    {
        runtimeAlive = provider;
    }

    public static bool ShouldDisplay(bool targetActive, bool runtimeAlive) =>
        targetActive && runtimeAlive;

    private void OnEnable()
    {
        Canvas.willRenderCanvases -= SyncBeforeCanvasRender;
        Canvas.willRenderCanvases += SyncBeforeCanvasRender;
        if (hudSlot == null)
            hudSlot = GetComponent<HUDSlot>();
        if (hudSlot == null)
            SetCanvasVisible(false); // Only reveal once the target has a valid position.
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= SyncBeforeCanvasRender;
        if (hudSlot == null)
            SetCanvasVisible(false);
    }

    private void SyncBeforeCanvasRender()
    {
        if (!isActiveAndEnabled || hudSlot != null)
            return;
        UpdatePosition();
    }

    private void LateUpdate()
    {
        if (hudSlot != null)
        {
            // HUDSlot handles position and visibility. Only preserve the
            // dead-unit hide rule that WorldFollowHUD previously provided.
            if (runtimeAlive != null && !runtimeAlive())
                hudSlot.ForceHideImmediately();
            return;
        }

        UpdatePosition();
    }

    private Vector3 GetFollowWorldPosition()
    {
        if (targetCollider2D != null && targetCollider2D.enabled &&
            targetCollider2D.gameObject.activeInHierarchy)
        {
            Bounds bounds = targetCollider2D.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, target.position.z);
        }

        return target.position + worldOffset;
    }

    private void UpdatePosition()
    {
        if (rectTransform == null)
            return;

        bool targetActive = target != null && target.gameObject.activeInHierarchy;
        bool alive = runtimeAlive == null || runtimeAlive();
        if (!ShouldDisplay(targetActive, alive) || worldCamera == null || canvasRect == null)
        {
            SetCanvasVisible(false);
            return;
        }

        Vector3 screenPos = worldCamera.WorldToScreenPoint(GetFollowWorldPosition());
        if (screenPos.z <= 0f)
        {
            SetCanvasVisible(false);
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, uiCamera, out Vector2 localPos))
        {
            SetCanvasVisible(false);
            return;
        }

        if (targetCollider2D != null)
        {
            float pivotOffset = rectTransform.rect.height * rectTransform.pivot.y;
            localPos.y += pivotOffset + colliderTopCanvasPadding;
        }

        // Apply the new location BEFORE revealing the HUD.
        rectTransform.anchoredPosition = localPos;
        SetCanvasVisible(true);
    }

    private void SetCanvasVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
