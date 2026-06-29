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

    private MaterialPropertyBlock highlightPropertyBlock;
    private Renderer[] baseRenderers;
    private bool executionRangeTintActive;
    private readonly List<ExecutionRendererState> executionRendererStates = new();

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
        CacheBaseRenderers();

        highlightPropertyBlock = new MaterialPropertyBlock();

        SetNormal();
    }

    private void Awake()
    {
        AutoFindHighlightIfNeeded();
        CacheBaseRenderers();
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
        SetHighlightActive(false);
    }

    public void SetPreview()
    {
        SetPreview(previewColor);
    }

    public void SetPreview(Color color)
    {
        SetHighlightColor(color);
        SetHighlightActive(true);
    }

    public void SetSelected()
    {
        SetHighlightColor(selectedColor);
        SetHighlightActive(true);
    }

    public void SetRangePreview()
    {
        SetRangePreview(rangePreviewColor);
    }

    public void SetRangePreview(Color color)
    {
        SetHighlightColor(color);
        SetHighlightActive(true);
    }

    public void SetExecutionRangeTint(Color color)
    {
        ClearExecutionRangeTint();
        CacheBaseRenderers();

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

        color.a = 1f;

        if (highlightPropertyBlock == null)
            highlightPropertyBlock = new MaterialPropertyBlock();

        highlightRenderer.GetPropertyBlock(highlightPropertyBlock);

        SetTintProperties(highlightPropertyBlock, color);

        highlightRenderer.SetPropertyBlock(highlightPropertyBlock);
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
