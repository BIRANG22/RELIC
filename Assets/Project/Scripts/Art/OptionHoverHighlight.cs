using UnityEngine;
using UnityEngine.UI;

public class OptionHoverHighlight : MonoBehaviour
{
    [SerializeField] private RectTransform highlightRect;
    [SerializeField] private bool matchWidth = true;
    [SerializeField] private bool matchHeight = true;
    [SerializeField] private Vector2 sizeOffset;
    [SerializeField] private Vector2 positionOffset;
    [SerializeField, Min(0f)] private float moveSpeed = 15f;
    [SerializeField] private bool hideOnExit = true;
    private RectTransform target;
    private Vector2? targetSizeOverride;
    private CanvasGroup group;
    private bool snap = true;

    private void Awake()
    {
        if (highlightRect == null) highlightRect = transform as RectTransform;
        if (highlightRect == null) return;
        group = highlightRect.GetComponent<CanvasGroup>();
        if (group == null) group = highlightRect.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        foreach (Graphic graphic in highlightRect.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        group.alpha = 0f;
    }

    private void OnDisable()
    {
        target = null;
        targetSizeOverride = null;
        snap = true;
        if (group != null) group.alpha = 0f;
    }

    public void MoveTo(RectTransform item) => MoveTo(item, null);

    public void MoveTo(RectTransform item, Vector2? sizeOverride)
    {
        if (item == null || highlightRect == null || group == null) return;
        snap = group.alpha <= 0f;
        target = item;
        targetSizeOverride = sizeOverride;
        UpdatePosition();
        group.alpha = 1f;
    }

    public void Clear(RectTransform item, bool force = false)
    {
        if (target != item || (!hideOnExit && !force)) return;
        target = null;
        targetSizeOverride = null;
        snap = true;
        if (group != null) group.alpha = 0f;
    }

    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            if (group != null) group.alpha = 0f;
            target = null;
            targetSizeOverride = null;
            snap = true;
            return;
        }
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (highlightRect == null || target == null) return;
        RectTransform parent = highlightRect.parent as RectTransform;
        if (parent == null) return;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 min = parent.InverseTransformPoint(corners[0]);
        Vector3 max = min;
        for (int i = 1; i < 4; i++)
        {
            Vector3 point = parent.InverseTransformPoint(corners[i]);
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        Vector2 size = highlightRect.rect.size;
        if (matchWidth) size.x = Mathf.Max(0f, max.x - min.x + sizeOffset.x);
        if (matchHeight) size.y = Mathf.Max(0f, max.y - min.y + sizeOffset.y);
        if (targetSizeOverride.HasValue) size = targetSizeOverride.Value;
        Vector2 center = (Vector2)((min + max) * 0.5f) + positionOffset;
        Vector2 pivotPosition = center + Vector2.Scale(size, highlightRect.pivot - Vector2.one * 0.5f);
        float t = snap || moveSpeed <= 0f ? 1f : 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);
        Vector2 nextSize = Vector2.Lerp(highlightRect.rect.size, size, t);
        highlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, nextSize.x);
        highlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nextSize.y);
        Vector3 local = highlightRect.localPosition;
        Vector2 next = Vector2.Lerp((Vector2)local, pivotPosition, t);
        highlightRect.localPosition = new Vector3(next.x, next.y, local.z);
        snap = false;
    }
}
