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
        Assert.That(charBtn, Does.Contain("SetNetworkViewedCharacterState"));
        Assert.That(charBtn, Does.Contain("remoteViewedCharacterAlpha"));
        Assert.That(synchronizer, Does.Contain("ViewCommandPrefix"));
        Assert.That(synchronizer, Does.Contain("TryApplyHostViewCommand"));
        Assert.That(synchronizer, Does.Contain("IsCharacterViewedByRemoteMember"));
    }

    [Test]
    public void NetworkCharacterViewing_RefreshesOnlyActiveCharacterPickerPanels()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs").Replace("\r\n", "\n");

        Assert.That(source, Does.Contain(
            "FindObjectsByType<CharPick>(\n            FindObjectsInactive.Exclude"));
    }

    [Test]
    public void NetworkCharacterViewing_AppliesAuthoritativeSnapshotsImmediately()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs").Replace("\r\n", "\n");

        Assert.That(source, Does.Contain("CurrentSnapshot = snapshot;"));
        Assert.That(source, Does.Not.Contain(
            "!clientCommandPipeline.HasPendingCommands)\n            CurrentSnapshot = snapshot"));
    }

    [Test]
    public void NetworkCharacterSelection_DoesNotBlockClearRequestsWithViewLocks()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");

        Assert.That(source, Does.Contain("bool isClearRequest = string.IsNullOrWhiteSpace(characterId);"));
        Assert.That(source, Does.Contain("!isClearRequest &&"));
        Assert.That(source, Does.Contain("IsCharacterViewedByOtherMember(characterId)"));
    }

    [Test]
    public void NetworkCharacterViewing_CanClearLocalViewedCharacterWhenLeavingSetting()
    {
        string synchronizer = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");
        string charPick = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/CharPick.cs");

        Assert.That(synchronizer, Does.Contain("RequestClearViewedCharacter"));
        Assert.That(charPick, Does.Contain("ClearNetworkViewedCharacterOnDisable"));
        Assert.That(charPick, Does.Contain("RequestClearViewedCharacter"));
    }

    [Test]
    public void ClientPartyCommands_ApplySnapshotCarriedByHostResponse()
    {
        string models = File.ReadAllText(
            SteamLobbyRoot + "LobbyPartyModels.cs");
        string serialization = File.ReadAllText(
            SteamLobbyRoot + "LobbyPartySerialization.cs");
        string synchronizer = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs").Replace("\r\n", "\n");

        Assert.That(models, Does.Contain("public LobbyPartySnapshot Snapshot"));
        Assert.That(serialization, Does.Contain("snapshot = ToDto(response.Snapshot)"));
        Assert.That(synchronizer, Does.Contain("ApplyResponseSnapshot(response)"));
        Assert.That(synchronizer, Does.Contain("bool isLocalRequester = response.RequesterSteamId == localSteamId"));
        Assert.That(synchronizer, Does.Not.Contain("IsLocalHost() || !clientCommandPipeline.HasPendingCommands"));
    }

    [Test]
    public void NetworkCharacterViewing_RefreshesVisibleButtonsFromSnapshot()
    {
        string source = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyPartySynchronizer.cs");

        Assert.That(source, Does.Contain("RefreshNetworkViewedCharacterState"));
        Assert.That(source, Does.Contain("RefreshFromNetworkPartyState"));
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

    [Test]
    public void SteamCallbacks_RunFromRuntimePumpInsteadOfInviteButtonLifetime()
    {
        string pumpPath = SteamLobbyRoot + "SteamworksCallbackPump.cs";
        Assert.That(File.Exists(pumpPath), Is.True);

        string inviteController = File.ReadAllText(
            SteamLobbyRoot + "SteamLobbyInviteController.cs");
        string pump = File.ReadAllText(pumpPath);

        Assert.That(inviteController, Does.Contain("internal static bool IsSteamApiReady"));
        Assert.That(inviteController, Does.Contain("internal static void RunSteamCallbacksIfReady"));
        Assert.That(inviteController, Does.Not.Contain("private void Update()"));
        Assert.That(pump, Does.Contain("RuntimeInitializeOnLoadMethod"));
        Assert.That(pump, Does.Contain("DontDestroyOnLoad"));
        Assert.That(pump, Does.Contain("SteamLobbyInviteController.RunSteamCallbacksIfReady"));
    }
}
