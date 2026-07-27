using NUnit.Framework;

public class LobbyPartyAuthorityStateTests
{
    [Test]
    public void CreateHost_AssignsAllThreeSlotsToHostAndPreservesCharacters()
    {
        LobbyPartyAuthorityState state = LobbyPartyAuthorityState.CreateHost(
            100UL,
            new[] { "Character_A", "Character_B", "Character_C" });

        Assert.That(state.GetSlot(0).OwnerSteamId, Is.EqualTo(100UL));
        Assert.That(state.GetSlot(1).OwnerSteamId, Is.EqualTo(100UL));
        Assert.That(state.GetSlot(2).OwnerSteamId, Is.EqualTo(100UL));
        Assert.That(state.GetSlot(0).CharacterId, Is.EqualTo("Character_A"));
        Assert.That(state.GetSlot(1).CharacterId, Is.EqualTo("Character_B"));
        Assert.That(state.GetSlot(2).CharacterId, Is.EqualTo("Character_C"));
        Assert.That(state.Revision, Is.EqualTo(1));
    }

    [Test]
    public void CreateHost_ClearsLaterDuplicateCharacters()
    {
        LobbyPartyAuthorityState state = LobbyPartyAuthorityState.CreateHost(
            100UL,
            new[] { "A", "B", "A" });

        Assert.That(state.GetSlot(0).CharacterId, Is.EqualTo("A"));
        Assert.That(state.GetSlot(1).CharacterId, Is.EqualTo("B"));
        Assert.That(state.GetSlot(2).CharacterId, Is.Empty);
    }

    [Test]
    public void AddFirstClient_TransfersThirdSlotOwnershipWithoutMovingCharacter()
    {
        LobbyPartyAuthorityState state = CreateABC();

        state.AddClient(200UL);

        AssertSlot(state, 0, 100UL, "A");
        AssertSlot(state, 1, 100UL, "B");
        AssertSlot(state, 2, 200UL, "C");
    }

