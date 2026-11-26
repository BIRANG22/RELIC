using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Monster;
using UnityEngine;

public sealed class NocturnPortalLandingPolicyTests
{
    [TestCase(BattleDirection.Right, 1, BattleDirection.Right)]
    [TestCase(BattleDirection.Left, 3, BattleDirection.Left)]
    public void Select_PrefersRearWhenBothSidesAreAvailable(
        BattleDirection targetDirection, int landingX, BattleDirection attackDirection)
    {
        bool found = NocturnPortalLandingPolicy.TrySelect(
            new Vector2Int(2, 1), targetDirection, _ => true,
            out Vector2Int landing, out BattleDirection direction);

        Assert.That(found, Is.True);
        Assert.That(landing, Is.EqualTo(new Vector2Int(landingX, 1)));
        Assert.That(direction, Is.EqualTo(attackDirection));
    }

    [TestCase(0, BattleDirection.Right, 1, BattleDirection.Left)]
    [TestCase(4, BattleDirection.Left, 3, BattleDirection.Right)]
    public void Select_UsesFrontWhenRearIsOutsideTheBoard(
        int targetX, BattleDirection targetDirection, int landingX, BattleDirection attackDirection)
    {
        bool found = NocturnPortalLandingPolicy.TrySelect(
            new Vector2Int(targetX, 1), targetDirection, cell => cell.x >= 0 && cell.x < 5,
            out Vector2Int landing, out BattleDirection direction);

        Assert.That(found, Is.True);
        Assert.That(landing, Is.EqualTo(new Vector2Int(landingX, 1)));
        Assert.That(direction, Is.EqualTo(attackDirection));
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Select_UsesFrontWhenRearCannotBeEntered(bool rearOccupied, bool rearBlocked)
    {
        bool found = NocturnPortalLandingPolicy.TrySelect(
            new Vector2Int(2, 1), BattleDirection.Right,
            cell => cell.x != 1 || (!rearOccupied && !rearBlocked),
            out Vector2Int landing, out BattleDirection direction);

        Assert.That(found, Is.True);
        Assert.That(landing, Is.EqualTo(new Vector2Int(3, 1)));
        Assert.That(direction, Is.EqualTo(BattleDirection.Left));
    }

    [Test]
    public void Select_RejectsTargetWhenBothSidesAreUnavailable()
    {
        bool found = NocturnPortalLandingPolicy.TrySelect(
            new Vector2Int(2, 1), BattleDirection.Right, _ => false, out _, out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void SelectAfterPull_UsesFrontVacatedByAnotherCharacterWhenRearWillBeOccupied()
    {
        Dictionary<int, int> pullDestinations = new() { { 0, 1 }, { 3, 4 } };
        bool found = NocturnPortalLandingPolicy.TrySelect(
            new Vector2Int(2, 1), BattleDirection.Right,
            cell => !NocturnPortalLandingPolicy.IsCharacterOccupied(
                cell.x, pullDestinations, index => index == 0 || index == 3),
            out Vector2Int landing, out BattleDirection direction);

        Assert.That(found, Is.True);
        Assert.That(landing, Is.EqualTo(new Vector2Int(3, 1)));
        Assert.That(direction, Is.EqualTo(BattleDirection.Left));
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, false)]
    public void OccupancyAfterPull_UsesDestinationAndKeepsUnmovedCharacter(int index, bool isOccupied)
    {
        Dictionary<int, int> pullDestinations = new() { { 0, 1 } };

        Assert.That(NocturnPortalLandingPolicy.IsCharacterOccupied(
            index, pullDestinations, current => current == 0 || current == 2), Is.EqualTo(isOccupied));
    }
}
