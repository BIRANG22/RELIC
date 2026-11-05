using NUnit.Framework;
using Relic.Gameplay.Battle;

public sealed class BattleGridEffectSortingPolicyTests
{
    [Test]
    public void ResolveOrder_PlacesGridEffectBetweenUnitAndShadowOnSameCell()
    {
        const float cellWorldY = -1.2f;

        int effectOrder = BattleGridEffectSortingPolicy.ResolveOrder(
            cellWorldY,
            100f,
            -1,
            0);
        BattleUnitHighlightSortingOrders unitOrders =
            BattleUnitHighlightSortingPolicy.Resolve(120);

        Assert.That(effectOrder, Is.EqualTo(unitOrders.Highlight));
        Assert.That(effectOrder, Is.GreaterThan(unitOrders.Shadow));
    }

    [Test]
    public void ResolveOrder_LowerCellDrawsAboveUpperCellShadow()
    {
        int lowerEffectOrder = BattleGridEffectSortingPolicy.ResolveOrder(
            -1.4f,
            100f,
            -1,
            0);
        int upperShadowOrder = BattleUnitHighlightSortingPolicy.Resolve(120).Shadow;

        Assert.That(lowerEffectOrder, Is.GreaterThan(upperShadowOrder));
    }

    [TestCase(-1.2f, 0, 119)]
    [TestCase(-1.2f, 3, 122)]
    [TestCase(0.25f, -2, -28)]
    public void ResolveOrder_PreservesPrefabRelativeOrder(
        float cellWorldY,
        int prefabOrder,
        int expected)
    {
        Assert.That(
            BattleGridEffectSortingPolicy.ResolveOrder(
                cellWorldY,
                100f,
                -1,
                prefabOrder),
            Is.EqualTo(expected));
    }
}
