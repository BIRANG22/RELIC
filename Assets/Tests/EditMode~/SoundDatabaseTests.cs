using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SoundDatabaseTests
{
    [Test]
    public void AudioManager_Initialize_UsesSoundDatabaseForIdLookups()
    {
        GameObject audioObject = new("SoundDatabaseAudioManagerTest");
        AudioClip sfxClip = null;
        AudioClip idClip = null;

        try
        {
            AudioSource source = audioObject.AddComponent<AudioSource>();
            AudioManager manager = audioObject.AddComponent<AudioManager>();
            SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
            sfxClip = AudioClip.Create("EnumSfx", 32, 1, 44100, false);
            idClip = AudioClip.Create("IdSfx", 32, 1, 44100, false);

            SetPrivateField(manager, "sfxSource", source);
            SetPrivateField(
                database,
                "sfxList",
                new List<SoundData>
                {
                    new()
                    {
                        id = "ui.confirm",
                        clip = sfxClip,
                        volume = 0.4f
                    },
                    new() { id = "vfx.custom", clip = idClip, volume = 0.7f }
                });
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();

            Assert.That(manager.GetSfxVolume(AudioIds.Sfx.Confirm), Is.EqualTo(0.4f).Within(0.001f));
            manager.PlaySfx("vfx.custom", 0.5f);
        }
        finally
        {
            DestroyObject(sfxClip);
            DestroyObject(idClip);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void AudioManager_Initialize_UsesEventSfxDatabaseForIdLookups()
    {
        GameObject audioObject = new("EventSoundDatabaseAudioManagerTest");
        AudioClip eventClip = null;

        try
        {
            AudioSource source = audioObject.AddComponent<AudioSource>();
            AudioManager manager = audioObject.AddComponent<AudioManager>();
            SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
            eventClip = AudioClip.Create("EventSfx", 32, 1, 44100, false);

            SetPrivateField(manager, "sfxSource", source);
            SetPrivateField(
                database,
                "eventSfxList",
                new List<SoundData>
                {
                    new()
                    {
                        id = "event.test",
                        clip = eventClip,
                        volume = 0.65f
                    }
                });
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();

            Assert.That(manager.TryGetSfxData("event.test", out SoundData data), Is.True);
            Assert.That(data.clip, Is.EqualTo(eventClip));
            Assert.That(data.volume, Is.EqualTo(0.65f).Within(0.001f));
        }
        finally
        {
            DestroyObject(eventClip);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void AudioManager_PlayBgm_UsesIdPrefixForLayeredBgm()
    {
        GameObject audioObject = new("SoundDatabaseBgmLayerTest");
        AudioClip mainClip = null;
        AudioClip layerClip = null;

        try
        {
            AudioSource mainSource = audioObject.AddComponent<AudioSource>();
            AudioManager manager = audioObject.AddComponent<AudioManager>();
            SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
            mainClip = AudioClip.Create("MainLayer", 32, 1, 44100, false);
            layerClip = AudioClip.Create("ExtraLayer", 32, 1, 44100, false);

            SetPrivateField(
                database,
                "bgmList",
                new List<SoundData>
                {
                    new()
                    {
                        id = "bgm.battle.main",
                        clip = mainClip,
                        volume = 0.8f
                    },
                    new()
                    {
                        id = "bgm.battle.ambience",
                        clip = layerClip,
                        volume = 0.2f
                    }
                });
            SetPrivateField(manager, "bgmSource", mainSource);
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();
            manager.PlayBgm(AudioIds.Bgm.Battle);

            AudioClip[] assignedClips = audioObject.GetComponentsInChildren<AudioSource>(true)
                .Select(source => source.clip)
                .Where(clip => clip == mainClip || clip == layerClip)
                .ToArray();

            Assert.That(assignedClips, Is.EquivalentTo(new[] { mainClip, layerClip }));
        }
        finally
        {
            DestroyObject(mainClip);
            DestroyObject(layerClip);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void SoundData_GetPlaybackPitch_ClampsFixedPitch()
    {
        SoundData data = new() { pitch = 1.25f };

        Assert.That(data.GetPlaybackPitch(), Is.EqualTo(1.25f).Within(0.001f));

        data.pitch = 9f;

        Assert.That(data.GetPlaybackPitch(), Is.EqualTo(SoundData.MaxPitch).Within(0.001f));

        data.pitch = -0.25f;

        Assert.That(data.GetPlaybackPitch(), Is.EqualTo(SoundData.MinPitch).Within(0.001f));
    }

    [Test]
    public void SoundData_GetPlaybackPitch_UsesRandomPitchRange()
    {
        SoundData data = new()
        {
            useRandomPitch = true,
            randomPitchMin = 1.6f,
            randomPitchMax = 0.4f
        };

        for (int i = 0; i < 20; i++)
        {
            float pitch = data.GetPlaybackPitch();

            Assert.That(pitch, Is.InRange(0.4f, 1.6f));
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
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
