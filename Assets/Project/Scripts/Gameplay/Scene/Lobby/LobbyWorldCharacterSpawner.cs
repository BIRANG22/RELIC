using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public sealed class LobbyWorldCharacterSpawnDefinition
{
    [SerializeField] private string characterId;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [SerializeField] private LobbyWorldObjectHoverName.HoverNameType hoverNameType;

    public string CharacterId => characterId;
    public Vector3 LocalPosition => localPosition;
    public Vector3 LocalEulerAngles => localEulerAngles;
    public Vector3 LocalScale => localScale;
    public LobbyWorldObjectHoverName.HoverNameType HoverNameType => hoverNameType;

    public LobbyWorldCharacterSpawnDefinition(
        string characterId,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale)
    {
        this.characterId = characterId;
        this.localPosition = localPosition;
        this.localEulerAngles = localEulerAngles;
        this.localScale = localScale;
        hoverNameType = ResolveHoverNameType(characterId);
    }

    private static LobbyWorldObjectHoverName.HoverNameType ResolveHoverNameType(string id)
    {
        return id switch
        {
            "Char_01" => LobbyWorldObjectHoverName.HoverNameType.Hilt,
            "Char_02" => LobbyWorldObjectHoverName.HoverNameType.Kaya,
            "Char_03" => LobbyWorldObjectHoverName.HoverNameType.Haze,
            "Char_04" => LobbyWorldObjectHoverName.HoverNameType.Ines,
            _ => LobbyWorldObjectHoverName.HoverNameType.Research
        };
    }
}

public static class LobbyWorldCharacterSpawner
{
    public static IReadOnlyList<GameObject> Spawn(
        Transform parent,
        IReadOnlyList<LobbyWorldCharacterSpawnDefinition> definitions,
        Func<string, GameObject> prefabResolver)
    {
        var result = new List<GameObject>();

        if (parent == null || definitions == null || prefabResolver == null)
            return result;

        var spawnedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < definitions.Count; i++)
        {
            LobbyWorldCharacterSpawnDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.CharacterId))
                continue;

            string characterId = definition.CharacterId.Trim();
            if (!spawnedIds.Add(characterId))
                continue;

            GameObject prefab = prefabResolver(characterId);
            if (prefab == null)
                continue;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.name = "LobbyWorld_" + characterId;
            instance.transform.localPosition = definition.LocalPosition;
            instance.transform.localRotation = Quaternion.Euler(definition.LocalEulerAngles);
            instance.transform.localScale = definition.LocalScale;
            instance.SetActive(true);

            Collider2D hoverCollider = instance.GetComponent<Collider2D>()
                ?? instance.GetComponentInChildren<Collider2D>(true);
            if (hoverCollider != null)
            {
                LobbyWorldObjectHoverName hoverName = hoverCollider.GetComponent<LobbyWorldObjectHoverName>();
                if (hoverName == null)
                    hoverName = hoverCollider.gameObject.AddComponent<LobbyWorldObjectHoverName>();

                hoverName.Configure(definition.HoverNameType);
            }

            RestoreLobbyPresentation(instance, characterId, hoverCollider);

            result.Add(instance);
        }

        return result;
    }

    private static void RestoreLobbyPresentation(
        GameObject instance,
        string characterId,
        Collider2D hoverCollider)
    {
        GetPresentation(
            characterId,
            out bool flipX,
            out int frontSortingOrder,
            out int backSortingOrder,
            out Vector3 lightPosition,
            out float lightRadius,
            out float lightOuterAngle,
            out float falloffIntensity);

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length > 0)
        {
            renderers[0].flipX = flipX;
            renderers[0].sortingLayerID = -2107577011;
            renderers[0].sortingOrder = frontSortingOrder;
        }

        if (renderers.Length > 1)
        {
            renderers[1].sortingLayerID = -2107577011;
            renderers[1].sortingOrder = backSortingOrder;
        }

        var lightObject = new GameObject("light");
        lightObject.SetActive(false);
        lightObject.transform.SetParent(instance.transform, false);
        lightObject.transform.localPosition = lightPosition;
        lightObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        lightObject.transform.localScale = Vector3.one * 1.6539199f;

        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        light.intensity = 2f;
        light.falloffIntensity = falloffIntensity;
        light.pointLightInnerAngle = 0f;
        light.pointLightOuterRadius = lightRadius;
        light.pointLightOuterAngle = lightOuterAngle;
        light.volumeIntensity = 0.1f;
        light.volumetricEnabled = true;
        light.targetSortingLayers = characterId == "Char_01" || characterId == "Char_02"
            ? new[] { unchecked((int)3953243251u), unchecked((int)2187390285u) }
            : new[]
            {
                unchecked((int)3953243251u),
                unchecked((int)2187390285u),
                735699439,
                0,
                1597553239,
                unchecked((int)4242771651u)
            };

        DBAudioSource audioSource = lightObject.AddComponent<DBAudioSource>();
        audioSource.SetSoundId("ui.statue.hover");
        audioSource.SetVolume(1f);
        lightObject.SetActive(true);

        if (hoverCollider == null)
            return;

        UIHoverLight2DFalloff hoverLight = hoverCollider.GetComponent<UIHoverLight2DFalloff>();
        if (hoverLight == null)
            hoverLight = hoverCollider.gameObject.AddComponent<UIHoverLight2DFalloff>();

        hoverLight.Configure(light);
    }

    private static void GetPresentation(
        string characterId,
        out bool flipX,
        out int frontSortingOrder,
        out int backSortingOrder,
        out Vector3 lightPosition,
        out float lightRadius,
        out float lightOuterAngle,
        out float falloffIntensity)
    {
        flipX = characterId != "Char_02";
        backSortingOrder = characterId == "Char_03" ? 2 : 1;
        frontSortingOrder = characterId switch
        {
            "Char_02" => 5,
            "Char_03" => 3,
            _ => 2
        };

        lightPosition = characterId switch
        {
            "Char_01" => new Vector3(0.3f, 22.6f, 0f),
            "Char_02" => new Vector3(0f, 30.98f, 0f),
            "Char_03" => new Vector3(0f, 31.4f, 0f),
            "Char_04" => new Vector3(0.2f, 30.6f, 0f),
            _ => Vector3.zero
        };

        lightRadius = characterId switch
        {
            "Char_01" => 11.5f,
            "Char_02" => 15.55f,
            _ => 15.8f
        };

        lightOuterAngle = characterId == "Char_01" ? 10.46311f : 7.434711f;

        falloffIntensity = characterId switch
        {
            "Char_01" => 0.044f,
            "Char_02" => 0.058f,
            "Char_03" => 0.024f,
            "Char_04" => 0.031f,
            _ => 1f
        };
    }

    public static void ReplaceSpawned(
        Transform parent,
        IReadOnlyList<LobbyWorldCharacterSpawnDefinition> definitions,
        Func<string, GameObject> prefabResolver,
        List<GameObject> spawnedInstances)
    {
        if (spawnedInstances == null)
            return;

        for (int i = spawnedInstances.Count - 1; i >= 0; i--)
        {
            GameObject instance = spawnedInstances[i];
            if (instance == null)
                continue;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(instance);
            else
                UnityEngine.Object.DestroyImmediate(instance);
        }

        spawnedInstances.Clear();
        spawnedInstances.AddRange(Spawn(parent, definitions, prefabResolver));
    }
}
