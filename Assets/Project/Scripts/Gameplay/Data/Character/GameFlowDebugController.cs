using UnityEngine;

public class GameFlowDebugController : MonoBehaviour
{
    public async void GoToLobby()
    {
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    public async void GoToCharacterSelect()
    {
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.CharacterSelect);
    }

    public async void GoToBattleSelect()
    {
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.BattleSelect);
    }

    public async void GoToBattle()
    {
        var context = GameManager.Instance.Context;
        context.PendingBattle.stageId = "debug_stage_001";
        context.PendingBattle.enemyGroupId = "debug_enemy_001";
        context.PendingBattle.isInitialized = true;

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
    }

    public void SelectCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[GameFlowDebugController] SelectCharacter failed: characterData is null.");
            return;
        }

        GameManager.Instance.Context.PlayerSelection.selectedCharacterId = characterData.CharacterId;
        Debug.Log($"[GameFlowDebugController] Selected Character: {characterData.CharacterId}");
    }
}