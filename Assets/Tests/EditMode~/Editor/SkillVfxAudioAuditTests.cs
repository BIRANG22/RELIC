using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

public class SkillVfxAudioAuditTests
{
    private const string SoundDatabasePath = "Assets/DB/SoundDatabase.asset";

    private static readonly Dictionary<string, string[]> ExpectedPlayerSkillClipsByVfxPath = new()
    {
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_01_attack_01.prefab"] =
            new[] { "Vefects_SFX_Fire_Burst_01" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_01_attack_02.prefab"] =
            new[] { "SFX_Slash_Generic" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_01_attack_03.prefab"] =
            new[] { "Vefects_SFX_Slash_Classic" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_02_attack_01.prefab"] =
            new[] { "Vefects_SFX_Slash_Classic" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_02_attack_02.prefab"] =
            new[] { "Vefects_SFX_Slash_Classic" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_02_attack_03.prefab"] =
            new[] { "Vefects_SFX_Slash_Classic" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_03_attack_01.prefab"] =
            new[] { "SFX_Magic_Attack_Sound_Hit" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_03_attack_02.prefab"] =
            new[] { "SFX_Bomb_Launch" },
        ["Assets/Project/Art/VFX/Cha/Vfx_Cha_03_attack_03.prefab"] =
            new[] { "SFX_Magic_Attack_Sound_Hit" }
    };

    private static readonly Dictionary<string, string[]> ExpectedMonsterSkillClipsByVfxPath = new()
    {
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_E_02_attack_01.prefab"] =
            new[] { "SFX_Vefects_Directional_Dust_01" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_E_03_attack_03.prefab"] =
            new[] { "SFX_Magic_Attack_Dark_Hit" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_01_attack_01.prefab"] =
            new[] { "SFX_Magic_Attack_Dark_Hit" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_01_attack_02_1.prefab"] =
            new[] { "SFX_Vefects_Hit_01", "SFX_Vefects_Fireball_01" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_01_attack_02.prefab"] =
            new[] { "SFX_Vefects_Directional_Dust_05_One_Shot_01" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_04_attack_02.prefab"] =
            new[] { "SFX_Slash_Dark" },
        ["Assets/Project/Art/VFX/Mon/Vfx_Mon_N_05_attack_01.prefab"] =
            new[] { "SFX_Bomb_Explosion" }
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
                        prefab = prefab
                    }
                }
            };

            IReadOnlyList<SkillVfxAudioAuditResult> results =
                SkillVfxAudioAudit.ScanEntries(entries, soundDatabase: null);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].SkillId, Is.EqualTo("S_Test"));
            Assert.That(results[0].EmbeddedAudioSourceCount, Is.EqualTo(1));
            Assert.That(results[0].RequiresMigration, Is.True);
            Assert.That(results[0].Status, Is.EqualTo("Needs embedded AudioSource cleanup"));
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void ScanEntries_AcceptsSkillVfxWithDatabaseVfxSoundAndNoEmbeddedAudio()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        GameObject prefab = new("SkillVfx");
        AudioClip clip = null;

        try
        {
            clip = AudioClip.Create("SkillClip", 32, 1, 44100, false);
            SetPrivateField(
                database,
                "playerSkillVfxSfxList",
                new List<VfxSoundData>
                {
                    new()
                    {
                        vfxPrefab = prefab,
                        cues = new List<VfxSoundCue>
                        {
                            new() { clip = clip }
                        }
                    }
                });

            List<SkillVfxEntry> entries = new()
            {
                new()
                {
                    SkillId = "S_Test",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = prefab
                    }
                }
            };

            IReadOnlyList<SkillVfxAudioAuditResult> results =
                SkillVfxAudioAudit.ScanEntries(entries, database);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].EmbeddedAudioSourceCount, Is.Zero);
            Assert.That(results[0].HasDatabaseVfxSound, Is.True);
            Assert.That(results[0].DatabaseClipNames, Is.EqualTo(new[] { "SkillClip" }));
            Assert.That(results[0].RequiresMigration, Is.False);
            Assert.That(results[0].Status, Is.EqualTo("OK"));
        }
        finally
        {
            Object.DestroyImmediate(clip);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void ProjectSkillVfxMigration_HasSoundDatabaseVfxEntries()
    {
        SoundDatabase database = UnityAssetDatabase.LoadAssetAtPath<SoundDatabase>(SoundDatabasePath);

        Assert.That(database, Is.Not.Null);

        foreach (string expectedPath in GetExpectedVfxPaths())
        {
            GameObject prefab = UnityAssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);

            Assert.That(prefab, Is.Not.Null, expectedPath);
            Assert.That(
                database.TryGetSkillVfxSfx(prefab, out VfxSoundData entry),
                Is.True,
                expectedPath);
            Assert.That(entry.HasPlayableCue, Is.True, expectedPath);
        }
    }

    [Test]
    public void ProjectSkillVfxMigration_CharacterPresentationVfxUseDatabaseVfxSound()
    {
        SoundDatabase database = UnityAssetDatabase.LoadAssetAtPath<SoundDatabase>(SoundDatabasePath);
        List<SkillVfxAudioAuditResult> results = SkillVfxAudioAudit
            .ScanCharacterPresentationPrefabs(database)
            .Where(result => ExpectedPlayerSkillClipsByVfxPath.ContainsKey(NormalizePath(result.PrefabPath)))
            .ToList();

        Assert.That(results, Has.Count.EqualTo(14));

        foreach (SkillVfxAudioAuditResult result in results)
        {
            string prefabPath = NormalizePath(result.PrefabPath);

            Assert.That(
                result.DatabaseClipNames,
                Is.EqualTo(ExpectedPlayerSkillClipsByVfxPath[prefabPath]),
                prefabPath);
            Assert.That(result.EmbeddedAudioSourceCount, Is.Zero, prefabPath);
            Assert.That(result.RequiresMigration, Is.False, prefabPath);
        }
    }

    [Test]
    public void ProjectSkillVfxMigration_MonsterPresentationVfxUseDatabaseVfxSound()
    {
        SoundDatabase database = UnityAssetDatabase.LoadAssetAtPath<SoundDatabase>(SoundDatabasePath);
        List<SkillVfxAudioAuditResult> results = SkillVfxAudioAudit
            .ScanMonsterPresentationPrefabs(database)
            .Where(result => ExpectedMonsterSkillClipsByVfxPath.ContainsKey(NormalizePath(result.PrefabPath)))
            .ToList();

        Assert.That(results, Has.Count.EqualTo(7));

        foreach (SkillVfxAudioAuditResult result in results)
        {
            string prefabPath = NormalizePath(result.PrefabPath);

            Assert.That(
                result.DatabaseClipNames,
                Is.EqualTo(ExpectedMonsterSkillClipsByVfxPath[prefabPath]),
                prefabPath);
            Assert.That(result.EmbeddedAudioSourceCount, Is.Zero, prefabPath);
            Assert.That(result.RequiresMigration, Is.False, prefabPath);
        }
    }

    private static IEnumerable<string> GetExpectedVfxPaths()
    {
        return ExpectedPlayerSkillClipsByVfxPath.Keys
            .Concat(ExpectedMonsterSkillClipsByVfxPath.Keys)
            .Distinct();
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? "" : path.Replace('\\', '/');
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
