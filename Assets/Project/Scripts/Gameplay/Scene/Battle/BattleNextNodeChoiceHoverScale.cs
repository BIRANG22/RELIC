using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class BattleNextNodeChoiceHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 normalScale = Vector3.one;
    private Vector3 hoverScaleMultiplier = Vector3.one;
    private bool pointerInside;

    public void Configure(Vector3 baseScale, Vector3 hoverMultiplier)
    {
        normalScale = baseScale;
        hoverScaleMultiplier = hoverMultiplier;
        ApplyCurrentScale();
    }

    public void SetBaseScale(Vector3 baseScale)
    {
        normalScale = baseScale;
        ApplyCurrentScale();
    }

    public void SetHoverScaleMultiplier(Vector3 hoverMultiplier)
    {
        hoverScaleMultiplier = hoverMultiplier;
        ApplyCurrentScale();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ApplyCurrentScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        ApplyCurrentScale();
    }

    private void OnDisable()
    {
        pointerInside = false;
        transform.localScale = normalScale;
    }

    private void ApplyCurrentScale()
    {
        transform.localScale = pointerInside
            ? Vector3.Scale(normalScale, hoverScaleMultiplier)
            : normalScale;
    }
}
