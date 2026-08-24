using System.IO;
using NUnit.Framework;

public sealed class LobbyQuestSourceTests
{
    [Test]
    public void Bootstrap_DoesNotCreateLobbyQuestManagerAtRuntime()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Core/Bootstrap.cs");

        StringAssert.Contains("SaveSystem.Instance.TryLoadProgress();", source);
        Assert.That(source, Does.Not.Contain("LobbyQuestManager.EnsureInstance();"));
    }

    [Test]
    public void OnlyBattlePlayButton_QueriesLobbyQuestGate()
    {
        AssertSourceContains("Assets/Project/Scripts/UI/Lobby/BattlePlayButton.cs", "LobbyQuestGate");
        AssertSourceDoesNotContain("Assets/Project/Scripts/UI/LobbyPanelTransitionButton.cs", "LobbyQuestGate");
        AssertSourceDoesNotContain("Assets/Project/Scripts/Gameplay/Scene/Lobby/PositionStageSelectController.cs", "LobbyQuestGate");
    }

    [Test]
    public void LobbyTutorialController_NoLongerOwnsQuestPanel()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/LobbyTutorialController.cs");

        Assert.That(source, Does.Not.Contain("questPanel"));
        Assert.That(source, Does.Not.Contain("questText"));
        Assert.That(source, Does.Not.Contain("RefreshQuestPanel"));
        StringAssert.Contains("LobbyQuestManager.Instance?.Refresh();", source);
    }

    [Test]
    public void LegacyLobbyQuestPanelArtifacts_AreRemoved()
    {
        string lobbyScene = File.ReadAllText("Assets/Project/Scenes/YDM/Lobby.unity");
        string blurBackground = File.ReadAllText("Assets/Project/Scripts/UIBlurBackground.cs");
        string questManager = File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Lobby/Quest/LobbyQuestManager.cs");

        Assert.That(lobbyScene, Does.Not.Contain("m_Name: QuestPanel"));
        Assert.That(lobbyScene, Does.Not.Contain("m_Name: QuestText"));
        Assert.That(lobbyScene, Does.Not.Contain("606622675"));
        Assert.That(blurBackground, Does.Not.Contain("AppendLobbyQuestPanelRoot"));
        Assert.That(questManager, Does.Not.Contain("LegacyQuestPanel"));
        Assert.That(questManager, Does.Not.Contain("new GameObject"));
        Assert.That(questManager, Does.Not.Contain("EnsurePanel"));
        Assert.That(questManager, Does.Not.Contain("EnsureInstance"));
        Assert.That(File.Exists("Assets/Project/PrefabsR/QuestPanel.prefab"), Is.False);
        Assert.That(File.Exists("Assets/Project/PrefabsR/QuestPanel.prefab.meta"), Is.False);
    }

    [Test]
    public void BootstrapScene_ContainsPlacedLobbyQuestObjects()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Bootstrap.unity");

        StringAssert.Contains("m_Name: LobbyQuestManager", scene);
        StringAssert.Contains("m_Name: LobbyQuestCanvas", scene);
        StringAssert.Contains("m_Name: LobbyQuestPanel", scene);
        StringAssert.Contains("m_Name: LobbyQuestText", scene);
    }

    [Test]
    public void LobbyQuestPanel_IsHiddenOutsideDefaultLobbyState()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Lobby/Quest/LobbyQuestManager.cs");
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Bootstrap.unity");

        StringAssert.Contains("hideWhenAnyActiveObjectNames", source);
        StringAssert.Contains("LateUpdate()", source);
        StringAssert.Contains("state.IsVisible && IsDefaultLobbyStateVisible()", source);
        StringAssert.Contains("DialoguePanel", scene);
        StringAssert.Contains("CharacterSettingPanel", scene);
        StringAssert.Contains("StageSelectPanel", scene);
        StringAssert.Contains("StoragePanel", scene);
    }

    private static void AssertSourceContains(string path, string expected)
    {
        string source = File.ReadAllText(path);
        StringAssert.Contains(expected, source, path);
    }

    private static void AssertSourceDoesNotContain(string path, string unexpected)
    {
        string source = File.ReadAllText(path);
        Assert.That(source, Does.Not.Contain(unexpected), path);
    }
}
