using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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
    public void CultureTankController_PanelLabelUsesReadableKoreanStateText()
    {
        DataManager createdDataManager = null;
        GameObject dataObject = null;
        if (DataManager.Instance == null)
        {
            dataObject = new GameObject("DataManager_Test");
            createdDataManager = dataObject.AddComponent<DataManager>();
        }

        GameObject tankObject = new GameObject("CultureTank_LabelTest");
        LobbyCultureTankController controller =
            tankObject.AddComponent<LobbyCultureTankController>();

        try
        {
            string label = controller.GetPanelLabel();

            Assert.That(label, Does.Contain("배양조"));
            Assert.That(label, Does.Contain("비어 있음"));
            Assert.That(label, Does.Not.Contain("鍮"));
            Assert.That(label, Does.Not.Contain("諛"));
        }
        finally
        {
            Object.DestroyImmediate(tankObject);

            if (createdDataManager != null)
                Object.DestroyImmediate(createdDataManager.gameObject);
            else if (dataObject != null)
                Object.DestroyImmediate(dataObject);
        }
    }

    [Test]
    public void ResearcherInteraction_UsesMouseReleaseInsteadOfMousePress()
    {
        string sourcePath = System.IO.Path.Combine(
            UnityEngine.Application.dataPath,
            "Project",
            "Scripts",
            "Gameplay",
            "Scene",
            "Lobby",
            "LobbyResearcherCultureTankInteraction.cs");
        string source = System.IO.File.ReadAllText(sourcePath);

        Assert.That(source, Does.Contain("private void OnMouseUpAsButton()"));
        Assert.That(source, Does.Not.Contain("private void OnMouseDown()"));
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
