using System;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class TitleDemoBattleButton : MonoBehaviour
{
    [Header("Demo Battle")]
    [SerializeField] private string chapterId = "Chapter1";
    [SerializeField] private string stageId = "Stage1";
    [SerializeField] private int battleSeed = 1001;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    private bool isStarting;

    public async void OnClickStartDemo()
    {
        if (isStarting)
            return;

        isStarting = true;
        PlayClickSound();
        TitleManager.CloseTitleModePanelsInScene();

        try
        {
            DataManager dataManager = DataManager.Instance;
            if (dataManager == null)
            {
                Debug.LogError("[TitleDemoBattleButton] DataManager is missing.", this);
                return;
            }

            BattleRuntimeData previousBattle = dataManager.BattleRuntimeStore?.Get();
            if (previousBattle != null && previousBattle.IsBattleRunInitialized)
                BattleRunAbandonService.AbandonCurrentRun(dataManager);

            dataManager.MapRuntimeStore?.Clear();
            dataManager.BattleRuntimeStore?.Clear();
            SaveSystem.Instance?.ClearBattleRoomResumeState();

            if (!DemoBattlePartySetup.TryPrepare(dataManager))
            {
                Debug.LogError("[TitleDemoBattleButton] Failed to prepare demo battle data.", this);
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
            {
                Debug.LogError("[TitleDemoBattleButton] GameManager state machine is missing.", this);
                return;
            }

            GameManager.Instance.Context.SelectedGameMode = GameMode.SingleStory;

            LobbyBattleStartCommand command = new(
                Guid.NewGuid().ToString("N"),
                0,
                0,
                Guid.NewGuid().ToString("N"),
                battleSeed,
                chapterId,
                stageId);

            LobbyBattleEntryResult result = await LobbyBattleEntryService.EnterBattleAsync(command);
            if (!result.Succeeded)
                Debug.LogError($"[TitleDemoBattleButton] Demo battle entry failed: {result.Error}", this);
        }
        finally
        {
            isStarting = false;
        }
    }

    private void PlayClickSound()
    {
        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(clickSfx);
    }
}
