using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultChecker : MonoBehaviour
{
    private const float DefeatRewardMultiplier = 0.5f;
    private static readonly Color NextStageUnavailableColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    public static BattleResultChecker Instance { get; private set; }
    public static event System.Action BattleFinished;

    public bool BattleEnded => battleEnded;

    [SerializeField] private BattleRewardResolver rewardResolver;
    [SerializeField] private BattleRewardPanelUI rewardPanel;
    [SerializeField] private ExplorationResultPanelUI explorationResultPanel;
    [SerializeField] private GameObject nextButtonRoot;

    [Header("Boss Clear Choice")]
    [SerializeField] private GameObject nextStageButtonRoot;
    [SerializeField] private GameObject returnButtonRoot;
    [SerializeField] private string nextStageUnavailableMessage = "아직 입장할 수 없는 구역입니다.";

    private bool battleEnded;
    private Button nextButton;
    private Button nextStageButton;
    private Button returnButton;
    private ButtonAnimationCoroutine nextStageButtonAnimation;
    private System.Action pendingRewardFlowCompletedCallback;

    private void Awake()
    {
        Instance = this;
        BindNextButton();
        BindBossClearButtons();
        SetNextButtonVisible(false);
        SetBossClearChoiceButtonsVisible(false);
    }

    private void OnDisable()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnBattleRewardContinueClicked);

        if (nextStageButton != null)
            nextStageButton.onClick.RemoveListener(OnNextStageButtonClicked);

        if (returnButton != null)
            returnButton.onClick.RemoveListener(OnReturnButtonClicked);
    }

    public void ResetBattle()
    {
        battleEnded = false;
        pendingRewardFlowCompletedCallback = null;
        SetNextButtonVisible(false);
        SetBossClearChoiceButtonsVisible(false);

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
            PrepareBattleFinishedPresentation();
            BattleFinished?.Invoke();
            Debug.Log("[BattleResultChecker] Battle Lose");
            OpenDefeatExplorationResultPanel();
            return true;
        }

        if (IsAllMonstersDead())
        {
            battleEnded = true;
            PrepareBattleFinishedPresentation();
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

                if (!OpenRewardPanel(ShowBossClearChoiceButtons))
                    ShowBossClearChoiceButtons();
            }
            else
            {
                OpenRewardPanel();
            }
            return true;
        }

        return false;
    }

    private static void PrepareBattleFinishedPresentation()
    {
        // 마지막 행동 애니메이션이 남아 있지 않도록 생존 유닛을 즉시 Idle로 복귀시킵니다.
        BattleHUDService hudService = new BattleHUDService();
        hudService.PlayAllAliveIdle();

        // 전투 실행 중 숨겨졌던 MenuRoot는 다음 플레이어 턴이 오지 않으면
        // 기존 복구 조건을 통과하지 못하므로 전투 종료 시 명시적으로 다시 표시합니다.
        BattleTurnExecutor[] executors = Object.FindObjectsByType<BattleTurnExecutor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < executors.Length; i++)
            executors[i]?.RestoreBattleExecutionUiAfterRoomEnd();
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

        ResumeData resume = CreateBattleRewardResume(rewards);
        SaveSystem.Instance?.SaveCheckpoint(resume);
        rewardPanel.Open(rewards, () => OnBattleRewardPanelCompleted(onRewardFlowCompleted), resume);
        return true;
    }


    private static ResumeData CreateBattleRewardResume(IReadOnlyList<BattleRewardData> rewards)
    {
        MapRuntimeData map = DataManager.Instance?.MapRuntimeStore?.Get();
        var resume = new ResumeData
        {
            Phase = ResumePhase.BattleReward,
            NodeIndex = map != null ? map.CurrentNodeIndex : -1,
            MapId = map?.CurrentMapId
        };

        if (rewards == null)
            return resume;

        for (int i = 0; i < rewards.Count; i++)
        {
            BattleRewardData reward = rewards[i];
            if (reward == null)
                continue;

            resume.PendingRewards.Add(new BattleRewardSaveData
            {
                Type = reward.Type,
                RewardId = reward.RewardId,
                Amount = reward.Amount
            });
        }

        return resume;
    }
    // Continue 복원 전용: 보상 수령 완료 뒤 정상 전투 종료와 같은 Next/보스 선택 UI를 재구성한다.
    public void RestoreBattleRewardCompletionPresentation()
    {
        if (IsCurrentNodeBoss())
        {
            ShowBossClearChoiceButtons();
            return;
        }

        OnBattleRewardPanelCompleted(null);
    }
    private void OnBattleRewardPanelCompleted(System.Action completedCallback)
    {
        pendingRewardFlowCompletedCallback = completedCallback;

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        // 보스전은 공용 NextButton을 거치지 않고 보상 종료 즉시
        // 다음 구역 / 거점 귀환 선택 버튼으로 전환합니다.
        if (completedCallback != null)
        {
            CompletePendingBattleRewardFlow();
            return;
        }

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

        // 보스전 등 별도 완료 콜백 경로는 기존처럼 즉시 버튼을 숨깁니다.
        // 일반 전투의 지도 복귀는 전환 화면이 완전히 덮인 뒤 숨겨 깜빡임을 방지합니다.
        if (completedCallback != null)
            SetNextButtonVisible(false);

        CompleteBattleRewardFlow(completedCallback);
    }

    private void CompleteBattleRewardFlow(System.Action completedCallback)
    {
        MarkCurrentBattleNodeCleared();

        if (completedCallback != null)
        {
            // 보스전은 보상 종료 후에도 BattleRoom 위에서 다음 구역 / 거점 귀환 선택 UI를 보여줍니다.
            // 이 시점에 BattleRoomCleaner를 실행하면 살아있는 캐릭터까지 제거되어 선택 화면에서 보이지 않으므로,
            // 실제 화면 전환이 시작되기 전까지 전투 유닛을 유지합니다.
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
            sceneController.ReturnToMap(() => SetNextButtonVisible(false));
            return;
        }

        // SceneController가 없는 예외 상황에서는 기존처럼 즉시 정리합니다.
        BattleRoomCleaner fallbackCleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);
        fallbackCleaner?.PrepareForMapSelection();
        Debug.LogWarning("[BattleResultChecker] BattleSceneController is missing.");
    }


    private void ShowBossClearChoiceButtons()
    {
        SetNextButtonVisible(false);
        BindBossClearButtons();
        SetBossClearChoiceButtonsVisible(true);
    }

    private void OnNextStageButtonClicked()
    {
        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        BattleWarningUI.ShowMessage(nextStageUnavailableMessage);
    }

    private void OnReturnButtonClicked()
    {
        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        SetBossClearChoiceButtonsVisible(false);
        OpenExplorationResultPanel();
    }

    private void BindBossClearButtons()
    {
        EnsureBossClearButtonRoots();

        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveListener(OnNextStageButtonClicked);
            nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(OnReturnButtonClicked);
            returnButton.onClick.AddListener(OnReturnButtonClicked);
        }

        // 2구역은 아직 구현 전이므로 NextStageButton은 클릭 자체는 받되
        // ButtonAnimationCoroutine의 Hover/Click 연출과 SFX는 진행하지 않습니다.
        if (nextStageButtonAnimation != null)
            nextStageButtonAnimation.SetInteractionEnabled(false);

        ApplyNextStageUnavailableVisual();
    }

    private void ApplyNextStageUnavailableVisual()
    {
        if (nextStageButtonRoot == null)
            return;

        Graphic[] graphics = nextStageButtonRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
                continue;

            // NextStageButton/ back 오브젝트는 비활성 상태에서도 원래 색을 유지합니다.
            // back (1), back (2), front, Text (TMP) 등 나머지 그래픽만 #777777로 표시합니다.
            if (string.Equals(graphic.transform.name, "back", System.StringComparison.OrdinalIgnoreCase))
                continue;

            Color color = graphic.color;
            color.r = NextStageUnavailableColor.r;
            color.g = NextStageUnavailableColor.g;
            color.b = NextStageUnavailableColor.b;
            graphic.color = color;
        }
    }

    private void EnsureBossClearButtonRoots()
    {
        if (nextStageButtonRoot == null)
        {
            Transform target = FindChildRecursive(transform, "NextStageButton");
            if (target != null)
                nextStageButtonRoot = target.gameObject;
        }

        if (returnButtonRoot == null)
        {
            Transform target = FindChildRecursive(transform, "ReturnButton");
            if (target != null)
                returnButtonRoot = target.gameObject;
        }

        if (nextStageButtonRoot != null)
        {
            if (nextStageButton == null || nextStageButton.gameObject != nextStageButtonRoot)
                nextStageButton = nextStageButtonRoot.GetComponent<Button>();

            if (nextStageButtonAnimation == null || nextStageButtonAnimation.gameObject != nextStageButtonRoot)
                nextStageButtonAnimation = nextStageButtonRoot.GetComponent<ButtonAnimationCoroutine>();
        }

        if (returnButtonRoot != null &&
            (returnButton == null || returnButton.gameObject != returnButtonRoot))
        {
            returnButton = returnButtonRoot.GetComponent<Button>();
        }
    }

    private void SetBossClearChoiceButtonsVisible(bool visible)
    {
        EnsureBossClearButtonRoots();

        if (nextStageButtonRoot != null)
            nextStageButtonRoot.SetActive(visible);

        if (returnButtonRoot != null)
            returnButtonRoot.SetActive(visible);
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
        SaveSystem.Instance?.ClearBattleRoomResumeState();
        SaveSystem.Instance?.SaveCheckpoint();
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
