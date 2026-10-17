using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Arranges SpawnGridCell UI objects into a perspective quad without world meshes.
/// Column-first, top-to-bottom order: Grid01..05, Grid06..10, Grid11..15.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SpawnGridUIWarpController : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField, Min(1)] private int width = 3;
    [SerializeField, Min(1)] private int height = 5;

    [Header("Board Corners - Grid Root Local UI Coordinates")]
    [SerializeField] private Vector2 bottomLeft = new Vector2(-300f, -250f);
    [SerializeField] private Vector2 bottomRight = new Vector2(300f, -250f);
    [SerializeField] private Vector2 topLeft = new Vector2(-300f, 250f);
    [SerializeField] private Vector2 topRight = new Vector2(300f, 250f);

    [Header("Cell Spacing (UI Units)")]
    [Tooltip("Horizontal distance between adjacent cell edges. The grid's four outer corners stay fixed.")]
    [SerializeField, Min(0f)] private float horizontalSpacing = 0f;
    [Tooltip("Vertical distance between adjacent cell edges. The grid's four outer corners stay fixed.")]
    [SerializeField, Min(0f)] private float verticalSpacing = 0f;

    [Header("SpawnGridCell Order (Optional)")]
    [Tooltip("Leave empty to find SpawnGridCell children in hierarchy order. Each column is top-to-bottom, starting with the left column.")]
    [SerializeField] private SpawnGridCell[] cells;

    [Header("Visuals")]
    [SerializeField] private bool warpBack = true;
    [SerializeField] private bool warpSelect = true;
    [SerializeField] private bool warpLine = true;
    [SerializeField, Range(0f, 0.35f)] private float visualInset = 0f;
    [Tooltip("Only arrange Char position at the cell center; never warp its image.")]
    [SerializeField] private bool centerCharacter = false;
    [SerializeField] private Vector2 characterOffset;

    [Header("Editor")]
    [SerializeField] private bool previewInEditMode = true;

#if UNITY_EDITOR
    private bool editorApplyQueued;
    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);
        if (!previewInEditMode || Application.isPlaying || editorApplyQueued) return;
        editorApplyQueued = true;
        EditorApplication.delayCall += DelayedApply;
    }
    private void DelayedApply()
    {
        editorApplyQueued = false;
        if (this == null || Application.isPlaying || !previewInEditMode) return;
        ApplyLayout();
    }
#endif

    private void Awake()
    {
        if (Application.isPlaying) ApplyLayout();
    }

    [ContextMenu("Apply UI Grid Layout")]
    public void ApplyLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            Debug.LogWarning("[SpawnGridUIWarpController] Attach to a UI RectTransform grid root.", this);
            return;
        }

        SpawnGridCell[] targets = cells;
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<SpawnGridCell>(true);

        int count = Mathf.Min(width * height, targets.Length);
        for (int i = 0; i < count; i++)
        {
            SpawnGridCell cell = targets[i];
            if (cell == null) continue;
            RectTransform rect = cell.transform as RectTransform;
            if (rect == null || rect.parent != root) // coordinates must be relative to this grid root
            {
                Debug.LogWarning("[SpawnGridUIWarpController] Cells must be direct RectTransform children of the grid root: " + cell.name, cell);
                continue;
            }

            int column = i / height;
            int rowFromBottom = height - 1 - (i % height);
            Vector2 bl = Point(column, rowFromBottom);
            Vector2 br = Point(column + 1, rowFromBottom);
            Vector2 tl = Point(column, rowFromBottom + 1);
            Vector2 tr = Point(column + 1, rowFromBottom + 1);
            // Pull the four cell corners inward independently along each grid direction.
            // This produces a real gap between the RectTransforms, not just a visual inset.
            // Cap each gap against the shortest corresponding edge to avoid inverted cells.
            float minWidth = Mathf.Min((br - bl).magnitude, (tr - tl).magnitude);
            float minHeight = Mathf.Min((tl - bl).magnitude, (tr - br).magnitude);
            float halfGapX = Mathf.Min(horizontalSpacing, Mathf.Max(0f, minWidth - 1f)) * 0.5f;
            float halfGapY = Mathf.Min(verticalSpacing, Mathf.Max(0f, minHeight - 1f)) * 0.5f;
            Vector2 rightBottom = (br - bl).normalized * halfGapX;
            Vector2 rightTop = (tr - tl).normalized * halfGapX;
            Vector2 upLeft = (tl - bl).normalized * halfGapY;
            Vector2 upRight = (tr - br).normalized * halfGapY;
            bl += rightBottom + upLeft;
            br += -rightBottom + upRight;
            tl += rightTop - upLeft;
            tr += -rightTop - upRight;

            Vector2 center = (bl + br + tl + tr) * 0.25f;

            // Fixed anchors ensure anchoredPosition matches coordinates in the root space.
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = center - root.rect.center;
            rect.sizeDelta = new Vector2(
                Mathf.Max(1f, Mathf.Max(Mathf.Abs(bl.x - center.x), Mathf.Abs(br.x - center.x), Mathf.Abs(tl.x - center.x), Mathf.Abs(tr.x - center.x)) * 2f),
                Mathf.Max(1f, Mathf.Max(Mathf.Abs(bl.y - center.y), Mathf.Abs(br.y - center.y), Mathf.Abs(tl.y - center.y), Mathf.Abs(tr.y - center.y)) * 2f));

            if (warpBack) WarpVisual(rect, "Back", bl, br, tl, tr, center);
            if (warpSelect) WarpVisual(rect, "Select", bl, br, tl, tr, center);
            if (warpLine) WarpVisual(rect, "Line", bl, br, tl, tr, center);

            if (centerCharacter)
            {
                Transform charTransform = rect.Find("Char");
                RectTransform character = charTransform as RectTransform;
                if (character != null)
                {
                    character.anchorMin = character.anchorMax = new Vector2(0.5f, 0.5f);
                    character.anchoredPosition = characterOffset;
                }
            }
        }
    }

    private Vector2 Point(int x, int y)
    {
        float u = x / (float)width;
        float v = y / (float)height;
        return Vector2.LerpUnclamped(Vector2.LerpUnclamped(bottomLeft, topLeft, v),
                                      Vector2.LerpUnclamped(bottomRight, topRight, v), u);
    }

    private void WarpVisual(RectTransform cell, string childName, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, Vector2 center)
    {
        Transform child = cell.Find(childName);
        if (child == null) return;
        Graphic graphic = child.GetComponent<Graphic>();
        if (graphic == null) return;
        UIGridQuadMeshEffect effect = child.GetComponent<UIGridQuadMeshEffect>();
        if (effect == null) effect = child.gameObject.AddComponent<UIGridQuadMeshEffect>();

        // The inset is measured toward each cell's center, preserving the perspective.
        bl = Vector2.Lerp(bl, center, visualInset);
        br = Vector2.Lerp(br, center, visualInset);
        tl = Vector2.Lerp(tl, center, visualInset);
        tr = Vector2.Lerp(tr, center, visualInset);
        Vector3 TransformCorner(Vector2 p) =>
            child.InverseTransformPoint(transform.TransformPoint(new Vector3(p.x, p.y, 0f)));
        effect.SetQuad(TransformCorner(bl), TransformCorner(br), TransformCorner(tl), TransformCorner(tr));
    }
}
