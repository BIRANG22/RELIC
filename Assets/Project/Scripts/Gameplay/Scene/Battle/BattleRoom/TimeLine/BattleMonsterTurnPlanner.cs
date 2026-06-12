using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleMonsterTurnPlanner : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private BattleTimelineController timelineController;

    [Header("Grid")]
    [SerializeField] private GridManager gridManager;

    [Header("Option")]
    [SerializeField] private int defaultMonsterSlotIndex = 2;

    public void PlanMonsterTurns(List<MonsterUnit> monsterUnits)
    {
        if (timelineController == null)
        {
            Debug.LogWarning("[BattleMonsterTurnPlanner] BattleTimelineController가 없습니다.");
            return;
        }

        if (monsterUnits == null)
            return;

        BattleContext context = new BattleContext();

        for (int i = 0; i < monsterUnits.Count; i++)
        {
            MonsterUnit monsterUnit = monsterUnits[i];

            if (monsterUnit == null || monsterUnit.RuntimeData == null)
                continue;

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;

            if (runtime.IsDead)
                continue;

            string skillId = monsterUnit.SelectSkill(context);

            if (string.IsNullOrWhiteSpace(skillId))
                continue;

            MonsterSkillData skillData = DataManager.Instance.MonsterSkillDatabase.Get(skillId);

            if (skillData == null)
            {
                Debug.LogWarning($"[BattleMonsterTurnPlanner] SkillData 없음: {skillId}");
                continue;
            }

            MonsterReservedCommand command =
                new MonsterReservedCommand(runtime, skillData);

            if (IsMoveSkill(skillData))
            {
                command.SetMoveOffset(GetMonsterMoveOffset(skillData));
            }
            else
            {
                SetMonsterRange(monsterUnit, skillData, command);
            }

            Debug.Log(
                $"[BattleMonsterTurnPlanner] Reserve / Monster:{runtime.Name} / " +
                $"Skill:{skillData.SkillId} / GridMove:{skillData.GridMove} / " +
                $"Notation:{skillData.TimelineNotation} / MoveOffset:{command.MoveOffset}"
            );

            timelineController.AddMonsterCommand(defaultMonsterSlotIndex, command);
        }
    }

    private bool IsMoveSkill(MonsterSkillData skillData)
    {
        if (skillData == null)
            return false;

        if (skillData.TimelineNotation == TimelineActionType.Move)
            return true;

        if (skillData.GridMove != 0)
            return true;

        if (!string.IsNullOrWhiteSpace(skillData.EffectIds) &&
            skillData.EffectIds.Contains("E_Move"))
            return true;

        return false;
    }

    private Vector2Int GetMonsterMoveOffset(MonsterSkillData skillData)
    {
        int move = Mathf.Abs(skillData.GridMove);

        if (move <= 0)
            move = 1;

        // 몬스터는 기본적으로 왼쪽으로 전진
        return new Vector2Int(-move, 0);
    }

    private void SetMonsterRange(
        MonsterUnit monsterUnit,
        MonsterSkillData skillData,
        MonsterReservedCommand command)
    {
        if (monsterUnit == null || skillData == null || command == null)
            return;

        if (gridManager == null)
            return;

        if (skillData.RangeType != RangeType.Direction)
            return;

        int casterGridIndex = monsterUnit.MainGridIndex;

        if (casterGridIndex < 0)
            return;

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            casterGridIndex,
            skillData.RangeId,
            BattleDirection.Left,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        command.SetRangeResult(rangeIndices, rangeIndices);
    }
}