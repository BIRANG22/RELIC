using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AudioManagerTests
{
    [Test]
    public void PlayBgmState_PlaysConfiguredMainAndAmbienceWithoutPrefixLookup()
    {
        GameObject audioObject = new("AudioManagerBgmStateTest");
        AudioClip mainClip = null;
        AudioClip ambienceClip = null;

        try
        {
            AudioSource mainSource = audioObject.AddComponent<AudioSource>();
            AudioManager manager = audioObject.AddComponent<AudioManager>();
            SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
            mainClip = AudioClip.Create("BattleStateMain", 32, 1, 44100, false);
            ambienceClip = AudioClip.Create("BattleStateAmbience", 32, 1, 44100, false);

            SetPrivateField(manager, "bgmSource", mainSource);
            SetPrivateField(
                database,
                "bgmList",
                new List<BgmData>
                {
                    new()
                    {
                        state = BgmState.BattleMain,
                        mainClip = new BgmClipData { clip = mainClip },
                        ambienceClips = new List<BgmClipData>
                        {
                            new() { clip = ambienceClip }
                        }
                    }
                });
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();
            manager.PlayBgmState(BgmState.BattleMain);

            AudioSource[] sources = audioObject.GetComponentsInChildren<AudioSource>(true);
            Assert.That(sources.Any(source => source.clip == mainClip && source.loop), Is.True);
            Assert.That(sources.Any(source => source.clip == ambienceClip && source.loop), Is.True);
        }
        finally
        {
            DestroyObject(mainClip);
            DestroyObject(ambienceClip);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void PlayBgmState_WithMultipleAmbience_AssignsEachClipToSeparateSource()
    {
        GameObject audioObject = new("AudioManagerTest");
        AudioClip mainClip = null;
        AudioClip layerClip = null;

        try
        {
            AudioSource mainSource = audioObject.AddComponent<AudioSource>();
            AudioManager manager = audioObject.AddComponent<AudioManager>();
            SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
            mainClip = AudioClip.Create("BattleMain", 32, 1, 44100, false);
            layerClip = AudioClip.Create("BattleLayer", 32, 1, 44100, false);

            SetPrivateField(manager, "bgmSource", mainSource);
            SetPrivateField(
                database,
                "bgmList",
                new List<BgmData>
                {
                    new()
                    {
                        state = BgmState.BattleMain,
                        mainClip = new BgmClipData { clip = mainClip },
                        ambienceClips = new List<BgmClipData> { new() { clip = layerClip } }
                    }
                });
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();
            manager.PlayBgm(BgmState.BattleMain);

            AudioClip[] assignedClips = audioObject
                .GetComponentsInChildren<AudioSource>(true)
                .Where(source => source.clip == mainClip || source.clip == layerClip)
                .Select(source => source.clip)
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
    public void PlayBgmState_AppliesSharedBgmVolumeToMainAndAmbience()
    {
        GameObject audioObject = new("AudioManagerVolumeTest");
        AudioClip mainClip = null;
        AudioClip layerClip = null;

        try
        {
            AudioSource mainSource = audioObject.AddComponent<AudioSource>();
            AudioManager manager = audioObject.AddComponent<AudioManager>();
            SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
            mainClip = AudioClip.Create("BattleMain", 32, 1, 44100, false);
            layerClip = AudioClip.Create("BattleLayer", 32, 1, 44100, false);

            SetPrivateField(manager, "bgmSource", mainSource);
            SetPrivateField(
                database,
                "bgmList",
                new List<BgmData>
                {
                    new()
                    {
                        state = BgmState.BattleMain,
                        mainClip = new BgmClipData { clip = mainClip },
                        ambienceClips = new List<BgmClipData> { new() { clip = layerClip } }
                    }
                });
            SetPrivateField(manager, "soundDatabase", database);

            manager.Initialize();
            manager.PlayBgm(BgmState.BattleMain);

            AudioSource layerSource = audioObject
                .GetComponentsInChildren<AudioSource>(true)
                .First(source => source.clip == layerClip);

            Assert.That(layerSource.volume, Is.EqualTo(mainSource.volume).Within(0.001f));
        }
        finally
        {
            DestroyObject(mainClip);
            DestroyObject(layerClip);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void SoundDatabaseData_DoesNotExposeEnumTypeFields()
    {
        Assert.That(typeof(SoundData).GetField("type"), Is.Null);
        Assert.That(typeof(SoundData).GetField("useType"), Is.Null);
    }

    [Test]
    public void AudioManager_DoesNotExposeEnumBasedPlaybackMethods()
    {
        bool hasEnumParameter = typeof(AudioManager)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(method => method.GetParameters())
            .Any(parameter =>
                parameter.ParameterType.Name == "BgmType" ||
                parameter.ParameterType.Name == "SfxType");

        Assert.That(hasEnumParameter, Is.False);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
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
