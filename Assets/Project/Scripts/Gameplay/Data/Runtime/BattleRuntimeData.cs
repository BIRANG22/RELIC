using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class BattleRuntimeData
    {
        // 소지 재화
        public int Remnant;

        // 소지 렐릭
        public List<string> OwnedRelicIds = new();

        // 가방 내부 아이템
        public List<string> BagItemIds = new();

        // 스킬 인벤토리 내부 스킬
        public List<string> SkillInventoryIds = new();

        public List<string> StartingSkillInventoryIds = new();
        public List<string> AcquiredSkillIds = new();
        public List<BattleRunCharacterStatisticsData> CharacterStatistics = new();

        // 전투 진입 직전 로비 파티/스킬/룬 세팅 복구용 스냅샷
        public List<BattleLobbyLoadoutSnapshotData> LobbyLoadoutSnapshots = new();
        public List<CultureTankBattleStartEffectRuntimeData> CultureTankBattleStartEffects = new();

        // 탐사 시작 1회 룬 효과 적용 여부. BattleRuntimeData가 새 탐사마다 초기화되므로
        // 같은 탐사 안의 다음 전투방에서는 다시 적용되지 않습니다.
        public List<string> AppliedExplorationStartRuneCharacterIds = new();

        // 진행 상태
        public int CurrentBattleCount;
        public int CurrentRewardCount;

        public bool IsBattleRunInitialized;
    }

    [System.Serializable]
    public class BattleLobbyLoadoutSnapshotData
    {
        public string CharacterId;

        public string MoveSkillId;
        public string PassiveSkillId;
        public string UniqueSkillId;
        public string AbilitySkillId;

        public string[] EquippedSkillIds = new string[4];
        public string[] EquippedRuneIds = new string[6];
        public string[] EquippedRelicIds = new string[7];

        public int MaxHP;
        public int MaxCost;
        public int RunMaxHPBonus;
        public int RunMaxCostBonus;
        public int CostRecovery;
        public int BonusCostRecovery;

        public int CurrentHP;
        public int CurrentCost;
        public int CurrentResource;
        public int CurrentMoveLevel;

        public BattleDirection Direction = BattleDirection.Right;
    }

    public static class BattleRunAbandonService
    {
        private const int EquippedSkillSlotCount = 4;
        private const int EquippedRuneSlotCount = 6;
        private const int EquippedRelicSlotCount = 7;

        public static void CaptureLobbyLoadoutSnapshot(global::DataManager dataManager)
        {
            if (dataManager == null || dataManager.BattleRuntimeStore == null)
                return;

            BattleRuntimeData battleRuntime = dataManager.BattleRuntimeStore.GetOrCreate();
            battleRuntime.LobbyLoadoutSnapshots ??= new List<BattleLobbyLoadoutSnapshotData>();
            battleRuntime.LobbyLoadoutSnapshots.Clear();

            CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;
            PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;

            if (characterStore == null || partyStore == null)
            {
                dataManager.BattleRuntimeStore.Set(battleRuntime);
                return;
            }

            var capturedCharacterIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
            {
                string characterId = partyStore.GetCharacterId(i);

                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                characterId = characterId.Trim();

                if (!capturedCharacterIds.Add(characterId))
                    continue;

                if (!characterStore.TryGet(characterId, out CharacterRuntimeData character) ||
                    character == null)
                {
                    continue;
                }

                battleRuntime.LobbyLoadoutSnapshots.Add(CreateSnapshot(character));
            }

            dataManager.BattleRuntimeStore.Set(battleRuntime);
        }

        public static void AbandonCurrentRun(global::DataManager dataManager)
        {
            if (dataManager == null)
                return;

            BattleRuntimeData currentBattleRuntime = dataManager.BattleRuntimeStore?.Get();

            Dictionary<string, BattleLobbyLoadoutSnapshotData> snapshots =
                BuildSnapshotMap(currentBattleRuntime);

            RestoreCharacterLoadoutsAndClearBattleState(dataManager, snapshots);
            dataManager.PartyRuntimeStore?.ResetCurrentGridIndicesToSpawn();
            dataManager.SkillRuntimeStore?.Clear();
            dataManager.MapRuntimeStore?.Clear();
            dataManager.BattleRuntimeStore?.Clear();

            // 전투 포기 후에는 로비에 남아 있는 전투용 장비 정보도 반드시 비웁니다.
            // 캐릭터 런타임만 비우면 로비 재진입 시 CharacterLoadouts가 장비를 다시 복원할 수 있습니다.
            LobbyRuntimeData lobbyRuntime = dataManager.LobbyRuntimeStore?.GetOrCreate();
            if (lobbyRuntime != null)
            {
                ClearLobbyEquippedRelics(lobbyRuntime.CharacterLoadouts);

                // 전투 포기는 탐사 종료와 동일하게 취급합니다.
                // 이전 탐사에서 남은 구매 잠금/리롤/제안 정보를 정리해 유물소환을 다시 사용할 수 있게 합니다.
                global::LobbyRelicShopPurchaseLimit.ResetAfterExploration(lobbyRuntime);
                dataManager.LobbyRuntimeStore?.Set(lobbyRuntime);
            }
        }

        private static BattleLobbyLoadoutSnapshotData CreateSnapshot(CharacterRuntimeData character)
        {
            return new BattleLobbyLoadoutSnapshotData
            {
                CharacterId = character.CharacterId,
                MoveSkillId = character.MoveSkillId,
                PassiveSkillId = character.PassiveSkillId,
                UniqueSkillId = character.UniqueSkillId,
                AbilitySkillId = character.AbilitySkillId,
                EquippedSkillIds = CopyStringArray(character.EquippedSkillIds, EquippedSkillSlotCount),
                EquippedRuneIds = CopyStringArray(character.EquippedRuneIds, EquippedRuneSlotCount),
                EquippedRelicIds = CopyStringArray(character.EquippedRelicIds, EquippedRelicSlotCount),
                MaxHP = character.MaxHP,
                MaxCost = character.MaxCost,
                RunMaxHPBonus = character.RunMaxHPBonus,
                RunMaxCostBonus = character.RunMaxCostBonus,
                CostRecovery = character.CostRecovery,
                BonusCostRecovery = character.BonusCostRecovery,
                CurrentHP = character.CurrentHP,
                CurrentCost = character.CurrentCost,
                CurrentResource = character.CurrentResource,
                CurrentMoveLevel = character.CurrentMoveLevel,
                Direction = character.Direction
            };
        }

        private static Dictionary<string, BattleLobbyLoadoutSnapshotData> BuildSnapshotMap(
            BattleRuntimeData battleRuntime)
        {
            var snapshots = new Dictionary<string, BattleLobbyLoadoutSnapshotData>(StringComparer.Ordinal);

            if (battleRuntime?.LobbyLoadoutSnapshots == null)
                return snapshots;

            for (int i = 0; i < battleRuntime.LobbyLoadoutSnapshots.Count; i++)
            {
                BattleLobbyLoadoutSnapshotData snapshot = battleRuntime.LobbyLoadoutSnapshots[i];

                if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.CharacterId))
                    continue;

                snapshots[snapshot.CharacterId.Trim()] = snapshot;
            }

            return snapshots;
        }

        private static void RestoreCharacterLoadoutsAndClearBattleState(
            global::DataManager dataManager,
            IReadOnlyDictionary<string, BattleLobbyLoadoutSnapshotData> snapshots)
        {
            IReadOnlyDictionary<string, CharacterRuntimeData> characters =
                dataManager.CharacterRuntimeStore?.GetAll();

            if (characters == null)
                return;

            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;

                if (character == null || string.IsNullOrWhiteSpace(character.CharacterId))
                    continue;

                if (snapshots != null &&
                    snapshots.TryGetValue(character.CharacterId.Trim(), out BattleLobbyLoadoutSnapshotData snapshot))
                {
                    RestoreSnapshot(character, snapshot);
                }
                else if (IsCurrentPartyCharacter(dataManager.PartyRuntimeStore, character.CharacterId))
                {
                    RestoreBaseBattleStats(dataManager, character);
                    NormalizeUpgradedSkillVariantsToBase(character);
                }

                ClearBattleOnlyCharacterState(character);
            }
        }

        private static void RestoreSnapshot(
            CharacterRuntimeData character,
            BattleLobbyLoadoutSnapshotData snapshot)
        {
            if (character == null || snapshot == null)
                return;

            character.MoveSkillId = snapshot.MoveSkillId;
            character.PassiveSkillId = snapshot.PassiveSkillId;
            character.UniqueSkillId = snapshot.UniqueSkillId;
            character.AbilitySkillId = snapshot.AbilitySkillId;
            character.EquippedSkillIds = CopyStringArray(snapshot.EquippedSkillIds, EquippedSkillSlotCount);
            character.EquippedRuneIds = CopyStringArray(snapshot.EquippedRuneIds, EquippedRuneSlotCount);
            character.EquippedRelicIds = CopyStringArray(snapshot.EquippedRelicIds, EquippedRelicSlotCount);

            character.MaxHP = Mathf.Max(1, snapshot.MaxHP);
            character.MaxCost = Mathf.Max(0, snapshot.MaxCost);
            character.RunMaxHPBonus = snapshot.RunMaxHPBonus;
            character.RunMaxCostBonus = snapshot.RunMaxCostBonus;
            character.CostRecovery = Mathf.Max(0, snapshot.CostRecovery);
            character.BonusCostRecovery = snapshot.BonusCostRecovery;
            character.CurrentHP = Mathf.Clamp(snapshot.CurrentHP, 1, character.MaxHP);
            character.CurrentCost = Mathf.Clamp(snapshot.CurrentCost, 0, character.MaxCost);
            character.CurrentResource = Mathf.Max(0, snapshot.CurrentResource);
            character.CurrentMoveLevel = Mathf.Max(0, snapshot.CurrentMoveLevel);
            character.Direction = snapshot.Direction;
        }

        private static void RestoreBaseBattleStats(
            global::DataManager dataManager,
            CharacterRuntimeData character)
        {
            if (dataManager?.CharacterDatabase == null ||
                character == null ||
                string.IsNullOrWhiteSpace(character.CharacterId) ||
                !dataManager.CharacterDatabase.TryGet(character.CharacterId, out CharacterMasterData master))
            {
                return;
            }

            character.MaxHP = Mathf.Max(1, master.MaxHP);
            character.MaxCost = Mathf.Max(0, master.MaxCost);
            character.RunMaxHPBonus = 0;
            character.RunMaxCostBonus = 0;
            character.CostRecovery = Mathf.Max(0, master.CostRecovery);
            character.BonusCostRecovery = 0;
            character.CurrentHP = character.MaxHP;
            character.CurrentCost = character.MaxCost;
            character.CurrentResource = 0;
            // 이동값은 캐릭터 데이터가 아니라 이동 관련 효과에서 결정합니다.
            character.CurrentMoveLevel = 0;
            character.MoveSkillId = "S_Move_1";
            character.Direction = BattleDirection.Right;
        }

        private static void ClearBattleOnlyCharacterState(CharacterRuntimeData character)
        {
            if (character == null)
                return;

            character.CurrentShield = 0;
            character.ClearReservedCosts();

            character.StatusEffects ??= new List<StatusEffectRuntimeData>();
            character.StatusEffects.Clear();

            character.EquippedRelicIds = new string[EquippedRelicSlotCount];
            character.ActiveRelicUses ??= new List<ActiveRelicUseRuntimeData>();
            character.ActiveRelicUses.Clear();

            character.AppliedBattleEquipmentEffectIds ??= new List<string>();
            character.AppliedBattleEquipmentEffectIds.Clear();
        }


        private static void ClearLobbyEquippedRelics(IReadOnlyList<LobbyCharacterLoadoutData> loadouts)
        {
            if (loadouts == null)
                return;

            for (int i = 0; i < loadouts.Count; i++)
            {
                LobbyCharacterLoadoutData loadout = loadouts[i];
                if (loadout == null)
                    continue;

                loadout.EquippedRelicIds = new string[EquippedRelicSlotCount];
            }
        }

        private static bool IsCurrentPartyCharacter(PartyRuntimeStore partyStore, string characterId)
        {
            if (partyStore == null || string.IsNullOrWhiteSpace(characterId))
                return false;

            return partyStore.FindCharacterSlot(characterId.Trim()) >= 0;
        }

        private static void NormalizeUpgradedSkillVariantsToBase(CharacterRuntimeData character)
        {
            if (character == null)
                return;

            character.PassiveSkillId = ConvertUpgradeVariantToBase(character.PassiveSkillId);
            character.UniqueSkillId = ConvertUpgradeVariantToBase(character.UniqueSkillId);
            character.AbilitySkillId = ConvertUpgradeVariantToBase(character.AbilitySkillId);

            if (character.EquippedSkillIds == null)
                return;

            for (int i = 0; i < character.EquippedSkillIds.Length; i++)
                character.EquippedSkillIds[i] = ConvertUpgradeVariantToBase(character.EquippedSkillIds[i]);
        }

        private static string ConvertUpgradeVariantToBase(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return skillId;

            string trimmedSkillId = skillId.Trim();

            if (!SkillRarityUtility.IsUpgradeSkillVariant(trimmedSkillId))
                return skillId;

            return SkillRarityUtility.TryGetPairedVariantId(trimmedSkillId, out string baseSkillId)
                ? baseSkillId
                : skillId;
        }

        private static string[] CopyStringArray(string[] source, int length)
        {
            var copy = new string[length];

            if (source == null)
                return copy;

            Array.Copy(source, copy, Mathf.Min(source.Length, copy.Length));
            return copy;
        }
    }
}
