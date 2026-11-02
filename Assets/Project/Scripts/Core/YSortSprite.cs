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
    private bool hasSortingReferenceY;
    private float sortingReferenceY;
    private SpriteRenderer[] shadowRenderers;

    public int sortingOrderOffset = 0;
    public float yMultiplier = 100f;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        CacheSpriteRootReferenceForIdleBack();
        CacheShadowRenderers();
    }

    void LateUpdate()
    {
        if (targetRenderer == null)
            return;

        if (ApplyIdleBackSorting())
            return;

        ApplySortingIfChanged();
        ApplyShadowSorting();
    }

    private int CalculateSortingOrder()
    {
        float y = hasSortingReferenceY ? sortingReferenceY : transform.position.y;

        if (useVirtualYOffset)
            y += virtualYOffset;

        return (int)(-y * yMultiplier) + sortingOrderOffset;
    }

    public void SetSortingReferenceY(float worldY)
    {
        hasSortingReferenceY = true;
        sortingReferenceY = worldY;
        hasAppliedSortingOrder = false;
    }

    public void ClearSortingReferenceY()
    {
        hasSortingReferenceY = false;
        hasAppliedSortingOrder = false;
    }

    public int CalculateCurrentSortingOrder()
    {
        return CalculateSortingOrder();
    }

    public void ApplySortingNow()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        if (shadowRenderers == null)
            CacheShadowRenderers();

        ApplySortingIfChanged();
        ApplyShadowSorting();
    }

    private void CacheShadowRenderers()
    {
        if (!string.Equals(name, SpriteRootName, System.StringComparison.OrdinalIgnoreCase) || transform.parent == null)
        {
            shadowRenderers = System.Array.Empty<SpriteRenderer>();
            return;
        }

        SpriteRenderer[] renderers = transform.parent.GetComponentsInChildren<SpriteRenderer>(true);
        var shadows = new System.Collections.Generic.List<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer != null && renderer.name.StartsWith("Shadow", System.StringComparison.OrdinalIgnoreCase))
                shadows.Add(renderer);
        }
        shadowRenderers = shadows.ToArray();
    }

    private void ApplyShadowSorting()
    {
        if (targetRenderer == null || shadowRenderers == null)
            return;

        int shadowOrder = targetRenderer.sortingOrder - 1;
        for (int i = 0; i < shadowRenderers.Length; i++)
        {
            SpriteRenderer shadow = shadowRenderers[i];
            if (shadow == null)
                continue;

            shadow.sortingLayerID = targetRenderer.sortingLayerID;
            shadow.sortingOrder = shadowOrder;
        }
    }

    private void ApplySortingIfChanged()
    {
        float positionY = hasSortingReferenceY ? sortingReferenceY : transform.position.y;
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
