using UnityEngine;
using Relic.Gameplay.Data;

public class SpawnGridPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelObject;
    [SerializeField] private SpawnGridCell[] cells;
    [SerializeField] private PartySlot[] partySlots;

    private string pendingCharacterId;

    private void Awake()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                cells[i].Init(this, i);
        }

        Close();
    }

    public void OpenForCharacter(string characterId)
    {
        pendingCharacterId = characterId;

        if (panelObject != null)
            panelObject.SetActive(true);
        else
            gameObject.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        pendingCharacterId = null;

        if (panelObject != null)
            panelObject.SetActive(false);
    }

    public void SelectGrid(int gridIndex)
    {
        if (string.IsNullOrWhiteSpace(pendingCharacterId))
            return;

        if (DataManager.Instance == null)
            return;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore.IsGridUsed(gridIndex))
        {
            Debug.LogWarning($"[SpawnGridPanel] 이미 사용 중인 그리드입니다: {gridIndex}");
            return;
        }

        int slotIndex = partyStore.FindCharacterSlot(pendingCharacterId);

        if (slotIndex < 0)
            slotIndex = partyStore.FindEmptySlot();

        if (slotIndex < 0)
        {
            Debug.LogWarning("[SpawnGridPanel] 파티 슬롯이 가득 찼습니다.");
            return;
        }

        bool success = partyStore.SetSlot(slotIndex, pendingCharacterId, gridIndex);

        if (!success)
            return;

        if (partySlots != null &&
            slotIndex >= 0 &&
            slotIndex < partySlots.Length &&
            partySlots[slotIndex] != null)
        {
            partySlots[slotIndex].SetChar(pendingCharacterId);
        }

        Refresh();
        Close();
    }

    public void Refresh()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                cells[i].Refresh();
        }
    }
}