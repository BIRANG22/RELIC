using UnityEngine;

public class StartModeButton : MonoBehaviour
{
    [SerializeField] private GameMode gameMode;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    public async void OnClickStartMode()
    {
        PlayClickSound();

        GameManager.Instance.Context.SelectedGameMode = gameMode;
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx);
    }
}
