using Relic.Gameplay.Data;
using UnityEngine;

public class BattleTimelineController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI;
    [SerializeField] private ReserveTurnSlotUI[] reserveSlots;

    private int activeSlotIndex = -1;
    private CharacterRuntimeData selectedCharacter;
    private SkillMasterData selectedSkill;

    private void Awake()
    {
        if (timelineBarUI != null)
            timelineBarUI.Init(this);

        if (reserveSlots != null)
        {
            for (int i = 0; i < reserveSlots.Length; i++)
            {
                if (reserveSlots[i] != null)
                    reserveSlots[i].Init(this, i);
            }
        }

        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    public void SelectCharacter(CharacterRuntimeData runtimeData)
    {
        selectedCharacter = runtimeData;
    }

    public void SelectSkill(SkillMasterData skillData)
    {
        selectedSkill = skillData;
        TryReserveSelectedSkill();
    }

    public void OnTimelineSlotClicked(int slotIndex)
    {
        activeSlotIndex = slotIndex;

        Debug.Log($"[BattleTimelineController] Timeline Slot Selected: {activeSlotIndex}");

        if (timelineBarUI != null)
            timelineBarUI.SetActiveTimelineSlot(activeSlotIndex);

        TryReserveSelectedSkill();
    }

    private void TryReserveSelectedSkill()
    {
        Debug.Log($"[BattleTimelineController] TryReserve / Slot:{activeSlotIndex} / Character:{selectedCharacter?.CharacterId} / Skill:{selectedSkill?.SkillId}");

        if (activeSlotIndex < 0)
            return;

        if (selectedCharacter == null || selectedSkill == null)
            return;

        if (reserveSlots == null || activeSlotIndex >= reserveSlots.Length)
            return;

        ReserveTurnSlotUI slot = reserveSlots[activeSlotIndex];

        if (slot == null)
            return;

        BattleReservedCommand command =
            new BattleReservedCommand(selectedCharacter, selectedSkill);

        if (!selectedCharacter.CanReserveHealth(command.HealthCost) ||
            !selectedCharacter.CanReserveStamina(command.StaminaCost) ||
            !selectedCharacter.CanReserveResource(command.ResourceCost) ||
            !selectedCharacter.CanReserveMove(command.MoveCost) ||
            !selectedCharacter.CanReserveShield(command.ShieldCost))
        {
            Debug.LogWarning(
                $"[BattleTimelineController] 자원 부족 / Character:{selectedCharacter.CharacterId} / " +
                $"HP:{command.HealthCost} / STA:{command.StaminaCost} / RES:{command.ResourceCost} / MOVE:{command.MoveCost} / SHIELD:{command.ShieldCost}"
            );

            selectedSkill = null;
            return;
        }

        if (!slot.CanAcceptCharacter(selectedCharacter))
        {
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            selectedSkill = null;
            return;
        }

        bool added = slot.AddCommand(command);

        if (!added)
        {
            Debug.LogWarning("[BattleTimelineController] 예약 슬롯이 가득 찼습니다.");
            return;
        }

        selectedCharacter.AddReservedHealth(command.HealthCost);
        selectedCharacter.AddReservedStamina(command.StaminaCost);
        selectedCharacter.AddReservedResource(command.ResourceCost);
        selectedCharacter.AddReservedMove(command.MoveCost);
        selectedCharacter.AddReservedShield(command.ShieldCost);

        selectedSkill = null;

        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    public void RemoveCommand(int slotIndex, int orderIndex)
    {
        if (reserveSlots == null)
            return;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return;

        bool removed = slot.RemoveCommandAt(orderIndex, out BattleReservedCommand removedCommand);

        if (!removed)
            return;

        if (removedCommand != null && removedCommand.UserRuntime != null)
        {
            removedCommand.UserRuntime.RemoveReservedHealth(removedCommand.HealthCost);
            removedCommand.UserRuntime.RemoveReservedStamina(removedCommand.StaminaCost);
            removedCommand.UserRuntime.RemoveReservedResource(removedCommand.ResourceCost);
            removedCommand.UserRuntime.RemoveReservedMove(removedCommand.MoveCost);
            removedCommand.UserRuntime.RemoveReservedShield(removedCommand.ShieldCost);
        }

        RefreshTimeline();
        RefreshPlayerHUDs();

        Debug.Log($"[BattleTimelineController] 예약 취소 / Slot:{slotIndex} / Order:{orderIndex}");
    }

    public void ClearAllReservations()
    {
        if (reserveSlots == null)
            return;

        for (int i = 0; i < reserveSlots.Length; i++)
        {
            if (reserveSlots[i] == null)
                continue;

            var commands = reserveSlots[i].Commands;

            for (int j = commands.Count - 1; j >= 0; j--)
            {
                if (reserveSlots[i].RemoveCommandAt(j, out BattleReservedCommand removedCommand))
                {
                    if (removedCommand != null && removedCommand.UserRuntime != null)
                    {
                        removedCommand.UserRuntime.RemoveReservedHealth(removedCommand.HealthCost);
                        removedCommand.UserRuntime.RemoveReservedStamina(removedCommand.StaminaCost);
                        removedCommand.UserRuntime.RemoveReservedResource(removedCommand.ResourceCost);
                        removedCommand.UserRuntime.RemoveReservedMove(removedCommand.MoveCost);
                        removedCommand.UserRuntime.RemoveReservedShield(removedCommand.ShieldCost);
                    }
                }
            }

            reserveSlots[i].Clear();
        }

        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    private void RefreshTimeline()
    {
        if (timelineBarUI != null)
            timelineBarUI.Refresh(reserveSlots);
    }

    private void RefreshPlayerHUDs()
    {
        PlayerHUDSlot[] hudSlots = FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] != null)
                hudSlots[i].Refresh();
        }
    }
}