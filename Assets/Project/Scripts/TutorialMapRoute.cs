using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

/// <summary>
/// Resolves the initial tutorial battle, tutorial event, and final tutorial battle
/// from the Tutorial and Map worksheets without fixed map identifiers.
/// </summary>
public static class TutorialMapRoute
{
    public static bool TryGetNext(DataManager data, string currentMapId, out string nextMapId)
    {
        nextMapId = null;
        if (!TryResolve(data, out string first, out string eventMap, out string last))
            return false;
        if (string.Equals(currentMapId, first, StringComparison.OrdinalIgnoreCase))
            nextMapId = eventMap;
        else if (string.Equals(currentMapId, eventMap, StringComparison.OrdinalIgnoreCase))
            nextMapId = last;
        return !string.IsNullOrEmpty(nextMapId);
    }

    public static bool IsFinalBattle(DataManager data, string currentMapId)
    {
        return TryResolve(data, out _, out _, out string last) &&
            string.Equals(last, currentMapId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolve(DataManager data, out string first, out string eventMap, out string last)
    {
        first = null;
        eventMap = null;
        last = null;
        if (data?.TutorialDatabase == null || data.MapDatabase == null) return false;
        first = data.TutorialDatabase.GetFirstEntry()?.MapId;
        if (string.IsNullOrWhiteSpace(first)) return false;
        List<MapData> maps = data.MapDatabase.GetAll();
        foreach (MapData map in maps)
        {
            if (map == null || !string.Equals(map.Type, "Tutorial", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(map.MapId, first, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(map.EventId) && map.EventId != "0")
            {
                if (eventMap != null) { Debug.LogError("[TutorialMapRoute] Multiple tutorial event maps found."); return false; }
                eventMap = map.MapId;
            }
            else if (!string.IsNullOrWhiteSpace(map.BattleMapId) && map.BattleMapId != "0")
            {
                if (last != null) { Debug.LogError("[TutorialMapRoute] Multiple subsequent tutorial battles found."); return false; }
                last = map.MapId;
            }
        }
        return eventMap != null && last != null;
    }
}
