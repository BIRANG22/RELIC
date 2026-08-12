using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class EventRoomRewardPanelUI : MonoBehaviour
{
    private const int MaxBagItemCount = 8;

    [Header("Reward List")]
    [SerializeField] private Transform rewardRoot;
    [SerializeField] private BattleRewardSlotUI rewardSlotPrefab;
    [SerializeField] private Sprite remnantIcon;
    [SerializeField] private Color remnantIconColor = Color.white;

    [Header("Legacy Confirm Button")]
    [SerializeField] private Button confirmButton;

    [Header("Reward Equip Panel")]
    [SerializeField] private BattleRewardEquipPanelUI equipPanel;

    private readonly List<BattleRewardData> currentRewards = new();
    private readonly List<BattleRewardData> claimedRewards = new();
    private readonly List<BattleRewardSlotUI> activeSlots = new();
    private Action onRewardFlowCompleted;
    private bool pendingEquipmentReward;
    private bool isOpening;

    private void Awake()
    {
        ResolveEquipPanelIfNeeded();
        HideLegacyConfirmButton();

        if (!isOpening)
            gameObject.SetActive(false);
    }

    public void Open(List<BattleRewardData> rewards, Action completedCallback = null)
    {
        currentRewards.Clear();
        claimedRewards.Clear();
        activeSlots.Clear();
        onRewardFlowCompleted = completedCallback;
        pendingEquipmentReward = false;
        ResolveEquipPanelIfNeeded();

        if (rewards != null)
            currentRewards.AddRange(rewards);

        isOpening = true;
        gameObject.SetActive(true);
        isOpening = false;
        transform.SetAsLastSibling();
        Refresh();
        RebuildRewardLayout();

        if (activeSlots.Count <= 0)
            FinishRewardFlow();
    }

    private void Refresh()
    {
        if (rewardRoot == null || rewardSlotPrefab == null)
        {
            Debug.LogWarning("[EventRoomRewardPanelUI] Reward UI references are missing.", this);
            return;
        }

        for (int i = rewardRoot.childCount - 1; i >= 0; i--)
            Destroy(rewardRoot.GetChild(i).gameObject);

        activeSlots.Clear();

        for (int i = 0; i < currentRewards.Count; i++)
        {
            BattleRewardData reward = currentRewards[i];

            if (reward == null || claimedRewards.Contains(reward))
                continue;

            BattleRewardSlotUI slot = Instantiate(rewardSlotPrefab, rewardRoot);
            slot.Setup(reward, remnantIcon, remnantIconColor, OnClickRewardSlot, null, null);
            activeSlots.Add(slot);
        }
    }

    private void OnClickRewardSlot(BattleRewardSlotUI slot)
    {
        if (slot == null || slot.Reward == null)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        BattleRewardData reward = slot.Reward;

        if (!CanClaimReward(reward) || pendingEquipmentReward)
            return;

        if (reward.Type == BattleRewardType.Relic || reward.Type == BattleRewardType.Skill)
        {
            if (!OpenEquipmentRewardPanel(slot, reward))
            {
                Debug.LogWarning(
                    $"[EventRoomRewardPanelUI] Equip_panel not found. Type:{reward.Type} / Id:{reward.RewardId}",
                    this);
            }

            return;
        }

        ApplyReward(reward);
        PlayRewardAcquireSfx(reward);
        CompleteRewardSlot(slot, reward);
    }

    private bool OpenEquipmentRewardPanel(BattleRewardSlotUI slot, BattleRewardData reward)
    {
        ResolveEquipPanelIfNeeded();

        if (equipPanel == null || slot == null || reward == null)
            return false;

        pendingEquipmentReward = true;
        PlayRewardAcquireSfx(reward);
        equipPanel.Open(reward, () => OnEquipmentRewardResolved(slot, reward));
        return true;
    }

    private void OnEquipmentRewardResolved(BattleRewardSlotUI slot, BattleRewardData reward)
    {
        pendingEquipmentReward = false;
        CompleteRewardSlot(slot, reward);
    }

    private void CompleteRewardSlot(BattleRewardSlotUI slot, BattleRewardData reward)
    {
        if (reward != null && !claimedRewards.Contains(reward))
            claimedRewards.Add(reward);

        if (slot != null)
        {
            activeSlots.Remove(slot);
            Destroy(slot.gameObject);
        }

        if (claimedRewards.Count >= currentRewards.Count)
        {
            FinishRewardFlow();
            return;
        }

        if (rewardRoot is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void ApplyReward(BattleRewardData reward)
    {
        if (reward == null || DataManager.Instance == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        runtime.BagItemIds ??= new List<string>();
        runtime.OwnedRelicIds ??= new List<string>();
        runtime.SkillInventoryIds ??= new List<string>();

        switch (reward.Type)
        {
            case BattleRewardType.Remnant:
                runtime.Remnant += reward.Amount;
                DataManager.Instance.BattleRuntimeStore.Set(runtime);
                BattleGoldHudUI.RefreshAll();
                break;

            case BattleRewardType.Item:
                if (!string.IsNullOrWhiteSpace(reward.RewardId) && runtime.BagItemIds.Count < MaxBagItemCount)
                {
                    runtime.BagItemIds.Add(reward.RewardId.Trim());
                    BattleBagPanelUI.RefreshAll();
                }
                break;

            case BattleRewardType.Relic:
            case BattleRewardType.Skill:
                break;
        }

        DataManager.Instance.BattleRuntimeStore.Set(runtime);
    }

    private void PlayRewardAcquireSfx(BattleRewardData reward)
    {
        if (reward == null || AudioManager.Instance == null)
            return;

        switch (reward.Type)
        {
            case BattleRewardType.Remnant:
                AudioManager.Instance.PlaySfx(SfxType.BattleRewardRemnantAcquire);
                break;

            case BattleRewardType.Item:
            case BattleRewardType.Relic:
            case BattleRewardType.Skill:
                AudioManager.Instance.PlaySfx(SfxType.BattleRewardRelicSkillAcquire);
                break;
        }
    }

    private bool CanClaimReward(BattleRewardData reward)
    {
        if (reward == null)
            return false;

        if (reward.Type != BattleRewardType.Item)
            return true;

        int currentCount = GetCurrentBagItemCount();

        if (currentCount < MaxBagItemCount)
            return true;

        BattleWarningUI.ShowMessage(string.Format(
            GameLocalization.Get(
                "battle.bag_full_unique_item_limit",
                "가방이 가득 찼습니다. 고유아이템은 최대 {0}개까지 보유할 수 있습니다."),
            MaxBagItemCount));
        return false;
    }

    private int GetCurrentBagItemCount()
    {
        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return 0;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        runtime.BagItemIds ??= new List<string>();
        return runtime.BagItemIds.Count;
    }

    private void FinishRewardFlow()
    {
        gameObject.SetActive(false);

        Action completedCallback = onRewardFlowCompleted;
        onRewardFlowCompleted = null;
        completedCallback?.Invoke();
    }

    private void ResolveEquipPanelIfNeeded()
    {
        if (equipPanel != null)
            return;

        equipPanel = Object.FindFirstObjectByType<BattleRewardEquipPanelUI>(FindObjectsInactive.Include);
    }

    private void HideLegacyConfirmButton()
    {
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
    }

    private void RebuildRewardLayout()
    {
        if (rewardRoot is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
}
