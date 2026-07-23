using System.Reflection;
using NUnit.Framework;

public sealed class LobbyCultureTankPanelTests
{
    [Test]
    public void CultureTankController_ExposesPanelInteractionEntryPoint()
    {
        MethodInfo interact = typeof(LobbyCultureTankController).GetMethod(
            "Interact",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(interact, Is.Not.Null);
        Assert.That(interact.ReturnType, Is.EqualTo(typeof(void)));
        Assert.That(interact.GetParameters(), Is.Empty);
    }

    [Test]
    public void CultureTankController_WorldClickIsDisabledByDefault()
    {
        FieldInfo field = typeof(LobbyCultureTankController).GetField(
            "allowWorldInteraction",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        var controller = new UnityEngine.GameObject("CultureTank_Test")
            .AddComponent<LobbyCultureTankController>();

        try
        {
            Assert.That(field.GetValue(controller), Is.EqualTo(false));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }
    }

    [Test]
    public void ResearcherInteraction_OpensCultureTankPanel()
    {
        MethodInfo openPanel = typeof(LobbyResearcherCultureTankInteraction).GetMethod(
            "OpenPanel",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(openPanel, Is.Not.Null);
        Assert.That(openPanel.ReturnType, Is.EqualTo(typeof(void)));
        Assert.That(openPanel.GetParameters(), Is.Empty);
    }

    [Test]
    public void AutoBinder_SourceContainsResearcherBindingRule()
    {
        string sourcePath = System.IO.Path.Combine(
            UnityEngine.Application.dataPath,
            "Project",
            "Scripts",
            "Gameplay",
            "Scene",
            "Lobby",
            "LobbyCultureTankAutoBinder.cs");
        string source = System.IO.File.ReadAllText(sourcePath);

        Assert.That(source, Does.Contain("Researcher"));
        Assert.That(source, Does.Contain("LobbyResearcherCultureTankInteraction"));
    }
}
