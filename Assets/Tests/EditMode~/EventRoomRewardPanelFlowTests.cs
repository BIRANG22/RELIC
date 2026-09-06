using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Relic.Gameplay.Data;
using UnityEngine;
using Object = UnityEngine.Object;

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
    public void CanSkipUnresolvedEvent_AllowsEvent02A()
    {
        EventDefinition definition = new()
        {
            EventId = "Event_02_A"
        };

        Assert.That(
            EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(definition),
            Is.True);
    }

    [Test]
    public void CanSkipUnresolvedEvent_BlocksRegularEvent()
    {
        EventDefinition definition = new()
        {
            EventId = "Event_06"
        };

        Assert.That(
            EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(definition),
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
    public void TryOpenPendingEventRewardPanel_WhenDelayed_DoesNotActivateRewardPanelImmediately()
    {
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonRoot = new("NextButtonRoot");
        GameObject rewardPanelObject = new("BattleRewardPanelUI");

        try
        {
            EventRoomController controller =
                eventRoomObject.AddComponent<EventRoomController>();
            BattleRewardPanelUI rewardPanel =
                rewardPanelObject.AddComponent<BattleRewardPanelUI>();

            nextButtonRoot.SetActive(true);
            rewardPanelObject.SetActive(false);

            SetPrivateField(controller, "nextButtonRoot", nextButtonRoot);
            SetPrivateField(controller, "rewardPanel", rewardPanel);
            SetPrivateField(controller, "eventRewardPanelOpenDelay", 0.6f);

            List<BattleRewardData> pendingRewards =
                GetPrivateField<List<BattleRewardData>>(
                    controller,
                    "pendingEventRewards");
            pendingRewards.Add(new BattleRewardData
            {
                Type = BattleRewardType.Remnant,
                RewardId = "0",
                Amount = 30,
                Name = "더스티움"
            });

            MethodInfo openMethod = typeof(EventRoomController).GetMethod(
                "TryOpenPendingEventRewardPanel",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool) },
                null);
            Assert.That(openMethod, Is.Not.Null);

            bool opened = (bool)openMethod.Invoke(
                controller,
                new object[] { true });

            Assert.That(opened, Is.True);
            Assert.That(rewardPanelObject.activeSelf, Is.False);
            Assert.That(nextButtonRoot.activeSelf, Is.False);
            Assert.That(
                GetPrivateField<bool>(
                    controller,
                    "isEventRewardPanelOpen"),
                Is.True);
            Assert.That(pendingRewards, Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(eventRoomObject);
            Object.DestroyImmediate(nextButtonRoot);
            Object.DestroyImmediate(rewardPanelObject);
        }
    }

    [Test]
    public void OnEventRewardPanelCompleted_ShowsNextButtonInsteadOfReturningImmediately()
    {
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonRoot = new("NextButtonRoot");

        try
        {
            EventRoomController controller =
                eventRoomObject.AddComponent<EventRoomController>();

            nextButtonRoot.SetActive(false);
            SetPrivateField(controller, "nextButtonRoot", nextButtonRoot);
            SetPrivateField(controller, "isEventRewardPanelOpen", true);

            MethodInfo completedMethod = typeof(EventRoomController).GetMethod(
                "OnEventRewardPanelCompleted",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(completedMethod, Is.Not.Null);

            completedMethod.Invoke(controller, null);

            Assert.That(
                GetPrivateField<bool>(controller, "isEventRewardPanelOpen"),
                Is.False);
            Assert.That(nextButtonRoot.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(eventRoomObject);
            Object.DestroyImmediate(nextButtonRoot);
        }
    }

    [Test]
    public void OnNextButtonClicked_WhenReturningToMap_HidesSharedNextButton()
    {
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonRoot = new("NextButtonRoot");

        try
        {
            EventRoomController controller =
                eventRoomObject.AddComponent<EventRoomController>();

            nextButtonRoot.SetActive(true);
            SetPrivateField(controller, "nextButtonRoot", nextButtonRoot);
            SetPrivateField(controller, "isChestOpened", true);

            controller.OnNextButtonClicked();

            Assert.That(nextButtonRoot.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(eventRoomObject);
            Object.DestroyImmediate(nextButtonRoot);
        }
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

    private static void SetPrivateField(
        object target,
        string fieldName,
        object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }
}
