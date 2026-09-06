using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleNextNodeChoiceHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color32 NormalArrowColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    private static readonly Color32 HoverArrowColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

    private Vector3 normalScale = Vector3.one;
    private Vector3 hoverScaleMultiplier = Vector3.one;
    private bool pointerInside;
    private bool isSelected;
    private Image arrowImage;

    public void Configure(Vector3 baseScale, Vector3 hoverMultiplier)
    {
        normalScale = baseScale;
        hoverScaleMultiplier = hoverMultiplier;
        ResolveArrowImage();
        ApplyCurrentState();
    }

    public void SetBaseScale(Vector3 baseScale)
    {
        normalScale = baseScale;
        ApplyCurrentState();
    }

    public void SetHoverScaleMultiplier(Vector3 hoverMultiplier)
    {
        hoverScaleMultiplier = hoverMultiplier;
        ApplyCurrentState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyCurrentState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ApplyCurrentState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        ApplyCurrentState();
    }

    private void Awake()
    {
        ResolveArrowImage();
        ApplyArrowColor();
    }

    private void OnEnable()
    {
        pointerInside = false;
        ResolveArrowImage();
        ApplyCurrentState();
    }

    private void OnDisable()
    {
        pointerInside = false;
        isSelected = false;
        transform.localScale = normalScale;
        ApplyArrowColor();
    }

    private void ApplyCurrentState()
    {
        bool highlighted = pointerInside || isSelected;

        transform.localScale = highlighted
            ? Vector3.Scale(normalScale, hoverScaleMultiplier)
            : normalScale;

        ApplyArrowColor();
    }

    private void ResolveArrowImage()
    {
        if (arrowImage != null)
            return;

        Transform arrow = transform.Find("arrow");
        if (arrow != null)
            arrowImage = arrow.GetComponent<Image>();
    }

    private void ApplyArrowColor()
    {
        if (arrowImage == null)
            ResolveArrowImage();

        if (arrowImage != null)
            arrowImage.color = (pointerInside || isSelected) ? HoverArrowColor : NormalArrowColor;
    }
}
