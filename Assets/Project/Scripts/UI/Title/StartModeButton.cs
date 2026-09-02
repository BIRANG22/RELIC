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

        // 파티 편성이 완전히 비어 있는 최초 게임 시작에서만 기본 파티를 구성합니다.
        // 이미 플레이해서 저장된 파티가 있다면 InitialDefaultPartySetup 내부에서 그대로 유지합니다.
        InitialDefaultPartySetup.TryInitialize(DataManager.Instance);

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
                "[StartModeButton] IntroSequenceController is missing. Skipping intro and moving to lobby.",
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
