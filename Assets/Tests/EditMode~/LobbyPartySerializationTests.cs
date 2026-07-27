using NUnit.Framework;

public class LobbyPartySerializationTests
{
    [Test]
    public void Command_RoundTripsAllFields()
    {
        LobbyPartyCharacterChangeCommand source = new(
            "request-42",
            76561198000000001UL,
            2,
            "Character_C",
            7);

        string payload = LobbyPartySerialization.SerializeCommand(source);
        bool success = LobbyPartySerialization.TryDeserializeCommand(payload, out var restored);

        Assert.That(success, Is.True);
        Assert.That(restored.RequestId, Is.EqualTo(source.RequestId));
        Assert.That(restored.RequesterSteamId, Is.EqualTo(source.RequesterSteamId));
        Assert.That(restored.SlotIndex, Is.EqualTo(source.SlotIndex));
        Assert.That(restored.RequestedCharacterId, Is.EqualTo(source.RequestedCharacterId));
        Assert.That(restored.KnownRevision, Is.EqualTo(source.KnownRevision));
    }

    [Test]
    public void Snapshot_RoundTripsAllPartyFields()
    {
        LobbyPartySnapshot source = CreateSnapshot();

        string payload = LobbyPartySerialization.SerializeSnapshot(source);
        bool success = LobbyPartySerialization.TryDeserializeSnapshot(payload, out var restored);

        Assert.That(success, Is.True);
        Assert.That(restored.HostSteamId, Is.EqualTo(source.HostSteamId));
        Assert.That(restored.Revision, Is.EqualTo(source.Revision));
        Assert.That(restored.OrderedClientSteamIds, Is.EqualTo(source.OrderedClientSteamIds));
        Assert.That(restored.Slots[1].OwnerSteamId, Is.EqualTo(200UL));
        Assert.That(restored.Slots[1].CharacterId, Is.EqualTo("C"));
    }

    [TestCase("")]
    [TestCase("{broken")]
    [TestCase("{}")]
    public void Deserialize_RejectsMalformedPayload(string payload)
    {
        Assert.That(
            LobbyPartySerialization.TryDeserializeSnapshot(payload, out _),
            Is.False);
    }

    [Test]
    public void ValidateSnapshot_RejectsDuplicateCharacters()
    {
        LobbyPartySnapshot snapshot = new(
            100UL,
            4,
            new[] { 200UL },
            new[]
            {
                new LobbyPartySlotState(0, 100UL, "A"),
                new LobbyPartySlotState(1, 100UL, "A"),
                new LobbyPartySlotState(2, 200UL, "C")
            });

        Assert.That(
            LobbyPartySerialization.ValidateSnapshot(
                snapshot,
                100UL,
                new[] { 100UL, 200UL }),
            Is.False);
    }

    [Test]
    public void ValidateSnapshot_RejectsUnknownOwnerOrWrongHost()
    {
        LobbyPartySnapshot snapshot = CreateSnapshot();

        Assert.That(
            LobbyPartySerialization.ValidateSnapshot(
                snapshot,
                999UL,
                new[] { 100UL, 200UL }),
            Is.False);

        Assert.That(
            LobbyPartySerialization.ValidateSnapshot(
                snapshot,
                100UL,
                new[] { 100UL }),
            Is.False);
    }

    [Test]
    public void CommandResponse_RoundTripsAcceptanceAndRevision()
    {
        LobbyPartyCommandResponse source = new(
            "request-42",
            200UL,
            true,
            LobbyPartyCommandRejectReason.None,
            8);

        string payload = LobbyPartySerialization.SerializeCommandResponse(source);
        bool success = LobbyPartySerialization.TryDeserializeCommandResponse(
            payload,
            out var restored);

        Assert.That(success, Is.True);
        Assert.That(restored.RequestId, Is.EqualTo(source.RequestId));
        Assert.That(restored.RequesterSteamId, Is.EqualTo(source.RequesterSteamId));
        Assert.That(restored.Accepted, Is.True);
        Assert.That(restored.ResultRevision, Is.EqualTo(8));
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
                new LobbyPartySlotState(1, 200UL, "C"),
                new LobbyPartySlotState(2, 100UL, "B")
            });
    }
}
