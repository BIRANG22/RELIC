using UnityEngine;

public enum BattleRewardType
{
    Remnant,
    Item,
    Relic
}

[System.Serializable]
public class BattleRewardData
{
    public BattleRewardType Type;
    public string RewardId;
    public int Amount;
    public Sprite Icon;
    public string Name;
}