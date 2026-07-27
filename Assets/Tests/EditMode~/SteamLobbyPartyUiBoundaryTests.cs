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
        Assert.That(source, Does.Contain("FindOwnedEmptySlot"));
    }
}
