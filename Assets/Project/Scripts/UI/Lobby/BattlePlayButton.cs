using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattlePlayButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;
    [SerializeField] private LobbyQuestGate questGate;

    [Header("Direct Battle Target")]
    [Tooltip("스테이지 선택 UI를 사용하지 않을 때 PlayButton이 바로 진입할 챕터입니다.")]
    [SerializeField] private string directChapterId = "Chapter1";
    [Tooltip("스테이지 선택 UI를 사용하지 않을 때 PlayButton이 바로 진입할 스테이지입니다.")]
    [SerializeField] private string directStageId = "Stage1";

    [Header("Option")]
    [SerializeField] private bool checkMapSelected = true;
    [SerializeField] private bool checkPartyExists = true;
    [SerializeField] private bool requireFullParty = true;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;
    [SerializeField] private string mapNotSelectedMessage = "스테이지를 선택해야 합니다.";
    [SerializeField] private string partyEmptyMessage = "캐릭터를 편성해야 합니다.";
    [SerializeField] private string partyNotFullMessage = "캐릭터 3명을 모두 편성해야 합니다. 현재 {0}/{1}";
    [SerializeField] private string dataManagerMissingMessage = "데이터 매니저가 없습니다.";
    [SerializeField] private string gameManagerMissingMessage = "게임 매니저가 없습니다.";
    [SerializeField] private string networkClientStartBlockedMessage = "Only the host can start in multiplayer lobby.";
    [SerializeField] private string networkBattleStartSyncFailedMessage = "Failed to synchronize battle start.";

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    private bool isProcessing;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (questGate == null)
        {
            questGate = GetComponent<LobbyQuestGate>();
            if (questGate == null)
                questGate = gameObject.AddComponent<LobbyQuestGate>();

            questGate.RequiredProgress = LobbyTutorialProgress.FirstExpeditionAssigned;
        }

        FindWarningUIIfMissing();
    }

    private void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public async void OnClickPlay()
    {
        if (isProcessing)
            return;

        if (questGate != null && !questGate.TryConsume())
            return;

        isProcessing = true;

        if (button != null)
            button.interactable = false;

        try
        {
            PlayClickSound();

            if (!CanLocalPlayerStartBattle())
            {
                ShowWarning(networkClientStartBlockedMessage);
                return;
            }

            if (DataManager.Instance == null)
            {
                ShowWarning(dataManagerMissingMessage);
                Debug.LogWarning("[BattlePlayButton] DataManager is null.");
                return;
            }

            EnsureDirectBattleMapRuntime();

            if (checkMapSelected && !IsMapSelected())
            {
                ShowWarning(mapNotSelectedMessage);
                Debug.LogWarning("[BattlePlayButton] 직접 진입할 챕터/스테이지 값이 없습니다.");
                return;
            }

            if (checkPartyExists && !CanStartWithCurrentParty())
                return;

            if (GameManager.Instance == null)
            {
                ShowWarning(gameManagerMissingMessage);
                Debug.LogWarning("[BattlePlayButton] GameManager is null.");
                return;
            }

            LobbyBattleEntryService.CommitRuntimeStateContributorsForBattleStart();

            if (!TryBroadcastNetworkBattleStart(out LobbyBattleStartCommand battleStartCommand))
                return;

            LobbyBattleEntryResult entryResult =
                await LobbyBattleEntryService.EnterBattleAsync(battleStartCommand);
            if (!entryResult.Succeeded)
            {
                Debug.LogWarning($"[BattlePlayButton] Failed to enter battle. {entryResult.Error}");
                ShowWarning(entryResult.Error);
                return;
            }
        }
        finally
        {
            isProcessing = false;

            if (button != null)
                button.interactable = true;
        }
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx);
    }

    private void EnsureDirectBattleMapRuntime()
    {
        if (DataManager.Instance == null || DataManager.Instance.MapRuntimeStore == null)
            return;

        if (string.IsNullOrWhiteSpace(directChapterId) || string.IsNullOrWhiteSpace(directStageId))
            return;

        MapRuntimeData current = DataManager.Instance.MapRuntimeStore.Get();
        if (current != null &&
            string.Equals(current.SelectedChapterId, directChapterId.Trim(), System.StringComparison.Ordinal) &&
            string.Equals(current.CurrentStage, directStageId.Trim(), System.StringComparison.Ordinal))
        {
            return;
        }

        DataManager.Instance.MapRuntimeStore.Set(new MapRuntimeData
        {
            SelectedChapterId = directChapterId.Trim(),
            CurrentStage = directStageId.Trim(),
            CurrentMapId = string.Empty,
            CurrentSceneName = SceneName.Battle,
            IsRunInitialized = false
        });
    }

    private bool CanStartWithCurrentParty()
    {
        if (DataManager.Instance == null)
            return false;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore == null)
        {
            ShowWarning(partyEmptyMessage);
            Debug.LogWarning("[BattlePlayButton] PartyRuntimeStore is null.");
            return false;
        }

        int currentCount = CountPartyCharacters(partyStore);
        int requiredCount = Mathf.Max(1, partyStore.MaxPartyCountValue);

        if (requireFullParty)
        {
            if (currentCount < requiredCount)
            {
                ShowWarning(FormatPartyNotFullMessage(currentCount, requiredCount));
                Debug.LogWarning($"[BattlePlayButton] 파티 인원이 부족합니다. Current:{currentCount} / Required:{requiredCount}");
                return false;
            }
        }
        else if (currentCount <= 0)
        {
            ShowWarning(partyEmptyMessage);
            Debug.LogWarning("[BattlePlayButton] 파티에 캐릭터가 없습니다.");
            return false;
        }

        return true;
    }

    private int CountPartyCharacters(PartyRuntimeStore partyStore)
    {
        if (partyStore == null)
            return 0;

        int count = 0;
        int maxPartyCount = partyStore.MaxPartyCountValue;

        for (int i = 0; i < maxPartyCount; i++)
        {
            if (!string.IsNullOrWhiteSpace(partyStore.GetCharacterId(i)))
                count++;
        }

        return count;
    }

    private string FormatPartyNotFullMessage(int currentCount, int requiredCount)
    {
        if (string.IsNullOrWhiteSpace(partyNotFullMessage))
            return $"캐릭터 {requiredCount}명을 모두 편성해야 합니다. 현재 {currentCount}/{requiredCount}";

        if (partyNotFullMessage.Contains("{0}") || partyNotFullMessage.Contains("{1}"))
            return string.Format(partyNotFullMessage, currentCount, requiredCount);

        return partyNotFullMessage;
    }

    private void ShowWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        FindWarningUIIfMissing();

        if (warningUI != null)
        {
            warningUI.Show(message);
            return;
        }

        if (SettingWarningUI.Instance != null)
        {
            SettingWarningUI.Instance.Show(message);
            return;
        }

        Debug.LogWarning($"[BattlePlayButton] Warning UI is missing. Message: {message}");
    }

    private void FindWarningUIIfMissing()
    {
        if (warningUI != null)
            return;

        warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);
    }

    private bool IsMapSelected()
    {
        MapRuntimeData mapData = DataManager.Instance.MapRuntimeStore.Get();

        return mapData != null &&
               !string.IsNullOrWhiteSpace(mapData.SelectedChapterId) &&
               !string.IsNullOrWhiteSpace(mapData.CurrentStage);
    }

    private bool TryBroadcastNetworkBattleStart(out LobbyBattleStartCommand command)
    {
        command = null;
        SteamLobbySharedStateSynchronizer sharedStateSynchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        SteamLobbyBattleStartSynchronizer battleStartSynchronizer =
            SteamLobbyBattleStartSynchronizer.Instance;

        bool networkBattleStartRequired =
            (sharedStateSynchronizer != null && sharedStateSynchronizer.IsNetworkSharedStateActive) ||
            (battleStartSynchronizer != null && battleStartSynchronizer.IsNetworkBattleStartActive);
        if (!networkBattleStartRequired)
            return true;

        if (battleStartSynchronizer == null ||
            !battleStartSynchronizer.IsNetworkBattleStartActive)
        {
            ShowWarning(networkBattleStartSyncFailedMessage);
            return false;
        }

        if (!battleStartSynchronizer.CanLocalPlayerStartBattle())
        {
            ShowWarning(networkClientStartBlockedMessage);
            return false;
        }

        if (sharedStateSynchronizer == null)
        {
            ShowWarning(networkBattleStartSyncFailedMessage);
            return false;
        }

        LobbySharedStateSnapshot snapshot =
            sharedStateSynchronizer.PublishHostSnapshotNow();
        if (snapshot == null || snapshot.Revision <= 0)
        {
            ShowWarning(networkBattleStartSyncFailedMessage);
            return false;
        }

        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();
        if (!battleStartSynchronizer.TryBroadcastBattleStart(
                snapshot,
                mapRuntime,
                out command))
        {
            ShowWarning(networkBattleStartSyncFailedMessage);
            return false;
        }

        return true;
    }

    private static bool CanLocalPlayerStartBattle()
    {
        SteamLobbyBattleStartSynchronizer battleStartSynchronizer =
            SteamLobbyBattleStartSynchronizer.Instance;
        if (battleStartSynchronizer != null &&
            !battleStartSynchronizer.CanLocalPlayerStartBattle())
        {
            return false;
        }

        SteamLobbySharedStateSynchronizer sharedStateSynchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return sharedStateSynchronizer == null ||
               sharedStateSynchronizer.CanLocalPlayerMutateHostOnlyState();
    }
}
