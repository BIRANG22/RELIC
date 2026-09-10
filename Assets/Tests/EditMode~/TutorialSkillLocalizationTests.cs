using NUnit.Framework;
using Relic.Gameplay.Data;

public class TutorialSkillLocalizationTests
{
    [Test]
    public void TutorialKeys_ExposeAllDialogueKeysUsedByTheTutorial()
    {
        Assert.That(LocalizationKeys.Tutorial.SpeakerElric, Is.EqualTo("tutorial.speaker.elric"));
        Assert.That(LocalizationKeys.Tutorial.Intro01, Is.EqualTo("tutorial.intro.01"));
        Assert.That(LocalizationKeys.Tutorial.Intro07, Is.EqualTo("tutorial.intro.07"));
        Assert.That(LocalizationKeys.Tutorial.FirstExpedition01, Is.EqualTo("tutorial.first_expedition.01"));
        Assert.That(LocalizationKeys.Tutorial.FirstExpedition03, Is.EqualTo("tutorial.first_expedition.03"));
    }

    [Test]
    public void SkillDetailsTemplate_NullSkillReturnsEmptyTemplateBeforeFormatting()
    {
        Assert.That(GameDataLocalization.SkillDetailsTemplate(null), Is.Empty);
    }
}
