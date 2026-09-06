using System;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum LobbyCultureTankPanelState { MissingData, Empty, Running, Completed }

[DisallowMultipleComponent]
public sealed class LobbyCultureTankController : MonoBehaviour
{
    [SerializeField] private string tankId;
    [SerializeField] private bool allowWorldInteraction;
    [SerializeField] private SpriteRenderer itemRenderer;
    [SerializeField] private Light2D tankLight;
    [SerializeField] private GameObject researchVfxRoot;

    public string TankId { get { AutoBind(); return tankId; } }

    private void Awake() { AutoBind(); RefreshNow(); }
    private void OnEnable() => RefreshNow();
    private void OnMouseUpAsButton() { if (allowWorldInteraction) Interact(); }

    public void Interact()
    {
        if (!CanLocalPlayerMutateHostOnlyState()) return;
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (!CultureTankResearchService.TryRemoveIngredient(lobby, TankId, out _)) return;
        SaveAndPublish();
        RefreshNow();
    }

    public bool TryStartResearchFromPanel(string itemId)
    {
        if (!CanLocalPlayerMutateHostOnlyState()) return false;
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (!CultureTankResearchService.TryPlaceIngredient(lobby, TankId, itemId, out string error))
        {
            Debug.LogWarning($"[LobbyCultureTankController] Failed to place ingredient. {error}");
            return false;
        }
        SaveAndPublish();
        RefreshNow();
        return true;
    }

    public string GetPanelLabel() => $"{GetPanelName()}\n{GetPanelStateText()}";
    public string GetPanelName() => FormatPanelTankName(TankId);
    public string GetPanelStateText() => GetPanelState() switch
    {
        LobbyCultureTankPanelState.Running => "재료 투입됨",
        LobbyCultureTankPanelState.MissingData => "데이터 없음",
        _ => "비어 있음"
    };

    public LobbyCultureTankPanelState GetPanelState()
    {
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null) return LobbyCultureTankPanelState.MissingData;
        return CultureTankResearchService.TryGetTank(lobby, TankId, out _)
            ? LobbyCultureTankPanelState.Running
            : LobbyCultureTankPanelState.Empty;
    }

    public bool TryGetPanelItemId(out string itemId)
    {
        itemId = string.Empty;
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (!CultureTankResearchService.TryGetTank(lobby, TankId, out CultureTankResearchRuntimeData slot)) return false;
        itemId = slot.ItemId;
        return true;
    }

    public void RefreshNow()
    {
        AutoBind();
        Sprite icon = null;
        bool occupied = TryGetPanelItemId(out string itemId);
        if (occupied) DataManager.Instance?.ItemIconDatabase?.TryGetIcon(itemId, out icon);
        if (itemRenderer != null) { itemRenderer.sprite = icon; itemRenderer.enabled = icon != null; }
        if (tankLight != null) tankLight.enabled = occupied;
        if (researchVfxRoot != null) researchVfxRoot.SetActive(false);
    }

    private void AutoBind()
    {
        if (string.IsNullOrWhiteSpace(tankId)) tankId = gameObject.name;
        if (itemRenderer == null) itemRenderer = transform.Find("Item")?.GetComponent<SpriteRenderer>();
        if (tankLight == null) tankLight = GetComponentInChildren<Light2D>(true);
    }

    private static string FormatPanelTankName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return GameLocalization.Get("lobby.culture_tank", "배양조");
        const string prefix = "CultureTank";
        string trimmed = value.Trim();
        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return trimmed;
        string suffix = trimmed[prefix.Length..].Trim();
        return string.IsNullOrEmpty(suffix)
            ? GameLocalization.Get("lobby.culture_tank", "배양조")
            : GameLocalization.Format("lobby.culture_tank_number", "배양조 {0}", suffix);
    }

    private static bool CanLocalPlayerMutateHostOnlyState() =>
        SteamLobbySharedStateSynchronizer.Instance == null ||
        SteamLobbySharedStateSynchronizer.Instance.CanLocalPlayerMutateHostOnlyState();

    private static void SaveAndPublish()
    {
        SaveSystem.Instance?.SaveCurrentProgress();
        BattleBagPanelUI.RefreshAll();
        SteamLobbySharedStateSynchronizer.Instance?.PublishHostSnapshotAfterLocalMutation();
    }
}
