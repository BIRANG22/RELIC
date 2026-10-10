using System;
using System.Collections.Generic;
using System.Globalization;
using Relic.Gameplay.Battle;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class EventMapRandomExclusionEntry
    {
        public string EventId;
        public bool Disabled;
    }

    [Serializable]
    public class EventMapRandomExclusionSettings
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private List<EventMapRandomExclusionEntry> entries = CreateDefaultEntries();

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        public List<EventMapRandomExclusionEntry> Entries => entries;

        public bool IsMapAllowedForRandomSelection(MapData mapData)
        {
            if (mapData == null)
                return false;

            return !IsEventDisabled(mapData.EventId);
        }

        public bool IsEventDisabled(string eventId)
        {
            if (!enabled || string.IsNullOrWhiteSpace(eventId) || entries == null)
                return false;

            string normalizedEventId = EventIdUtility.Normalize(eventId);

            if (string.IsNullOrWhiteSpace(normalizedEventId))
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                EventMapRandomExclusionEntry entry = entries[i];

                if (entry == null || !entry.Disabled)
                    continue;

                string disabledEventId = EventIdUtility.Normalize(entry.EventId);

                if (string.IsNullOrWhiteSpace(disabledEventId))
                    continue;

                if (string.Equals(disabledEventId, normalizedEventId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public string GetRuntimeKey()
        {
            if (!enabled || entries == null || entries.Count == 0)
                return string.Empty;

            List<string> disabledEventIds = CollectDisabledEventIds();

            if (disabledEventIds.Count == 0)
                return string.Empty;

            disabledEventIds.Sort(StringComparer.Ordinal);

            uint hash = 2166136261;
            for (int i = 0; i < disabledEventIds.Count; i++)
                hash = AppendHash(hash, disabledEventIds[i]);

            return $"EventMapRandomExclusion:{hash:X8}";
        }

        private List<string> CollectDisabledEventIds()
        {
            List<string> result = new();

            if (entries == null)
                return result;

            for (int i = 0; i < entries.Count; i++)
            {
                EventMapRandomExclusionEntry entry = entries[i];

                if (entry == null || !entry.Disabled)
                    continue;

                string normalizedEventId = EventIdUtility.Normalize(entry.EventId);

                if (string.IsNullOrWhiteSpace(normalizedEventId))
                    continue;

                if (!result.Contains(normalizedEventId))
                    result.Add(normalizedEventId);
            }

            return result;
        }

        private static List<EventMapRandomExclusionEntry> CreateDefaultEntries()
        {
            return new List<EventMapRandomExclusionEntry>
            {
                new() { EventId = "Event_01" },
                new() { EventId = "Event_02" },
                new() { EventId = "Event_03" },
                new() { EventId = "Event_04" },
                new() { EventId = "Event_05" },
                new() { EventId = "Event_06" },
                new() { EventId = "Event_07" },
                new() { EventId = "Event_08" },
                new() { EventId = "Event_09" }
            };
        }

        private static uint AppendHash(uint hash, string value)
        {
            if (string.IsNullOrEmpty(value))
                return AppendHash(hash, 0);

            unchecked
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }

                return hash;
            }
        }

        private static uint AppendHash(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                return hash * 16777619;
            }
        }
    }

    [Serializable]
    public class ManualBattleMapNodeDefinition
    {
        public int NodeIndex;
        public int LayerIndex;
        public int RowIndex;
        public string Type = "Common";
        public string MapIdOverride;
        public bool UseCustomPosition;
        public Vector2 CustomPosition;
        public List<int> NextNodeIndices = new();
    }

    [Serializable]
    public class ManualBattleMapFixedNodeDefinition
    {
        public string Type = "Common";
        public string MapIdOverride;
        public bool UseCustomPosition;
        public Vector2 CustomPosition;
        public List<int> NextNodeIndices = new();
    }

    [Serializable]
    public class ManualBattleMapSlotDefinition
    {
        public bool Enabled;
        public string Type = "Common";
        public string MapIdOverride;
        public bool UseCustomPosition;
        public Vector2 CustomPosition;
        public List<int> NextNodeIndices = new();
    }

    [Serializable]
    public class ManualBattleMapLayerSlots
    {
        public ManualBattleMapSlotDefinition Slot1 = new();
        public ManualBattleMapSlotDefinition Slot2 = new();
        public ManualBattleMapSlotDefinition Slot3 = new();
        public ManualBattleMapSlotDefinition Slot4 = new();

        public ManualBattleMapSlotDefinition GetSlot(int rowIndex)
        {
            return rowIndex switch
            {
                0 => Slot1,
                1 => Slot2,
                2 => Slot3,
                3 => Slot4,
                _ => null
            };
        }
    }

    [CreateAssetMenu(menuName = "Relic/Data/Manual Battle Map Template")]
    public class ManualBattleMapTemplate : ScriptableObject
    {
        private const int StartLayerIndex = 0;
        private const int DefaultBossLayerIndex = 9;
        private const int MinBossLayerIndex = 1;
        private const int MaxBossLayerIndex = 9;
        private const int FixedMiddleLayerRowCount = 4;
        private const int StartNodeIndex = 0;

        [Header("Layer 0")]
        [SerializeField] private ManualBattleMapFixedNodeDefinition layer0Start = new() { Type = "Special" };

        [Header("Layer 1")]
        [SerializeField] private ManualBattleMapLayerSlots layer1 = new();
        [Header("Layer 2")]
        [SerializeField] private ManualBattleMapLayerSlots layer2 = new();
        [Header("Layer 3")]
        [SerializeField] private ManualBattleMapLayerSlots layer3 = new();
        [Header("Layer 4")]
        [SerializeField] private ManualBattleMapLayerSlots layer4 = new();
        [Header("Layer 5")]
        [SerializeField] private ManualBattleMapLayerSlots layer5 = new();
        [Header("Layer 6")]
        [SerializeField] private ManualBattleMapLayerSlots layer6 = new();
        [Header("Layer 7")]
        [SerializeField] private ManualBattleMapLayerSlots layer7 = new();
        [Header("Layer 8")]
        [SerializeField] private ManualBattleMapLayerSlots layer8 = new();
        [Header("Layer 9 - Boss")]
        [FormerlySerializedAs("layer13Boss")]
        [SerializeField] private ManualBattleMapFixedNodeDefinition layer9Boss = new() { Type = "Boss" };

        public int BossLayerIndex
        {
            get => DefaultBossLayerIndex;
            set { }
        }

        public List<ManualBattleMapNodeDefinition> Nodes => BuildDefinitions();

        public string GetRuntimeKey()
        {
            string templateName = string.IsNullOrWhiteSpace(name)
                ? GetInstanceID().ToString(CultureInfo.InvariantCulture)
                : name.Trim();

            return $"ManualBattleMapTemplate:RoomSequenceV5_TwoEliteRoutePossible:{templateName}:{CalculateContentHash():X8}";
        }

        public bool TryBuildNodes(
            List<MapData> mapPool,
            string chapter,
            string stage,
            out List<GeneratedMapNodeData> generatedNodes)
        {
            return TryBuildNodes(mapPool, chapter, stage, null, out generatedNodes);
        }

        public bool TryBuildNodes(
            List<MapData> mapPool,
            string chapter,
            string stage,
            EventMapRandomExclusionSettings randomExclusionSettings,
            out List<GeneratedMapNodeData> generatedNodes)
        {
            generatedNodes = new List<GeneratedMapNodeData>();
            // Roll invalid random layouts again instead of creating disconnected nodes.
            List<ManualBattleMapNodeDefinition> definitions = null;
            List<(int from, int to)> connections = null;
            // Some expeditions must offer a route through two non-adjacent elite rooms.
            // Other expeditions retain unrestricted valid paths for map variety.
            bool requireTwoEliteRoute = BattleRandom.Value() < 0.5f;
            for (int attempt = 0; attempt < 4096; attempt++)
            {
                List<ManualBattleMapNodeDefinition> candidate = BuildRuntimeDefinitions();
                if (candidate.Count == 0 || !TryValidateDefinitions(candidate, out _))
                    continue;
                if (!TryBuildAdjacentConnections(candidate, out List<(int from, int to)> candidateConnections))
                    continue;
                if (!IsRoomSequenceValid(candidate, candidateConnections, mapPool, stage, requireTwoEliteRoute))
                    continue;
                definitions = candidate;
                connections = candidateConnections;
                break;
            }
            // If no two-elite layout exists with the current manual overrides,
            // do not prevent map generation: use any layout satisfying the base rules.
            if (definitions == null && requireTwoEliteRoute)
            {
                for (int attempt = 0; attempt < 4096; attempt++)
                {
                    List<ManualBattleMapNodeDefinition> candidate = BuildRuntimeDefinitions();
                    if (candidate.Count == 0 || !TryValidateDefinitions(candidate, out _))
                        continue;
                    if (!TryBuildAdjacentConnections(candidate, out List<(int from, int to)> candidateConnections))
                        continue;
                    if (!IsRoomSequenceValid(candidate, candidateConnections, mapPool, stage, false))
                        continue;
                    definitions = candidate;
                    connections = candidateConnections;
                    break;
                }
            }
            if (definitions == null)
            {
                Debug.LogError("[ManualBattleMapTemplate] Could not generate a valid 10-layer layout with 2-3 elite rooms in distinct slots, no connected elites, a Layer 1 battle, mixed battle/event layers (3 slots), and no three consecutive battles/events. Check manual overrides and available maps.");
                return false;
            }

            // Center only layers with exactly one generated node.
            // Multi-node layers retain their original 1-4 slot coordinates.
            Dictionary<int, int> nodeCountByLayer = new();
            foreach (ManualBattleMapNodeDefinition node in definitions)
            {
                nodeCountByLayer.TryGetValue(node.LayerIndex, out int count);
                nodeCountByLayer[node.LayerIndex] = count + 1;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                ManualBattleMapNodeDefinition definition = definitions[i];

                if (!TryResolveMap(
                    mapPool,
                    chapter,
                    stage,
                    definition,
                    randomExclusionSettings,
                    out string mapId,
                    out string type,
                    out string eventId))
                {
                    generatedNodes.Clear();
                    return false;
                }

                generatedNodes.Add(new GeneratedMapNodeData
                {
                    NodeIndex = definition.NodeIndex,
                    LayerIndex = definition.LayerIndex,
                    MapId = mapId,
                    Type = type,
                    EventId = eventId,
                    IsMapIdOverride = !string.IsNullOrWhiteSpace(definition.MapIdOverride),
                    Position = ResolvePosition(definition, nodeCountByLayer[definition.LayerIndex] == 1),
                    NextNodeIndices = new List<int>()
                });
            }

            // Replaced rooms have new slot counts. Restore adjacent-layer connections
            // without editing the serialized manual template.
            Dictionary<int, GeneratedMapNodeData> generatedByIndex = new();
            foreach (GeneratedMapNodeData node in generatedNodes)
                generatedByIndex[node.NodeIndex] = node;
            foreach (var connection in connections)
                generatedByIndex[connection.from].NextNodeIndices.Add(connection.to);

            generatedNodes.Sort((left, right) =>
            {
                int layerCompare = left.LayerIndex.CompareTo(right.LayerIndex);
                if (layerCompare != 0)
                    return layerCompare;

                return left.NodeIndex.CompareTo(right.NodeIndex);
            });

            return generatedNodes.Count > 0;
        }

        private uint CalculateContentHash()
        {
            unchecked
            {
                uint hash = 2166136261;
                List<ManualBattleMapNodeDefinition> definitions = BuildDefinitions();

                hash = AppendHash(hash, BossLayerIndex + 1);
                hash = AppendHash(hash, definitions.Count);

                for (int i = 0; i < definitions.Count; i++)
                {
                    ManualBattleMapNodeDefinition node = definitions[i];
                    hash = AppendHash(hash, node.NodeIndex);
                    hash = AppendHash(hash, node.LayerIndex);
                    hash = AppendHash(hash, node.RowIndex);
                    hash = AppendHash(hash, node.Type);
                    hash = AppendHash(hash, node.MapIdOverride);
                    hash = AppendHash(hash, node.UseCustomPosition ? 1 : 0);
                    hash = AppendHash(hash, Mathf.RoundToInt(node.CustomPosition.x * 1000f));
                    hash = AppendHash(hash, Mathf.RoundToInt(node.CustomPosition.y * 1000f));
                    hash = AppendHash(hash, node.NextNodeIndices?.Count ?? 0);

                    if (node.NextNodeIndices == null)
                        continue;

                    for (int j = 0; j < node.NextNodeIndices.Count; j++)
                        hash = AppendHash(hash, node.NextNodeIndices[j]);
                }

                return hash;
            }
        }

        private static uint AppendHash(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                return hash * 16777619;
            }
        }

        private static uint AppendHash(uint hash, string value)
        {
            if (string.IsNullOrEmpty(value))
                return AppendHash(hash, 0);

            unchecked
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }

                return hash;
            }
        }

        private List<ManualBattleMapNodeDefinition> BuildDefinitions()
        {
            List<ManualBattleMapNodeDefinition> result = new();
            int resolvedBossLayerIndex = BossLayerIndex;

            result.Add(CreateFixedNode(layer0Start, StartNodeIndex, StartLayerIndex, 0));

            for (int layerIndex = StartLayerIndex + 1; layerIndex < resolvedBossLayerIndex; layerIndex++)
                AddMiddleLayer(result, GetMiddleLayer(layerIndex), layerIndex);

            result.Add(CreateFixedNode(
                layer9Boss,
                GetReservedBossNodeIndex(resolvedBossLayerIndex),
                resolvedBossLayerIndex,
                0));
            return result;
        }

        // Runtime-only selection. Explicit map IDs remain intact unless a room is removed.
        private List<ManualBattleMapNodeDefinition> BuildRuntimeDefinitions()
        {
            List<ManualBattleMapNodeDefinition> definitions = BuildDefinitions();
            for (int layer = 1; layer < BossLayerIndex; layer++)
            {
                bool replaceShop = layer == 4 && BattleErosionEffectService.ShouldRemoveShopRoom;
                bool replaceRest = layer == 8 && BattleErosionEffectService.ShouldRemoveRestRoom;
                bool singleRoom = (layer == 4 || layer == 8) && !replaceShop && !replaceRest;
                bool randomLayer = layer <= 3 || (layer >= 5 && layer <= 7);
                if (!replaceShop && !replaceRest && !singleRoom && !randomLayer)
                    continue;

                List<ManualBattleMapNodeDefinition> existing = definitions.FindAll(n => n.LayerIndex == layer);
                int count = singleRoom ? 1 : BattleRandom.Range(2, 4);
                if (singleRoom && existing.Count == 0)
                {
                    Debug.LogWarning($"[ManualBattleMapTemplate] Layer {layer}: enable one slot and set its room type/map ID.");
                    continue;
                }


                definitions.RemoveAll(n => n.LayerIndex == layer);
                if (replaceShop || replaceRest)
                    existing.Clear();

                // Prioritize manually assigned IDs and types when limiting four slots.
                existing.Sort((left, right) =>
                {
                    int leftPriority = string.IsNullOrWhiteSpace(left.MapIdOverride) ? 1 : 0;
                    int rightPriority = string.IsNullOrWhiteSpace(right.MapIdOverride) ? 1 : 0;
                    return leftPriority != rightPriority ? leftPriority.CompareTo(rightPriority) : left.RowIndex.CompareTo(right.RowIndex);
                });
                HashSet<int> usedRows = new();
                for (int i = 0; i < existing.Count && usedRows.Count < count; i++)
                {
                    ManualBattleMapNodeDefinition source = existing[i];
                    // With two nodes, do not use the outermost slots together (1 and 4).
                    if (count == 2 &&
                        ((source.RowIndex == 0 && usedRows.Contains(3)) ||
                         (source.RowIndex == 3 && usedRows.Contains(0))))
                        continue;

                    // A procedural layer can retain explicit test IDs, but inherited room
                    // types from older serialized templates must not force Shop/Rest/Boss.
                    // Map IDs are intentionally not cleared unless erosion removes the room.
                    if (randomLayer && string.IsNullOrWhiteSpace(source.MapIdOverride) &&
                        !IsAllowedRandomRoomType(source.Type, layer))
                    {
                        source.Type = PickRandomRoomType(layer, false);
                    }
                    definitions.Add(source);
                    usedRows.Add(source.RowIndex);
                }
                List<int> freeRows = new();
                for (int row = 0; row < 4; row++)
                {
                    if (usedRows.Contains(row))
                        continue;
                    // A two-node layer must never occupy Slot 1 and Slot 4 only.
                    if (count == 2 &&
                        ((row == 0 && usedRows.Contains(3)) ||
                         (row == 3 && usedRows.Contains(0))))
                        continue;
                    freeRows.Add(row);
                }
                for (int i = freeRows.Count - 1; i > 0; i--)
                {
                    int other = BattleRandom.Range(0, i + 1);
                    (freeRows[i], freeRows[other]) = (freeRows[other], freeRows[i]);
                }
                for (int i = 0; usedRows.Count < count && i < freeRows.Count; i++)
                {
                    int row = freeRows[i];
                    // Check against the slots actually picked so far, not only the
                    // initial free-row list. Never select Slots 1 and 4 together
                    // when a layer has exactly two nodes.
                    if (count == 2 &&
                        ((row == 0 && usedRows.Contains(3)) ||
                         (row == 3 && usedRows.Contains(0))))
                        continue;

                    string roomType = PickRandomRoomType(layer, replaceShop || replaceRest);
                    definitions.Add(new ManualBattleMapNodeDefinition
                    {
                        NodeIndex = GetReservedMiddleNodeIndex(layer, row),
                        LayerIndex = layer,
                        RowIndex = row,
                        Type = roomType,
                        MapIdOverride = string.Empty,
                        NextNodeIndices = new List<int>()
                    });
                    usedRows.Add(row);
                }
            }
            return definitions;
        }

        private static bool IsAllowedRandomRoomType(string type, int layer)
        {
            if (string.Equals(type, "Common", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "Special", StringComparison.OrdinalIgnoreCase))
                return true;
            return layer >= 5 && layer <= 7 &&
                   string.Equals(type, "Elite", StringComparison.OrdinalIgnoreCase);
        }

        private static string PickRandomRoomType(int layer, bool replacementRoom)
        {
            float random = BattleRandom.Value();

            // Shop/rest removal: battle 50%, event 50%, no elite.
            if (replacementRoom)
                return random < 0.50f ? "Common" : "Special";

            // Layers 5-7: battle 35%, elite 35%, event 30%.
            if (layer >= 5 && layer <= 7)
            {
                if (random < 0.35f)
                    return "Common";
                if (random < 0.70f)
                    return "Elite";
                return "Special";
            }

            // Layers 1-3: battle 65%, event 35%.
            return random < 0.65f ? "Common" : "Special";
        }

        // Check the actual connected graph, not just the list of rooms per layer.
        // MapIdOverride is resolved against the DB so manually chosen maps are validated too.
        private static bool IsRoomSequenceValid(
            List<ManualBattleMapNodeDefinition> nodes,
            List<(int from, int to)> edges,
            List<MapData> mapPool,
            string stage,
            bool requireTwoEliteRoute)
        {
            Dictionary<int, string> types = new();
            Dictionary<int, int> layers = new();
            int eliteCount = 0;
            HashSet<int> eliteRows = new();
            bool layer1HasBattle = false;
            foreach (ManualBattleMapNodeDefinition node in nodes)
            {
                string type = node.Type?.Trim();
                if (!string.IsNullOrWhiteSpace(node.MapIdOverride))
                {
                    if (!TryFindMapById(mapPool, node.MapIdOverride.Trim(), stage, out MapData map))
                        return false;
                    type = map.Type?.Trim();
                }
                types[node.NodeIndex] = type;
                layers[node.NodeIndex] = node.LayerIndex;
                if (node.LayerIndex == 1 && Same(type, "Common"))
                    layer1HasBattle = true;
                if (node.LayerIndex >= 5 && node.LayerIndex <= 7 && Same(type, "Elite"))
                {
                    eliteCount++;
                    // Elite rooms cannot reuse a slot position anywhere in Layers 5-7.
                    if (!eliteRows.Add(node.RowIndex))
                        return false;
                }
            }

            // A random layer with 3 nodes must contain at least one
            // battle (Common) and one event (Special). Elite rooms do not
            // substitute for either category. Validate resolved DB types so
            // an explicit test MapIdOverride is also counted correctly.
            for (int layer = 1; layer <= 7; layer++)
            {
                if (layer == 4)
                    continue;

                int nodeCount = 0;
                bool hasBattle = false;
                bool hasEvent = false;
                foreach (ManualBattleMapNodeDefinition node in nodes)
                {
                    if (node.LayerIndex != layer)
                        continue;
                    nodeCount++;
                    string resolved = types[node.NodeIndex];
                    hasBattle |= Same(resolved, "Common");
                    hasEvent |= Same(resolved, "Special");
                }

                if (nodeCount >= 3 && (!hasBattle || !hasEvent))
                    return false;
            }

            if (!layer1HasBattle || eliteCount < 2 || eliteCount > 3)
                return false;

            Dictionary<int, List<int>> next = new();
            foreach (var edge in edges)
            {
                if (!next.TryGetValue(edge.from, out List<int> targets))
                {
                    targets = new List<int>();
                    next.Add(edge.from, targets);
                }
                targets.Add(edge.to);

                // An elite node must never lead immediately into another elite node.
                if (Same(types[edge.from], "Elite") && Same(types[edge.to], "Elite"))
                    return false;
            }

            // A two-elite route is possible only across Layers 5 -> 6 -> 7.
            // Layer 6 must be a non-elite room, so elites are never consecutive.
            if (requireTwoEliteRoute)
            {
                bool hasTwoEliteRoute = false;
                foreach (var first in edges)
                {
                    if (layers[first.from] != 5 || layers[first.to] != 6 ||
                        !Same(types[first.from], "Elite") || Same(types[first.to], "Elite"))
                        continue;
                    if (!next.TryGetValue(first.to, out List<int> thirdNodes))
                        continue;
                    foreach (int third in thirdNodes)
                    {
                        if (layers[third] == 7 && Same(types[third], "Elite"))
                        {
                            hasTwoEliteRoute = true;
                            break;
                        }
                    }
                    if (hasTwoEliteRoute)
                        break;
                }
                if (!hasTwoEliteRoute)
                    return false;
            }

            // Every route of length two is checked (A -> B -> C).
            // Shop/rest/elite/boss interrupt battle/event streaks.
            foreach (var first in edges)
            {
                if (!next.TryGetValue(first.to, out List<int> thirdNodes))
                    continue;
                string firstType = types[first.from];
                string secondType = types[first.to];
                if (!Same(firstType, secondType) ||
                    (!Same(firstType, "Common") && !Same(firstType, "Special")))
                    continue;
                foreach (int third in thirdNodes)
                    if (Same(firstType, types[third]))
                        return false;
            }
            return true;
        }

        // Only adjacent slot numbers may connect between multi-node layers.
        // Single-node special layers represent central hubs and connect to adjacent nodes.
        // Select a non-crossing set of edges that covers every node on both sides.
        private static bool TryBuildAdjacentConnections(
            List<ManualBattleMapNodeDefinition> definitions,
            out List<(int from, int to)> connections)
        {
            connections = new List<(int from, int to)>();
            Dictionary<int, List<ManualBattleMapNodeDefinition>> layers = new();
            foreach (ManualBattleMapNodeDefinition node in definitions)
            {
                if (!layers.TryGetValue(node.LayerIndex, out var group))
                {
                    group = new List<ManualBattleMapNodeDefinition>();
                    layers.Add(node.LayerIndex, group);
                }
                group.Add(node);
            }
            for (int layerIndex = StartLayerIndex; layerIndex < DefaultBossLayerIndex; layerIndex++)
            {
                if (!layers.TryGetValue(layerIndex, out var from) ||
                    !layers.TryGetValue(layerIndex + 1, out var to) ||
                    from.Count == 0 || to.Count == 0)
                    return false;
                from.Sort((a, b) => a.RowIndex.CompareTo(b.RowIndex));
                to.Sort((a, b) => a.RowIndex.CompareTo(b.RowIndex));

                List<(int a, int b)> candidates = new();
                bool hub = from.Count == 1 || to.Count == 1;
                for (int i = 0; i < from.Count; i++)
                    for (int j = 0; j < to.Count; j++)
                        if (hub || Math.Abs(from[i].RowIndex - to[j].RowIndex) <= 1)
                            candidates.Add((i, j));

                // At most 16 candidates (4x4), so exhaustive search is inexpensive.
                // Prefer layouts with more routes, choosing randomly among equal best ones.
                int bestEdgeCount = -1;
                List<int> bestMasks = new();
                for (int mask = 1; mask < (1 << candidates.Count); mask++)
                {
                    int fromCoverage = 0, toCoverage = 0, edgeCount = 0;
                    bool crosses = false;
                    for (int e = 0; e < candidates.Count && !crosses; e++)
                    {
                        if ((mask & (1 << e)) == 0) continue;
                        var current = candidates[e];
                        fromCoverage |= 1 << current.a;
                        toCoverage |= 1 << current.b;
                        edgeCount++;
                        for (int other = 0; other < e; other++)
                        {
                            if ((mask & (1 << other)) == 0) continue;
                            var previous = candidates[other];
                            if ((previous.a < current.a && previous.b > current.b) ||
                                (previous.a > current.a && previous.b < current.b))
                            {
                                crosses = true;
                                break;
                            }
                        }
                    }
                    if (crosses || fromCoverage != (1 << from.Count) - 1 ||
                        toCoverage != (1 << to.Count) - 1)
                        continue;
                    if (edgeCount > bestEdgeCount)
                    {
                        bestEdgeCount = edgeCount;
                        bestMasks.Clear();
                    }
                    if (edgeCount == bestEdgeCount)
                        bestMasks.Add(mask);
                }
                if (bestMasks.Count == 0)
                    return false;
                int selected = bestMasks[BattleRandom.Range(0, bestMasks.Count)];
                for (int e = 0; e < candidates.Count; e++)
                {
                    if ((selected & (1 << e)) == 0) continue;
                    var edge = candidates[e];
                    connections.Add((from[edge.a].NodeIndex, to[edge.b].NodeIndex));
                }
            }
            return true;
        }

        private ManualBattleMapLayerSlots GetMiddleLayer(int layerIndex)
        {
            return layerIndex switch
            {
                1 => layer1,
                2 => layer2,
                3 => layer3,
                4 => layer4,
                5 => layer5,
                6 => layer6,
                7 => layer7,
                8 => layer8,
                _ => null
            };
        }

        private static ManualBattleMapNodeDefinition CreateFixedNode(
            ManualBattleMapFixedNodeDefinition source,
            int nodeIndex,
            int layerIndex,
            int rowIndex)
        {
            source ??= new ManualBattleMapFixedNodeDefinition();

            return new ManualBattleMapNodeDefinition
            {
                NodeIndex = nodeIndex,
                LayerIndex = layerIndex,
                RowIndex = rowIndex,
                Type = source.Type,
                MapIdOverride = source.MapIdOverride,
                UseCustomPosition = source.UseCustomPosition,
                CustomPosition = source.CustomPosition,
                NextNodeIndices = source.NextNodeIndices == null
                    ? new List<int>()
                    : new List<int>(source.NextNodeIndices)
            };
        }

        private static void AddMiddleLayer(
            List<ManualBattleMapNodeDefinition> result,
            ManualBattleMapLayerSlots layer,
            int layerIndex)
        {
            if (result == null || layer == null)
                return;

            for (int rowIndex = 0; rowIndex < FixedMiddleLayerRowCount; rowIndex++)
            {
                ManualBattleMapSlotDefinition slot = layer.GetSlot(rowIndex);

                if (slot == null || !slot.Enabled)
                    continue;

                result.Add(new ManualBattleMapNodeDefinition
                {
                    NodeIndex = GetReservedMiddleNodeIndex(layerIndex, rowIndex),
                    LayerIndex = layerIndex,
                    RowIndex = rowIndex,
                    Type = slot.Type,
                    MapIdOverride = slot.MapIdOverride,
                    UseCustomPosition = slot.UseCustomPosition,
                    CustomPosition = slot.CustomPosition,
                    NextNodeIndices = slot.NextNodeIndices == null
                        ? new List<int>()
                        : new List<int>(slot.NextNodeIndices)
                });
            }
        }

        private static int GetReservedMiddleNodeIndex(int layerIndex, int rowIndex)
        {
            return 1 + ((layerIndex - 1) * FixedMiddleLayerRowCount) + rowIndex;
        }

        private static int GetReservedBossNodeIndex(int resolvedBossLayerIndex)
        {
            return 1 + ((resolvedBossLayerIndex - 1) * FixedMiddleLayerRowCount);
        }

        private bool TryValidateDefinitions(List<ManualBattleMapNodeDefinition> definitions, out HashSet<int> definedNodeIndices)
        {
            definedNodeIndices = new HashSet<int>();
            Dictionary<int, HashSet<int>> usedRowsByLayer = new();
            int startNodeCount = 0;
            int bossNodeCount = 0;

            for (int i = 0; i < definitions.Count; i++)
            {
                ManualBattleMapNodeDefinition definition = definitions[i];

                if (definition == null)
                    return false;

                if (definition.NodeIndex < 0 ||
                    definition.LayerIndex < StartLayerIndex ||
                    definition.LayerIndex > BossLayerIndex ||
                    definition.RowIndex < 0 ||
                    string.IsNullOrWhiteSpace(definition.Type))
                {
                    return false;
                }

                if (!definedNodeIndices.Add(definition.NodeIndex))
                    return false;

                if (!usedRowsByLayer.TryGetValue(definition.LayerIndex, out HashSet<int> usedRows))
                {
                    usedRows = new HashSet<int>();
                    usedRowsByLayer.Add(definition.LayerIndex, usedRows);
                }

                if (IsStartOrBossLayer(definition.LayerIndex))
                {
                    if (definition.RowIndex != 0)
                        return false;

                    if (!usedRows.Add(0))
                        return false;

                    if (definition.LayerIndex == StartLayerIndex)
                        startNodeCount++;
                    else
                        bossNodeCount++;

                    continue;
                }

                if (definition.RowIndex >= FixedMiddleLayerRowCount)
                    return false;

                if (!usedRows.Add(definition.RowIndex))
                    return false;
            }

            if (startNodeCount != 1 || bossNodeCount != 1)
                return false;

            for (int layerIndex = StartLayerIndex + 1; layerIndex < BossLayerIndex; layerIndex++)
            {
                if (usedRowsByLayer.TryGetValue(layerIndex, out HashSet<int> usedRows) &&
                    usedRows.Count > FixedMiddleLayerRowCount)
                {
                    return false;
                }
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                ManualBattleMapNodeDefinition definition = definitions[i];

                if (definition.NextNodeIndices == null)
                    continue;

                HashSet<int> seenNextIndices = new();

                for (int j = 0; j < definition.NextNodeIndices.Count; j++)
                {
                    int nextNodeIndex = definition.NextNodeIndices[j];

                    // Ȱȭ  Ű    ø ü ȿȭ ʴ´.
                    //    CopyValidConnections  Ȱ 常 .
                    if (!definedNodeIndices.Contains(nextNodeIndex))
                        continue;

                    if (!seenNextIndices.Add(nextNodeIndex))
                        return false;
                }
            }

            return true;
        }

        private Vector2 ResolvePosition(ManualBattleMapNodeDefinition definition, bool isOnlyNodeInLayer)
        {
            // Single-node layers are always centered, including manually selected slots.
            // The X coordinate remains on the layer's normal horizontal position.
            if (isOnlyNodeInLayer)
                return BattleMapLayoutUtility.CalculatePosition(definition.LayerIndex, 0, 1);

            if (!IsStartOrBossLayer(definition.LayerIndex) && definition.UseCustomPosition)
                return definition.CustomPosition;

            int rowIndex = IsStartOrBossLayer(definition.LayerIndex) ? 0 : definition.RowIndex;
            return BattleMapLayoutUtility.CalculatePosition(
                definition.LayerIndex,
                rowIndex,
                GetLayerRowCount(definition.LayerIndex));
        }

        private int GetLayerRowCount(int layerIndex)
        {
            if (IsStartOrBossLayer(layerIndex))
                return 1;

            if (layerIndex > StartLayerIndex && layerIndex < BossLayerIndex)
                return FixedMiddleLayerRowCount;

            return 1;
        }

        private bool IsStartOrBossLayer(int layerIndex)
        {
            return layerIndex == StartLayerIndex || layerIndex == BossLayerIndex;
        }

        private List<int> CopyValidConnections(
            ManualBattleMapNodeDefinition definition,
            HashSet<int> definedNodeIndices)
        {
            List<int> result = new();

            if (definition.NextNodeIndices == null)
                return result;

            for (int i = 0; i < definition.NextNodeIndices.Count; i++)
            {
                int nextNodeIndex = definition.NextNodeIndices[i];

                if (definedNodeIndices.Contains(nextNodeIndex))
                    result.Add(nextNodeIndex);
            }

            return result;
        }

        private static bool TryResolveMap(
            List<MapData> mapPool,
            string chapter,
            string stage,
            ManualBattleMapNodeDefinition definition,
            EventMapRandomExclusionSettings randomExclusionSettings,
            out string mapId,
            out string resolvedType,
            out string eventId)
        {
            mapId = string.Empty;
            resolvedType = string.Empty;
            eventId = string.Empty;

            string type = definition.Type?.Trim();

            if (string.IsNullOrWhiteSpace(type))
                return false;

            if (!string.IsNullOrWhiteSpace(definition.MapIdOverride))
            {
                string overrideId = definition.MapIdOverride.Trim();

                // MapIdOverride DB    ϴ ̹Ƿ
                // ø ȭǾ  ִ Type  ġ ʿ䰡 .
                //   Ÿ DB MapData.Type ״ Ѵ.
                if (TryFindMapById(mapPool, overrideId, stage, out MapData overrideMap))
                {
                    mapId = overrideMap.MapId;
                    resolvedType = overrideMap.Type;
                    eventId = EventIdUtility.Normalize(overrideMap.EventId);
                    return true;
                }

                return false;
            }

            // Procedural nodes carry only their room type until the player enters.
            // Validate that at least one matching map is available, but do not draw an ID yet.
            if (HasRandomCandidate(mapPool, stage, type, randomExclusionSettings))
            {
                resolvedType = type;
                return true;
            }

            return false;
        }

        private static bool HasRandomCandidate(
            List<MapData> mapPool,
            string stage,
            string type,
            EventMapRandomExclusionSettings exclusions)
        {
            if (mapPool == null)
                return false;

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData map = mapPool[i];
                if (map == null || !Same(map.Stage, stage) || !Same(map.Type, type))
                    continue;
                if (Same(type, "Common") && MapRoomSelectionResolver.IsTutorialCombatMap(map))
                    continue;
                if (Same(type, "Special") && !MapRoomSelectionResolver.IsNormalRandomEventMap(map))
                    continue;
                if (!IsRandomCandidateAllowed(map, exclusions))
                    continue;
                return true;
            }
            return false;
        }

        private static bool TryFindMapById(
            List<MapData> mapPool,
            string mapId,
            string stage,
            out MapData result)
        {
            result = null;

            if (mapPool == null)
                return false;

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData candidate = mapPool[i];

                if (candidate == null)
                    continue;

                if (!Same(candidate.MapId, mapId))
                    continue;

                if (!Same(candidate.Stage, stage))
                    continue;

                result = candidate;
                return true;
            }

            return false;
        }

        private static bool TryPickCandidate(
            List<MapData> mapPool,
            string chapter,
            string stage,
            string type,
            EventMapRandomExclusionSettings randomExclusionSettings,
            out MapData result)
        {
            result = null;

            if (mapPool == null)
                return false;

            List<MapData> candidates = new();

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData candidate = mapPool[i];

                if (candidate == null)
                    continue;

                if (!Same(candidate.Stage, stage) ||
                    !Same(candidate.Type, type))
                {
                    continue;
                }

                if (Same(type, "Special") && !MapRoomSelectionResolver.IsNormalRandomEventMap(candidate))
                    continue;

                if (!IsRandomCandidateAllowed(candidate, randomExclusionSettings))
                    continue;

                candidates.Add(candidate);
            }

            if (candidates.Count == 0)
                return false;

            result = PickByWeight(candidates);
            return result != null;
        }

        private static MapData PickByWeight(List<MapData> candidates)
        {
            int totalWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
                totalWeight += Mathf.Max(0, candidates[i].SpawnWeight);

            if (totalWeight <= 0)
                return candidates[BattleRandom.Range(0, candidates.Count)];

            int random = BattleRandom.Range(0, totalWeight);
            int current = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                current += Mathf.Max(0, candidates[i].SpawnWeight);

                if (random < current)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        private static bool IsRandomCandidateAllowed(
            MapData candidate,
            EventMapRandomExclusionSettings randomExclusionSettings)
        {
            return randomExclusionSettings == null ||
                   randomExclusionSettings.IsMapAllowedForRandomSelection(candidate);
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ManualBattleMapLayerSlots))]
        public class ManualBattleMapLayerSlotsDrawer : PropertyDrawer
        {
            private const float VerticalSpacing = 2f;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                float height = EditorGUIUtility.singleLineHeight;

                if (!property.isExpanded)
                    return height;

                height += VerticalSpacing;
                height += GetChildHeight(property, "Slot1");
                height += VerticalSpacing;
                height += GetChildHeight(property, "Slot2");
                height += VerticalSpacing;
                height += GetChildHeight(property, "Slot3");
                height += VerticalSpacing;
                height += GetChildHeight(property, "Slot4");
                return height;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    int baseNodeIndex = GetBaseNodeIndex(property.name);
                    float y = foldoutRect.yMax + VerticalSpacing;

                    y = DrawChild(position, property, "Slot1", baseNodeIndex + 0, y);
                    y = DrawChild(position, property, "Slot2", baseNodeIndex + 1, y);
                    y = DrawChild(position, property, "Slot3", baseNodeIndex + 2, y);
                    DrawChild(position, property, "Slot4", baseNodeIndex + 3, y);

                    EditorGUI.indentLevel--;
                }

                EditorGUI.EndProperty();
            }

            private static float GetChildHeight(SerializedProperty property, string childName)
            {
                SerializedProperty child = property.FindPropertyRelative(childName);
                return child == null
                    ? EditorGUIUtility.singleLineHeight
                    : EditorGUI.GetPropertyHeight(child, true);
            }

            private static float DrawChild(Rect totalRect, SerializedProperty property, string childName, int nodeIndex, float y)
            {
                SerializedProperty child = property.FindPropertyRelative(childName);
                if (child == null)
                    return y + EditorGUIUtility.singleLineHeight;

                float height = EditorGUI.GetPropertyHeight(child, true);
                Rect childRect = new Rect(totalRect.x, y, totalRect.width, height);
                EditorGUI.PropertyField(childRect, child, new GUIContent(nodeIndex.ToString()), true);
                return childRect.yMax + VerticalSpacing;
            }

            private static int GetBaseNodeIndex(string propertyName)
            {
                if (string.IsNullOrWhiteSpace(propertyName) || !propertyName.StartsWith("layer", StringComparison.OrdinalIgnoreCase))
                    return 1;

                if (!int.TryParse(propertyName.Substring(5), out int layerIndex))
                    return 1;

                if (layerIndex <= 0)
                    return 1;

                return 1 + ((layerIndex - 1) * 4);
            }
        }
