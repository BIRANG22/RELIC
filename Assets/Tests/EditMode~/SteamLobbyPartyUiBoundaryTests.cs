using System.IO;
using NUnit.Framework;

public class SteamLobbyPartyUiBoundaryTests
{
    private const string SteamLobbyRoot =
        "Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/";

    [Test]
    public void InviteController_DoesNotMirrorMembersBySteamArrayIndex()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyInviteController.cs");

        Assert.That(source, Does.Not.Contain("RefreshMembersIntoPartyRuntime"));
        Assert.That(source, Does.Not.Contain("\"partySlot\" + i"));
        Assert.That(source, Does.Not.Contain(
            "SetLobbyMemberData(currentLobbyId, \"characterId\""));
    }

    [Test]
    public void Synchronizer_OwnsCommandAndSnapshotSteamBoundaries()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");

        Assert.That(source, Does.Contain("RequestCharacterChange"));
        Assert.That(source, Does.Contain("CanLocalPlayerEditSlot"));
        Assert.That(source, Does.Contain("SetLobbyData"));
        Assert.That(source, Does.Contain("SendLobbyChatMsg"));
        Assert.That(source, Does.Contain("GetLobbyChatEntry"));
        Assert.That(source, Does.Contain("EnsureSteamCallbacks"));
    }

    [Test]
    public void PartySlotButton_ChecksOwnershipBeforeSelectingSlot()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/UI/Lobby/PartySlotButton.cs");

        Assert.That(source, Does.Contain("CanLocalPlayerEditSlot"));
    }

    [Test]
    public void CharacterSelectionPaths_UseSynchronizerInNetworkParty()
    {
        string charPick = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/CharPick.cs");
        string charBtn = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/CharBtn.cs");

        Assert.That(charPick, Does.Contain("RequestViewedCharacter"));
        Assert.That(charPick, Does.Contain("RequestAutomaticCharacterToggle"));
        Assert.That(charBtn, Does.Contain("RequestAutomaticCharacterToggle"));
        Assert.That(charPick, Does.Contain("IsNetworkPartyActive"));
        Assert.That(charBtn, Does.Contain("IsNetworkPartyActive"));
    }

    [Test]
    public void InviteController_DoesNotHandleCtrlVPasteInParallelWithTmpInputField()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyInviteController.cs");

        Assert.That(source, Does.Not.Contain("HandleLobbyIdPasteShortcut"));
        Assert.That(source, Does.Not.Contain("keyboard.vKey.wasPressedThisFrame"));
    }

    [Test]
    public void Synchronizer_ProvidesAutomaticOwnedSlotToggle()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");

        Assert.That(source, Does.Contain("RequestAutomaticCharacterToggle"));
        Assert.That(source, Does.Contain("RequestViewedCharacter"));
        Assert.That(source, Does.Contain("FindOwnedEmptySlot"));
    }

    [Test]
    public void NetworkCharacterViewing_UsesHostDistributedViewedCharacterState()
    {
        string charPick = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/CharPick.cs");
        string charBtn = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/CharBtn.cs");
        string synchronizer = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");

        Assert.That(charPick, Does.Contain("GetLocalViewedCharacterId"));
        Assert.That(charPick, Does.Contain("RefreshFromNetworkPartyState"));
        Assert.That(charBtn, Does.Contain("CanLocalPlayerViewCharacter"));
        Assert.That(synchronizer, Does.Contain("ViewCommandPrefix"));
        Assert.That(synchronizer, Does.Contain("TryApplyHostViewCommand"));
    }

    [Test]
    public void SelectedPartyMarker_ReadsSynchronizerDisplayStateInNetworkParty()
    {
        string charBtn = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/CharBtn.cs");
        string synchronizer = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");

        Assert.That(charBtn, Does.Contain("IsNetworkPartyActive"));
        Assert.That(charBtn, Does.Contain("FindDisplayedCharacterSlot"));
        Assert.That(synchronizer, Does.Contain(
            "charButtons[i]?.RefreshSelectedPartyMarker()"));
    }

    [Test]
    public void ClientPartyCommands_TrackEverySentRequestUntilHostSnapshotCatchesUp()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");

        Assert.That(source, Does.Contain("LobbyPartyClientCommandPipeline"));
        Assert.That(source, Does.Contain("TrackSentCommand"));
        Assert.That(source, Does.Contain("ApplyOptimisticCharacter"));
        Assert.That(source, Does.Contain("TryCompletePendingCommands"));
        Assert.That(source, Does.Contain("CommandResultPrefix"));
        Assert.That(source, Does.Not.Contain("queuedLocalIntent"));
    }
}
