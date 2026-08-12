using System;
using System.Collections.Generic;
using UnityEngine;

public class MapVisualActor : MonoBehaviour
{
    [SerializeField] private string visualObjectId;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform vfxRoot;
    [SerializeField] private List<MapVisualActionEntry> actions = new();

    [Header("World VFX Proxy")]
    [SerializeField] private bool useWorldVfxProxy = true;
    [SerializeField] private string vfxLayerName = "VFX";
    [SerializeField] private BattleVfxProxyBlendMode worldVfxProxyBlendMode = BattleVfxProxyBlendMode.Additive;
    [SerializeField, Min(1)] private int worldVfxRenderTextureWidth = 512;
    [SerializeField, Min(1)] private int worldVfxRenderTextureHeight = 512;
    [SerializeField, Min(0.01f)] private float worldVfxRenderCameraOrthographicSize = 5f;
    [SerializeField, Min(0.01f)] private float worldVfxProxyWorldHeight = 10f;
    [SerializeField] private Vector3 worldVfxProxyWorldOffset = Vector3.zero;
    [SerializeField] private string worldVfxProxySortingLayerName;
    [SerializeField] private int worldVfxProxySortingOrderOffset = 1;
    [SerializeField] private float worldVfxProxySortingWorldYOffset = 0f;
    [SerializeField, Min(0.01f)] private float worldVfxProxyYMultiplier = 100f;

    private string runtimeVisualObjectId;
    private readonly List<BattleWorldVfxHandle> spawnedWorldVfxHandles = new();

    public string VisualObjectId
    {
        get
        {
            string runtimeId = NormalizeId(runtimeVisualObjectId);
            return !string.IsNullOrEmpty(runtimeId)
                ? runtimeId
                : NormalizeId(visualObjectId);
        }
    }

    public void SetRuntimeVisualObjectId(string id)
    {
        runtimeVisualObjectId = NormalizeId(id);
    }

    public bool TryPlayAction(string actionId)
    {
        actionId = NormalizeId(actionId);

        if (string.IsNullOrEmpty(actionId) || actions == null)
            return false;

        bool matched = false;

        for (int i = 0; i < actions.Count; i++)
        {
            MapVisualActionEntry action = actions[i];
            if (action == null || !action.Matches(actionId))
                continue;

            action.Play(this);
            matched = true;
        }

        return matched;
    }

    internal Animator ResolveAnimator()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        return animator;
    }

    internal Transform ResolveVfxRoot()
    {
        return vfxRoot != null ? vfxRoot : transform;
    }

    internal bool TrySpawnWorldVfx(MapVisualActionEntry action, Transform actionRoot)
    {
        if (!useWorldVfxProxy || action == null || action.VfxPrefab == null)
            return false;

        int renderLayer = LayerMask.NameToLayer(vfxLayerName);
        if (renderLayer < 0)
            return false;

        Transform resolvedRoot = actionRoot != null ? actionRoot : ResolveVfxRoot();
        Vector3 worldPosition = resolvedRoot != null
            ? resolvedRoot.TransformPoint(action.VfxLocalPosition)
            : transform.TransformPoint(action.VfxLocalPosition);
        int visibleLayer = ResolveVisibleLayer(resolvedRoot, renderLayer);
        BattleVfxEntry entry = CreateWorldProxyEntry(action);
        float lifeTime = action.VfxLifetime > 0f
            ? action.VfxLifetime
            : float.PositiveInfinity;

        bool spawned = BattleWorldVfxRenderer.TrySpawnDetached(
            entry,
            worldPosition,
            renderLayer,
            visibleLayer,
            lifeTime,
            vfx => ConfigureWorldProxyVfxInstance(vfx, action, renderLayer),
            out BattleWorldVfxHandle handle);

        if (!spawned || handle == null)
            return false;

        spawnedWorldVfxHandles.Add(handle);
        return true;
    }

    private BattleVfxEntry CreateWorldProxyEntry(MapVisualActionEntry action)
    {
        return new BattleVfxEntry
        {
            prefab = action.VfxPrefab,
            renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture,
            proxyBlendMode = worldVfxProxyBlendMode,
            renderTextureWidth = Mathf.Max(1, worldVfxRenderTextureWidth),
            renderTextureHeight = Mathf.Max(1, worldVfxRenderTextureHeight),
            renderCameraOrthographicSize = Mathf.Max(0.01f, worldVfxRenderCameraOrthographicSize),
            proxyWorldHeight = Mathf.Max(0.01f, worldVfxProxyWorldHeight),
            proxyWorldOffset = worldVfxProxyWorldOffset,
            proxySortingLayerName = ResolveWorldVfxSortingLayerName(),
            proxySortingOrderOffset = ResolveWorldVfxSortingOrderOffset(),
            proxySortingWorldYOffset = worldVfxProxySortingWorldYOffset,
            proxyYMultiplier = Mathf.Max(0.01f, worldVfxProxyYMultiplier),
            sfx = new BattleVfxSfxEntry()
        };
    }

    private string ResolveWorldVfxSortingLayerName()
    {
        if (!string.IsNullOrWhiteSpace(worldVfxProxySortingLayerName))
            return worldVfxProxySortingLayerName.Trim();

        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null && !string.IsNullOrWhiteSpace(renderer.sortingLayerName))
            return renderer.sortingLayerName;

        return "Default";
    }

    private int ResolveWorldVfxSortingOrderOffset()
    {
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
        int baseOrder = renderer != null ? renderer.sortingOrder : 0;
        return baseOrder + worldVfxProxySortingOrderOffset;
    }

    private void ConfigureWorldProxyVfxInstance(
        GameObject vfx,
        MapVisualActionEntry action,
        int renderLayer)
    {
        if (vfx == null || action == null)
            return;

        Transform vfxTransform = vfx.transform;
        vfxTransform.localPosition = Vector3.zero;
        vfxTransform.localEulerAngles = action.VfxLocalEulerAngles;
        vfxTransform.localScale = action.VfxLocalScale;
        vfx.SetActive(true);

        SetLayerRecursively(vfx, renderLayer);
        EnsureVfxPauseController(vfx);
        RestartParticles(vfx);
        BattleVfxAudioUtility.PlayAndStripEmbeddedAudioSources(vfx, new BattleVfxSfxEntry(), this);
    }

    private static int ResolveVisibleLayer(Transform source, int renderLayer)
    {
        if (source == null)
            return 0;

        return source.gameObject.layer == renderLayer
            ? 0
            : source.gameObject.layer;
    }

    private static void EnsureVfxPauseController(GameObject vfx)
    {
        if (vfx == null)
            return;

        if (vfx.GetComponent<BattleVfxPlaybackPauseController>() == null)
            vfx.AddComponent<BattleVfxPlaybackPauseController>();
    }

    private static void RestartParticles(GameObject vfx)
    {
        if (vfx == null)
            return;

        ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].Clear(true);
            particles[i].Play(true);
        }
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;

        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
    }

    private void OnDisable()
    {
        CleanupSpawnedWorldVfxHandles();
    }

    private void OnDestroy()
    {
        CleanupSpawnedWorldVfxHandles();
    }

    private void CleanupSpawnedWorldVfxHandles()
    {
        for (int i = spawnedWorldVfxHandles.Count - 1; i >= 0; i--)
        {
            BattleWorldVfxHandle handle = spawnedWorldVfxHandles[i];
            if (handle != null)
                DestroyUnityObject(handle.gameObject);
        }

        spawnedWorldVfxHandles.Clear();
    }

    private static void DestroyUnityObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }

    internal static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}

