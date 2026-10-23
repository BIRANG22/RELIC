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
        private static readonly HashSet<string> TutorialCombatMapIds = new(StringComparer.OrdinalIgnoreCase)
        {
            TutorialBattleEntrySetup.FirstTutorialMapId,
            TutorialBattleEntrySetup.SecondTutorialMapId,
            TutorialBattleEntrySetup.ThirdTutorialMapId
        };

        public static bool IsNormalRandomEventMap(MapData map)
        {
            return map != null && Same(map.Type, "Special");
        }

        public static bool IsTutorialCombatMap(MapData map)
        {
            return map != null && TutorialCombatMapIds.Contains(map.MapId?.Trim() ?? string.Empty);
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
            // 수동 지정 맵은 슬롯에 설정한 방 종류를 유지합니다.
            // 예: 데이터 Type=Test, 슬롯 Type=Common이면 일반 전투방으로 진입합니다.
            if (!node.IsMapIdOverride)
                node.Type = resolvedMap.Type;
            node.EventId = EventIdUtility.Normalize(resolvedMap.EventId);
        }

        private static MapData ResolveCommon(
            int layerIndex,
            string stage,
            MapRuntimeData runtime,
            IReadOnlyList<MapData> mapPool)
        {
            List<MapData> allCommon = FilterMaps(CollectByType(mapPool, stage, "Common"), map => !IsTutorialCombatMap(map));
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

            // 레이어 1~3의 난이도는 고정입니다. 해당 그룹을 모두 방문한 경우에도
            // 다른 난이도로 넘어가지 않고 해당 그룹 안에서만 재선택합니다.
            if (layerIndex >= 1 && layerIndex <= 3)
            {
                string requiredGroup = layerIndex == 1 ? "Weak" : "Normal";
                List<MapData> required = FilterMaps(unvisited, map => Same(map.BattleGroup, requiredGroup));
                if (required.Count == 0)
                    required = FilterMaps(allCommon, map => Same(map.BattleGroup, requiredGroup));
                return PickByWeight(required);
            }

            string group = PickCommonGroup(layerIndex, unvisited);
            List<MapData> grouped = FilterMaps(unvisited, map => Same(map.BattleGroup, group));

            if (grouped.Count == 0)
            {
                List<MapData> eligible = FilterMaps(unvisited, map => IsCommonGroupEligible(layerIndex, map.BattleGroup, unvisited));
                if (eligible.Count > 0)
                    grouped = eligible;
            }

            if (grouped.Count == 0)
                grouped = FilterMaps(allCommon, map => IsCommonGroupEligible(layerIndex, map.BattleGroup, allCommon));

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

            HashSet<string> eliteIds = CollectMapIds(elites);
            HashSet<string> visitedEliteIds = CollectCurrentCycleUsedKeys(
                runtime, eliteIds, mapPool,
                map => map != null && Same(map.Type, "Elite"), map => map.MapId);
            List<MapData> unusedElites = FilterMaps(elites, map => !visitedEliteIds.Contains(map.MapId));
            if (unusedElites.Count > 0)
                elites = unusedElites;

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

                if (!IsNormalRandomEventMap(map))
                    continue;

                events.Add(map);
            }

            if (events.Count == 0)
                return null;

            HashSet<string> allIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (MapData map in events)
                allIds.Add(EventIdUtility.Normalize(map.EventId));
            HashSet<string> usedIds = CollectCurrentCycleUsedKeys(
                runtime,
                allIds,
                mapPool,
                map => map != null && IsNormalRandomEventMap(map),
                map => EventIdUtility.Normalize(map.EventId));

            List<MapData> available = FilterMaps(events, map => !usedIds.Contains(EventIdUtility.Normalize(map.EventId)));
            if (available.Count == 0)
                available = events;

            return PickByWeight(available);
        }

        private static string PickCommonGroup(int layerIndex, List<MapData> candidates)
        {
            bool hasWeak = HasGroup(candidates, "Weak");
            bool hasNormal = HasGroup(candidates, "Normal");
            bool hasHard = HasGroup(candidates, "Hard");

            // 확률은 해당 레이어에서 사용 가능한 미방문 그룹에만 적용합니다.
            // Layer 1은 Weak, Layer 2~3은 Normal만 선택합니다.
            // Layer 5~7에서 Normal을 모두 사용했다면 Hard만 선택합니다.
            switch (layerIndex)
            {
                case 1:
                    return PickGroupByChance(("Weak", hasWeak, 1f));
                case 2:
                    return PickGroupByChance(("Normal", hasNormal, 1f));
                case 3:
                    return PickGroupByChance(("Normal", hasNormal, 1f));
                case 5:
                    return PickGroupByChance(("Normal", hasNormal, 0.80f), ("Hard", hasHard, 0.20f));
                case 6:
                    return PickGroupByChance(("Normal", hasNormal, 0.50f), ("Hard", hasHard, 0.50f));
                case 7:
                    return PickGroupByChance(("Normal", hasNormal, 0.25f), ("Hard", hasHard, 0.75f));
                default:
                    // 침식도로 교체된 방 등: 해당 구간에서 가까운 확률을 사용합니다.
                    return layerIndex < 3
                        ? PickGroupByChance(("Weak", hasWeak, 0.50f), ("Normal", hasNormal, 0.50f))
                        : PickGroupByChance(("Normal", hasNormal, 0.50f), ("Hard", hasHard, 0.50f));
            }
        }

        private static bool IsCommonGroupEligible(int layerIndex, string battleGroup, List<MapData> candidates)
        {
            string group = NormalizeGroup(battleGroup);
            if (layerIndex == 1)
                return Same(group, "Weak");
            if (layerIndex == 2 || layerIndex == 3)
                return Same(group, "Normal");
            return Same(group, "Normal") || Same(group, "Hard");
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
