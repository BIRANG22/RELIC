using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class ManualBattleMapTemplateTests
{
    [Test]
    public void TryBuildNodes_PreservesManualTypesConnectionsAndCalculatedPositions()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 0,
            LayerIndex = 0,
            RowIndex = 0,
            Type = "Start",
            MapIdOverride = "start_map",
            NextNodeIndices = new List<int> { 1, 2 }
        });
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 1,
            LayerIndex = 1,
            RowIndex = 0,
            Type = "Common",
            MapIdOverride = "battle_a",
            NextNodeIndices = new List<int> { 3 }
        });
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 2,
            LayerIndex = 1,
            RowIndex = 1,
            Type = "Special",
            MapIdOverride = "event_a",
            NextNodeIndices = new List<int> { 3 }
        });
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 3,
            LayerIndex = 2,
            RowIndex = 0,
            Type = "Boss",
            MapIdOverride = "boss_a"
        });

        bool built = template.TryBuildNodes(CreateMapPool(), "chapter_01", "stage_01", out List<GeneratedMapNodeData> nodes);

        Object.DestroyImmediate(template);

        Assert.That(built, Is.True);
        Assert.That(nodes, Has.Count.EqualTo(4));
        Assert.That(nodes[0].Type, Is.EqualTo("Start"));
        Assert.That(nodes[0].NextNodeIndices, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(nodes[1].Type, Is.EqualTo("Common"));
        Assert.That(nodes[1].MapId, Is.EqualTo("battle_a"));
        Assert.That(nodes[1].Position, Is.EqualTo(BattleMapLayoutUtility.CalculatePosition(1, 0, 2)));
        Assert.That(nodes[2].Type, Is.EqualTo("Special"));
        Assert.That(nodes[2].MapId, Is.EqualTo("event_a"));
        Assert.That(nodes[2].Position, Is.EqualTo(BattleMapLayoutUtility.CalculatePosition(1, 1, 2)));
    }

    [Test]
    public void TryBuildNodes_BlankMapIdSelectsMatchingMapDataForManualNodeType()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 0,
            LayerIndex = 0,
            RowIndex = 0,
            Type = "Start",
            MapIdOverride = "start_map",
            NextNodeIndices = new List<int> { 1 }
        });
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 1,
            LayerIndex = 1,
            RowIndex = 0,
            Type = "Common"
        });

        bool built = template.TryBuildNodes(CreateMapPool(), "chapter_01", "stage_01", out List<GeneratedMapNodeData> nodes);

        Object.DestroyImmediate(template);

        Assert.That(built, Is.True);
        Assert.That(nodes[1].Type, Is.EqualTo("Common"));
        Assert.That(nodes[1].MapId, Is.EqualTo("battle_a"));
    }

    [Test]
    public void TryBuildNodes_RejectsConnectionToMissingNode()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 0,
            LayerIndex = 0,
            RowIndex = 0,
            Type = "Start",
            MapIdOverride = "start_map",
            NextNodeIndices = new List<int> { 99 }
        });

        bool built = template.TryBuildNodes(CreateMapPool(), "chapter_01", "stage_01", out List<GeneratedMapNodeData> nodes);

        Object.DestroyImmediate(template);

        Assert.That(built, Is.False);
        Assert.That(nodes, Is.Empty);
    }

    [Test]
    public void BattleMapGenerationResolver_UsesManualTemplateBeforeProceduralFallback()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 0,
            LayerIndex = 0,
            RowIndex = 0,
            Type = "Start",
            MapIdOverride = "start_map",
            NextNodeIndices = new List<int> { 1 }
        });
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 1,
            LayerIndex = 1,
            RowIndex = 0,
            Type = "Special",
            MapIdOverride = "event_a"
        });

        List<GeneratedMapNodeData> nodes = BattleMapGenerationResolver.Generate(
            CreateMapPool(),
            "chapter_01",
            "stage_01",
            template);

        Object.DestroyImmediate(template);

        Assert.That(nodes, Has.Count.EqualTo(2));
        Assert.That(nodes[1].Type, Is.EqualTo("Special"));
        Assert.That(nodes[0].NextNodeIndices, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void BattleMapGenerationResolver_GenerateResultReportsManualTemplateUsage()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 0,
            LayerIndex = 0,
            RowIndex = 0,
            Type = "Start",
            MapIdOverride = "start_map"
        });

        BattleMapGenerationResult result = BattleMapGenerationResolver.GenerateResult(
            CreateMapPool(),
            "chapter_01",
            "stage_01",
            template);

        Object.DestroyImmediate(template);

        Assert.That(result.UsedManualTemplate, Is.True);
        Assert.That(result.Nodes, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryBuildNodes_NormalizesNodeTypeFromMatchedMapData()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        template.Nodes.Add(new ManualBattleMapNodeDefinition
        {
            NodeIndex = 0,
            LayerIndex = 0,
            RowIndex = 0,
            Type = "common",
            MapIdOverride = "battle_a"
        });

        bool built = template.TryBuildNodes(CreateMapPool(), "chapter_01", "stage_01", out List<GeneratedMapNodeData> nodes);

        Object.DestroyImmediate(template);

        Assert.That(built, Is.True);
        Assert.That(nodes[0].Type, Is.EqualTo("Common"));
    }

    private static List<MapData> CreateMapPool()
    {
        return new List<MapData>
        {
            new()
            {
                MapId = "start_map",
                Type = "Start",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1,
                FixedPosition = FixedPosition.Front
            },
            new()
            {
                MapId = "battle_a",
                Type = "Common",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1,
                FixedPosition = FixedPosition.None
            },
            new()
            {
                MapId = "event_a",
                Type = "Special",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1,
                FixedPosition = FixedPosition.None
            },
            new()
            {
                MapId = "boss_a",
                Type = "Boss",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1,
                FixedPosition = FixedPosition.Final
            },
            new()
            {
                MapId = "wrong_stage_battle",
                Type = "Common",
                Chapter = "chapter_01",
                Stage = "stage_02",
                SpawnWeight = 1,
                FixedPosition = FixedPosition.None
            }
        };
    }
}
