using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public sealed class BattleTimelineTransformCache
    {
        private readonly Transform root;
        private readonly Dictionary<Vector2Int, Transform> sizeRoots = new();
        private readonly Dictionary<Transform, TMP_Text> valueTexts = new();

        public BattleTimelineTransformCache(Transform root)
        {
            this.root = root;
        }

        public Transform GetSizeRoot(int orderIndex, int size)
        {
            Vector2Int key = new(orderIndex, size);
            if (sizeRoots.TryGetValue(key, out Transform cached) && cached != null)
                return cached;

            Transform order = FindChildRecursive(root, "Order" + (orderIndex + 1).ToString("00"));
            Transform result = order != null ? order.Find("Use_skill" + size) : null;
            if (result != null)
                sizeRoots[key] = result;
            else
                sizeRoots.Remove(key);
            return result;
        }

        public TMP_Text GetValueText(Transform sizeRoot)
        {
            if (sizeRoot == null)
                return null;

            if (valueTexts.TryGetValue(sizeRoot, out TMP_Text cached) && cached != null)
                return cached;

            Transform textTransform = FindChildRecursive(sizeRoot, "Value") ??
                                      FindChildRecursive(sizeRoot, "Text (TMP)") ??
                                      FindChildRecursive(sizeRoot, "Text");
            TMP_Text result = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            if (result != null)
                valueTexts[sizeRoot] = result;
            else
                valueTexts.Remove(sizeRoot);
            return result;
        }

        public void Clear()
        {
            sizeRoots.Clear();
            valueTexts.Clear();
        }

        private static Transform FindChildRecursive(Transform searchRoot, string childName)
        {
            if (searchRoot == null)
                return null;

            for (int i = 0; i < searchRoot.childCount; i++)
            {
                Transform child = searchRoot.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
