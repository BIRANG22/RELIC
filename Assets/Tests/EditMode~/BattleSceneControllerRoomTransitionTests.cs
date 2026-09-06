using System.IO;
using NUnit.Framework;

public sealed class BattleSceneControllerRoomTransitionTests
{
    private const string SourcePath = "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs";

    [Test]
    public void OpenRoom_ResetsCameraWhenOpeningNonBattleRooms()
    {
        string source = File.ReadAllText(SourcePath);

        Assert.That(source, Does.Contain("ResetCameraForNonBattleRoom(roomObject, isBattleRoom);"));
        Assert.That(source, Does.Contain("private static void ResetCameraForNonBattleRoom(GameObject roomObject, bool isBattleRoom)"));
        Assert.That(source, Does.Contain("cameraController.ForceReturnMapImmediate();"));
    }

    [Test]
    public void StartNode_OpensEventRoom()
    {
        string source = File.ReadAllText(SourcePath);

        Assert.That(source, Does.Contain("case \"Start\":"));
        Assert.That(source, Does.Contain("case \"Start\":\r\n                OpenSpecialEvent(nodeData);"));
    }

    [Test]
    public void RestRoom_RefreshesSharedPresentationAfterOpeningRoom()
    {
        string source = File.ReadAllText(SourcePath);

        Assert.That(source, Does.Contain("OpenRoom(restRoom, \"RestRoom\");\r\n        sharedRoomPresentationController?.RefreshForMap(nodeData.MapId);"));
    }
}
