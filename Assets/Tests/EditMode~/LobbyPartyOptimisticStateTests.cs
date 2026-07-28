using System.Linq;
using NUnit.Framework;

public class LobbyPartyOptimisticStateTests
{
    [Test]
    public void Snapshot_WithViewedCharacter_ReplacesOnlyThatMemberView()
    {
        LobbyPartySnapshot source = CreateSnapshot();
        LobbyPartySnapshot updated = source.WithViewedCharacter(200UL, "D");

        Assert.That(source.ViewedCharacters.Single(v => v.MemberSteamId == 200UL).CharacterId, Is.EqualTo("C"));
        Assert.That(updated.ViewedCharacters.Single(v => v.MemberSteamId == 100UL).CharacterId, Is.EqualTo("A"));
        Assert.That(updated.ViewedCharacters.Single(v => v.MemberSteamId == 200UL).CharacterId, Is.EqualTo("D"));
        Assert.That(updated.Revision, Is.EqualTo(source.Revision));
    }

    [Test]
    public void Snapshot_WithViewedCharacter_ClearRemovesOnlyThatMemberView()
    {
        LobbyPartySnapshot source = CreateSnapshot();
        LobbyPartySnapshot updated = source.WithViewedCharacter(200UL, string.Empty);

        Assert.That(updated.ViewedCharacters.Any(v => v.MemberSteamId == 200UL), Is.False);
        Assert.That(updated.ViewedCharacters.Single(v => v.MemberSteamId == 100UL).CharacterId, Is.EqualTo("A"));
    }

    [Test]
    public void ClientCommandPipeline_AppliesPendingOptimismOverAuthoritativeSnapshot()
    {
        LobbyPartySnapshot authoritative = new LobbyPartySnapshot(
            100UL,
            7,
            new[] { 200UL },
            new[]
            {
                new LobbyPartySlotState(0, 100UL, "A"),
                new LobbyPartySlotState(1, 100UL, "B"),
                new LobbyPartySlotState(2, 200UL, string.Empty)
            },
            new[]
            {
                new LobbyPartyMemberViewState(100UL, "A")
            });
        LobbyPartyClientCommandPipeline pipeline = new LobbyPartyClientCommandPipeline();

        pipeline.TrackSentCommand(new LobbyPartyViewedCharacterCommand("view-1", 200UL, "C", 7));
        pipeline.TrackSentCommand(new LobbyPartyCharacterChangeCommand("slot-1", 200UL, 2, "C", 7));

        LobbyPartySnapshot optimistic = pipeline.ApplyPendingOptimism(authoritative);

        Assert.That(optimistic.Slots[2].CharacterId, Is.EqualTo("C"));
        Assert.That(optimistic.ViewedCharacters.Single(v => v.MemberSteamId == 200UL).CharacterId, Is.EqualTo("C"));
        Assert.That(optimistic.Revision, Is.EqualTo(authoritative.Revision));
    }

    private static LobbyPartySnapshot CreateSnapshot()
    {
        return new LobbyPartySnapshot(
            100UL,
            4,
            new[] { 200UL },
            new[]
            {
                new LobbyPartySlotState(0, 100UL, "A"),
                new LobbyPartySlotState(1, 100UL, "B"),
                new LobbyPartySlotState(2, 200UL, string.Empty)
            },
            new[]
            {
                new LobbyPartyMemberViewState(100UL, "A"),
                new LobbyPartyMemberViewState(200UL, "C")
            });
    }
}
