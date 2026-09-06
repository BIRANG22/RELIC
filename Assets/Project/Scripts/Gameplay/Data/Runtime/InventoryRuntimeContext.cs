using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public interface IInventoryRuntimeContext
    {
        List<string> OwnedRelicIds { get; }
        List<string> SkillInventoryIds { get; }
        List<string> BagItemIds { get; }
        bool IsLobby { get; }
    }

    public sealed class InventoryRuntimeContext : IInventoryRuntimeContext
    {
        private readonly LobbyRuntimeData lobby;
        private readonly BattleRuntimeData battle;

        private InventoryRuntimeContext(LobbyRuntimeData lobby, BattleRuntimeData battle)
        {
            this.lobby = lobby;
            this.battle = battle;
            Normalize();
        }

        public List<string> OwnedRelicIds => IsLobby ? lobby.OwnedRelicIds : battle.OwnedRelicIds;
        public List<string> SkillInventoryIds => IsLobby ? lobby.SkillInventoryIds : battle.SkillInventoryIds;
        public List<string> BagItemIds => IsLobby ? lobby.BagItemIds : battle.BagItemIds;
        public bool IsLobby => lobby != null;

        public static IInventoryRuntimeContext ForLobby(LobbyRuntimeData data)
        {
            return new InventoryRuntimeContext(data ?? new LobbyRuntimeData(), null);
        }

        public static IInventoryRuntimeContext ForBattle(BattleRuntimeData data)
        {
            return new InventoryRuntimeContext(null, data ?? new BattleRuntimeData());
        }

        private void Normalize()
        {
            if (IsLobby)
            {
                lobby.OwnedRelicIds ??= new List<string>();
                lobby.SkillInventoryIds ??= new List<string>();
                lobby.BagItemIds ??= new List<string>();
                return;
            }

            battle.OwnedRelicIds ??= new List<string>();
            battle.SkillInventoryIds ??= new List<string>();
            battle.BagItemIds ??= new List<string>();
        }
    }
}
