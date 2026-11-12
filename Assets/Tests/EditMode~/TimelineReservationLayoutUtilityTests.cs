using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TimelineReservationLayoutUtilityTests
{
    [TestCase(1, 0)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(99, 2)]
    public void ResolveReservationDragHighlightSprite_UsesOccupiedOrderVariant(
        int occupiedOrders,
        int expectedIndex)
    {
        Texture2D texture = new Texture2D(2, 2);
        Sprite[] sprites =
        {
            Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f),
            Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f),
            Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f)
        };
        try
        {
            Assert.That(
                BattleTimelineGroupUI.ResolveReservationDragHighlightSprite(
                    occupiedOrders,
                    sprites[0],
                    sprites[1],
                    sprites[2]),
                Is.SameAs(sprites[expectedIndex]));
        }
        finally
        {
            for (int i = 0; i < sprites.Length; i++)
                Object.DestroyImmediate(sprites[i]);
            Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void BuildInsertionPreview_NewTwoCellAction_ShiftsFollowingActionsByTwoCells()
    {
        TimelineReservationLayoutUtility.InsertionPreviewLayout result =
            TimelineReservationLayoutUtility.BuildInsertionPreview(
                new[] { 1, 1, 1 }, -1, 1, 2, 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PlaceholderStart, Is.EqualTo(1));
        Assert.That(result.ItemStarts, Is.EqualTo(new[] { 0, 3, 4 }));
    }

    [Test]
    public void BuildInsertionPreview_MovingTwoCellAction_RemovesOriginalSpaceBeforeInsertion()
    {
        TimelineReservationLayoutUtility.InsertionPreviewLayout result =
            TimelineReservationLayoutUtility.BuildInsertionPreview(
                new[] { 2, 1, 1 }, 0, 2, 2, 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.PlaceholderStart, Is.EqualTo(2));
        Assert.That(result.ItemStarts, Is.EqualTo(new[] { -1, 0, 1 }));
    }

    [Test]
    public void BuildInsertionPreview_WhenCapacityExceeded_ReturnsNull()
    {
        Assert.That(
            TimelineReservationLayoutUtility.BuildInsertionPreview(
                new[] { 2, 2 }, -1, 1, 2, 5),
            Is.Null);
    }

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
