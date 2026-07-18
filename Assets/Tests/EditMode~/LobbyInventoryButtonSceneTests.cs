using System.IO;
using NUnit.Framework;

public class LobbyInventoryButtonSceneTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";

    [Test]
    public void LobbyScene_ContainsSerializedInventoryMoveButton()
    {
        string yaml = File.ReadAllText(LobbyScenePath);

        Assert.That(yaml, Does.Contain("m_Name: Inventory"));
        Assert.That(yaml, Does.Contain("m_MethodName: MovePanel"));
        Assert.That(yaml, Does.Contain("moveOffset: {x: 0, y: -1080}"));
    }
}
