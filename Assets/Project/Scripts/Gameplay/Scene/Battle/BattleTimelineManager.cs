using System.Collections.Generic;
using Relic.Gameplay.Monster;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public class BattleTimelineManager : MonoBehaviour
    {
        private readonly BattleTimelineQueue queue = new();

        public IReadOnlyList<TimelineActionData> Actions => queue.Actions;

        public void ClearTimeline()
        {
            queue.Clear();
        }

        public void AddMonsterAction(
            MonsterUnit monster,
            string skillId,
            int order)
        {
            if (monster == null || monster.RuntimeData == null)
                return;

            TimelineActionData action = new TimelineActionData
            {
                ActionId = System.Guid.NewGuid().ToString(),
                ActorType = BattleActorType.Monster,
                ActorRuntimeId = monster.RuntimeData.RuntimeId,
                SkillId = skillId,
                ActionType = BattleActionType.Skill,
                Order = order
            };

            queue.AddAction(action);

            Debug.Log(
                $"[Timeline] Monster Action Add: {monster.RuntimeData.Name} / {skillId}"
            );
        }

        public void AddPlayerAction(
            string playerRuntimeId,
            string skillId,
            BattleActionType actionType,
            int order)
        {
            TimelineActionData action = new TimelineActionData
            {
                ActionId = System.Guid.NewGuid().ToString(),
                ActorType = BattleActorType.Player,
                ActorRuntimeId = playerRuntimeId,
                SkillId = skillId,
                ActionType = actionType,
                Order = order
            };

            queue.AddAction(action);

            Debug.Log(
                $"[Timeline] Player Action Add: {playerRuntimeId} / {skillId}"
            );
        }

        public IReadOnlyList<TimelineActionData> GetActions()
        {
            return queue.Actions;
        }
    }
}