#endif

        private static bool Same(string left, string right)
        {
            return string.Equals(
                left?.Trim(),
                right?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public readonly struct BattleMapGenerationResult
    {
        public BattleMapGenerationResult(
            List<GeneratedMapNodeData> nodes,
            bool usedManualTemplate)
        {
            Nodes = nodes;
            UsedManualTemplate = usedManualTemplate;
        }

        public List<GeneratedMapNodeData> Nodes { get; }
        public bool UsedManualTemplate { get; }
    }

    public static class BattleMapGenerationResolver
    {
        public static List<GeneratedMapNodeData> Generate(
            List<MapData> mapPool,
            string chapter,
            string stage,
            ManualBattleMapTemplate manualMapTemplate,
            EventMapRandomExclusionSettings randomExclusionSettings = null)
        {
            return GenerateResult(mapPool, chapter, stage, manualMapTemplate, randomExclusionSettings).Nodes;
        }

        public static BattleMapGenerationResult GenerateResult(
            List<MapData> mapPool,
            string chapter,
            string stage,
            ManualBattleMapTemplate manualMapTemplate,
            EventMapRandomExclusionSettings randomExclusionSettings = null)
        {
            if (manualMapTemplate != null &&
                manualMapTemplate.TryBuildNodes(
                    mapPool,
                    chapter,
                    stage,
                    randomExclusionSettings,
                    out List<GeneratedMapNodeData> manualNodes))
            {
                return new BattleMapGenerationResult(manualNodes, true);
            }

            if (manualMapTemplate != null)
            {
                // Never silently substitute the old procedural layout for a configured 10-layer template.
                Debug.LogError($"[BattleMapGenerationResolver] Manual template '{manualMapTemplate.name}' failed to generate. " +
                    "Check its layer slots, map IDs and connected-path constraints. " +
                    "The legacy procedural generator will NOT be used.");
                return new BattleMapGenerationResult(new List<GeneratedMapNodeData>(), false);
            }

            ProceduralMapGenerator generator = new();
            return new BattleMapGenerationResult(
                generator.Generate(mapPool, chapter, stage, randomExclusionSettings),
                false);
        }
    }
}
