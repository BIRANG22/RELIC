using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SoundUsageScannerTests
{
    private sealed class SoundReferenceComponent : MonoBehaviour
    {
        [SoundId(SoundCategory.Sfx)]
        public string clickSfx;

        [SoundId(SoundCategory.Sfx)]
        public string missingSfx;
    }

    [Test]
    public void Scan_ReportsDatabaseEntriesReferencesMissingUnusedAndEmbeddedAudioSources()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        AudioClip clickClip = null;
        AudioClip unusedClip = null;
        AudioClip embeddedClip = null;
        GameObject prefab = null;

        try
        {
            clickClip = AudioClip.Create("Click", 32, 1, 44100, false);
            unusedClip = AudioClip.Create("Unused", 32, 1, 44100, false);
            embeddedClip = AudioClip.Create("Embedded", 32, 1, 44100, false);

            SetPrivateField(
                database,
                "sfxList",
                new List<SoundData>
                {
                    new() { id = "ui.click", clip = clickClip, volume = 0.5f },
                    new() { id = "ui.unused", clip = unusedClip, volume = 1f }
                });

            prefab = new GameObject("AudioUser");
            SoundReferenceComponent component = prefab.AddComponent<SoundReferenceComponent>();
            component.clickSfx = "ui.click";
            component.missingSfx = "ui.missing";

            AudioSource audioSource = prefab.AddComponent<AudioSource>();
            audioSource.clip = embeddedClip;
            audioSource.volume = 0.35f;
            audioSource.pitch = 1.2f;
            audioSource.loop = true;

            SoundUsageReport report = SoundUsageScanner.Scan(
                new SoundUsageScanOptions
                {
                    Database = database,
                    Prefabs = new[] { prefab }
                });

            Assert.That(report.DatabaseEntries.Select(entry => entry.Id), Contains.Item("ui.click"));
            Assert.That(report.DatabaseEntries.Select(entry => entry.Id), Contains.Item("ui.unused"));
            Assert.That(
                report.GetReferences("ui.click").Select(reference => reference.MemberPath),
                Contains.Item("SoundReferenceComponent.clickSfx"));
            Assert.That(report.MissingDatabaseEntryIds, Contains.Item("ui.missing"));
            Assert.That(report.UnusedDatabaseEntryIds, Contains.Item("ui.unused"));

            EmbeddedAudioSourceUsage embedded = report.EmbeddedAudioSources.Single();
            Assert.That(embedded.OwnerName, Is.EqualTo("AudioUser"));
            Assert.That(embedded.ClipName, Is.EqualTo("Embedded"));
            Assert.That(embedded.Loop, Is.True);
            Assert.That(embedded.Volume, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(embedded.Pitch, Is.EqualTo(1.2f).Within(0.001f));
        }
        finally
        {
            DestroyObject(prefab);
            DestroyObject(clickClip);
            DestroyObject(unusedClip);
            DestroyObject(embeddedClip);
            DestroyObject(database);
        }
    }

    [Test]
    public void Scan_ReportsVfxSoundDatabaseEntriesAndMissingSkillVfxMappings()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        AudioClip mainClip = null;
        GameObject mappedVfx = null;
        GameObject missingVfx = null;

        try
        {
            mainClip = AudioClip.Create("Main", 32, 1, 44100, false);
            mappedVfx = new GameObject("MappedSkillVfx");
            missingVfx = new GameObject("MissingSkillVfx");

            SetPrivateField(
                database,
                "playerSkillVfxSfxList",
                new List<VfxSoundData>
                {
                    new()
                    {
                        vfxPrefab = mappedVfx,
                        cues = new List<VfxSoundCue>
                        {
                            new() { clip = mainClip, volume = 0.75f }
                        }
                    }
                });

            List<SkillVfxEntry> skillEntries = new()
            {
                new()
                {
                    SkillId = "S_Mapped",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = mappedVfx
                    }
                },
                new()
                {
                    SkillId = "S_Missing",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = missingVfx
                    }
                }
            };

            SoundUsageReport report = SoundUsageScanner.Scan(
                new SoundUsageScanOptions
                {
                    Database = database,
                    SkillVfxEntries = skillEntries
                });

            SoundUsageVfxSoundEntry entry = report.VfxSoundEntries.Single();
            Assert.That(entry.Group, Is.EqualTo("Player"));
            Assert.That(entry.VfxName, Is.EqualTo("MappedSkillVfx"));
            Assert.That(entry.CueCount, Is.EqualTo(1));
            Assert.That(entry.ClipNames, Is.EqualTo("Main"));
            Assert.That(report.MissingVfxSoundPrefabPaths, Contains.Item("MissingSkillVfx"));
        }
        finally
        {
            DestroyObject(mappedVfx);
            DestroyObject(missingVfx);
            DestroyObject(mainClip);
            DestroyObject(database);
        }
    }

    [Test]
    public void BuildMarkdown_IncludesUsageSections()
    {
        SoundUsageReport report = new();
        report.DatabaseEntries.Add(new SoundUsageDatabaseEntry(
            SoundCategory.Sfx,
            "ui.click",
            "Click",
            1f,
            1f,
            false,
            null));
        report.References.Add(new SoundUsageReference(
            "ui.click",
            SoundCategory.Sfx,
            "Prefab",
            "Button.prefab",
            "ButtonSound.clickSfx"));
        report.MissingDatabaseEntryIds.Add("ui.missing");
        report.UnusedDatabaseEntryIds.Add("ui.unused");
        report.VfxSoundEntries.Add(new SoundUsageVfxSoundEntry(
            "Player",
            "Assets/Vfx.prefab",
            "Vfx",
            1,
            "SkillClip"));
        report.MissingVfxSoundPrefabPaths.Add("Assets/MissingVfx.prefab");
        report.EmbeddedAudioSources.Add(new EmbeddedAudioSourceUsage(
            "Vfx.prefab",
            "Vfx",
            "AudioSource",
            "Legacy",
            true,
            true,
            0.5f,
            0.9f,
            false));

        string markdown = SoundUsageScanner.BuildMarkdown(report);

        Assert.That(markdown, Does.Contain("# Sound Usage Audit"));
        Assert.That(markdown, Does.Contain("## Usage By Sound ID"));
        Assert.That(markdown, Does.Contain("ui.click"));
        Assert.That(markdown, Does.Contain("ui.missing"));
        Assert.That(markdown, Does.Contain("## VFX Sound Mappings"));
        Assert.That(markdown, Does.Contain("Assets/MissingVfx.prefab"));
        Assert.That(markdown, Does.Contain("## Embedded AudioSources"));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        Object.DestroyImmediate(target);
    }
}
