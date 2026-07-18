using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class LobbyRelicShopPresenter : MonoBehaviour
{
    [SerializeField] private Transform[] worldAnchors = new Transform[3];
    [SerializeField] private LobbyBlueDustiumHudUI blueDustiumHud;
    [SerializeField] private Vector2 skillUpgradeButtonStartPosition = new(2400f, 200f);
    [SerializeField] private Sprite relicRefreshIcon;

    private readonly List<LobbyRelicOfferButtonUI> buttons = new();
    private Canvas ownerCanvas;
    private LobbyRelicRefreshButtonUI refreshButton;

    private void Awake()
    {
        ownerCanvas = GetComponentInParent<Canvas>();
        CreateButtonsIfNeeded();
    }

    private void OnEnable()
    {
        RefreshOffers();
    }

    public void RefreshOffers()
    {
        CreateButtonsIfNeeded();

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

    private void CreateButtonsIfNeeded()
    {
        if (buttons.Count > 0)
            return;

        Camera camera = Camera.main;
        for (int i = 0; i < worldAnchors.Length; i++)
        {
            LobbyRelicOfferButtonUI button = LobbyRelicOfferButtonUI.Create(transform, $"RelicOffer_{i + 1}");
            WorldAnchorCanvasFollower follower = button.gameObject.AddComponent<WorldAnchorCanvasFollower>();
            follower.Initialize(worldAnchors[i], ownerCanvas, camera);
            buttons.Add(button);
        }

        InitializeSkillUpgradeButton(camera);
        InitializeRelicRefreshButton(camera);
    }

    private void InitializeRelicRefreshButton(Camera camera)
    {
        if (refreshButton != null || ownerCanvas == null || camera == null ||
            worldAnchors == null || worldAnchors.Length == 0)
            return;

        Transform rightmost = worldAnchors[worldAnchors.Length - 1];
        if (rightmost == null)
            return;
        Vector3 spacing = Vector3.right * 2f;
        if (worldAnchors.Length > 1 && worldAnchors[worldAnchors.Length - 2] != null)
            spacing = rightmost.position - worldAnchors[worldAnchors.Length - 2].position;

        GameObject anchorObject = new("RelicRefreshAnchor");
        anchorObject.transform.SetParent(rightmost.parent, true);
        anchorObject.transform.position = rightmost.position + spacing;

        refreshButton = LobbyRelicRefreshButtonUI.Create(transform, relicRefreshIcon, RefreshRelicOffers);
        WorldAnchorCanvasFollower follower = refreshButton.gameObject.AddComponent<WorldAnchorCanvasFollower>();
        follower.Initialize(anchorObject.transform, ownerCanvas, camera);
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
            Debug.LogWarning($"[LobbyRelicShopPresenter] 유물 새로고침 실패: {result.Failure}");
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
