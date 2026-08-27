using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AudioManagerTests
{
    [Test]
    public void PlayBgm_WithMultipleClipsForSameId_AssignsEachClipToSeparateSource()
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
                new List<SoundData>
                {
                    new()
                    {
                        id = "bgm.battle.main",
                        clip = mainClip
                    },
                    new()
                    {
                        id = "bgm.battle.ambience",
                        clip = layerClip
                    }
                });
            SetPrivateField(manager, "soundDatabase", database);

            LogAssert.ignoreFailingMessages = true;
            manager.Initialize();
            manager.PlayBgm(AudioIds.Bgm.Battle);

            AudioClip[] assignedClips = audioObject
                .GetComponentsInChildren<AudioSource>(true)
                .Where(source => source.clip == mainClip || source.clip == layerClip)
                .Select(source => source.clip)
                .ToArray();

            Assert.That(assignedClips, Is.EquivalentTo(new[] { mainClip, layerClip }));
        }
        finally
        {
            LogAssert.ignoreFailingMessages = false;
            DestroyObject(mainClip);
            DestroyObject(layerClip);
            DestroyObject(audioObject);
        }
    }

    [Test]
    public void PlayBgm_AppliesIndividualVolumeForEachBgmEntry()
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
                        volume = 0.25f
                    }
                });
            SetPrivateField(manager, "soundDatabase", database);

            LogAssert.ignoreFailingMessages = true;
            manager.Initialize();
            manager.PlayBgm(AudioIds.Bgm.Battle);

            AudioSource layerSource = audioObject
                .GetComponentsInChildren<AudioSource>(true)
                .First(source => source.clip == layerClip);

            Assert.That(mainSource.volume, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(layerSource.volume, Is.EqualTo(0.25f).Within(0.001f));
        }
        finally
        {
            LogAssert.ignoreFailingMessages = false;
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
