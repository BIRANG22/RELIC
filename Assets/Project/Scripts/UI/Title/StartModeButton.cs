using Relic.Gameplay.Data;
using UnityEngine;

public class StartModeButton : MonoBehaviour
{
    [SerializeField] private GameMode gameMode;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    public async void OnClickStartMode()
    {
        PlayClickSound();
        TitleManager.CloseTitleModePanelsInScene();

        ResetPreviousRunRuntimeState();

        GameManager.Instance.Context.SelectedGameMode = gameMode;

        if (IntroSettings.ShouldPlayIntro)
        {
            IntroSequenceController introController = IntroSequenceController.Instance;
            if (introController != null)
            {
                introController.PlayFirstTimeIntro();
                return;
            }

            Debug.LogWarning(
                "[StartModeButton] IntroSequenceController�� ���� ��Ʈ�θ� �ǳʶٰ� �κ�� �̵��մϴ�.",
                this);
        }

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    private void ResetPreviousRunRuntimeState()
    {
        if (DataManager.Instance == null)
            return;

        BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);

        LobbyRuntimeData lobbyRuntime = DataManager.Instance.LobbyRuntimeStore?.Get();
        if (lobbyRuntime == null)
            return;

        LobbyBattleRuntimeTransferService.ClearTransferredLobbyState(lobbyRuntime);
        DataManager.Instance.LobbyRuntimeStore?.Set(lobbyRuntime);
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
