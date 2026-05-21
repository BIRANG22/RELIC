using Relic.Gameplay.Data;
using System;

namespace Relic.Gameplay.Battle
{
    public enum BattleActorType
    {
        Player,
        Monster
    }

    [Serializable]
    public class TimelineActionData
    {
        public string ActionId;

        public BattleActorType ActorType;

        public string ActorRuntimeId;

        public string SkillId;

        public TimelineActionType ActionType;

        public string TargetRuntimeId;

        public int TargetGridIndex = -1;

        public int SlotIndex = -1;

        public int Order;

        public SkillDirection Direction;
    }
}