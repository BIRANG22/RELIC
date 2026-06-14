using Relic.Gameplay.Data;
using UnityEngine;

public class RelicEquipPanelUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private RelicIconUI inventorySlotPrefab;

    [Header("Equipped Slots")]
    [SerializeField] private EquippedRelicSlotUI[] equippedSlots;

    [Header("Tooltip")]
    [SerializeField] private EquippedSkillPanelUI tooltipPanelOwner;

    private string selectedCharacterId;
    private int selectedRelicSlotIndex = -1;
    private RelicIconUI selectedInventoryRelicIcon;

    private void Awake()
    {
        ResolveTooltipPanelOwner();
        InitSlots();
    }

    private void OnEnable()
    {
        ResolveTooltipPanelOwner();
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
        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(this);

        selectedCharacterId = characterId;
        selectedRelicSlotIndex = relicSlotIndex;
        UpdateEquippedSlotSelectionVisuals();

        Debug.Log(
            $"[RelicEquipPanelUI] 장착 슬롯 선택 / Character:{selectedCharacterId} / RelicSlot:{selectedRelicSlotIndex + 1}"
        );
    }

    public void SelectInventoryRelicIcon(RelicIconUI selectedIcon)
    {
        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(this);

        selectedInventoryRelicIcon = selectedIcon;
        UpdateInventorySelectionVisuals();
    }

    public void ResetSelectionState()
    {
        selectedCharacterId = null;
        selectedRelicSlotIndex = -1;
        selectedInventoryRelicIcon = null;
        UpdateEquippedSlotSelectionVisuals();
        UpdateInventorySelectionVisuals();
    }

    public void SelectRelic(string relicId)
    {
        Debug.Log(
            $"[RelicEquipPanelUI] 유물 클릭 / Character:{selectedCharacterId} / RelicSlot:{selectedRelicSlotIndex + 1} / Relic:{relicId}"
        );

        if (string.IsNullOrWhiteSpace(selectedCharacterId))
        {
            Debug.LogWarning("[RelicEquipPanelUI] 먼저 유물 장착 슬롯을 선택해야 합니다.");
            return;
        }

        if (selectedRelicSlotIndex < 0)
        {
            Debug.LogWarning("[RelicEquipPanelUI] 먼저 유물 장착 슬롯을 선택해야 합니다.");
            return;
        }

        EquipRelic(selectedCharacterId, selectedRelicSlotIndex, relicId);
    }

    public void ShowRelicTooltip(string relicId, RectTransform hoveredSlotRect)
    {
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

    private void EquipRelic(string characterId, int relicSlotIndex, string relicId)
    {
        if (DataManager.Instance == null)
            return;

        BattleRuntimeData battleRuntimeData =
            DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        RelicEquipService service = new RelicEquipService(
            DataManager.Instance.CharacterRuntimeStore,
            battleRuntimeData
        );

        if (service.EquipRelic(characterId, relicSlotIndex, relicId))
        {
            ResetSelectionState();
            Refresh();
        }
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

        selectedInventoryRelicIcon = null;

        for (int i = inventoryContent.childCount - 1; i >= 0; i--)
            Destroy(inventoryContent.GetChild(i).gameObject);

        if (DataManager.Instance == null)
            return;

        BattleRuntimeData runtime =
            DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.OwnedRelicIds == null)
            return;

        for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
        {
            string relicId = runtime.OwnedRelicIds[i];

            if (string.IsNullOrWhiteSpace(relicId))
                continue;

            RelicIconUI icon = Instantiate(inventorySlotPrefab, inventoryContent);
            icon.Setup(relicId, this);
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
}
