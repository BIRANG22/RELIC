using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class RegularBattleTerminalEndingPolicyTests
{
    [Test]
    public void ShouldShow_WhenRegularVictoryClearsTerminalNode_ReturnsTrue()
    {
        Assert.That(RegularBattleTerminalEndingPolicy.ShouldShow(
            CreateBattle(), CreateMap(isCleared: true), isDefeat: false), Is.True);
    }

    [Test]
    public void ShouldShow_WhenRegularBattleHasNextNode_ReturnsFalse()
    {
        MapRuntimeData map = CreateMap(isCleared: true);
        map.GeneratedNodes[0].NextNodeIndices.Add(2);
        map.GeneratedNodes.Add(new GeneratedMapNodeData { NodeIndex = 2, Type = "Boss" });

        Assert.That(RegularBattleTerminalEndingPolicy.ShouldShow(
            CreateBattle(), map, isDefeat: false), Is.False);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void ShouldShow_WhenBattleUsesSpecialFlow_ReturnsFalse(bool isDemo, bool isTutorial)
    {
        BattleRuntimeData battle = CreateBattle();
        battle.IsDemoBattle = isDemo;
        battle.IsTutorialBattle = isTutorial;

        Assert.That(RegularBattleTerminalEndingPolicy.ShouldShow(
            battle, CreateMap(isCleared: true), isDefeat: false), Is.False);
    }

    [Test]
    public void ShouldShow_WhenResultIsDefeat_ReturnsFalse()
    {
        Assert.That(RegularBattleTerminalEndingPolicy.ShouldShow(
            CreateBattle(), CreateMap(isCleared: true), isDefeat: true), Is.False);
    }

    [Test]
    public void ShouldShow_WhenCurrentNodeIsNotCleared_ReturnsFalse()
    {
        Assert.That(RegularBattleTerminalEndingPolicy.ShouldShow(
            CreateBattle(), CreateMap(isCleared: false), isDefeat: false), Is.False);
    }

    [Test]
    public void ShouldShow_WhenRuntimeDataIsMissing_ReturnsFalse()
    {
        Assert.That(RegularBattleTerminalEndingPolicy.ShouldShow(null, null, isDefeat: false), Is.False);
        Assert.That(RegularBattleTerminalEndingPolicy.ShouldShow(CreateBattle(), null, isDefeat: false), Is.False);
    }

    [Test]
    public void ShouldEnter_WhenTerminalEndingHasNotBeenShown_ReturnsTrue()
    {
        Assert.That(RegularBattleTerminalEndingPolicy.ShouldEnter(
            CreateBattle(), CreateMap(isCleared: true), isDefeat: false, alreadyShown: false), Is.True);
    }

    [Test]
    public void ShouldEnter_WhenTerminalEndingWasAlreadyShown_ReturnsFalse()
    {
        Assert.That(RegularBattleTerminalEndingPolicy.ShouldEnter(
            CreateBattle(), CreateMap(isCleared: true), isDefeat: false, alreadyShown: true), Is.False);
    }

    private static BattleRuntimeData CreateBattle()
    {
        return new BattleRuntimeData
        {
            IsBattleRunInitialized = true,
            IsDemoBattle = false,
            IsTutorialBattle = false
        };
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
                    Type = "Boss",
                    NextNodeIndices = new List<int>()
                }
            }
        };

        if (isCleared)
            map.ClearedMapIds.Add("1");

        return map;
    }
}
