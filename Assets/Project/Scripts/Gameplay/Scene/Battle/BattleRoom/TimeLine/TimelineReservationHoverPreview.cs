using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class TimelineReservationHoverPreview : MonoBehaviour
{
    public static TimelineReservationHoverPreview Instance { get; private set; }

    [SerializeField] private RangePreview rangePreview;
    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        Instance = this;
    }

    public static void HideCurrent()
    {
        if (Instance != null)
            Instance.Hide();
    }
    public void Show(BattleTimelinePreviewEntry entry)
    {
        if (entry == null || rangePreview == null)
            return;

        if (!entry.IsPlayer || entry.PlayerCommand == null)
            return;

        PlayerReservedCommand command = entry.PlayerCommand;

        if (command.SkillData == null || command.UserRuntime == null)
            return;

        FindReferencesIfNeeded();

        if (timelineController == null || gridManager == null)
        {
            rangePreview.ShowRangeCells(command.RangeGridIndices);
            return;
        }

        int casterGridIndex =
            timelineController.GetPreviewGridIndexBeforeCommand(
                command.UserRuntime,
                entry.SlotIndex,
                entry.PlayerCommandIndex
            );

        if (casterGridIndex < 0)
        {
            rangePreview.ShowRangeCells(command.RangeGridIndices);
            return;
        }

        List<int> rangeIndices = BuildRange(command, casterGridIndex);

        rangePreview.ShowRangeCells(rangeIndices);
    }

    public void Hide()
    {
        if (rangePreview != null)
            rangePreview.ClearRangeOnly();
    }

    private List<int> BuildRange(PlayerReservedCommand command, int casterGridIndex)
    {
        if (command == null || command.SkillData == null)
            return new List<int>();

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null)
            return command.RangeGridIndices;

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            return BattleRangeCalculator.GetDirectionRangeIndices(
                casterGridIndex,
                BattleEquipmentEffectService.GetEffectiveRangeId(command.UserRuntime, command.SkillData),
                command.Direction,
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }

        if (command.SkillData.RangeType == RangeType.Selection)
        {
            return BattleRangeCalculator.GetSelectionRangeIndices(
                casterGridIndex,
                BattleEquipmentEffectService.GetEffectiveRangeId(command.UserRuntime, command.SkillData),
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }

        return command.RangeGridIndices;
    }

    private void FindReferencesIfNeeded()
    {
        if (timelineController == null)
            timelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);

        if (rangePreview == null)
            rangePreview = FindFirstObjectByType<RangePreview>(FindObjectsInactive.Include);
    }
}
