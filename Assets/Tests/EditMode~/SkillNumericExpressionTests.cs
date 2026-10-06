using NUnit.Framework;
using Relic.Gameplay.Data;

public class SkillNumericExpressionTests
{
    [Test]
    public void TryParse_BareX_TreatsItAsOneX()
    {
        Assert.That(SkillNumericExpression.TryParse("X", out SkillNumericExpression expression), Is.True);
        Assert.That(expression.UnitValue, Is.EqualTo(1));
        Assert.That(expression.UsesX, Is.True);
        Assert.That(expression.Resolve(0, 3), Is.EqualTo(3));
    }
    [TestCase("7", 11, 0, 7)]
    [TestCase("3X", 11, 3, 9)]
    [TestCase(" 3x ", 11, 3, 9)]
    public void Resolve_ReturnsExpectedValue(
        string source,
        int available,
        int expectedX,
        int expectedValue)
    {
        Assert.That(SkillNumericExpression.TryParse(source, out SkillNumericExpression expression), Is.True);

        Assert.That(expression.ResolveMaximumX(available), Is.EqualTo(expectedX));
        Assert.That(expression.Resolve(available, expectedX), Is.EqualTo(expectedValue));
    }

    [TestCase("")]
    [TestCase("X")]
    [TestCase("0X")]
    [TestCase("-2X")]
    [TestCase("3XX")]
    public void TryParse_RejectsInvalidExpressions(string source)
    {
        Assert.That(SkillNumericExpression.TryParse(source, out _), Is.False);
    }
}
