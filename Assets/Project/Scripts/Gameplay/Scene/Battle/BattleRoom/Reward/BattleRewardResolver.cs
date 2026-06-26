using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleRewardResolver : MonoBehaviour
{
    public List<BattleRewardData> Resolve(IReadOnlyList<MonsterRuntimeData> monsters)
    {
        List<BattleRewardData> rewards = new();

        Debug.Log($"[BattleRewardResolver] Resolve Start / MonsterCount:{monsters?.Count ?? 0}");

        if (monsters == null || DataManager.Instance == null)
            return rewards;

        HashSet<string> resolvedMonsterKeys = new();
        HashSet<string> uniqueItemResolvedMonsterKeys = new();

        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterRuntimeData monster = monsters[i];

            if (monster == null)
                continue;

            string monsterKey = GetMonsterRewardKey(monster);

            if (!resolvedMonsterKeys.Add(monsterKey))
            {
                Debug.LogWarning($"[BattleRewardResolver] 이미 처리한 몬스터 보상입니다. MonsterKey:{monsterKey} / MonsterId:{monster.MonsterId}");
                continue;
            }

            TryResolveMonster(monster, rewards, monsterKey, uniqueItemResolvedMonsterKeys);
        }

        int uniqueMonsterCount = resolvedMonsterKeys.Count;
        int sceneMonsterCount = GetCurrentBattleMonsterUnitCount();
        int uniqueItemLimit = sceneMonsterCount > 0
            ? Mathf.Min(uniqueMonsterCount, sceneMonsterCount)
            : uniqueMonsterCount;

        TrimUniqueItemRewardsToMonsterCount(rewards, uniqueItemLimit);
        AddSkillReward(rewards);

        return rewards;
    }

    private int GetCurrentBattleMonsterUnitCount()
    {
        MonsterUnit[] monsterUnits = Object.FindObjectsByType<MonsterUnit>(FindObjectsSortMode.None);

        if (monsterUnits == null || monsterUnits.Length == 0)
            return 0;

        HashSet<string> monsterKeys = new();

        for (int i = 0; i < monsterUnits.Length; i++)
        {
            MonsterUnit unit = monsterUnits[i];

            if (unit == null || unit.RuntimeData == null)
                continue;

            if (!unit.gameObject.scene.IsValid() || !unit.gameObject.scene.isLoaded)
                continue;

            string key = GetMonsterUnitSceneKey(unit);

            if (!string.IsNullOrWhiteSpace(key))
                monsterKeys.Add(key);
        }

        return monsterKeys.Count;
    }

    private string GetMonsterUnitSceneKey(MonsterUnit unit)
    {
        if (unit == null)
            return null;

        MonsterRuntimeData runtime = unit.RuntimeData;

        if (runtime != null && !string.IsNullOrWhiteSpace(runtime.RuntimeId))
            return $"Runtime:{runtime.RuntimeId.Trim()}";

        return $"Unit:{unit.GetInstanceID()}";
    }

    private void TrimUniqueItemRewardsToMonsterCount(List<BattleRewardData> rewards, int uniqueItemLimit)
    {
        if (rewards == null)
            return;

        uniqueItemLimit = Mathf.Max(0, uniqueItemLimit);

        for (int i = rewards.Count - 1; i >= 0; i--)
        {
            BattleRewardData reward = rewards[i];

            if (reward == null || reward.Type != BattleRewardType.Item)
                continue;

            int order = CountUniqueItemRewardsBeforeOrAt(rewards, i);

            if (order <= uniqueItemLimit)
                continue;

            Debug.LogWarning($"[BattleRewardResolver] 고유아이템 보상이 몬스터 수보다 많아 제거합니다. Limit:{uniqueItemLimit} / Removed:{reward.RewardId}");
            rewards.RemoveAt(i);
        }
    }

    private int CountUniqueItemRewardsBeforeOrAt(List<BattleRewardData> rewards, int index)
    {
        if (rewards == null)
            return 0;

        int count = 0;
        int lastIndex = Mathf.Min(index, rewards.Count - 1);

        for (int i = 0; i <= lastIndex; i++)
        {
            BattleRewardData reward = rewards[i];

            if (reward != null && reward.Type == BattleRewardType.Item)
                count++;
        }

        return count;
    }

    private void TryResolveMonster(
        MonsterRuntimeData monster,
        List<BattleRewardData> rewards,
        string monsterKey,
        HashSet<string> uniqueItemResolvedMonsterKeys)
    {
        if (monster == null)
            return;

        Debug.Log($"[BattleRewardResolver] Monster:{monster.MonsterId} / Key:{monsterKey} / Remnant:{monster.MinRemnant}-{monster.MaxRemnant} / Item:{monster.UniqueItemId}({monster.UniqueItemChance}) / Relic:{monster.RelicChance}");

        AddRemnant(monster, rewards);
        AddUniqueItem(monster, rewards, monsterKey, uniqueItemResolvedMonsterKeys);
        AddRelic(monster, rewards);
    }

    private void AddRemnant(MonsterRuntimeData monster, List<BattleRewardData> rewards)
    {
        int min = Mathf.Max(0, monster.MinRemnant);
        int max = Mathf.Max(min, monster.MaxRemnant);

        if (max <= 0)
            return;

        int amount = Random.Range(min, max + 1);

        if (amount <= 0)
            return;

        BattleRewardData existing = rewards.Find(x => x != null && x.Type == BattleRewardType.Remnant);

        if (existing != null)
        {
            existing.Amount += amount;
            existing.Name = "더스티움";
            existing.Description = "";
            return;
        }

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Remnant,
            RewardId = "0",
            Amount = amount,
            Name = "더스티움",
            Description = ""
        });
    }

    private void AddUniqueItem(
        MonsterRuntimeData monster,
        List<BattleRewardData> rewards,
        string monsterKey,
        HashSet<string> uniqueItemResolvedMonsterKeys)
    {
        if (string.IsNullOrWhiteSpace(monster.UniqueItemId))
            return;

        string itemMonsterKey = string.IsNullOrWhiteSpace(monsterKey) ? GetMonsterRewardKey(monster) : monsterKey;

        if (uniqueItemResolvedMonsterKeys != null && !uniqueItemResolvedMonsterKeys.Add(itemMonsterKey))
        {
            Debug.LogWarning($"[BattleRewardResolver] 이 몬스터의 고유아이템 판정은 이미 처리되었습니다. MonsterKey:{itemMonsterKey} / MonsterId:{monster.MonsterId}");
            return;
        }

        string uniqueItemId = monster.UniqueItemId.Trim();
        string uniqueRewardKey = $"{itemMonsterKey}|Item|{uniqueItemId}";

        if (HasRewardSource(rewards, uniqueRewardKey))
            return;

        if (!IsChanceSuccess(monster.UniqueItemChance))
            return;

        ItemData item = DataManager.Instance.ItemDatabase.Get(uniqueItemId);

        Sprite icon = null;

        if (DataManager.Instance.ItemIconDatabase != null)
            DataManager.Instance.ItemIconDatabase.TryGetIcon(uniqueItemId, out icon);

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Item,
            RewardId = uniqueItemId,
            SourceKey = uniqueRewardKey,
            Amount = 1,
            Icon = icon,
            Name = item != null ? item.Name : uniqueItemId,
            Description = item != null ? item.Desc : "",
            Value = item != null ? item.Value : 0
        });
    }

    private void AddRelic(MonsterRuntimeData monster, List<BattleRewardData> rewards)
    {
        if (!IsChanceSuccess(monster.RelicChance))
            return;

        RelicData relic = GetRandomAvailableRelic(rewards);

        if (relic == null)
        {
            Debug.Log("[BattleRewardResolver] 획득 가능한 새 유물이 없습니다.");
            return;
        }

        Sprite icon = null;

        if (DataManager.Instance.RelicIconDatabase != null)
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relic.FragmentId, out icon);

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Relic,
            RewardId = relic.FragmentId,
            SourceKey = $"Relic|{relic.FragmentId}",
            Amount = 1,
            Icon = icon,
            Name = relic.Name,
            Description = relic.EffectDesc,
            Value = 0
        });
    }

    private void AddSkillReward(List<BattleRewardData> rewards)
    {
        if (DataManager.Instance == null)
            return;

        BattleMapData dropSettings = GetCurrentBattleMapDropSettings();

        if (dropSettings == null)
            return;

        IReadOnlyList<SkillMasterData> candidates = GetAvailableSkillRewardCandidates(rewards);

        if (!SkillRewardRoller.TryRoll(
                dropSettings,
                candidates,
                new UnitySkillRewardRandom(),
                out SkillMasterData skill))
        {
            return;
        }

        Sprite icon = skill.Icon;

        if (icon == null && DataManager.Instance.SkillIconDatabase != null)
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Skill,
            RewardId = skill.SkillId,
            SourceKey = $"Skill|{dropSettings.BattleMapId}|{skill.SkillId}",
            Amount = 1,
            Icon = icon,
            Name = string.IsNullOrWhiteSpace(skill.Name) ? skill.SkillId : skill.Name,
            Description = BuildSkillRewardDescription(skill),
            Value = 0
        });
    }

    private BattleMapData GetCurrentBattleMapDropSettings()
    {
        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();

        if (mapRuntime == null || string.IsNullOrWhiteSpace(mapRuntime.CurrentMapId))
            return null;

        MapData mapData = DataManager.Instance.MapDatabase.Get(mapRuntime.CurrentMapId);

        if (mapData == null || string.IsNullOrWhiteSpace(mapData.BattleMapId))
            return null;

        return DataManager.Instance.BattleMapDatabase.GetDropSettings(mapData.BattleMapId);
    }

    private IReadOnlyList<SkillMasterData> GetAvailableSkillRewardCandidates(List<BattleRewardData> pendingRewards)
    {
        List<SkillMasterData> candidates = new();

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
            return candidates;

        HashSet<string> unavailableSkillIds = GetUnavailableSkillIds(pendingRewards);
        List<SkillMasterData> allSkills = DataManager.Instance.SkillDatabase.GetAll();

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillMasterData skill = allSkills[i];

            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            if (unavailableSkillIds.Contains(skill.SkillId.Trim()))
                continue;

            candidates.Add(skill);
        }

        return candidates;
    }

    private HashSet<string> GetUnavailableSkillIds(List<BattleRewardData> pendingRewards)
    {
        HashSet<string> ids = new();

        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.SkillInventoryIds != null)
        {
            for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
                AddSkillId(ids, runtime.SkillInventoryIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance?.CharacterRuntimeStore?.GetAll();

        if (characters != null)
        {
            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;

                if (character == null)
                    continue;

                AddSkillId(ids, character.MoveSkillId);
                AddSkillId(ids, character.PassiveSkillId);
                AddSkillId(ids, character.UniqueSkillId);
                AddSkillId(ids, character.AbilitySkillId);

                if (character.EquippedSkillIds == null)
                    continue;

                for (int i = 0; i < character.EquippedSkillIds.Length; i++)
                    AddSkillId(ids, character.EquippedSkillIds[i]);
            }
        }

        if (pendingRewards != null)
        {
            for (int i = 0; i < pendingRewards.Count; i++)
            {
                BattleRewardData reward = pendingRewards[i];

                if (reward == null || reward.Type != BattleRewardType.Skill)
                    continue;

                AddSkillId(ids, reward.RewardId);
            }
        }

        return ids;
    }

    private void AddSkillId(HashSet<string> ids, string skillId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(skillId))
            return;

        string normalizedSkillId = skillId.Trim();
        ids.Add(normalizedSkillId);

        if (SkillRarityUtility.TryGetPairedVariantId(normalizedSkillId, out string pairedSkillId))
            ids.Add(pairedSkillId);
    }

    private string BuildSkillRewardDescription(SkillMasterData skill)
    {
        if (skill == null)
            return "";

        string rarityName = SkillRarityUtility.GetDisplayName(skill.Rarity);
        string description = !string.IsNullOrWhiteSpace(skill.Details)
            ? skill.Details
            : skill.ToolTip;

        if (string.IsNullOrWhiteSpace(description))
            description = "획득 가능한 스킬입니다.";

        if (string.IsNullOrWhiteSpace(rarityName))
            return description;

        return $"[{rarityName}] {description}";
    }

    private bool HasRewardSource(List<BattleRewardData> rewards, string sourceKey)
    {
        if (rewards == null || string.IsNullOrWhiteSpace(sourceKey))
            return false;

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] == null)
                continue;

            if (rewards[i].SourceKey == sourceKey)
                return true;
        }

        return false;
    }

    private string GetMonsterRewardKey(MonsterRuntimeData monster)
    {
        if (monster == null)
            return "Monster:null";

        if (!string.IsNullOrWhiteSpace(monster.RuntimeId))
            return $"Runtime:{monster.RuntimeId.Trim()}";

        return $"Reference:{RuntimeHelpers.GetHashCode(monster)}:{monster.MonsterId}";
    }

    private bool IsChanceSuccess(float chance)
    {
        if (chance <= 0f)
            return false;

        if (chance > 1f)
            chance *= 0.01f;

        chance = Mathf.Clamp01(chance);
        return Random.value <= chance;
    }

    private RelicData GetRandomAvailableRelic(List<BattleRewardData> pendingRewards)
    {
        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
            return null;

        IReadOnlyList<RelicData> allRelics = DataManager.Instance.RelicDatabase.GetAll();

        if (allRelics == null || allRelics.Count == 0)
            return null;

        HashSet<string> unavailableRelicIds = GetUnavailableRelicIds(pendingRewards);
        List<RelicData> candidates = new();

        for (int i = 0; i < allRelics.Count; i++)
        {
            RelicData relic = allRelics[i];

            if (relic == null || string.IsNullOrWhiteSpace(relic.FragmentId))
                continue;

            string relicId = relic.FragmentId.Trim();

            if (unavailableRelicIds.Contains(relicId))
                continue;

            candidates.Add(relic);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private HashSet<string> GetUnavailableRelicIds(List<BattleRewardData> pendingRewards)
    {
        HashSet<string> ids = new();

        if (DataManager.Instance == null)
            return ids;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.OwnedRelicIds != null)
        {
            for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
                AddRelicId(ids, runtime.OwnedRelicIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance.CharacterRuntimeStore?.GetAll();

        if (characters != null)
        {
            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;

                if (character?.EquippedRelicIds == null)
                    continue;

                for (int i = 0; i < character.EquippedRelicIds.Length; i++)
                    AddRelicId(ids, character.EquippedRelicIds[i]);
            }
        }

        if (pendingRewards != null)
        {
            for (int i = 0; i < pendingRewards.Count; i++)
            {
                BattleRewardData reward = pendingRewards[i];

                if (reward == null || reward.Type != BattleRewardType.Relic)
                    continue;

                AddRelicId(ids, reward.RewardId);
            }
        }

        return ids;
    }

    private void AddRelicId(HashSet<string> ids, string relicId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(relicId))
            return;

        ids.Add(relicId.Trim());
    }
}
