using NUnit.Framework;
using UnityEngine;

public class TutorialSettingsTests
{
    private bool hadTutorialPreference;
    private int originalTutorialPreference;

    [SetUp]
    public void SetUp()
    {
        hadTutorialPreference = PlayerPrefs.HasKey(TutorialSettings.ShowTutorialPrefsKey);
        originalTutorialPreference = PlayerPrefs.GetInt(TutorialSettings.ShowTutorialPrefsKey, 1);
        PlayerPrefs.DeleteKey(TutorialSettings.ShowTutorialPrefsKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        if (hadTutorialPreference)
            PlayerPrefs.SetInt(TutorialSettings.ShowTutorialPrefsKey, originalTutorialPreference);
        else
            PlayerPrefs.DeleteKey(TutorialSettings.ShowTutorialPrefsKey);

        PlayerPrefs.Save();
    }

    [Test]
    public void ShouldShowTutorial_DefaultsToTrueWhenPreferenceIsMissing()
    {
        Assert.That(TutorialSettings.ShouldShowTutorial, Is.True);
    }

    [Test]
    public void SetShouldShowTutorial_PersistsSelectedState()
    {
        TutorialSettings.SetShouldShowTutorial(false);

        Assert.That(TutorialSettings.ShouldShowTutorial, Is.False);
        Assert.That(PlayerPrefs.GetInt(TutorialSettings.ShowTutorialPrefsKey), Is.EqualTo(0));

        TutorialSettings.SetShouldShowTutorial(true);

        Assert.That(TutorialSettings.ShouldShowTutorial, Is.True);
        Assert.That(PlayerPrefs.GetInt(TutorialSettings.ShowTutorialPrefsKey), Is.EqualTo(1));
    }

    [Test]
    public void MarkTutorialShown_DisablesTutorialForNextStart()
    {
        TutorialSettings.SetShouldShowTutorial(true);

        TutorialSettings.MarkTutorialShown();

        Assert.That(TutorialSettings.ShouldShowTutorial, Is.False);
    }
}
