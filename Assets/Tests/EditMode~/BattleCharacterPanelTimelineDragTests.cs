using NUnit.Framework;

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
}
