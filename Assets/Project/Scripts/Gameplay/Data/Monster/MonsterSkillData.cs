using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterSkillData
    {
        public string SkillId;
        public string Name;

        public TargetType Target;

        public string EffectIds;
        public string ValueRate;
        public string CountCalcTypes;
        public string CountRate;
        public int ValueRandomRange;

        public string RangeId;

        public TimelineActionType TimelineNotation;

        public string EffectDesc;

        // GameData MonsterSkill 시트의 SkillIcon 문자열 키입니다.
        // MonsterSkillIconDatabase에서 실제 Sprite를 찾을 때 사용합니다.
        public string SkillIcon;

        // GameData MonsterSkill 시트의 SkillType 표시 문자열입니다.
        // 예: 이동, 일반피해, 관통피해, 밀어냄, 버프, 디버프, 설치
        public string SkillType;

        public List<SkillEffectEntry> EffectEntries = new();
    }
}
