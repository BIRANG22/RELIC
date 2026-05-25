using System.Collections.Generic;
using UnityEngine;
using System;

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

        public bool RemoveAction(Predicate<TimelineActionData> match)
        {
            if (match == null)
                return false;

            int removed = actions.RemoveAll(match);

            if (removed <= 0)
                return false;

            Sort();
            return true;
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
