using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Ready 패널 드래그 중 일반 호버 효과를 차단하되 실제 드롭 대상의 시각적 안내는 유지합니다.
/// </summary>
public static class LobbyReadyDragHoverGate
{
    public enum DragKind { Character, Relic, Compound }

    private static readonly Dictionary<object, DragKind> Sources = new Dictionary<object, DragKind>();
    public static bool IsDragging => Sources.Count > 0;

    public static bool IsCharacterDrag => HasKind(DragKind.Character);
    public static bool IsEquipmentDrag => HasKind(DragKind.Relic) || HasKind(DragKind.Compound);

    public static bool AllowsGridTargetHover => IsCharacterDrag;
    public static bool AllowsEquipmentTargetHover(bool isCompound) =>
        HasKind(isCompound ? DragKind.Compound : DragKind.Relic);

    private static bool HasKind(DragKind kind)
    {
        foreach (DragKind sourceKind in Sources.Values)
            if (sourceKind == kind) return true;
        return false;
    }

    public static bool BlockHover(PointerEventData eventData)
    {
        return IsDragging || (eventData != null && eventData.dragging && eventData.pointerDrag != null);
    }

    public static void Begin(object source, DragKind kind)
    {
        if (source == null) return;
        bool wasDragging = IsDragging;
        Sources[source] = kind;
        if (wasDragging) return;

        // 드래그 시작 전에 이미 나타난 툴팁을 즉시 숨깁니다.
        var panels = Object.FindObjectsByType<LobbyInfoPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var panel in panels)
            if (panel != null) panel.HideTooltipForReadyDrag();
    }

    public static void End(object source)
    {
        if (source != null) Sources.Remove(source);
    }
}
