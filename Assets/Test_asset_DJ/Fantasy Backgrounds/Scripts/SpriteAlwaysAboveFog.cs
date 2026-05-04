using UnityEngine;

public class SpriteAlwaysAboveFog : MonoBehaviour
{
    [Header("Sprite Sorting")]
    [SerializeField] private string sortingLayerName = "Character";
    [SerializeField] private int orderInLayer = 10;

    private void Awake()
    {
        ApplySorting();
    }

    private void ApplySorting()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = orderInLayer;
        }
    }
}