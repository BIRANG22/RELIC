using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class GridCell : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Index { get; private set; }

    private GridManager owner;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color previewColor = Color.cyan;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color rangePreviewColor = Color.red;

    private Renderer rend;
    private MaterialPropertyBlock propertyBlock;

    public void Initialize(GridManager gridManager, int x, int y, int index)
    {
        owner = gridManager;
        X = x;
        Y = y;
        Index = index;

        rend = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        SetNormal();
    }

    private void OnMouseDown()
    {
        Debug.Log($"[GridCell] Clicked: {name} / Index:{Index} / X:{X} / Y:{Y}");

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
        SetColor(normalColor);
    }

    public void SetPreview()
    {
        SetColor(previewColor);
    }

    public void SetSelected()
    {
        SetColor(selectedColor);
    }

    public void SetRangePreview()
    {
        SetColor(rangePreviewColor);
    }

    private void SetColor(Color color)
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        rend.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        rend.SetPropertyBlock(propertyBlock);
    }
}