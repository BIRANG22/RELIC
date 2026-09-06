using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class LobbyBattleStartSerializationTests
{
    [Test]
    public void Command_RoundTripsBattleStartFields()
    {
        LobbyBattleStartCommand source = new(
            "battle-request-1",
            100UL,
            12,
            "session-abc",
            123456,
            "Chapter_A",
            "Stage_3");

        string payload = LobbyBattleStartSerialization.SerializeCommand(source);
        bool success = LobbyBattleStartSerialization.TryDeserializeCommand(
            payload,
            out LobbyBattleStartCommand restored);

        Assert.That(success, Is.True);
        Assert.That(restored.RequestId, Is.EqualTo("battle-request-1"));
        Assert.That(restored.HostSteamId, Is.EqualTo(100UL));
        Assert.That(restored.RequiredSharedStateRevision, Is.EqualTo(12));
        Assert.That(restored.BattleSessionId, Is.EqualTo("session-abc"));
        Assert.That(restored.BattleSeed, Is.EqualTo(123456));
        Assert.That(restored.ChapterId, Is.EqualTo("Chapter_A"));
        Assert.That(restored.StageId, Is.EqualTo("Stage_3"));
    }

    [TestCase("")]
    [TestCase("{broken")]
    [TestCase("{}")]
    public void Deserialize_RejectsMalformedCommandPayload(string payload)
    {
        Assert.That(
            LobbyBattleStartSerialization.TryDeserializeCommand(payload, out _),
            Is.False);
    }

    [Test]
    public void ApplyBattleStartMapRuntime_SetsBattleSceneMapSelection()
    {
        MapRuntimeStore mapStore = new();
        LobbyBattleStartCommand command = new(
            "battle-request-1",
            100UL,
            12,
            "session-abc",
            123456,
            "Chapter_A",
            "Stage_3");

        bool applied = LobbyBattleEntryService.ApplyBattleStartMapRuntime(
            mapStore,
            command);

        MapRuntimeData runtime = mapStore.Get();
        Assert.That(applied, Is.True);
        Assert.That(runtime.SelectedChapterId, Is.EqualTo("Chapter_A"));
        Assert.That(runtime.CurrentStage, Is.EqualTo("Stage_3"));
        Assert.That(runtime.CurrentMapId, Is.EqualTo(string.Empty));
        Assert.That(runtime.CurrentSceneName, Is.EqualTo(SceneName.Battle));
        Assert.That(runtime.IsRunInitialized, Is.False);
    }
}
