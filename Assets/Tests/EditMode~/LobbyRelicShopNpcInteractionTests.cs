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

    [Test]
    public void LobbyScene_ContainsSerializedRelicShopPanelReferences()
    {
        string presenter = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs");
        string scene = File.ReadAllText(ScenePath);

        Assert.That(presenter, Does.Contain("[SerializeField] private GameObject panelRoot;"));
        Assert.That(presenter, Does.Contain("[SerializeField] private LobbyRelicOfferButtonUI[] offerButtons"));
        Assert.That(presenter, Does.Contain("[SerializeField] private LobbyRelicRefreshButtonUI refreshButton;"));
        Assert.That(scene, Does.Contain("m_Name: RelicShopPanel"));
        Assert.That(scene, Does.Contain("panelRoot: {fileID: 230010000}"));
        Assert.That(scene, Does.Contain("- {fileID: 230010020}"));
        Assert.That(scene, Does.Contain("- {fileID: 230010030}"));
        Assert.That(scene, Does.Contain("- {fileID: 230010040}"));
        Assert.That(scene, Does.Contain("refreshButton: {fileID: 230010051}"));
    }
}
