using Relic.Gameplay.Data;
using UnityEngine;

public class RelicEquipPanelUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private RelicIconUI inventorySlotPrefab;

    [Header("Equipped Slots")]
    [SerializeField] private EquippedRelicSlotUI[] equippedSlots;

    private string selectedCharacterId;
    private int selectedRelicSlotIndex = -1;

    private void Awake()
    {
        InitSlots();
    }

    private void OnEnable()
    {
        Refresh();
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
        selectedCharacterId = characterId;
        selectedRelicSlotIndex = relicSlotIndex;

        Debug.Log(
            $"[RelicEquipPanelUI] 장착 슬롯 선택 / Character:{selectedCharacterId} / RelicSlot:{selectedRelicSlotIndex + 1}"
        );
    }

    public void SelectRelic(string relicId)
    {
        Debug.Log(
            $"[RelicEquipPanelUI] 유물 클릭 / Character:{selectedCharacterId} / RelicSlot:{selectedRelicSlotIndex + 1} / Relic:{relicId}"
        );

        if (string.IsNullOrWhiteSpace(selectedCharacterId))
        {
            Debug.LogWarning("[RelicEquipPanelUI] 먼저 렐릭 슬롯을 선택해야 합니다.");
            return;
        }

        if (selectedRelicSlotIndex < 0)
        {
            Debug.LogWarning("[RelicEquipPanelUI] 먼저 렐릭 슬롯을 선택해야 합니다.");
            return;
        }

        EquipRelic(selectedCharacterId, selectedRelicSlotIndex, relicId);
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
            selectedCharacterId = null;
            selectedRelicSlotIndex = -1;
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
    }
}