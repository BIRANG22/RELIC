using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class EventChoiceVisualActionTests
{
    [Test]
    public void ExecuteChoice_ReturnsSuccessVisualActionWhenChoiceSucceeds()
    {
        EventData choice = new()
        {
            ChoiceType = "Immediate",
            ResultType = "EndEvent",
            SuccessVisualObjectId = "event_visual_test_crystal",
            SuccessVisualActionId = "event_choice_success"
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(
            choice,
            new EventChoiceExecutionContext());

        Assert.That(result.Accepted, Is.True);
        Assert.That(result.VisualObjectId, Is.EqualTo("event_visual_test_crystal"));
        Assert.That(result.VisualActionId, Is.EqualTo("event_choice_success"));
        Assert.That(result.HasVisualAction, Is.True);
    }

    [Test]
    public void ExecuteChoice_ReturnsFailureVisualActionWhenDiceFails()
    {
        EventData choice = new()
        {
            ChoiceType = "Dice",
            SuccessCondition = "11~18",
            FailResult = "failed",
            FailureVisualObjectId = "event_visual_test_crystal",
            FailureVisualActionId = "event_choice_failure"
        };
        EventChoiceExecutionContext context = new()
        {
            RollThreeDice = () => 3
        };

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(choice, context);

        Assert.That(result.Accepted, Is.True);
        Assert.That(result.VisualObjectId, Is.EqualTo("event_visual_test_crystal"));
        Assert.That(result.VisualActionId, Is.EqualTo("event_choice_failure"));
        Assert.That(result.HasVisualAction, Is.True);
    }

    [Test]
    public void EventCsvLoader_LoadsVisualActionColumns()
    {
        Dictionary<string, string> row = new()
        {
            ["EventId"] = "Event_01",
            ["EventName"] = "Event Name",
            ["Title"] = "Event Title",
            ["ChoiceOrder"] = "1",
            ["ChoiceName"] = "Choice",
            ["ChoiceDesc"] = "Choice Desc",
            ["ChoiceType"] = "Immediate",
            ["ResultType"] = "EndEvent",
            ["SuccessVisualObjectId"] = "event_visual_test_crystal",
            ["SuccessVisualActionId"] = "event_choice_success",
            ["FailureVisualObjectId"] = "event_visual_test_crystal",
            ["FailureVisualActionId"] = "event_choice_failure"
        };
        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
        {
            ["Event"] = new List<Dictionary<string, string>> { row }
        };

        List<EventData> rows = EventCsvLoader.Load(workbook);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].SuccessVisualObjectId, Is.EqualTo("event_visual_test_crystal"));
        Assert.That(rows[0].SuccessVisualActionId, Is.EqualTo("event_choice_success"));
        Assert.That(rows[0].FailureVisualObjectId, Is.EqualTo("event_visual_test_crystal"));
        Assert.That(rows[0].FailureVisualActionId, Is.EqualTo("event_choice_failure"));
    }

    [Test]
    public void GameDataRuntime_EventSheetContainsVisualActionSample()
    {
        string csvText = File.ReadAllText("Assets/Resources/Data/GameDataRuntime.csv");

        Assert.That(csvText, Does.Contain("SuccessVisualObjectId"));
        Assert.That(csvText, Does.Contain("SuccessVisualActionId"));
        Assert.That(csvText, Does.Contain("FailureVisualObjectId"));
        Assert.That(csvText, Does.Contain("FailureVisualActionId"));
        Assert.That(csvText, Does.Contain("event_visual_test_crystal,event_choice_success"));
    }
}
