using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattlePlayButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Stage Carousel")]
    [Tooltip("?¤í…Œ?´ì? ë²„íŠ¼??ìºëŸ¬?€ ë°©ì‹?¼ë¡œ ?¬ìš©?????°ê²°?©ë‹ˆ?? ë¹„ì›Œ?ë©´ ?¬ì—???ë™?¼ë¡œ ì°¾ìŠµ?ˆë‹¤.")]
    [SerializeField] private LobbyStageButtonCarousel stageButtonCarousel;
    [Tooltip("ì¤‘ì•™???ˆëŠ” ?¤í…Œ?´ì?ê°€ ? ê²¨ ?ˆì„ ??PlayButton???„ë¥´ë©??…ì¥??ë§‰ê³  ê²½ê³ ë¥??œì‹œ?©ë‹ˆ??")]
    [SerializeField] private bool blockLockedCarouselStage = true;

    [Header("Option")]
    [SerializeField] private bool checkMapSelected = true;
    [SerializeField] private bool checkPartyExists = true;
    [SerializeField] private bool requireFullParty = true;

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;
    [SerializeField] private string lockedStageEnterMessage = "?„ì§ ?…ì¥?????†ëŠ” êµ¬ì—­?…ë‹ˆ??";
    [SerializeField] private string mapNotSelectedMessage = "?¤í…Œ?´ì?ë¥?? íƒ?´ì•¼ ?©ë‹ˆ??";
    [SerializeField] private string partyEmptyMessage = "ìºë¦­?°ë? ?¸ì„±?´ì•¼ ?©ë‹ˆ??";
    [SerializeField] private string partyNotFullMessage = "ìºë¦­??3ëª…ì„ ëª¨ë‘ ?¸ì„±?´ì•¼ ?©ë‹ˆ?? ?„ì¬ {0}/{1}";
    [SerializeField] private string dataManagerMissingMessage = "?°ì´??ë§¤ë‹ˆ?€ê°€ ?†ìŠµ?ˆë‹¤.";
    [SerializeField] private string gameManagerMissingMessage = "ê²Œì„ ë§¤ë‹ˆ?€ê°€ ?†ìŠµ?ˆë‹¤.";
    [SerializeField] private string networkClientStartBlockedMessage = "Only the host can start in multiplayer lobby.";
    [SerializeField] private string networkBattleStartSyncFailedMessage = "Failed to synchronize battle start.";

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    private bool isProcessing;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        FindWarningUIIfMissing();
        FindStageCarouselIfMissing();
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

            // ?…ì¥ ?œì ???„ì¬ ì¤‘ì•™??ë³´ì´??êµ¬ì—­???¤ì œ ? íƒê°’ìœ¼ë¡??•ì •?©ë‹ˆ??
            // ìºëŸ¬?€???´ë™????êµ¬ì—­ ë²„íŠ¼???¤ì‹œ ?„ë¥´ì§€ ?Šì•„???©ë‹ˆ??
            CommitCenteredCarouselStage();

            if (IsLockedCarouselStageCentered())
            {
                ShowWarning(lockedStageEnterMessage);
                return;
            }

            if (DataManager.Instance == null)
            {
                ShowWarning(dataManagerMissingMessage);
                Debug.LogWarning("[BattlePlayButton] DataManager is null.");
                return;
            }

            if (checkMapSelected && !IsMapSelected())
            {
                ShowWarning(mapNotSelectedMessage);
                Debug.LogWarning("[BattlePlayButton] ? íƒ??ì±•í„°/?¤í…Œ?´ì?ê°€ ?†ìŠµ?ˆë‹¤.");
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

    private void CommitCenteredCarouselStage()
    {
        FindStageCarouselIfMissing();

        if (stageButtonCarousel != null)
            stageButtonCarousel.CommitCurrentStageSelection();
    }

    private bool IsLockedCarouselStageCentered()
    {
        if (!blockLockedCarouselStage)
            return false;

        FindStageCarouselIfMissing();

        return stageButtonCarousel != null && stageButtonCarousel.IsCurrentStageLocked();
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
                Debug.LogWarning($"[BattlePlayButton] ?Œí‹° ?¸ì›??ë¶€ì¡±í•©?ˆë‹¤. Current:{currentCount} / Required:{requiredCount}");
                return false;
            }
        }
        else if (currentCount <= 0)
        {
            ShowWarning(partyEmptyMessage);
            Debug.LogWarning("[BattlePlayButton] ?Œí‹°??ìºë¦­?°ê? ?†ìŠµ?ˆë‹¤.");
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
            return $"ìºë¦­??{requiredCount}ëª…ì„ ëª¨ë‘ ?¸ì„±?´ì•¼ ?©ë‹ˆ?? ?„ì¬ {currentCount}/{requiredCount}";

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

    private void FindStageCarouselIfMissing()
    {
        if (stageButtonCarousel != null)
            return;

        stageButtonCarousel = FindFirstObjectByType<LobbyStageButtonCarousel>(FindObjectsInactive.Include);
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
