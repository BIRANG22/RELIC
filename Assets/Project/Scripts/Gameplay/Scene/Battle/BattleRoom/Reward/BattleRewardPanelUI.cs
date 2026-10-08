using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Object = UnityEngine.Object;

public class BattleRewardPanelUI : MonoBehaviour
{
    private const int MaxBagItemCount = 8;

    [Header("Reward List")]
    [SerializeField] private Transform rewardRoot;
    [SerializeField] private BattleRewardSlotUI rewardSlotPrefab;
    [Tooltip("보상 슬롯을 하나씩 생성할 때의 간격(초)입니다.")]
    [Min(0f)][SerializeField] private float rewardSpawnInterval = 0.15f;
    [Header("Reward Reveal Animation")]
    [Tooltip("각 보상 슬롯이 완전히 나타날 때까지 걸리는 시간(초)입니다.")]
    [Min(0f)][SerializeField] private float rewardRevealDuration = 0.35f;
    [Tooltip("보상 슬롯 내용이 아래에서 시작하는 거리(UI 단위)입니다.")]
    [Min(0f)][SerializeField] private float rewardStartOffsetY = 40f;
    [Tooltip("아래에서 위로 등장할 때 사용할 이동 곡선입니다.")]
    [SerializeField] private AnimationCurve rewardRevealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Sprite remnantIcon;
    [SerializeField] private Color remnantIconColor = Color.white;
    [Tooltip("유물 보상에 공통으로 표시할 아이콘입니다.")]
    [SerializeField] private Sprite relicRewardIcon;
    [Tooltip("기억 보상에 공통으로 표시할 아이콘입니다.")]
    [SerializeField] private Sprite memoryRewardIcon;

    [Header("Reward Transfer Effect")]
    [Tooltip("재료 아이템과 레드 더스티움 획득 시 재생할 화면 공간 이동 효과입니다.")]
    [SerializeField] private ScreenSpaceTransferOrbEffect screenSpaceTransferEffectPrefab;
    [Tooltip("보상 이동 효과가 도착할 MenuRoot/BagButton입니다.")]
    [SerializeField] private RectTransform rewardTransferTarget;

    [Header("Gain Button")]
    [Tooltip("자동 획득 시 보상 사이의 간격(초)입니다.")]
    [Min(0f)][SerializeField] private float gainInterval = 0.2f;
    [Tooltip("한 번 누르면 남은 보상을 순서대로 모두 수령하는 버튼입니다.")]
    [SerializeField] private Button gainButton;
    [Tooltip("GainButton 하위의 Text (TMP)입니다.")]
    [SerializeField] private TMP_Text gainButtonText;

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
    private Coroutine rewardSpawnCoroutine;
    private Coroutine autoGainCoroutine;
    private bool autoGaining;
    private bool gainButtonUsed;
    private bool continueWithGainButton = true;
    private bool awaitingContinue;
    private bool continueTransitionStarted;
    private bool pointerPressedOnNextButton;
    private bool validNextButtonClick;
    private int validNextButtonClickFrame = -1;

    public Button SharedNextButton => gainButton;

    public void HideNextButtonAfterCover()
    {
        if (gainButton == null) return;
        gainButton.interactable = false;
        gainButton.gameObject.SetActive(false);
    }


    // A release outside the button never counts as a click. The flag is armed
    // only by a real pointer down/up pair, not by hover/exit or UI Submit.
    public bool ConsumeNextButtonPointerClick()
    {
        bool valid = validNextButtonClick && validNextButtonClickFrame == Time.frameCount;
        validNextButtonClick = false;
        return valid;
    }

    private void BindNextButtonPointerEvents()
    {
        if (gainButton == null) return;
        EventTrigger trigger = gainButton.GetComponent<EventTrigger>();
        if (trigger == null) trigger = gainButton.gameObject.AddComponent<EventTrigger>();
        AddPointerListener(trigger, EventTriggerType.PointerDown, data =>
        {
            var pointer = data as PointerEventData;
            pointerPressedOnNextButton = pointer != null && pointer.button == PointerEventData.InputButton.Left;
            validNextButtonClick = false;
        });
        AddPointerListener(trigger, EventTriggerType.PointerUp, data =>
        {
            var pointer = data as PointerEventData;
            validNextButtonClick = pointerPressedOnNextButton && pointer != null &&
                pointer.button == PointerEventData.InputButton.Left &&
                pointer.pointerCurrentRaycast.gameObject != null &&
                (pointer.pointerCurrentRaycast.gameObject == gainButton.gameObject ||
                 pointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(gainButton.transform));
            validNextButtonClickFrame = Time.frameCount;
            pointerPressedOnNextButton = false;
        });
        AddPointerListener(trigger, EventTriggerType.PointerExit, data =>
        {
            pointerPressedOnNextButton = false;
            validNextButtonClick = false;
        });
    }

