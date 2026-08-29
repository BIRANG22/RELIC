using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SkillSfxDatabaseTests
{
    [Test]
    public void SoundDatabase_ExposesSkillSfxEntriesSeparately()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        AudioClip clip = null;

        try
        {
            clip = AudioClip.Create("SkillSlash", 32, 1, 44100, false);
            SetPrivateField(
                database,
                "skillSfxList",
                new List<SoundData>
                {
                    new() { id = "skill.slash", clip = clip, volume = 0.35f }
                });

            Assert.That(database.SkillSfxEntries, Has.Count.EqualTo(1));
            Assert.That(database.SkillSfxEntries[0].id, Is.EqualTo("skill.slash"));
        }
        finally
        {
            DestroyObject(clip);
            DestroyObject(database);
        }
    }

    [Test]
    public void AudioManager_Initialize_RegistersSkillSfxForPlaySfxLookups()
    {
        GameObject audioObject = new("SkillSfxAudioManagerTest");
        AudioClip clip = null;
        SoundDatabase database = null;

        try
        {
            AudioSource source = audioObject.AddComponent<AudioSource>();
            AudioManager manager = audioObject.AddComponent<AudioManager>();
            database = ScriptableObject.CreateInstance<SoundDatabase>();
            clip = AudioClip.Create("SkillImpact", 32, 1, 44100, false);

            SetPrivateField(manager, "sfxSource", source);
            SetPrivateField(
                database,
                "skillSfxList",
                new List<SoundData>
                {
                    new() { id = "skill.impact", clip = clip, volume = 0.42f }
                });
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();

            Assert.That(manager.GetSfxVolume("skill.impact"), Is.EqualTo(0.42f).Within(0.001f));
            Assert.DoesNotThrow(() => manager.PlaySfx("skill.impact"));
        }
        finally
        {
            DestroyObject(clip);
            DestroyObject(database);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void AudioManager_PlaySfxSource_UsesLoopingDatabaseEntry()
    {
        GameObject audioObject = new("LoopingSkillSfxAudioManagerTest");
        AudioClip clip = null;
        SoundDatabase database = null;

        try
        {
            AudioSource template = audioObject.AddComponent<AudioSource>();
            template.priority = 42;
            template.spatialBlend = 0.75f;

            AudioManager manager = audioObject.AddComponent<AudioManager>();
            database = ScriptableObject.CreateInstance<SoundDatabase>();
            clip = AudioClip.Create("SkillLoop", 32, 1, 44100, false);

            SetPrivateField(manager, "sfxSource", template);
            SetPrivateField(
                database,
                "skillSfxList",
                new List<SoundData>
                {
                    new()
                    {
                        id = "skill.loop",
                        clip = clip,
                        volume = 0.5f,
                        pitch = 1.35f,
                        loop = true
                    }
                });
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();

            Vector3 position = new(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);
            AudioSource routedSource = manager.PlaySfxSource(
                " skill.loop ",
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
            DestroyObject(database);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void BattleVfxSfxEntry_UsesSkillSfxSoundIdCategory()
    {
        FieldInfo field = typeof(BattleVfxSfxEntry).GetField("sfxId");

        Assert.That(field, Is.Not.Null);

        SoundIdAttribute attribute = field.GetCustomAttribute<SoundIdAttribute>();

        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute.Category, Is.EqualTo(SoundCategory.SkillSfx));
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

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }
}
