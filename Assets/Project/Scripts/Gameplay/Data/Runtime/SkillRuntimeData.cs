namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class SkillRuntimeData
    {
        public string CharacterId;
        public string SkillId;

        public int Level = 1;
        public int Exp = 0;
        public int PermanentValueBonus;
        public int BattleValueBonus;

        public bool IsUnlocked;
        public bool IsNew;
    }
}
