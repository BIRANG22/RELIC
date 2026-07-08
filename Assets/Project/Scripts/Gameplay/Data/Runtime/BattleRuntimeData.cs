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

        // 전투 진입 직전 로비 파티/스킬/룬 세팅 복구용 스냅샷
        public List<BattleLobbyLoadoutSnapshotData> LobbyLoadoutSnapshots = new();

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
        public string[] EquippedRuneIds = new string[12];

        public int MaxHP;
        public int MaxCost;
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
        private const int EquippedRuneSlotCount = 12;
        private const int EquippedRelicSlotCount = 5;

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

            Dictionary<string, BattleLobbyLoadoutSnapshotData> snapshots =
                BuildSnapshotMap(dataManager.BattleRuntimeStore?.Get());

            RestoreCharacterLoadoutsAndClearBattleState(dataManager, snapshots);
            dataManager.PartyRuntimeStore?.ResetCurrentGridIndicesToSpawn();
            dataManager.SkillRuntimeStore?.Clear();
            dataManager.MapRuntimeStore?.Clear();
            dataManager.BattleRuntimeStore?.Clear();
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
                MaxHP = character.MaxHP,
                MaxCost = character.MaxCost,
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

            character.MaxHP = Mathf.Max(1, snapshot.MaxHP);
            character.MaxCost = Mathf.Max(0, snapshot.MaxCost);
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
            character.CostRecovery = Mathf.Max(0, master.CostRecovery);
            character.BonusCostRecovery = 0;
            character.CurrentHP = character.MaxHP;
            character.CurrentCost = character.MaxCost;
            character.CurrentResource = 0;
            character.CurrentMoveLevel = Mathf.Max(0, master.MoveValue);
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

            character.AppliedBattleEquipmentEffectIds ??= new List<string>();
            character.AppliedBattleEquipmentEffectIds.Clear();
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
