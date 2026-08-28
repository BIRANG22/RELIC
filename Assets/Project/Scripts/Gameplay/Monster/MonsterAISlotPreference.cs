using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public enum MonsterAISlotPreference
    {
        Front,
        FirstTwo,
        Back,
        Center,
        Last,
        NextSlot,
        SameSlot
    }

    public class MonsterAIAction
    {
        public string SkillId;
        public Vector2Int MoveOffset;
        public MonsterAISlotPreference SlotPreference;
        public int SameSlotGroup;
        public int Priority;
        public int RangeOriginGridIndex;
        public int RangeOriginCasterGridIndex;
        public bool HasForcedDirection;
        public BattleDirection ForcedDirection;
        public bool IsPortalMove;
        public int SlotOffset;
        public List<int> ExplicitRangeGridIndices;

        public MonsterAIAction(
            string skillId,
            Vector2Int moveOffset,
            MonsterAISlotPreference slotPreference,
            int sameSlotGroup = -1,
            int priority = 0,
            int rangeOriginGridIndex = -1,
            bool hasForcedDirection = false,
            BattleDirection forcedDirection = BattleDirection.Right,
            bool isPortalMove = false,
            int slotOffset = 0,
            List<int> explicitRangeGridIndices = null,
            int rangeOriginCasterGridIndex = -1)
        {
            SkillId = skillId;
            MoveOffset = moveOffset;
            SlotPreference = slotPreference;
            SameSlotGroup = sameSlotGroup;
            Priority = priority;
            RangeOriginGridIndex = Mathf.Max(-1, rangeOriginGridIndex);
            RangeOriginCasterGridIndex = Mathf.Max(-1, rangeOriginCasterGridIndex);
            HasForcedDirection = hasForcedDirection;
            ForcedDirection = forcedDirection;
            IsPortalMove = isPortalMove;
            SlotOffset = slotOffset;
            ExplicitRangeGridIndices = explicitRangeGridIndices != null
                ? new List<int>(explicitRangeGridIndices)
                : null;
        }
    }

    public class MonsterAIPlan
    {
        public readonly List<MonsterAIAction> Actions = new();

        public void Add(MonsterAIAction action)
        {
            if (action != null && !string.IsNullOrWhiteSpace(action.SkillId))
                Actions.Add(action);
        }
    }
}
