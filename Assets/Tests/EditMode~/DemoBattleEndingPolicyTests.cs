using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class DemoBattleEndingPolicyTests
{
    [Test]
    public void ShouldShow_WhenCompletedDemoNodeHasNoNextNode_ReturnsTrue()
    {
        BattleRuntimeData battle = new() { IsDemoBattle = true };
        MapRuntimeData map = CreateMap(isCleared: true);

        Assert.That(DemoBattleEndingPolicy.ShouldShow(battle, map), Is.True);
    }

    [Test]
    public void ShouldShow_WhenRegularBattleEnds_ReturnsFalse()
    {
        BattleRuntimeData battle = new() { IsDemoBattle = false };
        MapRuntimeData map = CreateMap(isCleared: true);

        Assert.That(DemoBattleEndingPolicy.ShouldShow(battle, map), Is.False);
    }

    [Test]
    public void ShouldShow_WhenCurrentDemoNodeIsNotCleared_ReturnsFalse()
    {
        BattleRuntimeData battle = new() { IsDemoBattle = true };
        MapRuntimeData map = CreateMap(isCleared: false);

        Assert.That(DemoBattleEndingPolicy.ShouldShow(battle, map), Is.False);
    }

    [Test]
    public void ShouldShow_WhenDemoNodeHasValidNextNode_ReturnsFalse()
    {
        BattleRuntimeData battle = new() { IsDemoBattle = true };
        MapRuntimeData map = CreateMap(isCleared: true);
        map.GeneratedNodes[0].NextNodeIndices.Add(2);
        map.GeneratedNodes.Add(new GeneratedMapNodeData { NodeIndex = 2, Type = "Boss" });

        Assert.That(DemoBattleEndingPolicy.ShouldShow(battle, map), Is.False);
    }

    [Test]
    public void ShouldShow_WhenOnlyNextIndexPointsToMissingNode_ReturnsTrue()
    {
        BattleRuntimeData battle = new() { IsDemoBattle = true };
        MapRuntimeData map = CreateMap(isCleared: true);
        map.GeneratedNodes[0].NextNodeIndices.Add(999);

        Assert.That(DemoBattleEndingPolicy.ShouldShow(battle, map), Is.True);
    }

    [Test]
    public void ShouldShow_WhenRuntimeDataIsMissing_ReturnsFalse()
    {
        Assert.That(DemoBattleEndingPolicy.ShouldShow(null, null), Is.False);
        Assert.That(DemoBattleEndingPolicy.ShouldShow(new BattleRuntimeData { IsDemoBattle = true }, null), Is.False);
    }

    private static MapRuntimeData CreateMap(bool isCleared)
    {
        MapRuntimeData map = new()
        {
            CurrentNodeIndex = 1,
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new()
                {
                    NodeIndex = 1,
                    Type = "Rest",
                    NextNodeIndices = new List<int>()
                }
            }
        };

        if (isCleared)
            map.ClearedMapIds.Add("1");

        return map;
    }
}
