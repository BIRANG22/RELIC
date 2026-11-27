using Relic.Gameplay.Data;
using UnityEngine;

public sealed class LobbyConsumablePanelUI : MonoBehaviour
{
    [SerializeField] private LobbyEquipPanelUI equipPanel;
    [SerializeField] private LobbyInfoPanelUI infoPanel;
    [SerializeField] private LobbyConsumableSlotUI[] slots;

    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    private bool TryGetSelection(out string itemId)
    {
        itemId = null;
        return (infoPanel != null && infoPanel.isActiveAndEnabled && infoPanel.TryGetSelectedSharedCompound(out itemId)) ||
               (equipPanel != null && equipPanel.isActiveAndEnabled && equipPanel.TryGetSelectedSharedCompound(out itemId));
    }

    public void UseSlot(int index, bool unequip)
    {
        if (index < 0 || index >= PartyConsumableRuntime.SlotCount) return;
        var data = DataManager.Instance;
        var lobby = data?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null) return;
        bool selected = TryGetSelection(out string itemId);
        var network = SteamLobbySharedStateSynchronizer.Instance;
        bool online = network != null && network.IsNetworkSharedStateActive;
        bool changed;
        if (!unequip && selected)
        {
            if (data.CompoundDatabase == null || !data.CompoundDatabase.TryGet(itemId, out _)) return;
            changed = online ? network.RequestEquipConsumable(index, itemId) : PartyConsumableRuntime.TryEquip(lobby, index, itemId);
        }
        else
            changed = online ? network.RequestUnequipConsumable(index) : PartyConsumableRuntime.TryUnequip(lobby, index);
        if (!changed) return;
        infoPanel?.ClearSharedCompoundSelection();
        equipPanel?.ClearSharedCompoundSelection();
        LobbyEquipPanelUI.RefreshAllCharacterData();
        RelicEquipPanelUI.RefreshAll();
        SaveSystem.Instance?.SaveCurrentProgress();
        Refresh();
    }

    public void Refresh()
    {
        var lobby = DataManager.Instance?.LobbyRuntimeStore?.Get();
        bool selected = TryGetSelection(out _);
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            string itemId = lobby?.EquippedConsumableIds != null && i < lobby.EquippedConsumableIds.Length
                ? lobby.EquippedConsumableIds[i] : null;
            slots[i]?.Refresh(itemId, selected);
        }
    }
}
