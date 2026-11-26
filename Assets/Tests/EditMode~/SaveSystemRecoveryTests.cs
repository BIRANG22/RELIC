using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relic.Gameplay.Data;

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

    [Test]
    public void MonsterDiscoveries_RestoreFromSeparateFileWithoutChangingSavedCheckpoint()
    {
        string checkpointPath = Path.Combine(tempDirectory, "save.json");
        const string checkpointJson = "{\"Version\":1,\"Resume\":{\"NodeIndex\":4},\"Characters\":[{\"CurrentHP\":17}]}";
        File.WriteAllText(checkpointPath, checkpointJson);
        string discoveriesPath = Path.Combine(tempDirectory, "monsters.json");

        Assert.That(SaveSystem.TryWriteMonsterDiscoveries(discoveriesPath, new[] { " Mon_01 ", "Mon_02" }, out Exception firstError), Is.True, firstError?.ToString());
        Assert.That(SaveSystem.TryWriteMonsterDiscoveries(discoveriesPath, new[] { "Mon_02", "Mon_03", " " }, out Exception secondError), Is.True, secondError?.ToString());

        var player = new PlayerRuntimeData
        {
            Level = 3,
            DiscoveredMonsterIds = new List<string> { "Mon_Legacy", "Mon_02" },
            DiscoveredSkillIds = new List<string> { "Skill_01" }
        };
        SaveSystem.RestoreMonsterDiscoveries(discoveriesPath, player);
        SaveSystem.RestoreMonsterDiscoveries(discoveriesPath, player);

        Assert.That(player.DiscoveredMonsterIds, Is.EqualTo(new[] { "Mon_Legacy", "Mon_02", "Mon_01", "Mon_03" }));
        Assert.That(player.DiscoveredSkillIds, Is.EqualTo(new[] { "Skill_01" }));
        Assert.That(player.Level, Is.EqualTo(3));
        Assert.That(File.ReadAllText(checkpointPath), Is.EqualTo(checkpointJson));
    }

    [Test]
    public void MonsterDiscoveries_RestoreFromBackupWithoutProgressSave()
    {
        string discoveriesPath = Path.Combine(tempDirectory, "monsters.json");
        Assert.That(SaveSystem.TryWriteMonsterDiscoveries(discoveriesPath, new[] { "Mon_01" }, out _), Is.True);
        Assert.That(SaveSystem.TryWriteMonsterDiscoveries(discoveriesPath, new[] { "Mon_02" }, out _), Is.True);
        File.WriteAllText(discoveriesPath, "corrupt");

        var player = new PlayerRuntimeData { DiscoveredMonsterIds = null };
        SaveSystem.RestoreMonsterDiscoveries(discoveriesPath, player);

        Assert.That(player.DiscoveredMonsterIds, Is.EqualTo(new[] { "Mon_01" }));
        Assert.That(File.ReadAllText(discoveriesPath), Is.EqualTo(File.ReadAllText(discoveriesPath + ".bak")));
    }
}
