using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public sealed class SkillRewardIdAttribute : PropertyAttribute
    {
    }

    [CreateAssetMenu(menuName = "Relic/Data/Skill Reward Pool Database")]
    public sealed class SkillRewardPoolDatabase : ScriptableObject
    {
        [Tooltip("켜면 모든 스킬 보상 후보를 아래 ID 목록으로 제한합니다.")]
        [SerializeField] private bool restrictRewards;

        [Tooltip("전투, 이벤트, 시작방 등 모든 스킬 보상에서 허용할 SkillId 목록입니다.")]
        [SkillRewardId]
        [SerializeField] private List<string> allowedSkillIds = new();

        public bool RestrictRewards => restrictRewards;
        public IReadOnlyList<string> AllowedSkillIds => allowedSkillIds;
    }
}
