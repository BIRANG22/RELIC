using UnityEngine;

public class StartModeButton : MonoBehaviour
{
    [SerializeField] private GameMode gameMode;

    public async void OnClickStartMode()
    {
        AudioManager.Instance.PlaySfx(SfxType.Click);

        GameManager.Instance.Context.SelectedGameMode = gameMode;
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }
}