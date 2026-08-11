using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class StatusEffectTypeIconTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void DataRowMapper_MapsEffectTypeColumn()
    {
        var row = new Dictionary<string, string>
        {
            ["EffectId"] = "E_Boost",
            ["EffectType"] = "Beneficial"
        };

        EffectMasterData data = DataRowMapper.Map<EffectMasterData>(row);

        Assert.That(data.EffectType, Is.EqualTo(EffectType.Beneficial));
    }

    [Test]
    public void TryGetTypeIcon_UsesCommonSpriteForEffectTypeAndNoSpriteForNeutral()
    {
        StatusEffectIconDatabase iconDatabase = CreateIconDatabase(
            out Sprite beneficialIcon,
            out Sprite harmfulIcon);

        EffectDatabase effectDatabase = new();
        effectDatabase.Initialize(new[]
        {
            new EffectMasterData { EffectId = "E_Buff", EffectType = EffectType.Beneficial },
            new EffectMasterData { EffectId = "E_Debuff", EffectType = EffectType.Harmful },
            new EffectMasterData { EffectId = "E_Neutral", EffectType = EffectType.Neutral },
        });

        Assert.That(iconDatabase.TryGetTypeIcon("E_Buff", effectDatabase, out Sprite buffResult), Is.True);
        Assert.That(buffResult, Is.SameAs(beneficialIcon));

        Assert.That(iconDatabase.TryGetTypeIcon("E_Debuff", effectDatabase, out Sprite debuffResult), Is.True);
        Assert.That(debuffResult, Is.SameAs(harmfulIcon));

        Assert.That(iconDatabase.TryGetTypeIcon("E_Neutral", effectDatabase, out Sprite neutralResult), Is.False);
        Assert.That(neutralResult, Is.Null);
    }

    [Test]
    public void StatusEffectIconPrefab_UsesChildImageForEffectTypeIcon()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/PrefabsR/HUD_Prefab/StatusEffectIcon.prefab");

        Assert.That(prefab, Is.Not.Null);

        StatusEffectIcon statusEffectIcon = prefab.GetComponent<StatusEffectIcon>();
        Assert.That(statusEffectIcon, Is.Not.Null);

        Image typeIconImage = GetPrivateField<Image>(statusEffectIcon, "typeIconImage");
        Assert.That(typeIconImage, Is.Not.Null);
        Assert.That(typeIconImage.gameObject.name, Is.EqualTo("Image"));
    }

    [Test]
    public void StatusEffectIconPrefab_KeepsIconImageForStatusIcon()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/PrefabsR/HUD_Prefab/StatusEffectIcon.prefab");

        Assert.That(prefab, Is.Not.Null);

        StatusEffectIcon statusEffectIcon = prefab.GetComponent<StatusEffectIcon>();
        Assert.That(statusEffectIcon, Is.Not.Null);

        Image statusIconImage = GetPrivateField<Image>(statusEffectIcon, "iconImage");
        Assert.That(statusIconImage, Is.Not.Null);
        Assert.That(statusIconImage.gameObject.name, Is.EqualTo("IconImage"));
    }

    private StatusEffectIconDatabase CreateIconDatabase(
        out Sprite beneficialIcon,
        out Sprite harmfulIcon)
    {
        StatusEffectIconDatabase database = ScriptableObject.CreateInstance<StatusEffectIconDatabase>();
        createdObjects.Add(database);

        beneficialIcon = CreateSprite(Color.green);
        harmfulIcon = CreateSprite(Color.red);

        SetPrivateField(database, "beneficialIcon", beneficialIcon);
        SetPrivateField(database, "harmfulIcon", harmfulIcon);

        return database;
    }

    private Sprite CreateSprite(Color color)
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        createdObjects.Add(texture);

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
        createdObjects.Add(sprite);

        return sprite;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName) where T : Object
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        return field.GetValue(target) as T;
    }
}
