using System.Collections.Generic;
using Relic.Gameplay.Data;
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
            TimelineActionType actionType,
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
                ActionType = actionType,
                Order = order
            };

            AddAction(action);

            Debug.Log(
                $"[Timeline] Monster Action Add: {monster.RuntimeData.Name} / {skillId} / {actionType}"
            );
        }

        public void AddPlayerAction(
            string playerRuntimeId,
            string skillId,
            TimelineActionType actionType,
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

            AddAction(action);

            Debug.Log(
                $"[Timeline] Player Action Add: {playerRuntimeId} / {skillId} / {actionType}"
            );
        }

        public void AddAction(TimelineActionData action)
        {
            if (action == null)
                return;

            queue.AddAction(action);

            Debug.Log(
                $"[Timeline] Action Add: {action.ActorType} / {action.ActorRuntimeId} / {action.SkillId} / {action.ActionType}"
            );
        }

        public IReadOnlyList<TimelineActionData> GetActions()
        {
            return queue.Actions;
        }

        public bool RemoveAction(System.Predicate<TimelineActionData> match)
        {
            return queue.RemoveAction(match);
        }
    }
}
