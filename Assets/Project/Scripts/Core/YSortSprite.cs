using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortSprite : MonoBehaviour
{
    private const string IdleBackName = "Idle_Back";
    private const string LegacyIdleBackName = "Idle_back";
    private const string SpriteRootName = "SpriteRoot";

    private SpriteRenderer sr;
    private SpriteRenderer spriteRootRenderer;
    private YSortSprite spriteRootSorter;

    public int sortingOrderOffset = 0;
    public float yMultiplier = 100f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        CacheSpriteRootReferenceForIdleBack();
    }

    void LateUpdate()
    {
        if (ApplyIdleBackSorting())
            return;

        sr.sortingOrder = CalculateSortingOrder();
    }

    private int CalculateSortingOrder()
    {
        return (int)(-transform.position.y * yMultiplier) + sortingOrderOffset;
    }

    private bool ApplyIdleBackSorting()
    {
        if (spriteRootSorter == null)
            return false;

        if (spriteRootRenderer != null)
            sr.sortingLayerID = spriteRootRenderer.sortingLayerID;

        sr.sortingOrder = spriteRootSorter.CalculateSortingOrder() - 1;
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
                spriteRootRenderer = spriteRoot.GetComponent<SpriteRenderer>();
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
