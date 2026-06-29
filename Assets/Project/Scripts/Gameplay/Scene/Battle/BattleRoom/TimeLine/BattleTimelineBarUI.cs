using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

public class BattleTimelineBarUI : MonoBehaviour
{
    [Header("Owner")]
    [SerializeField] private BattleTimelineController owner;

    [Header("Timeline Slots")]
    [SerializeField] private BattleTimelineGroupUI[] timelineGroups;

    [SerializeField] private RangePreview rangePreview;
    [SerializeField] private GridManager gridManager;
    private int activeSlotIndex = -1;
    private bool[] slotHasTimelineEntry;

    private void Awake()
    {
        AutoFindGroupsIfNeeded();
        InitGroups();
    }

    public void Init(BattleTimelineController owner)
    {
        this.owner = owner;
        AutoFindGroupsIfNeeded();
        InitGroups();
    }

    public void OnTimelineSlotClicked(int slotIndex)
    {
        if (owner != null)
            owner.OnTimelineSlotClicked(slotIndex);
    }

    public bool TryGetOwner(out BattleTimelineController result)
    {
        result = owner;
        return result != null;
    }

    public void SetActiveTimelineSlot(int slotIndex)
    {
        activeSlotIndex = slotIndex;

        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] != null)
                timelineGroups[i].SetActiveTimelineSlot(i == activeSlotIndex);
        }
    }

    public void Refresh(
        ReserveTurnSlotUI[] reserveSlots,
        IReadOnlyList<MonsterReservedCommand>[] monsterCommandsBySlot)
    {
        AutoFindGroupsIfNeeded();
        InitGroups();

        if (timelineGroups == null)
        {
            Debug.LogWarning("[BattleTimelineBarUI] timelineGroups null");
            return;
        }

        if (slotHasTimelineEntry == null || slotHasTimelineEntry.Length != timelineGroups.Length)
            slotHasTimelineEntry = new bool[timelineGroups.Length];

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            List<BattleTimelinePreviewEntry> entries = new();

            List<PlayerReservedCommand> playerCommands = new();

            if (reserveSlots != null && i < reserveSlots.Length && reserveSlots[i] != null)
            {
                var commands = reserveSlots[i].Commands;

                for (int j = 0; j < commands.Count; j++)
                {
                    if (commands[j] != null)
                        playerCommands.Add(commands[j]);
                }
            }

            int orderIndex = 0;

            for (int j = 0; j < playerCommands.Count; j++)
            {
                PlayerReservedCommand command = playerCommands[j];

                if (!BattleActionOrderUtility.HasSwift(command))
                    continue;

                BattleTimelinePreviewEntry entry =
                    BattleTimelinePreviewEntry.CreatePlayer(i, orderIndex, command, j);

                if (entry != null)
                    entries.Add(entry);

                orderIndex++;
            }

            if (monsterCommandsBySlot != null &&
                i < monsterCommandsBySlot.Length &&
                monsterCommandsBySlot[i] != null)
            {
                var monsterCommands = monsterCommandsBySlot[i];

                for (int j = 0; j < monsterCommands.Count; j++)
                {
                    BattleTimelinePreviewEntry entry =
                        BattleTimelinePreviewEntry.CreateMonster(i, orderIndex, monsterCommands[j]);

                    if (entry != null)
                        entries.Add(entry);

                    orderIndex++;
                }
            }

            for (int j = 0; j < playerCommands.Count; j++)
            {
                PlayerReservedCommand command = playerCommands[j];

                if (BattleActionOrderUtility.HasSwift(command))
                    continue;

                BattleTimelinePreviewEntry entry =
                    BattleTimelinePreviewEntry.CreatePlayer(i, orderIndex, command, j);

                if (entry != null)
                    entries.Add(entry);

                orderIndex++;
            }

            bool hasEntry = entries.Count > 0;
            slotHasTimelineEntry[i] = hasEntry;

            if (timelineGroups[i] != null)
            {
                timelineGroups[i].SetTimelineEntries(entries, i);

                // 진행 중인 TimelineBar에서는 등록된 행동이 없어도 Turn1~Turn5 숫자는 항상 보여야 합니다.
                // Player_Icon / Enemy_Icon은 BattleTimelineGroupUI.SetTimelineEntries()가 실제 등록 상태에 맞게 따로 제어합니다.
                SetTurnMarkChildrenVisible(timelineGroups[i], true);
            }
        }
    }


    public void SetTurnMarkChildrenVisible(bool visible)
    {
        AutoFindGroupsIfNeeded();

        if (timelineGroups == null)
            return;

        if (slotHasTimelineEntry == null || slotHasTimelineEntry.Length != timelineGroups.Length)
            slotHasTimelineEntry = new bool[timelineGroups.Length];

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            // visible == true인 바는 현재 진행/등록 대상 TimelineBar입니다.
            // 이 바에서는 스킬 등록 여부와 관계없이 Turn1~Turn5 숫자는 항상 보여야 합니다.
            // visible == false인 바는 대기 TimelineBar이므로 TurnMark 자식 전체를 숨깁니다.
            SetTurnMarkChildrenVisible(timelineGroups[i], visible);
        }
    }

    private void SetTurnMarkChildrenVisible(BattleTimelineGroupUI group, bool visible)
    {
        if (group == null)
            return;

        Transform turnMark = FindChildRecursive(group.transform, "TurnMark");

        if (turnMark == null)
            return;

        for (int childIndex = 0; childIndex < turnMark.childCount; childIndex++)
        {
            Transform child = turnMark.GetChild(childIndex);

            if (child == null)
                continue;

            // Player_Icon / Enemy_Icon은 BattleTimelineGroupUI.SetTimelineEntries()에서
            // 실제 등록된 플레이어/몬스터 행동 유무에 따라 개별 제어합니다.
            // 여기서 한꺼번에 켜면 몬스터 행동만 있는 슬롯에도 Player_Icon이 켜질 수 있습니다.
            bool isOwnerIcon = child.name == "Player_Icon" || child.name == "Enemy_Icon";

            if (!visible)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (!isOwnerIcon)
                child.gameObject.SetActive(true);
        }
    }

    public void SetEmptyUseSkillSlotsVisible(bool visible)
    {
        AutoFindGroupsIfNeeded();

        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] != null)
                timelineGroups[i].SetEmptyUseSkillSlotsVisible(visible);
        }
    }

    public void Clear()
    {
        AutoFindGroupsIfNeeded();

        if (timelineGroups == null)
            return;

        if (slotHasTimelineEntry == null || slotHasTimelineEntry.Length != timelineGroups.Length)
            slotHasTimelineEntry = new bool[timelineGroups.Length];

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            slotHasTimelineEntry[i] = false;

            if (timelineGroups[i] != null)
            {
                timelineGroups[i].Clear();
                SetTurnMarkChildrenVisible(timelineGroups[i], false);
            }
        }
    }

    private void InitGroups()
    {
        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] == null)
                continue;

            timelineGroups[i].Init(this, i);

            // TimelineBar를 1/2로 나누면 두 바 안에 같은 TimelineSlot01~05가 각각 존재합니다.
            // 키보드 입력은 BattleTimelineController의 reserveSlots를 직접 사용하지만,
            // 마우스 클릭은 각 TimelineSlot에 붙은 ReserveTurnSlotUI가 owner를 가지고 있어야 동작합니다.
            ReserveTurnSlotUI clickSlot = timelineGroups[i].GetComponent<ReserveTurnSlotUI>();

            if (clickSlot == null)
                clickSlot = timelineGroups[i].GetComponentInChildren<ReserveTurnSlotUI>(true);

            if (clickSlot != null)
                clickSlot.Init(owner, i);
        }
    }

    private void AutoFindGroupsIfNeeded()
    {
        if (timelineGroups != null && timelineGroups.Length > 0)
            return;

        List<BattleTimelineGroupUI> groups = new();

        for (int i = 1; i <= 5; i++)
        {
            Transform found = FindChildRecursive(transform, "TimelineSlot" + i.ToString("00"));

            if (found == null)
                found = FindChildRecursive(transform, "TimelineSlot" + i);

            if (found == null)
                continue;

            BattleTimelineGroupUI group = found.GetComponent<BattleTimelineGroupUI>();

            if (group == null)
                group = found.gameObject.AddComponent<BattleTimelineGroupUI>();

            groups.Add(group);
        }

        timelineGroups = groups.ToArray();
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

    public void OnOrderClicked(int slotIndex, int orderIndex)
    {
        if (owner != null)
            owner.RemoveCommand(slotIndex, orderIndex);
    }

    public void OnEntryClicked(BattleTimelinePreviewEntry entry)
    {
        if (entry == null)
            return;

        if (!entry.IsPlayer)
            return;

        if (owner != null)
            owner.RemoveCommand(entry.SlotIndex, entry.PlayerCommandIndex);
    }

    public void ShowEntryRangePreview(BattleTimelinePreviewEntry entry)
    {
        if (entry == null || rangePreview == null || gridManager == null)
            return;

        if (!entry.IsPlayer || entry.PlayerCommand == null)
            return;

        PlayerReservedCommand command = entry.PlayerCommand;

        if (command.SkillData == null || command.UserRuntime == null)
            return;

        int casterGridIndex = owner.GetPreviewGridIndexBeforeCommand(
            command.UserRuntime,
            entry.SlotIndex,
            entry.PlayerCommandIndex
        );

        if (casterGridIndex < 0)
            return;

        List<int> rangeIndices = new();

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                casterGridIndex,
                BattleEquipmentEffectService.GetEffectiveRangeId(command.UserRuntime, command.SkillData),
                command.Direction,
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }
        else if (command.SkillData.RangeType == RangeType.Selection)
        {
            rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                casterGridIndex,
                BattleEquipmentEffectService.GetEffectiveRangeId(command.UserRuntime, command.SkillData),
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }
        else
        {
            rangeIndices = command.RangeGridIndices;
        }

        rangePreview.ShowDirectionCells(rangeIndices);
    }

    public void ClearEntryRangePreview()
    {
        if (rangePreview != null)
            rangePreview.Clear();
    }
}
