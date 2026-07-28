using System.IO;
using NUnit.Framework;

public class SteamLobbySharedStateUiBoundaryTests
{
    private const string SteamLobbyRoot =
        "Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/";

    [Test]
    public void InviteController_BindsSharedStateSynchronizerToSteamLobby()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyInviteController.cs");

        Assert.That(source, Does.Contain("SteamLobbySharedStateSynchronizer"));
        Assert.That(source, Does.Contain("sharedStateSynchronizer.EnterLobby"));
        Assert.That(source, Does.Contain("sharedStateSynchronizer.HandleLobbyDataChanged"));
        Assert.That(source, Does.Contain("sharedStateSynchronizer.HandleLobbyMembershipChanged"));
    }

    [Test]
    public void SharedSynchronizer_PublishesLobbyRuntimeSnapshotsImmediately()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbySharedStateSynchronizer.cs");

        Assert.That(source, Does.Contain("SharedSnapshotBroadcastPrefix"));
        Assert.That(source, Does.Contain("BroadcastSnapshot(snapshot)"));
        Assert.That(source, Does.Contain("PartyStateApplied"));
        Assert.That(source, Does.Contain("SetLobbyData"));
        Assert.That(source, Does.Contain("SendLobbyChatMsg"));
        Assert.That(source, Does.Contain("ApplySnapshotToRuntime"));
    }

    [Test]
    public void ClientHostOnlyLobbyActions_AreBlockedAtMutationEntryPoints()
    {
        string relicShop = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs");
        string erosion = File.ReadAllText(
            "Assets/Project/Scripts/ErosionSelectCarousel.cs");
        string cultureTank = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankController.cs");
        string battlePlay = File.ReadAllText(
            "Assets/Project/Scripts/UI/Lobby/BattlePlayButton.cs");

        Assert.That(relicShop, Does.Contain("CanLocalPlayerMutateHostOnlyState"));
        Assert.That(erosion, Does.Contain("CanLocalPlayerMutateHostOnlyState"));
        Assert.That(cultureTank, Does.Contain("CanLocalPlayerMutateHostOnlyState"));
        Assert.That(battlePlay, Does.Contain("CanLocalPlayerMutateHostOnlyState"));
    }

    [Test]
    public void LobbyEquipmentPanels_RouteNetworkChangesThroughSharedSynchronizer()
    {
        string relicPanel = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/RelicEquipPanelUI.cs");
        string skillPanel = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/SkillInventoryPanelUI.cs");

        Assert.That(relicPanel, Does.Contain("RequestEquipRelic"));
        Assert.That(relicPanel, Does.Contain("RequestUnequipRelic"));
        Assert.That(skillPanel, Does.Contain("RequestEquipSkill"));
        Assert.That(skillPanel, Does.Contain("RequestUnequipSkill"));
    }
}
