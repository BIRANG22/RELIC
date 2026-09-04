using System;
using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LobbyRelicOfferButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string RarityRingProxyObjectName = "RarityRingVfxProxy";
    private const string RarityRingRendererRootName = "__LobbyRelicOfferRarityVfxRenderer";
    private const string SharedVfxImageObjectName = "VFXImage";
    private const int VfxRendererIndex = 1;
    private const int DefaultRarityRingRenderLayer = 9;
    private const float RenderSpaceOriginX = 20000f;
    private const float RenderSlotSpacing = 1000f;
    private const float CameraDistance = 10f;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField, Min(1f)] private float hoverIconScale = 1.12f;

    [Header("Rarity Ring")]
    [SerializeField] private GameObject rarityRingRoot;
    [Tooltip("기존 등급 색상 변경 대상 파티클입니다.")]
    [SerializeField] private ParticleSystem rarityParticles;

    [Tooltip("등급에 따라 같은 색상으로 변경할 추가 파티클들입니다. Size를 늘려 원하는 만큼 지정할 수 있습니다.")]
    [SerializeField]
    private ParticleSystem[] additionalRarityColorParticles =
        Array.Empty<ParticleSystem>();

    [Header("Rarity Ring RenderTexture")]
    [Tooltip("유물 등급 VFX를 독립적으로 렌더링할 RenderTexture 가로 크기입니다.")]
    [SerializeField, Min(1)] private int rarityRingRenderTextureWidth = 512;

    [Tooltip("유물 등급 VFX를 독립적으로 렌더링할 RenderTexture 세로 크기입니다.")]
    [SerializeField, Min(1)] private int rarityRingRenderTextureHeight = 512;

    [Tooltip("독립 렌더 카메라의 orthographic size입니다.")]
    [SerializeField, Min(0.01f)] private float rarityRingRenderCameraOrthographicSize = 3f;

    [Tooltip("유물 버튼 안에서 VFX RawImage가 차지할 기준 크기입니다.")]
    [SerializeField] private Vector2 rarityRingProxySize = new(250f, 250f);

    [Tooltip("유물 버튼 중앙 기준 VFX RawImage 위치 보정입니다.")]
    [SerializeField] private Vector2 rarityRingProxyAnchoredPosition = Vector2.zero;

    [Tooltip("독립 렌더 공간 안에서 VFX 원본을 배치할 위치입니다.")]
    [SerializeField] private Vector3 rarityRingRenderVfxLocalPosition = Vector3.zero;

    [Tooltip("비워두면 씬의 VFXImage RawImage 재질을 복제해 사용합니다.")]
    [SerializeField] private Material rarityRingProxyMaterialTemplate;

    [Header("Rarity Particle Colors")]
    [Tooltip("일반 등급 테두리 색상")]
    [SerializeField]
    private Color commonParticleColor =
        new Color32(200, 208, 217, 255);

    [Tooltip("레어 등급 테두리 색상")]
    [SerializeField]
    private Color uncommonParticleColor =
        new Color32(92, 219, 131, 255);

    [Tooltip("에픽 등급 테두리 색상")]
    [SerializeField]
    private Color rareParticleColor =
        new Color32(78, 141, 255, 255);

    [Tooltip("유니크 등급 테두리 색상")]
    [SerializeField]
    private Color uniqueParticleColor =
        new Color32(255, 179, 71, 255);

    [Tooltip("알 수 없는 등급에 사용할 기본 색상")]
    [SerializeField] private Color defaultParticleColor = Color.white;

    private string relicId;
    private Action<string> purchaseRequested;
    private Action<string, bool> hoverChanged;
    private Vector3 iconOriginalScale = Vector3.one;
    private bool iconScaleCached;
    private bool isHovered;
    private RelicRarity currentRarity = RelicRarity.None;

    private bool clickListenerRegistered;
    private bool missingViewWarningLogged;

    private Coroutine rarityRingHideCoroutine;
    private CanvasGroup canvasGroup;
    private RawImage rarityRingProxyImage;
    private RenderTexture rarityRingRenderTexture;
    private Material rarityRingRuntimeMaterial;
    private GameObject rarityRingRenderGroup;
    private GameObject rarityRingRuntimeVfx;
    private Camera rarityRingRenderCamera;
    private ParticleSystem[] rarityRingRuntimeParticleSystems =
        Array.Empty<ParticleSystem>();

    private static int nextRarityRingRenderSlot;

    public string RelicId => relicId;
    public RelicRarity CurrentRarity => currentRarity;
    public Color CurrentRarityColor => GetParticleColor(currentRarity);
    public Sprite IconSprite => iconImage != null ? iconImage.sprite : null;
    public Image IconImage => iconImage;
    public RectTransform ButtonRectTransform => transform as RectTransform;
    public RectTransform IconRectTransform => iconImage != null ? iconImage.rectTransform : null;
    public GameObject RarityRingRootObject => rarityRingRoot;

    private void Awake()
    {
        EnsureView();
    }

    public void Bind(
        LobbyRelicOffer offer,
        Sprite icon,
        RelicRarity rarity,
        Action<string> callback)
    {
        Bind(offer, icon, rarity, callback, null);
    }

    public void Bind(
        LobbyRelicOffer offer,
        Sprite icon,
        RelicRarity rarity,
        Action<string> callback,
        Action<string, bool> hoverCallback)
    {
        if (!EnsureView())
            return;

        ResetHoverState();
        SetTemporaryHidden(false);

        relicId = offer.RelicId;
        purchaseRequested = callback;
        hoverChanged = hoverCallback;
        currentRarity = rarity;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        priceText.text = offer.Price.ToString();
        button.interactable = true;

        ShowRarityRing(rarity);
        UIBlurBackgroundManager.MarkReplicaDirty();
    }

    public void ShowSold()
    {
        if (!EnsureView())
            return;

        ResetHoverState();
        SetTemporaryHidden(false);
        priceText.text = GameLocalization.Get("lobby.sold_out", "판매 완료");
        button.interactable = false;

        FadeOutRarityRing();
        UIBlurBackgroundManager.MarkReplicaDirty();
    }

    public void ShowEmpty()
    {
        ResetHoverState();
        SetTemporaryHidden(false);
        HideRarityRingImmediately();

        if (!EnsureView())
            return;

        relicId = null;
        hoverChanged = null;
        currentRarity = RelicRarity.None;

        iconImage.sprite = null;
        iconImage.enabled = false;

        priceText.text = string.Empty;
        button.interactable = false;
        UIBlurBackgroundManager.MarkReplicaDirty();
    }

    public void SetInteractable(bool interactable)
    {
        if (EnsureView())
            button.interactable = interactable;
    }

    public void SetTemporaryHidden(bool hidden)
    {
        EnsureCanvasGroup();
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = hidden ? 0f : 1f;
        canvasGroup.interactable = !hidden;
        canvasGroup.blocksRaycasts = !hidden;
    }

    private bool EnsureView()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
        {
            iconImage =
                transform.Find("RelicIcon")?.GetComponent<Image>();
        }

        if (iconImage != null && !iconScaleCached)
        {
            iconOriginalScale = iconImage.rectTransform.localScale;
            iconScaleCached = true;
        }

        if (priceText == null)
        {
            priceText =
                transform.Find("Price")?.GetComponent<TMP_Text>();
        }

        EnsureRarityRingReferences();
        EnsureCanvasGroup();

        if (button == null ||
            iconImage == null ||
            priceText == null)
        {
            if (!missingViewWarningLogged)
            {
                Debug.LogWarning(
                    $"[LobbyRelicOfferButtonUI] " +
                    $"Serialized view references are missing on '{name}'.",
                    this);

                missingViewWarningLogged = true;
            }

            return false;
        }

        EnsureClickListener();
        return true;
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void EnsureClickListener()
    {
        if (button == null || clickListenerRegistered)
            return;

        button.onClick.AddListener(RequestPurchase);
        clickListenerRegistered = true;
    }

    private void LateUpdate()
    {
        ApplyRarityRingProxyLayout();
    }

    private void RequestPurchase()
    {
        if (button == null ||
            !button.interactable ||
            string.IsNullOrWhiteSpace(relicId))
        {
            return;
        }

        purchaseRequested?.Invoke(relicId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.interactable || string.IsNullOrWhiteSpace(relicId))
            return;

        isHovered = true;

        if (iconImage != null)
            iconImage.rectTransform.localScale = iconOriginalScale * hoverIconScale;

        ApplyRarityRingProxyLayout();
        UIBlurBackgroundManager.MarkReplicaDirty();

        hoverChanged?.Invoke(relicId, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverState();
    }

    private void ResetHoverState()
    {
        if (iconImage != null && iconScaleCached)
            iconImage.rectTransform.localScale = iconOriginalScale;

        ApplyRarityRingProxyLayout();
        UIBlurBackgroundManager.MarkReplicaDirty();

        if (isHovered && !string.IsNullOrWhiteSpace(relicId))
            hoverChanged?.Invoke(relicId, false);

        isHovered = false;
    }

    private void EnsureRarityRingReferences()
    {
        if (rarityRingRoot == null)
        {
            rarityRingRoot =
                transform.Find("magic_ring_06")?.gameObject;
        }

        if (rarityParticles == null &&
            rarityRingRoot != null)
        {
            ParticleSystem[] particles =
                rarityRingRoot.GetComponentsInChildren<ParticleSystem>(
                    true);

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null &&
                    particles[i].name == "03")
                {
                    rarityParticles = particles[i];
                    break;
                }
            }
        }

    }

    private void ShowRarityRing(RelicRarity rarity)
    {
        EnsureRarityRingReferences();
        CancelRarityRingFade();

        if (rarityRingRoot == null)
            return;

        rarityRingRoot.SetActive(false);

        Color rarityColor = GetParticleColor(rarity);
        ApplyRarityColor(rarityParticles, rarityColor);

        if (additionalRarityColorParticles != null)
        {
            for (int i = 0; i < additionalRarityColorParticles.Length; i++)
            {
                ApplyRarityColor(
                    additionalRarityColorParticles[i],
                    rarityColor);
            }
        }

        if (!EnsureRarityRingProxy())
            return;

        ApplyRarityColor(rarityRingRuntimeParticleSystems, rarityColor);
        SetRarityRingProxyVisible(true);
        ApplyRarityRingProxyLayout();

        for (int i = 0;
             i < rarityRingRuntimeParticleSystems.Length;
             i++)
        {
            ParticleSystem particles =
                rarityRingRuntimeParticleSystems[i];

            if (particles == null ||
                !particles.gameObject.activeInHierarchy)
            {
                continue;
            }

            particles.Clear(false);
            particles.Play(false);
        }
    }


    private static void ApplyRarityColor(
        ParticleSystem particles,
        Color color)
    {
        if (particles == null)
            return;

        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
    }

    private static void ApplyRarityColor(
        ParticleSystem[] particleSystems,
        Color color)
    {
        if (particleSystems == null)
            return;

        for (int i = 0; i < particleSystems.Length; i++)
            ApplyRarityColor(particleSystems[i], color);
    }

    /// <summary>
    /// 인스펙터에 지정한 등급별 색상을 반환합니다.
    /// </summary>
    private Color GetParticleColor(RelicRarity rarity)
    {
        switch (rarity)
        {
            case RelicRarity.Common:
                return commonParticleColor;

            case RelicRarity.Rare:
                return uncommonParticleColor;

            case RelicRarity.Epic:
                return rareParticleColor;

            case RelicRarity.Unique:
                return uniqueParticleColor;

            default:
                return defaultParticleColor;
        }
    }

    private void FadeOutRarityRing()
    {
        EnsureRarityRingReferences();
        CancelRarityRingFade();

        if (rarityRingRoot == null &&
            rarityRingProxyImage == null)
        {
            return;
        }

        bool stoppedParticles = false;

        for (int i = 0;
             i < rarityRingRuntimeParticleSystems.Length;
             i++)
        {
            ParticleSystem particles =
                rarityRingRuntimeParticleSystems[i];

            if (particles == null ||
                !particles.gameObject.activeInHierarchy)
            {
                continue;
            }

            particles.Stop(
                false,
                ParticleSystemStopBehavior.StopEmitting);

            stoppedParticles = true;
        }

        if (!stoppedParticles)
        {
            SetRarityRingProxyVisible(false);

            if (rarityRingRoot != null)
                rarityRingRoot.SetActive(false);

            return;
        }

        rarityRingHideCoroutine =
            StartCoroutine(HideRarityRingWhenFinished());
    }

    private IEnumerator HideRarityRingWhenFinished()
    {
        while (IsRarityRingAlive())
            yield return null;

        rarityRingHideCoroutine = null;

        if (rarityRingRoot != null)
            rarityRingRoot.SetActive(false);

        SetRarityRingProxyVisible(false);
    }

    private bool IsRarityRingAlive()
    {
        for (int i = 0;
             i < rarityRingRuntimeParticleSystems.Length;
             i++)
        {
            ParticleSystem particles =
                rarityRingRuntimeParticleSystems[i];

            if (particles != null &&
                particles.gameObject.activeInHierarchy &&
                particles.IsAlive(false))
            {
                return true;
            }
        }

        return false;
    }

    private void HideRarityRingImmediately()
    {
        CancelRarityRingFade();
        EnsureRarityRingReferences();

        if (rarityRingRoot != null)
            rarityRingRoot.SetActive(false);

        for (int i = 0; i < rarityRingRuntimeParticleSystems.Length; i++)
        {
            ParticleSystem particles = rarityRingRuntimeParticleSystems[i];
            if (particles == null)
                continue;

            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        SetRarityRingProxyVisible(false);
    }

    private void CancelRarityRingFade()
    {
        if (rarityRingHideCoroutine == null)
            return;

        StopCoroutine(rarityRingHideCoroutine);
        rarityRingHideCoroutine = null;
    }

    private bool EnsureRarityRingProxy()
    {
        if (rarityRingRoot == null)
            return false;

        EnsureRarityRingRenderTexture();
        EnsureRarityRingProxyImage();
        EnsureRarityRingRenderGroup();
        EnsureRarityRingRuntimeVfx();
        EnsureRarityRingRenderCamera();
        AssignRarityRingRenderTexture();

        return rarityRingRenderTexture != null &&
            rarityRingProxyImage != null &&
            rarityRingRuntimeVfx != null &&
            rarityRingRenderCamera != null;
    }

    private void EnsureRarityRingRenderTexture()
    {
        int width = Mathf.Max(1, rarityRingRenderTextureWidth);
        int height = Mathf.Max(1, rarityRingRenderTextureHeight);

        if (rarityRingRenderTexture != null &&
            rarityRingRenderTexture.width == width &&
            rarityRingRenderTexture.height == height)
        {
            return;
        }

        ReleaseRarityRingRenderTexture();

        rarityRingRenderTexture = new RenderTexture(
            width,
            height,
            16,
            RenderTextureFormat.ARGB32)
        {
            name = $"{name}_RarityRingRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };

        rarityRingRenderTexture.Create();
    }

    private void EnsureRarityRingProxyImage()
    {
        if (rarityRingProxyImage == null)
        {
            Transform existing = transform.Find(RarityRingProxyObjectName);
            if (existing != null)
                rarityRingProxyImage = existing.GetComponent<RawImage>();
        }

        if (rarityRingProxyImage == null)
        {
            GameObject proxyObject = new(
                RarityRingProxyObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            proxyObject.transform.SetParent(transform, false);
            rarityRingProxyImage = proxyObject.GetComponent<RawImage>();
        }

        rarityRingProxyImage.raycastTarget = false;
        rarityRingProxyImage.color = Color.white;
        rarityRingProxyImage.enabled = false;
        rarityRingProxyImage.rectTransform.SetAsFirstSibling();
        ApplyRarityRingProxyLayout();
    }

    private void EnsureRarityRingRenderGroup()
    {
        if (rarityRingRenderGroup != null)
            return;

        GameObject rendererRoot = EnsureRarityRingRendererRoot();
        if (rendererRoot == null)
            return;

        rarityRingRenderGroup = new GameObject($"{name}_RarityRingRender");
        rarityRingRenderGroup.transform.SetParent(rendererRoot.transform, false);
        rarityRingRenderGroup.transform.localPosition = new Vector3(
            RenderSpaceOriginX + nextRarityRingRenderSlot * RenderSlotSpacing,
            0f,
            0f);
        nextRarityRingRenderSlot++;
    }

    private GameObject EnsureRarityRingRendererRoot()
    {
        GameObject rendererRoot = GameObject.Find(RarityRingRendererRootName);
        if (rendererRoot != null)
            return rendererRoot;

        rendererRoot = new GameObject(RarityRingRendererRootName);

        Scene scene = gameObject.scene;
        if (scene.IsValid())
            SceneManager.MoveGameObjectToScene(rendererRoot, scene);

        return rendererRoot;
    }

    private void EnsureRarityRingRuntimeVfx()
    {
        if (rarityRingRuntimeVfx != null ||
            rarityRingRenderGroup == null ||
            rarityRingRoot == null)
        {
            return;
        }

        int renderLayer = ResolveRarityRingRenderLayer();

        rarityRingRuntimeVfx = Instantiate(
            rarityRingRoot,
            rarityRingRenderGroup.transform,
            false);
        rarityRingRuntimeVfx.name = $"{rarityRingRoot.name}_Render";
        rarityRingRuntimeVfx.transform.localPosition =
            rarityRingRenderVfxLocalPosition;
        rarityRingRuntimeVfx.transform.localRotation =
            rarityRingRoot.transform.localRotation;
        rarityRingRuntimeVfx.transform.localScale =
            rarityRingRoot.transform.localScale;
        SetLayerRecursively(rarityRingRuntimeVfx, renderLayer);
        rarityRingRuntimeVfx.SetActive(true);

        rarityRingRuntimeParticleSystems =
            rarityRingRuntimeVfx.GetComponentsInChildren<ParticleSystem>(
                true);
    }

    private int ResolveRarityRingRenderLayer()
    {
        if (rarityParticles != null &&
            rarityParticles.gameObject.layer != gameObject.layer)
        {
            return rarityParticles.gameObject.layer;
        }

        if (rarityRingRoot != null &&
            rarityRingRoot.layer != gameObject.layer)
        {
            return rarityRingRoot.layer;
        }

        return DefaultRarityRingRenderLayer;
    }

    private void EnsureRarityRingRenderCamera()
    {
        if (rarityRingRenderCamera != null ||
            rarityRingRenderGroup == null)
        {
            return;
        }

        GameObject cameraObject = new("Camera");
        cameraObject.transform.SetParent(rarityRingRenderGroup.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
        cameraObject.transform.localRotation = Quaternion.identity;

        rarityRingRenderCamera = cameraObject.AddComponent<Camera>();
        rarityRingRenderCamera.clearFlags = CameraClearFlags.SolidColor;
        rarityRingRenderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        rarityRingRenderCamera.orthographic = true;
        rarityRingRenderCamera.orthographicSize =
            Mathf.Max(0.01f, rarityRingRenderCameraOrthographicSize);
        rarityRingRenderCamera.nearClipPlane = 0.01f;
        rarityRingRenderCamera.farClipPlane = CameraDistance * 4f;
        rarityRingRenderCamera.cullingMask =
            1 << ResolveRarityRingRenderLayer();
        rarityRingRenderCamera.targetTexture = rarityRingRenderTexture;
        rarityRingRenderCamera.allowHDR = true;
        rarityRingRenderCamera.allowMSAA = false;
        rarityRingRenderCamera.enabled = false;

        UniversalAdditionalCameraData cameraData =
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.SetRenderer(VfxRendererIndex);
    }

    private void AssignRarityRingRenderTexture()
    {
        if (rarityRingProxyImage != null)
            rarityRingProxyImage.texture = rarityRingRenderTexture;

        if (rarityRingRenderCamera != null)
            rarityRingRenderCamera.targetTexture = rarityRingRenderTexture;

        EnsureRarityRingRuntimeMaterial();
        if (rarityRingRuntimeMaterial != null)
            rarityRingRuntimeMaterial.mainTexture = rarityRingRenderTexture;

        if (rarityRingProxyImage != null)
            rarityRingProxyImage.material = rarityRingRuntimeMaterial;
        UIBlurBackgroundManager.MarkReplicaDirty();
    }

    private void EnsureRarityRingRuntimeMaterial()
    {
        if (rarityRingRuntimeMaterial != null)
            return;

        Material template = ResolveRarityRingProxyMaterialTemplate();
        if (template == null)
            return;

        rarityRingRuntimeMaterial = new Material(template)
        {
            name = $"{name}_RarityRingProxyMaterial"
        };
    }

    private Material ResolveRarityRingProxyMaterialTemplate()
    {
        if (rarityRingProxyMaterialTemplate != null)
            return rarityRingProxyMaterialTemplate;

        GameObject sharedVfxImageObject = GameObject.Find(SharedVfxImageObjectName);
        if (sharedVfxImageObject == null)
            return null;

        RawImage sharedVfxImage =
            sharedVfxImageObject.GetComponent<RawImage>();
        return sharedVfxImage != null
            ? sharedVfxImage.material
            : null;
    }

    private void ApplyRarityRingProxyLayout()
    {
        if (rarityRingProxyImage == null)
            return;

        RectTransform rectTransform = rarityRingProxyImage.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = rarityRingProxyAnchoredPosition;
        rectTransform.sizeDelta = new Vector2(
            Mathf.Max(1f, rarityRingProxySize.x),
            Mathf.Max(1f, rarityRingProxySize.y));
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale =
            Vector3.one * (isHovered ? hoverIconScale : 1f);
        UIBlurBackgroundManager.MarkReplicaDirty();
    }

    private void SetRarityRingProxyVisible(bool visible)
    {
        if (rarityRingProxyImage != null)
            rarityRingProxyImage.enabled = visible;

        if (rarityRingRenderCamera != null)
            rarityRingRenderCamera.enabled = visible;

        if (rarityRingRuntimeVfx != null)
            rarityRingRuntimeVfx.SetActive(visible);
        UIBlurBackgroundManager.MarkReplicaDirty();
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            children[i].gameObject.layer = layer;
    }

    private void DestroyRarityRingProxy()
    {
        DestroyUnityObject(rarityRingRenderGroup);
        rarityRingRenderGroup = null;
        rarityRingRuntimeVfx = null;
        rarityRingRenderCamera = null;
        rarityRingRuntimeParticleSystems = Array.Empty<ParticleSystem>();

        DestroyUnityObject(rarityRingRuntimeMaterial);
        rarityRingRuntimeMaterial = null;

        ReleaseRarityRingRenderTexture();
    }

    private void ReleaseRarityRingRenderTexture()
    {
        if (rarityRingRenderTexture == null)
            return;

        rarityRingRenderTexture.Release();
        DestroyUnityObject(rarityRingRenderTexture);
        rarityRingRenderTexture = null;
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

    private void OnDestroy()
    {
        if (button != null && clickListenerRegistered)
        {
            button.onClick.RemoveListener(RequestPurchase);
            clickListenerRegistered = false;
        }

        DestroyRarityRingProxy();
    }

    private void OnDisable()
    {
        ResetHoverState();
        HideRarityRingImmediately();
    }
}
