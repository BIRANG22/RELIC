using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

public class EventRoomController : MonoBehaviour
{
    [Header("Chest")]
    [SerializeField] private ChestOpenButton chestOpenButton;

    [Header("Progression")]
    [SerializeField] private GameObject nextButtonRoot;

    [Header("Event Data")]
    [SerializeField] private GameObject dataEventRoot;
    [SerializeField] private TMP_Text eventNameText;
    [SerializeField] private TMP_Text eventTitleText;
    [SerializeField] private TMP_Text eventResultText;
    [SerializeField] private EventChoiceSlotUI[] choiceSlots;

    [Header("Event Rewards")]
    [SerializeField] private EventRoomRewardPanelUI rewardPanel;

    [Header("Hover Info Panel")]
    [SerializeField] private GameObject relicHoverInfoPanel;
    [SerializeField] private TMP_Text relicHoverNameText;
    [SerializeField] private TMP_Text relicHoverDescText;

    [Header("Relic Acquire Animation")]
    [SerializeField] private RectTransform relicFlyRoot;
    [SerializeField] private Image relicFlyIconImage;
    [SerializeField] private GameObject relicFlyHighlight;
    [SerializeField] private RectTransform relicSettingButtonTarget;
    [SerializeField] private TMP_Text relicSettingGuideText;

    [SerializeField] private float relicScaleUpDuration = 0.18f;
    [SerializeField] private float relicHoldDuration = 0.15f;
    [SerializeField] private float relicFlyDuration = 0.45f;
    [SerializeField] private float relicStartScale = 1f;
    [SerializeField] private float relicBigScale = 1.35f;
    [SerializeField] private float relicEndScale = 0.25f;
    [SerializeField] private float relicCurveHeight = 180f;

    [Header("SFX")]
    [SerializeField] private bool playAcquireSfx = true;
    [SerializeField] private SfxType acquireSfxType = SfxType.RelicChoiceAcquire;

    [Header("Background Sorting")]
    [SerializeField] private Transform backgroundRoot;
    [SerializeField] private int backgroundSortingOrder = -100;

    private bool isChestOpened;
    private bool isRelicClaimed;
    private Button nextButton;
    private Coroutine relicAcquireRoutine;
    private bool hasRelicFlyRootOriginalState;
    private Vector2 relicFlyRootOriginalAnchoredPosition;
    private Vector3 relicFlyRootOriginalLocalScale;
    private string pendingEventId;
    private EventDefinition currentEventDefinition;
    private bool isDataEventActive;
    private bool isEventResolved;
    private bool isEventRewardPanelOpen;
    private readonly List<BattleRewardData> pendingEventRewards = new();
    private readonly EventChoiceSessionState eventChoiceSessionState = new();

    private void Awake()
    {
        EnsureReferences();
        ApplyBackgroundSorting();
        BindNextButton();
        CacheRelicFlyRootOriginalState();
        HideRelicHoverInfo();
        HideRelicFlyObjects();
    }

    public void SetEventId(string eventId)
    {
        pendingEventId = EventIdUtility.Normalize(eventId);

        if (isActiveAndEnabled)
            TryStartDataEventMode();
    }

    private void OnEnable()
    {
        EnsureReferences();
        ApplyBackgroundSorting();
        BindNextButton();
        UnbindChestEvents();

        if (relicAcquireRoutine != null)
        {
            StopCoroutine(relicAcquireRoutine);
            relicAcquireRoutine = null;
        }

        CacheRelicFlyRootOriginalState();
        HideRelicHoverInfo();
        HideRelicFlyObjects();

        if (chestOpenButton != null)
            chestOpenButton.ResetForNewEventRoomEntry();

        isChestOpened = false;
        isRelicClaimed = false;
        isEventResolved = false;
        isEventRewardPanelOpen = false;
        pendingEventRewards.Clear();
        SetNextButtonVisible(false);

        if (TryStartDataEventMode())
            return;

        SetDataEventRootVisible(false);
        SetChestRootVisible(true);
        BindChestEvents();
    }

    private void OnDisable()
    {
        UnbindChestEvents();

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);

        if (relicAcquireRoutine != null)
        {
            StopCoroutine(relicAcquireRoutine);
            relicAcquireRoutine = null;
        }

