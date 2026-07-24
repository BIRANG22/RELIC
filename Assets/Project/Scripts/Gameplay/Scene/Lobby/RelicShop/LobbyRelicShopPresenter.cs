using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyRelicShopPresenter : MonoBehaviour
{
    [SerializeField] private Transform[] worldAnchors = new Transform[3];
    [SerializeField] private LobbyBlueDustiumHudUI blueDustiumHud;
    [SerializeField] private Vector2 skillUpgradeButtonStartPosition = new(2400f, 200f);
    [SerializeField] private Sprite relicRefreshIcon;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private LobbyRelicOfferButtonUI[] offerButtons = new LobbyRelicOfferButtonUI[3];
    [SerializeField] private LobbyRelicRefreshButtonUI refreshButton;

    private readonly List<LobbyRelicOfferButtonUI> buttons = new();
    private Canvas ownerCanvas;

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
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        RefreshOffers();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

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
        IReadOnlyList<LobbyRelicOffer> offers = ResolveOffers(runtime);

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
            buttons[i].Bind(offer, icon, Purchase);

            if (Contains(runtime.OwnedRelicIds, offer.RelicId))
                buttons[i].ShowSold();
        }

        blueDustiumHud?.Refresh();
        RefreshRefreshButton(runtime);
    }

    private IReadOnlyList<LobbyRelicOffer> ResolveOffers(LobbyRuntimeData runtime)
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
        LobbyRuntimeData runtime = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (runtime == null)
            return;

        LobbyRelicPurchaseResult result =
            new LobbyRelicPurchaseService(DataManager.Instance.RelicDatabase)
                .Execute(new LobbyRelicPurchaseCommand(relicId), runtime);

        if (!result.Succeeded)
            return;

        blueDustiumHud?.Refresh();

        for (int i = 0; i < buttons.Count && i < runtime.RelicOfferIds.Count; i++)
        {
            if (runtime.RelicOfferIds[i] == relicId)
                buttons[i].ShowSold();
        }

        RelicEquipPanelUI.RefreshAll();
        RefreshRefreshButton(runtime);
    }

    private void EnsureShopPanelReady()
    {
        if (panelRoot == null)
            panelRoot = FindScenePanelRoot();

        if (panelRoot == null)
            CreateRuntimeShopPanelIfNeeded();

        BindOfferButtons();

        if (refreshButton == null && panelRoot != null)
            refreshButton = panelRoot.GetComponentInChildren<LobbyRelicRefreshButtonUI>(true);

        refreshButton?.Initialize(relicRefreshIcon, RefreshRelicOffers);
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

    private void CreateRuntimeShopPanelIfNeeded()
    {
        if (panelRoot != null || ownerCanvas == null)
            return;

        panelRoot = new GameObject("RelicShopPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRect = (RectTransform)panelRoot.transform;
        panelRect.SetParent(ownerCanvas.transform, false);
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(660f, 390f);
        panelRoot.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.065f, 0.94f);

        for (int i = 0; i < 3; i++)
        {
            LobbyRelicOfferButtonUI button = LobbyRelicOfferButtonUI.Create(panelRect, $"RelicOffer_{i + 1}");
            RectTransform buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2((i - 1) * 190f, 35f);
        }

        refreshButton = LobbyRelicRefreshButtonUI.Create(panelRect, relicRefreshIcon, RefreshRelicOffers);
        RectTransform refreshRect = (RectTransform)refreshButton.transform;
        refreshRect.anchorMin = refreshRect.anchorMax = refreshRect.pivot = new Vector2(0.5f, 0.5f);
        refreshRect.sizeDelta = new Vector2(130f, 110f);
        refreshRect.anchoredPosition = new Vector2(0f, -125f);

        panelRoot.SetActive(false);
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
    }

    private void RefreshRefreshButton(LobbyRuntimeData runtime)
    {
        if (refreshButton == null || runtime == null)
            return;

        int price = LobbyRelicRefreshPricePolicy.GetPrice(runtime.RelicRefreshCount);
        refreshButton.SetState(price, !LobbyRelicRefreshService.AreAllOffersPurchased(runtime));
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
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].ShowEmpty();
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
