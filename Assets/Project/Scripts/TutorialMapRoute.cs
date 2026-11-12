using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

/// <summary>Resolves an ordered alternating tutorial route from Map sheet.</summary>
public static class TutorialMapRoute
{
    private static List<MapData> GetRoute(DataManager data)
    {
        var result = new List<MapData>();
        if (data?.MapDatabase == null || data.TutorialDatabase == null) return result;
        string firstId = data.TutorialDatabase.GetFirstEntry()?.MapId;
        if (string.IsNullOrWhiteSpace(firstId)) return result;
        List<MapData> maps = data.MapDatabase.GetAll();
        foreach (MapData map in maps)
            if (map != null && string.Equals(map.Type, "Tutorial", StringComparison.OrdinalIgnoreCase))
                result.Add(map);
        result.Sort((a, b) => MapNumber(a.MapId).CompareTo(MapNumber(b.MapId)));
        int firstIndex = result.FindIndex(m => string.Equals(m.MapId, firstId, StringComparison.OrdinalIgnoreCase));
        if (firstIndex < 0) { Debug.LogError("[TutorialMapRoute] First tutorial map is absent from Map sheet: " + firstId); return new List<MapData>(); }
        if (firstIndex > 0) result.RemoveRange(0, firstIndex);
        if (result.Count == 0 || result.Count % 2 == 0) { Debug.LogError("[TutorialMapRoute] Expected odd number of Battle/Event tutorial maps."); return new List<MapData>(); }
        for (int i = 0; i < result.Count; i++)
        {
            bool battle = !string.IsNullOrWhiteSpace(result[i].BattleMapId) && result[i].BattleMapId != "0";
            bool eventRoom = !string.IsNullOrWhiteSpace(result[i].EventId) && result[i].EventId != "0";
            if (battle == eventRoom || battle != (i % 2 == 0))
            {
                Debug.LogError("[TutorialMapRoute] Expected alternating Battle/Event: " + result[i].MapId);
                return new List<MapData>();
            }
        }
        return result;
    }

    private static int MapNumber(string value)
    {
        if (!string.IsNullOrEmpty(value) && value.StartsWith("Map_", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(value.Substring(4), out int number)) return number;
        return int.MaxValue;
    }

    public static bool IsTutorialMap(DataManager data, string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId)) return false;
        return GetRoute(data).Exists(m => string.Equals(m.MapId, mapId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool TryGetNext(DataManager data, string currentMapId, out string nextMapId)
    {
        nextMapId = null;
        var route = GetRoute(data);
        for (int i = 0; i < route.Count - 1; i++)
            if (string.Equals(route[i].MapId, currentMapId, StringComparison.OrdinalIgnoreCase))
            {
                nextMapId = route[i + 1].MapId;
                return true;
            }
        return false;
    }

    public static bool IsFinalBattle(DataManager data, string currentMapId)
    {
        var route = GetRoute(data);
        return route.Count > 0 && string.Equals(route[route.Count - 1].MapId, currentMapId, StringComparison.OrdinalIgnoreCase);
    }
}
