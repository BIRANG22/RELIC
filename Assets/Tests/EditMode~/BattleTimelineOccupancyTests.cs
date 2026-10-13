using NUnit.Framework;
using Relic.Gameplay.Data;

public class BattleTimelineOccupancyTests
{
    [Test]
    public void MonsterCommand_NormalizesMissingTimelineEntryCostToOne()
    {
        MonsterSkillData skill = new MonsterSkillData
        {
            TimelineEntryCost = 0
        };

        MonsterReservedCommand command = new MonsterReservedCommand(null, skill);

        Assert.That(command.TimelineEntryCost, Is.EqualTo(1));
    }

    [Test]
    public void CanFit_RejectsTwoOccupiedOrdersWhenOnlyOneRemains()
    {
        bool canFit = BattleTimelineCapacityPolicy.CanFit(
            ReserveTurnSlotUI.MaxCommandCount,
            currentOccupiedOrders: 4,
            requestedOccupiedOrders: 2);

        Assert.That(canFit, Is.False);
    }

    [Test]
    public void CanFit_AllowsOneOccupiedOrderWhenOneRemains()
    {
        bool canFit = BattleTimelineCapacityPolicy.CanFit(
            ReserveTurnSlotUI.MaxCommandCount,
            currentOccupiedOrders: 4,
            requestedOccupiedOrders: 1);

        Assert.That(canFit, Is.True);
    }
}
