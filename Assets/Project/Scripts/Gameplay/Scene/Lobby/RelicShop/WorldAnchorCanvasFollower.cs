using UnityEngine;

public sealed class WorldAnchorCanvasFollower : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private Canvas canvas;
    private Transform worldAnchor;
    private Camera worldCamera;
    private CanvasGroup canvasGroup;
    private Vector3 fixedWorldPosition;
    private bool useFixedWorldPosition;

    public void Initialize(Transform anchor, Canvas ownerCanvas, Camera camera)
    {
        rectTransform = transform as RectTransform;
        worldAnchor = anchor;
        canvas = ownerCanvas;
        canvasRect = ownerCanvas != null ? ownerCanvas.transform as RectTransform : null;
        worldCamera = camera;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void InitializeAtCanvasPosition(
        Vector2 canvasPosition,
        Transform referenceDepthAnchor,
        Canvas ownerCanvas,
        Camera camera)
    {
        Initialize(null, ownerCanvas, camera);
        if (rectTransform == null || canvasRect == null || worldCamera == null)
            return;

        rectTransform.anchoredPosition = canvasPosition;
        Vector3 canvasWorldPoint = canvasRect.TransformPoint(canvasPosition);
        Camera canvasCamera = ownerCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : ownerCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, canvasWorldPoint);
        float depth = referenceDepthAnchor != null
            ? worldCamera.WorldToScreenPoint(referenceDepthAnchor.position).z
            : Mathf.Max(1f, worldCamera.nearClipPlane + 1f);
        fixedWorldPosition = worldCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        useFixedWorldPosition = true;
    }

    private void LateUpdate()
    {
        if (rectTransform == null || canvasRect == null || worldCamera == null ||
            (!useFixedWorldPosition && worldAnchor == null))
            return;

        Vector3 worldPosition = useFixedWorldPosition ? fixedWorldPosition : worldAnchor.position;
        Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
        Vector2 localPoint = rectTransform.anchoredPosition;
        bool visible = screen.z > 0f &&
                       RectTransformUtility.ScreenPointToLocalPointInRectangle(
                           canvasRect,
                           screen,
                           canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                           out localPoint);

        rectTransform.anchoredPosition = localPoint;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
