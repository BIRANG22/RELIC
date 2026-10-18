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
    [SerializeField] private LobbyTutorialController tutorialController;
    [SerializeField] private TMP_Text buttonLabel;

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

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    [Header("Ready Transition - Button Lock")]
    [SerializeField] private ReadyTransitionProgressUI readyTransition;
    [SerializeField] private Graphic buttonLeftGraphic;
    [SerializeField] private Graphic buttonRightGraphic;
    [SerializeField] private Graphic playTextGraphic;
    [SerializeField] private Color transitionLockedColor = new Color32(0x77, 0x77, 0x77, 0xFF);
    [SerializeField, Min(0f)] private float transitionColorFadeDuration = 0.25f;

    private bool isProcessing;
    private bool waitingForReadyTransition;
    private bool transitionVisualLocked;
    private Color leftOriginalColor;
    private Color rightOriginalColor;
    private Color textOriginalColor;
    private float colorFadeElapsed;
    private Color leftFadeStart;
    private Color rightFadeStart;
    private Color textFadeStart;
    private bool colorFadeActive;


    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        ResolveReadyPanel();
        ResolveButtonLabel();
        ResolveTransitionVisuals();
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
        ResolveTransitionVisuals();
        BindReadyPanelState();
        RefreshButtonLabel();
    }

    private void OnDisable()
    {
        UnbindReadyPanelState();
        waitingForReadyTransition = false;
        ApplyTransitionButtonLock(false);
    }

    private void Update()
    {
        UpdateTransitionColors();
        if (!waitingForReadyTransition)
            return;

        ResolveTransitionVisuals();
        if (readyTransition == null || !readyTransition.IsAnimating)
        {
            waitingForReadyTransition = false;
            ApplyTransitionButtonLock(false);
        }
    }

    private void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public async void OnClickPlay()
    {
        if (isProcessing || waitingForReadyTransition)
            return;

        ResolveReadyPanel();
        if (readyPanel != null && !readyPanel.IsOpen)
        {
            // 최초 튜토리얼에서는 출발 대사를 듣기 전에도 Ready_Panel까지는 열 수 있어야 합니다.
            // Ready_Panel이 실제 열린 뒤 HandleReadyPanelStateChanged에서 출발 대사를 자동 시작합니다.
            if (questGate != null && !questGate.CanExecute() && !CanOpenReadyPanelForTutorial())
            {
                ShowWarningKey(LocalizationKeys.Warning.ElricDialogueRequired);
                return;
            }

            PlayClickSound();
            readyPanel.Open();
            RefreshButtonLabel();
            return;
        }

        if (questGate != null && !questGate.CanExecute())
        {
            ShowWarningKey(LocalizationKeys.Warning.ElricDialogueRequired);
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
                ShowWarningKey(LocalizationKeys.Warning.NetworkHostOnlyStart);
                return;
            }

            if (DataManager.Instance == null)
            {
                ShowWarningKey(LocalizationKeys.Warning.DataUnavailable);
                Debug.LogWarning("[BattlePlayButton] DataManager is null.");
                return;
            }

            EnsureDirectBattleMapRuntime();

            if (checkMapSelected && !IsMapSelected())
            {
                ShowWarningKey(LocalizationKeys.Warning.MapNotSelected);
                Debug.LogWarning("[BattlePlayButton] 직접 진입할 챕터/스테이지 값이 없습니다.");
                return;
            }

            if (checkPartyExists && !CanStartWithCurrentParty())
                return;

            if (GameManager.Instance == null)
            {
                ShowWarningKey(LocalizationKeys.Warning.GameManagerMissing);
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
                button.interactable = !waitingForReadyTransition;
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
        readyPanel.OpenTransitionCompleted -= HandleReadyPanelTransitionCompleted;
        readyPanel.OpenTransitionCompleted += HandleReadyPanelTransitionCompleted;
    }

    private void UnbindReadyPanelState()
    {
        if (readyPanel != null)
        {
            readyPanel.OpenStateChanged -= HandleReadyPanelStateChanged;
            readyPanel.OpenTransitionCompleted -= HandleReadyPanelTransitionCompleted;
        }
    }

    private void HandleReadyPanelStateChanged(bool opened)
    {
        // The open/close notification precedes the visual transition.
        waitingForReadyTransition = true;
        ResolveTransitionVisuals();
        ApplyTransitionButtonLock(true);
        RefreshButtonLabel();
    }

    private void HandleReadyPanelTransitionCompleted()
    {
        waitingForReadyTransition = false;
        ApplyTransitionButtonLock(false);
        if (readyPanel == null || !readyPanel.IsOpen)
            return;

        ResolveTutorialController();
        tutorialController?.TryBeginFirstExpeditionDialogueFromReadyPanel();
    }

    private bool CanOpenReadyPanelForTutorial()
    {
        if (DataManager.Instance == null || DataManager.Instance.LobbyRuntimeStore == null)
            return false;

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        return lobby.TutorialProgress == LobbyTutorialProgress.WaitingForSetup;
    }

    private void ResolveTutorialController()
    {
        if (tutorialController != null)
            return;

        tutorialController = FindFirstObjectByType<LobbyTutorialController>(FindObjectsInactive.Include);
    }

    private void ResolveTransitionVisuals()
    {
        if (readyTransition == null)
            readyTransition = FindFirstObjectByType<ReadyTransitionProgressUI>(FindObjectsInactive.Include);

        if (buttonLeftGraphic == null)
        {
            Transform child = transform.Find("Button_l");
            if (child != null) buttonLeftGraphic = child.GetComponent<Graphic>();
        }
        if (buttonRightGraphic == null)
        {
            Transform child = transform.Find("Button_r");
            if (child != null) buttonRightGraphic = child.GetComponent<Graphic>();
        }
        if (playTextGraphic == null)
        {
            Transform child = transform.Find("PlayText");
            if (child != null) playTextGraphic = child.GetComponent<Graphic>();
        }
    }

    private void ApplyTransitionButtonLock(bool locked)
    {
        if (transitionVisualLocked == locked)
            return;

        ResolveTransitionVisuals();
        if (locked)
        {
            if (buttonLeftGraphic != null) leftOriginalColor = buttonLeftGraphic.color;
            if (buttonRightGraphic != null) rightOriginalColor = buttonRightGraphic.color;
            if (playTextGraphic != null) textOriginalColor = playTextGraphic.color;
        }

        transitionVisualLocked = locked;
        // Lock the Button as well as the handler so rapid successive input cannot start a battle.
        if (button != null) button.interactable = !locked && !isProcessing;
        leftFadeStart = buttonLeftGraphic != null ? buttonLeftGraphic.color : Color.white;
        rightFadeStart = buttonRightGraphic != null ? buttonRightGraphic.color : Color.white;
        textFadeStart = playTextGraphic != null ? playTextGraphic.color : Color.white;
        colorFadeElapsed = 0f;
        colorFadeActive = true;
        if (transitionColorFadeDuration <= 0f)
            UpdateTransitionColors(true);
    }

    private void UpdateTransitionColors(bool completeImmediately = false)
    {
        if (!colorFadeActive)
            return;

        colorFadeElapsed += Time.unscaledDeltaTime;
        float t = completeImmediately || transitionColorFadeDuration <= 0f
            ? 1f
            : Mathf.Clamp01(colorFadeElapsed / transitionColorFadeDuration);
        t = t * t * (3f - 2f * t);
        if (buttonLeftGraphic != null)
            buttonLeftGraphic.color = Color.Lerp(leftFadeStart, transitionVisualLocked ? transitionLockedColor : leftOriginalColor, t);
        if (buttonRightGraphic != null)
            buttonRightGraphic.color = Color.Lerp(rightFadeStart, transitionVisualLocked ? transitionLockedColor : rightOriginalColor, t);
        if (playTextGraphic != null)
            playTextGraphic.color = Color.Lerp(textFadeStart, transitionVisualLocked ? transitionLockedColor : textOriginalColor, t);
        if (t >= 1f)
            colorFadeActive = false;
    }

    private void RefreshButtonLabel()
    {
        ResolveButtonLabel();
        if (buttonLabel == null)
            return;

        string key = readyPanel != null && readyPanel.IsOpen
            ? LocalizationKeys.Lobby.PlayDepart
            : LocalizationKeys.Lobby.PlayReady;
        LocalizedTMPText localizer = buttonLabel.GetComponent<LocalizedTMPText>();
        if (localizer != null)
            localizer.Configure(key, string.Empty, false);
        else
            buttonLabel.text = GameLocalization.Get(key);
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
            ShowWarningKey(LocalizationKeys.Warning.PartyEmpty);
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
            ShowWarningKey(LocalizationKeys.Warning.PartyEmpty);
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
        return GameLocalization.Format(LocalizationKeys.Warning.PartyNotFull, requiredCount, currentCount);
    }

    private void ShowWarningKey(string key, params object[] arguments) =>
        ShowWarning(GameLocalization.Format(key, arguments));

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
            ShowWarningKey(LocalizationKeys.Warning.NetworkBattleStartSyncFailed);
            return false;
        }

        if (!battleStartSynchronizer.CanLocalPlayerStartBattle())
        {
            ShowWarningKey(LocalizationKeys.Warning.NetworkHostOnlyStart);
            return false;
        }

        if (sharedStateSynchronizer == null)
        {
            ShowWarningKey(LocalizationKeys.Warning.NetworkBattleStartSyncFailed);
            return false;
        }

        LobbySharedStateSnapshot snapshot =
            sharedStateSynchronizer.PublishHostSnapshotNow();
        if (snapshot == null || snapshot.Revision <= 0)
        {
            ShowWarningKey(LocalizationKeys.Warning.NetworkBattleStartSyncFailed);
            return false;
        }

        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();
        if (!battleStartSynchronizer.TryBroadcastBattleStart(
                snapshot,
                mapRuntime,
                out command))
        {
            ShowWarningKey(LocalizationKeys.Warning.NetworkBattleStartSyncFailed);
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
