using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleDebugExplorationEndTests
{
    [Test]
    public void DebugHotkey_DefaultsToF8AndUsesDedicatedExplorationEndPath()
    {
        GameObject gameObject = new("BattleDebugExplorationEndTests");
        BattleDebugKillAllMonsters debugInput = gameObject.AddComponent<BattleDebugKillAllMonsters>();
        FieldInfo endExplorationKeyField = typeof(BattleDebugKillAllMonsters).GetField(
            "endExplorationKey",
            BindingFlags.Instance | BindingFlags.NonPublic);
        string resultSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs");

        try
        {
            Assert.That(endExplorationKeyField, Is.Not.Null);
            Assert.That(endExplorationKeyField.GetValue(debugInput), Is.EqualTo(KeyCode.F8));
            Assert.That(resultSource, Does.Contain("public bool EndExplorationForDebug()"));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void DebugExplorationEnd_DoesNotResolveRewardsOrClearCurrentNode()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs");
        int methodStart = source.IndexOf("public bool EndExplorationForDebug()", System.StringComparison.Ordinal);
        int nextMethod = source.IndexOf("\n    public bool CheckBattleEnd", methodStart, System.StringComparison.Ordinal);

        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(nextMethod, Is.GreaterThan(methodStart));
        string method = source.Substring(methodStart, nextMethod - methodStart);
        Assert.That(method, Does.Not.Contain("OpenRewardPanel"));
        Assert.That(method, Does.Not.Contain("MarkCurrentBattleNodeCleared"));
    }
}
