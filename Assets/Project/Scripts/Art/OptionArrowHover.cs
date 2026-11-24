using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class OptionArrowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private Vector3 hoverScale = new Vector3(1.15f, 1.15f, 1f);
    [SerializeField, Min(0f)] private float transitionDuration = 0.12f;

    [Header("Hover Sound (Optional)")]
    [SerializeField, SoundId(SoundCategory.Sfx)] private string hoverSfxId;

    private Vector3 originalScale;
    private Vector3 startScale;
    private Vector3 targetScale;
    private float elapsed;
    private bool animating;
    private bool hovered;

    private void OnEnable()
    {
        if (scaleTarget == null) scaleTarget = transform as RectTransform;
        if (scaleTarget == null) return;
        originalScale = scaleTarget.localScale;
        hovered = animating = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hovered || scaleTarget == null) return;
        hovered = true;
        if (!string.IsNullOrWhiteSpace(hoverSfxId))
            AudioManager.Instance?.PlaySfx(hoverSfxId);
        BeginScale(Vector3.Scale(originalScale, hoverScale));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        BeginScale(originalScale);
    }

    private void BeginScale(Vector3 destination)
    {
        if (scaleTarget == null) return;
        startScale = scaleTarget.localScale;
        targetScale = destination;
        elapsed = 0f;
        animating = transitionDuration > 0f;
        if (!animating) scaleTarget.localScale = destination;
    }

    private void Update()
    {
        if (!animating || scaleTarget == null) return;
        elapsed += Time.unscaledDeltaTime;
        float t = transitionDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / transitionDuration);
        scaleTarget.localScale = Vector3.LerpUnclamped(startScale, targetScale, t * t * (3f - 2f * t));
        if (t >= 1f) animating = false;
    }

    private void OnDisable()
    {
        if (scaleTarget != null) scaleTarget.localScale = originalScale;
        hovered = animating = false;
    }
}
