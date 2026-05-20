using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public class BattleTimelineQueue
    {
        private readonly List<TimelineActionData> actions = new();

        public IReadOnlyList<TimelineActionData> Actions => actions;

        public void AddAction(TimelineActionData action)
        {
            if (action == null)
                return;

            actions.Add(action);
            Sort();
        }

        public void RemoveActorActions(string actorRuntimeId)
        {
            actions.RemoveAll(x => x.ActorRuntimeId == actorRuntimeId);
            Sort();
        }

        public void Clear()
        {
            actions.Clear();
        }

        private void Sort()
        {
            actions.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}