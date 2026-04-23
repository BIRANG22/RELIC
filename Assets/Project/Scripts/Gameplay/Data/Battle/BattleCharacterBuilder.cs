namespace Relic.Gameplay.Data
{
    public class BattleCharacterBuilder
    {
        public BattleCharacterContext Build(CharacterMasterData master, CharacterGrowthData growth, CharacterEquipmentData equipment, CharacterStateData state)
        {
            return new BattleCharacterContext
            {
                CharacterId = master.CharacterId,
                Name = master.Name,
                MaxHealth = master.MaxHealth,
                CurrentHealth = state.CurrentHealth > 0 ? state.CurrentHealth : master.MaxHealth,
                CurrentStamina = state.CurrentStamina > 0 ? state.CurrentStamina : master.MaxStamina,
                SkillLoadout = equipment.SkillLoadout
            };
        }
    }

    public class BattleCharacterContext
    {
        public string CharacterId;
        public string Name;
        public int MaxHealth;
        public int CurrentHealth;
        public int CurrentStamina;
        public CharacterSkillLoadout SkillLoadout;
    }
}
