using NUnit.Framework;
using System.IO;

public sealed class IntroRemovalRegressionTests
{
    [Test]
    public void RetiredTitleIntroCodeAndSceneObjects_AreRemoved()
    {
        Assert.That(File.Exists("Assets/Project/Scripts/IntroSequenceController.cs"), Is.False);
        Assert.That(File.Exists("Assets/Project/Scripts/Art/PressAnyKeyIntro.cs"), Is.False);

        string bootstrap = File.ReadAllText("Assets/Project/Scenes/YDM/Bootstrap.unity");
        Assert.That(bootstrap, Does.Not.Contain("m_Name: IntroSequence"));
        Assert.That(bootstrap, Does.Not.Contain("m_Name: IntroInputBlocker"));
        Assert.That(bootstrap, Does.Not.Contain("m_Name: Intro Global Volume"));

        string option = File.ReadAllText("Assets/Project/PrefabsR/Option.prefab");
        Assert.That(option, Does.Not.Contain("m_Name: IntroToggle1"));
    }

    [Test]
    public void TitleFlow_HasNoRetiredIntroDependency()
    {
        foreach (string path in new[]
        {
            "Assets/Project/Scripts/UI/Title/StartModeButton.cs",
            "Assets/Project/Scripts/UI/Title/OptionPanelUI.cs",
            "Assets/Project/Scripts/UI/Title/BackToTitleButton.cs",
            "Assets/Project/Scripts/Core/Settings.cs",
        })
        {
            string source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("IntroSequenceController"), path);
            Assert.That(source, Does.Not.Contain("IntroSettings"), path);
            Assert.That(source, Does.Not.Contain("PressAnyKeyIntro"), path);
        }
    }

    [Test]
    public void BattleRoomIntroAndBattleText_ArePreserved()
    {
        Assert.That(File.Exists("Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Intro/IBattleRoomIntroSequence.cs"), Is.True);
        Assert.That(File.Exists("Assets/Project/Scripts/UI/BattleMapIntroText.cs"), Is.True);
    }
}
