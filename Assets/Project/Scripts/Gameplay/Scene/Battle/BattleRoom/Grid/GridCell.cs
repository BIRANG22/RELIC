using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class GridCell : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Index { get; private set; }

    private GridManager owner;

    [Header("Highlight Object")]
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private Renderer highlightRenderer;

    [Header("Highlight Colors")]
    [SerializeField] private Color previewColor = Color.cyan;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color rangePreviewColor = Color.red;

    private MaterialPropertyBlock highlightPropertyBlock;

    public void Initialize(GridManager gridManager, int x, int y, int index)
    {
        owner = gridManager;
        X = x;
        Y = y;
        Index = index;

        AutoFindHighlightIfNeeded();

        highlightPropertyBlock = new MaterialPropertyBlock();

        SetNormal();
    }

    private void Awake()
    {
        AutoFindHighlightIfNeeded();
    }

    private void OnMouseDown()
    {
        if (owner != null)
            owner.NotifyCellClicked(this);
    }

    private void OnMouseEnter()
    {
        if (owner != null)
            owner.NotifyCellHovered(this);
    }

    private void OnMouseExit()
    {
        if (owner != null)
            owner.NotifyCellHoverExited(this);
    }

    public void SetNormal()
    {
        SetHighlightActive(false);
    }

    public void SetPreview()
    {
        SetHighlightColor(previewColor);
        SetHighlightActive(true);
    }

    public void SetSelected()
    {
        SetHighlightColor(selectedColor);
        SetHighlightActive(true);
    }

    public void SetRangePreview()
    {
        SetHighlightColor(rangePreviewColor);
        SetHighlightActive(true);
    }

    private void AutoFindHighlightIfNeeded()
    {
        if (highlightObject == null)
        {
            Transform found = transform.Find("Highlight");

            if (found != null)
                highlightObject = found.gameObject;
        }

        if (highlightRenderer == null && highlightObject != null)
            highlightRenderer = highlightObject.GetComponent<Renderer>();
    }

    private void SetHighlightActive(bool active)
    {
        if (highlightObject == null)
            return;

        if (highlightObject.activeSelf != active)
            highlightObject.SetActive(active);
    }

    private void SetHighlightColor(Color color)
    {
        if (highlightRenderer == null)
            return;

        if (highlightPropertyBlock == null)
            highlightPropertyBlock = new MaterialPropertyBlock();

        highlightRenderer.GetPropertyBlock(highlightPropertyBlock);

        highlightPropertyBlock.SetColor("_BaseColor", color);
        highlightPropertyBlock.SetColor("_Color", color);

        highlightRenderer.SetPropertyBlock(highlightPropertyBlock);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoFindHighlightIfNeeded();
    }
#endif
}