using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발 중 저장 진행도를 빠르게 초기화하기 위한 단축키입니다.
/// ] 키를 누르면 게임 진행 데이터만 초기화하고 타이틀로 이동합니다.
/// ; 키를 누르면 현재 저장된 획득 기록 기준 도감을 열고, ' 키를 누르면 저장을 바꾸지 않고 도감을 전체 공개합니다.
/// - 키를 누르면 푸른 더스티움을 100 감소시키고, = 키를 누르면 100 증가시킵니다.
/// 언어, 음량 등 환경설정은 유지합니다.
/// </summary>
public sealed class DebugProgressResetHotkey : MonoBehaviour
{
    private static DebugProgressResetHotkey instance;
    private bool isResetting;

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

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

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

        // 저장 파일에 들어 있는 아이템, 재화, 캐릭터 성장, 편성,
        // 클리어 및 탐사 진행 데이터를 삭제합니다.
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.DeleteSaveFile();

        // PlayerPrefs에 별도로 저장되는 시련 해금 진행도를 삭제합니다.
        TrialUnlockProgress.ResetProgress();
        TrialSelectionState.Clear();

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