    private static void AddPointerListener(EventTrigger trigger, EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    public void SetContinueWithGainButton(bool enabled)
    {
        continueWithGainButton = enabled;
    }

    private void Awake()
    {
        ResolveEquipPanelIfNeeded();
        if (gainButton == null)
        {
            Transform found = transform.Find("GainButton");
            if (found != null) gainButton = found.GetComponent<Button>();
        }
        if (gainButton != null)
        {
            gainButton.gameObject.SetActive(false);
            if (gainButtonText == null)
                gainButtonText = gainButton.GetComponentInChildren<TMP_Text>(true);
            BindNextButtonPointerEvents();
            gainButton.onClick.AddListener(OnClickGainButton);
        }

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady += OnLocaleTableReady;
    }

    private void OnDisable()
    {
        StopRewardSpawn();
        StopAutoGain();
        pointerPressedOnNextButton = false;
        validNextButtonClick = false;
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
        if (gainButton != null)
        {
            gainButton.interactable = false;
            gainButton.gameObject.SetActive(false);
        }
    }

    private void OnLocaleTableReady(UnityEngine.Localization.Locale _)
    {
        for (int i = 0; i < currentRewards.Count; i++)
        {
            BattleRewardData reward = currentRewards[i];
            if (ShouldPopulateRewardPresentation(reward))
                PopulateRewardPresentation(reward);
        }

        for (int i = 0; i < activeSlots.Count; i++)
            activeSlots[i]?.RefreshLocalization();
    }

    public void Open(List<BattleRewardData> rewards, Action completedCallback = null)
    {
        Open(rewards, completedCallback, null);
    }

    public void Open(List<BattleRewardData> rewards, Action completedCallback, ResumeData resumeData)
    {
        StopAutoGain();
        StopRewardSpawn();
        gainButtonUsed = false;
        awaitingContinue = false;
        continueTransitionStarted = false;
        pointerPressedOnNextButton = false;
        validNextButtonClick = false;
        if (gainButton != null) gainButton.gameObject.SetActive(true);
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

                // 이전 저장 데이터에 튜토리얼 몬스터 보상이 남아 있어도 표시하거나 획득하지 않습니다.
                if (ShouldSuppressTutorialMonsterLoot(reward))
                    continue;

                if (ShouldPopulateRewardPresentation(reward))
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
        UpdateGainButton();

        if (currentRewards.Count <= 0)
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
        StopRewardSpawn();

        if (rewardRoot == null || rewardSlotPrefab == null)
        {
            UpdateGainButton();
            return;
        }

        for (int i = rewardRoot.childCount - 1; i >= 0; i--)
            Destroy(rewardRoot.GetChild(i).gameObject);

        activeSlots.Clear();
        rewardSpawnCoroutine = StartCoroutine(SpawnRewardSlotsSequentially());
        UpdateGainButton();
    }

    private IEnumerator SpawnRewardSlotsSequentially()
    {
        // The first reward appears immediately. Subsequent rewards use the inspector interval.
        bool firstSlot = true;
        for (int i = 0; i < currentRewards.Count; i++)
        {
            BattleRewardData reward = currentRewards[i];
            if (reward == null || claimedRewards.Contains(reward))
                continue;

            if (!firstSlot && rewardSpawnInterval > 0f)
                yield return new WaitForSecondsRealtime(rewardSpawnInterval);

            if (rewardRoot == null || rewardSlotPrefab == null)
                break;

            if (claimedRewards.Contains(reward))
                continue;

            BattleRewardSlotUI slot = Instantiate(rewardSlotPrefab, rewardRoot);
            slot.Setup(reward, remnantIcon, remnantIconColor, relicRewardIcon, memoryRewardIcon, OnClickRewardSlot, null, null);
            activeSlots.Add(slot);
            // Animate the direct visual children, not the layout-controlled slot root.
            StartCoroutine(RevealRewardSlot(slot));
            UpdateGainButton();
            firstSlot = false;
        }

        rewardSpawnCoroutine = null;
    }

    private IEnumerator RevealRewardSlot(BattleRewardSlotUI slot)
    {
        if (slot == null)
            yield break;

        CanvasGroup group = slot.GetComponent<CanvasGroup>();
        if (group == null)
            group = slot.gameObject.AddComponent<CanvasGroup>();

        // Use the slot root only for alpha. VerticalLayoutGroup owns its position.
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var visuals = new List<RectTransform>();
        var originalPositions = new List<Vector2>();
        Transform root = slot.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i) is RectTransform childRect)
            {
                visuals.Add(childRect);
                originalPositions.Add(childRect.anchoredPosition);
            }
        }

        float duration = Mathf.Max(0f, rewardRevealDuration);
        float elapsed = 0f;
        while (slot != null && elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = rewardRevealCurve != null ? rewardRevealCurve.Evaluate(t) : t;
            group.alpha = t;
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i] != null)
                    visuals[i].anchoredPosition = originalPositions[i] + Vector2.down * (rewardStartOffsetY * (1f - eased));
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (slot == null)
            yield break;

        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
                visuals[i].anchoredPosition = originalPositions[i];
        }
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        UpdateGainButton();
    }

    private void StopRewardSpawn()
    {
        if (rewardSpawnCoroutine == null)
            return;

        StopCoroutine(rewardSpawnCoroutine);
        rewardSpawnCoroutine = null;
    }

    private void OnClickGainButton()
    {
        // Event rooms share this button; do not consume their click when this panel is closed.
        if (!isActiveAndEnabled)
            return;

        // Require actual press and release on the button (not pointer exit).
        if (!ConsumeNextButtonPointerClick())
            return;

        if (awaitingContinue)
        {
            if (continueTransitionStarted) return;
            continueTransitionStarted = true;
            continueWithGainButton = false;
            FinishRewardFlow();
            return;
        }

        if (autoGaining || pendingEquipmentReward || currentRewards.Count == claimedRewards.Count)
            return;

        autoGaining = true;
        gainButtonUsed = true;
        if (gainButton != null) gainButton.gameObject.SetActive(false);
        UpdateGainButton();
        autoGainCoroutine = StartCoroutine(AutoGainRewards());
    }

    private IEnumerator AutoGainRewards()
    {
        while (claimedRewards.Count < currentRewards.Count)
        {
            // Wait for a slot to finish its entrance animation or for an equip panel.
            if (pendingEquipmentReward)
            {
                yield return null;
                continue;
            }

            BattleRewardSlotUI nextSlot = null;
            for (int i = 0; i < activeSlots.Count; i++)
            {
                BattleRewardSlotUI candidate = activeSlots[i];
                if (candidate == null || candidate.Reward == null || claimedRewards.Contains(candidate.Reward))
                    continue;
                nextSlot = candidate;
                break;
            }

            if (nextSlot == null)
            {
                if (rewardSpawnCoroutine == null)
                    break; // No more slots are being created; avoid waiting forever.
                yield return null;
                continue;
            }

            CanvasGroup group = nextSlot.GetComponent<CanvasGroup>();
            if (group != null && !group.interactable)
            {
                yield return null;
                continue;
            }

            BattleRewardData reward = nextSlot.Reward;
            if (!CanClaimReward(reward))
                break; // Bag full or another requirement failed: leave this reward unclaimed.

            OnClickRewardSlot(nextSlot, true);

            // Equipment selection remains interactive. Resume when its completion callback fires.
            while (pendingEquipmentReward)
                yield return null;

            if (!claimedRewards.Contains(reward))
                break; // The acquisition was blocked; do not retry indefinitely.

            if (claimedRewards.Count < currentRewards.Count && gainInterval > 0f)
                yield return new WaitForSecondsRealtime(gainInterval);
        }

        autoGainCoroutine = null;
        autoGaining = false;
        UpdateGainButton();
    }

    private void StopAutoGain()
    {
        if (autoGainCoroutine != null)
        {
            StopCoroutine(autoGainCoroutine);
            autoGainCoroutine = null;
        }
        autoGaining = false;
    }

    private void UpdateGainButton()
    {
        if (gainButton == null)
            return;

        int remaining = Mathf.Max(0, currentRewards.Count - claimedRewards.Count);
        if (awaitingContinue)
        {
            gainButton.gameObject.SetActive(true);
            if (gainButtonText != null) gainButtonText.text = "진행";
            gainButton.interactable = true;
            return;
        }
        // Automatic collection uses a one-shot button; never reveal it again during collection.
        if (gainButtonUsed || autoGaining || remaining <= 0)
        {
            gainButton.gameObject.SetActive(false);
            gainButton.interactable = false;
            return;
        }

        gainButton.gameObject.SetActive(true);
        if (gainButtonText != null)
            gainButtonText.text = $"획득 {remaining}";

        bool hasReadySlot = false;
        if (!pendingEquipmentReward)
        {
            for (int i = 0; i < activeSlots.Count; i++)
            {
                BattleRewardSlotUI slot = activeSlots[i];
                if (slot == null || slot.Reward == null || claimedRewards.Contains(slot.Reward))
                    continue;
                CanvasGroup group = slot.GetComponent<CanvasGroup>();
                hasReadySlot = group != null && group.interactable;
                break;
            }
        }
        gainButton.interactable = !gainButtonUsed && !autoGaining && remaining > 0 && hasReadySlot;
    }

    private void OnClickRewardSlot(BattleRewardSlotUI slot)
    {
        OnClickRewardSlot(slot, false);
    }

    private void OnClickRewardSlot(BattleRewardSlotUI slot, bool fromAutoGain)
    {
        if (autoGaining && !fromAutoGain)
            return;
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

        if (TryStartRewardTransfer(slot, reward))
            return;

        CompleteRewardSlot(slot, reward);
    }

    private bool TryStartRewardTransfer(BattleRewardSlotUI slot, BattleRewardData reward)
    {
        if (!ShouldPlayTransferEffect(reward) ||
            slot == null ||
            slot.IconRectTransform == null ||
            rewardTransferTarget == null ||
            screenSpaceTransferEffectPrefab == null)
        {
            return false;
        }

        Camera sourceCamera = ScreenSpaceTransferOrbEffect.ResolveUiCamera(
            slot.IconRectTransform,
            Camera.main);
        Camera targetCamera = ScreenSpaceTransferOrbEffect.ResolveUiCamera(
            rewardTransferTarget,
            Camera.main);
        Vector2 startScreenPosition = ScreenSpaceTransferOrbEffect.GetRectScreenCenter(
            slot.IconRectTransform,
            sourceCamera);
        Vector2 endScreenPosition = ScreenSpaceTransferOrbEffect.GetRectScreenCenter(
            rewardTransferTarget,
            targetCamera);
        Color iconColor = slot.CurrentIconColor;

        slot.SetClaimed();

        // 획득 연출은 보상 처리와 완전히 분리합니다.
        // 연출이 재생되는 동안에도 다른 보상을 즉시 선택하거나 다음 행동을 진행할 수 있습니다.
        ScreenSpaceTransferOrbEffect effect = Instantiate(screenSpaceTransferEffectPrefab);
        effect.PlayDetached(startScreenPosition, endScreenPosition, iconColor);

        CompleteRewardSlot(slot, reward);
        return true;
    }

    private static bool ShouldPlayTransferEffect(BattleRewardData reward)
    {
        return reward != null &&
               (reward.Type == BattleRewardType.Item ||
                reward.Type == BattleRewardType.Remnant);
    }

    private bool OpenEquipmentRewardPanel(BattleRewardSlotUI slot, BattleRewardData reward)
    {
        if (slot != null && reward != null && reward.Type == BattleRewardType.Skill)
        {
            BattleRewardSkillPanelUI skillPanel = BattleRewardSkillPanelUI.FindPanel();
            if (skillPanel != null)
            {
                pendingEquipmentReward = true;
                UpdateGainButton();
                if (skillPanel.Open(reward, () => OnEquipmentRewardResolved(slot, reward)))
                {
                    PlayRewardAcquireSfx(reward);
                    return true;
                }
                pendingEquipmentReward = false;
                UpdateGainButton();
            }
        }
        ResolveEquipPanelIfNeeded();

        if (slot == null || reward == null)
            return false;
        if (equipPanel == null && (reward.Type != BattleRewardType.Relic ||
            Object.FindFirstObjectByType<BattleRewardRelicPanelUI>(FindObjectsInactive.Include) == null))
            return false;

        pendingEquipmentReward = true;
        UpdateGainButton();
        PlayRewardAcquireSfx(reward);
        if (reward.Type == BattleRewardType.Relic)
        {
            BattleRewardRelicPanelUI relicPanel = Object.FindFirstObjectByType<BattleRewardRelicPanelUI>(FindObjectsInactive.Include);
            if (relicPanel != null)
            {
                relicPanel.Open(reward, () => OnEquipmentRewardResolved(slot, reward));
                return true;
            }
        }
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

        UpdateGainButton();

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
        reward.RewardId = id;
        switch (reward.Type)
        {
            case BattleRewardType.Item:
                ItemData item = DataManager.Instance.ItemDatabase?.Get(id);
                if (item != null)
                {
                    reward.Name = GameDataLocalization.ItemName(item);
                    reward.Description = GameDataLocalization.ItemDescription(item);
                }
                if (DataManager.Instance.ItemIconDatabase != null &&
                    DataManager.Instance.ItemIconDatabase.TryGetIcon(id, out Sprite itemIcon) &&
                    itemIcon != null)
                {
                    reward.Icon = itemIcon;
                }
                break;

            case BattleRewardType.Relic:
                if (DataManager.Instance.RelicDatabase != null && DataManager.Instance.RelicDatabase.TryGet(id, out RelicData relic))
                {
                    reward.Name = GameDataLocalization.RelicName(relic);
                    reward.Description = GameDataLocalization.RelicEffectDescription(relic);
                }
                if (DataManager.Instance.RelicIconDatabase != null &&
                    DataManager.Instance.RelicIconDatabase.TryGetIcon(id, out Sprite relicIcon) &&
                    relicIcon != null)
                {
                    reward.Icon = relicIcon;
                }
                break;

            case BattleRewardType.Skill:
                if (DataManager.Instance.SkillDatabase != null && DataManager.Instance.SkillDatabase.TryGet(id, out SkillMasterData skill))
                {
                    reward.Name = GameDataLocalization.SkillName(skill);
                    reward.Description = GameDataLocalization.SkillDetails(skill);

                    if (skill.Icon != null)
                        reward.Icon = skill.Icon;
                }
                if (DataManager.Instance.SkillIconDatabase != null &&
                    DataManager.Instance.SkillIconDatabase.TryGetIcon(id, out Sprite skillIcon) &&
                    skillIcon != null)
                {
                    reward.Icon = skillIcon;
                }
                break;
        }
    }

    private static bool ShouldPopulateRewardPresentation(BattleRewardData reward)
    {
        if (reward == null || string.IsNullOrWhiteSpace(reward.RewardId))
            return false;

        return reward.Type == BattleRewardType.Item ||
               reward.Type == BattleRewardType.Relic ||
               reward.Type == BattleRewardType.Skill;
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

        if (ShouldSuppressTutorialMonsterLoot(reward))
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

    private static bool ShouldSuppressTutorialMonsterLoot(BattleRewardData reward)
    {
        if (reward == null ||
            (reward.Type != BattleRewardType.Remnant && reward.Type != BattleRewardType.Item))
        {
            return false;
        }

        BattleRuntimeData battle = DataManager.Instance?.BattleRuntimeStore?.Get();
        return battle?.IsTutorialBattle == true;
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
                "battle.bag_full_unique_item_limit"),
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
        if (continueWithGainButton && !awaitingContinue)
        {
            StopAutoGain();
            awaitingContinue = true;
            UpdateGainButton();
            return;
        }

        StopAutoGain();
        // Keep the visible "진행" button on screen until the destination transition
        // handles panel teardown. Lock further clicks without hiding its visuals.
        if (gainButton != null)
        {
            gainButton.interactable = false;
            if (!continueTransitionStarted)
                gainButton.gameObject.SetActive(false);
        }
        if (!continueTransitionStarted)
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

        // Respect the VerticalLayoutGroup values configured on Contant in the Inspector.
        // No automatic ContentSizeFitter: the designer owns Contant sizing/anchors.
    }
}
