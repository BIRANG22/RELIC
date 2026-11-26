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

    private CharacterRuntimeData actor;
    private ActiveRelicService effectService;

    public void BindActor(CharacterRuntimeData value)
    {
        actor = value;
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
                view.Button.interactable = reservationPhase && actor != null && !actor.IsDead &&
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
        if (battle == null || actor == null || DataManager.Instance?.CompoundDatabase == null) return;
        PartyConsumableRuntime.NormalizeBattle(battle);
        BattleConsumableSlotRuntimeData slot = battle.ConsumableSlots[slotIndex];
        string itemId = slot.ItemId;
        effectService ??= new ActiveRelicService(DataManager.Instance.CompoundDatabase);
        ActiveRelicAvailability availability = effectService.GetConsumableAvailability(actor, itemId);
        bool reservationPhase = turnExecutor == null || turnExecutor.CanAcceptPlayerInput;
        UseConsumableCommand command = new()
        {
            RequestId = Guid.NewGuid().ToString("N"),
            ActorCharacterId = actor.CharacterId,
            SlotIndex = slotIndex,
            ItemId = itemId
        };

        if (availability.RequiresTarget)
        {
            if (!reservationPhase || targetingController == null ||
                !targetingController.BeginConsumableTargeting(
                    effectService, battle, command, actor, availability, Refresh))
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

        ActiveRelicUseResult effectResult = null;
        UseConsumableResult result = BattleConsumableService.TryUse(
            battle,
            command,
            actor,
            reservationPhase,
            () => (effectResult = effectService.TryUseConsumableImmediate(actor, itemId)).Succeeded);
        if (!result.Succeeded)
            BattleWarningUI.ShowMessage(effectResult?.Message ?? result.RejectReason.ToString());
        Refresh();
    }
}
