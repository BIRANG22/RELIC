using System.Collections.Generic;
using NUnit.Framework;

public class TimelineReservationLayoutUtilityTests
{
    [Test]
    public void ReorderSameList_MovingFirstBetweenSecondAndThird_ShiftsExistingCommands()
    {
        object first = new();
        object second = new();
        object third = new();
        List<object> commands = new() { first, second, third };

        List<object> result = TimelineReservationLayoutUtility.BuildReorderedList(
            commands, first, 2);

        Assert.That(result, Is.EqualTo(new[] { second, first, third }));
        Assert.That(commands, Is.EqualTo(new[] { first, second, third }));
    }

    [Test]
    public void ReorderSameList_MovingLastToFront_ShiftsExistingCommands()
    {
        object first = new();
        object second = new();
        object third = new();

        List<object> result = TimelineReservationLayoutUtility.BuildReorderedList(
            new List<object> { first, second, third }, third, 0);

        Assert.That(result, Is.EqualTo(new[] { third, first, second }));
    }

    [Test]
    public void ReorderSameList_StaleCommand_ReturnsNull()
    {
        object first = new();
        object stale = new();

        Assert.That(
            TimelineReservationLayoutUtility.BuildReorderedList(
                new List<object> { first }, stale, 0),
            Is.Null);
    }

    [TestCase(-5f, 0)]
    [TestCase(5f, 1)]
    [TestCase(15f, 2)]
    [TestCase(30f, 3)]
    public void FindNearestInsertion_MapsPointerBetweenActionCenters(float pointer, int expected)
    {
        Assert.That(
            TimelineReservationLayoutUtility.FindNearestInsertion(
                pointer,
                new[] { 0f, 10f, 20f }),
            Is.EqualTo(expected));
    }
}
