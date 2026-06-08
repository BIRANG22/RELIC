using System.Collections.Generic;
using UnityEngine;

public class BattleTimelineBarUI : MonoBehaviour
{
    [Header("Owner")]
    [SerializeField] private BattleTimelineController owner;

    [Header("Timeline Slots")]
    [SerializeField] private BattleTimelineGroupUI[] timelineGroups;

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
        Debug.Log("[BattleTimelineBarUI] Refresh »£√‚µ ");

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

            if (reserveSlots != null && i < reserveSlots.Length && reserveSlots[i] != null)
            {
                var commands = reserveSlots[i].Commands;

                for (int j = 0; j < commands.Count; j++)
                {
                    BattleTimelinePreviewEntry entry =
                        BattleTimelinePreviewEntry.CreatePlayer(i, j, commands[j]);

                    if (entry != null)
                        entries.Add(entry);
                }
            }

            if (monsterCommandsBySlot != null &&
                i < monsterCommandsBySlot.Length &&
                monsterCommandsBySlot[i] != null)
            {
                var monsterCommands = monsterCommandsBySlot[i];

                for (int j = 0; j < monsterCommands.Count; j++)
                {
                    BattleTimelinePreviewEntry entry =
                        BattleTimelinePreviewEntry.CreateMonster(i, j, monsterCommands[j]);

                    if (entry != null)
                        entries.Add(entry);
                }
            }

            if (timelineGroups[i] != null)
                timelineGroups[i].SetTimelineEntries(entries, i);
        }
    }

    public void Clear()
    {
        Debug.LogWarning("[BattleTimelineBarUI] Clear »£√‚µ \n" + System.Environment.StackTrace);

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
}