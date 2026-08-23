using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyCultureTankPanelPresenter : MonoBehaviour
{
    private const int StorageMinimumSlotCount = 36;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private Transform storageContentRoot;
    [SerializeField] private BattleBagItemSlotUI storageSlotPrefab;
    [SerializeField] private ScrollRect storageScrollRect;
    [SerializeField] private Scrollbar storageVerticalScrollbar;
    [SerializeField] private TankRow[] rows = new TankRow[3];
    [SerializeField] private Button combineButton;
    [SerializeField] private GameObject completionRoot;
    [SerializeField] private Button completionButton;
    [SerializeField] private Image completionIcon;

    private readonly List<BattleBagItemSlotUI> storageSlots = new();
    private readonly List<string> storageItemOrder = new();
    private int selectedSlotIndex = -1;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    private void Awake() { BindSceneObjects(); BindButtons(); EnsureStorageSlots(); }
    private void OnEnable() { BindSceneObjects(); BindButtons(); EnsureStorageSlots(); RefreshAll(); }
    private void Update() { if (IsOpen) RefreshAll(); }

    public void Open()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this)) return;
        BindSceneObjects(); BindButtons();
        if (panelRoot == null) return;
        LobbyPositionModalInputBlocker.Block(this);
        UIBlurBackground.EnsureForPanel(panelRoot);
        panelRoot.SetActive(true); panelRoot.transform.SetAsLastSibling(); RefreshAll();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        selectedSlotIndex = -1;
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDisable() => LobbyPositionModalInputBlocker.Unblock(this);
    private void OnDestroy() => LobbyPositionModalInputBlocker.Unblock(this);

    private void RefreshAll() { RefreshRows(); RefreshInventory(); RefreshCompletion(); }

    private void RefreshRows()
    {
        LobbyRuntimeData lobby = GetLobby();
        for (int i = 0; i < rows.Length; i++)
        {
            TankRow row = rows[i];
            if (row?.Root == null) continue;
            string slotId = GetSlotId(i);
            bool filled = CultureTankResearchService.TryGetTank(lobby, slotId, out CultureTankResearchRuntimeData slot);
            if (row.Label != null)
                row.Label.text = string.Format(
                    GameLocalization.Get("lobby.culture_tank_number", "배양조 {0}"),
                    i + 1);
            if (row.StateLabel != null)
                row.StateLabel.text = filled
                    ? GameLocalization.Get("lobby.material_inserted", "재료 투입됨")
                    : GameLocalization.Get("lobby.empty", "비어 있음");
            Sprite icon = null;
            if (filled) DataManager.Instance?.ItemIconDatabase?.TryGetIcon(slot.ItemId, out icon);
            row.SetIcon(icon);
            if (row.Button != null) row.Button.interactable = CanMutate();
        }
        if (emptyText != null) emptyText.gameObject.SetActive(false);
        if (combineButton != null)
            combineButton.interactable = CanMutate() && lobby != null && lobby.CultureTankResearches.Count == 3 &&
                                         string.IsNullOrEmpty(lobby.CompletedCultureTankCombinationId);
    }

    private void SelectRow(int index)
    {
        LobbyRuntimeData lobby = GetLobby();
        if (CultureTankResearchService.TryGetTank(lobby, GetSlotId(index), out _))
        {
            if (CultureTankResearchService.TryRemoveIngredient(lobby, GetSlotId(index), out _)) SaveAndPublish();
            selectedSlotIndex = -1;
        }
        else selectedSlotIndex = index;
        RefreshAll();
    }

    private void SelectInventoryItem(string itemId)
    {
        LobbyRuntimeData lobby = GetLobby();
        bool hasCompletedCombination = !string.IsNullOrWhiteSpace(lobby?.CompletedCultureTankCombinationId);

        if (!CanMutate() || hasCompletedCombination || string.IsNullOrWhiteSpace(itemId))
            return;

        // Storage의 재료를 바로 클릭하면 선택된 행이 있을 때는 그 행에,
        // 선택된 행이 없을 때는 CultureTankRow_1~3 중 첫 번째 빈 행에 자동 투입합니다.
        int targetSlotIndex = selectedSlotIndex >= 0
            ? selectedSlotIndex
            : FindFirstEmptyTankIndex(lobby);

        if (targetSlotIndex < 0)
        {
            Debug.LogWarning("[LobbyCultureTankPanelPresenter] 비어 있는 배양조가 없습니다.");
            return;
        }

        if (!CultureTankResearchService.TryPlaceIngredient(lobby, GetSlotId(targetSlotIndex), itemId, out string error))
        {
            Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}");
            return;
        }

        selectedSlotIndex = -1;
        SaveAndPublish();
        RefreshAll();
    }

    private void Combine()
    {
        DataManager data = DataManager.Instance;
        if (!CultureTankResearchService.TryCombine(GetLobby(), data?.ItemDatabase, data?.CompoundDatabase, out _, out string error))
        { BattleWarningUI.ShowMessage(GameLocalization.Get("lobby.cannot_combine", "조합할 수 없습니다.")); Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}"); return; }
        selectedSlotIndex = -1; SaveAndPublish(); RefreshAll();
    }

    private void ClaimCompletion()
    {
        if (!CultureTankResearchService.TryClaimCompletedCombination(GetLobby(), DataManager.Instance?.CompoundDatabase, out string compoundId, out string error))
        { Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}"); return; }

        RecordDiscoveryService.RegisterCompound(DataManager.Instance, compoundId);
        SaveAndPublish();
        RefreshAll();
    }

    private void RefreshCompletion()
    {
        LobbyRuntimeData lobby = GetLobby();
        string id = lobby?.CompletedCultureTankCombinationId;
        CompoundData recipe = null;
        bool completed = !string.IsNullOrWhiteSpace(id) &&
                         DataManager.Instance?.CompoundDatabase != null &&
                         DataManager.Instance.CompoundDatabase.TryGet(id, out recipe);
        if (completionRoot != null) completionRoot.SetActive(ShouldShowCompletionRoot(completed));
        if (completionIcon != null)
        {
            Sprite icon = null;
            if (completed && DataManager.Instance?.RelicIconDatabase != null)
                DataManager.Instance.RelicIconDatabase.TryGetIcon(recipe.CompoundId, out icon);
            completionIcon.sprite = icon;
            completionIcon.enabled = completed && icon != null;
            completionIcon.preserveAspect = true;
        }
        if (completionButton != null) completionButton.interactable = completed && CanMutate();
    }

    private void RefreshInventory()
    {
        EnsureStorageSlots();

        LobbyRuntimeData lobby = GetLobby();
        List<BagItemStack> stacks = BuildStorageStacksIncludingReserved(lobby);
        int stackCount = stacks != null ? stacks.Count : 0;
        int visibleSlotCount = Mathf.Max(StorageMinimumSlotCount, stackCount);
        EnsureStorageSlotCount(visibleSlotCount);

        bool hasCompletedCombination = !string.IsNullOrWhiteSpace(lobby?.CompletedCultureTankCombinationId);
        bool canSelect = CanMutate() && !hasCompletedCombination && HasEmptyTankSlot(lobby);

        for (int i = 0; i < storageSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = storageSlots[i];
            if (slot == null)
                continue;

            string itemId = string.Empty;
            int itemCount = 0;
            if (i < stackCount)
            {
                BagItemStack stack = stacks[i];
                itemId = stack.ItemId;
                itemCount = stack.Count;
                slot.SetupAllowZeroQuantity(stack.ItemId, stack.Count, null, null, null);
            }
            else
            {
                slot.Clear(null, null, null);
            }

            Button button = slot.GetComponent<Button>();
            CultureTankInventorySlotClickRelay relay =
                slot.GetComponent<CultureTankInventorySlotClickRelay>() ??
                slot.gameObject.AddComponent<CultureTankInventorySlotClickRelay>();
            relay.Configure(button, itemId, canSelect && slot.HasItem && itemCount > 0, SelectInventoryItem);
            slot.RefreshQuantityVisual();
        }

        if (storageContentRoot is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void BindSceneObjects()
    {
        if (panelRoot == null) panelRoot = gameObject;
        Transform root = panelRoot.transform;
        if (backButton == null) backButton = Find(root, "BackButton")?.GetComponent<Button>();
        // 새 하이어라키에서는 CultureTankPanel 바로 아래에 MixButton, completion, CultureTankRow_1~3, Storage가 위치합니다.
        // contentRoot는 구형 하이어라키 호환용으로만 유지하며, 새 구조 바인딩에는 사용하지 않습니다.
        if (contentRoot == null) contentRoot = Find(root, "Content") as RectTransform;
        Transform storage = Find(root, "Storage");
        if (storage != null)
        {
            Transform scrollView = Find(storage, "Scroll View");
            Transform viewport = Find(scrollView, "Viewport");

            if (storageContentRoot == null)
            {
                storageContentRoot = Find(viewport, "Content");
                if (storageContentRoot == null)
                    storageContentRoot = Find(storage, "Content");
            }

            if (storageScrollRect == null && scrollView != null)
                storageScrollRect = scrollView.GetComponent<ScrollRect>();

            if (storageVerticalScrollbar == null && scrollView != null)
                storageVerticalScrollbar = Find(scrollView, "Scrollbar Vertical")?.GetComponent<Scrollbar>();
        }

        BindStorageScrollView();
        if (combineButton == null)
            combineButton = Find(root, "MixButton")?.GetComponent<Button>();
        if (combineButton == null)
            Debug.LogError("[LobbyCultureTankPanelPresenter] MixButton 오브젝트에 Button 컴포넌트가 필요합니다.", this);

        if (completionRoot == null) completionRoot = Find(root, "completion")?.gameObject;
        if (completionRoot != null)
        {
            // completion에는 씬/프리팹에 미리 설정한 Button을 그대로 사용합니다.
            // 런타임에 Button/Image를 추가하면 기존 클릭 영역과 Graphic 설정이 바뀔 수 있습니다.
            completionButton = completionRoot.GetComponent<Button>();
            if (completionButton == null)
            {
                Debug.LogError("[LobbyCultureTankPanelPresenter] completion 오브젝트에 Button 컴포넌트가 필요합니다.", completionRoot);
            }
            else
            {
                Graphic targetGraphic = completionButton.targetGraphic;
                if (targetGraphic == null)
                    targetGraphic = completionRoot.GetComponent<Graphic>();
                if (targetGraphic == null)
                    targetGraphic = completionRoot.GetComponentInChildren<Graphic>(true);

                if (targetGraphic != null)
                {
                    targetGraphic.raycastTarget = true;
                    completionButton.targetGraphic = targetGraphic;
                }
            }
        }
        if (completionIcon == null && completionRoot != null)
            completionIcon = Find(completionRoot.transform, "icon")?.GetComponent<Image>();
        if (completionIcon == null && completionRoot != null)
            completionIcon = Find(completionRoot.transform, "Image")?.GetComponent<Image>();
        if (completionIcon == null && completionButton != null)
            completionIcon = completionButton.GetComponent<Image>();
        if (rows == null || rows.Length != 3) rows = new TankRow[3];
        for (int i = 0; i < 3; i++)
        {
            rows[i] ??= new TankRow();
            if (rows[i].Root == null) rows[i].Root = Find(root, $"CultureTankRow_{i + 1}")?.gameObject;
            rows[i].Bind();
        }
    }

    private void BindStorageScrollView()
    {
        if (storageScrollRect == null)
            return;

        // 씬/프리팹에서 설정한 Viewport/Content RectTransform 값은 절대 변경하지 않습니다.
        // ScrollRect 참조가 비어 있을 때만 연결하고, 스크롤 기능만 활성화합니다.
        if (storageScrollRect.content == null && storageContentRoot is RectTransform contentRect)
            storageScrollRect.content = contentRect;

        if (storageScrollRect.viewport == null)
        {
            Transform viewportTransform = Find(storageScrollRect.transform, "Viewport");
            if (viewportTransform != null)
                storageScrollRect.viewport = viewportTransform as RectTransform;
        }

        storageScrollRect.horizontal = false;
        storageScrollRect.vertical = true;

        if (storageVerticalScrollbar != null)
        {
            storageVerticalScrollbar.gameObject.SetActive(true);
            storageScrollRect.verticalScrollbar = storageVerticalScrollbar;
            storageScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }
    }

    private void BindButtons()
    {
        if (backButton != null) { backButton.onClick.RemoveListener(Close); backButton.onClick.AddListener(Close); }
        if (combineButton != null) { combineButton.onClick.RemoveListener(Combine); combineButton.onClick.AddListener(Combine); }
        if (completionButton != null) { completionButton.onClick.RemoveListener(ClaimCompletion); completionButton.onClick.AddListener(ClaimCompletion); }
        for (int i = 0; i < rows.Length; i++)
        {
            int index = i;
            if (rows[i]?.Button == null) continue;
            rows[i].Button.onClick.RemoveAllListeners(); rows[i].Button.onClick.AddListener(() => SelectRow(index));
        }
    }

    private void EnsureStorageSlots()
    {
        if (storageContentRoot == null || storageSlotPrefab == null)
            return;

        // Content 아래에 미리 배치한 StorageSlotUI_0~35를 우선 재사용합니다.
        // 기본 36칸이 이미 존재하면 새 슬롯을 만들지 않고, 36종류를 초과할 때만 추가 생성합니다.
        RegisterExistingStorageSlots();

        int targetCount = Mathf.Max(StorageMinimumSlotCount, storageSlots.Count);
        EnsureStorageSlotCount(targetCount);
    }

    private void RegisterExistingStorageSlots()
    {
        if (storageContentRoot == null)
            return;

        storageSlots.RemoveAll(slot => slot == null);
        if (storageSlots.Count > 0)
            return;

        for (int i = 0; i < storageContentRoot.childCount; i++)
        {
            Transform child = storageContentRoot.GetChild(i);
            BattleBagItemSlotUI slot = child.GetComponent<BattleBagItemSlotUI>();
            if (slot == null)
                continue;

            storageSlots.Add(slot);
        }
    }

    private void EnsureStorageSlotCount(int targetCount)
    {
        if (storageContentRoot == null || storageSlotPrefab == null)
            return;

        storageSlots.RemoveAll(slot => slot == null);

        while (storageSlots.Count < targetCount)
        {
            int index = storageSlots.Count;
            BattleBagItemSlotUI slot = Instantiate(storageSlotPrefab, storageContentRoot, false);
            slot.name = $"{storageSlotPrefab.name}_{index}";
            slot.gameObject.SetActive(true);
            slot.Clear(null, null, null);
            storageSlots.Add(slot);
        }

        for (int i = 0; i < storageSlots.Count; i++)
            storageSlots[i].gameObject.SetActive(i < targetCount);
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private List<BagItemStack> BuildStorageStacksIncludingReserved(LobbyRuntimeData lobby)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var activeIds = new HashSet<string>(StringComparer.Ordinal);
        var discoveryOrder = new List<string>();

        if (lobby?.BagItemIds != null)
        {
            for (int i = 0; i < lobby.BagItemIds.Count; i++)
            {
                string rawId = lobby.BagItemIds[i];
                if (string.IsNullOrWhiteSpace(rawId))
                    continue;

                string itemId = rawId.Trim();
                if (activeIds.Add(itemId))
                    discoveryOrder.Add(itemId);

                counts.TryGetValue(itemId, out int count);
                counts[itemId] = count + 1;
            }
        }

        if (lobby?.CultureTankResearches != null)
        {
            for (int i = 0; i < lobby.CultureTankResearches.Count; i++)
            {
                CultureTankResearchRuntimeData research = lobby.CultureTankResearches[i];
                if (research == null || string.IsNullOrWhiteSpace(research.ItemId))
                    continue;

                string itemId = research.ItemId.Trim();
                if (activeIds.Add(itemId))
                    discoveryOrder.Add(itemId);

                if (!counts.ContainsKey(itemId))
                    counts[itemId] = 0;
            }
        }

        // 작업대에 임시 등록한 재료는 실제 확정 소비 전까지 Storage의 원래 슬롯 위치를 유지합니다.
        // 취소 시에도 같은 슬롯에서 0 -> 1로 돌아오도록, 현재 존재하는 ID의 표시 순서를 캐시합니다.
        storageItemOrder.RemoveAll(itemId => !activeIds.Contains(itemId));

        var orderedIds = new HashSet<string>(storageItemOrder, StringComparer.Ordinal);
        for (int i = 0; i < discoveryOrder.Count; i++)
        {
            string itemId = discoveryOrder[i];
            if (orderedIds.Add(itemId))
                storageItemOrder.Add(itemId);
        }

        var stacks = new List<BagItemStack>(storageItemOrder.Count);
        for (int i = 0; i < storageItemOrder.Count; i++)
        {
            string itemId = storageItemOrder[i];
            if (!activeIds.Contains(itemId))
                continue;

            counts.TryGetValue(itemId, out int count);
            stacks.Add(new BagItemStack(itemId, count));
        }

        return stacks;
    }

    private static int FindFirstEmptyTankIndex(LobbyRuntimeData lobby)
    {
        if (lobby == null)
            return -1;

        for (int i = 0; i < 3; i++)
        {
            if (!CultureTankResearchService.TryGetTank(lobby, GetSlotId(i), out _))
                return i;
        }

        return -1;
    }

    private static bool HasEmptyTankSlot(LobbyRuntimeData lobby) => FindFirstEmptyTankIndex(lobby) >= 0;

    private static string GetSlotId(int index) => $"CultureTank{index + 1}";
    public static bool ShouldShowCompletionRoot(bool hasCompletedCombination) => true;
    public static bool CanSelectInventoryItem(bool hasSelectedRow, bool canMutate, bool hasCompletedCombination) =>
        hasSelectedRow && canMutate && !hasCompletedCombination;
    private static LobbyRuntimeData GetLobby() => DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
    private static bool CanMutate() => SteamLobbySharedStateSynchronizer.Instance == null || SteamLobbySharedStateSynchronizer.Instance.CanLocalPlayerMutateHostOnlyState();
    private static void SaveAndPublish() { SaveSystem.Instance?.SaveCurrentProgress(); BattleBagPanelUI.RefreshAll(); SteamLobbySharedStateSynchronizer.Instance?.PublishHostSnapshotAfterLocalMutation(); }

    [Serializable]
    private sealed class TankRow
    {
        [SerializeField] private GameObject root; [SerializeField] private Image background; [SerializeField] private Button button;
        [SerializeField] private TMP_Text label; [SerializeField] private TMP_Text stateLabel; [SerializeField] private Image itemIcon;
        public GameObject Root { get => root; set => root = value; }
        public Image Background => background; public Button Button => button;
        public TMP_Text Label => label; public TMP_Text StateLabel => stateLabel;
        public void Bind()
        {
            if (root == null) return;
            if (background == null) background = Find(root.transform, "back")?.GetComponent<Image>();
            if (background == null) background = root.GetComponent<Image>();
            if (button == null) button = root.GetComponent<Button>() ?? root.AddComponent<Button>();
            Image clickSurface = root.GetComponent<Image>();
            if (clickSurface == null)
            {
                clickSurface = root.AddComponent<Image>();
                clickSurface.color = Color.clear;
            }
            clickSurface.raycastTarget = true;
            button.targetGraphic = clickSurface;
            if (label == null) label = Find(root.transform, "Label")?.GetComponent<TMP_Text>() ?? root.GetComponentInChildren<TMP_Text>(true);
            if (stateLabel == null) stateLabel = Find(root.transform, "StateLabel")?.GetComponent<TMP_Text>();
            // CultureTankRow 안에 이미 배치된 Icon 오브젝트를 재료 아이콘 표시용으로 사용합니다.
            // 런타임에 ItemIcon 오브젝트를 새로 만들지 않습니다.
            if (itemIcon == null) itemIcon = Find(root.transform, "Icon")?.GetComponent<Image>();
            if (itemIcon == null) itemIcon = Find(root.transform, "icon")?.GetComponent<Image>();
            if (itemIcon != null) itemIcon.raycastTarget = false;
        }
        public void SetIcon(Sprite icon)
        {
            if (itemIcon == null) return;

            bool hasIcon = icon != null;
            itemIcon.sprite = icon;
            itemIcon.preserveAspect = true;
            itemIcon.enabled = hasIcon;
            itemIcon.gameObject.SetActive(hasIcon);
        }
    }

}
