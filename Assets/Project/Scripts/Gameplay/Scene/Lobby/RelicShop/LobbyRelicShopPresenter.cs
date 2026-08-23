using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicShopPresenter : MonoBehaviour
{
    [SerializeField] private Transform[] worldAnchors = new Transform[3];
    [SerializeField] private LobbyBlueDustiumHudUI blueDustiumHud;
    [SerializeField] private Vector2 skillUpgradeButtonStartPosition = new(2400f, 200f);
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private LobbyRelicOfferButtonUI[] offerButtons = new LobbyRelicOfferButtonUI[3];
    [SerializeField] private LobbyRelicRefreshButtonUI refreshButton;
    [SerializeField] private GameObject relicDescriptionRoot;
    [SerializeField] private TMP_Text relicDescriptionNameText;
    [SerializeField] private TMP_Text relicDescriptionRarityText;
    [SerializeField] private TMP_Text relicDescriptionBodyText;

    [Header("Purchase Animation")]
    [Tooltip("구매 효과가 처음 오른쪽 위로 튀어 오르는 UI 이동량입니다.")]
    [SerializeField] private Vector2 purchaseBounceOffset = new Vector2(180f, 120f);
    [Tooltip("오른쪽 위로 튀어 오르는 시간입니다. 처음에는 느리고 끝으로 갈수록 빨라집니다.")]
    [SerializeField, Min(0.01f)] private float purchaseBounceDuration = 0.18f;
    [Tooltip("튀어 오른 위치에서 Equip 버튼까지 돌진하는 시간입니다. 처음에는 느리고 끝으로 갈수록 빠르게 가속합니다.")]
    [SerializeField, Min(0.01f)] private float ringFlyDuration = 0.32f;
    [SerializeField, Min(0.1f)] private float ringEndScale = 0.72f;
    [Tooltip("유물 구매 후 Equip 버튼으로 날아갈 원형 효과 스프라이트입니다.")]
    [SerializeField] private Sprite purchaseTransferEffectSprite;
    [Tooltip("원형 효과의 UI 크기입니다.")]
    [SerializeField] private Vector2 purchaseTransferEffectSize = new Vector2(96f, 96f);
    [Tooltip("원형 효과가 최종적으로 들어갈 Equip 버튼 위치입니다. Equip 버튼 또는 버튼 아래의 빈 RectTransform을 직접 지정할 수 있습니다.")]
    [SerializeField] private RectTransform equipEffectTarget;

    [Header("Purchase Confirmation")]
    [Tooltip("유물을 선택했을 때 CHECK 프리팹에 표시할 확인 문구입니다.")]
    [SerializeField] private string purchaseConfirmMessage = "이 유물로 결정하시겠습니까?";

    [Header("Purchase Animation Trail")]
    [Tooltip("이동 중 꼬리 잔상을 생성하는 간격입니다. 값이 작을수록 꼬리가 촘촘해집니다.")]
    [SerializeField, Min(0.005f)] private float trailSpawnInterval = 0.025f;
    [Tooltip("각 꼬리 잔상이 사라지는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float trailLifetime = 0.18f;
    [Tooltip("생성되는 꼬리의 시작 크기입니다. 1이면 본체와 같은 크기입니다.")]
    [SerializeField, Range(0.05f, 1f)] private float trailStartScale = 0.78f;
    [Tooltip("꼬리가 사라질 때의 최종 크기입니다.")]
    [SerializeField, Range(0f, 1f)] private float trailEndScale = 0.2f;
    [Tooltip("꼬리의 시작 불투명도입니다.")]
    [SerializeField, Range(0f, 1f)] private float trailStartAlpha = 0.48f;

    private readonly List<LobbyRelicOfferButtonUI> buttons = new();
    private Canvas ownerCanvas;
    private Button closeButton;
    private RecordPanelUI recordPanelUI;
    private bool missingPanelWarningLogged;
    private bool isPurchaseAnimating;
    private Coroutine purchaseAnimationCoroutine;

    private void Awake()
    {
        ownerCanvas = GetComponentInParent<Canvas>();
        EnsureShopPanelReady();
        InitializeSkillUpgradeButton(Camera.main);
    }

    public void Open()
    {
        if (isPurchaseAnimating)
            return;

        // 다른 위치 모달이 열려 있으면 유물 상점을 중복으로 열지 않는다.
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return;

        EnsureShopPanelReady();
        if (panelRoot == null)
            return;

        LobbyPositionModalInputBlocker.Block(this);
        RestoreShopPanelVisualState();
        UIBlurBackground.EnsureForPanel(panelRoot);
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        RefreshOffers();
    }

    public void Close()
    {
        if (isPurchaseAnimating)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        HideRelicDescription();

        // ESC와 닫기 버튼 모두 같은 Close()를 사용하므로
        // 상점이 닫힐 때 월드 오브젝트 입력 차단도 반드시 해제한다.
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDisable()
    {
        if (purchaseAnimationCoroutine != null)
        {
            StopCoroutine(purchaseAnimationCoroutine);
            purchaseAnimationCoroutine = null;
        }

        isPurchaseAnimating = false;
        RestoreOfferVisibility();
        SetCloseButtonInteractable(true);

        // 씬 전환이나 오브젝트 비활성화로 닫히는 경우에도
        // 정적 입력 차단 상태가 남지 않게 정리한다.
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDestroy()
    {
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    public void RefreshOffers()
    {
        EnsureShopPanelReady();
        if (buttons.Count == 0)
            return;

        RestoreOfferVisibility();

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
        {
            ShowAllEmpty();
            return;
        }

        LobbyRuntimeData runtime = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        bool canMutate = CanLocalPlayerMutateHostOnlyState();
        bool hadRestoredOffers = runtime.RelicOfferIds != null &&
                                 runtime.RelicOfferIds.Count > 0;
        IReadOnlyList<LobbyRelicOffer> offers = ResolveOffers(runtime, canMutate);
        bool purchaseLimitReached = LobbyRelicShopPurchaseLimit.HasPurchasedOffer(runtime);
        bool generatedOffers =
            canMutate &&
            !hadRestoredOffers &&
            runtime.RelicOfferIds != null &&
            runtime.RelicOfferIds.Count > 0;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (i >= offers.Count)
            {
                buttons[i].ShowEmpty();
                continue;
            }

            LobbyRelicOffer offer = offers[i];
            Sprite icon = null;
            DataManager.Instance.RelicIconDatabase?.TryGetIcon(offer.RelicId, out icon);

            RelicRarity rarity = RelicRarity.None;
            if (DataManager.Instance.RelicDatabase.TryGet(offer.RelicId, out RelicData relic))
                RelicRarityUtility.TryParseChestRarity(relic.Rarity, out rarity);

            buttons[i].Bind(offer, icon, rarity, Purchase, HandleOfferHover);

            if (Contains(runtime.OwnedRelicIds, offer.RelicId))
                buttons[i].ShowSold();
            else
                buttons[i].SetInteractable(canMutate && !purchaseLimitReached && !isPurchaseAnimating);
        }

        blueDustiumHud?.Refresh();
        RefreshRefreshButton(runtime);

        if (generatedOffers)
            PublishHostSnapshotAfterLocalMutation();
    }

    private IReadOnlyList<LobbyRelicOffer> ResolveOffers(
        LobbyRuntimeData runtime,
        bool canGenerateMissingOffers)
    {
        var restored = new List<LobbyRelicOffer>();
        if (runtime.RelicOfferIds != null)
        {
            for (int i = 0; i < runtime.RelicOfferIds.Count; i++)
            {
                string id = runtime.RelicOfferIds[i];
                if (DataManager.Instance.RelicDatabase.TryGet(id, out RelicData relic) &&
                    LobbyRelicPricePolicy.TryGetPrice(relic.Rarity, out int price))
                {
                    restored.Add(new LobbyRelicOffer(id, price));
                }
            }
        }

        if (restored.Count > 0)
            return restored;

        if (!canGenerateMissingOffers)
            return restored;

        if (runtime.RelicOfferSeed == 0)
            runtime.RelicOfferSeed = Environment.TickCount == 0 ? 1 : Environment.TickCount;

        var service = new LobbyRelicOfferService(new SeededLobbyRelicShopRandom(runtime.RelicOfferSeed));
        IReadOnlyList<LobbyRelicOffer> generated = service.BuildOffers(
            DataManager.Instance.RelicDatabase.GetAll(), runtime.OwnedRelicIds, buttons.Count);

        runtime.RelicOfferIds.Clear();
        for (int i = 0; i < generated.Count; i++)
            runtime.RelicOfferIds.Add(generated[i].RelicId);

        return generated;
    }

    private void Purchase(string relicId)
    {
        if (isPurchaseAnimating)
            return;

        if (!CanLocalPlayerMutateHostOnlyState())
            return;

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (UIManager.Instance == null)
        {
            Debug.LogWarning(
                "[LobbyRelicShopPresenter] CHECK 프리팹을 표시할 UIManager를 찾을 수 없습니다.",
                this);
            return;
        }

        if (UIManager.Instance.IsConfirmDialogOpen)
            return;

        string confirmedRelicId = relicId.Trim();
        UIManager.Instance.ShowConfirmDialog(
            purchaseConfirmMessage,
            () =>
            {
                UIManager.Instance?.HideConfirmDialog();
                ConfirmPurchase(confirmedRelicId);
            },
            () => UIManager.Instance?.HideConfirmDialog());
    }

    private void ConfirmPurchase(string relicId)
    {
        if (isPurchaseAnimating)
            return;

        if (!CanLocalPlayerMutateHostOnlyState())
            return;

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
            return;

        LobbyRuntimeData runtime = DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
        if (runtime == null)
            return;

        LobbyRelicOfferButtonUI selectedButton = FindBoundOfferButton(relicId);

        LobbyRelicPurchaseResult result =
            new LobbyRelicPurchaseService(DataManager.Instance.RelicDatabase)
                .Execute(new LobbyRelicPurchaseCommand(relicId), runtime);

        if (!result.Succeeded)
        {
            RefreshOffers();
            return;
        }

        RecordDiscoveryService.RegisterRelic(DataManager.Instance, result.RelicId);

        if (selectedButton != null && ownerCanvas != null)
        {
            if (purchaseAnimationCoroutine != null)
                StopCoroutine(purchaseAnimationCoroutine);

            purchaseAnimationCoroutine =
                StartCoroutine(PlayPurchaseAnimationRoutine(runtime, selectedButton));
            return;
        }

        FinalizePurchasePresentation(runtime);
    }

    private IEnumerator PlayPurchaseAnimationRoutine(
        LobbyRuntimeData runtime,
        LobbyRelicOfferButtonUI selectedButton)
    {
        isPurchaseAnimating = true;
        HideRelicDescription();
        SetCloseButtonInteractable(false);

        // 선택된 유물의 현재 화면 위치를 먼저 저장한 뒤 상점을 바로 닫습니다.
        // 다른 유물을 숨기거나 선택 유물을 중앙으로 이동시키는 연출은 사용하지 않습니다.
        RectTransform selectedRect = selectedButton != null
            ? selectedButton.ButtonRectTransform
            : null;
        RectTransform equipButtonTarget = ResolveEquipButtonTarget();
        Camera targetCamera = ResolveTargetUiCamera(equipButtonTarget);
        Vector2 startScreenPosition = GetRectScreenCenter(selectedRect, targetCamera);
        Vector2 equipScreenPosition = GetRectScreenCenter(equipButtonTarget, targetCamera);
        Color rarityColor = selectedButton != null
            ? selectedButton.CurrentRarityColor
            : Color.white;

        HideShopPanelForPurchaseAnimation();

        Canvas transferCanvas = ResolveTransferEffectCanvas();
        Image transferEffect = CreateTransferEffectImage(
            transferCanvas,
            startScreenPosition,
            rarityColor);

        if (transferEffect != null && equipButtonTarget != null)
        {
            yield return AnimateUiTransferEffectRoutine(
                transferEffect.rectTransform,
                transferCanvas,
                startScreenPosition,
                equipScreenPosition);
        }

        if (transferEffect != null)
            Destroy(transferEffect.gameObject);

        FinalizePurchasePresentation(runtime);
    }

    private void FinalizePurchasePresentation(LobbyRuntimeData runtime)
    {
        isPurchaseAnimating = false;
        purchaseAnimationCoroutine = null;
        RestoreOfferVisibility();
        RestoreShopPanelVisualState();
        SetCloseButtonInteractable(true);

        blueDustiumHud?.Refresh();
        RelicEquipPanelUI.RefreshAll();
        PublishHostSnapshotAfterLocalMutation();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void EnsureShopPanelReady()
    {
        if (panelRoot == null)
            panelRoot = FindScenePanelRoot();

        if (panelRoot == null)
        {
            buttons.Clear();

            if (!missingPanelWarningLogged)
            {
                Debug.LogWarning(
                    "[LobbyRelicShopPresenter] RelicShopPanel must be assigned in the Lobby scene.",
                    this);
                missingPanelWarningLogged = true;
            }

            return;
        }

        BindOfferButtons();

        if (refreshButton == null && panelRoot != null)
            refreshButton = panelRoot.GetComponentInChildren<LobbyRelicRefreshButtonUI>(true);

        refreshButton?.Initialize(RefreshRelicOffers);
        EnsureDescriptionView();
        BindCloseButton();
    }

    private GameObject FindScenePanelRoot()
    {
        Transform localPanel = transform.Find("RelicShopPanel");
        if (localPanel != null)
            return localPanel.gameObject;

        if (ownerCanvas == null)
            return null;

        Transform[] children = ownerCanvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == "RelicShopPanel")
                return children[i].gameObject;
        }

        return null;
    }

    private void BindOfferButtons()
    {
        buttons.Clear();

        if (offerButtons != null)
        {
            for (int i = 0; i < offerButtons.Length; i++)
            {
                if (offerButtons[i] != null)
                    buttons.Add(offerButtons[i]);
            }
        }

        if (buttons.Count > 0 || panelRoot == null)
            return;

        LobbyRelicOfferButtonUI[] sceneButtons =
            panelRoot.GetComponentsInChildren<LobbyRelicOfferButtonUI>(true);
        for (int i = 0; i < sceneButtons.Length; i++)
            buttons.Add(sceneButtons[i]);
    }

    private void BindCloseButton()
    {
        if (panelRoot == null)
            return;

        closeButton = panelRoot.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
    }

    private void RefreshRelicOffers()
    {
        if (isPurchaseAnimating)
            return;

        if (!CanLocalPlayerMutateHostOnlyState())
            return;

        LobbyRuntimeData runtime = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (runtime == null || DataManager.Instance.RelicDatabase == null)
            return;

        int nextSeed = unchecked(runtime.RelicOfferSeed * 1664525 + 1013904223 + runtime.RelicRefreshCount);
        LobbyRelicRefreshResult result = new LobbyRelicRefreshService(
            DataManager.Instance.RelicDatabase,
            new SeededLobbyRelicShopRandom(nextSeed)).Execute(runtime, nextSeed);
        if (!result.Succeeded)
        {
            Debug.LogWarning($"[LobbyRelicShopPresenter] Relic offer refresh failed: {result.Failure}");
            RefreshRefreshButton(runtime);
            return;
        }

        RefreshOffers();
        PublishHostSnapshotAfterLocalMutation();
    }

    private void RefreshRefreshButton(LobbyRuntimeData runtime)
    {
        if (refreshButton == null || runtime == null)
            return;

        int price = LobbyRelicRefreshPricePolicy.GetPrice(runtime.RelicRefreshCount);
        int remainingCount = LobbyRelicRefreshPricePolicy.GetRemainingCount(runtime.RelicRefreshCount);
        refreshButton.SetState(
            price,
            remainingCount,
            !isPurchaseAnimating &&
            CanLocalPlayerMutateHostOnlyState() &&
            remainingCount > 0 &&
            !LobbyRelicShopPurchaseLimit.HasPurchasedOffer(runtime) &&
            !LobbyRelicRefreshService.AreAllOffersPurchased(runtime));
    }

    private static bool CanLocalPlayerMutateHostOnlyState()
    {
        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null ||
               synchronizer.CanLocalPlayerMutateHostOnlyState();
    }

    private static void PublishHostSnapshotAfterLocalMutation()
    {
        SteamLobbySharedStateSynchronizer.Instance
            ?.PublishHostSnapshotAfterLocalMutation();
    }

    private void InitializeSkillUpgradeButton(Camera camera)
    {
        Transform upgradeButton = transform.Find("SkillUpgradeOpenButton");
        if (upgradeButton == null || ownerCanvas == null || camera == null)
            return;

        WorldAnchorCanvasFollower follower = upgradeButton.GetComponent<WorldAnchorCanvasFollower>();
        if (follower == null)
            follower = upgradeButton.gameObject.AddComponent<WorldAnchorCanvasFollower>();

        Transform depthAnchor = worldAnchors != null && worldAnchors.Length > 0
            ? worldAnchors[worldAnchors.Length - 1]
            : null;
        follower.InitializeAtCanvasPosition(
            skillUpgradeButtonStartPosition,
            depthAnchor,
            ownerCanvas,
            camera);
    }

    private void ShowAllEmpty()
    {
        HideRelicDescription();
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].ShowEmpty();
    }

    private void HandleOfferHover(string relicId, bool hovered)
    {
        if (isPurchaseAnimating)
            return;

        if (!hovered || string.IsNullOrWhiteSpace(relicId))
        {
            HideRelicDescription();
            return;
        }

        EnsureDescriptionView();
        RelicData relic = DataManager.Instance?.RelicDatabase?.Get(relicId);
        if (relic == null || relicDescriptionRoot == null)
        {
            HideRelicDescription();
            return;
        }

        if (relicDescriptionNameText != null)
            relicDescriptionNameText.text = string.IsNullOrWhiteSpace(relic.Name)
                ? relicId
                : GameDataLocalization.RelicName(relic);

        if (relicDescriptionRarityText != null)
        {
            relicDescriptionRarityText.text = FormatRelicRarityLabel(relic.Rarity);
            relicDescriptionRarityText.color = ResolveRecordRarityColor(relic.Rarity);
        }

        if (relicDescriptionBodyText != null)
            relicDescriptionBodyText.text = GameDataLocalization.RelicEffectDescription(relic);

        relicDescriptionRoot.SetActive(true);
    }

    private void HideRelicDescription()
    {
        if (relicDescriptionNameText != null)
            relicDescriptionNameText.text = string.Empty;

        if (relicDescriptionRarityText != null)
        {
            relicDescriptionRarityText.text = string.Empty;
            relicDescriptionRarityText.color = Color.white;
        }

        if (relicDescriptionBodyText != null)
            relicDescriptionBodyText.text = string.Empty;
    }

    private void EnsureDescriptionView()
    {
        if (panelRoot == null)
            return;

        if (relicDescriptionRoot == null)
        {
            Transform info = panelRoot.transform.Find("relic_info");
            if (info != null)
                relicDescriptionRoot = info.gameObject;
        }

        if (relicDescriptionRoot == null)
            return;

        if (relicDescriptionNameText == null)
        {
            Transform nameTransform = FindDescendant(relicDescriptionRoot.transform, "relic_name");
            if (nameTransform != null)
                relicDescriptionNameText = nameTransform.GetComponent<TMP_Text>();
        }

        if (relicDescriptionRarityText == null)
        {
            Transform rarityTransform = FindDescendant(relicDescriptionRoot.transform, "relic_Rarity");
            if (rarityTransform != null)
                relicDescriptionRarityText = rarityTransform.GetComponent<TMP_Text>();
        }

        if (relicDescriptionBodyText == null)
        {
            Transform effectTransform = FindDescendant(relicDescriptionRoot.transform, "relic_effect");
            if (effectTransform != null)
                relicDescriptionBodyText = effectTransform.GetComponent<TMP_Text>();
        }

        relicDescriptionRoot.SetActive(true);
        HideRelicDescription();
    }

    private static string FormatRelicRarityLabel(string rarity)
    {
        string normalized = string.IsNullOrWhiteSpace(rarity) ? string.Empty : rarity.Trim();

        if (string.Equals(normalized, "Common", StringComparison.OrdinalIgnoreCase)) return "일반 유물";
        if (string.Equals(normalized, "Rare", StringComparison.OrdinalIgnoreCase)) return "레어 유물";
        if (string.Equals(normalized, "Epic", StringComparison.OrdinalIgnoreCase)) return "에픽 유물";
        if (string.Equals(normalized, "Unique", StringComparison.OrdinalIgnoreCase)) return "유니크 유물";

        return normalized;
    }

    private Color ResolveRecordRarityColor(string rarity)
    {
        if (recordPanelUI == null)
        {
            RecordPanelUI[] panels = FindObjectsByType<RecordPanelUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (panels.Length > 0)
                recordPanelUI = panels[0];
        }

        return recordPanelUI != null
            ? recordPanelUI.GetRarityDisplayColor(rarity)
            : Color.white;
    }

    private void RestoreOfferVisibility()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
                buttons[i].SetTemporaryHidden(false);
        }
    }

    private void SetCloseButtonInteractable(bool interactable)
    {
        if (closeButton != null)
            closeButton.interactable = interactable;
    }

    private LobbyRelicOfferButtonUI FindBoundOfferButton(string relicId)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null)
                continue;

            if (string.Equals(buttons[i].RelicId?.Trim(), relicId?.Trim(), StringComparison.Ordinal))
                return buttons[i];
        }

        return null;
    }

    private RectTransform ResolveEquipButtonTarget()
    {
        if (equipEffectTarget != null)
            return equipEffectTarget;

        LobbyEquipOpenButton[] equipButtons = FindObjectsByType<LobbyEquipOpenButton>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < equipButtons.Length; i++)
        {
            if (equipButtons[i] == null)
                continue;

            if (equipButtons[i].isActiveAndEnabled)
                return equipButtons[i].transform as RectTransform;
        }

        for (int i = 0; i < equipButtons.Length; i++)
        {
            if (equipButtons[i] != null)
                return equipButtons[i].transform as RectTransform;
        }

        return FindDescendant(ownerCanvas != null ? ownerCanvas.transform : transform, "Equip") as RectTransform;
    }

    private Canvas ResolveTransferEffectCanvas()
    {
        if (purchaseTransferEffectSprite == null)
        {
            Debug.LogWarning(
                "[LobbyRelicShopPresenter] Purchase Transfer Effect Sprite가 지정되지 않았습니다.",
                this);
            return null;
        }

        if (ownerCanvas == null)
            ownerCanvas = GetComponentInParent<Canvas>();

        if (ownerCanvas == null)
        {
            Debug.LogWarning(
                "[LobbyRelicShopPresenter] 구매 이동 효과를 표시할 기존 Canvas를 찾을 수 없습니다.",
                this);
            return null;
        }

        return ownerCanvas;
    }

    private Image CreateTransferEffectImage(
        Canvas transferCanvas,
        Vector2 screenPosition,
        Color rarityColor)
    {
        if (transferCanvas == null || purchaseTransferEffectSprite == null)
            return null;

        GameObject effectObject = new GameObject(
            "RelicPurchaseTransferEffect",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform rect = effectObject.GetComponent<RectTransform>();
        rect.SetParent(transferCanvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = purchaseTransferEffectSize;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = ScreenToCanvasLocalPosition(transferCanvas, screenPosition);
        rect.SetAsLastSibling();

        Image image = effectObject.GetComponent<Image>();
        image.sprite = purchaseTransferEffectSprite;
        image.color = rarityColor;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private sealed class PurchaseTrailGhost
    {
        public RectTransform Rect;
        public Image Image;
        public Vector3 StartScale;
        public Color StartColor;
        public float Age;
    }

    private IEnumerator AnimateUiTransferEffectRoutine(
        RectTransform target,
        Canvas transferCanvas,
        Vector2 fromScreenPosition,
        Vector2 toScreenPosition)
    {
        if (target == null || transferCanvas == null)
            yield break;

        Image sourceImage = target.GetComponent<Image>();
        Vector2 fromPosition = ScreenToCanvasLocalPosition(transferCanvas, fromScreenPosition);
        Vector2 toPosition = ScreenToCanvasLocalPosition(transferCanvas, toScreenPosition);
        Vector2 bouncePosition = fromPosition + purchaseBounceOffset;

        Vector3 originalScale = target.localScale;
        Vector3 bounceScale = originalScale * 1.08f;
        Vector3 endScale = originalScale * ringEndScale;
        var trailGhosts = new List<PurchaseTrailGhost>();
        float trailTimer = 0f;

        target.SetAsLastSibling();

        // 1단계: 오른쪽 위로 툭 튀어 오릅니다.
        // EaseInCubic을 사용해 처음에는 천천히 움직이다가 끝에서 속도가 붙도록 합니다.
        float bounceElapsed = 0f;
        float safeBounceDuration = Mathf.Max(0.01f, purchaseBounceDuration);
        while (bounceElapsed < safeBounceDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            bounceElapsed += deltaTime;
            float t = Mathf.Clamp01(bounceElapsed / safeBounceDuration);
            float eased = EaseInCubic(t);

            target.anchoredPosition = Vector2.LerpUnclamped(fromPosition, bouncePosition, eased);
            target.localScale = Vector3.LerpUnclamped(originalScale, bounceScale, eased);

            trailTimer += deltaTime;
            SpawnTrailGhostsIfNeeded(
                target,
                sourceImage,
                transferCanvas,
                trailGhosts,
                ref trailTimer);
            UpdateTrailGhosts(trailGhosts, deltaTime);
            yield return null;
        }

        target.anchoredPosition = bouncePosition;
        target.localScale = bounceScale;

        // 2단계: 튕겨 오른 위치에서 Equip 목표점으로 빨려 들어가듯 가속합니다.
        // 시작은 느리고 마지막에 빠르게 치고 들어가도록 EaseInQuint를 사용합니다.
        float dashElapsed = 0f;
        float safeDashDuration = Mathf.Max(0.01f, ringFlyDuration);
        while (dashElapsed < safeDashDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            dashElapsed += deltaTime;
            float t = Mathf.Clamp01(dashElapsed / safeDashDuration);
            float eased = EaseInQuint(t);

            target.anchoredPosition = Vector2.LerpUnclamped(bouncePosition, toPosition, eased);
            target.localScale = Vector3.LerpUnclamped(bounceScale, endScale, eased);

            trailTimer += deltaTime;
            SpawnTrailGhostsIfNeeded(
                target,
                sourceImage,
                transferCanvas,
                trailGhosts,
                ref trailTimer);
            UpdateTrailGhosts(trailGhosts, deltaTime);
            yield return null;
        }

        target.anchoredPosition = toPosition;
        target.localScale = endScale;

        // 본체가 도착한 뒤에도 남아 있는 꼬리는 짧게 자연스럽게 사라집니다.
        while (trailGhosts.Count > 0)
        {
            UpdateTrailGhosts(trailGhosts, Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private void SpawnTrailGhostsIfNeeded(
        RectTransform sourceRect,
        Image sourceImage,
        Canvas transferCanvas,
        List<PurchaseTrailGhost> trailGhosts,
        ref float trailTimer)
    {
        if (sourceRect == null ||
            sourceImage == null ||
            sourceImage.sprite == null ||
            transferCanvas == null ||
            trailGhosts == null)
        {
            return;
        }

        float safeInterval = Mathf.Max(0.005f, trailSpawnInterval);
        while (trailTimer >= safeInterval)
        {
            trailTimer -= safeInterval;
            PurchaseTrailGhost ghost = CreateTrailGhost(sourceRect, sourceImage, transferCanvas);
            if (ghost != null)
                trailGhosts.Add(ghost);
        }
    }

    private PurchaseTrailGhost CreateTrailGhost(
        RectTransform sourceRect,
        Image sourceImage,
        Canvas transferCanvas)
    {
        GameObject ghostObject = new GameObject(
            "RelicPurchaseTrail",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
        ghostRect.SetParent(transferCanvas.transform, false);
        ghostRect.anchorMin = sourceRect.anchorMin;
        ghostRect.anchorMax = sourceRect.anchorMax;
        ghostRect.pivot = sourceRect.pivot;
        ghostRect.sizeDelta = sourceRect.sizeDelta;
        ghostRect.anchoredPosition = sourceRect.anchoredPosition;
        ghostRect.localRotation = sourceRect.localRotation;
        ghostRect.localScale = sourceRect.localScale * trailStartScale;

        // 본체 바로 아래 형제 순서에 두어 꼬리가 본체를 덮지 않게 합니다.
        int sourceSiblingIndex = sourceRect.GetSiblingIndex();
        ghostRect.SetSiblingIndex(Mathf.Max(0, sourceSiblingIndex));
        sourceRect.SetAsLastSibling();

        Image ghostImage = ghostObject.GetComponent<Image>();
        ghostImage.sprite = sourceImage.sprite;
        Color ghostColor = sourceImage.color;
        ghostColor.a *= trailStartAlpha;
        ghostImage.color = ghostColor;
        ghostImage.preserveAspect = sourceImage.preserveAspect;
        ghostImage.raycastTarget = false;

        return new PurchaseTrailGhost
        {
            Rect = ghostRect,
            Image = ghostImage,
            StartScale = ghostRect.localScale,
            StartColor = ghostColor,
            Age = 0f
        };
    }

    private void UpdateTrailGhosts(List<PurchaseTrailGhost> trailGhosts, float deltaTime)
    {
        if (trailGhosts == null)
            return;

        float safeLifetime = Mathf.Max(0.01f, trailLifetime);
        for (int i = trailGhosts.Count - 1; i >= 0; i--)
        {
            PurchaseTrailGhost ghost = trailGhosts[i];
            if (ghost == null || ghost.Rect == null || ghost.Image == null)
            {
                trailGhosts.RemoveAt(i);
                continue;
            }

            ghost.Age += deltaTime;
            float t = Mathf.Clamp01(ghost.Age / safeLifetime);
            float fade = 1f - t;

            Color color = ghost.StartColor;
            color.a = ghost.StartColor.a * fade;
            ghost.Image.color = color;

            float scaleMultiplier = Mathf.Lerp(trailStartScale, trailEndScale, t) /
                                    Mathf.Max(0.0001f, trailStartScale);
            ghost.Rect.localScale = ghost.StartScale * scaleMultiplier;

            if (t < 1f)
                continue;

            Destroy(ghost.Rect.gameObject);
            trailGhosts.RemoveAt(i);
        }
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseInQuint(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * t * t;
    }

    private static Vector2 ScreenToCanvasLocalPosition(Canvas canvas, Vector2 screenPosition)
    {
        if (canvas == null)
            return Vector2.zero;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return Vector2.zero;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return localPoint;
        }

        return Vector2.zero;
    }

    private Camera ResolveTargetUiCamera(RectTransform targetRect)
    {
        if (targetRect != null)
        {
            Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
        }

        if (ownerCanvas != null && ownerCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return ownerCanvas.worldCamera != null ? ownerCanvas.worldCamera : Camera.main;

        return null;
    }

    private static Vector2 GetRectScreenCenter(RectTransform targetRect, Camera fallbackCamera)
    {
        if (targetRect == null)
            return Vector2.zero;

        Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
        Camera uiCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (targetCanvas.worldCamera != null ? targetCanvas.worldCamera : fallbackCamera)
            : null;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
    }

    private void HideShopPanelForPurchaseAnimation()
    {
        if (panelRoot == null)
            return;

        // 확인창에서 예를 눌러 실제 유물 획득이 확정되면
        // 상점 패널을 즉시 닫고, 그 뒤 획득 이펙트만 Canvas 위에서 진행합니다.
        panelRoot.SetActive(false);
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void RestoreShopPanelVisualState()
    {
        if (panelRoot == null)
            return;

        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group == null)
            return;

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == objectName)
                return children[i];
        }

        return null;
    }

    private static bool Contains(IEnumerable<string> ids, string target)
    {
        if (ids == null)
            return false;

        foreach (string id in ids)
        {
            if (string.Equals(id?.Trim(), target, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
