using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class LobbyReadyPanelBackButtonTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";

    [Test]
    public void ReadyBackButton_IsChildOfReadyPanelSortingCanvas()
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string readyPanelRect = ExtractYamlObject(scene, "224", "1175251468");
        string parentPanelRect = ExtractYamlObject(scene, "224", "1213641074");
        string backButtonRect = ExtractYamlObject(scene, "224", "532234611");

        Assert.That(readyPanelRect, Does.Contain("- {fileID: 532234611}"));
        Assert.That(parentPanelRect, Does.Not.Contain("- {fileID: 532234611}"));
        Assert.That(backButtonRect, Does.Contain("m_Father: {fileID: 1175251468}"));
    }

    private static string ExtractYamlObject(string yaml, string classId, string fileId)
    {
        Match match = Regex.Match(
            yaml,
            $@"(?ms)^--- !u!{classId} &{fileId}\r?\n.*?(?=^--- !u!|\z)");
        Assert.That(match.Success, Is.True, $"YAML object {classId}:{fileId} was not found.");
        return match.Value;
    }
}
