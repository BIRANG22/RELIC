using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Battle;
using UnityEngine;

public sealed class MoveDragPathStateTests
{
    [Test]
    public void ReservationPolicy_ExplicitDragIgnoresOccupiedUnitIndices()
    {
        HashSet<int> occupiedUnitIndices = new() { 4, 5 };
        HashSet<int> blockedGridEffectIndices = new();

        HashSet<int> blockedIndices = MoveDragReservationPolicy.BuildBlockedGridIndices(
            occupiedUnitIndices,
            blockedGridEffectIndices);

        Assert.That(blockedIndices, Does.Not.Contain(4));
        Assert.That(blockedIndices, Does.Not.Contain(5));
    }

    [Test]
    public void ReservationPolicy_ExplicitDragIgnoresBlockedGridEffectIndices()
    {
        HashSet<int> occupiedUnitIndices = new() { 4 };
        HashSet<int> blockedGridEffectIndices = new() { 7 };

        HashSet<int> blockedIndices = MoveDragReservationPolicy.BuildBlockedGridIndices(
            occupiedUnitIndices,
            blockedGridEffectIndices);

        Assert.That(blockedIndices, Is.Empty);
    }

    [TestCase(false, true, true)]
    [TestCase(true, true, false)]
    [TestCase(false, false, false)]
    public void ReservationPolicy_OccupiedDestinationOnlyBlocksAutomaticPath(
        bool hasExplicitDragPath,
        bool isOccupied,
        bool expected)
    {
        Assert.That(
            MoveDragReservationPolicy.ShouldRejectOccupiedDestination(
                hasExplicitDragPath,
                isOccupied),
            Is.EqualTo(expected));
    }

    [Test]
    public void TryVisit_AdjacentValidCoordinate_AppendsToPath()
    {
        MoveDragPathState state = new(new List<Vector2Int>
        {
            new(0, 0),
            new(1, 0)
        });

        bool changed = state.TryVisit(new Vector2Int(1, 1), true);

        Assert.That(changed, Is.True);
        Assert.That(state.Coordinates, Is.EqualTo(new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1)
        }));
    }

    [Test]
    public void TryVisit_NonAdjacentCoordinate_DoesNotChangePath()
    {
        MoveDragPathState state = new(new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        });

        bool changed = state.TryVisit(new Vector2Int(2, 1), true);

        Assert.That(changed, Is.False);
        Assert.That(state.Coordinates, Is.EqualTo(new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        }));
    }

    [Test]
    public void TryVisit_BlockedAdjacentCoordinate_DoesNotChangePath()
    {
        MoveDragPathState state = new(new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        });

        bool changed = state.TryVisit(new Vector2Int(1, 1), false);

        Assert.That(changed, Is.False);
        Assert.That(state.Coordinates, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryVisit_PreviousCoordinate_RemovesFollowingPath()
    {
        MoveDragPathState state = new(new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(2, 1)
        });

        bool changed = state.TryVisit(new Vector2Int(1, 0), false);

        Assert.That(changed, Is.True);
        Assert.That(state.Coordinates, Is.EqualTo(new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        }));
    }
}
