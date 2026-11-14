using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class MonsterInfoStatusItemUITests
{
    private StatusEffectIconDatabase iconDatabase;

    [SetUp]
    public void SetUp()
    {
        iconDatabase = ScriptableObject.CreateInstance<StatusEffectIconDatabase>();
        SetPrivateColor("innateTraitColor", new Color(0.2f, 0.3f, 0.4f, 1f));
        SetPrivateColor("beneficialColor", new Color(0.1f, 0.6f, 0.9f, 1f));
        SetPrivateColor("harmfulColor", new Color(0.9f, 0.2f, 0.1f, 1f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(iconDatabase);
    }

    [TestCase(EffectType.Beneficial, false, 0.1f, 0.6f, 0.9f)]
    [TestCase(EffectType.Harmful, false, 0.9f, 0.2f, 0.1f)]
    [TestCase(EffectType.Neutral, false, 1f, 1f, 1f)]
    [TestCase(EffectType.Harmful, true, 0.2f, 0.3f, 0.4f)]
    public void ResolveIconColor_UsesSharedStatusEffectPolicy(
        EffectType effectType,
        bool isInnateTrait,
        float expectedR,
        float expectedG,
        float expectedB)
    {
        const string effectId = "E_Test";
        EffectDatabase effectDatabase = new EffectDatabase();
        effectDatabase.Initialize(new[]
        {
            new EffectMasterData { EffectId = effectId, EffectType = effectType }
        });

        Color result = MonsterInfoStatusItemUI.ResolveIconColor(
            effectId,
            isInnateTrait,
            iconDatabase,
            effectDatabase);

        Assert.That(result.r, Is.EqualTo(expectedR).Within(0.0001f));
        Assert.That(result.g, Is.EqualTo(expectedG).Within(0.0001f));
        Assert.That(result.b, Is.EqualTo(expectedB).Within(0.0001f));
    }

    [Test]
    public void ComposeIconColor_UsesEffectRgbAndPreservesImageAlpha()
    {
        Color result = MonsterInfoStatusItemUI.ComposeIconColor(
            new Color(0.8f, 0.4f, 0.2f, 1f),
            new Color(1f, 1f, 1f, 0.35f));

        Assert.That(result, Is.EqualTo(new Color(0.8f, 0.4f, 0.2f, 0.35f)));
    }

    [Test]
    public void ComposeIconColor_WhenCleared_RestoresWhiteAndPreservesImageAlpha()
    {
        Color result = MonsterInfoStatusItemUI.ComposeIconColor(
            Color.white,
            new Color(0.8f, 0.4f, 0.2f, 0.35f));

        Assert.That(result, Is.EqualTo(new Color(1f, 1f, 1f, 0.35f)));
    }

    private void SetPrivateColor(string fieldName, Color value)
    {
        FieldInfo field = typeof(StatusEffectIconDatabase).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(iconDatabase, value);
    }
}
