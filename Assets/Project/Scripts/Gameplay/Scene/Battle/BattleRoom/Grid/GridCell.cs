using System.Collections.Generic;
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

    [Header("Sorting")]
    [SerializeField] private bool forceBackSorting = true;
    [SerializeField] private string backSortingLayerName = "Empty";
    [SerializeField] private int backSortingOrder = -1000;
    [SerializeField] private bool forceHighlightBackRenderQueue = true;
    [SerializeField] private int highlightBackRenderQueue = 2400;

    private MaterialPropertyBlock highlightPropertyBlock;
    private Material defaultHighlightMaterial;
    private Renderer[] baseRenderers;
    private bool executionRangeTintActive;
    private readonly List<ExecutionRendererState> executionRendererStates = new();
    private readonly Dictionary<Material, Material> highlightBackMaterials = new();

    private sealed class ExecutionRendererState
    {
        public Renderer Renderer;
        public bool Enabled;
        public MaterialPropertyBlock PropertyBlock;
    }

    public void Initialize(GridManager gridManager, int x, int y, int index)
    {
        owner = gridManager;
        X = x;
        Y = y;
        Index = index;

        AutoFindHighlightIfNeeded();
        CacheDefaultHighlightMaterial();
        CacheBaseRenderers();
        ApplyBackSorting();

        highlightPropertyBlock = new MaterialPropertyBlock();

        SetNormal();
    }

    private void Awake()
    {
        AutoFindHighlightIfNeeded();
        CacheDefaultHighlightMaterial();
        CacheBaseRenderers();
        ApplyBackSorting();
    }

    private void OnDestroy()
    {
        foreach (Material material in highlightBackMaterials.Values)
        {
            if (material == null)
                continue;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }

        highlightBackMaterials.Clear();
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
        RestoreDefaultHighlightMaterial();
        SetHighlightActive(false);
    }

    public void SetPreview()
    {
        SetPreview(previewColor);
    }

    public void SetPreview(Color color)
    {
        SetPreview(color, null);
    }

    public void SetPreview(Color color, Material materialOverride)
    {
        SetHighlightMaterial(materialOverride);
        SetHighlightColor(color);
        SetHighlightActive(true);
    }

    public void SetSelected()
    {
        SetHighlightMaterial(null);
        SetHighlightColor(selectedColor);
        SetHighlightActive(true);
    }

    public void SetRangePreview()
    {
        SetRangePreview(rangePreviewColor);
    }

    public void SetRangePreview(Color color)
    {
        SetHighlightMaterial(null);
        SetHighlightColor(color);
        SetHighlightActive(true);
    }

    public void SetExecutionRangeTint(Color color)
    {
        ClearExecutionRangeTint();
        CacheBaseRenderers();
        ApplyBackSorting();

        if (baseRenderers == null || baseRenderers.Length == 0)
            return;

        color.a = 1f;

        for (int i = 0; i < baseRenderers.Length; i++)
        {
            Renderer renderer = baseRenderers[i];

            if (renderer == null)
                continue;

            MaterialPropertyBlock originalBlock = new();
            renderer.GetPropertyBlock(originalBlock);

            executionRendererStates.Add(new ExecutionRendererState
            {
                Renderer = renderer,
                Enabled = renderer.enabled,
                PropertyBlock = originalBlock
            });

            MaterialPropertyBlock tintedBlock = new();
            renderer.GetPropertyBlock(tintedBlock);
            SetTintProperties(tintedBlock, color);

            renderer.enabled = true;
            renderer.SetPropertyBlock(tintedBlock);
        }

        executionRangeTintActive = executionRendererStates.Count > 0;
    }

    public void ClearExecutionRangeTint()
    {
        if (!executionRangeTintActive)
            return;

        for (int i = 0; i < executionRendererStates.Count; i++)
        {
            ExecutionRendererState state = executionRendererStates[i];

            if (state == null || state.Renderer == null)
                continue;

            state.Renderer.enabled = state.Enabled;
            state.Renderer.SetPropertyBlock(state.PropertyBlock);
        }

        executionRendererStates.Clear();
        executionRangeTintActive = false;
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

    private void CacheBaseRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> filteredRenderers = new();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (highlightRenderer != null && renderer == highlightRenderer)
                continue;

            if (highlightObject != null &&
                renderer.transform.IsChildOf(highlightObject.transform))
            {
                continue;
            }

            filteredRenderers.Add(renderer);
        }

        baseRenderers = filteredRenderers.ToArray();
    }


    private void CacheDefaultHighlightMaterial()
    {
        if (highlightRenderer == null || defaultHighlightMaterial != null)
            return;

        defaultHighlightMaterial = highlightRenderer.sharedMaterial;
    }

    private void SetHighlightMaterial(Material materialOverride)
    {
        if (highlightRenderer == null)
            return;

        CacheDefaultHighlightMaterial();
        Material targetMaterial = materialOverride != null
            ? materialOverride
            : defaultHighlightMaterial;

        highlightRenderer.sharedMaterial = GetHighlightBackMaterial(targetMaterial);
    }

    private void RestoreDefaultHighlightMaterial()
    {
        if (highlightRenderer == null)
            return;

        CacheDefaultHighlightMaterial();

        if (defaultHighlightMaterial != null &&
            highlightRenderer.sharedMaterial != defaultHighlightMaterial)
        {
            highlightRenderer.sharedMaterial = defaultHighlightMaterial;
        }
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

        ApplyBackSorting(highlightRenderer);
        color.a = 1f;

        if (highlightPropertyBlock == null)
            highlightPropertyBlock = new MaterialPropertyBlock();

        highlightRenderer.GetPropertyBlock(highlightPropertyBlock);

        SetTintProperties(highlightPropertyBlock, color);

        highlightRenderer.SetPropertyBlock(highlightPropertyBlock);
    }

    private void ApplyBackSorting()
    {
        ApplyBackSorting(highlightRenderer);

        if (baseRenderers == null)
            return;

        for (int i = 0; i < baseRenderers.Length; i++)
            ApplyBackSorting(baseRenderers[i]);
    }

    private void ApplyBackSorting(Renderer renderer)
    {
        if (!forceBackSorting || renderer == null)
            return;

        if (!string.IsNullOrWhiteSpace(backSortingLayerName))
            renderer.sortingLayerName = backSortingLayerName;

        renderer.sortingOrder = backSortingOrder;

        if (renderer == highlightRenderer)
            ApplyHighlightBackMaterial();
    }

    private void ApplyHighlightBackMaterial()
    {
        if (highlightRenderer == null)
            return;

        Material material = GetHighlightBackMaterial(highlightRenderer.sharedMaterial);

        if (material != highlightRenderer.sharedMaterial)
            highlightRenderer.sharedMaterial = material;
    }

    private Material GetHighlightBackMaterial(Material source)
    {
        if (!forceHighlightBackRenderQueue || source == null)
            return source;

        int renderQueue = Mathf.Clamp(highlightBackRenderQueue, 0, 5000);

        if (IsHighlightBackMaterialConfigured(source, renderQueue))
            return source;

        if (highlightBackMaterials.TryGetValue(source, out Material cachedMaterial) &&
            cachedMaterial != null)
        {
            return cachedMaterial;
        }

        Material backMaterial = new(source)
        {
            name = $"{source.name} (Grid Back)"
        };

        ConfigureHighlightBackMaterial(backMaterial, renderQueue);
        highlightBackMaterials[source] = backMaterial;
        return backMaterial;
    }

    private static bool IsHighlightBackMaterialConfigured(Material material, int renderQueue)
    {
        if (material == null || material.renderQueue != renderQueue)
            return false;

        if (material.HasProperty("_ZWrite") &&
            !Mathf.Approximately(material.GetFloat("_ZWrite"), 0f))
        {
            return false;
        }

        return true;
    }

    private static void ConfigureHighlightBackMaterial(Material material, int renderQueue)
    {
        if (material == null)
            return;

        material.renderQueue = renderQueue;

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_ZWriteControl"))
            material.SetFloat("_ZWriteControl", 0f);
    }

    private static void SetTintProperties(MaterialPropertyBlock propertyBlock, Color color)
    {
        if (propertyBlock == null)
            return;

        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_Tint", color);
        propertyBlock.SetColor("_RendererColor", color);
        propertyBlock.SetFloat("_Alpha", 1f);
        propertyBlock.SetFloat("_Opacity", 1f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoFindHighlightIfNeeded();
    }
#endif
}
