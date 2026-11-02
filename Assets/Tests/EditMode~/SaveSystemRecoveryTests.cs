using System;
using System.IO;
using NUnit.Framework;

public sealed class SaveSystemRecoveryTests
{
    private string tempDirectory;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "RelicSaveRecoveryTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDirectory))
            Directory.Delete(tempDirectory, true);
    }

    [Test]
    public void WriteJsonAtomically_ReplacesTargetAndKeepsPreviousBackup()
    {
        string target = Path.Combine(tempDirectory, "save.json");
        File.WriteAllText(target, "old");

        Assert.That(SaveSystem.TryWriteJsonAtomically(target, "new", out Exception error), Is.True, error?.ToString());
        Assert.That(File.ReadAllText(target), Is.EqualTo("new"));
        Assert.That(File.ReadAllText(target + ".bak"), Is.EqualTo("old"));
        Assert.That(File.Exists(target + ".tmp"), Is.False);
    }

    [Test]
    public void TryReadJsonWithBackup_WhenPrimaryIsRejected_ReturnsBackup()
    {
        string target = Path.Combine(tempDirectory, "save.json");
        File.WriteAllText(target, "broken");
        File.WriteAllText(target + ".bak", "valid");

        bool read = SaveSystem.TryReadJsonWithBackup(
            target,
            json => json == "valid",
            out string json,
            out bool usedBackup);

        Assert.That(read, Is.True);
        Assert.That(json, Is.EqualTo("valid"));
        Assert.That(usedBackup, Is.True);
    }

    [Test]
    public void RestorePrimaryFromBackup_ReplacesBrokenPrimaryWithoutOverwritingBackup()
    {
        string target = Path.Combine(tempDirectory, "save.json");
        File.WriteAllText(target, "broken");
        File.WriteAllText(target + ".bak", "valid");

        bool restored = SaveSystem.TryRestorePrimaryFromBackup(target, "valid", out Exception error);

        Assert.That(restored, Is.True, error?.ToString());
        Assert.That(File.ReadAllText(target), Is.EqualTo("valid"));
        Assert.That(File.ReadAllText(target + ".bak"), Is.EqualTo("valid"));
    }

    [Test]
    public void ShouldDiscardInvalidBattleState_BattleSceneWithoutMapOrBattle_ReturnsTrue()
    {
        var save = new GameSaveData { Version = 1, ActiveSceneName = SceneName.Battle };

        Assert.That(SaveSystem.ShouldDiscardInvalidBattleState(save), Is.True);
    }

    [Test]
    public void ShouldDiscardInvalidBattleState_LobbySaveWithoutBattleResidue_ReturnsFalse()
    {
        var save = new GameSaveData { Version = 1, ActiveSceneName = SceneName.Lobby };

        Assert.That(SaveSystem.ShouldDiscardInvalidBattleState(save), Is.False);
    }

    [Test]
    public void WriteJsonAtomically_WhenTemporaryFileIsLocked_PreservesExistingTarget()
    {
        string target = Path.Combine(tempDirectory, "save.json");
        File.WriteAllText(target, "old");
        using var lockStream = new FileStream(target + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        Assert.That(SaveSystem.TryWriteJsonAtomically(target, "new", out _), Is.False);
        Assert.That(File.ReadAllText(target), Is.EqualTo("old"));
    }
}
