using System;
using System.Collections.Generic;
using Relic.Gameplay.Battle;
using UnityEngine;

namespace Relic.Gameplay.Data
{
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

    [CreateAssetMenu(menuName = "Relic/Data/Manual Battle Map Template")]
    public class ManualBattleMapTemplate : ScriptableObject
    {
        [SerializeField] private List<ManualBattleMapNodeDefinition> nodes = new();

        public List<ManualBattleMapNodeDefinition> Nodes => nodes;

        public bool TryBuildNodes(
            List<MapData> mapPool,
            string chapter,
            string stage,
            out List<GeneratedMapNodeData> generatedNodes)
        {
            generatedNodes = new List<GeneratedMapNodeData>();

            if (nodes == null || nodes.Count == 0)
                return false;

            if (!TryValidateDefinitions(out HashSet<int> definedNodeIndices))
                return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                ManualBattleMapNodeDefinition definition = nodes[i];

                if (!TryResolveMap(mapPool, chapter, stage, definition, out string mapId, out string type))
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

        private bool TryValidateDefinitions(out HashSet<int> definedNodeIndices)
        {
            definedNodeIndices = new HashSet<int>();

            for (int i = 0; i < nodes.Count; i++)
            {
                ManualBattleMapNodeDefinition definition = nodes[i];

                if (definition == null)
                    return false;

                if (definition.NodeIndex < 0 ||
                    definition.LayerIndex < 0 ||
                    definition.RowIndex < 0 ||
                    string.IsNullOrWhiteSpace(definition.Type))
                {
                    return false;
                }

                if (!definedNodeIndices.Add(definition.NodeIndex))
                    return false;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                ManualBattleMapNodeDefinition definition = nodes[i];

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
            if (definition.UseCustomPosition)
                return definition.CustomPosition;

            return BattleMapLayoutUtility.CalculatePosition(
                definition.LayerIndex,
                definition.RowIndex,
                GetLayerRowCount(definition.LayerIndex));
        }

        private int GetLayerRowCount(int layerIndex)
        {
            int rowCount = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                ManualBattleMapNodeDefinition candidate = nodes[i];

                if (candidate == null || candidate.LayerIndex != layerIndex)
                    continue;

                rowCount = Mathf.Max(rowCount, candidate.RowIndex + 1);
            }

            return Mathf.Max(1, rowCount);
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
            out string mapId,
            out string resolvedType)
        {
            mapId = string.Empty;
            resolvedType = string.Empty;

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
                    return true;
                }

                return false;
            }

            if (string.Equals(type, "Start", StringComparison.OrdinalIgnoreCase))
            {
                if (TryPickCandidate(mapPool, chapter, stage, type, FixedPosition.Front, out MapData startMap))
                {
                    mapId = startMap.MapId;
                    resolvedType = startMap.Type;
                    return true;
                }

                mapId = "Start";
                resolvedType = "Start";
                return true;
            }

            if (string.Equals(type, "Boss", StringComparison.OrdinalIgnoreCase) &&
                TryPickCandidate(mapPool, chapter, stage, type, FixedPosition.Final, out MapData finalMap))
            {
                mapId = finalMap.MapId;
                resolvedType = finalMap.Type;
                return true;
            }

            if (TryPickCandidate(mapPool, chapter, stage, type, FixedPosition.None, out MapData mapData))
            {
                mapId = mapData.MapId;
                resolvedType = mapData.Type;
                return true;
            }

            return false;
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

                if (!Same(candidate.Chapter, chapter) || !Same(candidate.Stage, stage))
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
            FixedPosition fixedPosition,
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

                if (!Same(candidate.Chapter, chapter) ||
                    !Same(candidate.Stage, stage) ||
                    !Same(candidate.Type, type) ||
                    candidate.FixedPosition != fixedPosition)
                {
                    continue;
                }

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
            ManualBattleMapTemplate manualMapTemplate)
        {
            return GenerateResult(mapPool, chapter, stage, manualMapTemplate).Nodes;
        }

        public static BattleMapGenerationResult GenerateResult(
            List<MapData> mapPool,
            string chapter,
            string stage,
            ManualBattleMapTemplate manualMapTemplate)
        {
            if (manualMapTemplate != null &&
                manualMapTemplate.TryBuildNodes(mapPool, chapter, stage, out List<GeneratedMapNodeData> manualNodes))
            {
                return new BattleMapGenerationResult(manualNodes, true);
            }

            if (manualMapTemplate != null)
                Debug.LogWarning("[BattleMapGenerationResolver] Manual map template is invalid. Falling back to procedural generation.");

            ProceduralMapGenerator generator = new();
            return new BattleMapGenerationResult(
                generator.Generate(mapPool, chapter, stage),
                false);
        }
    }
}
