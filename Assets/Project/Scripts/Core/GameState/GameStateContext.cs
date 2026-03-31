using System;

[Serializable]
public class GameStateContext
{
    public PlayerSelectionData PlayerSelection = new PlayerSelectionData();
    public BattleStartPayload PendingBattle = new BattleStartPayload();

    public void ClearRunData()
    {
        PlayerSelection.Clear();
        PendingBattle.Clear();
    }
}