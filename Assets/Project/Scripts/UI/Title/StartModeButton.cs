using Relic.Gameplay.Data;
using UnityEngine;

public class StartModeButton : MonoBehaviour
{
    [SerializeField] private GameMode gameMode;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    private bool isProcessing;

    public void OnClickStartMode()
    {
        if (isProcessing)
            return;

        PlayClickSound();
        TitleManager.CloseTitleModePanelsInScene();

        ResetPreviousRunRuntimeState();

        if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
        {
            Debug.LogError("[StartModeButton] GameManager state machine is missing.", this);
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogError("[StartModeButton] DataManager is missing.", this);
            return;
        }

        GameManager.Instance.Context.SelectedGameMode = gameMode;

        // 인트로와 전체 튜토리얼은 각각 1회 예약 방식으로 동작합니다.
        // 인트로 ON + 튜토리얼 ON  : 인트로 -> 튜토리얼
        // 인트로 ON + 튜토리얼 OFF : 인트로 -> 로비
        // 인트로 OFF + 튜토리얼 ON : 튜토리얼
        // 인트로 OFF + 튜토리얼 OFF: 로비
        if (IntroSettings.ShouldPlayIntro)
        {
            IntroSequenceController introController = IntroSequenceController.Instance;
            if (introController != null)
            {
                isProcessing = true;

                void HandleIntroFinished()
                {
                    introController.IntroFinished -= HandleIntroFinished;
                    IntroSettings.MarkIntroSeen();
                    isProcessing = false;
                    ContinueAfterIntro();
                }

                introController.IntroFinished += HandleIntroFinished;
                introController.PlayIntroBeforeSceneChange();
                return;
            }

            Debug.LogWarning(
                "[StartModeButton] IntroSequenceController is missing. Continuing without intro.",
                this);
        }

        ContinueAfterIntro();
    }

    private void ContinueAfterIntro()
    {
        if (TutorialSettings.ShouldPlayTutorial)
        {
            StartTutorialBattle();
            return;
        }

        EnterLobby();
    }

    /// <summary>
    /// 전체 튜토리얼을 시작하고 첫 전투(Map_27)로 진입합니다.
    /// 실제 시작되는 순간 옵션의 튜토리얼 예약값을 자동으로 OFF 처리합니다.
    /// </summary>
    public async void StartTutorialBattle()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        try
        {
            ResetPreviousRunRuntimeState();

            if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
            {
                Debug.LogError("[StartModeButton] GameManager state machine is missing.", this);
                return;
            }

            if (DataManager.Instance == null)
            {
                Debug.LogError("[StartModeButton] DataManager is missing.", this);
                return;
            }

            GameManager.Instance.Context.SelectedGameMode = gameMode;

            TutorialSettings.MarkTutorialSeen();
            BattleFirstTutorialController.ResetAutoTutorialRunState();

            if (!TutorialBattleEntrySetup.TryPrepareFirstBattle(DataManager.Instance))
            {
                Debug.LogError("[StartModeButton] Failed to prepare tutorial battle 1.", this);
                return;
            }

            LobbyBattleEntryResult entryResult = await LobbyBattleEntryService.EnterBattleAsync();
            if (!entryResult.Succeeded)
            {
                Debug.LogError(
                    $"[StartModeButton] Failed to enter tutorial battle 1. {entryResult.Error}",
                    this);
            }
        }
        finally
        {
            isProcessing = false;
        }
    }

    private async void EnterLobby()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        try
        {
            if (DataManager.Instance != null)
                InitialDefaultPartySetup.TryInitialize(DataManager.Instance);

            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
        }
        finally
        {
            isProcessing = false;
        }
    }

    private void ResetPreviousRunRuntimeState()
    {
        if (DataManager.Instance == null)
            return;

        BattleRuntimeData battleRuntime = DataManager.Instance.BattleRuntimeStore?.Get();
        if (battleRuntime != null && battleRuntime.IsBattleRunInitialized)
            BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);
    }

    private void PlayClickSound()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx);
    }
}
