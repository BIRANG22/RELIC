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


    private const int MonsterActionCountPerRound = 2;
    public void PlanMonsterTurns(List<MonsterUnit> monsterUnits)
    {
        if (timelineController == null)
        {
            Debug.LogWarning("[BattleMonsterTurnPlanner] BattleTimelineController가 없습니다.");
            return;
        }

        timelineController.ClearMonsterCommands();

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

            for (int actionIndex = 0; actionIndex < MonsterActionCountPerRound; actionIndex++)
            {
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
                    int move = Mathf.Abs(skillData.GridMove);
                    if (move <= 0)
                        move = 1;

                    Vector2Int moveOffset = monsterUnit.SelectMoveOffset(
                        context,
                        gridManager,
                        move
                    );

                    command.SetMoveOffset(moveOffset);
                }
                else
                {
                    SetMonsterRange(monsterUnit, skillData, command);
                }

                int slotIndex = FindAvailableMonsterSlot(runtime);

                //int slotIndex = Random.Range(0, timelineController.SlotCount);

                timelineController.AddMonsterCommand(slotIndex, command);
            }
        }
    }

    private int FindAvailableMonsterSlot(MonsterRuntimeData runtime)
    {
        if (runtime == null)
            return -1;

        List<int> candidates = new();

        for (int i = 0; i < timelineController.SlotCount; i++)
        {
            var commands = timelineController.GetMonsterCommands(i);

            if (commands == null || commands.Count <= 0)
            {
                candidates.Add(i);
                continue;
            }

            bool sameMonsterOnly = true;

            for (int j = 0; j < commands.Count; j++)
            {
                if (commands[j] == null || commands[j].RuntimeId != runtime.RuntimeId)
                {
                    sameMonsterOnly = false;
                    break;
                }
            }

            if (sameMonsterOnly)
                candidates.Add(i);
        }

        if (candidates.Count <= 0)
            return -1;

        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
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

    private void SetMonsterRange(
     MonsterUnit monsterUnit,
     MonsterSkillData skillData,
     MonsterReservedCommand command)
    {
        if (monsterUnit == null || skillData == null || command == null)
        {
            Debug.LogWarning("[MonsterRange] null 참조");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogWarning("[MonsterRange] gridManager 없음");
            return;
        }

        int casterGridIndex = monsterUnit.MainGridIndex;

        if (casterGridIndex < 0)
        {
            Debug.LogWarning($"[MonsterRange] MainGridIndex 없음 / Monster:{monsterUnit.RuntimeData?.Name}");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillData.RangeId) || skillData.RangeId == "0")
        {
            Debug.LogWarning($"[MonsterRange] RangeId 없음 / Skill:{skillData.SkillId}");
            return;
        }

        BattleDirection direction = GetDirectionToNearestPlayer(monsterUnit);

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            casterGridIndex,
            skillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        if (rangeIndices == null)
            rangeIndices = new List<int>();

        command.SetRangeResult(rangeIndices, rangeIndices);

        Debug.Log(
            $"[MonsterRange] Monster:{monsterUnit.RuntimeData?.Name} / " +
            $"Skill:{skillData.SkillId} / RangeId:{skillData.RangeId} / " +
            $"CasterGrid:{casterGridIndex} / Direction:{direction} / " +
            $"RangeCount:{rangeIndices.Count}"
        );
    }

    private BattleDirection GetDirectionToNearestPlayer(MonsterUnit monsterUnit)
    {
        BattleCharacter[] players =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        if (players == null || players.Length <= 0)
            return BattleDirection.Left;

        int monsterGrid = monsterUnit.MainGridIndex;
        Vector2Int monsterCoord = gridManager.IndexToCoord(monsterGrid);

        BattleCharacter nearest = null;
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || players[i].CurrentGridIndex < 0)
                continue;

            Vector2Int playerCoord = gridManager.IndexToCoord(players[i].CurrentGridIndex);

            int distance =
                Mathf.Abs(playerCoord.x - monsterCoord.x) +
                Mathf.Abs(playerCoord.y - monsterCoord.y);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = players[i];
            }
        }

        if (nearest == null)
            return BattleDirection.Left;

        Vector2Int targetCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);

        if (targetCoord.x >= monsterCoord.x)
            return BattleDirection.Right;

        return BattleDirection.Left;
    }
}
