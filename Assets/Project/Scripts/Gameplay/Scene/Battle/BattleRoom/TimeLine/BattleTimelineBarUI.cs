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

            if (timelineGroups[i] != null)
                timelineGroups[i].SetTimelineEntries(entries, i);
        }
    }

    public void Clear()
    {
        Debug.LogWarning("[BattleTimelineBarUI] Clear È£ÃâµÊ\n" + System.Environment.StackTrace);

        AutoFindGroupsIfNeeded();

        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] != null)
                timelineGroups[i].Clear();
        }
    }

    private void InitGroups()
    {
        if (timelineGroups == null)
            return;

        for (int i = 0; i < timelineGroups.Length; i++)
        {
            if (timelineGroups[i] != null)
                timelineGroups[i].Init(this, i);
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
