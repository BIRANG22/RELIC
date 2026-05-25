using UnityEngine;

public class BattlePlacementController : MonoBehaviour
{
    [SerializeField] private BattlePlacementSelectUI selectUI;
    [SerializeField] private BattleUnitSpawner unitSpawner;

    [Header("Grid Click Mode")]
    [SerializeField] private Transform playerGridRoot;

    private int selectedGridIndex = -1;

    public void BeginPlacement()
    {
        selectedGridIndex = -1;

        SetGridPlacementMode(true);

        if (selectUI != null)
            selectUI.Close();

        Debug.Log("[BattlePlacementController] Placement mode started.");
    }

    public void SelectGrid(int gridIndex)
    {
        selectedGridIndex = gridIndex;

        Debug.Log($"[BattlePlacementController] Selected Grid: {gridIndex}");

        if (selectUI != null)
            selectUI.Open(this);
    }

    public void PlaceCharacter(int partySlotIndex)
    {
        if (selectedGridIndex < 0)
        {
            Debug.LogWarning("[BattlePlacementController] Grid is not selected.");
            return;
        }

        var partyStore = DataManager.Instance.PartyRuntimeStore;
        string characterId = partyStore.GetCharacterId(partySlotIndex);

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning($"[BattlePlacementController] CharacterId is empty. Slot: {partySlotIndex}");
            return;
        }

        if (!partyStore.SetGridIndex(partySlotIndex, selectedGridIndex))
            return;

        unitSpawner.SpawnSingleFromRuntimeData(partySlotIndex);

        if (selectUI != null)
            selectUI.Close();

        selectedGridIndex = -1;

        if (!NeedsPlacement())
            SetGridPlacementMode(false);

        Debug.Log($"[BattlePlacementController] Placed {characterId} at Grid_{selectedGridIndex:00}");
    }

    public bool NeedsPlacement()
    {
        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (partyStore.GetGridIndex(i) < 0)
                return true;
        }

        return false;
    }

    private void SetGridPlacementMode(bool value)
    {
        if (playerGridRoot == null)
            return;

        BattleGridClickHandler[] handlers =
            playerGridRoot.GetComponentsInChildren<BattleGridClickHandler>(true);

        foreach (var handler in handlers)
        {
            handler.SetPlacementMode(value);
        }
    }
}