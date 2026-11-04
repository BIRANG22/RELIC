using NUnit.Framework;
using Relic.Gameplay.Battle;

public sealed class BattleUnitHighlightSortingPolicyTests
{
    [Test]
    public void ResolveOrders_PlacesHighlightBetweenUnitAndShadow()
    {
        BattleUnitHighlightSortingOrders orders =
            BattleUnitHighlightSortingPolicy.Resolve(120);

        Assert.That(orders.Unit, Is.EqualTo(120));
        Assert.That(orders.Highlight, Is.EqualTo(119));
        Assert.That(orders.Shadow, Is.EqualTo(118));
    }
}
