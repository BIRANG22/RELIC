using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LobbyRelicOfferButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField, Min(1f)] private float hoverScale = 1.05f;
    [SerializeField, Min(0.01f)] private float hoverLerpSpeed = 14f;

    [Header("Rarity VFX - Direct UI Child")]
    [Tooltip("Prefab instantiated directly under the offer UI, like GoodsIconItem.")]
    [SerializeField] private GameObject rarityVfxPrefab;
    [Tooltip("Legacy scene VFX source. If no prefab is assigned, this object is cloned; the original is hidden.")]
    [SerializeField] private GameObject rarityRingRoot;
    [Tooltip("Optional parent inside the offer hierarchy. Defaults to this transform.")]
    [SerializeField] private Transform rarityVfxRoot;
    [SerializeField] private Vector3 rarityVfxOffset = Vector3.zero;
    [SerializeField] private Vector3 rarityVfxScale = Vector3.one;
    [Tooltip("Place the VFX as the last UI sibling, as in GoodsIconItem.")]
    [SerializeField] private bool rarityVfxAsLastSibling = true;

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
    private Transform hoverScaleTarget;
    private RectTransform hoverPointerRect;
    private Canvas hoverCanvas;
    private Vector3 originalScale = Vector3.one;
    private bool originalScaleCached;
    private bool isHovered;
    private bool pointerHovered;
    private bool externalHovered;
    private bool purchaseConfirmSelected;
    private bool purchaseConfirmInteractionLocked;
    private RelicRarity currentRarity = RelicRarity.None;

    private bool clickListenerRegistered;
    private bool missingViewWarningLogged;

    private CanvasGroup canvasGroup;
    private GameObject activeRarityVfx;

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
        button.interactable = false;

        // 구매 완료 후에도 진열 정보와 가격 표시는 그대로 유지한다.
        // 판매 완료 상태는 재구매/리롤 가능 여부로만 구분한다.
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

        EnsureHoverScaleTarget();

        if (!originalScaleCached && hoverScaleTarget != null)
        {
            originalScale = hoverScaleTarget.localScale;
            originalScaleCached = true;
        }

        if (priceText == null)
        {
            priceText =
                transform.Find("Price")?.GetComponent<TMP_Text>();
        }

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
        if (UIPanelButton.IsMenuPanelOpen)
        {
            if (pointerHovered || externalHovered || isHovered)
                ResetHoverState();
        }
        else
        {
            UpdatePointerHoverFromTargetRect();
        }

        ApplyHoverScale();
    }

    private void ApplyHoverScale()
    {
        EnsureHoverScaleTarget();
        if (!originalScaleCached || hoverScaleTarget == null)
            return;

        bool keepScaled = isHovered || purchaseConfirmSelected;
        Vector3 targetScale = keepScaled
            ? originalScale * hoverScale
            : originalScale;

        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, hoverLerpSpeed) * Time.unscaledDeltaTime);
        hoverScaleTarget.localScale = Vector3.Lerp(hoverScaleTarget.localScale, targetScale, t);

        if ((hoverScaleTarget.localScale - targetScale).sqrMagnitude < 0.000001f)
            hoverScaleTarget.localScale = targetScale;
    }

    private void EnsureHoverScaleTarget()
    {
        if (hoverScaleTarget != null)
            return;

        // Lobby hierarchy: Relic01~03 > RelicOffer_1~3.
        // Hovering the offer should scale the whole Relic01~03 card, not only RelicOffer.
        Transform parent = transform.parent;
        hoverScaleTarget = parent != null ? parent : transform;
        hoverPointerRect = hoverScaleTarget as RectTransform;
        hoverCanvas = hoverScaleTarget.GetComponentInParent<Canvas>();
    }

    private void UpdatePointerHoverFromTargetRect()
    {
        EnsureHoverScaleTarget();
        if (hoverPointerRect == null)
            return;

        Camera eventCamera = null;
        if (hoverCanvas != null && hoverCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = hoverCanvas.worldCamera;

        bool hovered = RectTransformUtility.RectangleContainsScreenPoint(
            hoverPointerRect,
            Input.mousePosition,
            eventCamera);

        if (pointerHovered == hovered)
            return;

        pointerHovered = hovered;
        UpdateHoverState();
    }

    private void RequestPurchase()
    {
        RequestPurchaseFromExternal();
    }

    public void RequestPurchaseFromExternal()
    {
        if (purchaseConfirmInteractionLocked ||
            button == null ||
            !button.interactable ||
            string.IsNullOrWhiteSpace(relicId))
        {
            return;
        }

        purchaseRequested?.Invoke(relicId);
    }

    public void SetExternalHover(bool hovered)
    {
        externalHovered = !UIPanelButton.IsMenuPanelOpen && hovered;
        UpdateHoverState();
    }

    /// <summary>
    /// CHECK 구매 확인창이 열려 있는 동안 선택한 유물 카드의 확대 상태를 유지합니다.
    /// 포인터가 카드 밖으로 이동해도 이 값이 true이면 원래 크기로 돌아가지 않습니다.
    /// </summary>
    public void SetPurchaseConfirmSelected(bool selected)
    {
        purchaseConfirmSelected = selected;
    }

    /// <summary>
    /// CHECK 구매 확인창이 열린 동안 다른 유물 슬롯의 호버/클릭 입력을 잠급니다.
    /// 선택된 슬롯은 확대 상태만 유지하고, 다른 슬롯은 호버 상태를 즉시 해제합니다.
    /// </summary>
    public void SetPurchaseConfirmInteractionLocked(bool locked, bool isSelectedOffer)
    {
        purchaseConfirmInteractionLocked = locked;
        purchaseConfirmSelected = locked && isSelectedOffer;

        if (locked)
        {
            pointerHovered = false;
            externalHovered = false;

            if (isHovered)
            {
                isHovered = false;
                UIBlurBackgroundManager.MarkReplicaDirty();

                if (!string.IsNullOrWhiteSpace(relicId))
                    hoverChanged?.Invoke(relicId, false);
            }
        }

        if (!locked)
            UpdateHoverState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerHovered = !UIPanelButton.IsMenuPanelOpen;
        UpdateHoverState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerHovered = false;
        UpdateHoverState();
    }

    private void UpdateHoverState()
    {
        bool canHover =
            !purchaseConfirmInteractionLocked &&
            !UIPanelButton.IsMenuPanelOpen &&
            !string.IsNullOrWhiteSpace(relicId) &&
            button != null &&
            button.interactable;
        bool nextHovered = canHover && (pointerHovered || externalHovered);

        if (nextHovered == isHovered)
            return;

        isHovered = nextHovered;

        UIBlurBackgroundManager.MarkReplicaDirty();

        if (!string.IsNullOrWhiteSpace(relicId))
            hoverChanged?.Invoke(relicId, isHovered);
    }

    private void ResetHoverState()
    {
        purchaseConfirmSelected = false;
        purchaseConfirmInteractionLocked = false;
        pointerHovered = false;
        externalHovered = false;

        UIBlurBackgroundManager.MarkReplicaDirty();

        if (isHovered && !string.IsNullOrWhiteSpace(relicId))
            hoverChanged?.Invoke(relicId, false);

        isHovered = false;
    }

    private void ShowRarityRing(RelicRarity rarity)
    {
        HideRarityRingImmediately();

        GameObject source = rarityVfxPrefab != null ? rarityVfxPrefab : rarityRingRoot;
        if (source == null)
        {
            Transform legacy = transform.Find("magic_ring_06");
            if (legacy != null)
            {
                rarityRingRoot = legacy.gameObject;
                source = rarityRingRoot;
            }
        }
        if (source == null)
            return;

        // A scene reference acts as a reusable template, never as the live effect.
        if (source.scene.IsValid())
            source.SetActive(false);

        Transform parent = rarityVfxRoot != null &&
            (rarityVfxRoot == transform || rarityVfxRoot.IsChildOf(transform))
            ? rarityVfxRoot : transform;

        activeRarityVfx = Instantiate(source, parent, false);
        activeRarityVfx.name = source.name + "_RarityVFX";
        activeRarityVfx.transform.localPosition = rarityVfxOffset;
        activeRarityVfx.transform.localRotation = Quaternion.identity;
        activeRarityVfx.transform.localScale = rarityVfxScale;
        if (rarityVfxAsLastSibling)
            activeRarityVfx.transform.SetAsLastSibling();

        Color color = GetParticleColor(rarity);
        foreach (ParticleSystem particle in activeRarityVfx.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particle.main;
            ParticleSystem.MinMaxGradient original = main.startColor;
            Color tint = color;
            if (original.mode == ParticleSystemGradientMode.Color)
                tint.a *= original.color.a;
            main.startColor = tint;
        }
        foreach (Graphic graphic in activeRarityVfx.GetComponentsInChildren<Graphic>(true))
        {
            Color original = graphic.color;
            graphic.color = new Color(color.r, color.g, color.b, original.a);
            graphic.raycastTarget = false;
        }
        foreach (SpriteRenderer sprite in activeRarityVfx.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color original = sprite.color;
            sprite.color = new Color(color.r, color.g, color.b, original.a);
        }
        CanvasGroup group = activeRarityVfx.GetComponent<CanvasGroup>();
        if (group == null)
            group = activeRarityVfx.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        activeRarityVfx.SetActive(true);
    }

    private Color GetParticleColor(RelicRarity rarity)
    {
        switch (rarity)
        {
            case RelicRarity.Common: return commonParticleColor;
            case RelicRarity.Rare: return uncommonParticleColor;
            case RelicRarity.Epic: return rareParticleColor;
            case RelicRarity.Unique: return uniqueParticleColor;
            default: return defaultParticleColor;
        }
    }

    private void HideRarityRingImmediately()
    {
        if (activeRarityVfx == null)
            return;
        activeRarityVfx.SetActive(false);
        if (Application.isPlaying)
            Destroy(activeRarityVfx);
        else
            DestroyImmediate(activeRarityVfx);
        activeRarityVfx = null;
    }

    private void OnDestroy()
    {
        if (button != null && clickListenerRegistered)
        {
            button.onClick.RemoveListener(RequestPurchase);
            clickListenerRegistered = false;
        }

        HideRarityRingImmediately();
    }

    private void OnDisable()
    {
        ResetHoverState();
        HideRarityRingImmediately();
    }
}
