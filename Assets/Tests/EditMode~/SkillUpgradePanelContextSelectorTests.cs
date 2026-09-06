using NUnit.Framework;

public class SkillUpgradePanelContextSelectorTests
{
    [TestCase("Lobby", SkillUpgradePanelMode.Lobby)]
    [TestCase("Battle", SkillUpgradePanelMode.Battle)]
    [TestCase("Bootstrap", SkillUpgradePanelMode.None)]
    public void ResolveMode_UsesSceneName(string sceneName, SkillUpgradePanelMode expected)
    {
        Assert.That(SkillUpgradePanelContextSelector.ResolveMode(sceneName), Is.EqualTo(expected));
    }
}
