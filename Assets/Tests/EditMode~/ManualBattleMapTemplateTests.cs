using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;

public sealed class ManualBattleMapTemplateTests
{
    private const string DemoTemplatePath =
        "Assets/Project/Data/MapTemplates/Test Manual Battle Map Template Demo.asset";

    [Test]
    public void ResolveTemplate_WhenDemoBattle_SelectsDemoTemplate()
    {
        ManualBattleMapTemplate regularTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        ManualBattleMapTemplate demoTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            ManualBattleMapTemplate selected = BattleMapTemplateSelection.Resolve(
                new BattleRuntimeData { IsDemoBattle = true },
                regularTemplate,
                demoTemplate);

            Assert.That(selected, Is.SameAs(demoTemplate));
        }
        finally
        {
            Object.DestroyImmediate(regularTemplate);
            Object.DestroyImmediate(demoTemplate);
        }
    }

    [Test]
    public void ResolveTemplate_WhenRegularBattle_SelectsRegularTemplate()
    {
        ManualBattleMapTemplate regularTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        ManualBattleMapTemplate demoTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            ManualBattleMapTemplate selected = BattleMapTemplateSelection.Resolve(
                new BattleRuntimeData(),
                regularTemplate,
                demoTemplate);

            Assert.That(selected, Is.SameAs(regularTemplate));
        }
        finally
        {
            Object.DestroyImmediate(regularTemplate);
            Object.DestroyImmediate(demoTemplate);
        }
    }

    [Test]
    public void Nodes_DefaultTemplate_UsesLayer9AsBoss()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            var nodes = template.Nodes;

            Assert.That(nodes.Max(node => node.LayerIndex), Is.EqualTo(9));
            Assert.That(nodes.Single(node => node.Type == "Boss").NodeIndex, Is.EqualTo(33));
        }
        finally
        {
            Object.DestroyImmediate(template);
        }
    }

    [Test]
    public void Nodes_WhenBossLayerIs5_EndsAtLayer5WithReservedBossNodeIndex()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            template.BossLayerIndex = 5;

            var nodes = template.Nodes;

            Assert.That(nodes.Max(node => node.LayerIndex), Is.EqualTo(5));
            Assert.That(nodes.Any(node => node.LayerIndex > 5), Is.False);
            Assert.That(nodes.Single(node => node.Type == "Boss").NodeIndex, Is.EqualTo(17));
        }
        finally
        {
            Object.DestroyImmediate(template);
        }
    }

    [Test]
    public void DemoTemplate_UsesExistingSixLayerAuthoredPath()
    {
        ManualBattleMapTemplate template =
            UnityEditor.AssetDatabase.LoadAssetAtPath<ManualBattleMapTemplate>(DemoTemplatePath);

        Assert.That(template, Is.Not.Null);
        Assert.That(template.GenerationPolicy, Is.EqualTo(ManualBattleMapGenerationPolicy.AuthoredLayout));
        Assert.That(template.BossLayerIndex, Is.EqualTo(5));

        var nodes = template.Nodes.OrderBy(node => node.LayerIndex).ToList();

        Assert.That(nodes.Select(node => node.LayerIndex),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
        Assert.That(nodes.Select(node => node.Type),
            Is.EqualTo(new[] { "Common", "Special", "Common", "Shop", "Rest", "Boss" }));
        Assert.That(nodes[0].MapIdOverride, Is.EqualTo("Map_01"),
            "The authored demo must keep its existing fixed opening battle map.");
        Assert.That(nodes.Select(node => node.NodeIndex),
            Is.EqualTo(new[] { 0, 1, 5, 9, 13, 17 }));

        for (int index = 0; index < nodes.Count - 1; index++)
        {
            Assert.That(nodes[index].NextNodeIndices,
                Is.EqualTo(new[] { nodes[index + 1].NodeIndex }),
                $"Layer {nodes[index].LayerIndex} must connect only to the next node.");
        }

        Assert.That(nodes[^1].NextNodeIndices, Is.Empty);

        Assert.That(nodes[0].CustomPosition, Is.EqualTo(Vector2.zero));

        foreach (var node in nodes.Where(node => node.LayerIndex > 0 && node.LayerIndex < 5))
        {
            Assert.That(node.UseCustomPosition, Is.True,
                $"Layer {node.LayerIndex} must use its centered demo position.");
            Assert.That(node.CustomPosition,
                Is.EqualTo(new Vector2(node.LayerIndex * BattleMapLayoutUtility.LayerGap, 0f)));
        }
    }

    [Test]
    public void TryBuildNodes_DemoAuthoredLayout_BuildsExistingSixLayerPath()
    {
        ManualBattleMapTemplate template =
            UnityEditor.AssetDatabase.LoadAssetAtPath<ManualBattleMapTemplate>(DemoTemplatePath);
        var mapPool = new List<MapData>
        {
            new() { MapId = "Map_01", Type = "Common", Stage = "Stage1", SpawnWeight = 1 },
            new() { MapId = "Map_Common", Type = "Common", Stage = "Stage1", SpawnWeight = 1 },
            new() { MapId = "Map_Event", Type = "Special", EventId = "Event_01", Stage = "Stage1", SpawnWeight = 1 },
            new() { MapId = "Map_22", Type = "Shop", Stage = "Stage1", SpawnWeight = 1 },
            new() { MapId = "Map_Rest", Type = "Rest", Stage = "Stage1", SpawnWeight = 1 },
            new() { MapId = "Map_Boss", Type = "Boss", Stage = "Stage1", SpawnWeight = 1 }
        };

        bool success = template.TryBuildNodes(
            mapPool,
            "Chapter1",
            "Stage1",
            out List<GeneratedMapNodeData> nodes);

        Assert.That(success, Is.True);
        Assert.That(nodes.Select(node => node.LayerIndex), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
        Assert.That(nodes.Select(node => node.Type),
            Is.EqualTo(new[] { "Common", "Special", "Common", "Shop", "Rest", "Boss" }));
        Assert.That(nodes[^1].NextNodeIndices, Is.Empty);
    }
}
