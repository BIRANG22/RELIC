using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발 중 저장 진행도를 빠르게 초기화하기 위한 단축키입니다.
/// ] 키를 누르면 게임 진행 데이터만 초기화하고 타이틀로 이동합니다.
/// ; 키를 누르면 현재 저장된 획득 기록 기준 도감을 열고, ' 키를 누르면 저장을 바꾸지 않고 도감을 전체 공개합니다.
/// - 키를 누르면 푸른 더스티움을 100 감소시키고, = 키를 누르면 100 증가시킵니다.
/// [ 키를 누르면 Item_001~Item_012를 각각 10개 획득하고, 푸른 더스티움 5000과 캐릭터 5레벨 테스트 상태를 준비합니다.
/// 언어, 음량 등 환경설정은 유지합니다.
/// </summary>
public sealed class DebugProgressResetHotkey : MonoBehaviour
{
    private static DebugProgressResetHotkey instance;
    private bool isResetting;
    private bool pendingLevel5Grant;

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

        TryApplyPendingLevel5Grant();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.leftBracketKey.wasPressedThisFrame)
        {
            GrantTestBundle();
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

    private static void GrantTestBundle()
    {
        DataManager dataManager = DataManager.Instance;
        LobbyRuntimeData lobby = dataManager?.LobbyRuntimeStore?.GetOrCreate();

        if (dataManager == null || lobby == null)
        {
            Debug.LogWarning("[DebugProgressResetHotkey] 테스트 지급에 필요한 런타임 데이터를 찾지 못했습니다.");
            return;
        }

        lobby.BagItemIds ??= new System.Collections.Generic.List<string>();
        for (int itemNumber = 1; itemNumber <= 12; itemNumber++)
        {
            string itemId = $"Item_{itemNumber:000}";
            for (int count = 0; count < 10; count++)
                lobby.BagItemIds.Add(itemId);
        }

        ChangeBlueDustium(5000);

        instance.pendingLevel5Grant = true;
        int leveledCharacterCount = ApplyLevel5ToExistingCharacters(dataManager);
        instance.TryCompletePendingLevel5Grant(dataManager);

        BattleBagPanelUI.RefreshAll();

        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveCurrentProgress();

        Debug.Log($"[DebugProgressResetHotkey] [ 치트 적용: Item_001~Item_012 각 10개, 푸른 더스티움 +5000, 5레벨 미만 캐릭터 {leveledCharacterCount}명 -> Lv.5. 아직 기본 캐릭터가 생성되지 않았다면 로비 생성 직후 자동 적용됩니다.");
    }

    private void TryApplyPendingLevel5Grant()
    {
        if (!pendingLevel5Grant)
            return;

        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null)
            return;

        int changedCount = ApplyLevel5ToExistingCharacters(dataManager);
        bool completed = TryCompletePendingLevel5Grant(dataManager);

        if (changedCount > 0 && SaveSystem.Instance != null)
            SaveSystem.Instance.SaveCurrentProgress();

        if (completed)
            Debug.Log("[DebugProgressResetHotkey] 기본 캐릭터 생성 후 [ 치트의 Lv.5 적용을 완료했습니다.");
    }

    private bool TryCompletePendingLevel5Grant(DataManager dataManager)
    {
        if (!pendingLevel5Grant || dataManager?.CharacterRuntimeStore == null)
            return false;

        for (int i = 0; i < DefaultTestCharacterIds.Length; i++)
        {
            if (!dataManager.CharacterRuntimeStore.TryGet(DefaultTestCharacterIds[i], out CharacterRuntimeData character) ||
                character == null ||
                character.Level < 5)
            {
                return false;
            }
        }

        pendingLevel5Grant = false;
        return true;
    }

    private static int ApplyLevel5ToExistingCharacters(DataManager dataManager)
    {
        if (dataManager?.CharacterRuntimeStore == null)
            return 0;

        int changedCount = 0;
        int level5Experience = BattleStageClearExperienceService.GetCumulativeExperienceForLevel(5);

        foreach (CharacterRuntimeData character in dataManager.CharacterRuntimeStore.GetAll().Values)
        {
            if (character == null || character.Level >= 5)
                continue;

            character.Level = 5;
            character.Exp = Mathf.Max(character.Exp, level5Experience);
            changedCount++;
        }

        return changedCount;
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
        pendingLevel5Grant = false;

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
