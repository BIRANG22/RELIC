using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// When this GameObject is enabled, animates the material's progress property
/// from Start Progress to Target Progress.
/// Supports both UI Graphic components and Renderer components.
/// </summary>
[DisallowMultipleComponent]
public class MaterialProgressOnEnable : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private string progressProperty = "_progress";
    [SerializeField] private float startProgress = 0.5f;
    [SerializeField] private float targetProgress = -2f;
    [SerializeField, Min(0f)] private float duration = 0.6f;

    [Header("Playback")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Return To Start")]
    [Tooltip("When enabled, Progress returns from Target Progress back to Start Progress after the forward animation finishes.")]
    [SerializeField] private bool returnToStart = false;

    [Tooltip("How long to stay at Target Progress before returning.")]
    [SerializeField, Min(0f)] private float returnDelay = 0f;

    [Tooltip("How long it takes to return from Target Progress to Start Progress.")]
    [SerializeField, Min(0f)] private float returnDuration = 0.6f;

    [Tooltip("Animation curve used while returning to Start Progress.")]
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Direction On Return")]
    [Tooltip("When enabled, changes the material Direction value immediately before the return animation starts.")]
    [SerializeField] private bool changeDirectionOnReturn = false;

    [Tooltip("Shader property name used for Direction.")]
    [SerializeField] private string directionProperty = "_direction";

    [Tooltip("Direction value applied when the return animation starts.")]
    [SerializeField] private float returnDirectionValue = 1f;

    private Material runtimeMaterial;
    private Coroutine progressRoutine;
    private int progressPropertyId;
    private int directionPropertyId;
    private bool hasInitialDirection;
    private float initialDirectionValue;

    private void Awake()
    {
        RefreshPropertyIds();
        TryCreateRuntimeMaterial();
        CaptureInitialDirection();
    }

    private void OnEnable()
    {
        RefreshPropertyIds();

        if (!TryCreateRuntimeMaterial())
            return;

        if (!runtimeMaterial.HasProperty(progressPropertyId))
        {
            Debug.LogWarning(
                $"[{nameof(MaterialProgressOnEnable)}] Material '{runtimeMaterial.name}' does not contain property '{progressProperty}'.",
                this);
            return;
        }

        CaptureInitialDirection();

        if (progressRoutine != null)
            StopCoroutine(progressRoutine);

        // Replaying this object starts with the original material direction again.
        if (changeDirectionOnReturn && hasInitialDirection)
            runtimeMaterial.SetFloat(directionPropertyId, initialDirectionValue);

        runtimeMaterial.SetFloat(progressPropertyId, startProgress);
        progressRoutine = StartCoroutine(AnimateProgressRoutine());
    }

    private void OnDisable()
    {
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeMaterial);
        else
            DestroyImmediate(runtimeMaterial);
    }

    private IEnumerator AnimateProgressRoutine()
    {
        if (duration <= 0f)
        {
            runtimeMaterial.SetFloat(progressPropertyId, targetProgress);
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float curvedTime = animationCurve != null
                    ? animationCurve.Evaluate(normalizedTime)
                    : normalizedTime;

                float value = Mathf.LerpUnclamped(startProgress, targetProgress, curvedTime);
                runtimeMaterial.SetFloat(progressPropertyId, value);

                yield return null;
            }

            runtimeMaterial.SetFloat(progressPropertyId, targetProgress);
        }

        if (!returnToStart)
        {
            progressRoutine = null;
            yield break;
        }

        if (returnDelay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < returnDelay)
            {
                delayElapsed += GetDeltaTime();
                yield return null;
            }
        }

        ApplyReturnDirection();

        if (returnDuration <= 0f)
        {
            runtimeMaterial.SetFloat(progressPropertyId, startProgress);
            progressRoutine = null;
            yield break;
        }

        float returnElapsed = 0f;
        while (returnElapsed < returnDuration)
        {
            returnElapsed += GetDeltaTime();

            float normalizedTime = Mathf.Clamp01(returnElapsed / returnDuration);
            float curvedTime = returnCurve != null
                ? returnCurve.Evaluate(normalizedTime)
                : normalizedTime;

            float value = Mathf.LerpUnclamped(targetProgress, startProgress, curvedTime);
            runtimeMaterial.SetFloat(progressPropertyId, value);

            yield return null;
        }

        runtimeMaterial.SetFloat(progressPropertyId, startProgress);
        progressRoutine = null;
    }

    private void ApplyReturnDirection()
    {
        if (!changeDirectionOnReturn || runtimeMaterial == null)
            return;

        if (directionPropertyId == 0)
            directionPropertyId = Shader.PropertyToID(directionProperty);

        if (!runtimeMaterial.HasProperty(directionPropertyId))
        {
            Debug.LogWarning(
                $"[{nameof(MaterialProgressOnEnable)}] Material '{runtimeMaterial.name}' does not contain direction property '{directionProperty}'.",
                this);
            return;
        }

        runtimeMaterial.SetFloat(directionPropertyId, returnDirectionValue);
    }

    private void CaptureInitialDirection()
    {
        if (hasInitialDirection || runtimeMaterial == null || !changeDirectionOnReturn)
            return;

        if (directionPropertyId == 0)
            directionPropertyId = Shader.PropertyToID(directionProperty);

        if (!runtimeMaterial.HasProperty(directionPropertyId))
            return;

        initialDirectionValue = runtimeMaterial.GetFloat(directionPropertyId);
        hasInitialDirection = true;
    }

    private void RefreshPropertyIds()
    {
        progressPropertyId = Shader.PropertyToID(progressProperty);
        directionPropertyId = Shader.PropertyToID(directionProperty);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private bool TryCreateRuntimeMaterial()
    {
        if (runtimeMaterial != null)
            return true;

        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null && graphic.material != null)
        {
            runtimeMaterial = new Material(graphic.material)
            {
                name = graphic.material.name + " (Runtime Instance)"
            };
            graphic.material = runtimeMaterial;
            return true;
        }

        Renderer targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null && targetRenderer.sharedMaterial != null)
        {
            runtimeMaterial = new Material(targetRenderer.sharedMaterial)
            {
                name = targetRenderer.sharedMaterial.name + " (Runtime Instance)"
            };
            targetRenderer.material = runtimeMaterial;
            return true;
        }

        Debug.LogWarning(
            $"[{nameof(MaterialProgressOnEnable)}] No UI Graphic or Renderer with a material was found on '{name}'.",
            this);
        return false;
    }
}
