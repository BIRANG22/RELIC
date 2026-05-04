using UnityEngine;

public class FogSortingSetter : MonoBehaviour
{
    [Header("Fog Sorting")]
    [SerializeField] private string fogSortingLayerName = "Fog";
    [SerializeField] private int fogOrderInLayer = 0;

    private void Awake()
    {
        ApplySorting();
    }

    private void ApplySorting()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = fogSortingLayerName;
            spriteRenderer.sortingOrder = fogOrderInLayer;
        }

        ParticleSystemRenderer particleRenderer = GetComponent<ParticleSystemRenderer>();

        if (particleRenderer != null)
        {
            particleRenderer.sortingLayerName = fogSortingLayerName;
            particleRenderer.sortingOrder = fogOrderInLayer;
        }
    }
}