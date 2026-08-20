using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// 도감의 영구 획득 이력을 관리합니다.
    /// 탐사 중 소모하거나 포기해도 PlayerRuntimeData에 기록된 획득 이력은 유지됩니다.
    /// </summary>
    public static class RecordDiscoveryService
    {
        public static void Normalize(PlayerRuntimeData player)
        {
            if (player == null)
                return;

            player.DiscoveredSkillIds ??= new List<string>();
            player.DiscoveredRuneIds ??= new List<string>();
            player.DiscoveredRelicIds ??= new List<string>();
            player.DiscoveredCompoundIds ??= new List<string>();
            player.DiscoveredItemIds ??= new List<string>();

            NormalizeIds(player.DiscoveredSkillIds);
            NormalizeIds(player.DiscoveredRuneIds);
            NormalizeIds(player.DiscoveredRelicIds);
            NormalizeIds(player.DiscoveredCompoundIds);
            NormalizeIds(player.DiscoveredItemIds);
        }

        public static bool RegisterSkill(DataManager dataManager, string skillId)
        {
            PlayerRuntimeData player = PreparePlayer(dataManager);
            return Register(skillId, player?.DiscoveredSkillIds);
        }

        public static bool RegisterRune(DataManager dataManager, string runeId)
        {
            PlayerRuntimeData player = PreparePlayer(dataManager);
            return Register(runeId, player?.DiscoveredRuneIds);
        }

        public static bool RegisterRelic(DataManager dataManager, string relicId)
        {
            PlayerRuntimeData player = PreparePlayer(dataManager);
            return Register(relicId, player?.DiscoveredRelicIds);
        }

        public static bool RegisterCompound(DataManager dataManager, string compoundId)
        {
            PlayerRuntimeData player = PreparePlayer(dataManager);
            return Register(compoundId, player?.DiscoveredCompoundIds);
        }

        public static bool RegisterItem(DataManager dataManager, string itemId)
        {
            PlayerRuntimeData player = PreparePlayer(dataManager);
            return Register(itemId, player?.DiscoveredItemIds);
        }

        public static bool IsSkillDiscovered(DataManager dataManager, string skillId)
        {
            return Contains(dataManager?.PlayerRuntimeStore?.Data?.DiscoveredSkillIds, skillId);
        }

        public static bool IsRuneDiscovered(DataManager dataManager, string runeId)
        {
            return Contains(dataManager?.PlayerRuntimeStore?.Data?.DiscoveredRuneIds, runeId);
        }

        public static bool IsRelicDiscovered(DataManager dataManager, string relicId)
        {
            return Contains(dataManager?.PlayerRuntimeStore?.Data?.DiscoveredRelicIds, relicId);
        }

        public static bool IsCompoundDiscovered(DataManager dataManager, string compoundId)
        {
            return Contains(dataManager?.PlayerRuntimeStore?.Data?.DiscoveredCompoundIds, compoundId);
        }

        public static bool IsItemDiscovered(DataManager dataManager, string itemId)
        {
            return Contains(dataManager?.PlayerRuntimeStore?.Data?.DiscoveredItemIds, itemId);
        }

        /// <summary>
        /// 기존 세이브와 기본 지급 콘텐츠를 도감 이력으로 보정합니다.
        /// 현재 소지/장착 중인 콘텐츠와 해금된 캐릭터의 기본 기억/파편을 획득한 것으로 처리합니다.
        /// </summary>
        public static void BackfillFromCurrentState(DataManager dataManager)
        {
            if (dataManager?.PlayerRuntimeStore?.Data == null)
                return;

            Normalize(dataManager.PlayerRuntimeStore.Data);

            BackfillCharacters(dataManager);
            BackfillBattleRuntime(dataManager);
            BackfillLobbyRuntime(dataManager);
        }

        private static void BackfillCharacters(DataManager dataManager)
        {
            IReadOnlyDictionary<string, CharacterRuntimeData> characters =
                dataManager.CharacterRuntimeStore?.GetAll();

            if (characters == null)
                return;

            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;
                if (character == null)
                    continue;

                RegisterSkill(dataManager, character.MoveSkillId);
                RegisterSkill(dataManager, character.PassiveSkillId);
                RegisterSkill(dataManager, character.UniqueSkillId);
                RegisterSkill(dataManager, character.AbilitySkillId);
                RegisterIds(character.EquippedSkillIds, id => RegisterSkill(dataManager, id));
                RegisterIds(character.EquippedRuneIds, id => RegisterRune(dataManager, id));
                if (character.EquippedRelicIds != null)
                {
                    for (int i = 0; i < character.EquippedRelicIds.Length; i++)
                    {
                        string equippedId = character.EquippedRelicIds[i];
                        if (IsCompoundId(equippedId))
                            RegisterCompound(dataManager, equippedId);
                        else
                            RegisterRelic(dataManager, equippedId);
                    }
                }

                if (!character.IsUnlocked ||
                    string.IsNullOrWhiteSpace(character.CharacterId) ||
                    dataManager.CharacterDatabase == null ||
                    !dataManager.CharacterDatabase.TryGet(character.CharacterId, out CharacterMasterData master) ||
                    master == null)
                {
                    continue;
                }

                // 캐릭터가 처음부터 가지고 있는 기본 기억은 처음부터 도감에 공개합니다.
                RegisterSkill(dataManager, master.PassiveSkill1);
                RegisterSkill(dataManager, master.UniqueSkill1);
                RegisterSkill(dataManager, master.CharacterSkill1);
                RegisterSkill(dataManager, master.CommonSkill1);

                // 캐릭터 기본 파편도 처음부터 획득한 것으로 처리합니다.
                RegisterIds(master.GetRuneIds(), id => RegisterRune(dataManager, id));
            }
        }

        private static void BackfillBattleRuntime(DataManager dataManager)
        {
            BattleRuntimeData battle = dataManager.BattleRuntimeStore?.Get();
            if (battle == null)
                return;

            RegisterIds(battle.SkillInventoryIds, id => RegisterSkill(dataManager, id));
            RegisterIds(battle.AcquiredSkillIds, id => RegisterSkill(dataManager, id));
            RegisterIds(battle.OwnedRelicIds, id => RegisterOwnedRelicOrCompound(dataManager, id));
            RegisterIds(battle.BagItemIds, id => RegisterItem(dataManager, id));
        }

        private static void BackfillLobbyRuntime(DataManager dataManager)
        {
            LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.Get();
            if (lobby == null)
                return;

            RegisterIds(lobby.SkillInventoryIds, id => RegisterSkill(dataManager, id));
            RegisterIds(lobby.OwnedRelicIds, id => RegisterOwnedRelicOrCompound(dataManager, id));
            RegisterIds(lobby.BagItemIds, id => RegisterItem(dataManager, id));

            if (lobby.CultureTankResearches != null)
            {
                for (int i = 0; i < lobby.CultureTankResearches.Count; i++)
                {
                    CultureTankResearchRuntimeData research = lobby.CultureTankResearches[i];
                    if (research != null)
                        RegisterItem(dataManager, research.ItemId);
                }
            }
        }

        private static void RegisterOwnedRelicOrCompound(DataManager dataManager, string id)
        {
            if (IsCompoundId(id))
                RegisterCompound(dataManager, id);
            else
                RegisterRelic(dataManager, id);
        }

        private static bool IsCompoundId(string id)
        {
            return !string.IsNullOrWhiteSpace(id) &&
                   id.Trim().StartsWith("Compound_", StringComparison.Ordinal);
        }

        private static PlayerRuntimeData PreparePlayer(DataManager dataManager)
        {
            PlayerRuntimeData player = dataManager?.PlayerRuntimeStore?.Data;
            Normalize(player);
            return player;
        }

        private static bool Register(string id, List<string> target)
        {
            if (target == null || string.IsNullOrWhiteSpace(id))
                return false;

            string normalizedId = id.Trim();
            if (Contains(target, normalizedId))
                return false;

            target.Add(normalizedId);
            return true;
        }

        private static bool Contains(List<string> ids, string targetId)
        {
            if (ids == null || string.IsNullOrWhiteSpace(targetId))
                return false;

            string normalizedTarget = targetId.Trim();
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i]?.Trim(), normalizedTarget, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void RegisterIds(IEnumerable<string> ids, Action<string> register)
        {
            if (ids == null || register == null)
                return;

            foreach (string id in ids)
                register(id);
        }

        private static void NormalizeIds(List<string> ids)
        {
            if (ids == null)
                return;

            HashSet<string> unique = new(StringComparer.Ordinal);
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                string id = ids[i]?.Trim();
                if (string.IsNullOrWhiteSpace(id) || !unique.Add(id))
                {
                    ids.RemoveAt(i);
                    continue;
                }

                ids[i] = id;
            }
        }
    }
}
