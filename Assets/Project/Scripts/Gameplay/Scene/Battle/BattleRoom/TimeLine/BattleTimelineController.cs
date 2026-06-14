using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

public class BattleTimelineController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI;
    [SerializeField] private ReserveTurnSlotUI[] reserveSlots;

    [Header("Reservation Preview")]
    [SerializeField] private PlayerSkillReservationController playerSkillReservationController;

    [Header("Grid")]
    [SerializeField] private GridManager gridManager;

    private int activeSlotIndex = -1;
    private CharacterRuntimeData selectedCharacter;
    private SkillMasterData selectedSkill;

    private readonly List<MonsterReservedCommand>[] monsterCommandsBySlot =
        new List<MonsterReservedCommand>[5];

    public int SlotCount => reserveSlots != null ? reserveSlots.Length : 0;

    private void Awake()
    {
        InitializeMonsterCommandSlots();

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
        TryStartSkillReservation();
    }

    public void ClearMonsterCommands()
    {
        if (monsterCommandsBySlot == null)
            return;

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] != null)
                monsterCommandsBySlot[i].Clear();
        }

        RefreshTimeline();
    }

    public void OnTimelineSlotClicked(int slotIndex)
    {
        activeSlotIndex = slotIndex;

        if (timelineBarUI != null)
            timelineBarUI.SetActiveTimelineSlot(activeSlotIndex);

        TryStartSkillReservation();
    }

    private void TryStartSkillReservation()
    {
        if (activeSlotIndex < 0)
            return;

        if (selectedCharacter == null || selectedSkill == null)
            return;

        if (reserveSlots == null || activeSlotIndex >= reserveSlots.Length)
            return;

        ReserveTurnSlotUI slot = reserveSlots[activeSlotIndex];

        if (slot == null)
            return;

        PlayerReservedCommand costCheckCommand =
            new PlayerReservedCommand(selectedCharacter, selectedSkill);

        if (!CanReserveCommand(costCheckCommand))
        {
            selectedSkill = null;
            return;
        }

        if (!slot.CanAcceptCharacter(selectedCharacter))
        {
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            selectedSkill = null;
            return;
        }

        int casterGridIndex = GetPreviewGridIndex(selectedCharacter);

        if (casterGridIndex < 0)
        {
            Debug.LogWarning($"[BattleTimelineController] 캐릭터 위치를 찾을 수 없습니다: {selectedCharacter.CharacterId}");
            selectedSkill = null;
            return;
        }

        if (playerSkillReservationController == null)
        {
            Debug.LogWarning("[BattleTimelineController] PlayerSkillReservationController가 없습니다.");
            selectedSkill = null;
            return;
        }

        playerSkillReservationController.StartReservation(
            selectedCharacter,
            selectedSkill,
            casterGridIndex,
            activeSlotIndex,
            GetSelectedCharacterSprite()
        );

        selectedSkill = null;
    }

    public bool ConfirmPlayerCommand(int slotIndex, PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        if (reserveSlots == null)
            return false;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return false;

        if (!CanReserveCommand(command))
            return false;

        if (!slot.CanAcceptCharacter(command.UserRuntime))
        {
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            return false;
        }

        bool added = slot.AddCommand(command);

        if (!added)
        {
            Debug.LogWarning("[BattleTimelineController] 예약 슬롯이 가득 찼습니다.");
            return false;
        }

        command.UserRuntime.AddReservedHealth(command.HealthCost);
        command.UserRuntime.AddReservedStamina(command.StaminaCost);
        command.UserRuntime.AddReservedResource(command.ResourceCost);
        command.UserRuntime.AddReservedMove(command.MoveCost);
        command.UserRuntime.AddReservedShield(command.ShieldCost);

        RefreshTimeline();
        RefreshPlayerHUDs();

        return true;
    }

    public IReadOnlyList<PlayerReservedCommand> GetPlayerCommands(int slotIndex)
    {
        if (reserveSlots == null)
            return null;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return null;

        if (reserveSlots[slotIndex] == null)
            return null;

        return reserveSlots[slotIndex].Commands;
    }

    public IReadOnlyList<MonsterReservedCommand> GetMonsterCommands(int slotIndex)
    {
        InitializeMonsterCommandSlots();

        if (slotIndex < 0 || slotIndex >= monsterCommandsBySlot.Length)
            return null;

        return monsterCommandsBySlot[slotIndex];
    }

    private bool CanReserveCommand(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null)
            return false;

        CharacterRuntimeData runtime = command.UserRuntime;

        if (command.SkillData != null &&
            command.SkillData.ResourceCostType == ResourceCostType.AllCurrent)
        {
            int minRequired = Mathf.Max(1, command.SkillData.ResourceCostValue);

            if (command.ResourceCost < minRequired)
            {
                Debug.LogWarning(
                    $"[BattleTimelineController] AllCurrent 자원 부족 / " +
                    $"Character:{runtime.CharacterId} / " +
                    $"Skill:{command.SkillId} / " +
                    $"Cost:{command.ResourceCost} / " +
                    $"MinRequired:{minRequired}"
                );

                return false;
            }
        }

        if (!runtime.CanReserveHealth(command.HealthCost) ||
            !runtime.CanReserveStamina(command.StaminaCost) ||
            !runtime.CanReserveResource(command.ResourceCost) ||
            !runtime.CanReserveMove(command.MoveCost) ||
            !runtime.CanReserveShield(command.ShieldCost))
        {
            Debug.LogWarning(
                $"[BattleTimelineController] 자원 부족 / Character:{runtime.CharacterId} / " +
                $"HP:{command.HealthCost} / STA:{command.StaminaCost} / RES:{command.ResourceCost} / " +
                $"MOVE:{command.MoveCost} / SHIELD:{command.ShieldCost}"
            );

            return false;
        }

        return true;
    }

    public int GetPreviewGridIndex(CharacterRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return -1;

        int gridIndex = GetCurrentBattleCharacterGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            gridIndex = GetRuntimeStartGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            return -1;

        if (reserveSlots == null)
            return gridIndex;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtimeData.CharacterId)
                    continue;

                if (command.ReservedMoveGridIndex >= 0)
                    gridIndex = command.ReservedMoveGridIndex;
            }
        }

        return gridIndex;
    }

    private int GetCurrentBattleCharacterGridIndex(string characterId)
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            if (characters[i].CharacterId != characterId)
                continue;

            return characters[i].CurrentGridIndex;
        }

        return -1;
    }

    private int GetRuntimeStartGridIndex(string characterId)
    {
        if (DataManager.Instance == null)
            return -1;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int slotIndex = 0; slotIndex < partyStore.MaxPartyCountValue; slotIndex++)
        {
            if (partyStore.GetCharacterId(slotIndex) == characterId)
                return partyStore.GetSpawnGridIndex(slotIndex);
        }

        return -1;
    }

    private Sprite GetSelectedCharacterSprite()
    {
        BattleCharacter[] battleCharacters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < battleCharacters.Length; i++)
        {
            BattleCharacter battleCharacter = battleCharacters[i];

            if (battleCharacter == null || battleCharacter.RuntimeData == null)
                continue;

            if (battleCharacter.RuntimeData.CharacterId != selectedCharacter.CharacterId)
                continue;

            SpriteRenderer spriteRenderer = battleCharacter.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                return spriteRenderer.sprite;
        }

        return null;
    }

    private void InitializeMonsterCommandSlots()
    {
        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] == null)
                monsterCommandsBySlot[i] = new List<MonsterReservedCommand>();
        }
    }

    public void AddMonsterCommand(int slotIndex, MonsterReservedCommand command)
    {
        InitializeMonsterCommandSlots();

        if (command == null)
            return;

        if (slotIndex < 0 || slotIndex >= monsterCommandsBySlot.Length)
            slotIndex = 0;

        monsterCommandsBySlot[slotIndex].Add(command);

        RefreshTimeline();
    }

    public void ClearMonsterReservations()
    {
        InitializeMonsterCommandSlots();

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
            monsterCommandsBySlot[i].Clear();

        RefreshTimeline();
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

        bool removed = slot.RemoveCommandAt(orderIndex, out PlayerReservedCommand removedCommand);

        if (!removed)
            return;

        RemoveReservedCosts(removedCommand);

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
                if (reserveSlots[i].RemoveCommandAt(j, out PlayerReservedCommand removedCommand))
                    RemoveReservedCosts(removedCommand);
            }

            reserveSlots[i].Clear();
        }

        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    private void RemoveReservedCosts(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null)
            return;

        command.UserRuntime.RemoveReservedHealth(command.HealthCost);
        command.UserRuntime.RemoveReservedStamina(command.StaminaCost);
        command.UserRuntime.RemoveReservedResource(command.ResourceCost);
        command.UserRuntime.RemoveReservedMove(command.MoveCost);
        command.UserRuntime.RemoveReservedShield(command.ShieldCost);
    }

    private void RefreshTimeline()
    {
        if (timelineBarUI != null)
            timelineBarUI.Refresh(reserveSlots, monsterCommandsBySlot);
        else
            Debug.LogWarning("[BattleTimelineController] timelineBarUI가 없습니다.");
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