using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class SkillRuntimeStore
    {
        private readonly Dictionary<string, SkillRuntimeData> map = new();

        private string MakeKey(string characterId, string skillId)
        {
            return $"{characterId}:{skillId}";
        }

        public void AddOrUpdate(SkillRuntimeData data)
        {
            map[MakeKey(data.CharacterId, data.SkillId)] = data;
        }

        public bool TryGet(string characterId, string skillId, out SkillRuntimeData data)
        {
            return map.TryGetValue(MakeKey(characterId, skillId), out data);
        }

        public SkillRuntimeData Get(string characterId, string skillId)
        {
            map.TryGetValue(MakeKey(characterId, skillId), out var data);
            return data;
        }

        public int GetPermanentValueBonus(string characterId, string skillId)
        {
            return TryGet(characterId, skillId, out SkillRuntimeData data) && data != null
                ? System.Math.Max(0, data.PermanentValueBonus)
                : 0;
        }

        public int GetBattleValueBonus(string characterId, string skillId)
        {
            return TryGet(characterId, skillId, out SkillRuntimeData data) && data != null
                ? System.Math.Max(0, data.BattleValueBonus)
                : 0;
        }

        public void AddPermanentValueBonus(string characterId, string skillId, int amount)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(skillId) || amount <= 0)
                return;

            if (!TryGet(characterId, skillId, out SkillRuntimeData data) || data == null)
            {
                data = new SkillRuntimeData
                {
                    CharacterId = characterId.Trim(),
                    SkillId = skillId.Trim(),
                    IsUnlocked = true
                };
            }

            data.PermanentValueBonus = System.Math.Max(0, data.PermanentValueBonus) + amount;
            AddOrUpdate(data);
        }

        public void AddBattleValueBonus(string characterId, string skillId, int amount)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(skillId) || amount <= 0)
                return;

            if (!TryGet(characterId, skillId, out SkillRuntimeData data) || data == null)
            {
                data = new SkillRuntimeData
                {
                    CharacterId = characterId.Trim(),
                    SkillId = skillId.Trim(),
                    IsUnlocked = true
                };
            }

            data.BattleValueBonus = System.Math.Max(0, data.BattleValueBonus) + amount;
            AddOrUpdate(data);
        }

        public void ResetBattleValueBonuses()
        {
            foreach (SkillRuntimeData data in map.Values)
            {
                if (data != null)
                    data.BattleValueBonus = 0;
            }
        }

        public IReadOnlyDictionary<string, SkillRuntimeData> GetAll()
        {
            return map;
        }

        public void Clear()
        {
            map.Clear();
        }

        public void SetAll(IEnumerable<SkillRuntimeData> skills)
        {
            Clear();

            if (skills == null)
                return;

            foreach (SkillRuntimeData skill in skills)
            {
                if (skill == null)
                    continue;

                if (string.IsNullOrWhiteSpace(skill.CharacterId) ||
                    string.IsNullOrWhiteSpace(skill.SkillId))
                    continue;

                map[MakeKey(skill.CharacterId, skill.SkillId)] = skill;
            }
        }
    }
}
