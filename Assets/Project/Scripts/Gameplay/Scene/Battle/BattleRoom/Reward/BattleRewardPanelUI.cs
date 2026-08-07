using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class BattleRewardPanelUI : MonoBehaviour
{
    private const int MaxBagItemCount = 8;

    [Header("Reward List")]
    [SerializeField] private Transform rewardRoot;
    [SerializeField] private BattleRewardSlotUI rewardSlotPrefab;
    [SerializeField] private Sprite remnantIcon;
    [SerializeField] private Color remnantIconColor = Color.white;

    [Header("Legacy Confirm Button")]
    [SerializeField] private Button confirmButton;

    [Header("After Reward")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject mapPanel;

    private readonly List<BattleRewardData> currentRewards = new();
    private readonly List<BattleRewardData> claimedRewards = new();
    private readonly List<BattleRewardSlotUI> activeSlots = new();
    private Action onRewardFlowCompleted;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void Open(List<BattleRewardData> rewards, Action completedCallback = null)
    {
        currentRewards.Clear();
        claimedRewards.Clear();
        activeSlots.Clear();
        onRewardFlowCompleted = completedCallback;

        if (rewards != null)
            currentRewards.AddRange(rewards);

        Debug.Log($"[BattleRewardPanelUI] Open / RewardCount:{currentRewards.Count}");

        gameObject.SetActive(true);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        EnsureVerticalRewardLayout();
        Refresh();

        if (activeSlots.Count <= 0)
        {
            FinishRewardFlow();
            return;
        }

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

        if (!CanClaimReward(reward))
            return;

        ApplyReward(reward);
        PlayRewardAcquireSfx(reward);
        claimedRewards.Add(reward);
        activeSlots.Remove(slot);

        Destroy(slot.gameObject);

        if (claimedRewards.Count >= currentRewards.Count)
        {
            FinishRewardFlow();
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rewardRoot as RectTransform);

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
                if (!string.IsNullOrWhiteSpace(reward.RewardId) && !HasRelic(runtime, reward.RewardId))
                {
                    runtime.OwnedRelicIds.Add(reward.RewardId.Trim());
                    NormalizeOwnedRelics(runtime);
                    RelicEquipPanelUI.RefreshAll();
                }
                break;

            case BattleRewardType.Skill:
                if (!string.IsNullOrWhiteSpace(reward.RewardId) && !HasSkill(runtime, reward.RewardId))
                {
                    string skillId = reward.RewardId.Trim();
                    runtime.SkillInventoryIds.Add(skillId);
                    runtime.AcquiredSkillIds ??= new List<string>();
                    if (!runtime.AcquiredSkillIds.Contains(skillId))
                        runtime.AcquiredSkillIds.Add(skillId);
                    SkillInventoryNotificationUI.ShowNewSkillNotice();
                    SkillInventoryPanelUI.RefreshAll();
                }
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

    private bool HasSkill(BattleRuntimeData runtime, string skillId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(skillId))
            return false;

        string targetId = skillId.Trim();

        if (runtime.SkillInventoryIds != null)
        {
            for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
            {
                if (IsSameSkillOrPairedVariant(runtime.SkillInventoryIds[i], targetId))
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

            if (character?.EquippedSkillIds == null)
                continue;

            for (int i = 0; i < character.EquippedSkillIds.Length; i++)
            {
                if (IsSameSkillOrPairedVariant(character.EquippedSkillIds[i], targetId))
                    return true;
            }
        }

        return false;
    }

    private bool IsSameSkillOrPairedVariant(string ownedSkillId, string targetSkillId)
    {
        if (string.IsNullOrWhiteSpace(ownedSkillId) || string.IsNullOrWhiteSpace(targetSkillId))
            return false;

        string normalizedOwnedSkillId = ownedSkillId.Trim();
        string normalizedTargetSkillId = targetSkillId.Trim();

        if (string.Equals(normalizedOwnedSkillId, normalizedTargetSkillId, System.StringComparison.Ordinal))
            return true;

        return SkillRarityUtility.TryGetPairedVariantId(normalizedOwnedSkillId, out string pairedSkillId) &&
               string.Equals(pairedSkillId, normalizedTargetSkillId, System.StringComparison.Ordinal);
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
        MarkCurrentBattleNodeCleared();

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();

        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);

        if (cleaner != null)
            cleaner.PrepareForMapSelection();

        gameObject.SetActive(false);

        Action completedCallback = onRewardFlowCompleted;
        onRewardFlowCompleted = null;
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
            Debug.LogWarning("[BattleRewardPanelUI] BattleSceneController is missing.");
    }

    private void MarkCurrentBattleNodeCleared()
    {
        if (DataManager.Instance == null || DataManager.Instance.MapRuntimeStore == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();
        if (!MapRuntimeProgressUtility.MarkCurrentNodeCleared(runtime))
            return;

        DataManager.Instance.MapRuntimeStore.Set(runtime);
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
