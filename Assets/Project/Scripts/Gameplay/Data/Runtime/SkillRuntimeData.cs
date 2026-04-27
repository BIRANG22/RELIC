namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class SkillRuntimeData
    {
        public string SkillId;

        public int Level = 1;
        public int Exp = 0;

        public bool IsUnlocked;
        public bool IsNew;
    }
}