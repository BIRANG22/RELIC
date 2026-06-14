using UnityEngine;

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

    public void Bind(
        Transform target,
        Camera worldCamera,
        RectTransform canvasRect,
        Camera uiCamera = null,
        Collider2D targetCollider2D = null)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = Vector2.zero;

        this.target = target;
        this.targetCollider2D = targetCollider2D;
        this.worldCamera = worldCamera;
        this.canvasRect = canvasRect;

        Canvas canvas = canvasRect != null
            ? canvasRect.GetComponentInParent<Canvas>()
            : null;

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            this.uiCamera = null;
        else
            this.uiCamera = uiCamera;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private Vector3 GetFollowWorldPosition()
    {
        if (targetCollider2D != null)
        {
            Bounds bounds = targetCollider2D.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, target.position.z);
        }

        return target.position + worldOffset;
    }

    private void UpdatePosition()
    {
        if (target == null || worldCamera == null || canvasRect == null || rectTransform == null)
            return;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(GetFollowWorldPosition());

        if (screenPos.z < 0f)
        {
            rectTransform.gameObject.SetActive(false);
            return;
        }

        if (!rectTransform.gameObject.activeSelf)
            rectTransform.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCamera,
            out Vector2 localPos
        );

        if (targetCollider2D != null)
        {
            float pivotOffset = rectTransform.rect.height * rectTransform.pivot.y;
            localPos.y += pivotOffset + colliderTopCanvasPadding;
        }

        rectTransform.anchoredPosition = localPos;
    }
}