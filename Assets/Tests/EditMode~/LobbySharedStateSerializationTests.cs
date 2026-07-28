using NUnit.Framework;
using Relic.Gameplay.Data;

public class LobbySharedStateSerializationTests
{
    [Test]
    public void Snapshot_RoundTripsHostOwnedLobbyRuntime()
    {
        LobbyRuntimeData lobby = CreateLobbyRuntime();
        LobbySharedStateSnapshot source = LobbySharedStateSnapshot.FromRuntime(
            100UL,
            7,
            5,
            lobby);

        string payload = LobbySharedStateSerialization.SerializeSnapshot(source);
        bool success = LobbySharedStateSerialization.TryDeserializeSnapshot(
            payload,
            out LobbySharedStateSnapshot restored);

        Assert.That(success, Is.True);
        Assert.That(restored.HostSteamId, Is.EqualTo(100UL));
        Assert.That(restored.Revision, Is.EqualTo(7));
        Assert.That(restored.TrialSelectionMask, Is.EqualTo(5));
        Assert.That(restored.Lobby.BlueDustium, Is.EqualTo(321));
        Assert.That(restored.Lobby.OwnedRelicIds, Is.EqualTo(new[] { "Relic_A" }));
        Assert.That(restored.Lobby.SkillInventoryIds, Is.EqualTo(new[] { "Skill_A" }));
        Assert.That(restored.Lobby.BagItemIds, Is.EqualTo(new[] { "Item_A" }));
        Assert.That(restored.Lobby.RelicOfferIds, Is.EqualTo(new[] { "Relic_Offer" }));
        Assert.That(restored.Lobby.CultureTankResearches[0].TankId, Is.EqualTo("CultureTank1"));
        Assert.That(restored.Lobby.PendingCultureTankBattleStartEffects[0].EffectId, Is.EqualTo("Effect_A"));
        Assert.That(restored.Lobby.CharacterLoadouts[0].EquippedRelicIds[1], Is.EqualTo("Relic_A"));
        Assert.That(restored.Lobby.CharacterLoadouts[0].EquippedSkillIds[2], Is.EqualTo("Skill_A"));
    }

    [Test]
    public void Snapshot_FromRuntimeDeepCopiesMutableLists()
    {
        LobbyRuntimeData lobby = CreateLobbyRuntime();
        LobbySharedStateSnapshot snapshot = LobbySharedStateSnapshot.FromRuntime(
            100UL,
            7,
            5,
            lobby);

        lobby.BlueDustium = 1;
        lobby.OwnedRelicIds[0] = "Changed";
        lobby.CharacterLoadouts[0].EquippedRelicIds[1] = "Changed";

        Assert.That(snapshot.Lobby.BlueDustium, Is.EqualTo(321));
        Assert.That(snapshot.Lobby.OwnedRelicIds, Is.EqualTo(new[] { "Relic_A" }));
        Assert.That(snapshot.Lobby.CharacterLoadouts[0].EquippedRelicIds[1], Is.EqualTo("Relic_A"));
    }

    [Test]
    public void EquipmentCommand_RoundTripsAllFields()
    {
        LobbySharedStateCommand source = new(
            "request-1",
            200UL,
            LobbySharedStateCommandType.EquipRelic,
            "Character_A",
            1,
            "Relic_A",
            7);

        string payload = LobbySharedStateSerialization.SerializeCommand(source);
        bool success = LobbySharedStateSerialization.TryDeserializeCommand(
            payload,
            out LobbySharedStateCommand restored);

        Assert.That(success, Is.True);
        Assert.That(restored.RequestId, Is.EqualTo(source.RequestId));
        Assert.That(restored.RequesterSteamId, Is.EqualTo(200UL));
        Assert.That(restored.CommandType, Is.EqualTo(LobbySharedStateCommandType.EquipRelic));
        Assert.That(restored.CharacterId, Is.EqualTo("Character_A"));
        Assert.That(restored.SlotIndex, Is.EqualTo(1));
        Assert.That(restored.ItemId, Is.EqualTo("Relic_A"));
        Assert.That(restored.KnownRevision, Is.EqualTo(7));
    }

    [TestCase("")]
    [TestCase("{broken")]
    [TestCase("{}")]
    public void Deserialize_RejectsMalformedSnapshotPayload(string payload)
    {
        Assert.That(
            LobbySharedStateSerialization.TryDeserializeSnapshot(payload, out _),
            Is.False);
    }

    private static LobbyRuntimeData CreateLobbyRuntime()
    {
        LobbyRuntimeData lobby = new()
        {
            BlueDustium = 321,
            RelicOfferSeed = 11,
            RelicRefreshCount = 2
        };

        lobby.OwnedRelicIds.Add("Relic_A");
        lobby.SkillInventoryIds.Add("Skill_A");
        lobby.BagItemIds.Add("Item_A");
        lobby.RelicOfferIds.Add("Relic_Offer");
        lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData
        {
            TankId = "CultureTank1",
            ItemId = "Item_A",
            StartedAtUtcTicks = 1234,
            DurationSeconds = 30,
            IsCompleted = true
        });
        lobby.PendingCultureTankBattleStartEffects.Add(new CultureTankBattleStartEffectRuntimeData
        {
            SourceItemId = "Item_A",
            EffectId = "Effect_A",
            Value = 3,
            Count = 1,
            RemainingBattleStarts = 2
        });
        lobby.CharacterLoadouts.Add(new LobbyCharacterLoadoutData
        {
            CharacterId = "Character_A",
            EquippedRelicIds = new[] { "", "Relic_A", "", "", "" },
            EquippedSkillIds = new[] { "", "", "Skill_A", "" }
        });

        return lobby;
    }
}
