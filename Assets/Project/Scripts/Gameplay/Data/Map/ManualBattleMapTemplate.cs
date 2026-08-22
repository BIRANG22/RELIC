using System;
using System.Collections.Generic;
using System.Globalization;
using Relic.Gameplay.Battle;
using UnityEngine;
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
        private const int BossLayerIndex = 13;
        private const int TotalLayerCount = 14;
        private const int FixedMiddleLayerRowCount = 4;
        private const int StartNodeIndex = 0;
        private const int BossNodeIndex = 49;

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
        [Header("Layer 9")]
        [SerializeField] private ManualBattleMapLayerSlots layer9 = new();
        [Header("Layer 10")]
        [SerializeField] private ManualBattleMapLayerSlots layer10 = new();
        [Header("Layer 11")]
        [SerializeField] private ManualBattleMapLayerSlots layer11 = new();
        [Header("Layer 12")]
        [SerializeField] private ManualBattleMapLayerSlots layer12 = new();

        [Header("Layer 13 - Boss")]
        [SerializeField] private ManualBattleMapFixedNodeDefinition layer13Boss = new() { Type = "Boss" };

        public List<ManualBattleMapNodeDefinition> Nodes => BuildDefinitions();

        public string GetRuntimeKey()
        {
            string templateName = string.IsNullOrWhiteSpace(name)
                ? GetInstanceID().ToString(CultureInfo.InvariantCulture)
                : name.Trim();

            return $"ManualBattleMapTemplate:{templateName}:{CalculateContentHash():X8}";
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
            List<ManualBattleMapNodeDefinition> definitions = BuildDefinitions();

            if (definitions.Count == 0)
                return false;

            if (!TryValidateDefinitions(definitions, out HashSet<int> definedNodeIndices))
                return false;

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
                    Position = ResolvePosition(definition),
                    NextNodeIndices = CopyValidConnections(definition, definedNodeIndices)
                });
            }

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

                hash = AppendHash(hash, TotalLayerCount);
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

            result.Add(CreateFixedNode(layer0Start, StartNodeIndex, StartLayerIndex, 0));

            AddMiddleLayer(result, layer1, 1);
            AddMiddleLayer(result, layer2, 2);
            AddMiddleLayer(result, layer3, 3);
            AddMiddleLayer(result, layer4, 4);
            AddMiddleLayer(result, layer5, 5);
            AddMiddleLayer(result, layer6, 6);
            AddMiddleLayer(result, layer7, 7);
            AddMiddleLayer(result, layer8, 8);
            AddMiddleLayer(result, layer9, 9);
            AddMiddleLayer(result, layer10, 10);
            AddMiddleLayer(result, layer11, 11);
            AddMiddleLayer(result, layer12, 12);

            result.Add(CreateFixedNode(layer13Boss, BossNodeIndex, BossLayerIndex, 0));
            return result;
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

                    if (!definedNodeIndices.Contains(nextNodeIndex))
                        return false;

                    if (!seenNextIndices.Add(nextNodeIndex))
                        return false;
                }
            }

            return true;
        }

        private Vector2 ResolvePosition(ManualBattleMapNodeDefinition definition)
        {
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

        private static bool IsStartOrBossLayer(int layerIndex)
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

                if (TryFindMapById(mapPool, overrideId, chapter, stage, type, out MapData overrideMap))
                {
                    mapId = overrideMap.MapId;
                    resolvedType = overrideMap.Type;
                    eventId = EventIdUtility.Normalize(overrideMap.EventId);
                    return true;
                }

                return false;
            }

            if (IsBuiltInRoomType(type))
            {
                if (TryPickCandidate(mapPool, chapter, stage, type, randomExclusionSettings, out MapData builtInMap))
                {
                    mapId = builtInMap.MapId;
                    resolvedType = builtInMap.Type;
                    eventId = EventIdUtility.Normalize(builtInMap.EventId);
                    return true;
                }

                resolvedType = NormalizeBuiltInRoomType(type);
                mapId = resolvedType;
                return true;
            }

            if (TryPickCandidate(mapPool, chapter, stage, type, randomExclusionSettings, out MapData mapData))
            {
                mapId = mapData.MapId;
                resolvedType = mapData.Type;
                eventId = EventIdUtility.Normalize(mapData.EventId);
                return true;
            }

            return false;
        }

        private static bool IsBuiltInRoomType(string type)
        {
            return string.Equals(type, "Start", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(type, "Rest", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeBuiltInRoomType(string type)
        {
            if (string.Equals(type, "Rest", StringComparison.OrdinalIgnoreCase))
                return "Rest";

            return "Start";
        }

        private static bool TryFindMapById(
            List<MapData> mapPool,
            string mapId,
            string chapter,
            string stage,
            string type,
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

                if (!Same(candidate.Type, type))
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
                Debug.LogWarning("[BattleMapGenerationResolver] Manual map template is invalid. Falling back to procedural generation.");

            ProceduralMapGenerator generator = new();
            return new BattleMapGenerationResult(
                generator.Generate(mapPool, chapter, stage, randomExclusionSettings),
                false);
        }
    }
}
