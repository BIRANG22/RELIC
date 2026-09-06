using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyUiVfxRenderTextureProxy : MonoBehaviour
{
    private const int VfxRendererIndex = 1;
    private const int DefaultRenderLayer = 9;
    private const float RenderSpaceOriginX = 30000f;
    private const float RenderSlotSpacing = 1000f;
    private const float CameraDistance = 10f;
    private const string SharedVfxImageObjectName = "VFXImage";
    private const string DefaultProxyObjectName = "LobbyUiVfxProxy";
    private const string DefaultRendererRootName = "__LobbyUiVfxRenderer";

    private static int nextRenderSlot;

    private GameObject sourceVfxRoot;
    private string proxyObjectName = DefaultProxyObjectName;
    private string rendererRootName = DefaultRendererRootName;
    private int renderTextureWidth = 512;
    private int renderTextureHeight = 512;
    private float renderCameraOrthographicSize = 3f;
    private Vector2 proxySize = new(400f, 400f);
    private Vector2 proxyAnchoredPosition = Vector2.zero;
    private Vector3 renderVfxLocalPosition = Vector3.zero;
    private Material proxyMaterialTemplate;
    private int renderLayer = DefaultRenderLayer;

    private RawImage proxyImage;
    private RenderTexture renderTexture;
    private Material runtimeMaterial;
    private GameObject renderGroup;
    private GameObject runtimeVfx;
    private Camera renderCamera;
    private ParticleSystem[] runtimeParticleSystems =
        Array.Empty<ParticleSystem>();

    public RawImage ProxyImage => proxyImage;
    public GameObject RuntimeVfx => runtimeVfx;
    public GameObject SourceVfxRoot => sourceVfxRoot;

    public void Configure(
        GameObject source,
        string proxyName,
        string rootName,
        int textureWidth,
        int textureHeight,
        float cameraOrthographicSize,
        Vector2 uiProxySize,
        Vector2 uiProxyAnchoredPosition,
        Vector3 vfxLocalPosition,
        Material materialTemplate,
        int layer = DefaultRenderLayer)
    {
        sourceVfxRoot = source;
        proxyObjectName = string.IsNullOrWhiteSpace(proxyName)
            ? DefaultProxyObjectName
            : proxyName.Trim();
        rendererRootName = string.IsNullOrWhiteSpace(rootName)
            ? DefaultRendererRootName
            : rootName.Trim();

        int width = Mathf.Max(1, textureWidth);
        int height = Mathf.Max(1, textureHeight);
        bool textureSizeChanged =
            width != renderTextureWidth ||
            height != renderTextureHeight;

        renderTextureWidth = width;
        renderTextureHeight = height;
        renderCameraOrthographicSize =
            Mathf.Max(0.01f, cameraOrthographicSize);
        proxySize = uiProxySize;
        proxyAnchoredPosition = uiProxyAnchoredPosition;
        renderVfxLocalPosition = vfxLocalPosition;
        proxyMaterialTemplate = materialTemplate;
        renderLayer = layer >= 0 && layer <= 31
            ? layer
            : DefaultRenderLayer;

        if (sourceVfxRoot != null)
            sourceVfxRoot.SetActive(false);

        if (textureSizeChanged)
            ReleaseRenderTexture();

        ConfigureProxyRect();
        ApplyRenderCameraSettings();

        if (runtimeVfx != null)
            runtimeVfx.transform.localPosition = renderVfxLocalPosition;
    }

    public bool Show()
    {
        if (sourceVfxRoot == null)
        {
            Hide();
            return false;
        }

        sourceVfxRoot.SetActive(false);

        if (!EnsureProxyImage() ||
            !EnsureRenderTexture() ||
            !EnsureRenderGroup() ||
            !EnsureRuntimeVfx() ||
            !EnsureRenderCamera())
        {
            Hide();
            return false;
        }

        AssignRenderTexture();
        renderGroup.SetActive(true);
        runtimeVfx.SetActive(true);
        proxyImage.gameObject.SetActive(true);
        proxyImage.enabled = true;
        RestartRuntimeParticles();
        return true;
    }

    public void Hide()
    {
        if (sourceVfxRoot != null)
            sourceVfxRoot.SetActive(false);

        StopAndClearRuntimeParticles();

        if (proxyImage != null)
            proxyImage.enabled = false;

        if (renderGroup != null)
            renderGroup.SetActive(false);
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        Hide();
        DestroyUnityObject(renderGroup);
        renderGroup = null;
        runtimeVfx = null;
        renderCamera = null;
        runtimeParticleSystems = Array.Empty<ParticleSystem>();
        ReleaseRenderTexture();
        DestroyUnityObject(runtimeMaterial);
        runtimeMaterial = null;
    }

    private bool EnsureProxyImage()
    {
        if (proxyImage != null)
        {
            ConfigureProxyRect();
            return true;
        }

        RectTransform ownerRect = transform as RectTransform;
        if (ownerRect == null)
            return false;

        Transform existing = transform.Find(proxyObjectName);
        if (existing != null)
            proxyImage = existing.GetComponent<RawImage>();

        if (proxyImage == null)
        {
            GameObject proxyObject = new(
                proxyObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            proxyObject.transform.SetParent(ownerRect, false);
            proxyImage = proxyObject.GetComponent<RawImage>();
        }

        proxyImage.raycastTarget = false;
        proxyImage.maskable = true;
        ConfigureProxyRect();
        return true;
    }

    private void ConfigureProxyRect()
    {
        if (proxyImage == null)
            return;

        RectTransform proxyRect = proxyImage.rectTransform;
        proxyRect.anchorMin = new Vector2(0.5f, 0.5f);
        proxyRect.anchorMax = new Vector2(0.5f, 0.5f);
        proxyRect.pivot = new Vector2(0.5f, 0.5f);
        proxyRect.anchoredPosition = proxyAnchoredPosition;
        proxyRect.sizeDelta = proxySize;
        proxyRect.localScale = Vector3.one;
    }

    private bool EnsureRenderTexture()
    {
        if (renderTexture != null &&
            renderTexture.width == renderTextureWidth &&
            renderTexture.height == renderTextureHeight)
        {
            return true;
        }

        ReleaseRenderTexture();

        renderTexture = new RenderTexture(
            renderTextureWidth,
            renderTextureHeight,
            16,
            RenderTextureFormat.ARGB32)
        {
            name = $"{proxyObjectName}_RT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        renderTexture.Create();
        return renderTexture != null;
    }

    private bool EnsureRenderGroup()
    {
        if (renderGroup != null)
            return true;

        Transform rendererRoot = EnsureRendererRoot();
        if (rendererRoot == null)
            return false;

        renderGroup = new GameObject($"{proxyObjectName}_Render");
        renderGroup.transform.SetParent(rendererRoot, false);
        renderGroup.transform.localPosition = new Vector3(
            RenderSpaceOriginX + nextRenderSlot * RenderSlotSpacing,
            0f,
            0f);
        nextRenderSlot++;
        return true;
    }

    private Transform EnsureRendererRoot()
    {
        GameObject rootObject = GameObject.Find(rendererRootName);
        if (rootObject == null)
            rootObject = new GameObject(rendererRootName);

        Scene ownerScene = gameObject.scene;
        if (ownerScene.IsValid() && rootObject.scene != ownerScene)
            SceneManager.MoveGameObjectToScene(rootObject, ownerScene);

        return rootObject.transform;
    }

    private bool EnsureRuntimeVfx()
    {
        if (runtimeVfx != null)
            return true;

        if (sourceVfxRoot == null || renderGroup == null)
            return false;

        runtimeVfx = Instantiate(sourceVfxRoot, renderGroup.transform, false);
        runtimeVfx.name = sourceVfxRoot.name;
        runtimeVfx.transform.localPosition = renderVfxLocalPosition;
        runtimeVfx.transform.localRotation = sourceVfxRoot.transform.localRotation;
        runtimeVfx.transform.localScale = sourceVfxRoot.transform.localScale;
        SetLayerRecursively(runtimeVfx, renderLayer);
        runtimeParticleSystems =
            runtimeVfx.GetComponentsInChildren<ParticleSystem>(true);
        return true;
    }

    private bool EnsureRenderCamera()
    {
        if (renderCamera != null)
        {
            ApplyRenderCameraSettings();
            return true;
        }

        if (renderGroup == null)
            return false;

        GameObject cameraObject = new("Camera");
        cameraObject.transform.SetParent(renderGroup.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
        cameraObject.transform.localRotation = Quaternion.identity;

        renderCamera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        ApplyRenderCameraSettings();
        return true;
    }

    private void ApplyRenderCameraSettings()
    {
        if (renderCamera == null)
            return;

        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = renderCameraOrthographicSize;
        renderCamera.nearClipPlane = 0.01f;
        renderCamera.farClipPlane = CameraDistance * 4f;
        renderCamera.cullingMask = 1 << renderLayer;
        renderCamera.rect = new Rect(0f, 0f, 1f, 1f);
        renderCamera.aspect = Mathf.Max(1, renderTextureWidth) /
                              Mathf.Max(1f, renderTextureHeight);
        renderCamera.targetTexture = renderTexture;
        renderCamera.allowHDR = true;
        renderCamera.allowMSAA = false;

        UniversalAdditionalCameraData cameraData =
            renderCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = false;
            cameraData.SetRenderer(VfxRendererIndex);
        }
    }

    private void AssignRenderTexture()
    {
        if (proxyImage == null)
            return;

        proxyImage.texture = renderTexture;

        Material template = ResolveProxyMaterialTemplate();
        if (runtimeMaterial == null && template != null)
            runtimeMaterial = new Material(template);

        if (runtimeMaterial != null)
        {
            runtimeMaterial.mainTexture = renderTexture;
            proxyImage.material = runtimeMaterial;
        }
    }

    private Material ResolveProxyMaterialTemplate()
    {
        if (proxyMaterialTemplate != null)
            return proxyMaterialTemplate;

        GameObject sharedVfxImageObject = GameObject.Find(SharedVfxImageObjectName);
        if (sharedVfxImageObject == null)
            return null;

        RawImage sharedVfxImage =
            sharedVfxImageObject.GetComponent<RawImage>();
        return sharedVfxImage != null ? sharedVfxImage.material : null;
    }

    private void RestartRuntimeParticles()
    {
        if (runtimeParticleSystems == null)
            return;

        for (int i = 0; i < runtimeParticleSystems.Length; i++)
        {
            ParticleSystem particleSystem = runtimeParticleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void StopAndClearRuntimeParticles()
    {
        if (runtimeParticleSystems == null)
            return;

        for (int i = 0; i < runtimeParticleSystems.Length; i++)
        {
            ParticleSystem particleSystem = runtimeParticleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
        }
    }

    private void ReleaseRenderTexture()
    {
        if (renderTexture == null)
            return;

        if (renderCamera != null)
            renderCamera.targetTexture = null;

        if (proxyImage != null && proxyImage.texture == renderTexture)
            proxyImage.texture = null;

        renderTexture.Release();
        DestroyUnityObject(renderTexture);
        renderTexture = null;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null)
                child.gameObject.layer = layer;
        }
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
}
