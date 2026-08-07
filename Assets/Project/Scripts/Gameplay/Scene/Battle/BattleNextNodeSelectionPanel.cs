using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleNextNodeSelectionPanel : MonoBehaviour
{
    [SerializeField] private BattleNextNodeChoiceButton[] choices;
    [SerializeField] private BattleNextNodeChoiceButton choicePrefab;
    [SerializeField, Min(1)] private int maxChoiceCount = 3;

    private readonly List<int> visibleNodeIndices = new();

    public IReadOnlyList<int> VisibleNodeIndices => visibleNodeIndices;

    private void Awake()
    {
        ResolveChoices();
    }

    public void Open(MapRuntimeData runtime, Action<int> onSelected)
    {
        ResolveChoices();
        visibleNodeIndices.Clear();

        int capacity = Mathf.Min(maxChoiceCount, choices.Length);
        List<GeneratedMapNodeData> nodes =
            MapRuntimeProgressUtility.CollectSelectableNextNodes(runtime, capacity);

        // 지도에서 실제로 보이는 순서와 NextNodeChoice의 순서를 일치시킵니다.
        // Position.y가 큰 노드가 화면 위쪽에 있으므로 위 -> 아래 순서로 정렬합니다.
        nodes.Sort(CompareNodeTopToBottom);

        for (int i = 0; i < choices.Length; i++)
        {
            BattleNextNodeChoiceButton choice = choices[i];
            if (choice == null)
                continue;

            if (i < nodes.Count)
            {
                GeneratedMapNodeData node = nodes[i];
                choice.Bind(node, onSelected);
                visibleNodeIndices.Add(node.NodeIndex);
            }
            else
            {
                choice.Clear();
            }
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void ResolveChoices()
    {
        List<BattleNextNodeChoiceButton> resolved = new(
            GetComponentsInChildren<BattleNextNodeChoiceButton>(true));

        if (choicePrefab != null)
        {
            while (resolved.Count < maxChoiceCount)
            {
                BattleNextNodeChoiceButton choice = Instantiate(choicePrefab, transform);
                choice.name = $"NextNodeChoice_{resolved.Count + 1}";
                resolved.Add(choice);
            }
        }

        // VerticalLayoutGroup은 sibling 순서대로 위 -> 아래 배치하므로
        // 실제 버튼 배열도 동일한 순서로 고정합니다.
        resolved.Sort(CompareChoiceHierarchyOrder);
        choices = resolved.ToArray();
    }

    private static int CompareNodeTopToBottom(
        GeneratedMapNodeData a,
        GeneratedMapNodeData b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        int yCompare = b.Position.y.CompareTo(a.Position.y);
        if (yCompare != 0)
            return yCompare;

        // 같은 높이에 있는 경우에도 실행마다 순서가 흔들리지 않도록 고정합니다.
        return a.NodeIndex.CompareTo(b.NodeIndex);
    }

    private static int CompareChoiceHierarchyOrder(
        BattleNextNodeChoiceButton a,
        BattleNextNodeChoiceButton b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }
}
