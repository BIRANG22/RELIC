using System;

namespace Relic.Gameplay.Battle
{
    public enum BattleActorType
    {
        Player,
        Monster
    }

    public enum BattleActionType
    {
        None,
        Move,
        Attack,
        Skill,
        Guard,
        Wait
    }

    [Serializable]
    public class TimelineActionData
    {
        public string ActionId;

        public BattleActorType ActorType;

        // Player RuntimeId 또는 Monster RuntimeId
        public string ActorRuntimeId;

        // 사용할 스킬 ID
        public string SkillId;

        public BattleActionType ActionType;

        // 타겟 RuntimeId
        public string TargetRuntimeId;

        // 이동/방향/타일용
        public int TargetGridIndex = -1;

        public int Order;
    }
}