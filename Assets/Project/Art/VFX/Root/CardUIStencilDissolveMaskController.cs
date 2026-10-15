using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the rendered stencil material made by Unity UI Mask.
/// Attach to the same GameObject as Image + Mask.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image), typeof(Mask))]
public sealed class CardUIStencilDissolveMaskController : MonoBehaviour
{
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

    [Header("Mask Dissolve Preview")]
    [SerializeField, Range(0f, 1f)] private float dissolveAmount;

    private Image maskImage;
    private Material renderedMaterial;
    private Material originalMaterial;
    private Material privateMaterial;
    private float lastAppliedAmount = -1f;

    public float DissolveAmount => dissolveAmount;

    private void OnEnable()
    {
        maskImage = GetComponent<Image>();
        EnsurePrivateMaterial();
        lastAppliedAmount = -1f;
        Apply();
    }

    private void OnValidate()
    {
        dissolveAmount = Mathf.Clamp01(dissolveAmount);
        lastAppliedAmount = -1f;
        // OnValidate may run while Unity is rebuilding UI. Update() will apply safely.
    }

    private void Update()
    {
        if (maskImage == null)
            maskImage = GetComponent<Image>();
        if (maskImage == null)
            return;

        EnsurePrivateMaterial();
        // Mask uses an internally generated materialForRendering. Keep it in sync
        // even when Unity regenerates that material after a canvas rebuild.
        Material current = maskImage.materialForRendering;
        if (current != renderedMaterial || !Mathf.Approximately(lastAppliedAmount, dissolveAmount))
            Apply(current);
    }

    /// <summary>Animate 0 (all visible) to 1 (all removed).</summary>
    public void SetDissolveAmount(float amount)
    {
        dissolveAmount = Mathf.Clamp01(amount);
        EnsurePrivateMaterial();
        Apply();
    }

    public void ResetDissolve() => SetDissolveAmount(0f);

    private void Apply()
    {
        if (maskImage == null)
            maskImage = GetComponent<Image>();
        if (maskImage == null)
            return;
        Apply(maskImage.materialForRendering);
    }

    private void Apply(Material current)
    {
        if (maskImage == null)
            return;

        // Never modify a shared project material: it can be used by the selected card.
        if (privateMaterial != null && privateMaterial.HasProperty(DissolveAmountId))
            privateMaterial.SetFloat(DissolveAmountId, dissolveAmount);

        if (current != null && current.HasProperty(DissolveAmountId))
        {
            current.SetFloat(DissolveAmountId, dissolveAmount);
            renderedMaterial = current;
            lastAppliedAmount = dissolveAmount;
        }
        else
        {
            renderedMaterial = null;
            lastAppliedAmount = -1f;
        }
    }

    private void EnsurePrivateMaterial()
    {
        if (maskImage == null) maskImage = GetComponent<Image>();
        if (maskImage == null || privateMaterial != null) return;
        Material source = maskImage.material;
        if (source == null || !source.HasProperty(DissolveAmountId)) return;

        originalMaterial = source;
        privateMaterial = new Material(source);
        privateMaterial.name = source.name + " (Mask Instance: " + gameObject.name + ")";
        privateMaterial.hideFlags = HideFlags.DontSave;
        maskImage.material = privateMaterial;
        maskImage.SetMaterialDirty();
        renderedMaterial = null;
        lastAppliedAmount = -1f;
    }

    private void OnDestroy()
    {
        if (privateMaterial == null) return;
        if (maskImage != null && maskImage.material == privateMaterial)
        {
            maskImage.material = originalMaterial;
            maskImage.SetMaterialDirty();
        }
        if (Application.isPlaying) Destroy(privateMaterial);
        else DestroyImmediate(privateMaterial);
        privateMaterial = null;
    }

    private void OnDisable()
    {
        renderedMaterial = null;
        lastAppliedAmount = -1f;
    }
}
