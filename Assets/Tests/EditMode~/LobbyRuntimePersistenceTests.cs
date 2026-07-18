using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class LobbyRuntimePersistenceTests
{
    [Test]
    public void GetOrCreate_FirstCreation_StartsWith999BlueDustium()
    {
        var store = new LobbyRuntimeStore();

        Assert.That(store.GetOrCreate().BlueDustium, Is.EqualTo(999));
    }

    [TestCase(0)]
    [TestCase(321)]
    public void Set_ExistingBalance_IsNotReinitialized(int savedBalance)
    {
        var store = new LobbyRuntimeStore();
        store.Set(new LobbyRuntimeData { BlueDustium = savedBalance });

        Assert.That(store.GetOrCreate().BlueDustium, Is.EqualTo(savedBalance));
    }

    [Test]
    public void SaveSnapshot_RoundTripsLobbyRuntime()
    {
        var lobby = new LobbyRuntimeData
        {
            BlueDustium = 777,
            OwnedRelicIds = new List<string> { "R_Active_1" },
            SkillInventoryIds = new List<string> { "S_Test" }
        };

        GameSaveData save = SaveSystem.CreateSaveDataSnapshot(
            null, null, null, null, null, null, "Lobby", GameMode.None, lobby);
        string json = JsonUtility.ToJson(save);
        GameSaveData restored = JsonUtility.FromJson<GameSaveData>(json);

        Assert.Multiple(() =>
        {
            Assert.That(restored.Lobby.BlueDustium, Is.EqualTo(777));
            Assert.That(restored.Lobby.OwnedRelicIds, Is.EquivalentTo(lobby.OwnedRelicIds));
            Assert.That(restored.Lobby.SkillInventoryIds, Is.EquivalentTo(lobby.SkillInventoryIds));
        });
    }
}
