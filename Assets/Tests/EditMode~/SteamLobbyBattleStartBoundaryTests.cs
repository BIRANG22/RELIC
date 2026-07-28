using System.Reflection;
using System.IO;
using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class SteamLobbyBattleStartBoundaryTests
{
    [Test]
    public void BattleStartSynchronizer_ExposesLobbyLifecycleAndHostBroadcastBoundary()
    {
        Assert.That(
            typeof(SteamLobbyBattleStartSynchronizer).GetMethod(
                "EnterLobby",
                BindingFlags.Instance | BindingFlags.Public),
            Is.Not.Null);
        Assert.That(
            typeof(SteamLobbyBattleStartSynchronizer).GetMethod(
                "LeaveLobby",
                BindingFlags.Instance | BindingFlags.Public),
            Is.Not.Null);
        Assert.That(
            typeof(SteamLobbyBattleStartSynchronizer).GetMethod(
                "HandleLobbyDataChanged",
                BindingFlags.Instance | BindingFlags.Public),
            Is.Not.Null);
        Assert.That(
            typeof(SteamLobbyBattleStartSynchronizer).GetMethod(
                "TryBroadcastBattleStart",
                new[] { typeof(LobbySharedStateSnapshot), typeof(MapRuntimeData), typeof(LobbyBattleStartCommand).MakeByRefType() }),
            Is.Not.Null);
    }

    [Test]
    public void InviteController_BindsBattleStartSynchronizerToSteamLobby()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs");

        Assert.That(source, Does.Contain("SteamLobbyBattleStartSynchronizer"));
        Assert.That(source, Does.Contain("battleStartSynchronizer.EnterLobby"));
        Assert.That(source, Does.Contain("battleStartSynchronizer.HandleLobbyDataChanged"));
        Assert.That(source, Does.Contain("battleStartSynchronizer.LeaveLobby"));
    }

    [Test]
    public void BattlePlayButton_BroadcastsHostBattleStartBeforeEnteringBattle()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/UI/Lobby/BattlePlayButton.cs");

        Assert.That(source, Does.Contain("TryBroadcastNetworkBattleStart"));
        Assert.That(source, Does.Contain("PublishHostSnapshotNow"));
        Assert.That(source, Does.Contain("TryBroadcastBattleStart"));
        Assert.That(source, Does.Contain("LobbyBattleEntryService.EnterBattleAsync"));
    }
}
