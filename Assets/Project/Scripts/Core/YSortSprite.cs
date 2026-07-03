using UnityEngine;

public class YSortSprite : MonoBehaviour
{
    [SerializeField]
    private bool useVirtualYOffset;

    [SerializeField]
    private float virtualYOffset = 0f;

    private const string IdleBackName = "Idle_Back";
    private const string LegacyIdleBackName = "Idle_back";
    private const string SpriteRootName = "SpriteRoot";

    private Renderer targetRenderer;
    private Renderer spriteRootRenderer;
    private YSortSprite spriteRootSorter;

    public int sortingOrderOffset = 0;
    public float yMultiplier = 100f;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        CacheSpriteRootReferenceForIdleBack();
    }

    void LateUpdate()
    {
        if (targetRenderer == null)
            return;

        if (ApplyIdleBackSorting())
            return;

        targetRenderer.sortingOrder = CalculateSortingOrder();
    }

    private int CalculateSortingOrder()
    {
        float y = transform.position.y;

        if (useVirtualYOffset)
            y += virtualYOffset;

        return (int)(-y * yMultiplier) + sortingOrderOffset;
    }

    private bool ApplyIdleBackSorting()
    {
        if (spriteRootSorter == null || targetRenderer == null)
            return false;

        if (spriteRootRenderer != null)
            targetRenderer.sortingLayerID = spriteRootRenderer.sortingLayerID;

        targetRenderer.sortingOrder = spriteRootSorter.CalculateSortingOrder() - 1;
        return true;
    }

    private void CacheSpriteRootReferenceForIdleBack()
    {
        if (!IsIdleBackObject())
            return;

        Transform current = transform.parent;

        while (current != null)
        {
            Transform spriteRoot = current.Find(SpriteRootName);

            if (spriteRoot != null)
            {
                spriteRootSorter = spriteRoot.GetComponent<YSortSprite>();
                spriteRootRenderer = spriteRoot.GetComponent<Renderer>();
                return;
            }

            current = current.parent;
        }
    }

    private bool IsIdleBackObject()
    {
        return string.Equals(name, IdleBackName, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, LegacyIdleBackName, System.StringComparison.OrdinalIgnoreCase);
    }
}
