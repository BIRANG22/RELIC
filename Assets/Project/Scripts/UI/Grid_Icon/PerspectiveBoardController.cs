using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerspectiveBoardController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider perspectiveSlider;

    [Header("Editor Test")]
    [SerializeField] private int editorTestRow = -1;
    [SerializeField] private int editorTestSlot = -1;

    [Header("Hover Animation")]
    [SerializeField] private float hoverLerpSpeed = 12f;

    private int hoveredSlot = -1;
    private float[] hoverWeights;

    [Header("Board")]
    [SerializeField] private RectTransform boardBackgroundRect;
    [SerializeField] private UIQuadWarp boardBackgroundWarp;

    [Header("Grid")]
    [SerializeField] private List<BoardSlotView> slots = new List<BoardSlotView>();

    [Header("Profile")]
    [SerializeField] private BoardPerspectiveProfile profile = new BoardPerspectiveProfile();

    [Header("Highlight State")]
    [SerializeField] private int highlightedRow = -1;   // -1 = none
    [SerializeField] private int highlightedSlot = -1;  // -1 = none

    [ContextMenu("Apply Layout")]
    public void ApplyLayout()
    {
        if (boardBackgroundRect == null)
            return;

        UpdateBoardQuadFromPerspective();
        ApplyBackground();
        ApplySlotsByBoardQuad();
    }

    [ContextMenu("Auto Collect Slots")]
    public void AutoCollectSlots()
    {
        slots.Clear();
        BoardSlotView[] found = GetComponentsInChildren<BoardSlotView>(true);
        slots.AddRange(found);
        EnsureHoverWeights();
    }

    private void EnsureHoverWeights()
    {
        int count = slots != null ? slots.Count : 0;
        if (hoverWeights == null || hoverWeights.Length != count)
            hoverWeights = new float[count];
    }
    private void Start()
    {
        SetupPerspectiveSlider();
        ApplyLayout();
    }

    private void OnEnable()
    {
        SetupPerspectiveSlider();
    }

    private void OnDisable()
    {
        if (perspectiveSlider != null)
            perspectiveSlider.onValueChanged.RemoveListener(OnPerspectiveSliderChanged);
    }

    private void SetupPerspectiveSlider()
    {
        if (perspectiveSlider == null)
            return;

        perspectiveSlider.minValue = 0f;
        perspectiveSlider.maxValue = 1f;
        perspectiveSlider.wholeNumbers = false;

        perspectiveSlider.onValueChanged.RemoveListener(OnPerspectiveSliderChanged);
        perspectiveSlider.onValueChanged.AddListener(OnPerspectiveSliderChanged);

        perspectiveSlider.SetValueWithoutNotify(profile.perspectiveAmount);
    }

    public void OnPerspectiveSliderChanged(float value)
    {
        Debug.Log($"Slider Value = {value}");
        profile.perspectiveAmount = value;
        ApplyLayout();
    }

    private void Update()
    {
        if (slots == null || slots.Count == 0)
            return;

        EnsureHoverWeights();

        bool changed = false;

        for (int i = 0; i < hoverWeights.Length; i++)
        {
            float target = (i == hoveredSlot) ? 1f : 0f;
            float next = Mathf.Lerp(hoverWeights[i], target, Time.deltaTime * hoverLerpSpeed);

            if (Mathf.Abs(next - hoverWeights[i]) > 0.001f)
            {
                hoverWeights[i] = next;
                changed = true;
            }
            else if (!Mathf.Approximately(hoverWeights[i], target))
            {
                hoverWeights[i] = target;
                changed = true;
            }
        }

        if (changed)
            ApplyLayout();
    }

    private void UpdateBoardQuadFromPerspective()
    {
        float t = Mathf.SmoothStep(0f, 1f, profile.perspectiveAmount);

        float topInset = Mathf.Lerp(profile.topInsetMin, profile.topInsetMax, t);
        float topDrop = Mathf.Lerp(profile.topDropMin, profile.topDropMax, t);
        float bottomExpand = Mathf.Lerp(profile.bottomExpandMin, profile.bottomExpandMax, t);
        float bottomLift = Mathf.Lerp(profile.bottomLiftMin, profile.bottomLiftMax, t);

        profile.boardQuad.topLeft = new Vector2(+topInset, -topDrop);
        profile.boardQuad.topRight = new Vector2(-topInset, -topDrop);
        profile.boardQuad.bottomRight = new Vector2(+bottomExpand, +bottomLift);
        profile.boardQuad.bottomLeft = new Vector2(-bottomExpand, +bottomLift);
    }

    private void ApplyBackground()
    {
        boardBackgroundRect.sizeDelta = new Vector2(profile.boardWidth, profile.boardHeight);
        boardBackgroundRect.anchoredPosition = new Vector2(0f, profile.boardCenterY);

        if (boardBackgroundWarp != null)
        {
            var q = profile.boardQuad;
            boardBackgroundWarp.SetCorners(
                q.topLeft,
                q.topRight,
                q.bottomRight,
                q.bottomLeft
            );
        }
    }

    private void ApplySlotsByBoardQuad()
    {
        int columns = Mathf.Max(1, profile.columns);
        int rows = Mathf.Max(1, profile.rows);

        if (slots == null || slots.Count == 0)
            return;

        float boardW = profile.boardWidth;
        float boardH = profile.boardHeight;
        float halfW = boardW * 0.5f;
        float halfH = boardH * 0.5f;

        Vector2 srcTL = new Vector2(-halfW, +halfH);
        Vector2 srcTR = new Vector2(+halfW, +halfH);
        Vector2 srcBR = new Vector2(+halfW, -halfH);
        Vector2 srcBL = new Vector2(-halfW, -halfH);

        var q = profile.boardQuad;

        Vector2 dstTL = srcTL + new Vector2(q.topLeft.x * halfW, q.topLeft.y * halfH);
        Vector2 dstTR = srcTR + new Vector2(q.topRight.x * halfW, q.topRight.y * halfH);
        Vector2 dstBR = srcBR + new Vector2(q.bottomRight.x * halfW, q.bottomRight.y * halfH);
        Vector2 dstBL = srcBL + new Vector2(q.bottomLeft.x * halfW, q.bottomLeft.y * halfH);

        float padU = profile.cellPaddingX / columns;
        float padV = profile.cellPaddingY / rows;

        float baseRectWidth = boardW / columns;
        float baseRectHeight = boardH / rows;

        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            float rowV0 = row / (float)rows;
            float rowV1 = (row + 1) / (float)rows;

            for (int col = 0; col < columns; col++)
            {
                if (index >= slots.Count)
                    return;

                float colU0 = col / (float)columns;
                float colU1 = (col + 1) / (float)columns;

                float u0 = colU0 + padU;
                float u1 = colU1 - padU;
                float v0 = rowV0 + padV;
                float v1 = rowV1 - padV;

                Vector2 cellTL = EvaluateQuad(dstTL, dstTR, dstBR, dstBL, u0, v0);
                Vector2 cellTR = EvaluateQuad(dstTL, dstTR, dstBR, dstBL, u1, v0);
                Vector2 cellBR = EvaluateQuad(dstTL, dstTR, dstBR, dstBL, u1, v1);
                Vector2 cellBL = EvaluateQuad(dstTL, dstTR, dstBR, dstBL, u0, v1);

                BoardSlotView slot = slots[index];
                if (slot != null && slot.rectTransform != null)
                {
                    ApplySlotFromCorners(
                        slot,
                        cellTL, cellTR, cellBR, cellBL,
                        row, col, index, rows,
                        baseRectWidth, baseRectHeight
                    );
                }

                index++;
            }
        }

        ApplyDrawOrder();
    }

    private void ApplySlotFromCorners(
        BoardSlotView slot,
        Vector2 tl,
        Vector2 tr,
        Vector2 br,
        Vector2 bl,
        int row,
        int col,
        int slotIndex,
        int totalRows,
        float baseRectWidth,
        float baseRectHeight)
    {
        Vector2 center = (tl + tr + br + bl) * 0.25f;

        float slotScaleMul = 1f;
        float extraY = 0f;
        float extraX = 0f;

        // Hover weight
        float hoverT = 0f;
        if (hoverWeights != null && slotIndex >= 0 && slotIndex < hoverWeights.Length)
            hoverT = hoverWeights[slotIndex];

        // Row highlight
        if (row == highlightedRow)
        {
            float dir = (row == 0) ? -1f : 1f; // 위행은 -, 아래행은 +

            extraY += profile.rowRaiseYOffset * dir;
            slotScaleMul *= profile.rowRaiseScale;

            float centerCol = (profile.columns - 1) * 0.5f;
            float spread = (col - centerCol) * profile.rowRaiseXSpread;
            extraX += spread;
        }

        // Single slot highlight
        if (slotIndex == highlightedSlot)
        {
            extraY += profile.slotRaiseYOffset;
            extraX += profile.slotRaiseXOffset;
            slotScaleMul *= profile.slotRaiseScale;
        }

        // Hover animation
        if (hoverT > 0f)
        {
            extraY += profile.slotRaiseYOffset * hoverT;
            slotScaleMul *= Mathf.Lerp(1f, profile.slotRaiseScale, hoverT);
        }

        slot.rectTransform.anchoredPosition =
            center + new Vector2(extraX, profile.boardCenterY + extraY);

        slot.rectTransform.sizeDelta = new Vector2(baseRectWidth, baseRectHeight);
        slot.rectTransform.localScale = new Vector3(slotScaleMul, slotScaleMul, 1f);

        UIQuadWarp.CornerOffset warp = BuildWarpFromCornersRelativeToRect(
            tl - center,
            tr - center,
            br - center,
            bl - center,
            baseRectWidth,
            baseRectHeight
        );

        if (slot.frameRoot != null)
        {
            slot.frameRoot.anchoredPosition = Vector2.zero;
            slot.frameRoot.localScale = Vector3.one * profile.frameScale;
            slot.frameRoot.sizeDelta = new Vector2(baseRectWidth, baseRectHeight);
        }

        if (slot.frameWarp != null)
        {
            slot.frameWarp.SetCorners(
                warp.topLeft,
                warp.topRight,
                warp.bottomRight,
                warp.bottomLeft
            );
        }

        if (slot.iconRoot != null)
        {
            float row01 = totalRows <= 1 ? 0f : row / (float)(totalRows - 1);
            float iconScale = Mathf.Lerp(profile.iconScaleFar, profile.iconScaleNear, row01);
            float iconYOffset = Mathf.Lerp(profile.iconYOffsetFar, profile.iconYOffsetNear, row01);

            if (slotIndex == highlightedSlot)
            {
                iconScale *= profile.slotRaiseIconScale;
                iconYOffset += profile.slotRaiseIconYOffset;
            }

            if (hoverT > 0f)
            {
                iconScale *= Mathf.Lerp(1f, profile.slotRaiseIconScale, hoverT);
                iconYOffset += profile.slotRaiseIconYOffset * hoverT;
            }

            slot.iconRoot.anchoredPosition = new Vector2(0f, iconYOffset);
            slot.iconRoot.localScale = new Vector3(iconScale, iconScale, 1f);
            slot.iconRoot.sizeDelta = new Vector2(baseRectWidth, baseRectHeight);
        }

        if (slot.iconWarp != null)
        {
            float t = Mathf.SmoothStep(0f, 1f, profile.iconPerspectiveAmount);
            float k = Mathf.Lerp(profile.iconWarpStrengthMin, profile.iconWarpStrengthMax, t);

            slot.iconWarp.SetCorners(
                warp.topLeft * k,
                warp.topRight * k,
                warp.bottomRight * k,
                warp.bottomLeft * k
            );
        }

        // Highlighted / hovered slot drawn last
        //slot.transform.SetSiblingIndex(slotIndex);
        //if (slotIndex == highlightedSlot || hoverT > 0.01f)
        //{
        //    slot.transform.SetAsLastSibling();
        //}
    }

    private void ApplyDrawOrder()
    {
        if (slots == null || slots.Count == 0)
            return;

        int columns = Mathf.Max(1, profile.columns);
        int rows = Mathf.Max(1, profile.rows);

        List<int> normalOrder = new List<int>();
        List<int> highlightedRowOrder = new List<int>();
        List<int> emphasizedOrder = new List<int>();

        for (int i = 0; i < slots.Count; i++)
        {
            int row = i / columns;

            float hoverT = 0f;
            if (hoverWeights != null && i >= 0 && i < hoverWeights.Length)
                hoverT = hoverWeights[i];

            bool isHighlightedSlot = (i == highlightedSlot);
            bool isHoveredSlot = (hoverT > 0.01f);
            bool isHighlightedRow = (row == highlightedRow);

            if (isHighlightedSlot || isHoveredSlot)
            {
                emphasizedOrder.Add(i);
            }
            else if (isHighlightedRow)
            {
                highlightedRowOrder.Add(i);
            }
            else
            {
                normalOrder.Add(i);
            }
        }

        int sibling = 0;

        // 먼저 일반 슬롯
        for (int i = 0; i < normalOrder.Count; i++)
        {
            int slotIndex = normalOrder[i];
            if (slots[slotIndex] != null)
                slots[slotIndex].transform.SetSiblingIndex(sibling++);
        }

        // 그 다음 강조된 행 전체
        for (int i = 0; i < highlightedRowOrder.Count; i++)
        {
            int slotIndex = highlightedRowOrder[i];
            if (slots[slotIndex] != null)
                slots[slotIndex].transform.SetSiblingIndex(sibling++);
        }

        // 마지막으로 hover/선택 슬롯
        for (int i = 0; i < emphasizedOrder.Count; i++)
        {
            int slotIndex = emphasizedOrder[i];
            if (slots[slotIndex] != null)
                slots[slotIndex].transform.SetSiblingIndex(sibling++);
        }
    }
    private UIQuadWarp.CornerOffset BuildWarpFromCornersRelativeToRect(
        Vector2 tl,
        Vector2 tr,
        Vector2 br,
        Vector2 bl,
        float rectWidth,
        float rectHeight)
    {
        float halfW = rectWidth * 0.5f;
        float halfH = rectHeight * 0.5f;

        Vector2 baseTL = new Vector2(-halfW, +halfH);
        Vector2 baseTR = new Vector2(+halfW, +halfH);
        Vector2 baseBR = new Vector2(+halfW, -halfH);
        Vector2 baseBL = new Vector2(-halfW, -halfH);

        UIQuadWarp.CornerOffset c = new UIQuadWarp.CornerOffset
        {
            topLeft = NormalizeCornerDelta(tl - baseTL, halfW, halfH),
            topRight = NormalizeCornerDelta(tr - baseTR, halfW, halfH),
            bottomRight = NormalizeCornerDelta(br - baseBR, halfW, halfH),
            bottomLeft = NormalizeCornerDelta(bl - baseBL, halfW, halfH)
        };

        return c;
    }

    private Vector2 NormalizeCornerDelta(Vector2 delta, float halfW, float halfH)
    {
        return new Vector2(
            halfW <= 0f ? 0f : delta.x / halfW,
            halfH <= 0f ? 0f : delta.y / halfH
        );
    }

    private Vector2 EvaluateQuad(Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl, float u, float v)
    {
        Vector2 left = Vector2.Lerp(tl, bl, v);
        Vector2 right = Vector2.Lerp(tr, br, v);
        return Vector2.Lerp(left, right, u);
    }

    public void SetHighlightedRow(int row)
    {
        highlightedRow = row;
        ApplyLayout();
    }

    public void SetHighlightedSlot(int slotIndex)
    {
        highlightedSlot = slotIndex;
        ApplyLayout();
    }

    public void ClearHighlights()
    {
        highlightedRow = -1;
        highlightedSlot = -1;
        ApplyLayout();
    }

    public void SetHoveredSlot(int slotIndex)
    {
        hoveredSlot = slotIndex;
    }

    public void ClearHoveredSlot(int slotIndex)
    {
        if (hoveredSlot == slotIndex)
            hoveredSlot = -1;
    }

    [ContextMenu("Test/Highlight Top Row")]
    private void TestHighlightTopRow()
    {
        SetHighlightedRow(0);
    }

    [ContextMenu("Test/Highlight Bottom Row")]
    private void TestHighlightBottomRow()
    {
        SetHighlightedRow(1);
    }

    [ContextMenu("Test/Highlight Slot 0")]
    private void TestHighlightSlot0()
    {
        SetHighlightedSlot(0);
    }

    [ContextMenu("Test/Highlight Slot 6")]
    private void TestHighlightSlot6()
    {
        SetHighlightedSlot(6);
    }

    [ContextMenu("Test/Clear Highlights")]
    private void TestClearHighlights()
    {
        ClearHighlights();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            highlightedRow = editorTestRow;
            highlightedSlot = editorTestSlot;
            ApplyLayout();
        }
    }
#endif
}