using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발 중 저장 진행도를 빠르게 초기화하기 위한 단축키입니다.
/// ] 키를 누르면 게임 진행 데이터만 초기화하고 타이틀로 이동합니다.
/// ; 키를 누르면 현재 저장된 획득 기록 기준 도감을 열고, ' 키를 누르면 저장을 바꾸지 않고 도감을 전체 공개합니다.
/// - 키를 누르면 푸른 더스티움을 100 감소시키고, = 키를 누르면 100 증가시킵니다.
/// L 키를 누르면 테스트 치트를 켜거나 끕니다. ON 시 Item_001~Item_012 각각 +10, 푸른 더스티움 +5000, 캐릭터 레벨 +5가 적용되고 OFF 시 안전하게 회수합니다.
/// 언어, 음량 등 환경설정은 유지합니다.
/// </summary>
public sealed class DebugProgressResetHotkey : MonoBehaviour
{
    private static DebugProgressResetHotkey instance;
    private bool isResetting;
    private bool pendingCharacterCheatGrant;
    private bool testBundleActive;
    private int blueDustiumBeforeCheat;

    private const int TestItemGrantCount = 10;
    private const int TestBlueDustiumGrant = 5000;
    private const int CharacterLevelCheatDelta = 5;
    private const int MaxCharacterLevel = 30;

    private readonly Dictionary<string, int> itemCountsBeforeCheat = new();
    private readonly Dictionary<string, int> grantedCharacterLevels = new();
    private readonly Dictionary<string, int> grantedCharacterExperience = new();

    private static readonly string[] DefaultTestCharacterIds =
    {
        "Char_01",
        "Char_02",
        "Char_03"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfNeeded()
    {
        if (instance != null)
            return;

        DebugProgressResetHotkey existing =
            FindFirstObjectByType<DebugProgressResetHotkey>(FindObjectsInactive.Include);

        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject root = new GameObject(nameof(DebugProgressResetHotkey));
        instance = root.AddComponent<DebugProgressResetHotkey>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (isResetting)
            return;

        TryApplyPendingCharacterCheat();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.lKey.wasPressedThisFrame)
        {
            ToggleTestBundle();
            return;
        }

        if (keyboard.rightBracketKey.wasPressedThisFrame)
        {
            ResetAllProgress();
            return;
        }

        if (keyboard.semicolonKey.wasPressedThisFrame)
        {
            OpenRecordForDebug(false);
            return;
        }

        if (keyboard.quoteKey.wasPressedThisFrame)
        {
            OpenRecordForDebug(true);
            return;
        }

        if (keyboard.minusKey.wasPressedThisFrame)
        {
            ChangeBlueDustium(-100);
            return;
        }

        if (keyboard.equalsKey.wasPressedThisFrame)
            ChangeBlueDustium(100);
    }

