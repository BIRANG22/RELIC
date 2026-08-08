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
        Assert.That(restored.Lobby.CompletedCultureTankCombinationId, Is.EqualTo("Culture_ABC"));
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

    [Test]
    public void ApplyLobbyLoadouts_CreatesMissingPartyCharacterRuntimeWithDefaultSkills()
    {
        CharacterRuntimeStore characterStore = new();
        CharacterDatabase characterDatabase = CreateCharacterDatabase();
        PartyRuntimeStore partyStore = new();
        Assert.That(partyStore.SetCharacter(0, "Character_A"), Is.True);

        LobbySharedStateCharacterRuntimeUtility.ApplyLobbyLoadouts(
            new LobbyRuntimeData(),
            partyStore,
            characterStore,
            characterDatabase,
            null);

        Assert.That(characterStore.TryGet("Character_A", out CharacterRuntimeData runtime), Is.True);
        Assert.That(runtime.CharacterId, Is.EqualTo("Character_A"));
        Assert.That(runtime.CurrentHP, Is.EqualTo(120));
        Assert.That(runtime.CurrentCost, Is.EqualTo(6));
        Assert.That(runtime.PassiveSkillId, Is.EqualTo("Passive_A"));
        Assert.That(runtime.UniqueSkillId, Is.EqualTo("Unique_A"));
        Assert.That(runtime.AbilitySkillId, Is.EqualTo("Ability_A"));
        Assert.That(runtime.EquippedSkillIds, Is.EqualTo(new[] { "Unique_A", "Ability_A", "Common_A", "" }));
        Assert.That(runtime.EquippedRelicIds, Has.Length.EqualTo(5));
    }

    [Test]
    public void ApplyLobbyLoadouts_AppliesSharedEquipmentToCreatedRuntime()
    {
        CharacterRuntimeStore characterStore = new();
        CharacterDatabase characterDatabase = CreateCharacterDatabase();
        PartyRuntimeStore partyStore = new();
        Assert.That(partyStore.SetCharacter(0, "Character_A"), Is.True);
        LobbyRuntimeData lobby = new();
        lobby.CharacterLoadouts.Add(new LobbyCharacterLoadoutData
        {
            CharacterId = "Character_A",
            EquippedRelicIds = new[] { "", "Relic_A", "", "", "" },
            EquippedSkillIds = new[] { "Unique_A", "Ability_A", "Skill_X", "" }
        });

        LobbySharedStateCharacterRuntimeUtility.ApplyLobbyLoadouts(
            lobby,
            partyStore,
            characterStore,
            characterDatabase,
            null);

        Assert.That(characterStore.TryGet("Character_A", out CharacterRuntimeData runtime), Is.True);
        Assert.That(runtime.EquippedRelicIds, Is.EqualTo(new[] { "", "Relic_A", "", "", "" }));
        Assert.That(runtime.EquippedSkillIds, Is.EqualTo(new[] { "Unique_A", "Ability_A", "Skill_X", "" }));
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
            RelicRefreshCount = 2,
            CultureTankCombinationSchemaVersion = CultureTankResearchService.CurrentSchemaVersion,
            CompletedCultureTankCombinationId = "Culture_ABC"
        };

        lobby.OwnedRelicIds.Add("Relic_A");
        lobby.SkillInventoryIds.Add("Skill_A");
        lobby.BagItemIds.Add("Item_A");
        lobby.RelicOfferIds.Add("Relic_Offer");
        lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData
        {
            TankId = "CultureTank1",
            ItemId = "Item_A"
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

    private static CharacterDatabase CreateCharacterDatabase()
    {
        CharacterDatabase database = new();
        database.Initialize(new[]
        {
            new CharacterMasterData
            {
                CharacterId = "Character_A",
                MaxHP = 120,
                MaxCost = 6,
                PassiveSkill1 = "Passive_A",
                UniqueSkill1 = "Unique_A",
                CharacterSkill1 = "Ability_A",
                CommonSkill1 = "Common_A",
                IsDefaultProvided = true
            }
        });
        return database;
    }
}
