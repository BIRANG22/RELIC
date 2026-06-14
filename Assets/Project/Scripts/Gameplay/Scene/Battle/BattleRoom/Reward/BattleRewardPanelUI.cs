using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleRewardPanelUI : MonoBehaviour
{
    [Header("Reward UI")]
    [SerializeField] private Transform rewardRoot;
    [SerializeField] private BattleRewardSlotUI rewardSlotPrefab;
    [SerializeField] private Sprite remnantIcon;

    [Header("After Confirm")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject mapPanel;

    private List<BattleRewardData> currentRewards = new();

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Open(List<BattleRewardData> rewards)
    {
        currentRewards = rewards ?? new List<BattleRewardData>();

        Debug.Log($"[BattleRewardPanelUI] Open / RewardCount:{currentRewards.Count}");

        gameObject.SetActive(true);
        Refresh();
    }

    private void Refresh()
    {
        if (rewardRoot == null || rewardSlotPrefab == null)
            return;

        for (int i = rewardRoot.childCount - 1; i >= 0; i--)
            Destroy(rewardRoot.GetChild(i).gameObject);

        for (int i = 0; i < currentRewards.Count; i++)
        {
            BattleRewardSlotUI slot = Instantiate(rewardSlotPrefab, rewardRoot);
            slot.Setup(currentRewards[i], remnantIcon);
        }
    }

    public void OnClickConfirm()
    {
        ApplyRewards();

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);

        if (cleaner != null)
            cleaner.Clean();

        gameObject.SetActive(false);

        if (battlePanel != null)
            battlePanel.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(true);

        MapViewSpawner mapViewSpawner =
            Object.FindFirstObjectByType<MapViewSpawner>(FindObjectsInactive.Include);

        if (mapViewSpawner != null)
            mapViewSpawner.Refresh();
    }

    private void ApplyRewards()
    {
        if (DataManager.Instance == null)
            return;

        BattleRuntimeData runtime =
            DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        runtime.BagItemIds ??= new List<string>();
        runtime.OwnedRelicIds ??= new List<string>();

        for (int i = 0; i < currentRewards.Count; i++)
        {
            BattleRewardData reward = currentRewards[i];

            if (reward == null)
                continue;

            switch (reward.Type)
            {
                case BattleRewardType.Remnant:
                    runtime.Remnant += reward.Amount;
                    break;

                case BattleRewardType.Item:
                    runtime.BagItemIds.Add(reward.RewardId);
                    break;

                case BattleRewardType.Relic:
                    runtime.OwnedRelicIds.Add(reward.RewardId);
                    break;
            }
        }

        DataManager.Instance.BattleRuntimeStore.Set(runtime);
    }
}