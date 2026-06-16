using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
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

    [Header("Selected Slot Text")]
    [SerializeField] private TMP_Text selectedSlotValueText;
    [SerializeField] private string emptySelectedSlotText = "-";
    [SerializeField] private bool autoFindSelectedSlotValueText = true;

    private int activeSlotIndex = -1;
    private CharacterRuntimeData selectedCharacter;
    private SkillMasterData selectedSkill;

    private readonly List<MonsterReservedCommand>[] monsterCommandsBySlot =
        new List<MonsterReservedCommand>[5];

    public int SlotCount => reserveSlots != null ? reserveSlots.Length : 0;

    private void Awake()
    {
        InitializeMonsterCommandSlots();
        AutoFindSelectedSlotValueTextIfNeeded();
        RefreshSelectedSlotValueText();

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

        RefreshSelectedSlotValueText();
        TryStartSkillReservation();
    }

    private void AutoFindSelectedSlotValueTextIfNeeded()
    {
        if (!autoFindSelectedSlotValueText)
            return;

        if (selectedSlotValueText != null)
            return;

        Transform searchRoot = null;

        if (timelineBarUI != null)
            searchRoot = timelineBarUI.transform;
        else
            searchRoot = transform;

        Transform found = FindChildRecursive(searchRoot, "Value_text");

        if (found == null)
        {
            BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

            if (foundTimelineBar != null)
                found = FindChildRecursive(foundTimelineBar.transform, "Value_text");
        }

        if (found == null)
            return;

        selectedSlotValueText = found.GetComponent<TMP_Text>();
    }

    private void RefreshSelectedSlotValueText()
    {
        AutoFindSelectedSlotValueTextIfNeeded();

        if (selectedSlotValueText == null)
            return;

        if (activeSlotIndex < 0)
            selectedSlotValueText.text = emptySelectedSlotText;
        else
            selectedSlotValueText.text = (activeSlotIndex + 1).ToString();
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void TryStartSkillReservation()
    {
        if (activeSlotIndex < 0)
        {
            if (selectedSkill != null)
                ShowBattleWarning("타임라인 슬롯을 먼저 선택해주세요.");

            return;
        }

        if (selectedCharacter == null && selectedSkill == null)
        {
            ShowBattleWarning("캐릭터와 스킬을 먼저 선택해주세요.");
            return;
        }

        if (selectedCharacter == null)
        {
            ShowBattleWarning("캐릭터를 먼저 선택해주세요.");
            return;
        }

        if (selectedSkill == null)
            return;

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("타임라인 슬롯이 없습니다.");
            selectedSkill = null;
            return;
        }

        if (activeSlotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            selectedSkill = null;
            return;
        }

        ReserveTurnSlotUI slot = reserveSlots[activeSlotIndex];

        if (slot == null)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            selectedSkill = null;
            return;
        }

        PlayerReservedCommand costCheckCommand =
            new PlayerReservedCommand(selectedCharacter, selectedSkill);

        string blockReason = GetReserveBlockReason(costCheckCommand);
        if (!string.IsNullOrEmpty(blockReason))
        {
            ShowBattleWarning(blockReason);
            selectedSkill = null;
            return;
        }

        if (!slot.CanAcceptCharacter(selectedCharacter))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            selectedSkill = null;
            return;
        }

        if (!slot.CanAddCommand())
        {
            ShowBattleWarning("한 슬롯에는 최대 3개의 스킬만 예약할 수 있습니다.");
            selectedSkill = null;
            return;
        }

        int casterGridIndex = GetPreviewGridIndex(selectedCharacter);

        if (casterGridIndex < 0)
        {
            ShowBattleWarning("캐릭터 위치를 찾을 수 없습니다.");
            Debug.LogWarning($"[BattleTimelineController] 캐릭터 위치를 찾을 수 없습니다: {selectedCharacter.CharacterId}");
            selectedSkill = null;
            return;
        }

        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        if (playerSkillReservationController == null)
        {
            ShowBattleWarning("스킬 예약 컨트롤러를 찾을 수 없습니다.");
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
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("타임라인 슬롯이 없습니다.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            return false;
        }

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            return false;
        }

        string blockReason = GetReserveBlockReason(command);
        if (!string.IsNullOrEmpty(blockReason))
        {
            ShowBattleWarning(blockReason);
            return false;
        }

        if (!slot.CanAcceptCharacter(command.UserRuntime))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            return false;
        }

        if (!slot.CanAddCommand())
        {
            ShowBattleWarning("한 슬롯에는 최대 3개의 스킬만 예약할 수 있습니다.");
            return false;
        }

        bool added = slot.AddCommand(command);

        if (!added)
        {
            ShowBattleWarning("스킬을 예약할 수 없습니다.");
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
        return string.IsNullOrEmpty(GetReserveBlockReason(command));
    }

    private string GetReserveBlockReason(PlayerReservedCommand command)
    {
        if (command == null)
            return "예약할 스킬 정보가 없습니다.";

        if (command.UserRuntime == null)
            return "선택된 캐릭터가 없습니다.";

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

                return $"{GetCostLabel(command.SkillData.ReferenceResource)}이 부족합니다. 필요:{minRequired} / 보유:{command.ResourceCost}";
            }
        }

        string shortageMessage = GetShortageMessage(runtime, command);
        if (!string.IsNullOrEmpty(shortageMessage))
            return shortageMessage;

        return string.Empty;
    }

    private string GetShortageMessage(CharacterRuntimeData runtime, PlayerReservedCommand command)
    {
        if (runtime == null || command == null)
            return "예약할 스킬 정보가 없습니다.";

        if (!runtime.CanReserveHealth(command.HealthCost))
            return BuildShortageMessage("체력", command.HealthCost, runtime.CurrentHealth - runtime.ReservedHealthCost);

        if (!runtime.CanReserveStamina(command.StaminaCost))
            return BuildShortageMessage("코스트", command.StaminaCost, runtime.CurrentStamina - runtime.ReservedStaminaCost);

        if (!runtime.CanReserveResource(command.ResourceCost))
            return BuildShortageMessage("고유자원", command.ResourceCost, runtime.CurrentResource - runtime.ReservedResourceCost);

        if (!runtime.CanReserveMove(command.MoveCost))
            return BuildShortageMessage("이동 포인트", command.MoveCost, runtime.CurrentMoveLevel - runtime.ReservedMoveCost);

        if (!runtime.CanReserveShield(command.ShieldCost))
            return BuildShortageMessage("방어도", command.ShieldCost, runtime.CurrentShield - runtime.ReservedShieldCost);

        return string.Empty;
    }

    private string BuildShortageMessage(string label, int required, int available)
    {
        int safeAvailable = Mathf.Max(0, available);
        return $"{label}이 부족합니다. 필요:{required} / 보유:{safeAvailable}";
    }

    private string GetCostLabel(ReferenceResource resource)
    {
        switch (resource)
        {
            case ReferenceResource.Health:
                return "체력";

            case ReferenceResource.Stamina:
                return "코스트";

            case ReferenceResource.UniqueResource:
                return "고유자원";

            case ReferenceResource.MovePoint:
                return "이동 포인트";

            default:
                return "자원";
        }
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

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    private void RefreshTimeline()
    {
        if (timelineBarUI != null)
            timelineBarUI.Refresh(reserveSlots, monsterCommandsBySlot);
        else
        {
            ShowBattleWarning("타임라인 UI를 찾을 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] timelineBarUI가 없습니다.");
        }
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