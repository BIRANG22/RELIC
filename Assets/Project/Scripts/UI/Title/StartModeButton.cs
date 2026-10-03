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

        // 튜토리얼 도중 저장 후 타이틀로 돌아온 경우에는 게임 시작 버튼을
        // 일반 탐사의 "탐사 진행" 버튼처럼 사용합니다. 저장된 튜토리얼 진행 상태를
        // 그대로 불러와 BattleScene으로 복귀하며, 새 튜토리얼을 시작하거나 탐사를 포기하지 않습니다.
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasTutorialBattleContinueSave())
        {
            ContinueSavedTutorial();
            return;
        }

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

            // 새 튜토리얼 시작 시 첫 전투 안내 상태만 초기화합니다.
            // 저장된 튜토리얼 이어하기가 있는 경우에는 OnClickStartMode에서 먼저 복원하므로
            // 여기서는 진행 저장을 삭제하지 않습니다.
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


    private async void ContinueSavedTutorial()
    {
        if (isProcessing)
            return;

        if (SaveSystem.Instance == null ||
            !SaveSystem.Instance.TryLoadBattleContinueProgress())
        {
            TitleManager.RefreshRunButtonsInScene();
            Debug.LogWarning("[StartModeButton] Failed to load saved tutorial progress.", this);
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
        {
            Debug.LogWarning("[StartModeButton] GameManager is not ready.", this);
            return;
        }

        isProcessing = true;

        try
        {
            // 일반 탐사의 탐사 진행과 동일하게 저장된 Map/Battle/Resume 데이터를 복원한 뒤
            // BattleScene으로 돌아갑니다. TutorialBattleEntrySetup으로 전투 1을 새로 만들지 않습니다.
            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
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
