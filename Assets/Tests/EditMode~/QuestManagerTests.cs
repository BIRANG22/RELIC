using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class QuestManagerTests
{
    [Test]
    public void CanPerformAction_BlocksLockedTutorialAction()
    {
        LobbyRuntimeData lobby = new()
        {
            ActiveQuestId = QuestManager.DefaultTutorialQuestId,
            UnlockedSystemIds = new List<string>()
        };
        QuestManager manager = new();
        manager.Initialize(lobby);

        QuestActionGateResult result = manager.CanPerformAction(
            QuestActionId.OpenCharacterSetting);

        Assert.That(result.Allowed, Is.False);
        Assert.That(result.BlockedReason, Is.Not.Empty);
    }

    [Test]
    public void MarkActionCompleted_UnlocksSystemAndCompletesActiveQuest()
    {
        LobbyRuntimeData lobby = new()
        {
            ActiveQuestId = QuestManager.DefaultTutorialQuestId,
            CompletedQuestIds = new List<string>(),
            UnlockedSystemIds = new List<string>()
        };
        QuestManager manager = new();
        manager.Initialize(lobby);

        manager.MarkActionCompleted(QuestActionId.OpenCharacterSetting);

        Assert.That(manager.CanPerformAction(QuestActionId.OpenCharacterSetting).Allowed, Is.True);
        Assert.That(lobby.UnlockedSystemIds, Does.Contain(QuestManager.CharacterSettingSystemId));
        Assert.That(lobby.CompletedQuestIds, Does.Contain(QuestManager.DefaultTutorialQuestId));
    }

    [Test]
    public void GetCurrentDisplayState_ReturnsActiveQuestText()
    {
        LobbyRuntimeData lobby = new()
        {
            ActiveQuestId = QuestManager.DefaultTutorialQuestId
        };
        QuestManager manager = new();
        manager.Initialize(lobby);

        QuestDisplayState state = manager.GetCurrentDisplayState();

        Assert.That(state.Visible, Is.True);
        Assert.That(state.QuestId, Is.EqualTo(QuestManager.DefaultTutorialQuestId));
        Assert.That(state.Text, Is.Not.Empty);
    }

    [Test]
    public void CreateSaveDataSnapshot_PreservesQuestRuntimeState()
    {
        LobbyRuntimeData lobby = new()
        {
            ActiveQuestId = QuestManager.DefaultTutorialQuestId,
            CompletedQuestIds = new List<string> { "quest.completed" },
            UnlockedSystemIds = new List<string> { QuestManager.CharacterSettingSystemId }
        };

        GameSaveData save = SaveSystem.CreateSaveDataSnapshot(
            null,
            null,
            null,
            null,
            null,
            null,
            "Lobby",
            GameMode.None,
            lobby);

        Assert.That(save.Lobby.ActiveQuestId, Is.EqualTo(QuestManager.DefaultTutorialQuestId));
        Assert.That(save.Lobby.CompletedQuestIds, Does.Contain("quest.completed"));
        Assert.That(save.Lobby.UnlockedSystemIds, Does.Contain(QuestManager.CharacterSettingSystemId));
    }
}
