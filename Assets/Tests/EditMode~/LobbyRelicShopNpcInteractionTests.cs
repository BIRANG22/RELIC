using System.IO;
using NUnit.Framework;

public class LobbyRelicShopNpcInteractionTests
{
    private const string ScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string InteractionPath =
        "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopNpcInteraction.cs";
    private const string InteractionGuid = "8f1d8c3bb10d4efc965c2a6bdac3a9e1";

    [Test]
    public void LobbyNpc_UsesDirectMouseDownInteractionConnectedToShopPresenter()
    {
        string source = File.ReadAllText(InteractionPath);
        string scene = File.ReadAllText(ScenePath);

        Assert.That(source, Does.Contain("private void OnMouseDown()"));
        Assert.That(source, Does.Contain("presenter?.Open();"));
        Assert.That(scene, Does.Contain($"guid: {InteractionGuid}"));
        Assert.That(scene, Does.Contain("presenter: {fileID: 2200000502}"));
    }
}
