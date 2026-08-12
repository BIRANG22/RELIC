using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class ManualBattleMapRuntimePolicyTests
{
    [Test]
    public void ShouldRegenerate_ReturnsTrueWhenProceduralRuntimeExistsAndManualTemplateIsAssigned()
    {
        MapRuntimeData runtime = new()
        {
            IsRunInitialized = true,
            IsManualMapTemplate = false,
            ManualMapTemplateKey = string.Empty,
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new()
                {
                    NodeIndex = 0,
                    LayerIndex = 0,
                    Type = "Start",
                    MapId = "procedural_start"
                }
            }
        };

        bool shouldRegenerate = BattleMapRuntimeGenerationPolicy.ShouldRegenerate(
            runtime,
            "ManualBattleMapTemplate:Fixed");

        Assert.That(shouldRegenerate, Is.True);
    }

    [Test]
    public void ShouldRegenerate_ReturnsFalseWhenExistingManualRuntimeMatchesAssignedTemplate()
    {
        MapRuntimeData runtime = new()
        {
            IsRunInitialized = true,
            IsManualMapTemplate = true,
            ManualMapTemplateKey = "ManualBattleMapTemplate:Fixed",
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new()
                {
                    NodeIndex = 0,
                    LayerIndex = 0,
                    Type = "Start",
                    MapId = "manual_start"
                }
            }
        };

        bool shouldRegenerate = BattleMapRuntimeGenerationPolicy.ShouldRegenerate(
            runtime,
            "ManualBattleMapTemplate:Fixed");

        Assert.That(shouldRegenerate, Is.False);
    }

    [Test]
    public void ResetProgressForRegeneratedMap_PreservesClearedCurrentNodeWhenNodeStillExists()
    {
        MapRuntimeData runtime = new()
        {
            CurrentMapId = "old_start",
            CurrentNodeIndex = 0,
            ClearedMapIds = new List<string> { "0" },
            VisitedMapIds = new List<string> { "0" },
            IsBossUnlocked = true,
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new()
                {
                    NodeIndex = 0,
                    LayerIndex = 0,
                    Type = "Start",
                    MapId = "Start",
                    NextNodeIndices = new List<int> { 1 }
                },
                new()
                {
                    NodeIndex = 1,
                    LayerIndex = 1,
                    Type = "Common",
                    MapId = "battle_a"
                }
            }
        };

        BattleMapRuntimeGenerationPolicy.ResetProgressForRegeneratedMap(runtime);

        Assert.That(runtime.CurrentNodeIndex, Is.EqualTo(0));
        Assert.That(runtime.CurrentMapId, Is.EqualTo("Start"));
        Assert.That(runtime.ClearedMapIds, Does.Contain("0"));
        Assert.That(
            MapRuntimeProgressUtility.CollectSelectableNextNodes(runtime),
            Has.Count.EqualTo(1));
    }
}
