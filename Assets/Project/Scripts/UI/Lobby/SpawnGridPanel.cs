using UnityEngine;
using Relic.Gameplay.Data;

public class SpawnGridPanel : MonoBehaviour
{
    [SerializeField] private SpawnGridCell[] cells;

    private int selectedPartySlotIndex = -1;

    private void Awake()
    {
        if (cells != null)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].Init(this, i);
            }
        }
    }

    private void Start()
    {
        AutoPlacePartyIfNeeded();
        Refresh();
    }

    private void OnEnable()
    {
        AutoPlacePartyIfNeeded();
        Refresh();
    }

    public void AutoPlacePartyIfNeeded()
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int partyIndex = 0; partyIndex < partyStore.MaxPartyCountValue; partyIndex++)
        {
            string characterId = partyStore.GetCharacterId(partyIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            int currentGridIndex = partyStore.GetGridIndex(partyIndex);

            if (currentGridIndex >= 0)
                continue;

            int emptyGridIndex = FindFirstEmptyGrid();

            if (emptyGridIndex < 0)
            {
                Debug.LogWarning("[SpawnGridPanel] 비어있는 그리드가 없습니다.");
                return;
            }

            partyStore.SetGridIndex(partyIndex, emptyGridIndex);
        }
    }

    public void OnClickCell(int gridIndex)
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        int clickedPartySlotIndex = FindPartySlotByGridIndex(gridIndex);

        if (clickedPartySlotIndex >= 0)
        {
            selectedPartySlotIndex = clickedPartySlotIndex;
            Refresh();
            return;
        }

        if (selectedPartySlotIndex < 0)
        {
            Debug.LogWarning("[SpawnGridPanel] 먼저 이동할 캐릭터를 선택하세요.");
            return;
        }

        bool success = partyStore.SetGridIndex(selectedPartySlotIndex, gridIndex);

        if (!success)
            return;

        selectedPartySlotIndex = -1;
        Refresh();
    }

    private int FindFirstEmptyGrid()
    {
        if (cells == null)
            return -1;

        if (DataManager.Instance == null)
            return -1;

        for (int i = 0; i < cells.Length; i++)
        {
            if (DataManager.Instance.PartyRuntimeStore.IsGridUsed(i))
                continue;

            return i;
        }

        return -1;
    }

    private int FindPartySlotByGridIndex(int gridIndex)
    {
        if (DataManager.Instance == null)
            return -1;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetGridIndex(i) == gridIndex)
                return i;
        }

        return -1;
    }

    public bool IsSelectedGrid(int gridIndex)
    {
        if (selectedPartySlotIndex < 0)
            return false;

        if (DataManager.Instance == null)
            return false;

        return DataManager.Instance.PartyRuntimeStore.GetGridIndex(selectedPartySlotIndex) == gridIndex;
    }

    public void Refresh()
    {
        if (cells == null)
            return;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                cells[i].Refresh();
        }
    }
}