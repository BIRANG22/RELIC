using System.IO;
using NUnit.Framework;

public class SharedBattleSceneIntegrationTests
{
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void BattleScene_UsesSharedPresentationWithoutLegacyMapRoom()
    {
        string sceneText = File.ReadAllText(BattleScenePath);

        Assert.That(sceneText, Does.Contain("m_Name: SharedRoomRoot"));
        Assert.That(sceneText, Does.Contain("sharedRoomPresentationController:"));
        Assert.That(sceneText, Does.Not.Contain("m_Name: MapRoom\n"));
        Assert.That(sceneText, Does.Not.Contain("guid: 0375ee787de9af44f9042027ad2fcec2"));
    }

    [Test]
    public void BattleScene_SharedRewardCanvasHasVisibleScale()
    {
        string sceneText = File.ReadAllText(BattleScenePath);
        int rewardCanvasIndex = sceneText.IndexOf("m_Name: BattleRewardCanvas");

        Assert.That(rewardCanvasIndex, Is.GreaterThanOrEqualTo(0));
        string rewardCanvasSection = sceneText.Substring(rewardCanvasIndex, 1200);
        Assert.That(rewardCanvasSection, Does.Contain("m_LocalScale: {x: 1, y: 1, z: 1}"));
        Assert.That(rewardCanvasSection, Does.Contain("m_Father: {fileID: 742669610}"));
    }
}
