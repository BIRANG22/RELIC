using UnityEngine;
using UnityEngine.UI;

public readonly struct MapLineLayout
{
    public Vector2 AnchoredPosition { get; }
    public Vector2 Size { get; }
    public float RotationDegrees { get; }

    public MapLineLayout(Vector2 anchoredPosition, Vector2 size, float rotationDegrees)
    {
        AnchoredPosition = anchoredPosition;
        Size = size;
        RotationDegrees = rotationDegrees;
    }
}

public class MapLineView : MonoBehaviour
{
    [SerializeField] private Image lineImage;
    [SerializeField] private Sprite lineSprite;
    [SerializeField, Min(0f)] private float thickness = 13f;
    [SerializeField, Min(0f)] private float endpointInset = 20f;

    public void Setup(Vector2 from, Vector2 to)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return;

        MapLineLayout layout = CalculateLayout(from, to, thickness, endpointInset);
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = layout.AnchoredPosition;
        rect.sizeDelta = layout.Size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, layout.RotationDegrees);

        if (lineImage == null) return;

        lineImage.sprite = lineSprite;
        lineImage.preserveAspect = false;
        lineImage.raycastTarget = false;
    }

    public static MapLineLayout CalculateLayout(
        Vector2 from,
        Vector2 to,
        float thickness,
        float endpointInset)
    {
        Vector2 direction = to - from;
        float trimmedDistance = Mathf.Max(0f, direction.magnitude - Mathf.Max(0f, endpointInset) * 2f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        return new MapLineLayout(
            (from + to) * 0.5f,
            new Vector2(trimmedDistance, Mathf.Max(0f, thickness)),
            angle);
    }
}