    private static void OpenRecordForDebug(bool revealAll)
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);

        if (uiManager == null)
        {
            Debug.LogWarning("[DebugProgressResetHotkey] UIManager를 찾지 못해 도감을 열 수 없습니다.");
            return;
        }

        uiManager.ShowRecord(revealAll);

        Debug.Log(revealAll
            ? "[DebugProgressResetHotkey] 도감 전체 공개 미리보기를 열었습니다. 저장 데이터는 변경하지 않습니다."
            : "[DebugProgressResetHotkey] 현재 저장된 획득 기록 기준으로 도감을 열었습니다.");
    }

    private void ToggleTestBundle()
    {
        if (testBundleActive)
            DisableTestBundle();
        else
            EnableTestBundle();
    }

    private void EnableTestBundle()
    {
        DataManager dataManager = DataManager.Instance;

        if (dataManager != null)
            InitialDefaultPartySetup.TryInitialize(dataManager);

        LobbyRuntimeData lobby = dataManager?.LobbyRuntimeStore?.GetOrCreate();
        if (dataManager == null || lobby == null)
        {
            Debug.LogWarning("[DebugProgressResetHotkey] 테스트 치트에 필요한 런타임 데이터를 찾지 못했습니다.");
            ShowCheatWarning("테스트 치트를 적용할 수 없습니다.");
            return;
        }

        lobby.BagItemIds ??= new List<string>();
        itemCountsBeforeCheat.Clear();

        for (int itemNumber = 1; itemNumber <= 12; itemNumber++)
        {
            string itemId = $"Item_{itemNumber:000}";
            itemCountsBeforeCheat[itemId] = CountItem(lobby.BagItemIds, itemId);

            for (int count = 0; count < TestItemGrantCount; count++)
                lobby.BagItemIds.Add(itemId);
        }

        blueDustiumBeforeCheat = lobby.BlueDustium;
        lobby.BlueDustium += TestBlueDustiumGrant;

        grantedCharacterLevels.Clear();
        grantedCharacterExperience.Clear();
        testBundleActive = true;
        pendingCharacterCheatGrant = true;

        int changedCharacterCount = ApplyCharacterCheatToExistingCharacters(dataManager);
        TryCompletePendingCharacterCheat(dataManager);

        RefreshCheatAffectedUI();
        SaveProgress();

        ShowCheatWarning("테스트 치트가 활성화되었습니다.");
        Debug.Log($"[DebugProgressResetHotkey] 테스트 치트 ON: Item_001~Item_012 각 +{TestItemGrantCount}, 푸른 더스티움 +{TestBlueDustiumGrant}, 캐릭터 {changedCharacterCount}명 레벨 +{CharacterLevelCheatDelta}.");
    }

    private void DisableTestBundle()
    {
        DataManager dataManager = DataManager.Instance;
        LobbyRuntimeData lobby = dataManager?.LobbyRuntimeStore?.GetOrCreate();

        if (dataManager == null || lobby == null)
        {
            Debug.LogWarning("[DebugProgressResetHotkey] 테스트 치트 해제에 필요한 런타임 데이터를 찾지 못했습니다.");
            ShowCheatWarning("테스트 치트를 해제할 수 없습니다.");
            return;
        }

        lobby.BagItemIds ??= new List<string>();

        for (int itemNumber = 1; itemNumber <= 12; itemNumber++)
        {
            string itemId = $"Item_{itemNumber:000}";
            int originalCount = itemCountsBeforeCheat.TryGetValue(itemId, out int savedCount) ? savedCount : 0;
            int currentCount = CountItem(lobby.BagItemIds, itemId);
            int reclaimCount = Mathf.Min(TestItemGrantCount, Mathf.Max(0, currentCount - originalCount));
            RemoveItemOccurrences(lobby.BagItemIds, itemId, reclaimCount);
        }

        int blueDustiumReclaim = Mathf.Min(TestBlueDustiumGrant, Mathf.Max(0, lobby.BlueDustium - blueDustiumBeforeCheat));
        lobby.BlueDustium = Mathf.Max(0, lobby.BlueDustium - blueDustiumReclaim);

        int changedCharacterCount = RemoveCharacterCheat(dataManager);

        pendingCharacterCheatGrant = false;
        testBundleActive = false;
        itemCountsBeforeCheat.Clear();
        grantedCharacterLevels.Clear();
        grantedCharacterExperience.Clear();
        blueDustiumBeforeCheat = 0;

        RefreshCheatAffectedUI();
        SaveProgress();

        ShowCheatWarning("테스트 치트가 비활성화되었습니다.");
        Debug.Log($"[DebugProgressResetHotkey] 테스트 치트 OFF: 남아 있는 치트 지급분만 회수하고 캐릭터 {changedCharacterCount}명에서 치트 레벨을 제거했습니다.");
    }

    private static void ShowCheatWarning(string message)
    {
        if (SettingWarningUI.ShowMessage(message))
            return;

        TitleWarningUI titleWarningUI = TitleWarningUI.Instance;
        if (titleWarningUI == null)
            titleWarningUI = FindFirstObjectByType<TitleWarningUI>(FindObjectsInactive.Include);

        if (titleWarningUI != null)
            titleWarningUI.Show(message);
    }

    private void TryApplyPendingCharacterCheat()
    {
        if (!testBundleActive || !pendingCharacterCheatGrant)
            return;

        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null)
            return;

        int changedCount = ApplyCharacterCheatToExistingCharacters(dataManager);
        bool completed = TryCompletePendingCharacterCheat(dataManager);

        if (changedCount > 0)
            SaveProgress();

        if (completed)
            Debug.Log("[DebugProgressResetHotkey] 기본 캐릭터 생성 후 테스트 치트의 레벨 +5 적용을 완료했습니다.");
    }

    private bool TryCompletePendingCharacterCheat(DataManager dataManager)
    {
        if (!pendingCharacterCheatGrant || dataManager?.CharacterRuntimeStore == null)
            return false;

        for (int i = 0; i < DefaultTestCharacterIds.Length; i++)
        {
            string characterId = DefaultTestCharacterIds[i];
            if (!dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData character) ||
                character == null ||
                !grantedCharacterLevels.ContainsKey(characterId))
            {
                return false;
            }
        }

        pendingCharacterCheatGrant = false;
        return true;
    }

    private int ApplyCharacterCheatToExistingCharacters(DataManager dataManager)
    {
        if (dataManager?.CharacterRuntimeStore == null)
            return 0;

        int changedCount = 0;

        foreach (CharacterRuntimeData character in dataManager.CharacterRuntimeStore.GetAll().Values)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId))
                continue;

            if (grantedCharacterLevels.ContainsKey(character.CharacterId))
                continue;

            int levelBefore = Mathf.Clamp(character.Level, 1, MaxCharacterLevel);
            int levelAfter = Mathf.Min(MaxCharacterLevel, levelBefore + CharacterLevelCheatDelta);
            int grantedLevels = levelAfter - levelBefore;
            int experienceBeforeLevel = BattleStageClearExperienceService.GetCumulativeExperienceForLevel(levelBefore);
            int experienceAfterLevel = BattleStageClearExperienceService.GetCumulativeExperienceForLevel(levelAfter);
            int grantedExperience = Mathf.Max(0, experienceAfterLevel - experienceBeforeLevel);

            character.Level = levelAfter;
            character.Exp = Mathf.Max(0, character.Exp + grantedExperience);

            grantedCharacterLevels[character.CharacterId] = grantedLevels;
            grantedCharacterExperience[character.CharacterId] = grantedExperience;
            changedCount++;
        }

        return changedCount;
    }

    private int RemoveCharacterCheat(DataManager dataManager)
    {
        if (dataManager?.CharacterRuntimeStore == null)
            return 0;

        int changedCount = 0;

        foreach (KeyValuePair<string, int> pair in grantedCharacterLevels)
        {
            if (!dataManager.CharacterRuntimeStore.TryGet(pair.Key, out CharacterRuntimeData character) || character == null)
                continue;

            int grantedLevels = Mathf.Max(0, pair.Value);
            int grantedExperience = grantedCharacterExperience.TryGetValue(pair.Key, out int experience)
                ? Mathf.Max(0, experience)
                : 0;

            character.Level = Mathf.Max(1, character.Level - grantedLevels);
            character.Exp = Mathf.Max(0, character.Exp - grantedExperience);
            changedCount++;
        }

        return changedCount;
    }

    private static int CountItem(List<string> itemIds, string itemId)
    {
        if (itemIds == null || string.IsNullOrEmpty(itemId))
            return 0;

        int count = 0;
        for (int i = 0; i < itemIds.Count; i++)
        {
            if (itemIds[i] == itemId)
                count++;
        }

        return count;
    }

    private static void RemoveItemOccurrences(List<string> itemIds, string itemId, int removeCount)
    {
        if (itemIds == null || string.IsNullOrEmpty(itemId) || removeCount <= 0)
            return;

        for (int i = itemIds.Count - 1; i >= 0 && removeCount > 0; i--)
        {
            if (itemIds[i] != itemId)
                continue;

            itemIds.RemoveAt(i);
            removeCount--;
        }
    }

    private static void RefreshCheatAffectedUI()
    {
        BattleBagPanelUI.RefreshAll();
        LobbyBlueDustiumHudUI.RefreshAll();
    }

    private static void SaveProgress()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveCurrentProgress();
    }

    private static void ChangeBlueDustium(int amount)
    {
        DataManager dataManager = DataManager.Instance;
        LobbyRuntimeData lobby = dataManager?.LobbyRuntimeStore?.GetOrCreate();

        if (lobby == null)
        {
            Debug.LogWarning("[DebugProgressResetHotkey] 로비 런타임 데이터를 찾지 못해 푸른 더스티움을 변경하지 못했습니다.");
            return;
        }

        lobby.BlueDustium = Mathf.Max(0, lobby.BlueDustium + amount);

        LobbyBlueDustiumHudUI.RefreshAll();

        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveCurrentProgress();

        Debug.Log($"[DebugProgressResetHotkey] 푸른 더스티움 변경: {amount:+#;-#;0}, 현재 {lobby.BlueDustium}");
    }

    private async void ResetAllProgress()
    {
        isResetting = true;
        pendingCharacterCheatGrant = false;
        testBundleActive = false;
        itemCountsBeforeCheat.Clear();
        grantedCharacterLevels.Clear();
        grantedCharacterExperience.Clear();
        blueDustiumBeforeCheat = 0;

        // 저장 파일에 들어 있는 아이템, 재화, 캐릭터 성장, 편성,
        // 클리어 및 탐사 진행 데이터를 삭제합니다.
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.DeleteSaveFile();

        // PlayerPrefs에 별도로 저장되는 시련 해금 진행도를 삭제합니다.
        TrialUnlockProgress.ResetProgress();
        TrialSelectionState.Clear();
        IntroSettings.ResetIntroSeenState();

        ResetRuntimeStores();

        Debug.Log("[DebugProgressResetHotkey] 모든 게임 진행 데이터를 초기화했습니다.");

        // 현재 화면에 남아 있는 UI와 런타임 표시를 정리하기 위해 타이틀로 돌아갑니다.
        if (GameManager.Instance != null &&
            GameManager.Instance.StateMachine != null)
        {
            try
            {
                await GameManager.Instance.StateMachine.ChangeState(GameStateType.Title);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // 씬 전환 과정에서 다른 UI/런타임 코드가 저장 파일을 다시 생성했더라도
        // 초기화 결과가 확실히 유지되도록 타이틀 진입 후 한 번 더 삭제합니다.
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.DeleteSaveFile();

            if (SaveSystem.Instance.HasSaveFile())
                Debug.LogError("[DebugProgressResetHotkey] 초기화 후에도 저장 파일이 남아 있습니다.");
        }

        await TitleManager.RefreshRunButtonsAfterSceneReadyAsync();
        isResetting = false;
    }

    private static void ResetRuntimeStores()
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
            return;

        dataManager.PlayerRuntimeStore?.SetData(null);
        dataManager.PartyRuntimeStore?.Clear();
        dataManager.CharacterRuntimeStore?.Clear();
        dataManager.SkillRuntimeStore?.Clear();
        dataManager.MapRuntimeStore?.Clear();
        dataManager.BattleRuntimeStore?.Clear();
        dataManager.LobbyRuntimeStore?.Set(new LobbyRuntimeData());

        if (GameManager.Instance != null && GameManager.Instance.Context != null)
            GameManager.Instance.Context.SelectedGameMode = GameMode.None;
    }
}
