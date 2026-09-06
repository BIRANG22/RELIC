using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class BattleWorldVfxRenderer : MonoBehaviour
{
    private const string RootName = "__BattleWorldVfxRenderer";
    private const string RenderRootName = "RenderSpace";
    private const string ProxyRootName = "WorldProxies";
    private const string AdditiveProxyMaterialResourcePath = "BattleWorldVfxProxyMaterial";
    private const string AlphaProxyMaterialResourcePath = "BattleWorldVfxProxyAlphaMaterial";
    private const int VfxRendererIndex = 1;
    private const float RenderSpaceOriginX = 10000f;
    private const float RenderSlotSpacing = 1000f;
    private const float CameraDistance = 10f;

    private static BattleWorldVfxRenderer instance;

    private Transform renderRoot;
    private Transform proxyRoot;
    private Material additiveProxyMaterialTemplate;
    private Material alphaProxyMaterialTemplate;
    private int nextSlot;
    private bool warnedMissingProxyMaterialTemplate;

    public static bool TrySpawn(
        BattleVfxEntry entry,
        Transform followTarget,
        int renderLayer,
        float lifeTime,
        Action<GameObject> configureVfx)
    {
        return TrySpawn(
            entry,
            followTarget,
            renderLayer,
            lifeTime,
            configureVfx,
            out _);
    }

    public static bool TrySpawn(
        BattleVfxEntry entry,
        Transform followTarget,
        int renderLayer,
        float lifeTime,
        Action<GameObject> configureVfx,
        out BattleWorldVfxHandle handle)
    {
        if (entry == null || entry.prefab == null)
        {
            handle = null;
            return false;
        }

        if (entry.renderMode != BattleVfxRenderMode.IndividualWorldRenderTexture)
        {
            handle = null;
            return false;
        }

        if (renderLayer < 0)
        {
            handle = null;
            return false;
        }

        BattleWorldVfxRenderer renderer = EnsureInstance();
        if (renderer == null)
        {
            handle = null;
            return false;
        }

        return renderer.SpawnInternal(
            entry,
            followTarget,
            followTarget != null ? followTarget.position : Vector3.zero,
            renderLayer,
            ResolveVisibleLayer(followTarget, renderLayer),
            lifeTime,
            configureVfx,
            out handle) != null;
    }

    public static bool TrySpawnDetached(
        BattleVfxEntry entry,
        Vector3 worldPosition,
        int renderLayer,
        int visibleLayer,
        float lifeTime,
        Action<GameObject> configureVfx,
        out BattleWorldVfxHandle handle)
    {
        handle = null;

        if (entry == null || entry.prefab == null)
            return false;

        if (entry.renderMode != BattleVfxRenderMode.IndividualWorldRenderTexture)
            return false;

        if (renderLayer < 0)
            return false;

        BattleWorldVfxRenderer renderer = EnsureInstance();
        if (renderer == null)
            return false;

        return renderer.SpawnInternal(
            entry,
            null,
            worldPosition,
            renderLayer,
            ResolveVisibleLayer(visibleLayer, renderLayer),
            lifeTime,
            configureVfx,
            out handle) != null;
    }

    private static BattleWorldVfxRenderer EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject existing = GameObject.Find(RootName);
        if (existing != null && existing.TryGetComponent(out BattleWorldVfxRenderer found))
        {
            instance = found;
            instance.EnsureRoots();
            return instance;
        }

        GameObject root = new(RootName);
        instance = root.AddComponent<BattleWorldVfxRenderer>();
        instance.EnsureRoots();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureRoots();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private BattleWorldVfxHandle SpawnInternal(
        BattleVfxEntry entry,
        Transform followTarget,
        Vector3 initialWorldPosition,
        int renderLayer,
        int visibleLayer,
        float lifeTime,
        Action<GameObject> configureVfx,
        out BattleWorldVfxHandle handle)
    {
        handle = null;

        EnsureRoots();

        RenderTexture renderTexture = null;
        GameObject renderGroup = null;
        Material material = null;
        GameObject proxy = null;

        try
        {
            renderTexture = CreateRenderTexture(entry);
            renderGroup = CreateRenderGroup();
            GameObject vfx = Instantiate(entry.prefab, renderGroup.transform, false);
            configureVfx?.Invoke(vfx);

            CreateRenderCamera(renderGroup.transform, renderTexture, renderLayer, entry);

            material = CreateProxyMaterial(entry, renderTexture);

            if (material == null)
            {
                CleanupFailedSpawn(renderGroup, proxy, renderTexture, material);
                return null;
            }

            proxy = CreateProxy(entry, material, visibleLayer, initialWorldPosition);
            TryCopyHoverColliderToProxy(vfx, renderGroup.transform, proxy, entry);
            MeshRenderer proxyRenderer = proxy.GetComponent<MeshRenderer>();

            handle = proxy.AddComponent<BattleWorldVfxHandle>();
            handle.Initialize(
                followTarget,
                entry.proxyWorldOffset,
                proxyRenderer,
                entry.proxySortingOrderOffset,
                entry.proxySortingWorldYOffset,
                entry.proxyYMultiplier,
                renderGroup,
                renderTexture,
                material);

            StartCoroutine(handle.DestroyAfter(lifeTime));
            return handle;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[BattleWorldVfxRenderer] Individual VFX proxy spawn failed. Prefab:{entry.prefab.name}\n{exception}");

            handle = null;
            CleanupFailedSpawn(renderGroup, proxy, renderTexture, material);
            return null;
        }
    }

    private Material CreateProxyMaterial(BattleVfxEntry entry, RenderTexture renderTexture)
    {
        Material material = null;
        Material template = LoadProxyMaterialTemplate(entry.proxyBlendMode);

        if (template != null)
            material = new Material(template);

        if (material == null)
            return null;

        string prefabName = entry?.prefab != null ? entry.prefab.name : "VFX";
        material.name = $"{prefabName}_WorldVfxProxy_Material";
        material.mainTexture = renderTexture;
        return material;
    }

    private static void CleanupFailedSpawn(
        GameObject renderGroup,
        GameObject proxy,
        RenderTexture renderTexture,
        Material material)
    {
        if (renderTexture != null)
            renderTexture.Release();

        DestroyUnityObject(proxy);
        DestroyUnityObject(renderGroup);
        DestroyUnityObject(material);
        DestroyUnityObject(renderTexture);
    }

    private static void DestroyUnityObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private void EnsureRoots()
    {
        if (renderRoot == null)
        {
            GameObject renderRootObject = new(RenderRootName);
            renderRootObject.transform.SetParent(transform, false);
            renderRoot = renderRootObject.transform;
        }

        if (proxyRoot == null)
        {
            GameObject proxyRootObject = new(ProxyRootName);
            proxyRootObject.transform.SetParent(transform, false);
            proxyRoot = proxyRootObject.transform;
        }
    }

    private Material LoadProxyMaterialTemplate(BattleVfxProxyBlendMode blendMode)
    {
        bool useAlpha = blendMode == BattleVfxProxyBlendMode.Alpha;
        Material cachedTemplate = useAlpha
            ? alphaProxyMaterialTemplate
            : additiveProxyMaterialTemplate;

        if (cachedTemplate != null)
            return cachedTemplate;

        string resourcePath = useAlpha
            ? AlphaProxyMaterialResourcePath
            : AdditiveProxyMaterialResourcePath;

        Material loadedTemplate = Resources.Load<Material>(resourcePath);

        if (useAlpha)
            alphaProxyMaterialTemplate = loadedTemplate;
        else
            additiveProxyMaterialTemplate = loadedTemplate;

        if (loadedTemplate != null)
            return loadedTemplate;

        if (!warnedMissingProxyMaterialTemplate)
        {
            warnedMissingProxyMaterialTemplate = true;
            Debug.LogWarning(
                $"[BattleWorldVfxRenderer] Missing Resources material: {resourcePath}");
        }

        return null;
    }

    private RenderTexture CreateRenderTexture(BattleVfxEntry entry)
    {
        int width = Mathf.Max(1, entry.renderTextureWidth);
        int height = Mathf.Max(1, entry.renderTextureHeight);

        RenderTexture renderTexture = new(width, height, 16, RenderTextureFormat.ARGB32)
        {
            name = $"{entry.prefab.name}_WorldVfxRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };

        renderTexture.Create();
        return renderTexture;
    }

    private GameObject CreateRenderGroup()
    {
        GameObject renderGroup = new($"WorldVfxRender_{nextSlot:000}");
        renderGroup.transform.SetParent(renderRoot, false);
        renderGroup.transform.localPosition = new Vector3(
            RenderSpaceOriginX + nextSlot * RenderSlotSpacing,
            0f,
            0f);
        nextSlot++;
        return renderGroup;
    }

    private void CreateRenderCamera(
        Transform parent,
        RenderTexture renderTexture,
        int renderLayer,
        BattleVfxEntry entry)
    {
        GameObject cameraObject = new("Camera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
        cameraObject.transform.localRotation = Quaternion.identity;

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(0.01f, entry.renderCameraOrthographicSize);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = CameraDistance * 4f;
        camera.cullingMask = renderLayer >= 0 ? 1 << renderLayer : 0;
        camera.targetTexture = renderTexture;
        camera.allowHDR = true;
        camera.allowMSAA = false;

        UniversalAdditionalCameraData cameraData =
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.SetRenderer(VfxRendererIndex);
    }

    private GameObject CreateProxy(
        BattleVfxEntry entry,
        Material material,
        int visibleLayer,
        Vector3 initialWorldPosition)
    {
        GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Quad);
        proxy.name = $"{entry.prefab.name}_WorldVfxProxy";
        proxy.transform.SetParent(proxyRoot, true);
        proxy.transform.position = initialWorldPosition + entry.proxyWorldOffset;
        proxy.layer = visibleLayer;

        Collider collider = proxy.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        float height = Mathf.Max(0.01f, entry.proxyWorldHeight);
        float width = height * Mathf.Max(1, entry.renderTextureWidth) /
                      Mathf.Max(1f, entry.renderTextureHeight);
        proxy.transform.localScale = new Vector3(width, height, 1f);

        MeshRenderer meshRenderer = proxy.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        if (!string.IsNullOrWhiteSpace(entry.proxySortingLayerName))
            meshRenderer.sortingLayerName = entry.proxySortingLayerName;

        meshRenderer.sortingOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(
            proxy.transform.position.y + entry.proxySortingWorldYOffset,
            entry.proxyYMultiplier,
            entry.proxySortingOrderOffset);

        return proxy;
    }

    /// <summary>
    /// IndividualWorldRenderTexture VFX는 실제 프리팹과 월드에 표시되는 Proxy가 서로 다른 오브젝트입니다.
    /// 원본 VFX 프리팹의 BoxCollider2D 영역을 Proxy 아래의 별도 HoverCollider 자식에 복사합니다.
    /// 호버 콜라이더 복사 실패는 시각 VFX 생성 실패로 전파하지 않습니다.
    /// </summary>
    private static void TryCopyHoverColliderToProxy(
        GameObject vfx,
        Transform renderGroup,
        GameObject proxy,
        BattleVfxEntry entry)
    {
        if (vfx == null || renderGroup == null || proxy == null || entry == null)
            return;

        try
        {
            BoxCollider2D source = vfx.GetComponentInChildren<BoxCollider2D>(true);
            if (source == null)
                return;

            Vector2 half = source.size * 0.5f;
            Vector2 sourceOffset = source.offset;

            Vector3[] corners =
            {
                source.transform.TransformPoint(sourceOffset + new Vector2(-half.x, -half.y)),
                source.transform.TransformPoint(sourceOffset + new Vector2(-half.x,  half.y)),
                source.transform.TransformPoint(sourceOffset + new Vector2( half.x, -half.y)),
                source.transform.TransformPoint(sourceOffset + new Vector2( half.x,  half.y))
            };

            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = renderGroup.InverseTransformPoint(corners[i]);
                min.x = Mathf.Min(min.x, local.x);
                min.y = Mathf.Min(min.y, local.y);
                max.x = Mathf.Max(max.x, local.x);
                max.y = Mathf.Max(max.y, local.y);
            }

            Vector2 renderLocalCenter = (min + max) * 0.5f;
            Vector2 renderLocalSize = max - min;

            float renderHeight = Mathf.Max(0.01f, entry.renderCameraOrthographicSize * 2f);
            float renderToWorld = Mathf.Max(0.01f, entry.proxyWorldHeight) / renderHeight;

            Vector2 desiredWorldOffset = renderLocalCenter * renderToWorld;
            Vector2 desiredWorldSize = new(
                Mathf.Max(0.0001f, renderLocalSize.x * renderToWorld),
                Mathf.Max(0.0001f, renderLocalSize.y * renderToWorld));

            Vector3 proxyScale3 = proxy.transform.lossyScale;
            float proxyScaleX = Mathf.Max(0.0001f, Mathf.Abs(proxyScale3.x));
            float proxyScaleY = Mathf.Max(0.0001f, Mathf.Abs(proxyScale3.y));

            GameObject hoverObject = new("HoverCollider");
            hoverObject.layer = proxy.layer;
            hoverObject.transform.SetParent(proxy.transform, false);
            hoverObject.transform.localPosition = new Vector3(
                desiredWorldOffset.x / proxyScaleX,
                desiredWorldOffset.y / proxyScaleY,
                0f);
            hoverObject.transform.localRotation = Quaternion.identity;
            hoverObject.transform.localScale = new Vector3(
                1f / proxyScaleX,
                1f / proxyScaleY,
                1f);

            BoxCollider2D target = hoverObject.AddComponent<BoxCollider2D>();
            if (target == null)
            {
                DestroyUnityObject(hoverObject);
                return;
            }

            target.offset = Vector2.zero;
            target.size = desiredWorldSize;
            target.isTrigger = true;
        }
        catch (Exception exception)
        {
            string prefabName = entry.prefab != null ? entry.prefab.name : "Unknown VFX";
            Debug.LogWarning(
                $"[BattleWorldVfxRenderer] VFX 호버 콜라이더 복사에 실패했습니다. " +
                $"시각 VFX는 계속 표시합니다. Prefab:{prefabName}\n{exception.Message}");
        }
    }

    private static int ResolveVisibleLayer(Transform followTarget, int renderLayer)
    {
        if (followTarget == null)
            return 0;

        return ResolveVisibleLayer(followTarget.gameObject.layer, renderLayer);
    }

    private static int ResolveVisibleLayer(int sourceLayer, int renderLayer)
    {
        return sourceLayer == renderLayer ? 0 : sourceLayer;
    }
}
