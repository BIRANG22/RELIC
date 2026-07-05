using System.Collections.Generic;
using UnityEngine;

public struct GridEffectWorldVfxSpawnContext
{
    public Vector3 WorldPosition;
    public int RenderLayer;
    public int VisibleLayer;
    public string SortingLayerName;
    public int SortingOrderOffset;
    public float YMultiplier;
    public float LifeTime;

    public GridEffectWorldVfxSpawnContext(
        Vector3 worldPosition,
        int renderLayer,
        int visibleLayer,
        string sortingLayerName,
        int sortingOrderOffset,
        float yMultiplier,
        float lifeTime)
    {
        WorldPosition = worldPosition;
        RenderLayer = renderLayer;
        VisibleLayer = visibleLayer;
        SortingLayerName = sortingLayerName;
        SortingOrderOffset = sortingOrderOffset;
        YMultiplier = yMultiplier;
        LifeTime = lifeTime;
    }
}

public class GridEffectWorldVfxPresenter : MonoBehaviour
{
    private const float DefaultPersistentLifeTime = 9999f;

    [SerializeField] private BattleVfxEntry[] vfxEntries;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private float lifeTime = DefaultPersistentLifeTime;
    [SerializeField] private bool hideSceneSourceObjects = true;

    private readonly List<BattleWorldVfxHandle> spawnedHandles = new();

    public void Play(GridEffectWorldVfxSpawnContext context)
    {
        CleanupSpawnedVfx();

        if (context.RenderLayer < 0 || vfxEntries == null || vfxEntries.Length == 0)
            return;

        List<BattleVfxEntry> runtimeEntries = CreateRuntimeEntries(context);
        PrepareSourceObjectsForProxy();

        Vector3 position = context.WorldPosition + worldOffset;
        float resolvedLifeTime = ResolveLifeTime(context.LifeTime);

        for (int i = 0; i < runtimeEntries.Count; i++)
        {
            BattleVfxEntry entry = runtimeEntries[i];

            if (!BattleWorldVfxRenderer.TrySpawnDetached(
                    entry,
                    position,
                    context.RenderLayer,
                    context.VisibleLayer,
                    resolvedLifeTime,
                    vfx => ConfigureVfxInstance(vfx, entry, context.RenderLayer),
                    out BattleWorldVfxHandle handle))
            {
                continue;
            }

            if (handle != null)
                spawnedHandles.Add(handle);
        }
    }

    public void PrepareSourceObjectsForProxy()
    {
        if (!hideSceneSourceObjects || vfxEntries == null)
            return;

        for (int i = 0; i < vfxEntries.Length; i++)
        {
            BattleVfxEntry entry = vfxEntries[i];

            if (entry == null || !ShouldHideSourceObject(entry.prefab))
                continue;

            entry.prefab.SetActive(false);
        }
    }

    public void CleanupSpawnedVfx()
    {
        for (int i = spawnedHandles.Count - 1; i >= 0; i--)
        {
            BattleWorldVfxHandle handle = spawnedHandles[i];

            if (handle == null)
                continue;

            DestroyUnityObject(handle.gameObject);
        }

        spawnedHandles.Clear();
    }

    private void OnDisable()
    {
        CleanupSpawnedVfx();
    }

    private void OnDestroy()
    {
        CleanupSpawnedVfx();
    }

    private List<BattleVfxEntry> CreateRuntimeEntries(GridEffectWorldVfxSpawnContext context)
    {
        List<BattleVfxEntry> runtimeEntries = new(vfxEntries.Length);

        for (int i = 0; i < vfxEntries.Length; i++)
        {
            BattleVfxEntry entry = vfxEntries[i];

            if (entry == null || entry.prefab == null)
                continue;

            runtimeEntries.Add(CreateRuntimeEntry(entry, context));
        }

        return runtimeEntries;
    }

    private BattleVfxEntry CreateRuntimeEntry(
        BattleVfxEntry source,
        GridEffectWorldVfxSpawnContext context)
    {
        return new BattleVfxEntry
        {
            prefab = source.prefab,
            flipType = source.flipType,
            sfx = BattleVfxSfxEntry.CopyFrom(source.sfx),
            renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture,
            renderTextureWidth = source.renderTextureWidth,
            renderTextureHeight = source.renderTextureHeight,
            renderCameraOrthographicSize = source.renderCameraOrthographicSize,
            proxyWorldHeight = source.proxyWorldHeight,
            proxyWorldOffset = source.proxyWorldOffset,
            proxySortingLayerName = ResolveSortingLayerName(source, context),
            proxySortingOrderOffset = source.proxySortingOrderOffset + context.SortingOrderOffset,
            proxySortingWorldYOffset = source.proxySortingWorldYOffset,
            proxyYMultiplier = ResolveYMultiplier(source, context)
        };
    }

    private static string ResolveSortingLayerName(
        BattleVfxEntry entry,
        GridEffectWorldVfxSpawnContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.SortingLayerName))
            return context.SortingLayerName;

        return entry.proxySortingLayerName;
    }

    private static float ResolveYMultiplier(
        BattleVfxEntry entry,
        GridEffectWorldVfxSpawnContext context)
    {
        if (context.YMultiplier > 0f)
            return context.YMultiplier;

        return entry.proxyYMultiplier;
    }

    private float ResolveLifeTime(float contextLifeTime)
    {
        float resolvedLifeTime = lifeTime > 0f ? lifeTime : contextLifeTime;
        return Mathf.Max(0.01f, resolvedLifeTime);
    }

    private bool ShouldHideSourceObject(GameObject source)
    {
        if (source == null)
            return false;

        Transform sourceTransform = source.transform;

        if (sourceTransform == transform)
            return false;

        return sourceTransform.IsChildOf(transform);
    }

    private void ConfigureVfxInstance(
        GameObject vfx,
        BattleVfxEntry entry,
        int renderLayer)
    {
        if (vfx == null)
            return;

        vfx.SetActive(true);

        if (renderLayer >= 0)
            SetLayerRecursively(vfx, renderLayer);

        ApplyVfxFlip(vfx, entry.flipType);
        BattleVfxAudioUtility.PlayAndStripEmbeddedAudioSources(vfx, entry.sfx, this);
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void ApplyVfxFlip(GameObject vfx, VfxFlipType flipType)
    {
        switch (flipType)
        {
            case VfxFlipType.None:
                break;

            case VfxFlipType.RotationY180:
                AddLocalRotationY(vfx.transform, 180f);
                break;

            case VfxFlipType.ParticleRendererFlipY:
                FlipParticleRendererY(vfx);
                break;
        }
    }

    private static void AddLocalRotationY(Transform target, float amount)
    {
        Vector3 euler = target.localEulerAngles;
        euler.y += amount;
        target.localEulerAngles = euler;
    }

    private static void FlipParticleRendererY(GameObject vfx)
    {
        ParticleSystemRenderer[] renderers =
            vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Vector3 flip = renderers[i].flip;
            flip.y = 1f - flip.y;
            renderers[i].flip = flip;
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
