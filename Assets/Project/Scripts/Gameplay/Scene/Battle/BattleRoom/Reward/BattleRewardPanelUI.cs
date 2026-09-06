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

    [Header("Reward Equip Panel")]
    [SerializeField] private BattleRewardEquipPanelUI equipPanel;

    [Header("After Reward")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject mapPanel;

    private readonly List<BattleRewardData> currentRewards = new();
    private readonly List<BattleRewardData> claimedRewards = new();
    private readonly List<BattleRewardSlotUI> activeSlots = new();
    private Action onRewardFlowCompleted;
    private bool pendingEquipmentReward;

    private void Awake()
    {
        ResolveEquipPanelIfNeeded();

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void Open(List<BattleRewardData> rewards, Action completedCallback = null)
    {
        Open(rewards, completedCallback, null);
    }

    public void Open(List<BattleRewardData> rewards, Action completedCallback, ResumeData resumeData)
    {
        currentRewards.Clear();
        claimedRewards.Clear();
        activeSlots.Clear();
        onRewardFlowCompleted = completedCallback;
        pendingEquipmentReward = false;
        ResolveEquipPanelIfNeeded();

        if (rewards != null)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                BattleRewardData reward = rewards[i];
                if (reward == null)
                    continue;

                if (string.IsNullOrWhiteSpace(reward.Name) || reward.Icon == null)
                    PopulateRewardPresentation(reward);
                currentRewards.Add(reward);
            }
        }

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

    public void OpenSavedRewards(ResumeData resumeData, Action completedCallback = null)
    {
        if (resumeData == null)
            return;

        // BattleReward checkpoint는 항상 수령 전 snapshot이다.
        Open(ToRuntimeRewards(resumeData.PendingRewards), completedCallback, null);
    }

    public void PrepareForResumePresentation()
    {
        ResolveEquipPanelIfNeeded();
        pendingEquipmentReward = false;
        if (equipPanel != null)
            equipPanel.gameObject.SetActive(false);
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

        if (pendingEquipmentReward)
            return;

        if (reward.Type == BattleRewardType.Relic || reward.Type == BattleRewardType.Skill)
        {
            if (!OpenEquipmentRewardPanel(slot, reward))
                Debug.LogWarning($"[BattleRewardPanelUI] Equip_panel을 찾을 수 없어 보상 처리를 보류합니다. Type:{reward.Type} / Id:{reward.RewardId}");

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

    private List<BattleRewardData> ToRuntimeRewards(IReadOnlyList<BattleRewardSaveData> saved)
    {
        var rewards = new List<BattleRewardData>();
        if (saved == null)
            return rewards;

        for (int i = 0; i < saved.Count; i++)
        {
            BattleRewardSaveData reward = saved[i];
            if (reward != null)
            {
                var runtimeReward = new BattleRewardData
                {
                    Type = reward.Type,
                    RewardId = reward.RewardId,
                    Amount = reward.Amount
                };
                PopulateRewardPresentation(runtimeReward);
                rewards.Add(runtimeReward);
            }
        }
        return rewards;
    }

    private static void PopulateRewardPresentation(BattleRewardData reward)
    {
        if (reward == null || DataManager.Instance == null || string.IsNullOrWhiteSpace(reward.RewardId))
            return;

        string id = reward.RewardId.Trim();
        switch (reward.Type)
        {
            case BattleRewardType.Item:
                ItemData item = DataManager.Instance.ItemDatabase?.Get(id);
                if (item != null)
                {
                    reward.Name = GameDataLocalization.ItemName(item);
                    reward.Description = GameDataLocalization.ItemDescription(item);
                }
                DataManager.Instance.ItemIconDatabase?.TryGetIcon(id, out reward.Icon);
                break;

            case BattleRewardType.Relic:
                if (DataManager.Instance.RelicDatabase != null && DataManager.Instance.RelicDatabase.TryGet(id, out RelicData relic))
                {
                    reward.Name = GameDataLocalization.RelicName(relic);
                    reward.Description = GameDataLocalization.RelicEffectDescription(relic);
                }
                DataManager.Instance.RelicIconDatabase?.TryGetIcon(id, out reward.Icon);
                break;

            case BattleRewardType.Skill:
                if (DataManager.Instance.SkillDatabase != null && DataManager.Instance.SkillDatabase.TryGet(id, out SkillMasterData skill))
                {
                    reward.Name = GameDataLocalization.SkillName(skill);
                    reward.Description = GameDataLocalization.SkillDetails(skill);
                }
                DataManager.Instance.SkillIconDatabase?.TryGetIcon(id, out reward.Icon);
                break;
        }
    }

    private void ResolveEquipPanelIfNeeded()
    {
        if (equipPanel != null)
            return;

        equipPanel = Object.FindFirstObjectByType<BattleRewardEquipPanelUI>(FindObjectsInactive.Include);
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
                if (!string.IsNullOrWhiteSpace(reward.RewardId))
                {
                    string itemId = reward.RewardId.Trim();

                    if (BagItemStackUtility.CanAddItem(runtime.BagItemIds, itemId, MaxBagItemCount))
                    {
                        int amount = Mathf.Max(1, reward.Amount);

                        for (int i = 0; i < amount; i++)
                            runtime.BagItemIds.Add(itemId);

                        RecordDiscoveryService.RegisterItem(DataManager.Instance, itemId);
                        BattleBagPanelUI.RefreshAll();
                    }
                }
                break;

            case BattleRewardType.Relic:
            case BattleRewardType.Skill:
                // 기억/유물은 Equip_panel에서 직접 처리하며 인벤토리에 저장하지 않습니다.
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
                AudioManager.Instance.PlaySfx(AudioIds.Sfx.BattleRewardRemnantAcquire);
                break;

            case BattleRewardType.Item:
            case BattleRewardType.Relic:
            case BattleRewardType.Skill:
                AudioManager.Instance.PlaySfx(AudioIds.Sfx.BattleRewardRelicSkillAcquire);
                break;
        }
    }

    private bool CanClaimReward(BattleRewardData reward)
    {
        if (reward == null)
            return false;

        if (reward.Type != BattleRewardType.Item)
            return true;

        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return false;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        runtime.BagItemIds ??= new List<string>();

        if (BagItemStackUtility.CanAddItem(runtime.BagItemIds, reward.RewardId, MaxBagItemCount))
            return true;

        ShowWarning(string.Format(
            GameLocalization.Get(
                "battle.bag_full_unique_item_limit",
                "가방이 가득 찼습니다. 서로 다른 아이템은 최대 {0}종류까지 보유할 수 있습니다."),
            MaxBagItemCount));
        return false;
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
        gameObject.SetActive(false);

        Action completedCallback = onRewardFlowCompleted;
        onRewardFlowCompleted = null;
        completedCallback?.Invoke();
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
