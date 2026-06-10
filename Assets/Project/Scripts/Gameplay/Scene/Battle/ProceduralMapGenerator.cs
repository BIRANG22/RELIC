using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class ProceduralMapGenerator
    {
        private int nextNodeIndex;

        private const int TotalLayerCount = 10;
        private const int MaxColumnCount = 5;

        private const int MaxOutgoingConnections = 2;
        private const int MaxIncomingConnections = 2;
        private const float MinNodeXDistance = 110f;

        private const int MinTotalNodeCount = 24;
        private const int MaxTotalNodeCount = 30;

        private const float YStart = -1070f;
        private const float YGap = 150f;
        private const float XGap = 280f;

        private const float XJitter = 45f;
        private const float YJitter = 25f;

        private const float ExtraConnectionChance = 0.60f;
        private const float EdgeExtraConnectionChance = 0.80f;
        private const float EdgeColumnThresholdX = 140f;

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

            int[] layerNodeCounts = GenerateValidLayerCounts();

            List<List<GeneratedMapNodeData>> layers = new();

            for (int layer = 0; layer < TotalLayerCount; layer++)
            {
                List<GeneratedMapNodeData> currentLayer = new();

                int nodeCount = layerNodeCounts[layer];
                int[] columns = DecideColumns(layer, nodeCount);

                for (int i = 0; i < nodeCount; i++)
                {
                    MapData mapData = PickMapDataForLayer(
                        mapPool,
                        chapter,
                        stage,
                        layer
                    );

                    if (mapData == null)
                        continue;

                    Vector2 position = CalculatePosition(layer, columns[i]);

                    GeneratedMapNodeData node = CreateNode(
                        mapData.MapId,
                        mapData.Type,
                        position,
                        layer
                    );

                    currentLayer.Add(node);
                    result.Add(node);
                }

                FixLayerNodeOverlap(currentLayer);
                layers.Add(currentLayer);
            }

            for (int layer = 0; layer < layers.Count - 1; layer++)
            {
                ConnectLayersWithoutCrossing(
                    layers[layer],
                    layers[layer + 1]
                );
            }

            return result;
        }

        private int[] GenerateValidLayerCounts()
        {
            for (int attempt = 0; attempt < 500; attempt++)
            {
                int[] counts = new int[TotalLayerCount];

                counts[0] = 1;
                counts[TotalLayerCount - 1] = 1;

                for (int layer = 1; layer < TotalLayerCount - 1; layer++)
                {
                    if (layer == 1)
                    {
                        counts[layer] = 2;
                    }
                    else if(layer == 2)
                    {
                        counts[layer] = 3;
                    }
                    else if (layer == TotalLayerCount - 2)
                    {
                        counts[layer] = 2;
                    }
                    else
                    {
                        counts[layer] = Random.Range(2, MaxColumnCount + 1);
                    }
                }

                if (!IsValidLayerCountSet(counts))
                    continue;

                int totalNodeCount = 0;

                for (int i = 0; i < counts.Length; i++)
                    totalNodeCount += counts[i];

                if (totalNodeCount < MinTotalNodeCount)
                    continue;

                if (totalNodeCount > MaxTotalNodeCount)
                    continue;

                return counts;
            }

            return new int[]
            {
                1,3,4,3,4,4,3,3,2,1
            };
        }

        private bool IsValidLayerCountSet(int[] counts)
        {
            for (int i = 0; i < counts.Length - 1; i++)
            {
                int previous = counts[i];
                int current = counts[i + 1];

                if (current > previous * MaxOutgoingConnections)
                    return false;

                if (previous > current * MaxIncomingConnections)
                    return false;
            }

            return true;
        }

        private int[] DecideColumns(int layer, int nodeCount)
        {
            if (layer == 0)
                return new int[] { 2 };

            if (layer == 1 && nodeCount == 2)
                return new int[] { 1, 3 };

            if (layer == TotalLayerCount - 3 && nodeCount >= 2)
            {
                List<int> fixedColumns = new();

                fixedColumns.Add(Random.value < 0.5f ? 0 : 1);
                fixedColumns.Add(Random.value < 0.5f ? 3 : 4);

                while (fixedColumns.Count < nodeCount)
                {
                    int column = Random.Range(0, MaxColumnCount);

                    if (!fixedColumns.Contains(column))
                        fixedColumns.Add(column);
                }

                fixedColumns.Sort();

                return fixedColumns.ToArray();
            }

            if (layer == TotalLayerCount - 2)
                return new int[] { 1, 3 };

            if (layer == TotalLayerCount - 1)
                return new int[] { 2 };

            List<int> columns = new();

            while (columns.Count < nodeCount)
            {
                int column = Random.Range(0, MaxColumnCount);

                if (!columns.Contains(column))
                    columns.Add(column);
            }

            columns.Sort();
            return columns.ToArray();
        }

        private Vector2 CalculatePosition(int layer, int column)
        {
            float centerOffset = (MaxColumnCount - 1) * 0.5f;

            float baseX = (column - centerOffset) * XGap;
            float baseY = YStart + layer * YGap;

            bool isStartLayer = layer == 0;
            bool isPenultimateLayer = layer == TotalLayerCount - 2;
            bool isBossLayer = layer == TotalLayerCount - 1;


            if (isStartLayer || isBossLayer)
                return new Vector2(0f, baseY);

            float randomX = Random.Range(-XJitter, XJitter);
            float randomY = Random.Range(-YJitter, YJitter);

            return new Vector2(
                baseX + randomX,
                baseY + randomY
            );
        }

        private void ConnectLayersWithoutCrossing(
            List<GeneratedMapNodeData> previousLayer,
            List<GeneratedMapNodeData> currentLayer)
        {
            if (previousLayer == null || currentLayer == null)
                return;

            if (previousLayer.Count == 0 || currentLayer.Count == 0)
                return;

            previousLayer.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            currentLayer.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));

            Dictionary<int, int> outgoingCount = new();
            Dictionary<int, int> incomingCount = new();

            for (int i = 0; i < previousLayer.Count; i++)
                outgoingCount[previousLayer[i].NodeIndex] = 0;

            for (int i = 0; i < currentLayer.Count; i++)
                incomingCount[currentLayer[i].NodeIndex] = 0;

            BuildMainNonCrossingConnections(
                previousLayer,
                currentLayer,
                outgoingCount,
                incomingCount
            );

            AddBalancedExtraConnections(
                previousLayer,
                currentLayer,
                outgoingCount,
                incomingCount
            );

            ClampCurrentLayerXInsideParents(previousLayer, currentLayer);
        }

        private void BuildMainNonCrossingConnections(
            List<GeneratedMapNodeData> previousLayer,
            List<GeneratedMapNodeData> currentLayer,
            Dictionary<int, int> outgoingCount,
            Dictionary<int, int> incomingCount)
        {
            for (int previousIndex = 0; previousIndex < previousLayer.Count; previousIndex++)
            {
                int currentIndex = Mathf.FloorToInt(
                    (float)previousIndex * currentLayer.Count / previousLayer.Count
                );

                currentIndex = Mathf.Clamp(currentIndex, 0, currentLayer.Count - 1);

                AddLimitedConnection(
                    previousLayer[previousIndex],
                    currentLayer[currentIndex],
                    outgoingCount,
                    incomingCount
                );
            }

            for (int currentIndex = 0; currentIndex < currentLayer.Count; currentIndex++)
            {
                GeneratedMapNodeData current = currentLayer[currentIndex];

                if (incomingCount[current.NodeIndex] > 0)
                    continue;

                int previousIndex = Mathf.FloorToInt(
                    (float)currentIndex * previousLayer.Count / currentLayer.Count
                );

                previousIndex = Mathf.Clamp(previousIndex, 0, previousLayer.Count - 1);

                AddLimitedConnection(
                    previousLayer[previousIndex],
                    current,
                    outgoingCount,
                    incomingCount
                );
            }
        }

        private void AddBalancedExtraConnections(
            List<GeneratedMapNodeData> previousLayer,
            List<GeneratedMapNodeData> currentLayer,
            Dictionary<int, int> outgoingCount,
            Dictionary<int, int> incomingCount)
        {
            for (int previousIndex = 0; previousIndex < previousLayer.Count; previousIndex++)
            {
                GeneratedMapNodeData from = previousLayer[previousIndex];

                if (outgoingCount[from.NodeIndex] >= MaxOutgoingConnections)
                    continue;

                float chance = ExtraConnectionChance;

                if (Mathf.Abs(from.Position.x) >= EdgeColumnThresholdX)
                    chance = EdgeExtraConnectionChance;

                if (Random.value > chance)
                    continue;

                int leftIndex = FindNearestCurrentIndexOnSide(
                    from,
                    currentLayer,
                    incomingCount,
                    true
                );

                int rightIndex = FindNearestCurrentIndexOnSide(
                    from,
                    currentLayer,
                    incomingCount,
                    false
                );

                if (leftIndex < 0 || rightIndex < 0)
                    continue;

                if (WouldCrossExistingConnections(previousLayer, currentLayer, previousIndex, leftIndex))
                    continue;

                if (WouldCrossExistingConnections(previousLayer, currentLayer, previousIndex, rightIndex))
                    continue;

                int connectedIndex = GetConnectedCurrentIndex(from, currentLayer);

                if (connectedIndex >= 0)
                {
                    float connectedX = currentLayer[connectedIndex].Position.x;

                    if (connectedX < from.Position.x)
                    {
                        AddLimitedConnection(
                            from,
                            currentLayer[rightIndex],
                            outgoingCount,
                            incomingCount
                        );
                    }
                    else if (connectedX > from.Position.x)
                    {
                        AddLimitedConnection(
                            from,
                            currentLayer[leftIndex],
                            outgoingCount,
                            incomingCount
                        );
                    }
                    else
                    {
                        int targetIndex = Random.value < 0.5f ? leftIndex : rightIndex;

                        AddLimitedConnection(
                            from,
                            currentLayer[targetIndex],
                            outgoingCount,
                            incomingCount
                        );
                    }
                }
            }
        }

        private int FindNearestCurrentIndexOnSide(
            GeneratedMapNodeData from,
            List<GeneratedMapNodeData> currentLayer,
            Dictionary<int, int> incomingCount,
            bool leftSide)
        {
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < currentLayer.Count; i++)
            {
                GeneratedMapNodeData candidate = currentLayer[i];

                if (incomingCount[candidate.NodeIndex] >= MaxIncomingConnections)
                    continue;

                if (from.NextNodeIndices.Contains(candidate.NodeIndex))
                    continue;

                bool isLeft = candidate.Position.x < from.Position.x;
                bool isRight = candidate.Position.x > from.Position.x;

                if (leftSide && !isLeft)
                    continue;

                if (!leftSide && !isRight)
                    continue;

                float distance = Mathf.Abs(candidate.Position.x - from.Position.x);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }        

        private void AddLimitedConnection(
            GeneratedMapNodeData from,
            GeneratedMapNodeData to,
            Dictionary<int, int> outgoingCount,
            Dictionary<int, int> incomingCount)
        {
            if (from == null || to == null)
                return;

            if (from.NextNodeIndices.Contains(to.NodeIndex))
                return;

            if (outgoingCount[from.NodeIndex] >= MaxOutgoingConnections)
                return;

            if (incomingCount[to.NodeIndex] >= MaxIncomingConnections)
                return;

            from.NextNodeIndices.Add(to.NodeIndex);
            outgoingCount[from.NodeIndex]++;
            incomingCount[to.NodeIndex]++;
        }

        private void ClampCurrentLayerXInsideParents(
            List<GeneratedMapNodeData> previousLayer,
            List<GeneratedMapNodeData> currentLayer)
        {
            for (int i = 0; i < currentLayer.Count; i++)
            {
                GeneratedMapNodeData current = currentLayer[i];

                float minParentX = float.MaxValue;
                float maxParentX = float.MinValue;
                int parentCount = 0;

                for (int p = 0; p < previousLayer.Count; p++)
                {
                    GeneratedMapNodeData parent = previousLayer[p];

                    if (!parent.NextNodeIndices.Contains(current.NodeIndex))
                        continue;

                    minParentX = Mathf.Min(minParentX, parent.Position.x);
                    maxParentX = Mathf.Max(maxParentX, parent.Position.x);
                    parentCount++;
                }

                if (parentCount < 2)
                    continue;

                Vector2 position = current.Position;
                position.x = Mathf.Clamp(position.x, minParentX, maxParentX);
                current.Position = position;
            }
        }

        private MapData PickMapDataForLayer(
            List<MapData> mapPool,
            string chapter,
            string stage,
            int layer)
        {
            if (layer == 0)
            {
                MapData startMap = PickFixedMapData(
                    mapPool,
                    chapter,
                    stage,
                    FixedPosition.Front
                );

                if (startMap != null)
                    return startMap;

                return CreateVirtualMapData("Start", "Start");
            }

            if (layer == TotalLayerCount - 2)
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

            if (layer == TotalLayerCount - 1)
            {
                MapData finalMap = PickFixedMapData(
                    mapPool,
                    chapter,
                    stage,
                    FixedPosition.Final
                );

                if (finalMap != null)
                    return finalMap;

                return PickRandomMapData(mapPool, chapter, stage, "Boss");
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
                return PickAnyNormalMap(mapPool, chapter, stage);

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

        private void FixLayerNodeOverlap(List<GeneratedMapNodeData> layer)
        {
            if (layer == null || layer.Count <= 1)
                return;

            layer.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));

            for (int i = 1; i < layer.Count; i++)
            {
                GeneratedMapNodeData left = layer[i - 1];
                GeneratedMapNodeData right = layer[i];

                float distance = right.Position.x - left.Position.x;

                if (distance >= MinNodeXDistance)
                    continue;

                float lack = MinNodeXDistance - distance;
                float half = lack * 0.5f;

                Vector2 leftPos = left.Position;
                Vector2 rightPos = right.Position;

                leftPos.x -= half;
                rightPos.x += half;

                left.Position = leftPos;
                right.Position = rightPos;
            }

            ClampLayerInsideMapWidth(layer);
        }

        private void ClampLayerInsideMapWidth(List<GeneratedMapNodeData> layer)
        {
            float maxX = ((MaxColumnCount - 1) * 0.5f) * XGap + XJitter;
            float minX = -maxX;

            for (int i = 0; i < layer.Count; i++)
            {
                Vector2 position = layer[i].Position;

                position.x = Mathf.Clamp(position.x, minX, maxX);

                layer[i].Position = position;
            }
        }

        private MapData CreateVirtualMapData(string mapId, string type)
        {
            return new MapData
            {
                MapId = mapId,
                Name = mapId,
                Type = type,
                SpawnWeight = 1,
                FixedPosition = FixedPosition.Front
            };
        }

        private bool WouldCrossExistingConnections(
            List<GeneratedMapNodeData> previousLayer,
            List<GeneratedMapNodeData> currentLayer,
            int fromIndex,
            int toIndex)
        {
            for (int i = 0; i < previousLayer.Count; i++)
            {
                GeneratedMapNodeData otherFrom = previousLayer[i];

                for (int j = 0; j < otherFrom.NextNodeIndices.Count; j++)
                {
                    int otherToIndex = FindLayerIndexByNodeIndex(
                        currentLayer,
                        otherFrom.NextNodeIndices[j]
                    );

                    if (otherToIndex < 0)
                        continue;

                    if (i == fromIndex)
                        continue;

                    if (fromIndex < i && toIndex > otherToIndex)
                        return true;

                    if (fromIndex > i && toIndex < otherToIndex)
                        return true;
                }
            }

            return false;
        }

        private int GetConnectedCurrentIndex(
            GeneratedMapNodeData from,
            List<GeneratedMapNodeData> currentLayer)
        {
            for (int i = 0; i < currentLayer.Count; i++)
            {
                if (from.NextNodeIndices.Contains(currentLayer[i].NodeIndex))
                    return i;
            }

            return -1;
        }

        private int FindLayerIndexByNodeIndex(
            List<GeneratedMapNodeData> layer,
            int nodeIndex)
        {
            for (int i = 0; i < layer.Count; i++)
            {
                if (layer[i].NodeIndex == nodeIndex)
                    return i;
            }

            return -1;
        }

        private GeneratedMapNodeData CreateNode(
            string mapId,
            string type,
            Vector2 position,
            int layerIndex)
        {
            return new GeneratedMapNodeData
            {
                NodeIndex = nextNodeIndex++,
                LayerIndex = layerIndex,
                MapId = mapId,
                Type = type,
                Position = position
            };
        }
    }
}