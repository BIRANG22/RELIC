using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattleNextNodeSelectionPanel : MonoBehaviour
{
    [SerializeField] private BattleNextNodeChoiceButton[] choices;
    [SerializeField] private BattleNextNodeChoiceButton choicePrefab;
    [SerializeField, Min(1)] private int maxChoiceCount = 3;

    private readonly List<int> visibleNodeIndices = new();

    public IReadOnlyList<int> VisibleNodeIndices => visibleNodeIndices;

    private void Awake()
    {
        EnsureLayout();
        ResolveChoices();
    }

    public void Open(MapRuntimeData runtime, Action<int> onSelected)
    {
        ResolveChoices();
        visibleNodeIndices.Clear();

        int capacity = Mathf.Min(maxChoiceCount, choices.Length);
        List<GeneratedMapNodeData> nodes =
            MapRuntimeProgressUtility.CollectSelectableNextNodes(runtime, capacity);

        for (int i = 0; i < choices.Length; i++)
        {
            BattleNextNodeChoiceButton choice = choices[i];
            if (choice == null)
                continue;

            if (i < nodes.Count)
            {
                choice.Bind(nodes[i], onSelected);
                visibleNodeIndices.Add(nodes[i].NodeIndex);
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

        choices = resolved.ToArray();
    }

    private void EnsureLayout()
    {
        VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<VerticalLayoutGroup>();

        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }
}
