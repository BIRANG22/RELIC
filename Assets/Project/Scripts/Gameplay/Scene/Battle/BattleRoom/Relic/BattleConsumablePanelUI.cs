using System;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleConsumablePanelUI : MonoBehaviour
{
    [Serializable]
    private sealed class SlotView
    {
        public Button Button;
        public Image Icon;
        public GameObject UsedOverlay;
    }

    [SerializeField] private SlotView[] slots = new SlotView[PartyConsumableRuntime.SlotCount];
    [SerializeField] private Transform slotRoot;
    [SerializeField] private ActiveRelicTargetingController targetingController;
    [SerializeField] private BattleTurnExecutor turnExecutor;

    private ActiveRelicService effectService;
    private bool lastReservationPhase;

    private void OnEnable() => Refresh();
    private void Update()
    {
        if (lastReservationPhase != (turnExecutor == null || turnExecutor.CanAcceptPlayerInput)) Refresh();
    }

    private void RefreshAfterUse()
    {
        FindFirstObjectByType<BattleRoomLoader>()?.RefreshBattleHUDs();
        Refresh();
    }

    private void Awake()
    {
        targetingController ??= FindFirstObjectByType<ActiveRelicTargetingController>(FindObjectsInactive.Include);
        turnExecutor ??= FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
        AutoBindSlots();
        BindButtons();
        Refresh();
    }

    private void OnDestroy()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
            slots[i]?.Button?.onClick.RemoveAllListeners();
    }

    public void Refresh()
    {
        AutoBindSlots();
        BattleRuntimeData battle = DataManager.Instance?.BattleRuntimeStore?.Get();
        if (battle != null) PartyConsumableRuntime.NormalizeBattle(battle);
        bool reservationPhase = turnExecutor == null || turnExecutor.CanAcceptPlayerInput;
        lastReservationPhase = reservationPhase;
        bool hasLivingMember = BattleConsumableService.GetPartyMembers(DataManager.Instance?.PartyRuntimeStore, DataManager.Instance?.CharacterRuntimeStore).Exists(member => !member.IsDead);
        for (int i = 0; i < PartyConsumableRuntime.SlotCount; i++)
        {
            SlotView view = slots != null && i < slots.Length ? slots[i] : null;
            if (view == null) continue;
            BattleConsumableSlotRuntimeData slot = battle?.ConsumableSlots != null && i < battle.ConsumableSlots.Count
                ? battle.ConsumableSlots[i]
                : null;
            string itemId = slot?.ItemId ?? string.Empty;
            bool used = slot?.IsUsed == true;
            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(itemId))
                DataManager.Instance?.RelicIconDatabase?.TryGetIcon(itemId, out icon);
            if (view.Icon != null)
            {
                view.Icon.sprite = icon;
                view.Icon.enabled = icon != null;
                view.Icon.color = used ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
            if (view.UsedOverlay != null) view.UsedOverlay.SetActive(used);
            if (view.Button != null)
                view.Button.interactable = reservationPhase &&
                                           hasLivingMember &&
                                           !used && !string.IsNullOrWhiteSpace(itemId);
        }
    }

    private void AutoBindSlots()
    {
        slotRoot ??= transform;
        if (slots == null || slots.Length != PartyConsumableRuntime.SlotCount)
            slots = new SlotView[PartyConsumableRuntime.SlotCount];

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] ??= new SlotView();
            Transform root = slotRoot.Find($"ConsumableSlot{i + 1:00}");
            if (root == null) continue;
            Transform legacyLabel = root.Find("Compound");
            if (legacyLabel != null) legacyLabel.gameObject.SetActive(false);
            Transform legacyKey = root.Find("Key");
            if (legacyKey != null) legacyKey.gameObject.SetActive(false);
            slots[i].Button ??= root.GetComponent<Button>();
            Transform icon = root.Find("Icon");
            slots[i].Icon ??= icon != null ? icon.GetComponent<Image>() : null;
            Transform used = root.Find("UsedOverlay");
            slots[i].UsedOverlay ??= used != null ? used.gameObject : null;
        }
    }

    private void BindButtons()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = i;
            if (slots[i]?.Button == null) continue;
            slots[i].Button.onClick.RemoveAllListeners();
            slots[i].Button.onClick.AddListener(() => UseSlot(slotIndex));
        }
    }

    private void UseSlot(int slotIndex)
    {
        BattleRuntimeData battle = DataManager.Instance?.BattleRuntimeStore?.Get();
        if (battle == null || slotIndex < 0 || slotIndex >= PartyConsumableRuntime.SlotCount || DataManager.Instance?.CompoundDatabase == null) return;
        PartyConsumableRuntime.NormalizeBattle(battle);
        BattleConsumableSlotRuntimeData slot = battle.ConsumableSlots[slotIndex];
        string itemId = slot.ItemId;
        effectService ??= new ActiveRelicService(DataManager.Instance.CompoundDatabase);
        ActiveRelicAvailability availability = effectService.GetConsumableAvailability(itemId);
        bool reservationPhase = turnExecutor == null || turnExecutor.CanAcceptPlayerInput;
        UseConsumableCommand command = new()
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SlotIndex = slotIndex,
            ItemId = itemId
        };

        if (!availability.CanUse || !reservationPhase || slot.IsUsed) return;
        targetingController?.CancelTargeting();
        if (availability.RequiresTarget)
        {
            if (!reservationPhase || targetingController == null ||
                !targetingController.BeginConsumableTargeting(
                    effectService, battle, command, availability, RefreshAfterUse))
                BattleWarningUI.ShowMessage(availability.Message);
            return;
        }


        if (SteamBattleStateSynchronizer.TryHandleConsumableUse(command, out bool networkAccepted))
        {
            if (!networkAccepted)
                BattleWarningUI.ShowMessage("소모품 사용 요청이 거부되었습니다.");
            Refresh();
            return;
        }

        UseConsumableResult result = BattleConsumableService.TryUseParty(
            battle, command,
            BattleConsumableService.GetPartyMembers(DataManager.Instance.PartyRuntimeStore, DataManager.Instance.CharacterRuntimeStore),
            reservationPhase, effectService);
        if (!result.Succeeded)
            BattleWarningUI.ShowMessage(result.RejectReason.ToString());
        RefreshAfterUse();
    }
}
