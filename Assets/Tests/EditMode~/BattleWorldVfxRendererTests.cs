using NUnit.Framework;

public class BattleWorldVfxRendererTests
{
    [Test]
    public void BattleVfxEntry_DefaultsToIndividualWorldRenderTexture()
    {
        BattleVfxEntry entry = new();

        Assert.That(entry.renderMode, Is.EqualTo(BattleVfxRenderMode.IndividualWorldRenderTexture));
        Assert.That(entry.renderTextureWidth, Is.GreaterThan(0));
        Assert.That(entry.renderTextureHeight, Is.GreaterThan(0));
        Assert.That(entry.proxySortingLayerName, Is.EqualTo("Unit"));
    }

    [Test]
    public void SortUtility_UsesSameNegativeYConventionAsUnitYSort()
    {
        int order = BattleWorldVfxSortUtility.CalculateSortingOrder(
            y: 1.25f,
            yMultiplier: 100f,
            offset: 7);

        Assert.That(order, Is.EqualTo(-118));
    }
}
