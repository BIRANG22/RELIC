using System.IO;
using NUnit.Framework;

public sealed class StartupDiagnosticsTests
{
    [Test]
    public void FormatCheckpoint_UsesStableTagAndThreeDigitSequence()
    {
        string message = StartupDiagnostics.FormatCheckpoint(12, "BEGIN", "Bootstrap.Data");

        Assert.That(message, Is.EqualTo("[Startup] #012 BEGIN Bootstrap.Data"));
    }

    [Test]
    public void Bootstrap_ContainsEveryApprovedStartupCheckpoint()
    {
        const string path = "Assets/Project/Scripts/Core/Bootstrap.cs";
        string source = File.ReadAllText(path);

        Assert.That(source, Does.Contain("Bootstrap.Resolution"));
        Assert.That(source, Does.Contain("Bootstrap.Blur"));
        Assert.That(source, Does.Contain("Bootstrap.Settings"));
        Assert.That(source, Does.Contain("Bootstrap.Save"));
        Assert.That(source, Does.Contain("Bootstrap.EventBus"));
        Assert.That(source, Does.Contain("Bootstrap.Data"));
        Assert.That(source, Does.Contain("Bootstrap.Audio"));
        Assert.That(source, Does.Contain("Bootstrap.Input"));
        Assert.That(source, Does.Contain("Bootstrap.UI"));
        Assert.That(source, Does.Contain("Bootstrap.GameManager"));
        Assert.That(source, Does.Contain("Bootstrap.Localization"));
        Assert.That(source, Does.Contain("StartupDiagnostics.Complete"));
    }

    [Test]
    public void StateAndSceneFlow_ContainDiagnosticBoundaries()
    {
        string stateMachine = File.ReadAllText(
            "Assets/Project/Scripts/Core/GameState/GameStateMachine.cs");
        string sceneFlow = File.ReadAllText(
            "Assets/Project/Scripts/Core/GameState/SceneFlow/SceneFlowManager.cs");

        Assert.That(stateMachine, Does.Contain("State.Exit"));
        Assert.That(stateMachine, Does.Contain("State.Enter"));
        Assert.That(stateMachine, Does.Contain("StartupDiagnostics.Fail"));

        Assert.That(sceneFlow, Does.Contain("Scene.Transition.Close"));
        Assert.That(sceneFlow, Does.Contain("Scene.Load"));
        Assert.That(sceneFlow, Does.Contain("Scene.Transition.Open"));
        Assert.That(sceneFlow, Does.Contain("StartupDiagnostics.Fail"));
    }
}
