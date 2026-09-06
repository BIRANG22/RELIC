using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class MapRuntimeData
    {
        public string SelectedChapterId;

        public string CurrentStage;
        public string CurrentMapId;
        public int CurrentNodeIndex = -1;

        public string CurrentSceneName;

        public List<string> ClearedMapIds = new();
        public List<string> VisitedMapIds = new();

        public bool IsBossUnlocked;
        public bool IsRunInitialized;
        public bool IsManualMapTemplate;
        public string ManualMapTemplateKey;
        public string MapGenerationKey;

        public List<GeneratedMapNodeData> GeneratedNodes = new();
    }

    public static class BattleMapRuntimeGenerationPolicy
    {
        public static bool ShouldRegenerate(MapRuntimeData runtime, string generationKey)
        {
            if (runtime == null)
                return false;

            bool hasGeneratedNodes = runtime.GeneratedNodes != null && runtime.GeneratedNodes.Count > 0;

            if (!runtime.IsRunInitialized || !hasGeneratedNodes)
                return true;

            string requestedGenerationKey = generationKey?.Trim() ?? string.Empty;
            string existingGenerationKey = runtime.MapGenerationKey?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(existingGenerationKey))
                return !string.Equals(
                    existingGenerationKey,
                    requestedGenerationKey,
                    StringComparison.Ordinal);

            if (string.IsNullOrWhiteSpace(requestedGenerationKey))
                return false;

            if (!runtime.IsManualMapTemplate)
                return true;

            return !string.Equals(
                runtime.ManualMapTemplateKey?.Trim(),
                requestedGenerationKey,
                StringComparison.Ordinal);
        }

        public static void ResetProgressForRegeneratedMap(MapRuntimeData runtime)
        {
            if (runtime == null)
                return;

            runtime.ClearedMapIds ??= new();
            runtime.VisitedMapIds ??= new();

            int currentNodeIndex = runtime.CurrentNodeIndex;
            string currentNodeKey = currentNodeIndex.ToString();
            bool wasCurrentNodeCleared = ContainsNodeKey(runtime.ClearedMapIds, currentNodeKey);
            bool wasCurrentNodeVisited = ContainsNodeKey(runtime.VisitedMapIds, currentNodeKey);

            runtime.ClearedMapIds.Clear();
            runtime.VisitedMapIds.Clear();

            GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(runtime);
            if (currentNode != null)
            {
                runtime.CurrentMapId = currentNode.MapId ?? string.Empty;

                if (wasCurrentNodeVisited || wasCurrentNodeCleared)
                    runtime.VisitedMapIds.Add(currentNodeKey);

                if (wasCurrentNodeCleared)
                    runtime.ClearedMapIds.Add(currentNodeKey);
            }
            else
            {
                runtime.CurrentMapId = string.Empty;
                runtime.CurrentNodeIndex = -1;
            }

            runtime.IsBossUnlocked = false;
        }

        private static bool ContainsNodeKey(List<string> nodeKeys, string nodeKey)
        {
            if (nodeKeys == null || string.IsNullOrWhiteSpace(nodeKey))
                return false;

            for (int i = 0; i < nodeKeys.Count; i++)
            {
                if (string.Equals(nodeKeys[i], nodeKey, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    public static class MapRuntimeProgressUtility
    {
        public static bool HasUnclearedCurrentNode(MapRuntimeData runtime)
        {
            if (runtime == null || runtime.CurrentNodeIndex < 0)
                return false;

            if (IsCurrentNodeCleared(runtime))
                return false;

            return FindCurrentNode(runtime) != null;
        }

        public static bool IsCurrentNodeCleared(MapRuntimeData runtime)
        {
            if (runtime == null || runtime.CurrentNodeIndex < 0 || runtime.ClearedMapIds == null)
                return false;

            string nodeKey = runtime.CurrentNodeIndex.ToString();
            return ContainsNodeKey(runtime, nodeKey);
        }

        public static bool MarkCurrentNodeCleared(MapRuntimeData runtime)
        {
            if (runtime == null || runtime.CurrentNodeIndex < 0)
                return false;

            runtime.ClearedMapIds ??= new();

            string nodeKey = runtime.CurrentNodeIndex.ToString();
            if (ContainsNodeKey(runtime, nodeKey))
                return false;

            runtime.ClearedMapIds.Add(nodeKey);
            return true;
        }

        public static GeneratedMapNodeData FindCurrentNode(MapRuntimeData runtime)
        {
            if (runtime == null || runtime.GeneratedNodes == null)
                return null;

            for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
            {
                GeneratedMapNodeData node = runtime.GeneratedNodes[i];

                if (node != null && node.NodeIndex == runtime.CurrentNodeIndex)
                    return node;
            }

            return null;
        }

        public static GeneratedMapNodeData FindStartNode(MapRuntimeData runtime)
        {
            if (runtime?.GeneratedNodes == null)
                return null;

            // 시작 지점은 방 Type이 아니라 지도 구조의 Layer 0으로 결정한다.
            // 따라서 Layer 0에 Special 이벤트를 배치해도 새 탐사 시작 시 바로 해당 노드를 연다.
            for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
            {
                GeneratedMapNodeData node = runtime.GeneratedNodes[i];
                if (node != null && node.LayerIndex == 0)
                    return node;
            }

            // 기존/레거시 데이터 호환용 fallback.
            for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
            {
                GeneratedMapNodeData node = runtime.GeneratedNodes[i];
                if (node != null && string.Equals(node.Type, "Start", StringComparison.Ordinal))
                    return node;
            }

            return null;
        }

        public static List<GeneratedMapNodeData> CollectSelectableNextNodes(
            MapRuntimeData runtime,
            int maxCount = 3)
        {
            List<GeneratedMapNodeData> result = new();

            if (runtime == null || maxCount <= 0 || !IsCurrentNodeCleared(runtime))
                return result;

            GeneratedMapNodeData currentNode = FindCurrentNode(runtime);
            if (currentNode?.NextNodeIndices == null || runtime.GeneratedNodes == null)
                return result;

            for (int i = 0; i < currentNode.NextNodeIndices.Count && result.Count < maxCount; i++)
            {
                int nextNodeIndex = currentNode.NextNodeIndices[i];

                for (int j = 0; j < runtime.GeneratedNodes.Count; j++)
                {
                    GeneratedMapNodeData candidate = runtime.GeneratedNodes[j];
                    if (candidate == null || candidate.NodeIndex != nextNodeIndex)
                        continue;

                    result.Add(candidate);
                    break;
                }
            }

            return result;
        }

        public static bool IsNodeClickableFromCurrentProgress(
            MapRuntimeData runtime,
            GeneratedMapNodeData node)
        {
            if (runtime == null || node == null)
                return false;

            if (runtime.CurrentNodeIndex < 0)
                return string.Equals(node.Type, "Start", StringComparison.Ordinal);

            GeneratedMapNodeData currentNode = FindCurrentNode(runtime);
            if (currentNode == null)
                return false;

            if (!IsCurrentNodeCleared(runtime))
                return node.NodeIndex == runtime.CurrentNodeIndex;

            if (currentNode.NextNodeIndices == null)
                return false;

            return currentNode.NextNodeIndices.Contains(node.NodeIndex);
        }

        private static bool ContainsNodeKey(MapRuntimeData runtime, string nodeKey)
        {
            if (runtime?.ClearedMapIds == null || string.IsNullOrWhiteSpace(nodeKey))
                return false;

            for (int i = 0; i < runtime.ClearedMapIds.Count; i++)
            {
                if (string.Equals(runtime.ClearedMapIds[i], nodeKey, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
