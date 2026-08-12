using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleResultChecker : MonoBehaviour
{
    private const float DefeatRewardMultiplier = 0.5f;

    public static BattleResultChecker Instance { get; private set; }
    public static event System.Action BattleFinished;

    public bool BattleEnded => battleEnded;

    [SerializeField] private BattleRewardResolver rewardResolver;
    [SerializeField] private BattleRewardPanelUI rewardPanel;
    [SerializeField] private ExplorationResultPanelUI explorationResultPanel;

    private bool battleEnded;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetBattle()
    {
        battleEnded = false;

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

        rewardPanel.Open(rewards, () => CompleteBattleRewardFlow(onRewardFlowCompleted));
        return true;
    }

    private static void CompleteBattleRewardFlow(System.Action completedCallback)
    {
        MarkCurrentBattleNodeCleared();

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);
        cleaner?.PrepareForMapSelection();

        if (completedCallback != null)
        {
            completedCallback.Invoke();
            return;
        }

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[BattleResultChecker] BattleSceneController is missing.");
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