[Serializable]
public sealed class MapVisualActionEntry
{
    public string ActionId;

    [Header("Animator")]
    public string AnimatorTrigger;
    [Tooltip("Trigger가 없는 Controller에서 선택 시 직접 재생할 상태 이름입니다.")]
    public string AnimatorStateName;

    [Header("VFX")]
    public GameObject VfxPrefab;
    public Transform VfxRootOverride;
    public Vector3 VfxLocalPosition;
    public Vector3 VfxLocalEulerAngles;
    public Vector3 VfxLocalScale = Vector3.one;
    public float VfxLifetime;

    [Header("Sprite")]
    public SpriteRenderer TintTarget;
    public bool ApplyTint;
    public Color TintColor = Color.white;

    [Header("Transform")]
    public Transform ScaleTarget;
    public bool ApplyLocalScale;
    public Vector3 LocalScale = Vector3.one;

    [Header("Activation")]
    public GameObject ActiveTarget;
    public bool ApplyActiveState;
    public bool ActiveState = true;

    public bool Matches(string actionId)
    {
        return string.Equals(
            MapVisualActor.NormalizeId(ActionId),
            MapVisualActor.NormalizeId(actionId),
            StringComparison.Ordinal);
    }

    internal void Play(MapVisualActor owner)
    {
        PlayAnimator(owner);
        SpawnVfx(owner);
        ApplySpriteTint();
        ApplyTransformScale();
        ApplyActivation();
    }

    private void PlayAnimator(MapVisualActor owner)
    {
        if (string.IsNullOrWhiteSpace(AnimatorTrigger) &&
            string.IsNullOrWhiteSpace(AnimatorStateName))
        {
            return;
        }

        Animator resolvedAnimator = owner != null ? owner.ResolveAnimator() : null;
        if (resolvedAnimator == null)
            return;

        if (!resolvedAnimator.enabled)
            resolvedAnimator.enabled = true;

        if (!string.IsNullOrWhiteSpace(AnimatorTrigger))
        {
            resolvedAnimator.SetTrigger(AnimatorTrigger.Trim());
            return;
        }

        if (!string.IsNullOrWhiteSpace(AnimatorStateName))
            resolvedAnimator.Play(AnimatorStateName.Trim(), 0, 0f);
    }

    private void SpawnVfx(MapVisualActor owner)
    {
        if (VfxPrefab == null)
            return;

        Transform parent = VfxRootOverride != null
            ? VfxRootOverride
            : owner != null
                ? owner.ResolveVfxRoot()
                : null;

        if (owner != null && owner.TrySpawnWorldVfx(this, parent))
            return;

        GameObject instance = UnityEngine.Object.Instantiate(VfxPrefab, parent, false);
        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = VfxLocalPosition;
        instanceTransform.localEulerAngles = VfxLocalEulerAngles;
        instanceTransform.localScale = VfxLocalScale;

        if (VfxLifetime > 0f)
            UnityEngine.Object.Destroy(instance, VfxLifetime);
    }

    private void ApplySpriteTint()
    {
        if (!ApplyTint || TintTarget == null)
            return;

        TintTarget.color = TintColor;
    }

    private void ApplyTransformScale()
    {
        if (!ApplyLocalScale || ScaleTarget == null)
            return;

        ScaleTarget.localScale = LocalScale;
    }

    private void ApplyActivation()
    {
        if (!ApplyActiveState || ActiveTarget == null)
            return;

        ActiveTarget.SetActive(ActiveState);
    }
}
