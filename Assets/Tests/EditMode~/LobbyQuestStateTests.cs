using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class LobbyQuestStateTests
{
    [Test]
    public void Build_WaitingForSetup_ShowsSetupQuestAndAllowsSetupOnly()
    {
        var lobby = new LobbyRuntimeData
        {
            TutorialProgress = LobbyTutorialProgress.WaitingForSetup
        };

        var config = new LobbyQuestTextConfig
        {
            SetupQuestText = "세팅을 완료하세요.",
            FirstExpeditionQuestText = "아라벨라 처치 {Current}/{Target}",
            FirstExpeditionTargetCount = 1,
            FirstExpeditionRequiredItemId = "Arabella_Core"
        };

        LobbyQuestState state = LobbyQuestState.Build(lobby, config);

        Assert.That(state.IsVisible, Is.True);
        Assert.That(state.Text, Is.EqualTo("세팅을 완료하세요."));
        Assert.That(LobbyQuestState.CanUseFeature(state.Progress, LobbyTutorialProgress.WaitingForSetup), Is.True);
        Assert.That(LobbyQuestState.CanUseFeature(state.Progress, LobbyTutorialProgress.FirstExpeditionAssigned), Is.False);
    }

    [Test]
    public void Build_FirstExpeditionAssigned_FormatsCurrentAndTarget()
    {
        var lobby = new LobbyRuntimeData
        {
            TutorialProgress = LobbyTutorialProgress.FirstExpeditionAssigned
        };
        lobby.BagItemIds.Add("Arabella_Core");

        var config = new LobbyQuestTextConfig
        {
            SetupQuestText = "세팅을 완료하세요.",
            FirstExpeditionQuestText = "아라벨라 처치 {Current}/{Target}",
            FirstExpeditionTargetCount = 1,
            FirstExpeditionRequiredItemId = "Arabella_Core"
        };

        LobbyQuestState state = LobbyQuestState.Build(lobby, config);

        Assert.That(state.IsVisible, Is.True);
        Assert.That(state.Text, Is.EqualTo("아라벨라 처치 1/1"));
        Assert.That(LobbyQuestState.CanUseFeature(state.Progress, LobbyTutorialProgress.FirstExpeditionAssigned), Is.True);
    }

    [Test]
    public void Build_Completed_HidesQuest()
    {
        var lobby = new LobbyRuntimeData
        {
            TutorialProgress = LobbyTutorialProgress.Completed
        };

        LobbyQuestState state = LobbyQuestState.Build(lobby, LobbyQuestTextConfig.Default);

        Assert.That(state.IsVisible, Is.False);
        Assert.That(state.Text, Is.Empty);
        Assert.That(LobbyQuestState.CanUseFeature(state.Progress, LobbyTutorialProgress.Completed), Is.True);
    }
}
