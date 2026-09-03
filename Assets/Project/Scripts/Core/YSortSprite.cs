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
    private float lastPositionY;
    private float lastVirtualYOffset;
    private float lastYMultiplier;
    private int lastSortingOrderOffset;
    private int lastCalculatedSortingOrder;
    private bool lastUseVirtualYOffset;
    private bool hasAppliedSortingOrder;

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

        ApplySortingIfChanged();
    }

    private int CalculateSortingOrder()
    {
        float y = transform.position.y;

        if (useVirtualYOffset)
            y += virtualYOffset;

        return (int)(-y * yMultiplier) + sortingOrderOffset;
    }

    private void ApplySortingIfChanged()
    {
        float positionY = transform.position.y;
        if (hasAppliedSortingOrder &&
            Mathf.Approximately(lastPositionY, positionY) &&
            Mathf.Approximately(lastVirtualYOffset, virtualYOffset) &&
            Mathf.Approximately(lastYMultiplier, yMultiplier) &&
            lastSortingOrderOffset == sortingOrderOffset &&
            lastUseVirtualYOffset == useVirtualYOffset)
        {
            return;
        }

        int sortingOrder = CalculateSortingOrder();
        if (!hasAppliedSortingOrder || targetRenderer.sortingOrder != sortingOrder)
            targetRenderer.sortingOrder = sortingOrder;

        lastPositionY = positionY;
        lastVirtualYOffset = virtualYOffset;
        lastYMultiplier = yMultiplier;
        lastSortingOrderOffset = sortingOrderOffset;
        lastUseVirtualYOffset = useVirtualYOffset;
        lastCalculatedSortingOrder = sortingOrder;
        hasAppliedSortingOrder = true;
    }

    private bool ApplyIdleBackSorting()
    {
        if (spriteRootSorter == null || targetRenderer == null)
            return false;

        if (spriteRootRenderer != null)
            targetRenderer.sortingLayerID = spriteRootRenderer.sortingLayerID;

        int sortingOrder = spriteRootSorter.CalculateSortingOrder() - 1;
        if (!hasAppliedSortingOrder ||
            targetRenderer.sortingOrder != sortingOrder ||
            lastCalculatedSortingOrder != sortingOrder)
        {
            targetRenderer.sortingOrder = sortingOrder;
            lastCalculatedSortingOrder = sortingOrder;
            hasAppliedSortingOrder = true;
        }

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
