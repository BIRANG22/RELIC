using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class SkillEffectTooltipDownwardLayout
{
    public static List<float> BuildTopOffsets(IReadOnlyList<float> heights, float spacing)
    {
        var result = new List<float>();
        float offset = 0f;
        if (heights == null)
            return result;

        for (int i = 0; i < heights.Count; i++)
        {
            result.Add(offset);
            offset -= Mathf.Max(0f, heights[i]) + Mathf.Max(0f, spacing);
        }

        return result;
    }

    public static void Apply(
        UnitStatusEffectTooltipUI tooltip,
        RectTransform tooltipPanel,
        RectTransform customAnchor,
        Vector2 offset,
        float verticalSpacing)
    {
        if (tooltip == null || tooltipPanel == null)
            return;

        UnitStatusEffectTooltipItemUI[] items =
            tooltip.GetComponentsInChildren<UnitStatusEffectTooltipItemUI>(false);
        if (items == null || items.Length == 0)
            return;

        System.Array.Sort(items, (left, right) =>
            left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
        RectTransform itemRoot = items[0].transform.parent as RectTransform;
        if (itemRoot == null)
            return;

        RectTransform anchor = customAnchor != null ? customAnchor : tooltipPanel;
        Canvas anchorCanvas = anchor.GetComponentInParent<Canvas>();
        Camera anchorCamera = anchorCanvas != null && anchorCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? anchorCanvas.worldCamera
            : null;
        Canvas effectCanvas = itemRoot.GetComponentInParent<Canvas>();
        Camera effectCamera = effectCanvas != null && effectCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? effectCanvas.worldCamera
            : null;

        Vector3[] anchorCorners = new Vector3[4];
        anchor.GetWorldCorners(anchorCorners);
        // Upper-right corner of the hovered card, not the previous upper-left reference.
        Vector3 targetWorld = anchorCorners[2] + anchor.TransformVector(new Vector3(offset.x, offset.y, 0f));
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(anchorCamera, targetWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                itemRoot,
                targetScreen,
                effectCamera,
                out Vector2 firstTopLeft))
            return;

        var heights = new List<float>(items.Length);
        var rects = new List<RectTransform>(items.Length);
        Vector3[] corners = new Vector3[4];
        foreach (UnitStatusEffectTooltipItemUI item in items)
        {
            RectTransform rect = item.transform as RectTransform;
            if (rect == null)
                continue;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            rect.GetWorldCorners(corners);
            Vector3 bottomLeft = itemRoot.InverseTransformPoint(corners[0]);
            Vector3 topLeft = itemRoot.InverseTransformPoint(corners[1]);
            rects.Add(rect);
            heights.Add(Mathf.Max(0f, topLeft.y - bottomLeft.y));
        }

        List<float> topOffsets = BuildTopOffsets(heights, verticalSpacing);
        for (int i = 0; i < rects.Count; i++)
        {
            RectTransform rect = rects[i];
            rect.GetWorldCorners(corners);
            Vector3 currentTopLeft = itemRoot.InverseTransformPoint(corners[1]);
            Vector2 desiredTopLeft = firstTopLeft + new Vector2(0f, topOffsets[i]);
            Vector3 delta = itemRoot.TransformVector(new Vector3(
                desiredTopLeft.x - currentTopLeft.x,
                desiredTopLeft.y - currentTopLeft.y,
                0f));
            rect.position += delta;
        }
    }
}
