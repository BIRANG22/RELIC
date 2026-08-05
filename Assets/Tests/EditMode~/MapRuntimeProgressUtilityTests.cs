using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Collections.Generic;

public class MapRuntimeProgressUtilityTests
{
    [Test]
    public void IsNodeClickableFromCurrentProgress_UnclearedCurrentNode_AllowsCurrentNodeOnly()
    {
        MapRuntimeData runtime = CreateRuntime();

        GeneratedMapNodeData currentNode = runtime.GeneratedNodes[1];
        GeneratedMapNodeData nextNode = runtime.GeneratedNodes[2];

        Assert.That(MapRuntimeProgressUtility.HasUnclearedCurrentNode(runtime), Is.True);
        Assert.That(MapRuntimeProgressUtility.IsNodeClickableFromCurrentProgress(runtime, currentNode), Is.True);
        Assert.That(MapRuntimeProgressUtility.IsNodeClickableFromCurrentProgress(runtime, nextNode), Is.False);
    }

    [Test]
    public void IsNodeClickableFromCurrentProgress_ClearedCurrentNode_AllowsNextNode()
    {
        MapRuntimeData runtime = CreateRuntime();
        runtime.ClearedMapIds.Add("1");

        GeneratedMapNodeData currentNode = runtime.GeneratedNodes[1];
        GeneratedMapNodeData nextNode = runtime.GeneratedNodes[2];

        Assert.That(MapRuntimeProgressUtility.HasUnclearedCurrentNode(runtime), Is.False);
        Assert.That(MapRuntimeProgressUtility.IsNodeClickableFromCurrentProgress(runtime, currentNode), Is.False);
        Assert.That(MapRuntimeProgressUtility.IsNodeClickableFromCurrentProgress(runtime, nextNode), Is.True);
    }

    [Test]
    public void MarkCurrentNodeCleared_AddsCurrentNodeOnce()
    {
        MapRuntimeData runtime = CreateRuntime();

        Assert.That(MapRuntimeProgressUtility.MarkCurrentNodeCleared(runtime), Is.True);
        Assert.That(MapRuntimeProgressUtility.MarkCurrentNodeCleared(runtime), Is.False);
        Assert.That(runtime.ClearedMapIds, Is.EqualTo(new[] { "1" }));
    }

    [Test]
    public void FindStartNode_ReturnsStartNodeRegardlessOfListOrder()
    {
        MapRuntimeData runtime = CreateRuntime();

        Assert.That(MapRuntimeProgressUtility.FindStartNode(runtime)?.NodeIndex, Is.EqualTo(0));
    }

    [Test]
    public void CollectSelectableNextNodes_ReturnsConnectedNodesInConnectionOrder()
    {
        MapRuntimeData runtime = CreateRuntime();
        runtime.ClearedMapIds.Add("1");
        runtime.GeneratedNodes.Add(new GeneratedMapNodeData
        {
            NodeIndex = 3,
            Type = "Rest",
            MapId = "rest_01"
        });
        runtime.GeneratedNodes[1].NextNodeIndices.Add(3);

        List<GeneratedMapNodeData> result =
            MapRuntimeProgressUtility.CollectSelectableNextNodes(runtime, 3);

        Assert.That(result.ConvertAll(node => node.NodeIndex), Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void CollectSelectableNextNodes_UnclearedCurrentNode_ReturnsEmpty()
    {
        MapRuntimeData runtime = CreateRuntime();

        Assert.That(
            MapRuntimeProgressUtility.CollectSelectableNextNodes(runtime, 3),
            Is.Empty);
    }

    private static MapRuntimeData CreateRuntime()
    {
        MapRuntimeData runtime = new()
        {
            CurrentNodeIndex = 1,
            IsRunInitialized = true
        };

        runtime.GeneratedNodes.Add(new GeneratedMapNodeData
        {
            NodeIndex = 0,
            Type = "Start",
            MapId = "start",
        });
        runtime.GeneratedNodes.Add(new GeneratedMapNodeData
        {
            NodeIndex = 1,
            Type = "Common",
            MapId = "battle_01",
        });
        runtime.GeneratedNodes.Add(new GeneratedMapNodeData
        {
            NodeIndex = 2,
            Type = "Common",
            MapId = "battle_02",
        });

        runtime.GeneratedNodes[1].NextNodeIndices.Add(2);

        return runtime;
    }
}
