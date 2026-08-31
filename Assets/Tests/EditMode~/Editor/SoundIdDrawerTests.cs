using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SoundIdDrawerTests
{
    [Test]
    public void GetSoundIds_ReturnsOnlyIdsForSelectedCategory()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        AudioClip bgmClip = null;
        AudioClip sfxClip = null;

        try
        {
            bgmClip = AudioClip.Create("Bgm", 32, 1, 44100, false);
            sfxClip = AudioClip.Create("Sfx", 32, 1, 44100, false);

            SetPrivateField(
                database,
                "bgmList",
                new List<SoundData>
                {
                    new() { id = "bgm.lobby", aliases = new List<string> { "Lobby" }, clip = bgmClip }
                });
            SetPrivateField(
                database,
                "sfxList",
                new List<SoundData>
                {
                    new() { id = "ui.normal.click", aliases = new List<string> { "NormalButtonClick" }, clip = sfxClip }
                });

            IReadOnlyList<string> bgmIds = SoundIdDrawer.GetSoundIdsForTest(
                database,
                SoundCategory.Bgm);
            IReadOnlyList<string> sfxIds = SoundIdDrawer.GetSoundIdsForTest(
                database,
                SoundCategory.Sfx);

            Assert.That(bgmIds, Is.EquivalentTo(new[] { "bgm.lobby" }));
            Assert.That(sfxIds, Is.EquivalentTo(new[] { "ui.normal.click" }));
        }
        finally
        {
            DestroyObject(bgmClip);
            DestroyObject(sfxClip);
            DestroyObject(database);
        }
    }

    [Test]
    public void GetSoundIds_ReturnsOnlyEventSfxIdsForEventSfxCategory()
    {
        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        AudioClip sfxClip = null;
        AudioClip eventClip = null;

        try
        {
            sfxClip = AudioClip.Create("Sfx", 32, 1, 44100, false);
            eventClip = AudioClip.Create("EventSfx", 32, 1, 44100, false);

            SetPrivateField(
                database,
                "sfxList",
                new List<SoundData>
                {
                    new() { id = "ui.normal.click", clip = sfxClip }
                });
            SetPrivateField(
                database,
                "eventSfxList",
                new List<SoundData>
                {
                    new() { id = "event.test", clip = eventClip }
                });

            IReadOnlyList<string> sfxIds = SoundIdDrawer.GetSoundIdsForTest(
                database,
                SoundCategory.Sfx);
            IReadOnlyList<string> eventSfxIds = SoundIdDrawer.GetSoundIdsForTest(
                database,
                SoundCategory.EventSfx);

            Assert.That(sfxIds, Is.EquivalentTo(new[] { "ui.normal.click" }));
            Assert.That(eventSfxIds, Is.EquivalentTo(new[] { "event.test" }));
        }
        finally
        {
            DestroyObject(sfxClip);
            DestroyObject(eventClip);
            DestroyObject(database);
        }
    }

    [Test]
    public void SoundIdAttribute_UsesDefaultSoundDatabasePath()
    {
        SoundIdAttribute attribute = new(SoundCategory.Sfx);

        Assert.That(attribute.Category, Is.EqualTo(SoundCategory.Sfx));
        Assert.That(attribute.DatabasePath, Is.EqualTo(SoundIdAttribute.DefaultDatabasePath));
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
