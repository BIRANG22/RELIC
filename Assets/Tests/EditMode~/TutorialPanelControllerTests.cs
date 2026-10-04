using NUnit.Framework;
using Relic.Gameplay.Data;
using System.IO;

public sealed class TutorialPanelControllerTests
{
    [Test]
    public void PageKeys_AreFiveStableLocalizationKeys()
    {
        Assert.That(TutorialPanelController.PageCount, Is.EqualTo(5));
        Assert.That(TutorialPanelController.GetPageKey(0), Is.EqualTo("tutorial.battle_guide.01"));
        Assert.That(TutorialPanelController.GetPageKey(4), Is.EqualTo("tutorial.battle_guide.05"));
    }

    [TestCase(-1, 0)]
    [TestCase(0, 0)]
    [TestCase(2, 2)]
    [TestCase(4, 4)]
    [TestCase(5, 4)]
    public void ClampPageIndex_StaysInsideFivePages(int requested, int expected)
    {
        Assert.That(TutorialPanelController.ClampPageIndex(requested), Is.EqualTo(expected));
    }

    [Test]
    public void DemoTitleDemoBattle_IsEligibleForPanelSpawn()
    {
        var runtime = new BattleRuntimeData
        {
            IsDemoBattle = true,
            IsTutorialBattle = false
        };

        Assert.That(TutorialGuideEntryPolicy.ShouldSpawn(runtime), Is.True);
    }

    [Test]
    public void LobbyNormalBattle_IsNotEligibleForPanelSpawn()
    {
        var runtime = new BattleRuntimeData
        {
            IsDemoBattle = false,
            IsTutorialBattle = true
        };

        Assert.That(TutorialGuideEntryPolicy.ShouldSpawn(runtime), Is.False);
        Assert.That(TutorialGuideEntryPolicy.ShouldSpawn(null), Is.False);
    }

    [Test]
    public void BattleScene_TutorialPanelUsesAlwaysActiveRootCanvas()
    {
        string sceneYaml = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");

        StringAssert.Contains("tutorialPanelParent: {fileID: 742669610}", sceneYaml);
        StringAssert.DoesNotContain("tutorialPanelParent: {fileID: 1512511010}", sceneYaml,
            "BattleHUDCanvas는 BattleRoom 종료 처리에서 비활성화되므로 패널 부모로 사용할 수 없습니다.");
    }
}
