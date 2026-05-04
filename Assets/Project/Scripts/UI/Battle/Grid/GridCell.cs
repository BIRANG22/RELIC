using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class GridCell : MonoBehaviour
{
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private Renderer rend;
    private MaterialPropertyBlock propertyBlock;
    private bool selected;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        SetColor(normalColor);
    }

    private void OnMouseDown()
    {
        selected = !selected;
        SetColor(selected ? selectedColor : normalColor);

        Debug.Log($"[GridCell] Clicked: {name}");
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