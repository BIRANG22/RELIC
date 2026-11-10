using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TimeAwareMovePathUtilityTests
{
    [Test]
    public void FollowingCharacter_CanEnterCellAfterLeaderLeaves()
    {
        List<TimedMoveTransition> reservations = new()
        {
            new TimedMoveTransition(0, new Vector2Int(1, 0), new Vector2Int(2, 0)),
            new TimedMoveTransition(1, new Vector2Int(2, 0), new Vector2Int(3, 0))
        };

        List<Vector2Int> path = TimeAwareMovePathUtility.FindShortestPath(
            Vector2Int.zero,
            new Vector2Int(2, 0),
            4,
            2,
            2,
            new HashSet<Vector2Int>(),
            reservations);

        CollectionAssert.AreEqual(
            new[] { Vector2Int.right, Vector2Int.right },
            path);
    }

    [Test]
    public void SameTimeVertexCollision_UsesShortestDetour()
    {
        List<TimedMoveTransition> reservations = new()
        {
            new TimedMoveTransition(0, new Vector2Int(1, 1), new Vector2Int(1, 0))
        };

        List<Vector2Int> path = TimeAwareMovePathUtility.FindShortestPath(
            Vector2Int.zero,
            new Vector2Int(2, 0),
            3,
            2,
            4,
            new HashSet<Vector2Int>(),
            reservations);

        Assert.That(path, Has.Count.EqualTo(4));
        Assert.That(path[0], Is.EqualTo(Vector2Int.up));
    }

    [Test]
    public void OppositeEdgeSwap_UsesDetour()
    {
        List<TimedMoveTransition> reservations = new()
        {
            new TimedMoveTransition(0, Vector2Int.right, Vector2Int.zero)
        };

        List<Vector2Int> path = TimeAwareMovePathUtility.FindShortestPath(
            Vector2Int.zero,
            Vector2Int.right,
            2,
            2,
            3,
            new HashSet<Vector2Int>(),
            reservations);

        Assert.That(path, Has.Count.EqualTo(3));
        Assert.That(path[0], Is.EqualTo(Vector2Int.up));
    }

    [Test]
    public void DetourBeyondMaxSteps_ReturnsNull()
    {
        List<TimedMoveTransition> reservations = new()
        {
            new TimedMoveTransition(0, new Vector2Int(1, 1), new Vector2Int(1, 0))
        };

        List<Vector2Int> path = TimeAwareMovePathUtility.FindShortestPath(
            Vector2Int.zero,
            new Vector2Int(2, 0),
            3,
            2,
            3,
            new HashSet<Vector2Int>(),
            reservations);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void NoTimedReservations_ReturnsManhattanShortestPath()
    {
        List<Vector2Int> path = TimeAwareMovePathUtility.FindShortestPath(
            Vector2Int.zero,
            new Vector2Int(2, 1),
            3,
            2,
            3,
            new HashSet<Vector2Int>(),
            new List<TimedMoveTransition>());

        Assert.That(path, Has.Count.EqualTo(3));
        Assert.That(Sum(path), Is.EqualTo(new Vector2Int(2, 1)));
    }

    private static Vector2Int Sum(IReadOnlyList<Vector2Int> path)
    {
        Vector2Int result = Vector2Int.zero;

        for (int i = 0; i < path.Count; i++)
            result += path[i];

        return result;
    }
}