        HideRelicHoverInfo();
        HideRelicFlyObjects();
        ClearChoiceSlots();
        SetDataEventRootVisible(false);
        isDataEventActive = false;
        isEventRewardPanelOpen = false;
        pendingEventRewards.Clear();
    }

    public void NotifyChestOpened()
    {
        if (isDataEventActive)
            return;

        isChestOpened = true;

        if (chestOpenButton == null || !chestOpenButton.IsAwaitingRewardSelection)
            SetNextButtonVisible(true);
    }

    public void OnNextButtonClicked()
    {
        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (isDataEventActive)
        {
            if (isEventRewardPanelOpen)
                return;

            if (!isEventResolved)
                return;

            if (pendingEventRewards.Count > 0 && TryOpenPendingEventRewardPanel())
                return;

            CompleteCurrentNode();
            ReturnToMap();
            return;
        }

        if (!isChestOpened)
            return;

        if (chestOpenButton != null && chestOpenButton.IsAwaitingRewardSelection && !isRelicClaimed)
            return;

        CompleteCurrentNode();

        ReturnToMap();
    }

    public void ShowRelicHoverInfo(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[EventRoomController] DataManager or RelicDatabase is null.");
            return;
        }

        if (!DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relicData) || relicData == null)
            return;

        if (relicHoverNameText != null)
            relicHoverNameText.text = GameDataLocalization.RelicName(relicData);

        if (relicHoverDescText != null)
            relicHoverDescText.text = GameDataLocalization.RelicDescription(relicData);

        if (relicHoverInfoPanel != null)
        {
            relicHoverInfoPanel.transform.SetAsLastSibling();
            relicHoverInfoPanel.SetActive(true);
        }
    }

    public void HideRelicHoverInfo()
    {
        if (relicHoverInfoPanel != null)
            relicHoverInfoPanel.SetActive(false);
    }

    private void OnRelicRewardClaimed(string relicId)
    {
        isRelicClaimed = true;
        HideRelicHoverInfo();
        PlayAcquireSfx();

        if (relicAcquireRoutine != null)
        {
            StopCoroutine(relicAcquireRoutine);
            relicAcquireRoutine = null;
        }

        HideRelicFlyObjects();
        SetNextButtonVisible(true);
    }

    private IEnumerator PlayRelicAcquireRoutine(string relicId)
    {
        Sprite relicSprite = GetRelicSprite(relicId);

        if (relicFlyIconImage != null)
        {
            relicFlyIconImage.sprite = relicSprite;
            relicFlyIconImage.enabled = relicSprite != null;
        }

        if (relicFlyRoot != null)
        {
            ResetRelicFlyRootTransform();
            relicFlyRoot.gameObject.SetActive(true);
            relicFlyRoot.localScale = Vector3.one * relicStartScale;
        }

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(true);

        yield return ScaleRelicRoutine(relicStartScale, relicBigScale, relicScaleUpDuration);
        yield return new WaitForSecondsRealtime(relicHoldDuration);

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(false);

        yield return FlyRelicToSettingButtonRoutine();

        HideRelicFlyObjects();

        if (relicSettingGuideText != null)
            relicSettingGuideText.gameObject.SetActive(true);

        SetNextButtonVisible(true);
        relicAcquireRoutine = null;
    }

    private bool TryStartDataEventMode()
    {
        ClearChoiceSlots();
        currentEventDefinition = null;
        isDataEventActive = false;
        isEventResolved = false;

        if (string.IsNullOrWhiteSpace(pendingEventId))
            return false;

        if (DataManager.Instance == null || DataManager.Instance.EventDatabase == null)
        {
            Debug.LogWarning("[EventRoomController] EventDatabase is not ready.");
            return false;
        }

        if (!DataManager.Instance.EventDatabase.TryGetEvent(pendingEventId, out EventDefinition definition) ||
            definition == null)
        {
            Debug.LogWarning($"[EventRoomController] Event data not found: {pendingEventId}");
            return false;
        }

        SetChestRootVisible(false);
        EnsureDataEventReferences();
        LoadEventDefinition(definition, string.Empty);
        return true;
    }

    private void LoadEventDefinition(EventDefinition definition, string resultMessage)
    {
        if (definition == null)
            return;

        EnsureDataEventReferences();

        currentEventDefinition = definition;
        pendingEventId = definition.EventId;
        isDataEventActive = true;
        isEventResolved = false;

        SetDataEventRootVisible(true);
        SetNextButtonVisible(false);

        if (eventNameText != null)
            eventNameText.text = string.IsNullOrWhiteSpace(definition.EventName)
                ? definition.EventId
                : definition.EventName;

        if (eventTitleText != null)
            eventTitleText.text = definition.Title ?? string.Empty;

        if (eventResultText != null)
            eventResultText.text = resultMessage ?? string.Empty;

        BindChoiceSlots(definition.Choices);
    }

    private void BindChoiceSlots(IReadOnlyList<EventData> choices)
    {
        EnsureChoiceSlots();
        ClearChoiceSlots();

        if (choiceSlots == null || choiceSlots.Length == 0 || choices == null)
            return;

        int slotIndex = 0;
        for (int i = 0; i < choices.Count && slotIndex < choiceSlots.Length; i++)
        {
            EventData choice = choices[i];
            if (choice == null)
                continue;

            EventChoiceSlotUI slot = choiceSlots[slotIndex];
            if (slot == null)
                continue;

            bool selectable = EventChoiceExecutionService.CanSelect(
                choice,
                CreateExecutionContext(),
                out string unavailableReason);
            EventData captured = choice;
            slot.Bind(
                choice,
                selectable,
                unavailableReason,
                () => OnEventChoiceClicked(captured));
            slotIndex++;
        }
    }

    private void OnEventChoiceClicked(EventData choice)
    {
        if (choice == null)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        SetChoiceSlotsInteractable(false);

        EventChoiceExecutionResult result = EventChoiceExecutionService.Execute(
            choice,
            CreateExecutionContext());

        if (!result.Accepted)
        {
            if (eventResultText != null)
                eventResultText.text = result.ResultMessage;

            BindChoiceSlots(currentEventDefinition?.Choices);
            return;
        }

        PersistEventRuntime();
        PlayVisualAction(result);

        bool hasContinuingEvent = false;

        if (!string.IsNullOrWhiteSpace(result.NextEventId) &&
            DataManager.Instance != null &&
            DataManager.Instance.EventDatabase != null &&
            DataManager.Instance.EventDatabase.TryGetEvent(result.NextEventId, out EventDefinition nextDefinition) &&
            nextDefinition != null)
        {
            hasContinuingEvent = true;
            LoadEventDefinition(nextDefinition, result.ResultMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.NextEventId))
        {
            Debug.LogWarning(
                $"[EventRoomController] Next event '{result.NextEventId}' not found. Treating this choice as terminal.",
                this);
        }

        if (eventResultText != null)
            eventResultText.text = result.ResultMessage;

        isEventResolved = true;

        if (EventRoomRewardFlowUtility.ShouldOpenPendingRewards(result, pendingEventRewards.Count, hasContinuingEvent) &&
            TryOpenPendingEventRewardPanel())
        {
            return;
        }

        SetNextButtonVisible(true);
    }

    private void PlayVisualAction(EventChoiceExecutionResult result)
    {
        if (!result.HasVisualAction)
            return;

        MapVisualController visualController = GetComponent<MapVisualController>();
        if (visualController == null)
            visualController = GetComponentInParent<MapVisualController>();
        if (visualController == null)
            visualController = GetComponentInChildren<MapVisualController>(true);

        if (visualController == null)
        {
            Debug.LogWarning(
                $"[EventRoomController] MapVisualController not found for visual action: {result.VisualObjectId}/{result.VisualActionId}",
                this);
            return;
        }

        if (!visualController.TryPlayAction(result.VisualObjectId, result.VisualActionId))
        {
            Debug.LogWarning(
                $"[EventRoomController] Visual action not found: {result.VisualObjectId}/{result.VisualActionId}",
                this);
        }
    }

    private string ResolveChoice(EventData choice, out string nextEventId)
    {
        nextEventId = string.Empty;

        int diceRoll = 0;
        bool success = true;
        List<string> messages = new();

        if (!string.IsNullOrWhiteSpace(choice.ChoiceDesc))
            messages.Add(choice.ChoiceDesc.Trim());

        if (SameToken(choice.ChoiceType, "Dice"))
        {
            diceRoll = RollThreeSixSidedDice();
            messages.Add($"주사위 결과: {diceRoll}");

            if (!string.IsNullOrWhiteSpace(choice.SuccessCondition))
                success = IsDiceSuccess(diceRoll, choice.SuccessCondition);
        }
        else if (SameToken(choice.ChoiceType, "Chance"))
        {
            success = RollChance(choice.SuccessRate);
            messages.Add(success ? "판정 성공" : "판정 실패");
        }

        if (!success)
        {
            string failure = ApplyFailureResult(choice.FailResult);
            if (!string.IsNullOrWhiteSpace(failure))
                messages.Add(failure);

            return string.Join("\n", messages);
        }

        string result = ApplySuccessResult(choice, diceRoll);
        if (!string.IsNullOrWhiteSpace(result))
            messages.Add(result);

        nextEventId = EventIdUtility.Normalize(choice.NextEventId);
        return string.Join("\n", messages);
    }

    private string ApplySuccessResult(EventData choice, int diceRoll)
    {
        string resultType = choice.ResultType?.Trim();

        if (string.IsNullOrWhiteSpace(resultType))
            return BuildResultSummary(choice);

        if (SameToken(resultType, "RollTable"))
            return ApplyRollTable(choice, diceRoll);

        if (SameToken(resultType, "GainRandom"))
        {
            if (Contains(choice.ResultTarget, "유물"))
                return GrantRandomRelic();

            return BuildResultSummary(choice);
        }

        if (SameToken(resultType, "GainMultiple"))
        {
            if (Contains(choice.ResultTarget, "유물"))
                return $"{BuildResultSummary(choice)}\n{GrantRandomRelic()}";

            return BuildResultSummary(choice);
        }

        if (SameToken(resultType, "Modify"))
        {
            if (TryParseSignedValue(choice.ResultValue, out int amount))
            {
                if (Contains(choice.ResultTarget, "코스트 회복"))
                {
                    int count = ModifyPartyCostRecovery(amount);
                    return $"파티 코스트 회복량 {amount:+#;-#;0} 적용 ({count}명)";
                }

                if (Contains(choice.ResultTarget, "최대 코스트"))
                {
                    int count = ModifyPartyMaxCost(amount);
                    return $"파티 최대 코스트 {amount:+#;-#;0} 적용 ({count}명)";
                }
            }

            return BuildResultSummary(choice);
        }

        if (SameToken(resultType, "Heal"))
        {
            if (TryParseSignedValue(choice.ResultValue, out int amount))
            {
                int count = ModifyPartyCurrentHp(Mathf.Max(0, amount));
                return $"파티 체력 {amount} 회복 ({count}명)";
            }

            return BuildResultSummary(choice);
        }

        if (SameToken(resultType, "Accumulate"))
        {
            eventChoiceSessionState.AccumulatedRemnant += EventChoiceExecutionService.SmallRemnantAmount;
            return BuildResultSummary(choice);
        }

        if (SameToken(resultType, "CommitAccumulated"))
        {
            bool hadReward = eventChoiceSessionState.AccumulatedRemnant > 0;
            eventChoiceSessionState.AccumulatedRemnant = 0;
            return hadReward ? BuildResultSummary(choice) : "확정할 누적 보상이 없습니다.";
        }

        if (SameToken(resultType, "OpenPanel"))
        {
            if (Contains(choice.ResultTarget, "상점") && TryOpenShopPanel())
                return "상점 패널을 열었습니다.";

            return BuildResultSummary(choice);
        }

        if (SameToken(resultType, "EndEvent"))
            return string.IsNullOrWhiteSpace(choice.ChoiceDesc) ? "이벤트를 종료합니다." : string.Empty;

        return BuildResultSummary(choice);
    }

    private string ApplyRollTable(EventData choice, int diceRoll)
    {
        string tableId = choice.ResultValue?.Trim();

        if (SameToken(tableId, "RT001"))
        {
            int amount = diceRoll <= 8 ? 3 : diceRoll <= 15 ? 5 : 10;
            int count = ModifyPartyCurrentHp(amount);
            return $"파티 전원 체력 {amount} 회복 ({count}명)";
        }

        if (SameToken(tableId, "RT002"))
        {
            int amount = diceRoll <= 8 ? 2 : diceRoll <= 15 ? 4 : 8;
            int count = ModifyPartyMaxHp(amount);
            return $"파티 전원 최대 체력 {amount} 증가 ({count}명)";
        }

        if (SameToken(tableId, "RT003"))
            return BuildResultSummary(choice);

        return BuildResultSummary(choice);
    }

    private string ApplyFailureResult(string failResult)
    {
        if (string.IsNullOrWhiteSpace(failResult))
            return "실패했습니다.";

        if (Contains(failResult, "현재 체력") && TryParseSignedValue(failResult, out int hpAmount))
            ModifyPartyCurrentHp(hpAmount);

        if (Contains(failResult, "최대 코스트") && TryParseSignedValue(failResult, out int maxCostAmount))
            ModifyPartyMaxCost(maxCostAmount);

        if (Contains(failResult, "누적") && Contains(failResult, "소실"))
            eventChoiceSessionState.AccumulatedRemnant = 0;

        return failResult.Trim();
    }

    private string GrantRandomRelic()
    {
        if (!ChestRelicRewardService.TryRollReward(DataManager.Instance, out ChestRelicReward reward) ||
            !ChestRelicRewardService.GrantReward(DataManager.Instance, reward))
        {
            return "획득 가능한 유물이 없습니다.";
        }

        string relicName = reward.Relic != null
            ? GameDataLocalization.RelicName(reward.Relic)
            : reward.RelicId;

        return $"유물 획득: {relicName}";
    }

    private int ModifyPartyCurrentHp(int amount)
    {
        int count = 0;

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null)
                continue;

            character.CurrentHP = Mathf.Clamp(character.CurrentHP + amount, 0, Mathf.Max(0, character.MaxHP));
            count++;
        }

        return count;
    }

    private int ModifyPartyMaxHp(int amount)
    {
        int count = 0;

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null)
                continue;

            character.MaxHP = Mathf.Max(0, character.MaxHP + amount);
            character.CurrentHP = Mathf.Clamp(character.CurrentHP + Mathf.Max(0, amount), 0, character.MaxHP);
            count++;
        }

        return count;
    }

    private int ModifyPartyMaxCost(int amount)
    {
        int count = 0;

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null)
                continue;

            character.MaxCost = Mathf.Max(0, character.MaxCost + amount);
            character.CurrentCost = Mathf.Clamp(character.CurrentCost, 0, character.MaxCost);
            count++;
        }

        return count;
    }

    private int ModifyPartyCostRecovery(int amount)
    {
        int count = 0;

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null)
                continue;

            character.BonusCostRecovery += amount;
            count++;
        }

        return count;
    }

    private IEnumerable<CharacterRuntimeData> EnumeratePartyCharacters()
    {
        if (DataManager.Instance == null || DataManager.Instance.CharacterRuntimeStore == null)
            yield break;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;
        HashSet<string> yielded = new();

        if (partyStore != null)
        {
            for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
            {
                string characterId = partyStore.GetCharacterId(i);
                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                characterId = characterId.Trim();
                if (!yielded.Add(characterId))
                    continue;

                if (characterStore.TryGet(characterId, out CharacterRuntimeData character) && character != null)
                    yield return character;
            }
        }

        if (yielded.Count > 0)
            yield break;

        IReadOnlyDictionary<string, CharacterRuntimeData> allCharacters = characterStore.GetAll();
        if (allCharacters == null)
            yield break;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in allCharacters)
        {
            if (pair.Value != null)
                yield return pair.Value;
        }
    }

    private bool TryOpenShopPanel()
    {
        RestRoomShopPanel shopPanel =
            Object.FindFirstObjectByType<RestRoomShopPanel>(FindObjectsInactive.Include);

        if (shopPanel == null)
            return false;

        shopPanel.Open();
        return true;
    }

    private EventChoiceExecutionContext CreateExecutionContext()
    {
        BattleRuntimeData battleRuntime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();

        return new EventChoiceExecutionContext
        {
            BattleRuntime = battleRuntime,
            PartyCharacters = CollectPartyCharacters(),
            SessionState = eventChoiceSessionState,
            GrantRandomRelic = TryQueueRandomRelicReward,
            GrantRandomSkill = TryQueueRandomSkillReward,
            UpgradeRandomSkill = TryUpgradeRandomSkill,
            GrantRemnant = TryQueueRemnantReward,
            RevokeRemnant = RevokeQueuedRemnantReward,
            OpenShop = TryOpenShopPanel,
            RefreshRemnantHud = BattleGoldHudUI.RefreshAll,
            SuppressRewardResultMessages = true
        };
    }

    private void PersistEventRuntime()
    {
        if (DataManager.Instance == null)
            return;

        BattleRuntimeData battleRuntime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();
        if (battleRuntime != null)
            DataManager.Instance.BattleRuntimeStore.Set(battleRuntime);

        BattleGoldHudUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();
        EquippedSkillPanelUI.RefreshAll();
        RelicEquipPanelUI.RefreshAll();
    }

    private List<CharacterRuntimeData> CollectPartyCharacters()
    {
        List<CharacterRuntimeData> characters = new();

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character != null)
                characters.Add(character);
        }

        return characters;
    }

    private bool TryQueueRemnantReward(int amount, out string resultMessage)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (safeAmount <= 0)
        {
            resultMessage = "획득 가능한 레드 더스티움이 없습니다.";
            return false;
        }

        QueueEventReward(EventRoomRewardFlowUtility.CreateRemnantReward(safeAmount));
        resultMessage = $"레드 더스티움 {safeAmount} 획득";
        return true;
    }

    private void RevokeQueuedRemnantReward(int amount)
    {
        int remaining = Mathf.Max(0, amount);

        if (remaining <= 0)
            return;

        for (int i = pendingEventRewards.Count - 1; i >= 0 && remaining > 0; i--)
        {
            BattleRewardData reward = pendingEventRewards[i];
            if (reward == null || reward.Type != BattleRewardType.Remnant)
                continue;

            int consumed = Mathf.Min(reward.Amount, remaining);
            reward.Amount -= consumed;
            remaining -= consumed;

            if (reward.Amount <= 0)
                pendingEventRewards.RemoveAt(i);
        }
    }

    private bool TryQueueRandomRelicReward(out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!TryPickRandomAvailableRelic(out ChestRelicReward reward))
        {
            resultMessage = "획득 가능한 유물이 없습니다.";
            return false;
        }

        string relicName = reward.Relic != null
            ? GameDataLocalization.RelicName(reward.Relic)
            : reward.RelicId;

        QueueEventReward(EventRoomRewardFlowUtility.CreateRelicReward(
            reward.Relic,
            GetRelicSprite(reward.RelicId)));

        resultMessage = $"유물 획득: {relicName}";
        return true;
    }

    private bool TryQueueRandomSkillReward(out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!TryPickRandomAvailableSkill(out SkillMasterData skill) ||
            skill == null ||
            string.IsNullOrWhiteSpace(skill.SkillId))
        {
            resultMessage = "획득 가능한 기억이 없습니다.";
            return false;
        }

        string skillId = skill.SkillId.Trim();
        QueueEventReward(EventRoomRewardFlowUtility.CreateSkillReward(
            skill,
            GetSkillSprite(skillId, skill)));

        resultMessage = $"기억 획득: {GameDataLocalization.SkillName(skill)}";
        return true;
    }

    private bool TryGrantRandomRelic(out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!ChestRelicRewardService.TryRollReward(DataManager.Instance, out ChestRelicReward reward) ||
            !ChestRelicRewardService.GrantReward(DataManager.Instance, reward))
        {
            resultMessage = "획득 가능한 유물이 없습니다.";
            return false;
        }

        string relicName = reward.Relic != null
            ? GameDataLocalization.RelicName(reward.Relic)
            : reward.RelicId;

        resultMessage = $"유물 획득: {relicName}";
        return true;
    }

    private bool TryGrantRandomSkill(out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!TryPickRandomAvailableSkill(out SkillMasterData skill) ||
            skill == null ||
            string.IsNullOrWhiteSpace(skill.SkillId))
        {
            resultMessage = "획득 가능한 기억이 없습니다.";
            return false;
        }

        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null)
        {
            resultMessage = "전투 런타임 데이터가 없습니다.";
            return false;
        }

        runtime.SkillInventoryIds ??= new List<string>();
        runtime.AcquiredSkillIds ??= new List<string>();

        string skillId = skill.SkillId.Trim();
        runtime.SkillInventoryIds.Add(skillId);

        if (!ContainsId(runtime.AcquiredSkillIds, skillId))
            runtime.AcquiredSkillIds.Add(skillId);

        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        resultMessage = $"기억 획득: {GameDataLocalization.SkillName(skill)}";
        return true;
    }

    private bool TryUpgradeRandomSkill(out string resultMessage)
    {
        resultMessage = string.Empty;
        List<OwnedSkillReference> candidates = CollectUpgradeableSkillReferences();

        if (candidates.Count == 0)
        {
            resultMessage = "강화 가능한 기억이 없습니다.";
            return false;
        }

        OwnedSkillReference selected = candidates[BattleRandom.Range(0, candidates.Count)];

        if (!SkillRarityUtility.TryGetPairedVariantId(selected.SkillId, out string upgradeId) ||
            string.IsNullOrWhiteSpace(upgradeId))
        {
            resultMessage = "강화 가능한 기억이 없습니다.";
            return false;
        }

        ApplySkillUpgrade(selected, upgradeId);

        SkillMasterData upgradedSkill = DataManager.Instance.SkillDatabase.Get(upgradeId);
        resultMessage = $"기억 강화: {GameDataLocalization.SkillName(upgradedSkill)}";
        return true;
    }

    private bool TryPickRandomAvailableRelic(out ChestRelicReward reward)
    {
        reward = default;

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
            return false;

        IReadOnlyList<RelicData> allRelics = DataManager.Instance.RelicDatabase.GetAll();
        List<RelicData> candidates = ChestRelicRewardService.GetChestRewardCandidates(
            allRelics,
            CollectUnavailableRelicIds());

        if (candidates.Count == 0)
            return false;

        RelicData selected = candidates[BattleRandom.Range(0, candidates.Count)];
        if (selected == null || !RelicRarityUtility.TryParseChestRarity(selected.Rarity, out RelicRarity rarity))
            return false;

        reward = new ChestRelicReward(selected, rarity);
        return reward.IsValid;
    }

    private bool TryPickRandomAvailableSkill(out SkillMasterData selectedSkill)
    {
        selectedSkill = null;

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
            return false;

        List<SkillMasterData> allSkills = DataManager.Instance.SkillDatabase.GetAll();
        if (allSkills == null || allSkills.Count == 0)
            return false;

        HashSet<string> unavailableIds = CollectUnavailableSkillIds();
        List<SkillMasterData> candidates = new();

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillMasterData skill = allSkills[i];

            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            string skillId = skill.SkillId.Trim();

            if (skill.Category != Category.Core)
                continue;

            if (!SkillRarityUtility.IsBaseSkillVariant(skillId))
                continue;

            if (unavailableIds.Contains(skillId))
                continue;

            candidates.Add(skill);
        }

        if (candidates.Count == 0)
            return false;

        selectedSkill = candidates[BattleRandom.Range(0, candidates.Count)];
        return selectedSkill != null;
    }

    private HashSet<string> CollectUnavailableSkillIds()
    {
        HashSet<string> ids = new();
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.SkillInventoryIds != null)
        {
            for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
                AddSkillAndPair(ids, runtime.SkillInventoryIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance?.CharacterRuntimeStore?.GetAll();

        if (characters != null)
        {
            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;

                if (character == null)
                    continue;

                AddSkillAndPair(ids, character.MoveSkillId);
                AddSkillAndPair(ids, character.PassiveSkillId);
                AddSkillAndPair(ids, character.UniqueSkillId);
                AddSkillAndPair(ids, character.AbilitySkillId);

                if (character.EquippedSkillIds == null)
                    continue;

                for (int i = 0; i < character.EquippedSkillIds.Length; i++)
                    AddSkillAndPair(ids, character.EquippedSkillIds[i]);
            }
        }

        for (int i = 0; i < pendingEventRewards.Count; i++)
        {
            BattleRewardData reward = pendingEventRewards[i];
            if (reward != null && reward.Type == BattleRewardType.Skill)
                AddSkillAndPair(ids, reward.RewardId);
        }

        return ids;
    }

    private HashSet<string> CollectUnavailableRelicIds()
    {
        HashSet<string> ids = new();
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.OwnedRelicIds != null)
        {
            for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
                AddRelicId(ids, runtime.OwnedRelicIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance?.CharacterRuntimeStore?.GetAll();

        if (characters != null)
        {
            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;

                if (character?.EquippedRelicIds == null)
                    continue;

                for (int i = 0; i < character.EquippedRelicIds.Length; i++)
                    AddRelicId(ids, character.EquippedRelicIds[i]);
            }
        }

        for (int i = 0; i < pendingEventRewards.Count; i++)
        {
            BattleRewardData reward = pendingEventRewards[i];
            if (reward != null && reward.Type == BattleRewardType.Relic)
                AddRelicId(ids, reward.RewardId);
        }

        return ids;
    }

    private List<OwnedSkillReference> CollectUpgradeableSkillReferences()
    {
        List<OwnedSkillReference> candidates = new();

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
            return candidates;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore?.GetOrCreate();

        if (runtime?.SkillInventoryIds != null)
        {
            for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
                AddUpgradeableReference(candidates, runtime.SkillInventoryIds[i], null, -1, i);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters =
            DataManager.Instance.CharacterRuntimeStore?.GetAll();

        if (characters != null)
        {
            foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
            {
                CharacterRuntimeData character = pair.Value;

                if (character?.EquippedSkillIds == null)
                    continue;

                for (int i = 0; i < character.EquippedSkillIds.Length; i++)
                    AddUpgradeableReference(candidates, character.EquippedSkillIds[i], character, i, -1);
            }
        }

        return candidates;
    }

    private void AddUpgradeableReference(
        List<OwnedSkillReference> candidates,
        string skillId,
        CharacterRuntimeData character,
        int equippedIndex,
        int inventoryIndex)
    {
        if (candidates == null || string.IsNullOrWhiteSpace(skillId))
            return;

        string normalizedSkillId = skillId.Trim();

        if (SkillRarityUtility.IsUpgradeSkillVariant(normalizedSkillId))
            return;

        if (!DataManager.Instance.SkillDatabase.TryGet(normalizedSkillId, out SkillMasterData skill) ||
            !SkillRarityUtility.CanUpgrade(skill))
        {
            return;
        }

        if (!SkillRarityUtility.TryGetPairedVariantId(normalizedSkillId, out string upgradeId) ||
            !DataManager.Instance.SkillDatabase.TryGet(upgradeId, out _))
        {
            return;
        }

        candidates.Add(new OwnedSkillReference(normalizedSkillId, character, equippedIndex, inventoryIndex));
    }

    private void ApplySkillUpgrade(OwnedSkillReference selected, string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId))
            return;

        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();

        if (selected.InventoryIndex >= 0 &&
            runtime?.SkillInventoryIds != null &&
            selected.InventoryIndex < runtime.SkillInventoryIds.Count)
        {
            runtime.SkillInventoryIds[selected.InventoryIndex] = upgradeId;
            DataManager.Instance.BattleRuntimeStore.Set(runtime);
            return;
        }

        CharacterRuntimeData character = selected.Character;
        if (character == null)
            return;

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);

        if (selected.EquippedIndex >= 0 &&
            selected.EquippedIndex < character.EquippedSkillIds.Length)
        {
            character.EquippedSkillIds[selected.EquippedIndex] = upgradeId;
        }

        if (string.Equals(character.AbilitySkillId?.Trim(), selected.SkillId, System.StringComparison.Ordinal))
            character.AbilitySkillId = upgradeId;

        if (string.Equals(character.UniqueSkillId?.Trim(), selected.SkillId, System.StringComparison.Ordinal))
            character.UniqueSkillId = upgradeId;
    }

    private void QueueEventReward(BattleRewardData reward)
    {
        if (reward == null)
            return;

        if (reward.Type == BattleRewardType.Remnant)
        {
            BattleRewardData existing = pendingEventRewards.Find(x => x != null && x.Type == BattleRewardType.Remnant);
            if (existing != null)
            {
                existing.Amount += Mathf.Max(0, reward.Amount);
                return;
            }
        }

        pendingEventRewards.Add(reward);
    }

    private bool TryOpenPendingEventRewardPanel()
    {
        if (pendingEventRewards.Count <= 0)
            return false;

        EnsureRewardPanelReference();

        if (rewardPanel == null)
        {
            Debug.LogWarning("[EventRoomController] EventRoomRewardPanelUI not found for event rewards.");
            return false;
        }

        isEventRewardPanelOpen = true;
        SetNextButtonVisible(false);
        SetChoiceSlotsInteractable(false);

        List<BattleRewardData> rewards = new(pendingEventRewards);
        pendingEventRewards.Clear();
        rewardPanel.Open(rewards, OnEventRewardPanelCompleted);
        return true;
    }

    private void OnEventRewardPanelCompleted()
    {
        isEventRewardPanelOpen = false;
        pendingEventRewards.Clear();
        PersistEventRuntime();
        CompleteCurrentNode();
        ReturnToMap();
    }

    private void EnsureRewardPanelReference()
    {
        if (rewardPanel != null)
            return;

        rewardPanel = GetComponentInChildren<EventRoomRewardPanelUI>(true);

        if (rewardPanel == null)
            rewardPanel = Object.FindFirstObjectByType<EventRoomRewardPanelUI>(FindObjectsInactive.Include);
    }

    private Sprite GetSkillSprite(string skillId, SkillMasterData skill)
    {
        if (skill != null && skill.Icon != null)
            return skill.Icon;

        if (string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    private static void AddSkillAndPair(HashSet<string> ids, string skillId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(skillId))
            return;

        string normalizedSkillId = skillId.Trim();
        ids.Add(normalizedSkillId);

        if (SkillRarityUtility.TryGetPairedVariantId(normalizedSkillId, out string pairedSkillId))
            ids.Add(pairedSkillId);
    }

    private static void AddRelicId(HashSet<string> ids, string relicId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(relicId))
            return;

        ids.Add(relicId.Trim());
    }

    private static bool ContainsId(IReadOnlyList<string> ids, string targetId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(targetId))
            return false;

        for (int i = 0; i < ids.Count; i++)
        {
            if (string.Equals(ids[i]?.Trim(), targetId.Trim(), System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private readonly struct OwnedSkillReference
    {
        public OwnedSkillReference(
            string skillId,
            CharacterRuntimeData character,
            int equippedIndex,
            int inventoryIndex)
        {
            SkillId = skillId;
            Character = character;
            EquippedIndex = equippedIndex;
            InventoryIndex = inventoryIndex;
        }

        public string SkillId { get; }
        public CharacterRuntimeData Character { get; }
        public int EquippedIndex { get; }
        public int InventoryIndex { get; }
    }

    private bool CanSelectChoice(EventData choice)
    {
        if (choice == null || string.IsNullOrWhiteSpace(choice.SelectCondition))
            return true;

        string condition = choice.SelectCondition;

        if (Contains(condition, "채굴") && Contains(condition, "성공"))
            return eventChoiceSessionState.AccumulatedRemnant > 0;

        if (Contains(condition, "유물") && Contains(condition, "보유"))
            return HasAnyOwnedRelic();

        return true;
    }

    private bool HasAnyOwnedRelic()
    {
        BattleRuntimeData battleRuntime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (battleRuntime?.OwnedRelicIds != null && battleRuntime.OwnedRelicIds.Count > 0)
            return true;

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character?.EquippedRelicIds == null)
                continue;

            for (int i = 0; i < character.EquippedRelicIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(character.EquippedRelicIds[i]))
                    return true;
            }
        }

        return false;
    }

    private int RollThreeSixSidedDice()
    {
        return BattleRandom.Range(1, 7) +
               BattleRandom.Range(1, 7) +
               BattleRandom.Range(1, 7);
    }

    private bool RollChance(string successRate)
    {
        if (!TryParsePercentage(successRate, out float rate))
            rate = 1f;

        return BattleRandom.Range(0, 10000) < Mathf.RoundToInt(rate * 10000f);
    }

    private bool IsDiceSuccess(int diceRoll, string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        string[] ranges = condition.Split(new[] { ',', '/' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < ranges.Length; i++)
        {
            if (TryParseRange(ranges[i], out int min, out int max) &&
                diceRoll >= min &&
                diceRoll <= max)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryParseRange(string text, out int min, out int max)
    {
        min = 0;
        max = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string normalized = text.Trim().Replace("~", "-");
        string[] parts = normalized.Split(new[] { '-' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1 && int.TryParse(parts[0].Trim(), out int single))
        {
            min = single;
            max = single;
            return true;
        }

        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0].Trim(), out min) ||
            !int.TryParse(parts[1].Trim(), out max))
        {
            return false;
        }

        if (min > max)
            (min, max) = (max, min);

        return true;
    }

    private bool TryParsePercentage(string value, out float rate)
    {
        rate = 0f;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = value.Trim().Replace("%", string.Empty);

        if (!float.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsed))
            return false;

        rate = Mathf.Clamp01(parsed > 1f ? parsed / 100f : parsed);
        return true;
    }

    private static bool TryParseSignedValue(string value, out int amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        MatchCollection matches = Regex.Matches(value, @"[+-]?\d+");
        if (matches.Count == 0)
            return false;

        return int.TryParse(matches[matches.Count - 1].Value, out amount);
    }

    private static bool SameToken(string left, string right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string source, string value)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               !string.IsNullOrWhiteSpace(value) &&
               source.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string BuildResultSummary(EventData choice)
    {
        if (choice == null)
            return string.Empty;

        List<string> parts = new();

        if (!string.IsNullOrWhiteSpace(choice.ResultType))
            parts.Add(choice.ResultType.Trim());

        if (!string.IsNullOrWhiteSpace(choice.ResultTarget))
            parts.Add(choice.ResultTarget.Trim());

        if (!string.IsNullOrWhiteSpace(choice.ResultValue))
            parts.Add(choice.ResultValue.Trim());

        return parts.Count > 0 ? string.Join(" / ", parts) : string.Empty;
    }

    private static string BuildChoiceLabel(EventData choice)
    {
        if (choice == null)
            return string.Empty;

        string order = choice.ChoiceOrder > 0 ? $"{choice.ChoiceOrder}. " : string.Empty;
        string name = choice.ChoiceName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(choice.ChoiceDesc))
            return $"{order}{name}";

        return $"{order}{name}\n{choice.ChoiceDesc}";
    }

    private void EnsureDataEventReferences()
    {
        if (dataEventRoot == null)
        {
            Transform dataRoot = FindChildRecursive(transform, "DataEventRoot");
            if (dataRoot != null)
                dataEventRoot = dataRoot.gameObject;
        }

        Transform searchRoot = dataEventRoot != null ? dataEventRoot.transform : transform;

        if (eventNameText == null)
            eventNameText = FindText(searchRoot, "EventNameText");

        if (eventTitleText == null)
            eventTitleText = FindText(searchRoot, "EventTitleText");

        if (eventResultText == null)
            eventResultText = FindText(searchRoot, "EventResultText");

        EnsureChoiceSlots();
    }

    private void EnsureChoiceSlots()
    {
        if (choiceSlots != null && choiceSlots.Length > 0)
            return;

        Transform searchRoot = dataEventRoot != null ? dataEventRoot.transform : transform;
        choiceSlots = searchRoot.GetComponentsInChildren<EventChoiceSlotUI>(true);
        SortChoiceSlotsByName(choiceSlots);
    }

    private static void SortChoiceSlotsByName(EventChoiceSlotUI[] slots)
    {
        if (slots == null || slots.Length <= 1)
            return;

        System.Array.Sort(slots, (left, right) =>
        {
            string leftName = left != null ? left.name : string.Empty;
            string rightName = right != null ? right.name : string.Empty;
            return string.CompareOrdinal(leftName, rightName);
        });
    }

    private TMP_Text FindText(Transform root, string targetName)
    {
        Transform target = FindChildRecursive(root, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private void SetChoiceSlotsInteractable(bool interactable)
    {
        EnsureChoiceSlots();

        if (choiceSlots == null)
            return;

        for (int i = 0; i < choiceSlots.Length; i++)
            choiceSlots[i]?.SetInteractable(interactable);
    }

    private void ClearChoiceSlots()
    {
        EnsureChoiceSlots();

        if (choiceSlots == null)
            return;

        for (int i = 0; i < choiceSlots.Length; i++)
            choiceSlots[i]?.Clear();
    }

    private void EnsureReferences()
    {
        if (chestOpenButton == null)
            chestOpenButton = GetComponentInChildren<ChestOpenButton>(true);

        if (relicHoverInfoPanel == null)
        {
            Transform hoverPanel = FindChildRecursive(transform, "RelicHoverInfoPanel");
            if (hoverPanel != null)
                relicHoverInfoPanel = hoverPanel.gameObject;
        }

        if (relicHoverInfoPanel != null)
        {
            TMP_Text[] texts = relicHoverInfoPanel.GetComponentsInChildren<TMP_Text>(true);
            if (relicHoverNameText == null && texts.Length > 0)
                relicHoverNameText = texts[0];
            if (relicHoverDescText == null && texts.Length > 1)
                relicHoverDescText = texts[1];
        }

        if (relicFlyRoot == null)
        {
            Transform flyRoot = FindChildRecursive(transform, "RelicFlyRoot");
            if (flyRoot != null)
                relicFlyRoot = flyRoot as RectTransform;
        }

        if (relicFlyRoot != null && relicFlyIconImage == null)
            relicFlyIconImage = relicFlyRoot.GetComponentInChildren<Image>(true);

        if (relicSettingButtonTarget == null)
        {
            Transform settingTarget = FindChildRecursive(null, "RelicSettingButton");
            if (settingTarget != null)
                relicSettingButtonTarget = settingTarget as RectTransform;
        }

        if (backgroundRoot == null)
        {
            Transform backgroundTransform = FindChildRecursive(transform, "background");

            if (backgroundTransform != null)
                backgroundRoot = backgroundTransform;
        }

        EnsureRewardPanelReference();
        EnsureNextButtonRoot();
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

    private void BindChestEvents()
    {
        if (chestOpenButton == null)
            return;

        chestOpenButton.Opened -= NotifyChestOpened;
        chestOpenButton.RewardPointerEntered -= ShowRelicHoverInfo;
        chestOpenButton.RewardPointerExited -= HideRelicHoverInfo;
        chestOpenButton.RewardClaimed -= OnRelicRewardClaimed;

        chestOpenButton.Opened += NotifyChestOpened;
        chestOpenButton.RewardPointerEntered += ShowRelicHoverInfo;
        chestOpenButton.RewardPointerExited += HideRelicHoverInfo;
        chestOpenButton.RewardClaimed += OnRelicRewardClaimed;
    }

    private void UnbindChestEvents()
    {
        if (chestOpenButton == null)
            return;

        chestOpenButton.Opened -= NotifyChestOpened;
        chestOpenButton.RewardPointerEntered -= ShowRelicHoverInfo;
        chestOpenButton.RewardPointerExited -= HideRelicHoverInfo;
        chestOpenButton.RewardClaimed -= OnRelicRewardClaimed;
    }

    private void BindNextButton()
    {
        EnsureNextButtonRoot();

        if (nextButton == null)
            return;

        nextButton.onClick.RemoveListener(OnNextButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void SetNextButtonVisible(bool visible)
    {
        EnsureNextButtonRoot();

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(visible);
    }

    private void SetChestRootVisible(bool visible)
    {
        if (chestOpenButton != null)
            chestOpenButton.gameObject.SetActive(visible);
    }

    private void SetDataEventRootVisible(bool visible)
    {
        if (dataEventRoot != null)
            dataEventRoot.SetActive(visible);
    }

    private void ReturnToMap()
    {
        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[EventRoomController] BattleSceneController not found");
    }

    private void DestroyGeneratedObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private void HideRelicFlyObjects()
    {
        ResetRelicFlyRootTransform();

        if (relicFlyRoot != null)
            relicFlyRoot.gameObject.SetActive(false);

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(false);

        if (relicSettingGuideText != null)
            relicSettingGuideText.gameObject.SetActive(false);
    }


    private void CacheRelicFlyRootOriginalState()
    {
        if (relicFlyRoot == null || hasRelicFlyRootOriginalState)
            return;

        relicFlyRootOriginalAnchoredPosition = relicFlyRoot.anchoredPosition;
        relicFlyRootOriginalLocalScale = relicFlyRoot.localScale;
        hasRelicFlyRootOriginalState = true;
    }

    private void ResetRelicFlyRootTransform()
    {
        if (relicFlyRoot == null)
            return;

        CacheRelicFlyRootOriginalState();

        if (!hasRelicFlyRootOriginalState)
            return;

        relicFlyRoot.anchoredPosition = relicFlyRootOriginalAnchoredPosition;
        relicFlyRoot.localScale = relicFlyRootOriginalLocalScale;
    }

    private Sprite GetRelicSprite(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        if (DataManager.Instance == null || DataManager.Instance.RelicIconDatabase == null)
            return null;

        if (!DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
            return null;

        return icon;
    }

    private void PlayAcquireSfx()
    {
        if (!playAcquireSfx || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(acquireSfxType);
    }

    private IEnumerator ScaleRelicRoutine(float from, float to, float duration)
    {
        if (relicFlyRoot == null)
            yield break;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float scale = Mathf.Lerp(from, to, EaseOutCubic(t));
            relicFlyRoot.localScale = Vector3.one * scale;
            yield return null;
        }

        relicFlyRoot.localScale = Vector3.one * to;
    }

    private IEnumerator FlyRelicToSettingButtonRoutine()
    {
        if (relicFlyRoot == null || relicSettingButtonTarget == null)
            yield break;

        Vector2 start = relicFlyRoot.anchoredPosition;
        Vector2 end = GetTargetLocalPosition(relicFlyRoot, relicSettingButtonTarget);
        Vector2 control = (start + end) * 0.5f + Vector2.up * relicCurveHeight;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, relicFlyDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = EaseInOutCubic(t);

            Vector2 p1 = Vector2.Lerp(start, control, eased);
            Vector2 p2 = Vector2.Lerp(control, end, eased);

            relicFlyRoot.anchoredPosition = Vector2.Lerp(p1, p2, eased);
            relicFlyRoot.localScale = Vector3.one * Mathf.Lerp(relicBigScale, relicEndScale, eased);

            yield return null;
        }

        relicFlyRoot.anchoredPosition = end;
        relicFlyRoot.localScale = Vector3.one * relicEndScale;
    }

    private Vector2 GetTargetLocalPosition(RectTransform movingRect, RectTransform targetRect)
    {
        RectTransform parentRect = movingRect.parent as RectTransform;

        if (parentRect == null || targetRect == null)
            return movingRect.anchoredPosition;

        Canvas canvas = movingRect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint))
        {
            return localPoint;
        }

        return movingRect.anchoredPosition;
    }

    private void ApplyBackgroundSorting()
    {
        if (backgroundRoot == null)
            return;

        Renderer[] renderers = backgroundRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sortingOrder = backgroundSortingOrder;
        }
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root == null)
        {
            Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i] != null && string.Equals(allTransforms[i].name, targetName, System.StringComparison.Ordinal))
                    return allTransforms[i];
            }

            return null;
        }

        if (string.Equals(root.name, targetName, System.StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private void CompleteCurrentNode()
    {
        if (DataManager.Instance == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        string nodeKey = runtime.CurrentNodeIndex.ToString();

        if (!runtime.ClearedMapIds.Contains(nodeKey))
            runtime.ClearedMapIds.Add(nodeKey);

        if (!runtime.VisitedMapIds.Contains(nodeKey))
            runtime.VisitedMapIds.Add(nodeKey);

        DataManager.Instance.MapRuntimeStore.Set(runtime);

        Debug.Log(
            $"[EventRoomController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
}
