using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LobbyWorldCharacterSpawnerTests
{
    private readonly List<Object> cleanup = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = cleanup.Count - 1; i >= 0; i--)
        {
            if (cleanup[i] != null)
                Object.DestroyImmediate(cleanup[i]);
        }

        cleanup.Clear();
    }

    [Test]
    public void Spawn_SkipsEmptyDuplicateAndMissingEntries_AndAppliesTransform()
    {
        GameObject parentObject = Track(new GameObject("CharacterRoot"));
        GameObject prefab = Track(new GameObject("Char01Prefab"));
        prefab.AddComponent<BoxCollider2D>();
        var definitions = new[]
        {
            new LobbyWorldCharacterSpawnDefinition(
                "Char_01",
                new Vector3(1f, 2f, 3f),
                new Vector3(0f, 0f, 15f),
                new Vector3(0.5f, 0.5f, 1f)),
            new LobbyWorldCharacterSpawnDefinition("Char_01", Vector3.zero, Vector3.zero, Vector3.one),
            new LobbyWorldCharacterSpawnDefinition("", Vector3.zero, Vector3.zero, Vector3.one),
            new LobbyWorldCharacterSpawnDefinition("Char_02", Vector3.zero, Vector3.zero, Vector3.one)
        };

        IReadOnlyList<GameObject> spawned = LobbyWorldCharacterSpawner.Spawn(
            parentObject.transform,
            definitions,
            id => id == "Char_01" ? prefab : null);

        Assert.That(spawned, Has.Count.EqualTo(1));
        Assert.That(spawned[0].name, Is.EqualTo("LobbyWorld_Char_01"));
        Assert.That(spawned[0].transform.parent, Is.SameAs(parentObject.transform));
        Assert.That(spawned[0].transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
        Assert.That(spawned[0].transform.localEulerAngles.z, Is.EqualTo(15f).Within(0.01f));
        Assert.That(spawned[0].transform.localScale, Is.EqualTo(new Vector3(0.5f, 0.5f, 1f)));
        Assert.That(spawned[0].GetComponent<LobbyWorldObjectHoverName>(), Is.Not.Null);

        cleanup.Add(spawned[0]);
    }

    [Test]
    public void ReplaceSpawned_DestroysPreviousInstancesBeforeRespawn()
    {
        GameObject parentObject = Track(new GameObject("CharacterRoot"));
        GameObject prefab = Track(new GameObject("Char01Prefab"));
        var definitions = new[]
        {
            new LobbyWorldCharacterSpawnDefinition("Char_01", Vector3.zero, Vector3.zero, Vector3.one)
        };
        var existing = new List<GameObject>
        {
            Track(Object.Instantiate(prefab, parentObject.transform))
        };

        LobbyWorldCharacterSpawner.ReplaceSpawned(
            parentObject.transform,
            definitions,
            id => prefab,
            existing);

        Assert.That(existing, Has.Count.EqualTo(1));
        Assert.That(parentObject.transform.childCount, Is.EqualTo(1));
        Assert.That(existing[0].name, Is.EqualTo("LobbyWorld_Char_01"));
    }

    [Test]
    public void Spawn_PreservesPrefabAuthoredHoverLightWithoutCreatingAnother()
    {
        GameObject parentObject = Track(new GameObject("CharacterRoot"));
        GameObject prefab = Track(new GameObject("Char01Prefab"));
        prefab.AddComponent<BoxCollider2D>();
        SpriteRenderer front = prefab.AddComponent<SpriteRenderer>();
        GameObject backObject = new GameObject("Back");
        backObject.transform.SetParent(prefab.transform, false);
        SpriteRenderer back = backObject.AddComponent<SpriteRenderer>();
        GameObject lightObject = new GameObject("light");
        lightObject.transform.SetParent(prefab.transform, false);
        Light2D authoredLight = lightObject.AddComponent<Light2D>();
        authoredLight.pointLightOuterRadius = 12.34f;
        authoredLight.falloffIntensity = 0.42f;

        IReadOnlyList<GameObject> spawned = LobbyWorldCharacterSpawner.Spawn(
            parentObject.transform,
            new[] { new LobbyWorldCharacterSpawnDefinition("Char_01", Vector3.zero, Vector3.zero, Vector3.one) },
            _ => prefab);

        Light2D[] lights = spawned[0].GetComponentsInChildren<Light2D>(true);
        Assert.That(lights, Has.Length.EqualTo(1));
        Assert.That(lights[0].pointLightOuterRadius, Is.EqualTo(12.34f).Within(0.0001f));
        Assert.That(lights[0].falloffIntensity, Is.EqualTo(0.42f).Within(0.0001f));
        Assert.That(spawned[0].GetComponent<UIHoverLight2DFalloff>(), Is.Not.Null);
        SpriteRenderer[] renderers = spawned[0].GetComponentsInChildren<SpriteRenderer>(true);
        Assert.That(renderers[0].flipX, Is.True);
        Assert.That(renderers[0].sortingOrder, Is.EqualTo(2));
        Assert.That(renderers[1].sortingOrder, Is.EqualTo(1));

        cleanup.Add(spawned[0]);
    }

    [Test]
    public void BindCharacterChildren_RecognizesRuntimeSpawnedCharacterName()
    {
        GameObject ownerObject = Track(new GameObject("CharacterRoot"));
        LobbyWorldCharacterSettingOpenObject owner = ownerObject.AddComponent<LobbyWorldCharacterSettingOpenObject>();
        GameObject characterObject = new GameObject("LobbyWorld_Char_01");
        characterObject.transform.SetParent(ownerObject.transform, false);
        characterObject.AddComponent<BoxCollider2D>();

        owner.BindCharacterChildren();

        Assert.That(characterObject.GetComponent<LobbyWorldCharacterClickRelay>(), Is.Not.Null);
    }

    private T Track<T>(T value) where T : Object
    {
        cleanup.Add(value);
        return value;
    }
}
