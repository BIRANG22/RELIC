using System;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapNodeHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private RectTransform iconTransform;
    [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.1f;
    [SerializeField, Min(0f)] private float scaleResponse = 12f;

    private GeneratedMapNodeData node;
    private Sprite icon;
    private Action<GeneratedMapNodeData, Sprite> onEntered;
    private Action onExited;
    private Vector3 baseIconScale = Vector3.one;
    private bool isHovered;

    public void Configure(GeneratedMapNodeData data, Sprite nodeIcon,
        Action<GeneratedMapNodeData, Sprite> entered, Action exited)
    {
        node = data;
        icon = nodeIcon;
        onEntered = entered;
        onExited = exited;
        ResolveIconTransform();
        if (iconTransform != null)
            baseIconScale = iconTransform.localScale;
    }

    private void Update() => AdvanceHoverScale(Time.unscaledDeltaTime);

    private void OnDisable()
    {
        isHovered = false;
        if (iconTransform != null)
            iconTransform.localScale = baseIconScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => EnterHover();

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isHovered) EnterHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        onExited?.Invoke();
    }

    public void AdvanceHoverScale(float deltaTime)
    {
        ResolveIconTransform();
        if (iconTransform == null) return;

        Vector3 target = baseIconScale * (isHovered ? hoverScaleMultiplier : 1f);
        float t = scaleResponse <= 0f ? 1f : 1f - Mathf.Exp(-scaleResponse * Mathf.Max(0f, deltaTime));
        iconTransform.localScale = Vector3.Lerp(iconTransform.localScale, target, t);
    }

    private void EnterHover()
    {
        isHovered = true;
        onEntered?.Invoke(node, icon);
    }

    private void ResolveIconTransform()
    {
        if (iconTransform == null)
            iconTransform = transform.Find("Icon") as RectTransform;

        if (iconTransform == null && transform.parent != null)
            iconTransform = transform.parent.Find("Icon") as RectTransform;
    }
}
