using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

public class SkillVfxAudioAuditTests
{
    private const string SoundDatabasePath = "Assets/DB/SoundDatabase.asset";

    private static readonly Dictionary<string, string> ExpectedPlayerSkillSfxByVfxPath = new()
    {
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_01_attack_01.prefab"] = "skill.vfx.cha.01.attack.01",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_01_attack_02.prefab"] = "skill.vfx.cha.01.attack.02",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_01_attack_03.prefab"] = "skill.vfx.cha.01.attack.03",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_02_attack_01.prefab"] = "skill.vfx.cha.02.attack.01",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_02_attack_02.prefab"] = "skill.vfx.cha.02.attack.02",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_02_attack_03.prefab"] = "skill.vfx.cha.02.attack.03",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_03_attack_01.prefab"] = "skill.vfx.cha.03.attack.01",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_03_attack_02.prefab"] = "skill.vfx.cha.03.attack.02",
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_03_attack_03.prefab"] = "skill.vfx.cha.03.attack.03"
    };

    private static readonly Dictionary<string, string[]> ExpectedMonsterSkillSfxByVfxPath = new()
    {
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_E_02_attack_01.prefab"] =
            new[] { "skill.vfx.mon.e.02.attack.01" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_E_03_attack_03.prefab"] =
            new[] { "skill.vfx.mon.e.03.attack.03" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_01_attack_01.prefab"] =
            new[] { "skill.vfx.mon.n.01.attack.01.impact" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_01_attack_02_1.prefab"] =
            new[]
            {
                "skill.vfx.mon.n.01.attack.02.1.loop",
                "skill.vfx.mon.n.01.attack.02.1.hit"
            },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_01_attack_02.prefab"] =
            new[] { "skill.vfx.mon.n.01.attack.02" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_04_attack_02.prefab"] =
            new[] { "skill.vfx.mon.n.04.attack.02" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_05_attack_01.prefab"] =
            new[] { "skill.vfx.mon.n.05.attack.01" }
    };

    [Test]
    public void ScanEntries_ReportsEmbeddedAudioSourcesInsideSkillVfxPrefab()
    {
        GameObject prefab = new("SkillVfx");
        GameObject audioChild = new("Audio");

        try
        {
            audioChild.transform.SetParent(prefab.transform);
            audioChild.AddComponent<AudioSource>();

            List<SkillVfxEntry> entries = new()
            {
                new()
                {
                    SkillId = "S_Test",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = prefab,
                        sfx = new BattleVfxSfxEntry()
                    }
                }
            };

            IReadOnlyList<SkillVfxAudioAuditResult> results =
                SkillVfxAudioAudit.ScanEntries(entries);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].SkillId, Is.EqualTo("S_Test"));
            Assert.That(results[0].EmbeddedAudioSourceCount, Is.EqualTo(1));
            Assert.That(results[0].RequiresMigration, Is.True);
            Assert.That(results[0].Status, Is.EqualTo("Needs migration"));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void ScanEntries_AcceptsSkillVfxWithDatabaseSfxIdAndNoEmbeddedAudio()
    {
        GameObject prefab = new("SkillVfx");

        try
        {
            List<SkillVfxEntry> entries = new()
            {
                new()
                {
                    SkillId = "S_Test",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = prefab,
                        sfx = new BattleVfxSfxEntry
                        {
                            playSfx = true,
                            sfxId = "skill.test"
                        }
                    }
                }
            };

            IReadOnlyList<SkillVfxAudioAuditResult> results =
                SkillVfxAudioAudit.ScanEntries(entries);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].EmbeddedAudioSourceCount, Is.Zero);
            Assert.That(results[0].HasDatabaseSfxId, Is.True);
            Assert.That(results[0].DatabaseSfxIds, Is.EqualTo(new[] { "skill.test" }));
            Assert.That(results[0].RequiresMigration, Is.False);
            Assert.That(results[0].Status, Is.EqualTo("OK"));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void ProjectSkillVfxMigration_HasSoundDatabaseEntries()
    {
        SoundDatabase database = UnityAssetDatabase.LoadAssetAtPath<SoundDatabase>(SoundDatabasePath);

        Assert.That(database, Is.Not.Null);

        foreach (string expectedId in GetExpectedSkillSfxIds())
        {
            SoundData entry = database.SkillSfxEntries.FirstOrDefault(
                data => data != null && data.id == expectedId);

            Assert.That(entry, Is.Not.Null, expectedId);
            Assert.That(entry.clip, Is.Not.Null, expectedId);
        }
    }

    [Test]
    public void ProjectSkillVfxMigration_CharacterPresentationVfxUseDatabaseSfx()
    {
        List<SkillVfxAudioAuditResult> results = SkillVfxAudioAudit
            .ScanCharacterPresentationPrefabs()
            .Where(result => ExpectedPlayerSkillSfxByVfxPath.ContainsKey(NormalizePath(result.PrefabPath)))
            .ToList();

        Assert.That(results, Has.Count.EqualTo(14));

        foreach (SkillVfxAudioAuditResult result in results)
        {
            string prefabPath = NormalizePath(result.PrefabPath);

            Assert.That(
                result.DatabaseSfxId,
                Is.EqualTo(ExpectedPlayerSkillSfxByVfxPath[prefabPath]),
                prefabPath);
            Assert.That(
                result.DatabaseSfxIds,
                Is.EqualTo(new[] { ExpectedPlayerSkillSfxByVfxPath[prefabPath] }),
                prefabPath);
            Assert.That(result.EmbeddedAudioSourceCount, Is.Zero, prefabPath);
            Assert.That(result.RequiresMigration, Is.False, prefabPath);
        }
    }

    [Test]
    public void ProjectSkillVfxMigration_MonsterPresentationVfxUseDatabaseSfx()
    {
        List<SkillVfxAudioAuditResult> results = SkillVfxAudioAudit
            .ScanMonsterPresentationPrefabs()
            .Where(result => ExpectedMonsterSkillSfxByVfxPath.ContainsKey(NormalizePath(result.PrefabPath)))
            .ToList();

        Assert.That(results, Has.Count.EqualTo(7));

        foreach (SkillVfxAudioAuditResult result in results)
        {
            string prefabPath = NormalizePath(result.PrefabPath);

            Assert.That(
                result.DatabaseSfxIds,
                Is.EqualTo(ExpectedMonsterSkillSfxByVfxPath[prefabPath]),
                prefabPath);
            Assert.That(result.EmbeddedAudioSourceCount, Is.Zero, prefabPath);
            Assert.That(result.RequiresMigration, Is.False, prefabPath);
        }
    }

    private static IEnumerable<string> GetExpectedSkillSfxIds()
    {
        return ExpectedPlayerSkillSfxByVfxPath.Values
            .Concat(ExpectedMonsterSkillSfxByVfxPath.Values.SelectMany(ids => ids))
            .Distinct();
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? "" : path.Replace('\\', '/');
    }
}
