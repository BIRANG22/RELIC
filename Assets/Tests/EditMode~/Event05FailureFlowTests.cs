using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class Event05FailureFlowTests
{
    [TestCase(1)]
    [TestCase(2)]
    public void ExecuteChoice_DiceFailureIsAcceptedButNotSucceeded(int choiceOrder)
    {
        EventChoiceExecutionContext context = new()
        {
            RollThreeDice = () => 3
        };
        EventData choice = CreateEvent05MiningChoice(choiceOrder);
        choice.SuccessCondition = "9~18";

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.That(result.Accepted, Is.True);
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.NextEventId, Is.EqualTo("Event_05"));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void EventRoomController_Event05FailedMiningChoiceShowsNextButtonAndLocksChoices(
        int failingChoiceOrder)
    {
        GameObject dataManagerObject = new("DataManager");
        GameObject eventRoomObject = new("EventRoom");
        GameObject nextButtonObject = new("NextButton");
        GameObject firstChoiceObject = new("ChoiceSlot1");
        GameObject secondChoiceObject = new("ChoiceSlot2");

        try
        {
            DataManager dataManager = dataManagerObject.AddComponent<DataManager>();
            EventData firstChoice = CreateEvent05MiningChoice(1);
            EventData secondChoice = CreateEvent05MiningChoice(2);
            EventData failingChoice = failingChoiceOrder == 1 ? firstChoice : secondChoice;
            EventDefinition definition = new()
            {
                EventId = "Event_05",
                Choices = new List<EventData>
                {
                    firstChoice,
                    secondChoice
                }
            };
            dataManager.EventDatabase.Initialize(definition.Choices);

            EventRoomController controller = eventRoomObject.AddComponent<EventRoomController>();
            EventChoiceSlotUI firstSlot = CreateChoiceSlot(firstChoiceObject);
            EventChoiceSlotUI secondSlot = CreateChoiceSlot(secondChoiceObject);
            Button firstButton = firstChoiceObject.GetComponent<Button>();
            Button secondButton = secondChoiceObject.GetComponent<Button>();
            nextButtonObject.SetActive(false);

            SetPrivateField(controller, "nextButtonRoot", nextButtonObject);
            SetPrivateField(controller, "choiceSlots", new[] { firstSlot, secondSlot });
            InvokePrivate(controller, "LoadEventDefinition", definition, string.Empty);

            InvokePrivate(
                controller,
                "ExecuteEventChoice",
                failingChoice,
                default(EventChoiceEquippedRelicCost));

            Assert.That(nextButtonObject.activeSelf, Is.True);
            Assert.That(firstButton.interactable, Is.False);
            Assert.That(secondButton.interactable, Is.False);
            Assert.That(GetPrivateField<bool>(controller, "isEventResolved"), Is.True);
        }
        finally
        {
            DestroyObject(dataManagerObject);
            DestroyObject(eventRoomObject);
            DestroyObject(nextButtonObject);
            DestroyObject(firstChoiceObject);
            DestroyObject(secondChoiceObject);
        }
    }

    private static EventData CreateEvent05MiningChoice(int choiceOrder)
    {
        return new EventData
        {
            EventId = "Event_05",
            ChoiceOrder = choiceOrder,
            ChoiceName = choiceOrder == 1 ? "Small Mining" : "Large Mining",
            ChoiceType = "Dice",
            SuccessCondition = "99~100",
            FailResult = "3~18: failed",
            NextEventId = "Event_05"
        };
    }

    private static EventChoiceSlotUI CreateChoiceSlot(GameObject slotObject)
    {
        slotObject.AddComponent<Button>();
        return slotObject.AddComponent<EventChoiceSlotUI>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
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

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method.Invoke(target, args);
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }
}
