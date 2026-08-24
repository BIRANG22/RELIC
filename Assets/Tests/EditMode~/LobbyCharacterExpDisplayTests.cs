using NUnit.Framework;

public sealed class LobbyCharacterExpDisplayTests
{
    [Test]
    public void CurrentLevelExpDisplay_SubtractsExperienceRequiredForCurrentLevel()
    {
        Assert.That(Setting.GetDisplayedCharacterExperienceInCurrentLevel(1, 900), Is.EqualTo(900));
        Assert.That(Setting.GetDisplayedCharacterExperienceInCurrentLevel(2, 1500), Is.EqualTo(500));
        Assert.That(Setting.GetDisplayedCharacterExperienceInCurrentLevel(3, 1500), Is.EqualTo(0));
    }
}
