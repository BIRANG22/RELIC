using UnityEngine;

public sealed class WorldAnchorCanvasFollower : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private Canvas canvas;
    private Transform worldAnchor;
    private Camera worldCamera;
    private CanvasGroup canvasGroup;

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

    private void LateUpdate()
    {
        if (rectTransform == null || canvasRect == null || worldAnchor == null || worldCamera == null)
            return;

        Vector3 screen = worldCamera.WorldToScreenPoint(worldAnchor.position);
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
