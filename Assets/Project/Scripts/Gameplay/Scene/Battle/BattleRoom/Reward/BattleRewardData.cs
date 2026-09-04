using UnityEngine;

public enum BattleRewardType
{
    Remnant,
    Item,
    Relic,
    Skill
}

[System.Serializable]
public class BattleRewardData
{
    private const string DefaultRemnantDisplayName = "더스티움";

    public BattleRewardType Type;
    public string RewardId;
    public string SourceKey;
    public int Amount;
    public Sprite Icon;
    public string Name;
    public string Description;
    public int Value;

    public string GetDisplayName()
    {
        if (Type == BattleRewardType.Remnant)
        {
            string remnantName = string.IsNullOrWhiteSpace(Name) ? DefaultRemnantDisplayName : Name;
            return $"{remnantName} x{Mathf.Max(0, Amount)}";
        }

        string displayName = string.IsNullOrWhiteSpace(Name) ? RewardId : Name;

        if (Type == BattleRewardType.Item && Amount > 1)
            return $"{displayName} x{Amount}";

        return displayName;
    }

    public string GetRemnantAmountDescription()
    {
        int amount = Mathf.Max(0, Amount);
        return GameLocalization.Format("battle.obtain_dustium", "{0} 더스티움을 얻는다.", amount);
    }
}
