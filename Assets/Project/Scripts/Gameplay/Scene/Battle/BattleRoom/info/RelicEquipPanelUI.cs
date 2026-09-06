using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class RelicEquipPanelUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private RelicIconUI inventorySlotPrefab;
    [SerializeField] private bool useVerticalInventoryLayout = true;
    [SerializeField] private float inventorySlotSpacing = 8f;
    [SerializeField] private Vector2 fallbackSlotSize = new Vector2(64f, 64f);

    [Header("Equipped Slots")]
    [SerializeField] private EquippedRelicSlotUI[] equippedSlots;

    [Header("Tooltip")]
    [SerializeField] private EquippedSkillPanelUI tooltipPanelOwner;
    [SerializeField] private InventoryRuntimeContextProvider runtimeContextProvider;

    private string selectedCharacterId;
    private int selectedRelicSlotIndex = -1;
    private RelicIconUI selectedInventoryRelicIcon;

    [SerializeField] private bool lockEditInBattleRoom = true;
    [SerializeField] private string battleRoomLockMessage = "전투 중에는 유물을 변경할 수 없습니다.";

    private void Awake()
    {
        ResolveTooltipPanelOwner();
        EnsureInventoryVerticalLayout();
        InitSlots();
    }

    private void OnEnable()
    {
        ResolveTooltipPanelOwner();
        EnsureInventoryVerticalLayout();
        ResetSelectionState();
        Refresh();
    }

    private void OnDisable()
    {
        ResetSelectionState();
        HideRelicTooltip();
    }

    private void InitSlots()
    {
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] != null)
                equippedSlots[i].Init(this);
        }
    }

    public void SelectEquipSlot(string characterId, int relicSlotIndex)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(this);

        if (!CanLocalPlayerEditCharacter(characterId))
            return;

        selectedCharacterId = characterId;
        selectedRelicSlotIndex = relicSlotIndex;
        UpdateEquippedSlotSelectionVisuals();

        Debug.Log($"[RelicEquipPanelUI] 장착 슬롯 선택 / Character:{selectedCharacterId} / RelicSlot:{selectedRelicSlotIndex + 1}");
    }

    public void SelectInventoryRelicIcon(RelicIconUI selectedIcon)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(this);

        selectedInventoryRelicIcon = selectedIcon;
        UpdateInventorySelectionVisuals();
        UpdateEmptyEquipSlotHighlights();
    }

    public void ResetSelectionState()
    {
        selectedCharacterId = null;
        selectedRelicSlotIndex = -1;
        selectedInventoryRelicIcon = null;
        UpdateEquippedSlotSelectionVisuals();
        UpdateInventorySelectionVisuals();
        UpdateEmptyEquipSlotHighlights();
    }

    public void SelectRelic(string relicId)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        Debug.Log($"[RelicEquipPanelUI] 유물 클릭 / Character:{selectedCharacterId} / RelicSlot:{selectedRelicSlotIndex + 1} / Relic:{relicId}");

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (string.IsNullOrWhiteSpace(selectedCharacterId))
        {
            Debug.Log("[RelicEquipPanelUI] 유물 먼저 선택됨. 장착할 유물 슬롯을 선택하면 장착됩니다.");
            return;
        }

        if (selectedRelicSlotIndex < 0)
        {
            Debug.Log("[RelicEquipPanelUI] 유물 먼저 선택됨. 장착할 유물 슬롯을 선택하면 장착됩니다.");
            return;
        }

        EquipRelic(selectedCharacterId, selectedRelicSlotIndex, relicId);
    }

    public bool EquipSelectedInventoryRelicToSlot(string characterId, int relicSlotIndex)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return false;

        if (selectedInventoryRelicIcon == null)
            return false;

        string relicId = selectedInventoryRelicIcon.RelicId;

        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        selectedCharacterId = characterId;
        selectedRelicSlotIndex = relicSlotIndex;
        UpdateEquippedSlotSelectionVisuals();

        EquipRelic(characterId, relicSlotIndex, relicId);
        return true;
    }

    public void ShowRelicTooltip(string relicId, RectTransform hoveredSlotRect)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        ResolveTooltipPanelOwner();

        if (tooltipPanelOwner == null)
            return;

        tooltipPanelOwner.ShowRelicTooltip(relicId, hoveredSlotRect);
    }

    public void HideRelicTooltip()
    {
        if (tooltipPanelOwner == null)
            return;

        tooltipPanelOwner.HideSkillTooltip();
    }

    private bool EquipRelic(string characterId, int relicSlotIndex, string relicId)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return false;

        if (CheckRelicEditLocked())
            return false;

        if (TryRequestNetworkEquipRelic(
                characterId,
                relicSlotIndex,
                relicId,
                out bool networkEquipResult))
        {
            if (networkEquipResult)
                ResetSelectionState();

            return networkEquipResult;
        }

        if (DataManager.Instance == null)
            return false;

        IInventoryRuntimeContext context = ResolveRuntimeContext();
        if (context == null)
            return false;

        RelicEquipService service = new RelicEquipService(
            DataManager.Instance.CharacterRuntimeStore,
            context.OwnedRelicIds,
            DataManager.Instance.RelicDatabase
        );

        if (service.EquipRelic(characterId, relicSlotIndex, relicId))
        {
            ResetSelectionState();
            Refresh();
            return true;
        }

        return false;
    }

    public void Refresh()
    {
        RefreshInventory();
        RefreshEquippedSlots();
    }

    private void RefreshInventory()
    {
        if (inventoryContent == null || inventorySlotPrefab == null)
            return;

        EnsureInventoryVerticalLayout();
        selectedInventoryRelicIcon = null;
        UpdateEmptyEquipSlotHighlights();

        ClearInventoryIcons();

        if (DataManager.Instance == null)
            return;

        IInventoryRuntimeContext context = ResolveRuntimeContext();
        if (context == null)
            return;

        NormalizeOwnedRelicIds(context.OwnedRelicIds);

        HashSet<string> displayedRelicIds = new();

        for (int i = 0; i < context.OwnedRelicIds.Count; i++)
        {
            string relicId = context.OwnedRelicIds[i];

            if (string.IsNullOrWhiteSpace(relicId))
                continue;

            relicId = relicId.Trim();

            if (!displayedRelicIds.Add(relicId))
                continue;

            RelicIconUI icon = Instantiate(inventorySlotPrefab, inventoryContent);
            icon.Setup(relicId, this);
            EnsureInventoryIconLayoutElement(icon);
        }

        RebuildInventoryLayout();
        ScheduleRebuildInventoryLayout();
    }

    private void ClearInventoryIcons()
    {
        if (inventoryContent == null)
            return;

        for (int i = inventoryContent.childCount - 1; i >= 0; i--)
        {
            Transform child = inventoryContent.GetChild(i);

            if (child == null)
                continue;

            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void EnsureInventoryVerticalLayout()
    {
        if (!useVerticalInventoryLayout || inventoryContent == null)
            return;

        GameObject contentObject = inventoryContent.gameObject;

        GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
        if (grid != null)
            grid.enabled = false;

        HorizontalLayoutGroup horizontal = contentObject.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
            horizontal.enabled = false;

        VerticalLayoutGroup vertical = contentObject.GetComponent<VerticalLayoutGroup>();
        if (vertical == null)
            vertical = contentObject.AddComponent<VerticalLayoutGroup>();

        vertical.enabled = true;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.spacing = inventorySlotSpacing;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = false;
        vertical.childForceExpandHeight = false;
        vertical.childScaleWidth = false;
        vertical.childScaleHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureInventoryIconLayoutElement(RelicIconUI icon)
    {
        if (icon == null)
            return;

        RectTransform rect = icon.GetComponent<RectTransform>();
        LayoutElement layoutElement = icon.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = icon.gameObject.AddComponent<LayoutElement>();

        Vector2 size = fallbackSlotSize;

        if (rect != null)
        {
            if (rect.sizeDelta.x > 0f)
                size.x = rect.sizeDelta.x;

            if (rect.sizeDelta.y > 0f)
                size.y = rect.sizeDelta.y;
        }

        layoutElement.ignoreLayout = false;
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void RebuildInventoryLayout()
    {
        if (inventoryContent is RectTransform rect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void ScheduleRebuildInventoryLayout()
    {
        if (!isActiveAndEnabled)
            return;

        StopCoroutine(nameof(RebuildInventoryLayoutNextFrame));
        StartCoroutine(nameof(RebuildInventoryLayoutNextFrame));
    }

    private IEnumerator RebuildInventoryLayoutNextFrame()
    {
        yield return null;
        RebuildInventoryLayout();
        Canvas.ForceUpdateCanvases();
        RebuildInventoryLayout();
    }

    private static void NormalizeOwnedRelicIds(IList<string> ownedRelicIds)
    {
        if (ownedRelicIds == null)
            return;

        HashSet<string> uniqueIds = new();

        for (int i = ownedRelicIds.Count - 1; i >= 0; i--)
        {
            string relicId = ownedRelicIds[i];

            if (string.IsNullOrWhiteSpace(relicId))
            {
                ownedRelicIds.RemoveAt(i);
                continue;
            }

            relicId = relicId.Trim();

            if (!uniqueIds.Add(relicId))
            {
                ownedRelicIds.RemoveAt(i);
                continue;
            }

            ownedRelicIds[i] = relicId;
        }
    }

    public static void RefreshAll()
    {
        RelicEquipPanelUI[] panels = Object.FindObjectsByType<RelicEquipPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].Refresh();
        }
    }

    private void RefreshEquippedSlots()
    {
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            if (equippedSlots[i] != null)
                equippedSlots[i].Refresh();
        }

        UpdateEquippedSlotSelectionVisuals();
    }

    private void UpdateInventorySelectionVisuals()
    {
        if (inventoryContent == null)
            return;

        for (int i = inventoryContent.childCount - 1; i >= 0; i--)
        {
            RelicIconUI icon = inventoryContent.GetChild(i).GetComponent<RelicIconUI>();
            if (icon != null)
                icon.SetSelected(icon == selectedInventoryRelicIcon);
        }
    }

    private void UpdateEmptyEquipSlotHighlights()
    {
        if (equippedSlots == null)
            return;

        bool hasSelectedRelic = TryGetSelectedRelicType(out bool isActiveRelic);

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            EquippedRelicSlotUI slot = equippedSlots[i];
            if (slot == null)
                continue;

            bool isActiveSlot =
                slot.RelicSlotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;

            bool isCompatibleSlot =
                hasSelectedRelic && isActiveRelic == isActiveSlot;

            slot.SetEquipAvailableHighlight(isCompatibleSlot);
        }
    }

    private bool TryGetSelectedRelicType(out bool isActiveRelic)
    {
        isActiveRelic = false;

        if (selectedInventoryRelicIcon == null || DataManager.Instance == null)
            return false;

        string relicId = selectedInventoryRelicIcon.RelicId;
        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        if (DataManager.Instance.CompoundDatabase != null &&
            DataManager.Instance.CompoundDatabase.TryGet(relicId, out _))
        {
            isActiveRelic = true;
            return true;
        }

        if (DataManager.Instance.RelicDatabase != null &&
            DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relic))
        {
            isActiveRelic = ActiveRelicEffectResolver.IsActiveRelic(relic);
            return true;
        }

        return false;
    }

    private void UpdateEquippedSlotSelectionVisuals()
    {
        if (equippedSlots == null)
            return;

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            EquippedRelicSlotUI slot = equippedSlots[i];
            if (slot == null)
                continue;

            bool selected = !string.IsNullOrWhiteSpace(selectedCharacterId) &&
                            slot.RelicSlotIndex == selectedRelicSlotIndex &&
                            GetCharacterIdByPartySlot(slot.PartySlotIndex) == selectedCharacterId;

            slot.SetSelected(selected);
        }
    }

    private string GetCharacterIdByPartySlot(int partySlotIndex)
    {
        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return null;

        return DataManager.Instance.PartyRuntimeStore.GetCharacterId(partySlotIndex);
    }

    private void ResolveTooltipPanelOwner()
    {
        if (tooltipPanelOwner != null)
            return;

        tooltipPanelOwner = GetComponentInParent<EquippedSkillPanelUI>();

        if (tooltipPanelOwner == null)
            tooltipPanelOwner = FindFirstObjectByType<EquippedSkillPanelUI>();
    }

    public void UnequipRelic(string characterId, int relicSlotIndex)
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (CheckRelicEditLocked())
            return;

        if (TryRequestNetworkUnequipRelic(
                characterId,
                relicSlotIndex,
                out bool networkUnequipResult))
        {
            if (networkUnequipResult)
            {
                selectedCharacterId = null;
                selectedRelicSlotIndex = -1;
            }

            return;
        }

        if (DataManager.Instance == null)
            return;

        IInventoryRuntimeContext context = ResolveRuntimeContext();
        if (context == null)
            return;

        RelicEquipService service = new RelicEquipService(
            DataManager.Instance.CharacterRuntimeStore,
            context.OwnedRelicIds,
            DataManager.Instance.RelicDatabase
        );

        if (service.UnequipRelic(characterId, relicSlotIndex))
        {
            selectedCharacterId = null;
            selectedRelicSlotIndex = -1;
            Refresh();
        }
    }

    private bool IsRelicEditLocked()
    {
        SteamBattleStateSynchronizer battleSynchronizer =
            SteamBattleStateSynchronizer.Instance;

        if (battleSynchronizer != null && battleSynchronizer.IsNetworkBattleActive)
            return false;

        if (!lockEditInBattleRoom)
            return false;

        BattleRoomLoader battleRoomLoader =
            Object.FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);

        return battleRoomLoader != null && battleRoomLoader.gameObject.activeInHierarchy;
    }

    private bool CheckRelicEditLocked()
    {
        if (!IsRelicEditLocked())
            return false;

        BattleWarningUI.ShowMessage(battleRoomLockMessage);
        return true;
    }

    private static bool CanLocalPlayerEditCharacter(string characterId)
    {
        SteamBattleStateSynchronizer battleSynchronizer =
            SteamBattleStateSynchronizer.Instance;

        if (battleSynchronizer != null && battleSynchronizer.IsNetworkBattleActive)
            return battleSynchronizer.CanLocalPlayerEditCharacter(characterId);

        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null ||
               !synchronizer.IsNetworkSharedStateActive ||
               synchronizer.CanLocalPlayerEditCharacter(characterId);
    }

    private static bool TryRequestNetworkEquipRelic(
        string characterId,
        int relicSlotIndex,
        string relicId,
        out bool requestSent)
    {
        requestSent = false;
        SteamBattleStateSynchronizer battleSynchronizer =
            SteamBattleStateSynchronizer.Instance;

        if (battleSynchronizer != null && battleSynchronizer.IsNetworkBattleActive)
        {
            requestSent = battleSynchronizer.RequestEquipRelic(
                characterId,
                relicSlotIndex,
                relicId);
            return true;
        }

        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;

        if (synchronizer == null || !synchronizer.IsNetworkSharedStateActive)
            return false;

        requestSent = synchronizer.RequestEquipRelic(
            characterId,
            relicSlotIndex,
            relicId);
        return true;
    }

    private static bool TryRequestNetworkUnequipRelic(
        string characterId,
        int relicSlotIndex,
        out bool requestSent)
    {
        requestSent = false;
        SteamBattleStateSynchronizer battleSynchronizer =
            SteamBattleStateSynchronizer.Instance;

        if (battleSynchronizer != null && battleSynchronizer.IsNetworkBattleActive)
        {
            requestSent = battleSynchronizer.RequestUnequipRelic(
                characterId,
                relicSlotIndex);
            return true;
        }

        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;

        if (synchronizer == null || !synchronizer.IsNetworkSharedStateActive)
            return false;

        requestSent = synchronizer.RequestUnequipRelic(
            characterId,
            relicSlotIndex);
        return true;
    }

    private IInventoryRuntimeContext ResolveRuntimeContext()
    {
        if (runtimeContextProvider == null)
            runtimeContextProvider = GetComponentInParent<InventoryRuntimeContextProvider>(true);

        if (runtimeContextProvider != null)
            return runtimeContextProvider.GetContext();

        if (DataManager.Instance == null)
            return null;

        return InventoryRuntimeContext.ForBattle(DataManager.Instance.BattleRuntimeStore.GetOrCreate());
    }
}
