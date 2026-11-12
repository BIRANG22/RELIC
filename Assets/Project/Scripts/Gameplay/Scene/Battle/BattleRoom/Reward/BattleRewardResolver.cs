using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleRewardResolver : MonoBehaviour
{
    public List<BattleRewardData> Resolve(IReadOnlyList<MonsterRuntimeData> monsters)
    {
        List<BattleRewardData> rewards = new();

        Debug.Log($"[BattleRewardResolver] Resolve Start / MonsterCount:{monsters?.Count ?? 0}");

        // A tutorial reward belongs to the cleared battle map, even when the
        // monster collector has no surviving entries at the moment of resolution.
        if (DataManager.Instance == null)
            return rewards;
        if (monsters == null)
            monsters = System.Array.Empty<MonsterRuntimeData>();

        bool guaranteeTutorialMonsterItems = IsTutorialBattle();

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

            TryResolveMonster(monster, rewards, monsterKey, uniqueItemResolvedMonsterKeys, guaranteeTutorialMonsterItems);
        }

        // Tutorial drops are derived from the configured battle-map spawns, not from
        // the death collector. That collector can be empty by the reward-screen phase.
        if (guaranteeTutorialMonsterItems)
        {
            AddTutorialBattleMapMonsterItems(rewards);
        }
        else
        {
            int uniqueMonsterCount = resolvedMonsterKeys.Count;
            int sceneMonsterCount = GetCurrentBattleMonsterUnitCount();
            int uniqueItemLimit = sceneMonsterCount > 0
                ? Mathf.Min(uniqueMonsterCount, sceneMonsterCount)
                : uniqueMonsterCount;

            TrimUniqueItemRewardsToMonsterCount(rewards, uniqueItemLimit);
        }

        BattleMapData dropSettings = GetCurrentBattleMapDropSettings();
        AddBattleMapRelicReward(rewards, dropSettings);
        AddMemoryReward(rewards, dropSettings);
        MergeDuplicateItemRewards(rewards);
        AddQuestClearRewards(rewards);
        NormalizeQuestItemRewards(rewards);

        return rewards;
    }

    private static void NormalizeQuestItemRewards(List<BattleRewardData> rewards)
    {
        if (rewards == null || DataManager.Instance == null)
            return;

        HashSet<string> seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < rewards.Count; i++)
        {
            BattleRewardData reward = rewards[i];
            if (reward == null || reward.Type != BattleRewardType.Item || string.IsNullOrWhiteSpace(reward.RewardId))
                continue;

            string id = reward.RewardId.Trim();
            ItemData item = DataManager.Instance.ItemDatabase?.Get(id);
            if ((item == null || item.RewardStage <= 0) && !string.Equals(id, "Item_013", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (!seen.Add(id))
            {
                rewards.RemoveAt(i--);
                continue;
            }

            reward.Amount = 1;
            // Quest rewards must precede currency and ordinary drops.
            if (i > 0)
            {
                rewards.RemoveAt(i);
                rewards.Insert(0, reward);
            }
        }
    }

    private static void AddQuestClearRewards(List<BattleRewardData> rewards)
    {
        BattleRuntimeData battle = DataManager.Instance?.BattleRuntimeStore?.Get();
        MapRuntimeData map = DataManager.Instance?.MapRuntimeStore?.Get();
        if (battle?.QuestRewardItemIds == null || map == null ||
            !string.Equals(map.CurrentStage, "Stage1", System.StringComparison.OrdinalIgnoreCase))
            return;

        foreach (string itemId in battle.QuestRewardItemIds)
        {
            if (string.IsNullOrWhiteSpace(itemId) || rewards.Exists(r => r.Type == BattleRewardType.Item && r.RewardId == itemId))
                continue;
            ItemData item = DataManager.Instance.ItemDatabase?.Get(itemId);
            Sprite icon = null;
            DataManager.Instance.ItemIconDatabase?.TryGetIcon(itemId, out icon);
            rewards.Insert(0, new BattleRewardData
            {
                Type = BattleRewardType.Item,
                RewardId = itemId,
                SourceKey = "QuestFirstClear|" + itemId,
                Amount = 1,
                Icon = icon,
                Name = item != null ? GameDataLocalization.ItemName(item) : itemId,
                Description = item != null ? GameDataLocalization.ItemDescription(item) : string.Empty
            });
        }
    }

    private void MergeDuplicateItemRewards(List<BattleRewardData> rewards)
    {
        if (rewards == null || rewards.Count <= 1)
            return;

        Dictionary<string, BattleRewardData> firstRewardByItemId = new();

        for (int i = 0; i < rewards.Count; i++)
        {
            BattleRewardData reward = rewards[i];

            if (reward == null || reward.Type != BattleRewardType.Item || string.IsNullOrWhiteSpace(reward.RewardId))
                continue;

            string itemId = reward.RewardId.Trim();
            reward.RewardId = itemId;
            reward.Amount = Mathf.Max(1, reward.Amount);

            if (!firstRewardByItemId.TryGetValue(itemId, out BattleRewardData existing))
            {
                firstRewardByItemId.Add(itemId, reward);
                continue;
            }

            existing.Amount += Mathf.Max(1, reward.Amount);
            rewards.RemoveAt(i);
            i--;
        }
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
        HashSet<string> uniqueItemResolvedMonsterKeys,
        bool guaranteeTutorialMonsterItems)
    {
        if (monster == null)
            return;

        Debug.Log($"[BattleRewardResolver] Monster:{monster.MonsterId} / Key:{monsterKey} / Remnant:{monster.MinRemnant}-{monster.MaxRemnant} / Item:{monster.UniqueItemId}({monster.UniqueItemChance})");

        // Monster currency follows the normal reward rules in all battles.
        // Tutorial unique items are granted from the actual configured battle map below.
        AddRemnant(monster, rewards);
        if (!guaranteeTutorialMonsterItems)
            AddUniqueItem(monster, rewards, monsterKey, uniqueItemResolvedMonsterKeys, false);
    }

    private static bool IsTutorialBattle()
    {
        BattleRuntimeData battle = DataManager.Instance?.BattleRuntimeStore?.Get();
        return battle?.IsTutorialBattle == true;
    }

    private void AddRemnant(MonsterRuntimeData monster, List<BattleRewardData> rewards)
    {
        int min = Mathf.Max(0, monster.MinRemnant);
        int max = Mathf.Max(min, monster.MaxRemnant);

        if (max <= 0)
            return;

        int amount = BattleRandom.Range(min, max + 1);
        amount = BattleEquipmentEffectService.ModifyBattleRewardCurrencyAmount(amount);

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

    private void AddTutorialBattleMapMonsterItems(List<BattleRewardData> rewards)
    {
        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();
        if (mapRuntime == null || string.IsNullOrWhiteSpace(mapRuntime.CurrentMapId))
        {
            Debug.LogWarning("[BattleRewardResolver] Tutorial monster drops: current map missing.");
            return;
        }

        MapData map = DataManager.Instance.MapDatabase?.Get(mapRuntime.CurrentMapId);
        if (map == null || string.IsNullOrWhiteSpace(map.BattleMapId))
        {
            Debug.LogWarning($"[BattleRewardResolver] Tutorial monster drops: battle map missing for {mapRuntime.CurrentMapId}.");
            return;
        }

        IReadOnlyList<BattleMapData> spawns = DataManager.Instance.BattleMapDatabase?.GetSpawns(map.BattleMapId);
        if (spawns == null || spawns.Count == 0)
        {
            Debug.LogWarning($"[BattleRewardResolver] Tutorial monster drops: no spawns for {map.BattleMapId}.");
            return;
        }

        int added = 0;
        for (int i = 0; i < spawns.Count; i++)
        {
            BattleMapData spawn = spawns[i];
            if (spawn == null || string.IsNullOrWhiteSpace(spawn.MonsterId))
                continue;

            MonsterMasterData monster = DataManager.Instance.MonsterDatabase?.Get(spawn.MonsterId.Trim());
            if (monster == null || string.IsNullOrWhiteSpace(monster.UniqueItemId) || monster.UniqueItemId == "0")
                continue;

            string itemId = monster.UniqueItemId.Trim();
            ItemData item = DataManager.Instance.ItemDatabase?.Get(itemId);
            if (item == null)
            {
                Debug.LogWarning($"[BattleRewardResolver] Tutorial drop item not found: {itemId} ({spawn.MonsterId}).");
                continue;
            }

            Sprite icon = null;
            DataManager.Instance.ItemIconDatabase?.TryGetIcon(itemId, out icon);
            rewards.Add(new BattleRewardData
            {
                Type = BattleRewardType.Item,
                RewardId = itemId,
                SourceKey = $"TutorialMonster|{map.BattleMapId}|{i}|{itemId}",
                Amount = 1,
                Icon = icon,
                Name = GameDataLocalization.ItemName(item),
                Description = GameDataLocalization.ItemDescription(item),
                Value = 0
            });
            added++;
        }

        Debug.Log($"[BattleRewardResolver] Tutorial guaranteed monster items / BattleMap:{map.BattleMapId} / SpawnCount:{spawns.Count} / Added:{added}");
    }

    private void AddUniqueItem(
        MonsterRuntimeData monster,
        List<BattleRewardData> rewards,
        string monsterKey,
        HashSet<string> uniqueItemResolvedMonsterKeys,
        bool guaranteeDrop)
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

        if (!guaranteeDrop && !IsChanceSuccess(monster.UniqueItemChance))
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
            Name = item != null ? GameDataLocalization.ItemName(item) : uniqueItemId,
            Description = item != null ? GameDataLocalization.ItemDescription(item) : "",
            Value = 0
        });
    }

    private void AddBattleMapRelicReward(
        List<BattleRewardData> rewards,
        BattleMapData dropSettings)
    {
        if (DataManager.Instance == null)
            return;

        // The second tutorial battle awards a memory instead of a relic.
        if (IsSecondTutorialBattle())
            return;

        // Other tutorial battles still award their fixed relic.
        if (TryGetTutorialBattleRelicId(out string tutorialRelicId))
        {
            if (DataManager.Instance.RelicDatabase == null ||
                !DataManager.Instance.RelicDatabase.TryGet(tutorialRelicId, out RelicData tutorialRelic) ||
                tutorialRelic == null)
            {
                Debug.LogError($"[BattleRewardResolver] Tutorial relic not found: {tutorialRelicId}");
                return;
            }

            string battleMapId = dropSettings != null ? dropSettings.BattleMapId : "Tutorial";
            AddRelicReward(rewards, tutorialRelic, battleMapId);
            return;
        }

        if (dropSettings == null)
            return;

        if (!TryRollRelicRarity(dropSettings, out RelicRarity rarity))
        {
            Debug.LogWarning($"[BattleRewardResolver] 유물 등급 확률 합계가 0입니다. BattleMapId:{dropSettings.BattleMapId}");
            return;
        }

        // 전투 보상 유물은 항상 1개를 생성합니다.
        // 우선 아직 보유하지 않은 같은 등급 유물을 선택하고, 해당 등급을 모두 보유했다면
        // 같은 등급의 유물 전체에서 다시 선택해 보상 자체가 사라지지 않도록 합니다.
        RelicData relic = GetRandomRelicByRarity(rewards, rarity, true);

        if (relic == null)
        {
            relic = GetRandomRelicByRarity(rewards, rarity, false);

            if (relic != null)
            {
                Debug.LogWarning(
                    $"[BattleRewardResolver] {rarity} 등급의 미보유 유물이 없어 중복 가능한 풀에서 유물을 선택합니다. " +
                    $"BattleMapId:{dropSettings.BattleMapId} / RelicId:{relic.FragmentId}");
            }
        }

        if (relic == null)
        {
            Debug.LogWarning(
                $"[BattleRewardResolver] {rarity} 등급 유물을 찾을 수 없습니다. " +
                $"BattleMapId:{dropSettings.BattleMapId}");
            return;
        }

        AddRelicReward(rewards, relic, dropSettings.BattleMapId);
    }

    private static bool TryGetTutorialBattleRelicId(out string relicId)
    {
        relicId = null;
        if (!IsTutorialBattle())
            return false;

        string mapId = DataManager.Instance?.MapRuntimeStore?.Get()?.CurrentMapId;
        if (string.IsNullOrWhiteSpace(mapId))
            return false;

        switch (mapId.Trim())
        {
            case "Map_26": relicId = "Relic_P_14"; break;
            case "Map_30": relicId = "Relic_P_68"; break;
        }

        return relicId != null;
    }

    private static bool IsSecondTutorialBattle()
    {
        return IsTutorialBattle() &&
            string.Equals(DataManager.Instance?.MapRuntimeStore?.Get()?.CurrentMapId?.Trim(),
                "Map_28", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void AddRelicReward(List<BattleRewardData> rewards, RelicData relic, string battleMapId)
    {
        Sprite icon = null;
        DataManager.Instance.RelicIconDatabase?.TryGetIcon(relic.FragmentId, out icon);

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Relic,
            RewardId = relic.FragmentId,
            SourceKey = $"Relic|{battleMapId}|{relic.FragmentId}",
            Amount = 1,
            Icon = icon,
            Name = GameDataLocalization.RelicName(relic),
            Description = GameDataLocalization.RelicEffectDescription(relic),
            Value = 0
        });
    }

    private bool TryRollRelicRarity(BattleMapData mapData, out RelicRarity rarity)
    {
        rarity = RelicRarity.None;

        if (mapData == null)
            return false;

        float common = Mathf.Max(0f, mapData.RelicCommonChance);
        float rare = Mathf.Max(0f, mapData.RelicRareChance);
        float epic = Mathf.Max(0f, mapData.RelicEpicChance);
        float unique = Mathf.Max(0f, mapData.RelicUniqueChance);

        if (BattleErosionEffectService.ShouldReduceRarity)
        {
            common += rare;
            rare = epic;
            epic = unique;
            unique = 0f;
        }

        float total = common + rare + epic + unique;

        if (total <= 0f)
            return false;

        float roll = BattleRandom.Value() * total;

        if (roll < common)
        {
            rarity = RelicRarity.Common;
            return true;
        }

        roll -= common;

        if (roll < rare)
        {
            rarity = RelicRarity.Rare;
            return true;
        }

        roll -= rare;

        if (roll < epic)
        {
            rarity = RelicRarity.Epic;
            return true;
        }

        rarity = RelicRarity.Unique;
        return true;
    }

    private void AddMemoryReward(
        List<BattleRewardData> rewards,
        BattleMapData dropSettings)
    {
        if (DataManager.Instance == null || dropSettings == null)
            return;

        // The second tutorial battle always offers these three Hilt core memories,
        // in this exact card order. The player chooses one; they are not all granted.
        if (IsSecondTutorialBattle())
        {
            AddSecondTutorialFixedMemoryOffer(rewards, dropSettings);
            return;
        }

        IReadOnlyList<SkillMasterData> candidates = GetAvailableSkillRewardCandidates(rewards);

        BattleMapData memorySettings = dropSettings;

        if (!SkillRewardRoller.TryRoll(
                memorySettings,
                candidates,
                GetPartyCharacterIds(),
                new BattleSkillRewardRandom(),
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
            Name = string.IsNullOrWhiteSpace(skill.Name) ? skill.SkillId : GameDataLocalization.SkillName(skill),
            Description = BuildSkillRewardDescription(skill),
            Value = 0
        });
    }

    private void AddSecondTutorialFixedMemoryOffer(
        List<BattleRewardData> rewards,
        BattleMapData dropSettings)
    {
        const string ownerId = "Char_01";
        string[] memoryIds = { "S_Core_01", "S_Core_04", "S_Core_03" };
        var skillDatabase = DataManager.Instance?.SkillDatabase;
        if (skillDatabase == null)
        {
            Debug.LogError("[BattleRewardResolver] Map_28: skill database missing.");
            return;
        }

        SkillMasterData first = null;
        foreach (string memoryId in memoryIds)
        {
            if (!skillDatabase.TryGet(memoryId, out SkillMasterData skill) || skill == null)
            {
                Debug.LogError($"[BattleRewardResolver] Map_28: fixed memory missing: {memoryId}");
                return;
            }
            if (!string.Equals(skill.CharacterId?.Trim(), ownerId,
                System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[BattleRewardResolver] Map_28: {memoryId} is not owned by {ownerId}.");
                return;
            }
            if (first == null)
                first = skill;
        }

        Sprite icon = first.Icon;
        if (icon == null)
            DataManager.Instance.SkillIconDatabase?.TryGetIcon(first.SkillId, out icon);

        // BattleRewardSkillPanelUI reads BattleOwner and BattleOffers from SourceKey.
        // This bypasses its random character and memory selection for this map only.
        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Skill,
            RewardId = first.SkillId,
            SourceKey = $"Skill|{dropSettings.BattleMapId}|BattleOwner={ownerId}|BattleOffers={string.Join(",", memoryIds)}",
            Amount = 1,
            Icon = icon,
            Name = string.IsNullOrWhiteSpace(first.Name) ? first.SkillId : GameDataLocalization.SkillName(first),
            Description = BuildSkillRewardDescription(first),
            Value = 0
        });
        Debug.Log($"[BattleRewardResolver] Map_28 fixed Hilt memory offers: {string.Join(", ", memoryIds)}");
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

        return SkillRewardPoolPolicy.FilterCandidates(
            candidates,
            DataManager.Instance.SkillRewardPoolDatabase);
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

    private static IEnumerable<string> GetPartyCharacterIds()
    {
        PartyRuntimeStore partyStore = DataManager.Instance?.PartyRuntimeStore;
        if (partyStore == null)
            yield break;

        IReadOnlyList<PartySlotRuntimeData> slots = partyStore.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            string characterId = slots[i]?.CharacterId;
            if (!string.IsNullOrWhiteSpace(characterId))
                yield return characterId;
        }
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

        string rarityName = SkillRarityUtility.GetDisplayName(skill);
        string description = GameDataLocalization.SkillDetails(skill);

        if (string.IsNullOrWhiteSpace(description))
            description = GameLocalization.Get("battle.available_skill");

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
        return BattleRandom.Value() <= chance;
    }

    private RelicData GetRandomRelicByRarity(
        List<BattleRewardData> pendingRewards,
        RelicRarity targetRarity,
        bool excludeUnavailable)
    {
        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
            return null;

        IReadOnlyList<RelicData> allRelics = DataManager.Instance.RelicDatabase.GetAll();

        if (allRelics == null || allRelics.Count == 0)
            return null;

        HashSet<string> unavailableRelicIds = excludeUnavailable
            ? GetUnavailableRelicIds(pendingRewards)
            : null;
        List<RelicData> candidates = new();

        for (int i = 0; i < allRelics.Count; i++)
        {
            RelicData relic = allRelics[i];

            if (relic == null || string.IsNullOrWhiteSpace(relic.FragmentId))
                continue;

            if (!RelicRarityUtility.TryParseChestRarity(relic.Rarity, out RelicRarity relicRarity))
                continue;

            if (relicRarity != targetRarity)
                continue;

            string relicId = relic.FragmentId.Trim();

            if (unavailableRelicIds != null && unavailableRelicIds.Contains(relicId))
                continue;

            candidates.Add(relic);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[BattleRandom.Range(0, candidates.Count)];
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
