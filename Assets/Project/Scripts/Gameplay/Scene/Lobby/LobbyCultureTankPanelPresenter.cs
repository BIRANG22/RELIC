using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyCultureTankPanelPresenter : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private Transform inventoryItemRoot;
    [SerializeField] private TankRow[] rows = new TankRow[3];
    [SerializeField] private Button combineButton;
    [SerializeField] private GameObject completionRoot;
    [SerializeField] private Button completionButton;
    [SerializeField] private Image completionIcon;

    private readonly List<BattleBagItemSlotUI> inventorySlots = new();
    private int selectedSlotIndex = -1;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    private void Awake() { BindSceneObjects(); BindButtons(); BindInventorySlots(); }
    private void OnEnable() { BindSceneObjects(); BindButtons(); BindInventorySlots(); RefreshAll(); }
    private void Update() { if (IsOpen) RefreshAll(); }

    public void Open()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this)) return;
        BindSceneObjects(); BindButtons();
        if (panelRoot == null) return;
        LobbyPositionModalInputBlocker.Block(this);
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
            if (row.Background != null) row.Background.color = selectedSlotIndex == i
                ? new Color(0.25f, 0.42f, 0.3f, 0.96f)
                : filled ? new Color(0.48f, 0.24f, 0.08f, 0.96f) : new Color(0.16f, 0.18f, 0.22f, 0.96f);
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
        if (!CanSelectInventoryItem(selectedSlotIndex >= 0, CanMutate(), hasCompletedCombination) ||
            string.IsNullOrWhiteSpace(itemId))
            return;
        if (!CultureTankResearchService.TryPlaceIngredient(lobby, GetSlotId(selectedSlotIndex), itemId, out string error))
        { Debug.LogWarning($"[LobbyCultureTankPanelPresenter] {error}"); return; }
        selectedSlotIndex = -1; SaveAndPublish(); RefreshAll();
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
        BindInventorySlots();
        LobbyRuntimeData lobby = GetLobby();
        IReadOnlyList<string> items = lobby?.BagItemIds;
        bool canSelect = CanSelectInventoryItem(
            selectedSlotIndex >= 0,
            CanMutate(),
            !string.IsNullOrWhiteSpace(lobby?.CompletedCultureTankCombinationId));
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            string id = items != null && i < items.Count ? items[i] : string.Empty;
            BattleBagItemSlotUI slot = inventorySlots[i];
            if (slot == null) continue;
            slot.Setup(id, null, null, null);
            Button button = slot.GetComponent<Button>();
            CultureTankInventorySlotClickRelay relay =
                slot.GetComponent<CultureTankInventorySlotClickRelay>() ??
                slot.gameObject.AddComponent<CultureTankInventorySlotClickRelay>();
            relay.Configure(button, id, canSelect && slot.HasItem, SelectInventoryItem);
        }
    }

    private void BindSceneObjects()
    {
        if (panelRoot == null) panelRoot = gameObject;
        Transform root = panelRoot.transform;
        if (backButton == null) backButton = Find(root, "BackButton")?.GetComponent<Button>();
        if (contentRoot == null) contentRoot = Find(root, "Content") as RectTransform;
        Transform inventory = Find(root, "Inventory");
        if (inventory == null) inventory = Find(root, "inventory");
        if (inventoryItemRoot == null) inventoryItemRoot = Find(inventory, "SlotRoot");
        if (inventoryItemRoot == null) inventoryItemRoot = Find(inventory, "itemRoot");
        if (inventoryItemRoot == null) inventoryItemRoot = Find(inventory, "item");
        if (combineButton == null)
            Debug.LogError("[LobbyCultureTankPanelPresenter] Combine Button inspector reference is missing.", this);
        if (completionRoot == null) completionRoot = Find(contentRoot, "completion")?.gameObject;
        if (completionButton == null && completionRoot != null)
            completionButton = completionRoot.GetComponentInChildren<Button>(true);
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
            if (rows[i].Root == null) rows[i].Root = Find(contentRoot, $"CultureTankRow_{i + 1}")?.gameObject;
            rows[i].Bind();
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

    private void BindInventorySlots()
    {
        if (inventoryItemRoot == null) return;
        BattleBagItemSlotUI[] foundSlots = inventoryItemRoot.GetComponentsInChildren<BattleBagItemSlotUI>(true);
        if (inventorySlots.Count == foundSlots.Length) return;
        inventorySlots.Clear();
        inventorySlots.AddRange(foundSlots);
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

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
            if (itemIcon == null) itemIcon = Find(root.transform, "ItemIcon")?.GetComponent<Image>();
            if (itemIcon == null) { GameObject go = new("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(root.transform, false); itemIcon = go.GetComponent<Image>(); itemIcon.raycastTarget = false; ((RectTransform)go.transform).sizeDelta = new Vector2(88, 88); }
        }
        public void SetIcon(Sprite icon) { if (itemIcon == null) return; itemIcon.sprite = icon; itemIcon.enabled = icon != null; itemIcon.preserveAspect = true; }
    }

}
