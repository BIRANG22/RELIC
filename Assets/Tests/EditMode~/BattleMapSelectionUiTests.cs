using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;

public class BattleMapSelectionUiTests
{
    [Test]
    public void Open_ShowsAtMostThreeConnectedNextNodes()
    {
        GameObject root = new("NextNodePanel");

        try
        {
            BattleNextNodeSelectionPanel panel = root.AddComponent<BattleNextNodeSelectionPanel>();
            for (int i = 0; i < 3; i++)
            {
                GameObject slot = new($"Choice_{i}");
                slot.transform.SetParent(root.transform);
                slot.AddComponent<BattleNextNodeChoiceButton>();
            }

            MapRuntimeData runtime = CreateClearedRuntimeWithFourNextNodes();
            panel.Open(runtime, _ => { });

            Assert.That(panel.VisibleNodeIndices, Is.EqualTo(new[] { 1, 2, 3 }));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ChoiceButton_ClickReportsStableNodeIndex()
    {
        GameObject root = new("Choice");

        try
        {
            BattleNextNodeChoiceButton choice = root.AddComponent<BattleNextNodeChoiceButton>();
            int selectedNodeIndex = -1;
            choice.Bind(new GeneratedMapNodeData { NodeIndex = 17, Type = "Rest" },
                index => selectedNodeIndex = index);

            choice.Select();

            Assert.That(selectedNodeIndex, Is.EqualTo(17));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BattleMapPanel_DoesNotCreateCodeGeneratedChoiceVisuals()
    {
        GameObject mapPanelObject = new("MapPanel", typeof(RectTransform));
        GameObject selectionRoot = new("NextNodeSelectionRoot", typeof(RectTransform));
        selectionRoot.transform.SetParent(mapPanelObject.transform, false);
        selectionRoot.AddComponent<BattleNextNodeSelectionPanel>();

        try
        {
            mapPanelObject.AddComponent<BattleMapPanel>();

            Assert.That(
                selectionRoot.GetComponentsInChildren<BattleNextNodeChoiceButton>(true).Length,
                Is.Zero);
            Assert.That(mapPanelObject.transform.childCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(mapPanelObject);
        }
    }

    [Test]
    public void BattleMapPanel_DoesNotContainLayoutOverrideMethod()
    {
        MethodInfo method = typeof(BattleMapPanel).GetMethod(
            "ConfigureHorizontalLayout",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Null,
            "사용자가 배치한 MapPanel RectTransform을 런타임에서 덮어쓰면 안 됩니다.");
    }

    [Test]
    public void MapScrollContentWidth_CoversAllLayersAndViewport()
    {
        float width = BattleMapScrollUtility.CalculateContentWidth(
            minNodeX: 0f,
            maxNodeX: 1260f,
            viewportWidth: 420f,
            horizontalPadding: 40f);

        Assert.That(width, Is.EqualTo(1300f));
    }

    [Test]
    public void NextNodeChoicePrefab_ContainsOnlyIconVisual()
    {
        const string path = "Assets/Project/PrefabsR/Map/NextNodeChoicePrefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<BattleNextNodeChoiceButton>(), Is.Not.Null);
        Assert.That(prefab.transform.Find("NodeIcon"), Is.Not.Null);
        Assert.That(prefab.transform.Find("NodeType"), Is.Null);
        Assert.That(prefab.transform.Find("MapId"), Is.Null);
    }

    [Test]
    public void MapScrollFocus_PlacesCurrentNodeAtLeftPadding()
    {
        float anchoredX = BattleMapScrollUtility.CalculateAnchoredX(
            currentNodeX: 560f,
            minNodeX: 0f,
            contentWidth: 1300f,
            viewportWidth: 420f);

        Assert.That(anchoredX, Is.EqualTo(-560f));
    }

    private static MapRuntimeData CreateClearedRuntimeWithFourNextNodes()
    {
        MapRuntimeData runtime = new() { CurrentNodeIndex = 0 };
        GeneratedMapNodeData current = new() { NodeIndex = 0, Type = "Start" };
        runtime.GeneratedNodes.Add(current);
        runtime.ClearedMapIds.Add("0");

        for (int i = 1; i <= 4; i++)
        {
            runtime.GeneratedNodes.Add(new GeneratedMapNodeData
            {
                NodeIndex = i,
                Type = i % 2 == 0 ? "Common" : "Rest"
            });
            current.NextNodeIndices.Add(i);
        }

        return runtime;
    }
}
