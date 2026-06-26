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
        return string.IsNullOrWhiteSpace(Name) ? RewardId : Name;
    }

    public string GetRemnantAmountDescription()
    {
        int amount = Mathf.Max(0, Amount);
        return $"{amount} 더스티움을 얻는다.";
    }
}
