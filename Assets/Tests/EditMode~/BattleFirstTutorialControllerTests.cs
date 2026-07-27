using System.IO;
using NUnit.Framework;

public class BattleFirstTutorialControllerTests
{
    private const string ControllerPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Tutorial/BattleFirstTutorialController.cs";

    private const string LoaderPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs";

    private const string BattleScenePath =
        "Assets/Project/Scenes/YDM/Battle.unity";

    private const string ControllerGuid = "69a18dd6f2b4476f91d89f5950e1f7c8";

    [Test]
    public void Controller_AdvancesTutorialStepsAndMarksSettingOffAfterLastStep()
    {
        string controller = File.ReadAllText(ControllerPath);

        Assert.That(controller, Does.Contain("TutorialSettings.ShouldShowTutorial"));
        Assert.That(controller, Does.Contain("TutorialSettings.MarkTutorialShown()"));
        Assert.That(controller, Does.Contain("tutorialSteps"));
        Assert.That(controller, Does.Contain("TryStartTutorialIfNeeded"));
        Assert.That(controller, Does.Contain("AdvanceStep"));
        Assert.That(controller, Does.Contain("Input.GetMouseButtonDown(0)"));
        Assert.That(controller, Does.Contain("Input.touchCount"));
    }

    [Test]
    public void BattleRoomLoader_StartsTutorialAfterInitialBattleInputBecomesReady()
    {
        string loader = File.ReadAllText(LoaderPath);

        Assert.That(loader, Does.Contain("BattleFirstTutorialController firstBattleTutorialController"));
        Assert.That(loader, Does.Contain("EnsureFirstBattleTutorialController"));
        Assert.That(loader, Does.Contain("firstBattleTutorialController.TryStartTutorialIfNeeded()"));
        Assert.That(
            loader.IndexOf("OpenSelectedCharacterSkillListWhenInputReady()"),
            Is.LessThan(loader.IndexOf("firstBattleTutorialController.TryStartTutorialIfNeeded()")));
    }

    [Test]
    public void BattleScene_WiresTutorialPanelAndStepsInRequestedOrder()
    {
        string scene = File.ReadAllText(BattleScenePath);

        Assert.That(scene, Does.Contain($"guid: {ControllerGuid}"));
        Assert.That(scene, Does.Contain("firstBattleTutorialController: {fileID: 1242706633}"));
        Assert.That(scene, Does.Contain("tutorialRoot: {fileID: 1812109027}"));
        Assert.That(scene, Does.Contain("- {fileID: 1715916663}"));
        Assert.That(scene, Does.Contain("- {fileID: 726934343}"));
        Assert.That(scene, Does.Contain("- {fileID: 1731089724}"));
        Assert.That(scene, Does.Contain("- {fileID: 202841091}"));
    }
}
