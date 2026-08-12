using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class MapVisualController : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private MapVisualDatabase databaseOverride;

    [Header("Spawn Roots")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform[] anchors;

    [Header("Missing Entry")]
    [SerializeField] private bool clearWhenNoEntry = true;

    private readonly List<GameObject> spawnedObjects = new();
    private readonly Dictionary<string, MapVisualActor> actorsById = new();

    public void ApplyMapVisual(string mapId)
    {
        MapVisualDatabase database = ResolveDatabase();

        if (database == null || !database.TryGetEntry(mapId, out MapVisualEntry entry))
        {
            if (clearWhenNoEntry)
                ClearVisuals();

            return;
        }

        ClearVisuals();
        SpawnEntry(entry);
    }

    public void ClearVisuals()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            DestroySpawnedObject(spawnedObjects[i]);

        spawnedObjects.Clear();
        actorsById.Clear();
    }

    public bool TryPlayAction(string visualObjectId, string actionId)
    {
        visualObjectId = NormalizeId(visualObjectId);
        actionId = NormalizeId(actionId);

        if (string.IsNullOrEmpty(visualObjectId) || string.IsNullOrEmpty(actionId))
            return false;

        return actorsById.TryGetValue(visualObjectId, out MapVisualActor actor) &&
               actor != null &&
               actor.TryPlayAction(actionId);
    }

    private void OnDisable()
    {
        ClearVisuals();
    }

    private MapVisualDatabase ResolveDatabase()
    {
        if (databaseOverride != null)
            return databaseOverride;

        return DataManager.Instance != null
            ? DataManager.Instance.MapVisualDatabase
            : null;
    }

    private void SpawnEntry(MapVisualEntry entry)
    {
        if (entry?.Spawns == null)
            return;

        for (int i = 0; i < entry.Spawns.Count; i++)
            Spawn(entry.Spawns[i]);
    }

    private void Spawn(MapVisualSpawnEntry spawn)
    {
        if (spawn == null || !spawn.Active || spawn.Prefab == null)
            return;

        Transform parent = ResolveAnchor(spawn.AnchorName);
        GameObject instance = Instantiate(spawn.Prefab, parent, false);
        instance.name = spawn.Prefab.name;

        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = spawn.LocalPosition;
        instanceTransform.localEulerAngles = spawn.LocalEulerAngles;
        instanceTransform.localScale = spawn.LocalScale;

        spawnedObjects.Add(instance);
        RegisterActors(instance, spawn.VisualObjectId);
    }

    private void RegisterActors(GameObject instance, string visualObjectIdOverride)
    {
        if (instance == null)
            return;

        MapVisualActor[] actors = instance.GetComponentsInChildren<MapVisualActor>(true);
        for (int i = 0; i < actors.Length; i++)
            RegisterActor(actors[i], visualObjectIdOverride);
    }

    private void RegisterActor(MapVisualActor actor, string visualObjectIdOverride)
    {
        if (actor == null)
            return;

        string overrideId = NormalizeId(visualObjectIdOverride);
        if (!string.IsNullOrEmpty(overrideId))
            actor.SetRuntimeVisualObjectId(overrideId);

        string actorId = NormalizeId(actor.VisualObjectId);
        if (string.IsNullOrEmpty(actorId))
            return;

        if (actorsById.ContainsKey(actorId))
        {
            Debug.LogWarning($"[MapVisualController] Duplicate VisualObjectId: {actorId}", actor);
            return;
        }

        actorsById.Add(actorId, actor);
    }

    private Transform ResolveAnchor(string anchorName)
    {
        if (!string.IsNullOrWhiteSpace(anchorName) && anchors != null)
        {
            string normalizedAnchorName = anchorName.Trim();

            for (int i = 0; i < anchors.Length; i++)
            {
                Transform anchor = anchors[i];
                if (anchor != null && anchor.name == normalizedAnchorName)
                    return anchor;
            }
        }

        return visualRoot != null ? visualRoot : transform;
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private static void DestroySpawnedObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
