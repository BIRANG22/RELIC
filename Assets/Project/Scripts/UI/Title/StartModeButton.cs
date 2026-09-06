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
                // IntroToggle1은 다음 게임 시작 시 인트로를 1회 재생하는 예약 토글입니다.
                // 실제 인트로가 시작되는 시점에 예약을 소비하여 OFF 상태로 저장합니다.
                IntroSettings.MarkIntroSeen();
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

        // 타이틀에서 로비로 다시 들어오는 것만으로 저장된 로비 장착 정보를 지우면 안 됩니다.
        // 실제 탐사 중에 타이틀로 빠져나온 경우에만 탐사 상태를 포기/복구합니다.
        BattleRuntimeData battleRuntime = DataManager.Instance.BattleRuntimeStore?.Get();
        if (battleRuntime != null && battleRuntime.IsBattleRunInitialized)
            BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);
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
