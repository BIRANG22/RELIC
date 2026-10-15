using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class RewardCardDissolveUI : MonoBehaviour
{
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveEdgeColorId = Shader.PropertyToID("_DissolveEdgeColor");
    private static readonly int ReverseDissolveId = Shader.PropertyToID("_RewardReverseDissolve");

    [SerializeField, Min(0f)] private float delay = 0f;
    [SerializeField, Min(0f)] private float duration = 2f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Image image;
    private Material originalMaterial;
    private Material runtimeMaterial;
    private Coroutine animationRoutine;

    // Prepare the material while the card is still inactive, before its first draw.
    public void Prepare(Color edgeColor)
    {
        if (image == null) image = GetComponent<Image>();
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
        ReleaseMaterial();
        if (image == null) return;

        originalMaterial = image.material;
        if (originalMaterial == null || !originalMaterial.HasProperty(DissolveAmountId))
        {
            Debug.LogWarning($"[RewardCardDissolveUI] Material missing _DissolveAmount: {name}", this);
            return;
        }
        runtimeMaterial = new Material(originalMaterial);
        runtimeMaterial.name = originalMaterial.name + " (Reward Card Instance)";
        if (runtimeMaterial.HasProperty(DissolveEdgeColorId))
            runtimeMaterial.SetColor(DissolveEdgeColorId, edgeColor);
        // At amount 0 the card is hidden; at 1 it is fully visible.
        // Only this card instance has reversed dissolve enabled.
        if (runtimeMaterial.HasProperty(ReverseDissolveId))
            runtimeMaterial.SetFloat(ReverseDissolveId, 1f);
        else
            Debug.LogWarning($"[RewardCardDissolveUI] Shader missing _RewardReverseDissolve: {name}", this);
        runtimeMaterial.SetFloat(DissolveAmountId, 0f);
        image.material = runtimeMaterial;
        image.SetMaterialDirty();
    }

    // Start once after the full panel becomes active; amount stays 0 during delay.
    public void PlayPrepared(float startDelay, float playDuration)
    {
        delay = Mathf.Max(0f, startDelay);
        duration = Mathf.Max(0f, playDuration);
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = null;
        if (runtimeMaterial == null || !isActiveAndEnabled) return;
        runtimeMaterial.SetFloat(DissolveAmountId, 0f);
        image.SetMaterialDirty();
        animationRoutine = StartCoroutine(Animate());
    }

    public void Play(Color edgeColor)
    {
        Play(edgeColor, delay, duration);
    }

    public void Play(Color edgeColor, float startDelay, float playDuration)
    {
        Prepare(edgeColor);
        PlayPrepared(startDelay, playDuration);
    }

    private IEnumerator Animate()
    {
        // Keep the dissolve hidden at zero during the entire delay.
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (runtimeMaterial == null)
        {
            animationRoutine = null;
            yield break;
        }
        if (duration <= 0f)
        {
            runtimeMaterial.SetFloat(DissolveAmountId, 1f);
            image.SetMaterialDirty();
            animationRoutine = null;
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration && runtimeMaterial != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easing != null ? easing.Evaluate(Mathf.Clamp01(elapsed / duration)) : Mathf.Clamp01(elapsed / duration);
            runtimeMaterial.SetFloat(DissolveAmountId, Mathf.Lerp(0f, 1f, Mathf.Clamp01(t)));
            image.SetMaterialDirty();
            yield return null;
        }
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(DissolveAmountId, 1f);
            image.SetMaterialDirty();
        }
        animationRoutine = null;
    }

    private void ReleaseMaterial()
    {
        if (runtimeMaterial == null) return;
        if (image != null) image.material = originalMaterial;
        Destroy(runtimeMaterial);
        runtimeMaterial = null;
    }

    private void OnDisable()
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = null;
        ReleaseMaterial();
    }

    private void OnDestroy() => ReleaseMaterial();
}
