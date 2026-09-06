using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VfxSoundDatabaseTests
{
    [Test]
    public void SoundDatabase_ExposesPlayerAndMonsterSkillVfxSfxEntriesSeparately()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        GameObject playerVfx = new("PlayerSkillVfx");
        GameObject monsterVfx = new("MonsterSkillVfx");
        AudioClip playerClip = null;
        AudioClip monsterClip = null;

        try
        {
            playerClip = AudioClip.Create("PlayerSkillSlash", 32, 1, 44100, false);
            monsterClip = AudioClip.Create("MonsterSkillImpact", 32, 1, 44100, false);

            SetPrivateField(
                database,
                "playerSkillVfxSfxList",
                new List<VfxSoundData>
                {
                    new()
                    {
                        vfxPrefab = playerVfx,
                        cues = new List<VfxSoundCue>
                        {
                            new() { clip = playerClip, volume = 0.35f }
                        }
                    }
                });
            SetPrivateField(
                database,
                "monsterSkillVfxSfxList",
                new List<VfxSoundData>
                {
                    new()
                    {
                        vfxPrefab = monsterVfx,
                        cues = new List<VfxSoundCue>
                        {
                            new() { clip = monsterClip, volume = 0.65f }
                        }
                    }
                });

            Assert.That(database.PlayerSkillVfxSfxEntries, Has.Count.EqualTo(1));
            Assert.That(database.PlayerSkillVfxSfxEntries[0].vfxPrefab, Is.SameAs(playerVfx));
            Assert.That(database.MonsterSkillVfxSfxEntries, Has.Count.EqualTo(1));
            Assert.That(database.MonsterSkillVfxSfxEntries[0].vfxPrefab, Is.SameAs(monsterVfx));
        }
        finally
        {
            DestroyObject(playerClip);
            DestroyObject(monsterClip);
            DestroyObject(playerVfx);
            DestroyObject(monsterVfx);
            DestroyObject(database);
        }
    }

    [Test]
    public void SoundDatabase_TryGetSkillVfxSfx_ReturnsEntryByVfxPrefab()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        GameObject mappedVfx = new("MappedSkillVfx");
        GameObject unmappedVfx = new("UnmappedSkillVfx");
        AudioClip clip = null;

        try
        {
            clip = AudioClip.Create("MappedSkillClip", 32, 1, 44100, false);
            VfxSoundData expected = new()
            {
                vfxPrefab = mappedVfx,
                cues = new List<VfxSoundCue>
                {
                    new() { clip = clip, volume = 0.5f, pitch = 1.25f }
                }
            };
            SetPrivateField(
                database,
                "playerSkillVfxSfxList",
                new List<VfxSoundData> { expected });

            bool found = database.TryGetSkillVfxSfx(mappedVfx, out VfxSoundData actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(actual.Cues, Has.Count.EqualTo(1));
            Assert.That(database.TryGetSkillVfxSfx(unmappedVfx, out _), Is.False);
        }
        finally
        {
            DestroyObject(clip);
            DestroyObject(mappedVfx);
            DestroyObject(unmappedVfx);
            DestroyObject(database);
        }
    }

    [Test]
    public void AudioManager_PlayVfxSfxCue_UsesLoopingCuePlaybackProperties()
    {
        GameObject audioObject = new("LoopingVfxSfxAudioManagerTest");
        AudioClip clip = null;

        try
        {
            AudioSource template = audioObject.AddComponent<AudioSource>();
            template.priority = 42;
            template.spatialBlend = 0.75f;

            AudioManager manager = audioObject.AddComponent<AudioManager>();
            clip = AudioClip.Create("SkillLoop", 32, 1, 44100, false);
            SetPrivateField(manager, "sfxSource", template);
            manager.Initialize();

            Vector3 position = new(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);
            AudioSource routedSource = manager.PlayVfxSfxCue(
                new VfxSoundCue
                {
                    clip = clip,
                    volume = 0.5f,
                    pitch = 1.35f,
                    loop = true
                },
                position,
                rotation,
                volumeMultiplier: 0.5f);

            Assert.That(routedSource, Is.Not.Null);
            Assert.That(routedSource.clip, Is.SameAs(clip));
            Assert.That(routedSource.loop, Is.True);
            Assert.That(routedSource.priority, Is.EqualTo(42));
            Assert.That(routedSource.volume, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(routedSource.pitch, Is.EqualTo(1.35f).Within(0.0001f));
            Assert.That(routedSource.spatialBlend, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(routedSource.transform.position, Is.EqualTo(position));
            Assert.That(
                routedSource.transform.rotation.eulerAngles,
                Is.EqualTo(rotation.eulerAngles));
        }
        finally
        {
            DestroyObject(clip);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void SkillVfxSoundModel_RemovesSkillSfxIdsFromVfxEntries()
    {
        Assert.That(Enum.GetNames(typeof(SoundCategory)), Does.Not.Contain("SkillSfx"));
        Assert.That(typeof(BattleVfxEntry).GetField("sfx"), Is.Null);
        Assert.That(Type.GetType("BattleVfxSfxEntry"), Is.Null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }
}
