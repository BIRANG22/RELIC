namespace Relic.Gameplay.Data
{
    public class CharacterSelectionManager
    {
        public string CurrentCharacterId { get; private set; }
        public void SelectCharacter(string characterId) => CurrentCharacterId = characterId;
    }
}
