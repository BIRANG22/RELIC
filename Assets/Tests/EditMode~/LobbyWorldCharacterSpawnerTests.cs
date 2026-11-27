using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LobbyWorldCharacterSpawnerTests
{
    private readonly List<Object> cleanup = new();
    [TearDown] public void TearDown()
    {
        for (int i = cleanup.Count - 1; i >= 0; i--)
            if (cleanup[i] != null) Object.DestroyImmediate(cleanup[i]);
        cleanup.Clear();
    }

    [Test] public void Spawn_InheritsScenePointTransform_AndDoesNotDuplicate()
    {
        var point = CreatePoint(out var prefab);
        point.transform.position = new Vector3(3, -4, 7);
        point.transform.rotation = Quaternion.Euler(0, 0, 25);
        point.transform.localScale = new Vector3(-0.5f, 0.5f, 0.5f);
        prefab.transform.localScale = Vector3.one * 9;
        var instance = point.Spawn();
        Assert.That(instance.transform.parent, Is.SameAs(point.transform));
        Assert.That(instance.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(instance.transform.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(instance.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(instance.transform.position, Is.EqualTo(point.transform.position));
        Assert.That(point.Spawn(), Is.SameAs(instance));
        Assert.That(point.transform.childCount, Is.EqualTo(1));
    }

    [TestCase(false)] [TestCase(true)]
    public void Spawn_AppliesInspectorPresentation_AndLayerScope(bool children)
    {
        var point = CreatePoint(out var prefab);
        Set(point, "flipX", false);
        Set(point, "sortingLayerId", SortingLayer.NameToID("Unit"));
        Set(point, "frontSortingOrder", 17);
        Set(point, "backSortingOrder", -3);
        Set(point, "overrideLayer", true);
        Set(point, "objectLayer", 8);
        Set(point, "applyLayerToChildren", children);
        var instance = point.Spawn();
        var sprites = instance.GetComponentsInChildren<SpriteRenderer>(true);
        Assert.That(sprites[0].flipX, Is.False);
        Assert.That(sprites[0].sortingOrder, Is.EqualTo(17));
        Assert.That(sprites[1].sortingOrder, Is.EqualTo(-3));
        Assert.That(sprites[0].sortingLayerID, Is.EqualTo(SortingLayer.NameToID("Unit")));
        Assert.That(sprites[1].sortingLayerID, Is.EqualTo(sprites[0].sortingLayerID));
        Assert.That(instance.layer, Is.EqualTo(8));
        Assert.That(sprites[1].gameObject.layer, Is.EqualTo(children ? 8 : 9));
        Assert.That(prefab.GetComponent<SpriteRenderer>().flipX, Is.True);
        Assert.That(prefab.layer, Is.EqualTo(7));
    }

    [Test] public void Spawn_PreservesPrefabLayersAndLight_AndBindsStableId()
    {
        var point = CreatePoint(out var prefab);
        var ownerObject = Track(new GameObject("Owner"));
        var owner = ownerObject.AddComponent<LobbyWorldCharacterSettingOpenObject>();
        point.Initialize(owner);
        var instance = point.Spawn();
        var light = instance.GetComponentInChildren<Light2D>(true);
        Assert.That(instance.layer, Is.EqualTo(7));
        Assert.That(instance.GetComponentsInChildren<Light2D>(true), Has.Length.EqualTo(1));
        Assert.That(light.pointLightOuterRadius, Is.EqualTo(12.34f).Within(0.001f));
        Assert.That(instance.GetComponent<UIHoverLight2DFalloff>(), Is.Not.Null);
        Assert.That(instance.GetComponent<LobbyWorldObjectHoverName>(), Is.Not.Null);
        var relay = instance.GetComponent<LobbyWorldCharacterClickRelay>();
        Assert.That(relay, Is.Not.Null);
        Assert.That(relay.CharacterId, Is.EqualTo("Char_01"));
    }

    [Test] public void Spawn_SkipsInactiveMissingDatabaseIdAndPrefab()
    {
        var point = CreatePoint(out _);
        point.gameObject.SetActive(false);
        Assert.That(point.Spawn(), Is.Null);
        point.gameObject.SetActive(true);
        Set(point, "characterId", "Unknown");
        Assert.That(point.Spawn(), Is.Null);
        Set(point, "characterId", " ");
        Assert.That(point.Spawn(), Is.Null);
        Set(point, "database", null);
        Assert.That(point.Spawn(), Is.Null);
        Assert.That(point.transform.childCount, Is.Zero);
    }

    [Test] public void Initialize_InactivePointSpawnsWhenEnabled_WithoutDuplicates()
    {
        var point = CreatePoint(out _);
        point.gameObject.SetActive(false);
        var owner = Track(new GameObject("Owner")).AddComponent<LobbyWorldCharacterSettingOpenObject>();
        point.Initialize(owner);
        Assert.That(point.transform.childCount, Is.Zero);
        point.gameObject.SetActive(true);
        Assert.That(point.transform.childCount, Is.EqualTo(1));
        var instance = point.Spawn();
        point.gameObject.SetActive(false);
        point.gameObject.SetActive(true);
        Assert.That(point.Spawn(), Is.SameAs(instance));
        Assert.That(instance.GetComponent<LobbyWorldCharacterClickRelay>().CharacterId, Is.EqualTo("Char_01"));
    }

    [Test] public void Spawn_MissingLobbyPrefabDoesNotCreateChild()
    {
        var point = CreatePoint(out var prefab);
        Object.DestroyImmediate(prefab);
        Assert.That(point.Spawn(), Is.Null);
        Assert.That(point.transform.childCount, Is.Zero);
    }

    [Test] public void Spawn_SameCharacterAtTwoPointsIsIndependent()
    {
        var first = CreatePoint(out _);
        var second = CreatePoint(out _);
        Assert.That(first.Spawn(), Is.Not.SameAs(second.Spawn()));
        Assert.That(first.transform.childCount, Is.EqualTo(1));
        Assert.That(second.transform.childCount, Is.EqualTo(1));
    }

    private LobbyCharacterSpawnPoint CreatePoint(out GameObject prefab)
    {
        prefab = Track(new GameObject("UnrelatedPrefabName"));
        prefab.layer = 7;
        prefab.AddComponent<BoxCollider2D>();
        prefab.AddComponent<SpriteRenderer>().flipX = true;
        var back = new GameObject("Back");
        back.transform.SetParent(prefab.transform, false);
        back.layer = 9;
        back.AddComponent<SpriteRenderer>();
        var light = new GameObject("AuthoredLight");
        light.transform.SetParent(prefab.transform, false);
        light.AddComponent<Light2D>().pointLightOuterRadius = 12.34f;
        var database = Track(ScriptableObject.CreateInstance<CharacterPrefabDatabase>());
        Set(database, "entries", new List<CharacterPrefabEntry> {
            new CharacterPrefabEntry { CharacterId = "Char_01", LobbyPrefab = prefab }
        });
        var point = Track(new GameObject("ArbitrarySpawnPoint")).AddComponent<LobbyCharacterSpawnPoint>();
        Set(point, "database", database);
        Set(point, "characterId", "Char_01");
        return point;
    }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private T Track<T>(T value) where T : Object { cleanup.Add(value); return value; }
}
