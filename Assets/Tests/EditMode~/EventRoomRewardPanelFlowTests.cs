using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Relic.Gameplay.Data;

public class EventRoomRewardPanelFlowTests
{
    [Test]
    public void ShouldOpenPendingRewards_WhenAcceptedChoiceHasNoNextEvent()
    {
        EventChoiceExecutionResult result = new(
            true,
            "보상 획득",
            string.Empty);

        Assert.That(
            EventRoomRewardFlowUtility.ShouldOpenPendingRewards(result, 1),
            Is.True);
    }

    [Test]
    public void ShouldKeepPendingRewards_WhenAcceptedChoiceContinuesToNextEvent()
    {
        EventChoiceExecutionResult result = new(
            true,
            "보상 획득",
            "Event_02");

        Assert.That(
            EventRoomRewardFlowUtility.ShouldOpenPendingRewards(result, 1),
            Is.False);
        Assert.That(
            EventRoomRewardFlowUtility.ShouldKeepRewardsPending(result, 1),
            Is.True);
    }

    [Test]
    public void ShouldOpenPendingRewards_WhenNextEventIdDoesNotContinueToLoadedEvent()
    {
        EventChoiceExecutionResult result = new(
            true,
            "보상 획득",
            "Missing_Event");

        Assert.That(
            EventRoomRewardFlowUtility.ShouldOpenPendingRewards(result, 1, false),
            Is.True);
        Assert.That(
            EventRoomRewardFlowUtility.ShouldOpenPendingRewards(result, 1, true),
            Is.False);
    }

    [Test]
    public void CreateRemnantReward_UsesBattleRewardPanelDataContract()
    {
        BattleRewardData reward = EventRoomRewardFlowUtility.CreateRemnantReward(30);

        Assert.That(reward.Type, Is.EqualTo(BattleRewardType.Remnant));
        Assert.That(reward.RewardId, Is.EqualTo("0"));
        Assert.That(reward.Amount, Is.EqualTo(30));
        Assert.That(reward.Name, Is.EqualTo("더스티움"));
    }

    [Test]
    public void ExecuteGainRemnant_WithRewardGrantCallbackDoesNotApplyRuntimeImmediately()
    {
        BattleRuntimeData runtime = new()
        {
            Remnant = 5
        };
        int queuedAmount = 0;
        EventChoiceExecutionContext context = new()
        {
            BattleRuntime = runtime,
            GrantRemnant = (int amount, out string message) =>
            {
                queuedAmount += amount;
                message = $"레드 더스티움 {amount} 획득";
                return true;
            }
        };
        EventData choice = new()
        {
            ResultType = "Gain",
            ResultTarget = "레드 더스티움",
            ResultValue = "30"
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.That(result.ResultMessage, Does.Contain("30"));
        Assert.That(queuedAmount, Is.EqualTo(30));
        Assert.That(runtime.Remnant, Is.EqualTo(5));
    }

    [Test]
    public void ExecuteGainRandomRelic_WhenRewardTextSuppressed_LeavesResultTextEmpty()
    {
        EventChoiceExecutionContext context = new()
        {
            SuppressRewardResultMessages = true,
            GrantRandomRelic = (out string message) =>
            {
                message = "유물 획득: 테스트 유물";
                return true;
            }
        };
        EventData choice = new()
        {
            ResultType = "GainRandom",
            ResultTarget = "유물",
            ResultValue = "1개"
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.That(result.ResultMessage, Is.Empty);
    }

    [Test]
    public void ExecuteGainRemnant_WhenRewardTextSuppressed_QueuesWithoutResultText()
    {
        int queuedAmount = 0;
        EventChoiceExecutionContext context = new()
        {
            SuppressRewardResultMessages = true,
            GrantRemnant = (int amount, out string message) =>
            {
                queuedAmount += amount;
                message = $"레드 더스티움 {amount} 획득";
                return true;
            }
        };
        EventData choice = new()
        {
            ResultType = "Gain",
            ResultTarget = "레드 더스티움",
            ResultValue = "30"
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.That(queuedAmount, Is.EqualTo(30));
        Assert.That(result.ResultMessage, Is.Empty);
    }

    [Test]
    public void EventRoomController_UsesSharedBattleRewardPanelReference()
    {
        FieldInfo field = typeof(EventRoomController).GetField(
            "rewardPanel",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        Assert.That(field.FieldType, Is.EqualTo(typeof(BattleRewardPanelUI)));
    }

    [Test]
    public void SharedRewardPanel_ExposesRewardOpenContract()
    {
        MethodInfo openMethod = typeof(BattleRewardPanelUI).GetMethod(
            "Open",
            new[] { typeof(List<BattleRewardData>), typeof(Action) });

        Assert.That(openMethod, Is.Not.Null);
    }
}
