using System.IO;
using NUnit.Framework;

public sealed class DemoTitleStateTests
{
    [Test]
    public void DemoTitleState_UsesDemoTitleTypeAndScene()
    {
        const string statePath =
            "Assets/Project/Scripts/Core/GameState/States/DemoTitleState.cs";
        string source = File.ReadAllText(statePath);

        Assert.That(source, Does.Contain("GameStateType.DemoTitle"));
        Assert.That(source, Does.Contain("SceneName.DemoTitle"));
        Assert.That(source, Does.Contain("BgmState.TitleMain"));
    }

    [Test]
    public void GameManager_RegistersDemoTitleState()
    {
        const string managerPath =
            "Assets/Project/Scripts/Core/Managers/GameManager.cs";
        string source = File.ReadAllText(managerPath);

        Assert.That(source, Does.Contain(
            "StateMachine.RegisterState(new DemoTitleState(sceneFlowManager))"));
    }

    [Test]
    public void SceneName_DefinesDemoTitleScene()
    {
        const string sceneNamePath =
            "Assets/Project/Scripts/Core/GameState/SceneFlow/SceneName.cs";
        string source = File.ReadAllText(sceneNamePath);

        Assert.That(source, Does.Contain(
            "public const string DemoTitle = \"DemoTitle\";"));
    }
}
