using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class OptionHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private OptionHoverHighlight highlight;
    [SerializeField] private bool respondToSelection = true;
    [Header("Hover Sound")]
    [SerializeField, SoundId(SoundCategory.Sfx)] private string hoverSfxId;
    private Vector2? overrideHighlightSize;
    public void SetHighlightSize(Vector2 size) => overrideHighlightSize = size;
    public void SetHoverSfxId(string sfxId) => hoverSfxId = sfxId;

    private RectTransform rectTransform;
    private bool hovered;
    private bool selected;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        if (highlight == null)
        {
            for (Transform root = transform.parent; root != null && highlight == null; root = root.parent)
                highlight = root.GetComponentInChildren<OptionHoverHighlight>(true);
        }
        // Make the entire row receive pointer events, including empty space.
        Graphic graphic = GetComponent<Graphic>();
        if (graphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
        }
        else graphic.raycastTarget = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hovered && !string.IsNullOrWhiteSpace(hoverSfxId))
            AudioManager.Instance?.PlaySfx(hoverSfxId);
        hovered = true;
        if (highlight != null) highlight.MoveTo(rectTransform, overrideHighlightSize);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        if (!selected && highlight != null) highlight.Clear(rectTransform);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!respondToSelection) return;
        selected = true;
        if (highlight != null) highlight.MoveTo(rectTransform, overrideHighlightSize);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        if (!hovered && highlight != null) highlight.Clear(rectTransform);
    }

    private void OnDisable()
    {
        hovered = selected = false;
        if (highlight != null) highlight.Clear(rectTransform, true);
    }
}
