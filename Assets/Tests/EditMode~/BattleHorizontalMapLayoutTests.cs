using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class BattleHorizontalMapLayoutTests
{
    [Test]
    public void CalculatePosition_AdvancesLayersFromLeftToRight()
    {
        Vector2 first = BattleMapLayoutUtility.CalculatePosition(0, 0, 1);
        Vector2 second = BattleMapLayoutUtility.CalculatePosition(1, 0, 1);

        Assert.That(second.x, Is.GreaterThan(first.x));
        Assert.That(second.y, Is.EqualTo(first.y));
    }

    [Test]
    public void CalculatePosition_CentersRowsWithoutRandomJitter()
    {
        Vector2 top = BattleMapLayoutUtility.CalculatePosition(4, 0, 3);
        Vector2 middle = BattleMapLayoutUtility.CalculatePosition(4, 1, 3);
        Vector2 bottom = BattleMapLayoutUtility.CalculatePosition(4, 2, 3);

        Assert.That(top.x, Is.EqualTo(middle.x));
        Assert.That(middle.x, Is.EqualTo(bottom.x));
        Assert.That(top.y, Is.EqualTo(-bottom.y));
        Assert.That(middle.y, Is.Zero);
        Assert.That(
            BattleMapLayoutUtility.CalculatePosition(4, 0, 3),
            Is.EqualTo(top));
    }

    [Test]
    public void CalculatePosition_UsesHalfSizeLayerAndRowSpacing()
    {
        Vector2 firstLayer = BattleMapLayoutUtility.CalculatePosition(0, 0, 1);
        Vector2 secondLayer = BattleMapLayoutUtility.CalculatePosition(1, 0, 1);
        Vector2 top = BattleMapLayoutUtility.CalculatePosition(2, 0, 3);
        Vector2 middle = BattleMapLayoutUtility.CalculatePosition(2, 1, 3);
        Vector2 bottom = BattleMapLayoutUtility.CalculatePosition(2, 2, 3);

        Assert.That(secondLayer.x - firstLayer.x, Is.EqualTo(100f));
        Assert.That(top.y, Is.EqualTo(40f));
        Assert.That(middle.y, Is.Zero);
        Assert.That(bottom.y, Is.EqualTo(-40f));
    }

    [Test]
    public void GeneratedLayerCounts_NeverExceedThreeNodes()
    {
        ProceduralMapGenerator generator = new();
        MethodInfo generateCounts = typeof(ProceduralMapGenerator).GetMethod(
            "GenerateValidLayerCounts",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(generateCounts, Is.Not.Null);

        for (int attempt = 0; attempt < 32; attempt++)
        {
            int[] counts = (int[])generateCounts.Invoke(generator, null);
            Assert.That(counts, Has.All.LessThanOrEqualTo(3));
        }
    }

    [Test]
    public void Generate_FirstLayerSelectsStartMapByType()
    {
        ProceduralMapGenerator generator = new();

        List<GeneratedMapNodeData> nodes = generator.Generate(
            CreateMapPool(),
            "chapter_01",
            "stage_01");

        Assert.That(nodes, Is.Not.Empty);
        Assert.That(nodes[0].LayerIndex, Is.Zero);
        Assert.That(nodes[0].Type, Is.EqualTo("Start"));
        Assert.That(nodes[0].MapId, Is.EqualTo("start_map"));
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
                SpawnWeight = 1
            },
            new()
            {
                MapId = "battle_a",
                Type = "Common",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1
            },
            new()
            {
                MapId = "event_a",
                Type = "Special",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1
            },
            new()
            {
                MapId = "elite_a",
                Type = "Elite",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1
            },
            new()
            {
                MapId = "rest_a",
                Type = "Rest",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1
            },
            new()
            {
                MapId = "boss_a",
                Type = "Boss",
                Chapter = "chapter_01",
                Stage = "stage_01",
                SpawnWeight = 1
            }
        };
    }
}
