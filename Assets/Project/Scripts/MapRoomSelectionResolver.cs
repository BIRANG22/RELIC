using System;
using System.Collections.Generic;
using Relic.Gameplay.Battle;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// 실제로 노드를 방문하는 순간 방 내용을 확정합니다.
    /// 지도에 생성만 되고 선택하지 않은 방은 사용 이력에 포함하지 않습니다.
    /// </summary>
    public static class MapRoomSelectionResolver
    {
        private static readonly HashSet<string> NormalEventMapIds = new(StringComparer.Ordinal)
        {
            "Map_17",
            "Map_18",
            "Map_20",
            "Map_21",
            "Map_23",
            "Map_24"
        };

        private const float MidHardChance = 0.10f;
        private const float LateUnvisitedWeakChance = 0.10f;

        public static bool IsNormalRandomEventMap(MapData map)
        {
            if (map == null || !Same(map.Type, "Special"))
                return false;

            return NormalEventMapIds.Contains(map.MapId?.Trim() ?? string.Empty);
        }

        public static MapData ResolveForVisit(
            GeneratedMapNodeData selectedNode,
            MapRuntimeData runtime,
            IReadOnlyList<MapData> mapPool)
        {
            if (selectedNode == null || runtime == null || mapPool == null)
                return null;

            string stage = runtime.CurrentStage?.Trim() ?? string.Empty;

            if (selectedNode.IsMapIdOverride)
                return FindByMapId(selectedNode.MapId, stage, mapPool);

            switch (selectedNode.Type)
            {
                case "Common":
                    return ResolveCommon(selectedNode.LayerIndex, stage, runtime, mapPool);

                case "Elite":
                    return ResolveElite(stage, runtime, mapPool);

                case "Special":
                    return ResolveEvent(stage, runtime, mapPool);

                default:
                    return FindByMapId(selectedNode.MapId, stage, mapPool);
            }
        }

        public static void ApplyToNode(GeneratedMapNodeData node, MapData resolvedMap)
        {
            if (node == null || resolvedMap == null)
                return;

            node.MapId = resolvedMap.MapId;
            node.Type = resolvedMap.Type;
            node.EventId = EventIdUtility.Normalize(resolvedMap.EventId);
        }

        private static MapData ResolveCommon(
            int layerIndex,
            string stage,
            MapRuntimeData runtime,
            IReadOnlyList<MapData> mapPool)
        {
            List<MapData> allCommon = CollectByType(mapPool, stage, "Common");
            if (allCommon.Count == 0)
                return null;

            HashSet<string> allKeys = CollectMapIds(allCommon);
            HashSet<string> usedThisCycle = CollectCurrentCycleUsedKeys(
                runtime,
                allKeys,
                mapPool,
                map => map != null && Same(map.Type, "Common"),
                map => map.MapId);

            List<MapData> unvisited = FilterMaps(allCommon, map => !usedThisCycle.Contains(map.MapId));
            if (unvisited.Count == 0)
                unvisited = allCommon;

            string group = PickCommonGroup(layerIndex, unvisited);
            List<MapData> grouped = FilterMaps(unvisited, map => Same(map.BattleGroup, group));

            if (grouped.Count == 0)
            {
                List<MapData> eligible = FilterMaps(unvisited, map => IsCommonGroupEligible(layerIndex, map.BattleGroup, unvisited));
                if (eligible.Count > 0)
                    grouped = eligible;
            }

            if (grouped.Count == 0)
                grouped = unvisited;

            return PickByWeight(grouped);
        }

        private static MapData ResolveElite(
            string stage,
            MapRuntimeData runtime,
            IReadOnlyList<MapData> mapPool)
        {
            List<MapData> elites = CollectByType(mapPool, stage, "Elite");
            if (elites.Count == 0)
                return null;

            HashSet<string> allGroups = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < elites.Count; i++)
            {
                string group = NormalizeGroup(elites[i].BattleGroup);
                if (!string.IsNullOrEmpty(group) && group != "0")
                    allGroups.Add(group);
            }

            if (allGroups.Count == 0)
                return PickByWeight(elites);

            HashSet<string> usedGroups = CollectCurrentCycleUsedKeys(
                runtime,
                allGroups,
                mapPool,
                map => map != null && Same(map.Type, "Elite"),
                map => NormalizeGroup(map.BattleGroup));

            List<string> availableGroups = new();
            foreach (string group in allGroups)
            {
                if (!usedGroups.Contains(group))
                    availableGroups.Add(group);
            }

            if (availableGroups.Count == 0)
                availableGroups.AddRange(allGroups);

            string selectedGroup = PickEliteGroupByWeight(elites, availableGroups);
            List<MapData> groupMaps = FilterMaps(elites, map => Same(map.BattleGroup, selectedGroup));
            return PickByWeight(groupMaps.Count > 0 ? groupMaps : elites);
        }

        private static MapData ResolveEvent(
            string stage,
            MapRuntimeData runtime,
            IReadOnlyList<MapData> mapPool)
        {
            List<MapData> events = new();
            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData map = mapPool[i];
                if (map == null || !Same(map.Stage, stage) || !Same(map.Type, "Special"))
                    continue;

                if (!NormalEventMapIds.Contains(map.MapId?.Trim() ?? string.Empty))
                    continue;

                events.Add(map);
            }

            if (events.Count == 0)
                return null;

            HashSet<string> allIds = CollectMapIds(events);
            HashSet<string> usedIds = CollectCurrentCycleUsedKeys(
                runtime,
                allIds,
                mapPool,
                map => map != null && Same(map.Type, "Special") && NormalEventMapIds.Contains(map.MapId?.Trim() ?? string.Empty),
                map => map.MapId);

            List<MapData> available = FilterMaps(events, map => !usedIds.Contains(map.MapId));
            if (available.Count == 0)
                available = events;

            return PickByWeight(available);
        }

        private static string PickCommonGroup(int layerIndex, List<MapData> candidates)
        {
            bool hasWeak = HasGroup(candidates, "Weak");
            bool hasNormal = HasGroup(candidates, "Normal");
            bool hasHard = HasGroup(candidates, "Hard");

            if (layerIndex <= 1)
                return hasWeak ? "Weak" : FirstAvailableGroup(hasNormal, hasHard);

            if (layerIndex <= 3)
                return PickGroupByChance(
                    ("Weak", hasWeak, 0.50f),
                    ("Normal", hasNormal, 0.50f));

            if (layerIndex <= 5)
                return PickGroupByChance(
                    ("Weak", hasWeak, 0.45f),
                    ("Normal", hasNormal, 0.45f),
                    ("Hard", hasHard, MidHardChance));

            if (hasWeak)
            {
                return PickGroupByChance(
                    ("Weak", true, LateUnvisitedWeakChance),
                    ("Normal", hasNormal, 0.45f),
                    ("Hard", hasHard, 0.45f));
            }

            return PickGroupByChance(
                ("Normal", hasNormal, 0.50f),
                ("Hard", hasHard, 0.50f));
        }

        private static bool IsCommonGroupEligible(int layerIndex, string battleGroup, List<MapData> candidates)
        {
            string group = NormalizeGroup(battleGroup);

            if (layerIndex <= 1)
                return group == "Weak";

            if (layerIndex <= 3)
                return group == "Weak" || group == "Normal";

            if (layerIndex <= 5)
                return group == "Weak" || group == "Normal" || group == "Hard";

            if (group == "Normal" || group == "Hard")
                return true;

            return group == "Weak" && HasGroup(candidates, "Weak");
        }

        private static string PickGroupByChance(params (string group, bool available, float weight)[] options)
        {
            float total = 0f;
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].available)
                    total += Math.Max(0f, options[i].weight);
            }

            if (total <= 0f)
                return string.Empty;

            float roll = BattleRandom.Range(0f, total);
            float current = 0f;

            for (int i = 0; i < options.Length; i++)
            {
                if (!options[i].available)
                    continue;

                current += Math.Max(0f, options[i].weight);
                if (roll < current)
                    return options[i].group;
            }

            for (int i = options.Length - 1; i >= 0; i--)
            {
                if (options[i].available)
                    return options[i].group;
            }

            return string.Empty;
        }

        private static string FirstAvailableGroup(bool hasNormal, bool hasHard)
        {
            if (hasNormal)
                return "Normal";
            if (hasHard)
                return "Hard";
            return string.Empty;
        }

        private static bool HasGroup(List<MapData> maps, string group)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (Same(maps[i]?.BattleGroup, group))
                    return true;
            }

            return false;
        }

        private static string PickEliteGroupByWeight(List<MapData> elites, List<string> availableGroups)
        {
            List<(string group, int weight)> weightedGroups = new();
            int total = 0;

            for (int i = 0; i < availableGroups.Count; i++)
            {
                string group = availableGroups[i];
                int weight = 0;

                // 같은 BattleGroup에 맵이 여러 개 있어도 같은 엘리트 한 종류로 취급합니다.
                // 그룹의 확률이 맵 개수 때문에 커지지 않도록 그룹 내부 최대 가중치만 사용합니다.
                for (int j = 0; j < elites.Count; j++)
                {
                    if (Same(elites[j].BattleGroup, group))
                        weight = Math.Max(weight, Math.Max(0, elites[j].SpawnWeight));
                }

                if (weight <= 0)
                    weight = 1;

                weightedGroups.Add((group, weight));
                total += weight;
            }

            if (weightedGroups.Count == 0)
                return string.Empty;

            int roll = BattleRandom.Range(0, Math.Max(1, total));
            int current = 0;

            for (int i = 0; i < weightedGroups.Count; i++)
            {
                current += weightedGroups[i].weight;
                if (roll < current)
                    return weightedGroups[i].group;
            }

            return weightedGroups[weightedGroups.Count - 1].group;
        }

        private static HashSet<string> CollectCurrentCycleUsedKeys(
            MapRuntimeData runtime,
            HashSet<string> allKeys,
            IReadOnlyList<MapData> mapPool,
            Func<MapData, bool> includeMap,
            Func<MapData, string> keySelector)
        {
            HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);

            if (runtime?.VisitedMapIds == null || runtime.GeneratedNodes == null || allKeys == null || allKeys.Count == 0)
                return used;

            for (int i = 0; i < runtime.VisitedMapIds.Count; i++)
            {
                if (!int.TryParse(runtime.VisitedMapIds[i], out int nodeIndex))
                    continue;

                GeneratedMapNodeData node = FindNode(runtime.GeneratedNodes, nodeIndex);
                if (node == null)
                    continue;

                MapData map = FindByMapId(node.MapId, runtime.CurrentStage, mapPool);
                if (map == null || !includeMap(map))
                    continue;

                string key = NormalizeGroup(keySelector(map));
                if (string.IsNullOrEmpty(key) || !allKeys.Contains(key))
                    continue;

                if (used.Count >= allKeys.Count)
                    used.Clear();

                used.Add(key);
            }

            if (used.Count >= allKeys.Count)
                used.Clear();

            return used;
        }

        private static GeneratedMapNodeData FindNode(List<GeneratedMapNodeData> nodes, int nodeIndex)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedMapNodeData node = nodes[i];
                if (node != null && node.NodeIndex == nodeIndex)
                    return node;
            }

            return null;
        }

        private static List<MapData> CollectByType(IReadOnlyList<MapData> mapPool, string stage, string type)
        {
            List<MapData> result = new();

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData map = mapPool[i];
                if (map != null && Same(map.Stage, stage) && Same(map.Type, type))
                    result.Add(map);
            }

            return result;
        }

        private static HashSet<string> CollectMapIds(List<MapData> maps)
        {
            HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < maps.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(maps[i]?.MapId))
                    result.Add(maps[i].MapId.Trim());
            }
            return result;
        }

        private static List<MapData> FilterMaps(List<MapData> maps, Func<MapData, bool> predicate)
        {
            List<MapData> result = new();
            for (int i = 0; i < maps.Count; i++)
            {
                MapData map = maps[i];
                if (map != null && predicate(map))
                    result.Add(map);
            }
            return result;
        }

        private static MapData PickByWeight(List<MapData> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            int total = 0;
            for (int i = 0; i < candidates.Count; i++)
                total += Math.Max(0, candidates[i].SpawnWeight);

            if (total <= 0)
                return candidates[BattleRandom.Range(0, candidates.Count)];

            int roll = BattleRandom.Range(0, total);
            int current = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                current += Math.Max(0, candidates[i].SpawnWeight);
                if (roll < current)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        private static MapData FindByMapId(string mapId, string stage, IReadOnlyList<MapData> mapPool)
        {
            if (string.IsNullOrWhiteSpace(mapId))
                return null;

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData map = mapPool[i];
                if (map == null || !Same(map.MapId, mapId))
                    continue;

                if (string.IsNullOrWhiteSpace(stage) || Same(map.Stage, stage))
                    return map;
            }

            return null;
        }

        private static string NormalizeGroup(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
