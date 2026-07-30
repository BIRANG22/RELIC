using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SteamOverlayLifecycleTests
{
    [TestCase("RELIC.exe", false)]
    [TestCase("RELIC.exe -logFile player.log", false)]
    [TestCase("RELIC.exe +connect_lobby 109775241199441234", true)]
    public void ShouldInitializeSteamForLaunchCommand_RequiresConnectLobby(
        string commandLine,
        bool expected)
    {
        MethodInfo method = typeof(SteamLobbyInviteController).GetMethod(
            "ShouldInitializeSteamForLaunchCommand",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        Assert.That(
            (bool)method.Invoke(null, new object[] { commandLine }),
            Is.EqualTo(expected));
    }

    [Test]
    public void Controller_DeclaresApplicationQuitShutdownHandler()
    {
        MethodInfo method = typeof(SteamLobbyInviteController).GetMethod(
            "ShutdownSteamOnApplicationQuit",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
    }
}
