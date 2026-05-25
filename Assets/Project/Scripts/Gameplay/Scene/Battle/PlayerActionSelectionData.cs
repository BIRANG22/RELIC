using System;
using Relic.Gameplay.Data;

namespace Relic.Gameplay.Battle
{
    [Serializable]
    public class PlayerActionSelectionData
    {
        public string PlayerRuntimeId;
        public string SkillId;

        public TimelineActionType ActionType = TimelineActionType.None;

        public string TargetRuntimeId;
        public int TargetGridIndex = -1;

        public SkillDirection Direction;
        public bool HasPlayer => !string.IsNullOrWhiteSpace(PlayerRuntimeId);
        public bool HasSkill => !string.IsNullOrWhiteSpace(SkillId);

        public void Clear()
        {
            PlayerRuntimeId = null;
            SkillId = null;
            ActionType = TimelineActionType.None;
            TargetRuntimeId = null;
            TargetGridIndex = -1;
        }

        public void ClearActionOnly()
        {
            SkillId = null;
            ActionType = TimelineActionType.None;
            TargetRuntimeId = null;
            TargetGridIndex = -1;
        }
    }
}