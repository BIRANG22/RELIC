using System;
using System.Collections.Generic;

[Serializable]
public enum ResumePhase
{
    None,
    BattleEntry,
    BattleReward,
    EventEntry,
    EventChoice,
    EventDice,
    Rest
}

public enum ResumePresentation
{
    None,
    ChoiceList,
    ResultOnly,
    RewardPanel,
    DiceResolved,
    Shop
}

[Serializable]
public class ResumeData
{
    public ResumePhase Phase;
    public int NodeIndex = -1;
    public string MapId;
    public string EventId;

    public List<BattleRoomGridEffectSaveData> InitialGridEffects = new();
    public List<BattleRoomMonsterCommandSaveData> InitialMonsterCommands = new();

    public List<BattleRewardSaveData> PendingRewards = new();
    public List<EventChoiceReferenceSaveData> VisibleChoices = new();
    // 이전 저장 파일과의 호환용이며 새 checkpoint에는 쓰지 않는다.
    public List<BattleRewardSaveData> ClaimedRewards = new();

    public string SelectedChoiceId;
    // EventChoice 결과가 Runtime에 반영된 뒤에만 true가 된다.
    // Continue는 true인 선택지를 다시 Execute하지 않고 저장된 후속 상태만 복원한다.
    public bool ChoiceResultApplied;
    public string NextEventId;
    public ResumePresentation Presentation;
    public string ResultMessage;
    public bool NextButtonVisible;
    public bool ChanceSucceeded;
    public int[] DiceFaces = Array.Empty<int>();
    public bool DiceRollResolved;
    public List<ResumeShopGoodsSaveData> ShopGoods = new();
    public bool IsRestActionResolved;
}

[Serializable]
public class EventChoiceReferenceSaveData
{
    public string EventId;
    public int ChoiceOrder;
}

[Serializable]
public class ResumeShopGoodsSaveData
{
    public RestRoomShopGoodsKind Kind;
    public string Id;
    public int Price;
}

[Serializable]
public class BattleRewardSaveData
{
    public BattleRewardType Type;
    public string RewardId;
    public int Amount;
}
