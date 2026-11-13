using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Place on TutorialMode/HighlightFrame/Image. Matches its sibling Line rect,
/// then repeatedly expands and fades without changing the Line artwork.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(Image))]
public sealed class TutorialHighlightPulseImage : MonoBehaviour
{
    [SerializeField] private RectTransform line;
    [SerializeField, Min(0.05f)] private float pulseDuration = 1.5f;
    [SerializeField, Min(1f)] private float endScale = 1.2f;
    [SerializeField, Range(0f, 1f)] private float startingAlpha = 1f;
    [SerializeField, Min(0.05f)] private float fadeInSeconds = 0.4f;
    [SerializeField, Min(0f)] private float fullOpacityHoldSeconds = 0.35f;
    [SerializeField, Min(0.05f)] private float minimumFadeOutSeconds = 0.5f;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform imageRect;
    private Image image;
    private float elapsed;
    private Vector3 initialScale;
    private Color initialColor;

    private void Awake()
    {
        imageRect = (RectTransform)transform;
        image = GetComponent<Image>();
        if (line == null)
        {
            Transform sibling = transform.parent != null ? transform.parent.Find("Line") : null;
            line = sibling as RectTransform;
        }
        initialScale = imageRect.localScale;
        initialColor = image.color;
        // The prefab may be active at battle scene load. Never show its initial sprite.
        image.enabled = false;
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        elapsed = 0f;
        if (image != null)
        {
            image.enabled = false;
            SetAlpha(0f);
        }
    }

    private void LateUpdate()
    {
        // Hide the image before the first focused tutorial step and whenever focus ends.
        if (line == null || !line.gameObject.activeInHierarchy || !TutorialFocusGuideRunner.IsRunning)
        {
            if (image != null) image.enabled = false;
            elapsed = 0f;
            return;
        }

        // Follow Line's actual size after TutorialFocusGuideRunner updates it.
        imageRect.anchorMin = line.anchorMin;
        imageRect.anchorMax = line.anchorMax;
        imageRect.pivot = line.pivot;
        imageRect.anchoredPosition = line.anchoredPosition;
        imageRect.localRotation = line.localRotation;
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, line.rect.width);
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, line.rect.height);

        // Keep enough time for the white image to be completely visible, even if
        // an old prefab still has the previous short Pulse Duration serialized.
        float duration = Mathf.Max(pulseDuration, fadeInSeconds + fullOpacityHoldSeconds + minimumFadeOutSeconds);
        float t = Mathf.Clamp01(elapsed / duration);
        float scale = Mathf.Lerp(1f, endScale, t);
        imageRect.localScale = Vector3.Scale(line.localScale, new Vector3(scale, scale, 1f));
        Color color = initialColor;
        // Start transparent, fade in gently, then fade out to transparent.
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeInSeconds));
        float fadeOutStart = fadeInSeconds + fullOpacityHoldSeconds;
        float fadeOut = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((elapsed - fadeOutStart) / (duration - fadeOutStart)));
        color.a = startingAlpha * fadeIn * fadeOut;
        image.color = color;
        image.enabled = true;
        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (elapsed >= duration) elapsed %= duration;
    }

    private void OnDisable()
    {
        if (imageRect != null) imageRect.localScale = initialScale;
        if (image != null)
        {
            SetAlpha(0f);
            image.enabled = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}
