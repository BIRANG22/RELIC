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

        public List<GeneratedMapNodeData> GeneratedNodes = new();
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
