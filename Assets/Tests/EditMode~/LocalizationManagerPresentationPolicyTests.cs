using NUnit.Framework;

public sealed class LocalizationManagerPresentationPolicyTests
{
    [TestCase(0, 0, 0, 0)]
    [TestCase(12, 0, 0, 12)]
    [TestCase(120, 0, 0, 50)]
    [TestCase(120, 1, 50, 100)]
    [TestCase(120, 2, 100, 120)]
    [TestCase(120, 99, 100, 120)]
    public void GetPageBounds_ClampsPageAndLimitsRenderedCandidates(
        int totalCount,
        int requestedPage,
        int expectedStart,
        int expectedEnd)
    {
        LocalizationManagerPresentationPolicy.GetPageBounds(
            totalCount,
            requestedPage,
            out int start,
            out int end);

        Assert.That(start, Is.EqualTo(expectedStart));
        Assert.That(end, Is.EqualTo(expectedEnd));
    }
}
