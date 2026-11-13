using NUnit.Framework;
using Relic.Gameplay.Data;

public class BattleCharacterPanelTimelineDragTests
{
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    public void CanBeginNewSkillDrag_RequiresInputAndPreviewResources(
        bool canAcceptInput,
        bool hasPreviewResources,
        bool expected)
    {
        Assert.That(
            BattleCharacterPanelUI.CanBeginNewSkillDrag(
                canAcceptInput,
                hasPreviewResources),
            Is.EqualTo(expected));
    }

    [Test]
    public void CanAffordPreviewCosts_RejectsSecondaryManaCost()
    {
        Assert.That(
            BattleCharacterPanelUI.CanAffordPreviewCosts(
                previewHP: 10,
                previewCost: 0,
                previewResource: 5,
                previewShield: 0,
                hpCost: 0,
                cost: 1,
                resourceCost: 5,
                shieldCost: 0),
            Is.False);
    }

    [Test]
    public void CanAffordPreviewCosts_RequiresHpToRemainPositive()
    {
        Assert.That(
            BattleCharacterPanelUI.CanAffordPreviewCosts(
                previewHP: 5,
                previewCost: 10,
                previewResource: 10,
                previewShield: 10,
                hpCost: 5,
                cost: 0,
                resourceCost: 0,
                shieldCost: 0),
            Is.False);
    }

    [Test]
    public void BeginNewFacingFlip_CreatesDistinctSingleOrderContext()
    {
        var runtime = new CharacterRuntimeData
        {
            CharacterId = "Char01",
            CurrentHP = 10
        };
        var moveSkill = new SkillMasterData
        {
            SkillId = "S_Move_1",
            Category = Category.Move
        };

        TimelineReservationDragContext.BeginNewFacingFlip(
            runtime,
            moveSkill,
            null,
            null);

        Assert.That(TimelineReservationDragContext.Active, Is.Not.Null);
        Assert.That(TimelineReservationDragContext.Active.IsFacingFlip, Is.True);
        Assert.That(TimelineReservationDragContext.Active.IsExistingReservation, Is.False);
        Assert.That(TimelineReservationDragContext.Active.OccupiedOrders, Is.EqualTo(0));

        TimelineReservationDragContext.End();
    }
}