    [Test]
    public void AddSecondClient_SwapsSecondAndThirdCharactersSoFirstClientKeepsCharacter()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);

        state.AddClient(300UL);

        AssertSlot(state, 0, 100UL, "A");
        AssertSlot(state, 1, 200UL, "C");
        AssertSlot(state, 2, 300UL, "B");
    }

    [Test]
    public void RemoveFirstClientFromThree_ReturnsSecondSlotToHostWithoutMovingCharacters()
    {
        LobbyPartyAuthorityState state = CreateThreePlayers();

        state.RemoveClient(200UL);

        AssertSlot(state, 0, 100UL, "A");
        AssertSlot(state, 1, 100UL, "C");
        AssertSlot(state, 2, 300UL, "B");
    }

    [Test]
    public void RemoveSecondClientFromThree_MovesFirstClientAndCharacterToThirdSlot()
    {
        LobbyPartyAuthorityState state = CreateThreePlayers();

        state.RemoveClient(300UL);

        AssertSlot(state, 0, 100UL, "A");
        AssertSlot(state, 1, 100UL, "B");
        AssertSlot(state, 2, 200UL, "C");
    }

    [Test]
    public void RemoveOnlyClient_ReturnsThirdSlotToHostWithoutMovingCharacter()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);

        state.RemoveClient(200UL);

        AssertSlot(state, 0, 100UL, "A");
        AssertSlot(state, 1, 100UL, "B");
        AssertSlot(state, 2, 100UL, "C");
    }

    [Test]
    public void RepeatingSameMembershipChange_DoesNotChangeRevisionOrSwapAgain()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);
        long revision = state.Revision;

        LobbyPartyMembershipResult result = state.AddClient(200UL);

        Assert.That(result.Changed, Is.False);
        Assert.That(state.Revision, Is.EqualTo(revision));
        AssertSlot(state, 2, 200UL, "C");
    }

    [Test]
    public void ChangeCharacter_BySlotOwner_UpdatesCharacterAndRevision()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);
        long revision = state.Revision;

        LobbyPartyCommandResult result = state.TryChangeCharacter(
            Command(200UL, 2, "D", revision),
            _ => true);

        Assert.That(result.Accepted, Is.True);
        Assert.That(result.RejectReason, Is.EqualTo(LobbyPartyCommandRejectReason.None));
        Assert.That(state.GetSlot(2).CharacterId, Is.EqualTo("D"));
        Assert.That(state.Revision, Is.EqualTo(revision + 1));
    }

    [Test]
    public void ChangeCharacter_ByDifferentMember_IsRejected()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);
        long revision = state.Revision;

        LobbyPartyCommandResult result = state.TryChangeCharacter(
            Command(200UL, 1, "D", revision),
            _ => true);

        AssertRejectedWithoutMutation(
            state,
            result,
            LobbyPartyCommandRejectReason.NotSlotOwner,
            revision,
            1,
            "B");
    }

    [Test]
    public void ChangeCharacter_ToCharacterUsedByOtherSlot_IsRejected()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);
        long revision = state.Revision;

        LobbyPartyCommandResult result = state.TryChangeCharacter(
            Command(200UL, 2, "A", revision),
            _ => true);

        AssertRejectedWithoutMutation(
            state,
            result,
            LobbyPartyCommandRejectReason.DuplicateCharacter,
            revision,
            2,
            "C");
    }

    [Test]
    public void ChangeCharacter_WithStaleRevision_IsRejected()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);
        long revision = state.Revision;

        LobbyPartyCommandResult result = state.TryChangeCharacter(
            Command(200UL, 2, "D", revision - 1),
            _ => true);

        AssertRejectedWithoutMutation(
            state,
            result,
            LobbyPartyCommandRejectReason.StaleRevision,
            revision,
            2,
            "C");
    }

    [Test]
    public void ChangeCharacter_WithUnknownCharacter_IsRejected()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);
        long revision = state.Revision;

        LobbyPartyCommandResult result = state.TryChangeCharacter(
            Command(200UL, 2, "Missing", revision),
            _ => false);

        AssertRejectedWithoutMutation(
            state,
            result,
            LobbyPartyCommandRejectReason.InvalidCharacter,
            revision,
            2,
            "C");
    }

    private static LobbyPartyAuthorityState CreateABC()
    {
        return LobbyPartyAuthorityState.CreateHost(100UL, new[] { "A", "B", "C" });
    }

    private static LobbyPartyAuthorityState CreateThreePlayers()
    {
        LobbyPartyAuthorityState state = CreateABC();
        state.AddClient(200UL);
        state.AddClient(300UL);
        return state;
    }

    private static LobbyPartyCharacterChangeCommand Command(
        ulong requester,
        int slotIndex,
        string characterId,
        long revision)
    {
        return new LobbyPartyCharacterChangeCommand(
            "request-1",
            requester,
            slotIndex,
            characterId,
            revision);
    }

    private static void AssertSlot(
        LobbyPartyAuthorityState state,
        int slotIndex,
        ulong ownerSteamId,
        string characterId)
    {
        LobbyPartySlotState slot = state.GetSlot(slotIndex);
        Assert.That(slot.OwnerSteamId, Is.EqualTo(ownerSteamId));
        Assert.That(slot.CharacterId, Is.EqualTo(characterId));
    }

    private static void AssertRejectedWithoutMutation(
        LobbyPartyAuthorityState state,
        LobbyPartyCommandResult result,
        LobbyPartyCommandRejectReason reason,
        long revision,
        int slotIndex,
        string characterId)
    {
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectReason, Is.EqualTo(reason));
        Assert.That(state.Revision, Is.EqualTo(revision));
        Assert.That(state.GetSlot(slotIndex).CharacterId, Is.EqualTo(characterId));
    }
}
