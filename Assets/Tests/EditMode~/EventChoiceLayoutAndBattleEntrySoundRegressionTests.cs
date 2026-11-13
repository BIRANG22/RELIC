using System.IO;
using NUnit.Framework;
using UnityEngine;

public class EventChoiceLayoutAndBattleEntrySoundRegressionTests
{
    private static string ProjectPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
    }

    private static string ReadNormalized(string relativePath)
    {
        return File.ReadAllText(ProjectPath(relativePath)).Replace("\r\n", "\n");
    }

    [Test]
    public void BattleEventChoiceSlots_DoNotAnimateLayoutOwnedPosition()
    {
        string scene = ReadNormalized("Assets/Project/Scenes/YDM/Battle.unity");

        Assert.That(Count(scene, "hoverButtonMoveOffset: {x: 0, y: 0, z: 0}"), Is.GreaterThanOrEqualTo(3));
        Assert.That(scene, Does.Contain("buttonContent: {fileID: 910000000000000016}\n  hoverButtonMoveOffset: {x: 0, y: 0, z: 0}\n  hoverButtonScale: {x: 1.1, y: 1.1, z: 1}"));
        Assert.That(scene, Does.Contain("buttonContent: {fileID: 910000000000000038}\n  hoverButtonMoveOffset: {x: 0, y: 0, z: 0}\n  hoverButtonScale: {x: 1.1, y: 1.1, z: 1}"));
        Assert.That(scene, Does.Contain("buttonContent: {fileID: 910000000000000060}\n  hoverButtonMoveOffset: {x: 0, y: 0, z: 0}\n  hoverButtonScale: {x: 1.1, y: 1.1, z: 1}"));
    }

    [Test]
    public void BattleScenes_DoNotContainInvalidSkylightSoundId()
    {
        string battle = ReadNormalized("Assets/Project/Scenes/YDM/Battle.unity");
        string debugBattle = ReadNormalized("Assets/Project/Scenes/YDM/DebugBattle.unity");
        string component = ReadNormalized("Assets/Project/Scripts/Art/DBAudioSource.cs");

        Assert.That(battle, Does.Not.Contain("event..09.Skylight"));
        Assert.That(debugBattle, Does.Not.Contain("event..09.Skylight"));
        Assert.That(component, Does.Not.Contain("event..09.Skylight"));
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
