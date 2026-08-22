using System;
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
    [SerializeField] private TMP_Text relicDescriptionBodyText;

    private readonly List<LobbyRelicOfferButtonUI> buttons = new();
    private Canvas ownerCanvas;
    private bool missingPanelWarningLogged;

    private void Awake()
    {
        ownerCanvas = GetComponentInParent<Canvas>();
        EnsureShopPanelReady();
        InitializeSkillUpgradeButton(Camera.main);
    }

    public void Open()
    {
        // 다른 위치 모달이 열려 있으면 유물 상점을 중복으로 열지 않는다.
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return;

        EnsureShopPanelReady();
        if (panelRoot == null)
            return;

        LobbyPositionModalInputBlocker.Block(this);
        UIBlurBackground.EnsureForPanel(panelRoot);
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        RefreshOffers();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        HideRelicDescription();

        // ESC와 닫기 버튼 모두 같은 Close()를 사용하므로
        // 상점이 닫힐 때 월드 오브젝트 입력 차단도 반드시 해제한다.
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDisable()
    {
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
                buttons[i].SetInteractable(canMutate && !purchaseLimitReached);
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
        if (!CanLocalPlayerMutateHostOnlyState())
            return;

        LobbyRuntimeData runtime = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (runtime == null)
            return;

        LobbyRelicPurchaseResult result =
            new LobbyRelicPurchaseService(DataManager.Instance.RelicDatabase)
                .Execute(new LobbyRelicPurchaseCommand(relicId), runtime);

        if (!result.Succeeded)
            return;

        RecordDiscoveryService.RegisterRelic(DataManager.Instance, result.RelicId);
        blueDustiumHud?.Refresh();

        RelicEquipPanelUI.RefreshAll();
        RefreshOffers();
        PublishHostSnapshotAfterLocalMutation();
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

        // 패널 안에 직접 만든 CloseButton만 찾아 닫기 기능을 연결한다.
        // 버튼이나 텍스트를 런타임에 자동 생성하지 않는다.
        Button closeButton = panelRoot.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
    }

    private void RefreshRelicOffers()
    {
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
        refreshButton.SetState(
            price,
            CanLocalPlayerMutateHostOnlyState() &&
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

        if (relicDescriptionBodyText != null)
            relicDescriptionBodyText.text = GameDataLocalization.RelicDescription(relic);

        relicDescriptionRoot.SetActive(true);
    }

    private void HideRelicDescription()
    {
        if (relicDescriptionNameText != null)
            relicDescriptionNameText.text = string.Empty;

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

        if (relicDescriptionBodyText == null)
        {
            Transform effectTransform = FindDescendant(relicDescriptionRoot.transform, "relic_effect");
            if (effectTransform != null)
                relicDescriptionBodyText = effectTransform.GetComponent<TMP_Text>();
        }

        relicDescriptionRoot.SetActive(true);
        HideRelicDescription();
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
