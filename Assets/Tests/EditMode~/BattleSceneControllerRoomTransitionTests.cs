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
}
