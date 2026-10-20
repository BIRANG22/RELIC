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
        GetRendererPresentation(
            characterId,
            out bool flipX,
            out int frontSortingOrder,
            out int backSortingOrder);

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

        if (hoverCollider == null)
            return;

        Light2D light = instance.GetComponentInChildren<Light2D>(true);
        if (light == null)
            return;

        UIHoverLight2DFalloff hoverLight = hoverCollider.GetComponent<UIHoverLight2DFalloff>();
        if (hoverLight == null)
            hoverLight = hoverCollider.gameObject.AddComponent<UIHoverLight2DFalloff>();

        hoverLight.Configure(light);
    }

    private static void GetRendererPresentation(
        string characterId,
        out bool flipX,
        out int frontSortingOrder,
        out int backSortingOrder)
    {
        flipX = characterId != "Char_02";
        backSortingOrder = characterId == "Char_03" ? 2 : 1;
        frontSortingOrder = characterId switch
        {
            "Char_02" => 5,
            "Char_03" => 3,
            _ => 2
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
