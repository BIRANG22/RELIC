namespace Relic.Gameplay.Battle
{
    public readonly struct BattleUnitHighlightSortingOrders
    {
        public int Unit { get; }
        public int Highlight { get; }
        public int Shadow { get; }

        public BattleUnitHighlightSortingOrders(int unit, int highlight, int shadow)
        {
            Unit = unit;
            Highlight = highlight;
            Shadow = shadow;
        }
    }

    public static class BattleUnitHighlightSortingPolicy
    {
        public static BattleUnitHighlightSortingOrders Resolve(int unitSortingOrder)
        {
            return new BattleUnitHighlightSortingOrders(
                unitSortingOrder,
                unitSortingOrder - 1,
                unitSortingOrder - 2);
        }
    }
}
