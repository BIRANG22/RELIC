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
        AudioClip skillSfxClip = null;

        try
        {
            bgmClip = AudioClip.Create("Bgm", 32, 1, 44100, false);
            sfxClip = AudioClip.Create("Sfx", 32, 1, 44100, false);
            skillSfxClip = AudioClip.Create("SkillSfx", 32, 1, 44100, false);

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
            SetPrivateField(
                database,
                "skillSfxList",
                new List<SoundData>
                {
                    new() { id = "skill.slash", aliases = new List<string> { "SkillSlash" }, clip = skillSfxClip }
                });

            IReadOnlyList<string> bgmIds = SoundIdDrawer.GetSoundIdsForTest(
                database,
                SoundCategory.Bgm);
            IReadOnlyList<string> sfxIds = SoundIdDrawer.GetSoundIdsForTest(
                database,
                SoundCategory.Sfx);
            IReadOnlyList<string> skillSfxIds = SoundIdDrawer.GetSoundIdsForTest(
                database,
                SoundCategory.SkillSfx);

            Assert.That(bgmIds, Is.EquivalentTo(new[] { "bgm.lobby" }));
            Assert.That(sfxIds, Is.EquivalentTo(new[] { "ui.normal.click" }));
            Assert.That(skillSfxIds, Is.EquivalentTo(new[] { "skill.slash" }));
        }
        finally
        {
            DestroyObject(bgmClip);
            DestroyObject(sfxClip);
            DestroyObject(skillSfxClip);
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
