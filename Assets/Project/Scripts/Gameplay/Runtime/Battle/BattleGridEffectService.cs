using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public sealed class BattleGridEffectApplyResult
    {
        public static readonly BattleGridEffectApplyResult None = new();

        private readonly List<string> appliedEffectIds = new();

        public bool Applied { get; private set; }
        public bool Consumed { get; private set; }
        public int GridIndex { get; private set; } = -1;
        public string GridEffectId { get; private set; }
        public IReadOnlyList<string> AppliedEffectIds => appliedEffectIds;

        public static BattleGridEffectApplyResult Create(
            int gridIndex,
            string gridEffectId,
            bool applied,
            bool consumed,
            IReadOnlyList<string> effectIds)
        {
            BattleGridEffectApplyResult result = new()
            {
                GridIndex = gridIndex,
                GridEffectId = gridEffectId,
                Applied = applied,
                Consumed = consumed
            };

            if (effectIds != null)
                result.appliedEffectIds.AddRange(effectIds);

            return result;
        }
    }

    public sealed class BattleGridEffectService
    {
        private const string SpiderWebGridEffectId = "GR_spider_web";
        private const string SpiderWebEffectId = "E_Spider_Web";
        private const int SpiderWebPendingTurnCount = 2;
        private const int DefaultMinSpawnCount = 2;
        private const int DefaultMaxSpawnCount = 3;

        private readonly GridEffectDatabase database;

        public BattleGridEffectService(GridEffectDatabase database)
        {
            this.database = database;
        }

        public IReadOnlyList<BattleGridEffectPlacement> SpawnRandomEffects(
            BattleGridEffectState state,
            int width,
            int height,
            IReadOnlyCollection<int> excludedGridIndices,
            int minCount = DefaultMinSpawnCount,
            int maxCount = DefaultMaxSpawnCount)
        {
            List<BattleGridEffectPlacement> placements = new();

            if (state == null || database == null || width <= 0 || height <= 0)
                return placements;

            List<string> effectIds = GetEffectIds();

            if (effectIds.Count <= 0)
                return placements;

            List<int> availableGridIndices = BuildAvailableGridIndices(
                state,
                width,
                height,
                excludedGridIndices
            );

            if (availableGridIndices.Count <= 0)
                return placements;

            int safeMin = Mathf.Max(0, minCount);
            int safeMax = Mathf.Max(safeMin, maxCount);
            int spawnCount = BattleRandom.Range(safeMin, safeMax + 1);
            spawnCount = Mathf.Min(spawnCount, availableGridIndices.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                int cellPickIndex = BattleRandom.Range(0, availableGridIndices.Count);
                int gridIndex = availableGridIndices[cellPickIndex];
                availableGridIndices.RemoveAt(cellPickIndex);

                string gridEffectId = BattleRandom.Pick(effectIds);

                if (!database.TryGet(gridEffectId, out GridEffectData gridEffectData) ||
                    gridEffectData == null ||
                    !state.Place(
                        gridIndex,
                        gridEffectId,
                        gridEffectData.Duration,
                        Mathf.Max(0, gridEffectData.HP)))
                {
                    continue;
                }

                placements.Add(new BattleGridEffectPlacement(gridIndex, gridEffectId));
            }

            return placements;
        }

        public bool IsBlocked(BattleGridEffectState state, int gridIndex)
        {
            if (!TryGetGridEffectData(state, gridIndex, out GridEffectData data))
                return false;

            return data.Passed == 0;
        }

        public BattleGridEffectApplyResult ApplyToPlayer(
            BattleGridEffectState state,
            int gridIndex,
            CharacterRuntimeData runtimeData)
        {
            if (runtimeData == null || runtimeData.IsDead)
                return BattleGridEffectApplyResult.None;

            return ApplyToRuntime(
                state,
                gridIndex,
                data => ApplyPlayerEffect(runtimeData, data),
                () =>
                {
                    if (runtimeData.IsDead)
                        runtimeData.HandleDeath();
                }
            );
        }

        public BattleGridEffectApplyResult ApplyToMonster(
            BattleGridEffectState state,
            int gridIndex,
            MonsterRuntimeData runtimeData)
        {
            if (runtimeData == null || runtimeData.IsDead)
                return BattleGridEffectApplyResult.None;

            // 잔여물은 머크와 블롭이 생성하는 몬스터 전용 지형입니다.
            // 거미줄은 플레이어 전용 방해 지형이므로 몬스터가 지나가도 발동하거나 사라지지 않습니다.
            if (TryGetGridEffectData(state, gridIndex, out GridEffectData gridEffectData) &&
                (string.Equals(gridEffectData.GridEffectID, "GR_Residue", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(gridEffectData.GridEffectID, SpiderWebGridEffectId, StringComparison.OrdinalIgnoreCase)))
            {
                return BattleGridEffectApplyResult.None;
            }

            return ApplyToRuntime(
                state,
                gridIndex,
                data => ApplyMonsterEffect(runtimeData, data),
                null
            );
        }

        private BattleGridEffectApplyResult ApplyToRuntime(
            BattleGridEffectState state,
            int gridIndex,
            Func<GridEffectData, IReadOnlyList<string>> applyEffects,
            Action onDeath)
        {
            if (!TryGetGridEffectData(state, gridIndex, out GridEffectData data))
                return BattleGridEffectApplyResult.None;

            if (data.Passed == 0)
                return BattleGridEffectApplyResult.None;

            IReadOnlyList<string> appliedEffectIds = applyEffects(data);
            bool applied = appliedEffectIds != null && appliedEffectIds.Count > 0;
            bool consumed = applied &&
                (data.Consumable == 1 ||
                 string.Equals(data.GridEffectID, SpiderWebGridEffectId, StringComparison.OrdinalIgnoreCase));

            if (consumed)
                state.Remove(gridIndex);

            onDeath?.Invoke();

            return BattleGridEffectApplyResult.Create(
                gridIndex,
                data.GridEffectID,
                applied,
                consumed,
                appliedEffectIds
            );
        }

        private IReadOnlyList<string> ApplyPlayerEffect(CharacterRuntimeData runtimeData, GridEffectData data)
        {
            List<string> applied = new();
            string[] effectIds = SplitEffectIds(data.EffectIds);

            for (int i = 0; i < effectIds.Length; i++)
            {
                string effectId = effectIds[i].Trim();

                if (string.IsNullOrWhiteSpace(effectId))
                    continue;

                if (ApplyDamage(runtimeData, data.ValueRate, effectId) ||
                    ApplyArmor(runtimeData, data.ValueRate, effectId) ||
                    ApplyHeal(runtimeData, data.ValueRate, effectId) ||
                    ApplyStatus(runtimeData.StatusEffects, data.ValueRate, effectId))
                {
                    applied.Add(effectId);
                }
            }

            return applied;
        }

        private IReadOnlyList<string> ApplyMonsterEffect(MonsterRuntimeData runtimeData, GridEffectData data)
        {
            List<string> applied = new();
            string[] effectIds = SplitEffectIds(data.EffectIds);

            for (int i = 0; i < effectIds.Length; i++)
            {
                string effectId = effectIds[i].Trim();

                if (string.IsNullOrWhiteSpace(effectId))
                    continue;

                if (ApplyDamage(runtimeData, data.ValueRate, effectId) ||
                    ApplyArmor(runtimeData, data.ValueRate, effectId) ||
                    ApplyHeal(runtimeData, data.ValueRate, effectId) ||
                    ApplyStatus(runtimeData.StatusEffects, data.ValueRate, effectId))
                {
                    applied.Add(effectId);
                }
            }

            return applied;
        }

        private bool ApplyDamage(CharacterRuntimeData runtimeData, int value, string effectId)
        {
            if (!IsDamageEffect(effectId))
                return false;

            int damage = Mathf.Max(0, value);

            if (damage <= 0)
                return false;

            int hpBefore = runtimeData.CurrentHP;
            int shieldBefore = runtimeData.CurrentShield;

            int shieldDamage = Mathf.Min(runtimeData.CurrentShield, damage);
            runtimeData.CurrentShield -= shieldDamage;
            damage -= shieldDamage;

            if (damage > 0)
                runtimeData.CurrentHP = Mathf.Max(0, runtimeData.CurrentHP - damage);

            int hpDamage = Mathf.Max(0, hpBefore - runtimeData.CurrentHP);
            int appliedDamage = Mathf.Max(0, shieldBefore - runtimeData.CurrentShield) + hpDamage;

            if (appliedDamage > 0)
                BattleEquipmentEffectService.MarkPlayerDamagedThisTurn(runtimeData);

            return true;
        }

        private bool ApplyDamage(MonsterRuntimeData runtimeData, int value, string effectId)
        {
            if (!IsDamageEffect(effectId))
                return false;

            int damage = Mathf.Max(0, value);

            if (damage <= 0)
                return false;

            int shieldDamage = runtimeData.AbsorbShieldDamage(damage);
            damage -= shieldDamage;

            if (damage > 0)
                runtimeData.TakeDamage(damage);

            return true;
        }

        private bool ApplyArmor(CharacterRuntimeData runtimeData, int value, string effectId)
        {
            if (!IsArmorEffect(effectId))
                return false;

            int shield = BattleEquipmentEffectService.ModifyArmorGainForPlayer(
                runtimeData,
                Mathf.Max(0, value));

            if (shield <= 0)
                return false;

            runtimeData.CurrentShield += shield;
            return true;
        }

        private bool ApplyArmor(MonsterRuntimeData runtimeData, int value, string effectId)
        {
            if (!IsArmorEffect(effectId))
                return false;

            int shield = Mathf.Max(0, value);

            if (shield <= 0)
                return false;

            runtimeData.AddTemporaryShield(shield);
            return true;
        }

        private bool ApplyHeal(CharacterRuntimeData runtimeData, int value, string effectId)
        {
            if (!IsHealEffect(effectId))
                return false;

            int heal = Mathf.Max(0, value);

            if (heal <= 0)
                return false;

            if (BattleEquipmentEffectService.ShouldBlockPlayerHealing(runtimeData))
                return false;

            int overhealArmor = BattleEquipmentEffectService.GetOverhealArmorAmount(
                runtimeData,
                heal);

            if (overhealArmor > 0)
            {
                runtimeData.CurrentShield += BattleEquipmentEffectService.ModifyArmorGainForPlayer(
                    runtimeData,
                    overhealArmor);
                return true;
            }

            if (runtimeData.MaxHP > 0)
                runtimeData.CurrentHP = Mathf.Min(runtimeData.MaxHP, runtimeData.CurrentHP + heal);
            else
                runtimeData.CurrentHP += heal;

            return true;
        }

        private bool ApplyHeal(MonsterRuntimeData runtimeData, int value, string effectId)
        {
            if (!IsHealEffect(effectId))
                return false;

            int heal = Mathf.Max(0, value);

            if (heal <= 0)
                return false;

            runtimeData.Heal(heal);
            return true;
        }

        private bool ApplyStatus(
            List<StatusEffectRuntimeData> statusEffects,
            int value,
            string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return false;

            if (IsDamageEffect(effectId) || IsArmorEffect(effectId) || IsHealEffect(effectId))
                return false;

            string normalizedEffectId = effectId.Trim();

            if (string.Equals(normalizedEffectId, SpiderWebEffectId, StringComparison.OrdinalIgnoreCase))
            {
                if (statusEffects == null)
                    return false;

                int multiplier = Mathf.Max(1, value);

                for (int i = 0; i < statusEffects.Count; i++)
                {
                    StatusEffectRuntimeData existing = statusEffects[i];

                    if (existing == null ||
                        !string.Equals(existing.EffectId, SpiderWebEffectId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    existing.Stack = Mathf.Max(existing.Stack, multiplier);
                    existing.TurnCount = SpiderWebPendingTurnCount;
                    return true;
                }

                statusEffects.Add(new StatusEffectRuntimeData(
                    SpiderWebEffectId,
                    multiplier,
                    SpiderWebPendingTurnCount));
                return true;
            }

            return BattleEffectUtility.AddOrStackStatus(
                statusEffects,
                normalizedEffectId,
                Mathf.Max(1, value),
                1
            );
        }

        private bool TryGetGridEffectData(
            BattleGridEffectState state,
            int gridIndex,
            out GridEffectData data)
        {
            data = null;

            if (state == null || database == null)
                return false;

            if (!state.TryGetEffectId(gridIndex, out string gridEffectId))
                return false;

            return database.TryGet(gridEffectId, out data) && data != null;
        }

        private List<string> GetEffectIds()
        {
            List<string> effectIds = new();

            IReadOnlyDictionary<string, GridEffectData> all = database.GetAll();

            if (all == null)
                return effectIds;

            foreach (KeyValuePair<string, GridEffectData> pair in all)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    continue;

                if (!string.Equals(
                        pair.Value.SpawnType?.Trim(),
                        "BattleStart",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                effectIds.Add(pair.Key);
            }

            return effectIds;
        }

        private static List<int> BuildAvailableGridIndices(
            BattleGridEffectState state,
            int width,
            int height,
            IReadOnlyCollection<int> excludedGridIndices)
        {
            HashSet<int> excluded = excludedGridIndices != null
                ? new HashSet<int>(excludedGridIndices)
                : new HashSet<int>();

            if (state != null)
            {
                foreach (BattleGridEffectPlacement placement in state.GetPlacements())
                    excluded.Add(placement.GridIndex);
            }

            int cellCount = Mathf.Max(0, width * height);
            List<int> available = new();

            for (int i = 0; i < cellCount; i++)
            {
                if (!excluded.Contains(i))
                    available.Add(i);
            }

            return available;
        }

        private static string[] SplitEffectIds(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsDamageEffect(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return false;

            string normalized = effectId.Trim();
            return string.Equals(normalized, "E_Damage", StringComparison.Ordinal) ||
                   string.Equals(normalized, "E_Strike", StringComparison.Ordinal) ||
                   string.Equals(normalized, "E_Pierce", StringComparison.Ordinal);
        }

        private static bool IsArmorEffect(string effectId)
        {
            return string.Equals(effectId?.Trim(), "E_Armor", StringComparison.Ordinal);
        }

        private static bool IsHealEffect(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return false;

            string normalized = effectId.Trim();
            return string.Equals(normalized, "E_Focus", StringComparison.Ordinal) ||
                   normalized.IndexOf("Heal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   normalized.IndexOf("Recover", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
