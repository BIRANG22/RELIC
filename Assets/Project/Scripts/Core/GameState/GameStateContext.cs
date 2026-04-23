using System;

[Serializable]
public class GameStateContext
{
    public PlayerSelectionData PlayerSelection = new PlayerSelectionData();
    public BattleStartPayload PendingBattle = new BattleStartPayload();
    public GameStateType OptionReturnStateType = GameStateType.None;
    public GameMode SelectedGameMode = GameMode.None;


    public void ClearRunData()
    {
        PlayerSelection.Clear();
        PendingBattle.Clear();
    }
}