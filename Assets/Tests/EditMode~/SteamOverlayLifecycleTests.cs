using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SteamOverlayLifecycleTests
{
    [Test]
    public void InitializeSteamBeforeSplashScreen_RunsBeforeRendererInitialization()
    {
        MethodInfo method = typeof(SteamLobbyInviteController).GetMethod(
            "InitializeSteamBeforeSplashScreen",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);

        RuntimeInitializeOnLoadMethodAttribute attribute =
            method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();

        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute.loadType, Is.EqualTo(RuntimeInitializeLoadType.BeforeSplashScreen));
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
