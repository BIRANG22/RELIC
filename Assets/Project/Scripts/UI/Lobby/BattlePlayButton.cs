using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePlayButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;
    [SerializeField] private LobbyQuestGate questGate;

    [Header("Ready Panel")]
    [SerializeField] private LobbyEquipPanelUI readyPanel;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private string readyButtonText = "탐사 준비";
    [SerializeField] private string departButtonText = "출정";

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
    [SerializeField] private string elricDialogueRequiredMessage = "엘릭과 대화를 마친 뒤 탐사를 시작해 주세요.";

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    private bool isProcessing;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        ResolveReadyPanel();
        ResolveButtonLabel();
        BindReadyPanelState();
        RefreshButtonLabel();

        if (questGate == null)
        {
            questGate = GetComponent<LobbyQuestGate>();
            if (questGate == null)
                questGate = gameObject.AddComponent<LobbyQuestGate>();

            questGate.RequiredProgress = LobbyTutorialProgress.FirstExpeditionAssigned;
        }

        // 탐사 시작 버튼은 잠금 상태에서도 클릭을 받아 안내 문구를 표시해야 합니다.
        if (questGate != null)
        {
            questGate.UpdateButtonInteractable = false;
            questGate.RequiredProgress = LobbyTutorialProgress.FirstExpeditionAssigned;
        }

        FindWarningUIIfMissing();
    }

    private void OnEnable()
    {
        ResolveReadyPanel();
        ResolveButtonLabel();
        BindReadyPanelState();
        RefreshButtonLabel();
    }

    private void OnDisable()
    {
        UnbindReadyPanelState();
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

        ResolveReadyPanel();
        if (readyPanel != null && !readyPanel.IsOpen)
        {
            if (questGate != null && !questGate.CanExecute())
            {
                ShowWarning(elricDialogueRequiredMessage);
                return;
            }

            PlayClickSound();
            readyPanel.Open();
            RefreshButtonLabel();
            return;
        }

        if (questGate != null && !questGate.CanExecute())
        {
            ShowWarning(elricDialogueRequiredMessage);
            return;
        }

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

    private void ResolveReadyPanel()
    {
        if (readyPanel != null)
            return;

        LobbyEquipPanelUI[] panels = FindObjectsByType<LobbyEquipPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            LobbyEquipPanelUI candidate = panels[i];
            if (candidate == null)
                continue;

            if (candidate.gameObject.name == "Ready_Panel")
            {
                readyPanel = candidate;
                return;
            }
        }

        if (panels.Length > 0)
            readyPanel = panels[0];
    }

    private void ResolveButtonLabel()
    {
        if (buttonLabel == null)
            buttonLabel = GetComponentInChildren<TMP_Text>(true);
    }

    private void BindReadyPanelState()
    {
        if (readyPanel == null)
            return;

        readyPanel.OpenStateChanged -= HandleReadyPanelStateChanged;
        readyPanel.OpenStateChanged += HandleReadyPanelStateChanged;
    }

    private void UnbindReadyPanelState()
    {
        if (readyPanel != null)
            readyPanel.OpenStateChanged -= HandleReadyPanelStateChanged;
    }

    private void HandleReadyPanelStateChanged(bool opened)
    {
        RefreshButtonLabel();
    }

    private void RefreshButtonLabel()
    {
        ResolveButtonLabel();
        if (buttonLabel == null)
            return;

        buttonLabel.text = readyPanel != null && readyPanel.IsOpen
            ? departButtonText
            : readyButtonText;
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

        // 로비의 출정 버튼은 이어하기가 아니라 새 탐사를 시작하는 경로입니다.
        // 같은 Chapter/Stage의 이전 MapRuntime이 남아 있어도 재사용하지 않습니다.
        // CurrentNodeIndex를 -1로 시작해야 BattleScene에서 구역 진입 연출을 먼저 재생한 뒤
        // Layer 0 시작 노드로 진입합니다.
        DataManager.Instance.MapRuntimeStore.Set(new MapRuntimeData
        {
            SelectedChapterId = directChapterId.Trim(),
            CurrentStage = directStageId.Trim(),
            CurrentMapId = string.Empty,
            CurrentNodeIndex = -1,
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
