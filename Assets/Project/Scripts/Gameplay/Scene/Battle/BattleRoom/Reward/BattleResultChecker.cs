using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultChecker : MonoBehaviour
{
    private const float DefeatRewardMultiplier = 0.5f;

    public static BattleResultChecker Instance { get; private set; }
    public static event System.Action BattleFinished;

    public bool BattleEnded => battleEnded;

    [SerializeField] private BattleRewardResolver rewardResolver;
    [SerializeField] private BattleRewardPanelUI rewardPanel;
    [SerializeField] private ExplorationResultPanelUI explorationResultPanel;
    [SerializeField] private GameObject nextButtonRoot;

    private bool battleEnded;
    private Button nextButton;
    private System.Action pendingRewardFlowCompletedCallback;

    private void Awake()
    {
        Instance = this;
        BindNextButton();
        SetNextButtonVisible(false);
    }

    private void OnDisable()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnBattleRewardContinueClicked);
    }

    public void ResetBattle()
    {
        battleEnded = false;
        pendingRewardFlowCompletedCallback = null;
        SetNextButtonVisible(false);

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();
    }

    public bool CheckBattleEnd()
    {
        if (battleEnded)
            return false;

        if (IsAllPlayersDead())
        {
            battleEnded = true;
            BattleFinished?.Invoke();
            Debug.Log("[BattleResultChecker] Battle Lose");
            OpenDefeatExplorationResultPanel();
            return true;
        }

        if (IsAllMonstersDead())
        {
            battleEnded = true;
            BattleFinished?.Invoke();
            Debug.Log("[BattleResultChecker] Battle Win");
            BattleEquipmentEffectService.ApplyBattleEndHealToParty();

            IReadOnlyList<MonsterRuntimeData> defeatedMonsters =
                BattleRewardCollector.Instance != null
                    ? BattleRewardCollector.Instance.CollectedMonsters
                    : null;

            TrialUnlockProgress.RecordDefeatedMonsters(defeatedMonsters);

            bool isBossNode = IsCurrentNodeBoss();
            if (isBossNode)
            {
                MapRuntimeData runtime = DataManager.Instance?.MapRuntimeStore?.Get();
                TrialUnlockProgress.RecordBossClear(
                    runtime,
                    TrialSelectionState.SelectedMask);

                if (!OpenRewardPanel(OpenExplorationResultPanel))
                    OpenExplorationResultPanel();
            }
            else
            {
                OpenRewardPanel();
            }
            return true;
        }

        return false;
    }

    private void OpenExplorationResultPanel()
    {
        if (explorationResultPanel == null)
        {
            explorationResultPanel = Object.FindFirstObjectByType<ExplorationResultPanelUI>(
                FindObjectsInactive.Include);
        }

        if (explorationResultPanel == null)
        {
            Debug.LogError("[BattleResultChecker] ExplorationResultPanelUI is missing.");
            return;
        }

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        explorationResultPanel.Open();
    }

    private void OpenDefeatExplorationResultPanel()
    {
        if (explorationResultPanel == null)
        {
            explorationResultPanel = Object.FindFirstObjectByType<ExplorationResultPanelUI>(
                FindObjectsInactive.Include);
        }

        if (explorationResultPanel == null)
        {
            Debug.LogError("[BattleResultChecker] ExplorationResultPanelUI is missing.");
            return;
        }

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        CleanBattleRoom();
        explorationResultPanel.OpenDefeat(DefeatRewardMultiplier);
    }

    private static void CleanBattleRoom()
    {
        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);

        if (cleaner != null)
            cleaner.Clean();
    }

    private static bool IsCurrentNodeBoss()
    {
        MapRuntimeData runtime = DataManager.Instance?.MapRuntimeStore?.Get();
        GeneratedMapNodeData node = MapRuntimeProgressUtility.FindCurrentNode(runtime);
        return node != null && string.Equals(node.Type, "Boss", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool OpenRewardPanel(System.Action onRewardFlowCompleted = null)
    {
        if (rewardResolver == null || rewardPanel == null)
        {
            Debug.LogError("[BattleResultChecker] RewardResolver or RewardPanel is missing.");
            return false;
        }

        IReadOnlyList<MonsterRuntimeData> monsters =
            BattleRewardCollector.Instance != null
                ? BattleRewardCollector.Instance.CollectedMonsters
                : null;

        Debug.Log($"[BattleResultChecker] RewardMonsterCount:{monsters?.Count ?? 0}");

        List<BattleRewardData> rewards = rewardResolver.Resolve(monsters) ?? new List<BattleRewardData>();

        Debug.Log($"[BattleResultChecker] ResolvedRewardCount:{rewards.Count}");

        rewardPanel.Open(rewards, () => OnBattleRewardPanelCompleted(onRewardFlowCompleted));
        return true;
    }

    private void OnBattleRewardPanelCompleted(System.Action completedCallback)
    {
        pendingRewardFlowCompletedCallback = completedCallback;

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        BindNextButton();

        if (nextButtonRoot == null || nextButton == null)
        {
            Debug.LogWarning("[BattleResultChecker] NextButton is missing; completing reward flow immediately.");
            CompletePendingBattleRewardFlow();
            return;
        }

        SetNextButtonVisible(true);
    }

    private void OnBattleRewardContinueClicked()
    {
        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        CompletePendingBattleRewardFlow();
    }

    private void CompletePendingBattleRewardFlow()
    {
        System.Action completedCallback = pendingRewardFlowCompletedCallback;
        pendingRewardFlowCompletedCallback = null;
        SetNextButtonVisible(false);
        CompleteBattleRewardFlow(completedCallback);
    }

    private static void CompleteBattleRewardFlow(System.Action completedCallback)
    {
        MarkCurrentBattleNodeCleared();

        if (completedCallback != null)
        {
            BattleRoomCleaner cleaner =
                Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);
            cleaner?.PrepareForMapSelection();

            completedCallback.Invoke();
            return;
        }

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
        {
            // 일반 전투의 지도 복귀에서는 캐릭터/몬스터를 여기서 먼저 정리하지 않습니다.
            // BattleSceneController가 전환 화면으로 BattleRoom을 완전히 덮은 순간
            // PrepareRoomForMapSelection()을 호출해 전투 유닛을 정리합니다.
            sceneController.ReturnToMap();
            return;
        }

        // SceneController가 없는 예외 상황에서는 기존처럼 즉시 정리합니다.
        BattleRoomCleaner fallbackCleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);
        fallbackCleaner?.PrepareForMapSelection();
        Debug.LogWarning("[BattleResultChecker] BattleSceneController is missing.");
    }

    private void BindNextButton()
    {
        EnsureNextButtonRoot();

        if (nextButton == null)
            return;

        nextButton.onClick.RemoveListener(OnBattleRewardContinueClicked);
        nextButton.onClick.AddListener(OnBattleRewardContinueClicked);
    }

    private void EnsureNextButtonRoot()
    {
        if (nextButtonRoot == null)
        {
            Transform nextButtonTransform = FindChildRecursive(transform, "NextButton");

            if (nextButtonTransform != null)
                nextButtonRoot = nextButtonTransform.gameObject;
        }

        if (nextButtonRoot == null)
            return;

        if (nextButton == null || nextButton.gameObject != nextButtonRoot)
            nextButton = nextButtonRoot.GetComponent<Button>();
    }

    private void SetNextButtonVisible(bool visible)
    {
        EnsureNextButtonRoot();

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(visible);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child != null && child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void MarkCurrentBattleNodeCleared()
    {
        if (DataManager.Instance == null || DataManager.Instance.MapRuntimeStore == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();
        if (!MapRuntimeProgressUtility.MarkCurrentNodeCleared(runtime))
            return;

        DataManager.Instance.MapRuntimeStore.Set(runtime);
    }

    private bool IsAllMonstersDead()
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int aliveCount = 0;

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (!monster.RuntimeData.IsDead)
                aliveCount++;
        }

        return aliveCount <= 0;
    }

    private bool IsAllPlayersDead()
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        if (characters.Length == 0)
            return false;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || characters[i].RuntimeData == null)
                continue;

            if (characters[i].RuntimeData.CurrentHP > 0)
                return false;
        }

        return true;
    }
}
