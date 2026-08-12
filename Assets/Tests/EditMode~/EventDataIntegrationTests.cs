using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class EventDataIntegrationTests
{
    [TestCase("EVT001", "Event_01")]
    [TestCase("EVT002_A", "Event_02_A")]
    [TestCase("EVENT_001", "Event_01")]
    [TestCase("Event_2_B", "Event_02_B")]
    [TestCase("Event_09", "Event_09")]
    public void NormalizeEventId_ConvertsLegacyIdsToMapIds(string input, string expected)
    {
        Assert.That(EventIdUtility.Normalize(input), Is.EqualTo(expected));
    }

    [Test]
    public void EventCsvLoader_LoadsEventSheetAndNormalizesNextEventIds()
    {
        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
        {
            ["Event"] = new List<Dictionary<string, string>>
            {
                Row("EVT002", "2", "Second", "EVT002_A"),
                Row("EVT002", "1", "First", string.Empty),
                Row("EVT002_A", "1", "Follow", string.Empty)
            }
        };

        List<EventData> rows = EventCsvLoader.Load(workbook);
        EventDatabase database = new();
        database.Initialize(rows);

        EventDefinition definition = database.GetEvent("Event_02");

        Assert.That(definition, Is.Not.Null);
        Assert.That(definition.EventId, Is.EqualTo("Event_02"));
        Assert.That(definition.Choices, Has.Count.EqualTo(2));
        Assert.That(definition.Choices[0].ChoiceName, Is.EqualTo("First"));
        Assert.That(definition.Choices[1].NextEventId, Is.EqualTo("Event_02_A"));
        Assert.That(database.TryGetEvent("EVT002_A", out EventDefinition follow), Is.True);
        Assert.That(follow.EventId, Is.EqualTo("Event_02_A"));
    }

    private static Dictionary<string, string> Row(
        string eventId,
        string choiceOrder,
        string choiceName,
        string nextEventId)
    {
        return new Dictionary<string, string>
        {
            ["EventId"] = eventId,
            ["EventName"] = "Event Name",
            ["Title"] = "Event Title",
            ["ChoiceOrder"] = choiceOrder,
            ["ChoiceName"] = choiceName,
            ["ChoiceDesc"] = "Choice Desc",
            ["ChoiceType"] = "Immediate",
            ["ResultType"] = "EndEvent",
            ["NextEventId"] = nextEventId
        };
    }
}
