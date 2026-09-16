using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class CharacterIconDatabaseSkillCutsceneTests
{
    [Test]
    public void TryGetSkillCutsceneImage_ReturnsSpriteConfiguredForCharacter()
    {
        CharacterIconDatabase database = ScriptableObject.CreateInstance<CharacterIconDatabase>();
        Texture2D texture = new(2, 2);
        Sprite expected = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);

        try
        {
            SetEntries(database, new List<CharacterIconEntry>
            {
                new()
                {
                    CharacterId = "Char_01",
                    SkillCutsceneImage = expected
                }
            });

            bool found = database.TryGetSkillCutsceneImage(" Char_01 ", out Sprite actual);

            Assert.That(found, Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }
        finally
        {
            Object.DestroyImmediate(expected);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void TryGetSkillCutsceneImage_ReturnsFalseWhenCharacterHasNoConfiguredImage()
    {
        CharacterIconDatabase database = ScriptableObject.CreateInstance<CharacterIconDatabase>();

        try
        {
            SetEntries(database, new List<CharacterIconEntry>
            {
                new() { CharacterId = "Char_04" }
            });

            bool found = database.TryGetSkillCutsceneImage("Char_04", out Sprite actual);

            Assert.That(found, Is.False);
            Assert.That(actual, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void SkillCutsceneView_SetImage_AssignsTheCharacterCutsceneSprite()
    {
        GameObject root = new("SkillCutscene");
        GameObject imageObject = new("image");
        Texture2D texture = new(2, 2);
        Sprite expected = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);

        try
        {
            imageObject.transform.SetParent(root.transform);
            Image image = imageObject.AddComponent<Image>();
            SkillCutsceneView view = root.AddComponent<SkillCutsceneView>();
            SetPrivateField(view, "cutsceneImage", image);

            view.SetImage(expected);

            Assert.That(image.sprite, Is.SameAs(expected));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(expected);
            Object.DestroyImmediate(texture);
        }
    }

    private static void SetEntries(CharacterIconDatabase database, List<CharacterIconEntry> entries)
    {
        FieldInfo field = typeof(CharacterIconDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(database, entries);
        database.Initialize();
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
