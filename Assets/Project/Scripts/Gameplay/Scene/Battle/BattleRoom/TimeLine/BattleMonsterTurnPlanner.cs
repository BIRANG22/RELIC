using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleMonsterTurnPlanner : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private BattleTimelineController timelineController;

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
        {
            Debug.LogWarning("[BattleMonsterTurnPlanner] monsterUnits null");
            return;
        }

        Debug.Log($"[BattleMonsterTurnPlanner] Found Monsters: {monsterUnits.Count}");

        BattleContext context = new BattleContext();

        for (int i = 0; i < monsterUnits.Count; i++)
        {
            MonsterUnit monsterUnit = monsterUnits[i];

            Debug.Log($"[BattleMonsterTurnPlanner] Check Monster Index:{i} / Unit:{monsterUnit}");

            if (monsterUnit == null)
            {
                Debug.LogWarning($"[BattleMonsterTurnPlanner] monsterUnit null / Index:{i}");
                continue;
            }

            if (monsterUnit.RuntimeData == null)
            {
                Debug.LogWarning($"[BattleMonsterTurnPlanner] RuntimeData null / Unit:{monsterUnit.name}");
                continue;
            }

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;

            Debug.Log($"[BattleMonsterTurnPlanner] Runtime: {runtime.Name} / MonsterId:{runtime.MonsterId} / Hp:{runtime.CurrentHp}");

            if (runtime.IsDead)
            {
                Debug.LogWarning($"[BattleMonsterTurnPlanner] Monster is dead: {runtime.Name}");
                continue;
            }

            string skillId = monsterUnit.SelectSkill(context);

            Debug.Log($"[BattleMonsterTurnPlanner] Selected SkillId: {skillId}");

            if (string.IsNullOrWhiteSpace(skillId))
            {
                Debug.LogWarning($"[BattleMonsterTurnPlanner] SkillId empty / Monster:{runtime.Name}");
                continue;
            }

            MonsterSkillData skillData = DataManager.Instance.MonsterSkillDatabase.Get(skillId);

            if (skillData == null)
            {
                Debug.LogWarning($"[BattleMonsterTurnPlanner] SkillData 없음: {skillId}");
                continue;
            }

            MonsterReservedCommand command =
                new MonsterReservedCommand(runtime, skillData);

            timelineController.AddMonsterCommand(defaultMonsterSlotIndex, command);

            Debug.Log($"[BattleMonsterTurnPlanner] Monster Reserved: {runtime.Name} / {skillId}");
        }
    }
}