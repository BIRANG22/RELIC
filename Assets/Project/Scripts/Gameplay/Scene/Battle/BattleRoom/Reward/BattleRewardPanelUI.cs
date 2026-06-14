using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardPanelUI : MonoBehaviour
{
    private const int MaxBagItemCount = 8;

    [Header("Reward List")]
    [SerializeField] private Transform rewardRoot;
    [SerializeField] private BattleRewardSlotUI rewardSlotPrefab;
    [SerializeField] private Sprite remnantIcon;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailValueText;

    [Header("Legacy Confirm Button")]
    [SerializeField] private Button confirmButton;

    [Header("After Reward")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject mapPanel;

    private readonly List<BattleRewardData> currentRewards = new();
    private readonly List<BattleRewardData> claimedRewards = new();
    private readonly List<BattleRewardSlotUI> activeSlots = new();

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        HideDetailPanel();
        gameObject.SetActive(false);
    }

    public void Open(List<BattleRewardData> rewards)
    {
        currentRewards.Clear();
        claimedRewards.Clear();
        activeSlots.Clear();

        if (rewards != null)
            currentRewards.AddRange(rewards);

        Debug.Log($"[BattleRewardPanelUI] Open / RewardCount:{currentRewards.Count}");

        gameObject.SetActive(true);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        EnsureVerticalRewardLayout();
        Refresh();

        if (activeSlots.Count > 0)
            ShowRewardDetail(activeSlots[0].Reward);
        else
            FinishRewardFlow();
    }

    private void Refresh()
    {
        if (rewardRoot == null || rewardSlotPrefab == null)
            return;

        for (int i = rewardRoot.childCount - 1; i >= 0; i--)
            Destroy(rewardRoot.GetChild(i).gameObject);

        activeSlots.Clear();

        for (int i = 0; i < currentRewards.Count; i++)
        {
            BattleRewardData reward = currentRewards[i];

            if (reward == null || claimedRewards.Contains(reward))
                continue;

            BattleRewardSlotUI slot = Instantiate(rewardSlotPrefab, rewardRoot);
            slot.Setup(reward, remnantIcon, OnClickRewardSlot, OnFocusRewardSlot);
            activeSlots.Add(slot);
        }
    }

    private void OnFocusRewardSlot(BattleRewardSlotUI slot)
    {
        if (slot == null)
            return;

        ShowRewardDetail(slot.Reward);
    }

    private void OnClickRewardSlot(BattleRewardSlotUI slot)
    {
        if (slot == null || slot.Reward == null)
            return;

        BattleRewardData reward = slot.Reward;

        ShowRewardDetail(reward);

        if (!CanClaimReward(reward))
            return;

        ApplyReward(reward);
        claimedRewards.Add(reward);
        activeSlots.Remove(slot);

        Destroy(slot.gameObject);

        if (claimedRewards.Count >= currentRewards.Count)
        {
            FinishRewardFlow();
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rewardRoot as RectTransform);

        if (activeSlots.Count > 0)
            ShowRewardDetail(activeSlots[0].Reward);
        else
            HideDetailPanel();
    }

    private void ApplyReward(BattleRewardData reward)
    {
        if (reward == null || DataManager.Instance == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        runtime.BagItemIds ??= new List<string>();
        runtime.OwnedRelicIds ??= new List<string>();

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
                if (!string.IsNullOrWhiteSpace(reward.RewardId) && !HasRelic(runtime, reward.RewardId))
                {
                    runtime.OwnedRelicIds.Add(reward.RewardId.Trim());
                    NormalizeOwnedRelics(runtime);
                    RelicEquipPanelUI.RefreshAll();
                }
                break;
        }

        DataManager.Instance.BattleRuntimeStore.Set(runtime);
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

        ShowWarning($"가방이 가득 찼습니다. 고유아이템은 최대 {MaxBagItemCount}개까지 보유할 수 있습니다.");
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

    private void ShowWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        BattleWarningUI.ShowMessage(message);
    }

    private bool HasRelic(BattleRuntimeData runtime, string relicId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(relicId))
            return false;

        string targetId = relicId.Trim();

        if (runtime.OwnedRelicIds != null)
        {
            for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
            {
                if (string.Equals(runtime.OwnedRelicIds[i]?.Trim(), targetId, System.StringComparison.Ordinal))
                    return true;
            }
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance?.CharacterRuntimeStore?.GetAll();

        if (characters == null)
            return false;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
        {
            CharacterRuntimeData character = pair.Value;

            if (character?.EquippedRelicIds == null)
                continue;

            for (int i = 0; i < character.EquippedRelicIds.Length; i++)
            {
                if (string.Equals(character.EquippedRelicIds[i]?.Trim(), targetId, System.StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private void NormalizeOwnedRelics(BattleRuntimeData runtime)
    {
        if (runtime == null || runtime.OwnedRelicIds == null)
            return;

        HashSet<string> uniqueIds = new();

        for (int i = runtime.OwnedRelicIds.Count - 1; i >= 0; i--)
        {
            string relicId = runtime.OwnedRelicIds[i];

            if (string.IsNullOrWhiteSpace(relicId))
            {
                runtime.OwnedRelicIds.RemoveAt(i);
                continue;
            }

            relicId = relicId.Trim();

            if (!uniqueIds.Add(relicId))
            {
                runtime.OwnedRelicIds.RemoveAt(i);
                continue;
            }

            runtime.OwnedRelicIds[i] = relicId;
        }
    }

    private void FinishRewardFlow()
    {
        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);

        if (cleaner != null)
            cleaner.Clean();

        HideDetailPanel();
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

    private void ShowRewardDetail(BattleRewardData reward)
    {
        if (detailPanel != null)
            detailPanel.SetActive(reward != null);

        if (reward == null)
        {
            HideDetailPanel();
            return;
        }

        Sprite icon = reward.Icon;

        if (reward.Type == BattleRewardType.Remnant && icon == null)
            icon = remnantIcon;

        if (detailIconImage != null)
        {
            detailIconImage.sprite = icon;
            detailIconImage.enabled = icon != null;
        }

        if (detailNameText != null)
            detailNameText.text = reward.GetDisplayName();

        if (detailDescriptionText != null)
        {
            if (reward.Type == BattleRewardType.Remnant)
                detailDescriptionText.text = reward.GetRemnantAmountDescription();
            else
                detailDescriptionText.text = string.IsNullOrWhiteSpace(reward.Description) ? GetDefaultDescription(reward) : reward.Description;
        }

        if (detailValueText != null)
        {
            if (reward.Type == BattleRewardType.Item)
                detailValueText.text = $"가치 {reward.Value}";
            else
                detailValueText.text = "";
        }
    }

    private void HideDetailPanel()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    private string GetDefaultDescription(BattleRewardData reward)
    {
        if (reward == null)
            return "";

        switch (reward.Type)
        {
            case BattleRewardType.Remnant:
                return reward.GetRemnantAmountDescription();
            case BattleRewardType.Item:
                return "획득 가능한 아이템입니다.";
            case BattleRewardType.Relic:
                return "획득 가능한 유물입니다.";
            default:
                return "";
        }
    }

    private void EnsureVerticalRewardLayout()
    {
        if (rewardRoot == null)
            return;

        VerticalLayoutGroup verticalLayout = rewardRoot.GetComponent<VerticalLayoutGroup>();

        if (verticalLayout == null)
            verticalLayout = rewardRoot.gameObject.AddComponent<VerticalLayoutGroup>();

        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = rewardRoot.GetComponent<ContentSizeFitter>();

        if (fitter == null)
            fitter = rewardRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
}
