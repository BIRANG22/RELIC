using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class ProceduralMapGenerator
    {
        private int nextNodeIndex;

        private const float YStart = -4f;
        private const float YGap = 2f;
        private const float XGap = 2.3f;

        public List<GeneratedMapNodeData> Generate(
            List<MapData> mapPool,
            string chapter,
            string stage)
        {
            nextNodeIndex = 0;

            List<GeneratedMapNodeData> result = new();

            if (mapPool == null || mapPool.Count == 0)
            {
                Debug.LogWarning("[ProceduralMapGenerator] MapPool이 비어 있습니다.");
                return result;
            }

            GeneratedMapNodeData startNode = CreateNode(
                "Start",
                "Start",
                new Vector2(0f, YStart)
            );

            result.Add(startNode);

            int pathCount = Random.Range(2, 4); // 2~3갈래
            int middleLayerCount = 4;           // 시작과 보스 사이 라인 수

            List<GeneratedMapNodeData> previousLayer = new() { startNode };

            for (int layer = 1; layer <= middleLayerCount; layer++)
            {
                List<GeneratedMapNodeData> currentLayer = new();

                bool isFrontLayer = layer == 1;
                bool isPenultimateLayer = layer == middleLayerCount;

                int nodeCount = Random.Range(2, pathCount + 1);

                for (int i = 0; i < nodeCount; i++)
                {
                    MapData selectedMap = PickMapDataForLayer(
                        mapPool,
                        chapter,
                        stage,
                        layer,
                        middleLayerCount
                    );

                    if (selectedMap == null)
                        continue;

                    Vector2 position = CalculatePosition(layer, i, nodeCount);

                    GeneratedMapNodeData node = CreateNode(
                        selectedMap.MapId,
                        selectedMap.Type,
                        position
                    );

                    currentLayer.Add(node);
                    result.Add(node);
                }

                if (currentLayer.Count == 0)
                {
                    Debug.LogWarning($"[ProceduralMapGenerator] Layer 생성 실패: {layer}");
                    continue;
                }

                ConnectLayers(previousLayer, currentLayer);
                previousLayer = currentLayer;
            }

            MapData finalMap = PickFixedMapData(
                mapPool,
                chapter,
                stage,
                FixedPosition.Final
            );

            if (finalMap == null)
            {
                Debug.LogWarning("[ProceduralMapGenerator] Final 맵 데이터가 없습니다.");
                return result;
            }

            GeneratedMapNodeData bossNode = CreateNode(
                finalMap.MapId,
                finalMap.Type,
                new Vector2(0f, YStart + (middleLayerCount + 1) * YGap)
            );

            result.Add(bossNode);
            ConnectLayers(previousLayer, new List<GeneratedMapNodeData> { bossNode });

            return result;
        }

        private GeneratedMapNodeData CreateNode(
            string mapId,
            string type,
            Vector2 position)
        {
            return new GeneratedMapNodeData
            {
                NodeIndex = nextNodeIndex++,
                MapId = mapId,
                Type = type,
                Position = position
            };
        }

        private MapData PickMapDataForLayer(
            List<MapData> mapPool,
            string chapter,
            string stage,
            int layer,
            int maxMiddleLayer)
        {
            if (layer == 1)
            {
                MapData frontMap = PickFixedMapData(
                    mapPool,
                    chapter,
                    stage,
                    FixedPosition.Front
                );

                if (frontMap != null)
                    return frontMap;
            }

            if (layer == maxMiddleLayer)
            {
                MapData penultimateMap = PickFixedMapData(
                    mapPool,
                    chapter,
                    stage,
                    FixedPosition.Penultimate
                );

                if (penultimateMap != null)
                    return penultimateMap;
            }

            string randomType = DecideRandomType();

            return PickRandomMapData(
                mapPool,
                chapter,
                stage,
                randomType
            );
        }

        private string DecideRandomType()
        {
            float r = Random.value;

            if (r < 0.50f) return "Common";
            if (r < 0.65f) return "Elite";
            if (r < 0.78f) return "Chest";
            if (r < 0.90f) return "Special";

            return "Rest";
        }

        private MapData PickRandomMapData(
            List<MapData> mapPool,
            string chapter,
            string stage,
            string type)
        {
            List<MapData> candidates = new();

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData data = mapPool[i];

                if (data.Chapter != chapter)
                    continue;

                if (data.Stage != stage)
                    continue;

                if (data.Type != type)
                    continue;

                if (data.FixedPosition != FixedPosition.None)
                    continue;

                candidates.Add(data);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[ProceduralMapGenerator] 랜덤 맵 후보 없음: {type}");
                return PickAnyNormalMap(mapPool, chapter, stage);
            }

            return PickByWeight(candidates);
        }

        private MapData PickFixedMapData(
            List<MapData> mapPool,
            string chapter,
            string stage,
            FixedPosition fixedPosition)
        {
            List<MapData> candidates = new();

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData data = mapPool[i];

                if (data.Chapter != chapter)
                    continue;

                if (data.Stage != stage)
                    continue;

                if (data.FixedPosition != fixedPosition)
                    continue;

                candidates.Add(data);
            }

            if (candidates.Count == 0)
                return null;

            return PickByWeight(candidates);
        }

        private MapData PickAnyNormalMap(
            List<MapData> mapPool,
            string chapter,
            string stage)
        {
            List<MapData> candidates = new();

            for (int i = 0; i < mapPool.Count; i++)
            {
                MapData data = mapPool[i];

                if (data.Chapter != chapter)
                    continue;

                if (data.Stage != stage)
                    continue;

                if (data.FixedPosition != FixedPosition.None)
                    continue;

                candidates.Add(data);
            }

            if (candidates.Count == 0)
                return null;

            return PickByWeight(candidates);
        }

        private MapData PickByWeight(List<MapData> candidates)
        {
            int totalWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
                totalWeight += Mathf.Max(0, candidates[i].SpawnWeight);

            if (totalWeight <= 0)
                return candidates[Random.Range(0, candidates.Count)];

            int random = Random.Range(0, totalWeight);
            int current = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                current += Mathf.Max(0, candidates[i].SpawnWeight);

                if (random < current)
                    return candidates[i];
            }

            return candidates[0];
        }

        private Vector2 CalculatePosition(int layer, int index, int nodeCount)
        {
            float startX = -((nodeCount - 1) * XGap) / 2f;

            float x = startX + index * XGap;
            float y = YStart + layer * YGap;

            return new Vector2(x, y);
        }

        private void ConnectLayers(
            List<GeneratedMapNodeData> previousLayer,
            List<GeneratedMapNodeData> currentLayer)
        {
            if (previousLayer == null || currentLayer == null)
                return;

            if (previousLayer.Count == 0 || currentLayer.Count == 0)
                return;

            for (int i = 0; i < previousLayer.Count; i++)
            {
                GeneratedMapNodeData from = previousLayer[i];

                int targetIndex = Mathf.RoundToInt(
                    (float)i / Mathf.Max(1, previousLayer.Count - 1)
                    * (currentLayer.Count - 1)
                );

                GeneratedMapNodeData to = currentLayer[targetIndex];
                AddConnection(from, to);

                if (currentLayer.Count > 1 && Random.value < 0.35f)
                {
                    int extraIndex = Mathf.Clamp(
                        targetIndex + Random.Range(-1, 2),
                        0,
                        currentLayer.Count - 1
                    );

                    GeneratedMapNodeData extra = currentLayer[extraIndex];
                    AddConnection(from, extra);
                }
            }
        }

        private void AddConnection(
            GeneratedMapNodeData from,
            GeneratedMapNodeData to)
        {
            if (from == null || to == null)
                return;

            if (!from.NextNodeIndices.Contains(to.NodeIndex))
                from.NextNodeIndices.Add(to.NodeIndex);
        }
    }
}