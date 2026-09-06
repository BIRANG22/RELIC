using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Object = UnityEngine.Object;


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

    [Header("Event Choice Entrance")]
    [SerializeField] private RectTransform eventChoiceScrollView;
    [SerializeField] private float eventChoiceScrollStartY = -400f;
    [SerializeField] private float eventChoiceScrollEndY = -100f;
    [SerializeField] private RectTransform eventChoiceGradation;
    [SerializeField] private float eventChoiceGradationStartY = -600f;
    [SerializeField] private float eventChoiceGradationEndY = -250f;
    [SerializeField, Min(0.01f)] private float eventChoiceScrollMoveDuration = 0.4f;

    [Header("Terminal Choice Exit")]
    [SerializeField, Min(0.01f)] private float terminalChoiceFadeDuration = 0.25f;

    [Header("Event Rewards")]
    [SerializeField] private BattleRewardPanelUI rewardPanel;
    [SerializeField, Min(0f)] private float eventRewardPanelOpenDelay = 0.6f;

    [Header("Event Dustium Acquire")]
    [Tooltip("이벤트에서 레드 더스티움을 획득할 때 표시할 Dustium UI입니다.")]
    [SerializeField] private RectTransform dustiumAcquireRoot;
    [SerializeField] private TMP_Text dustiumAcquireValueText;
    [Tooltip("Dustium이 날아가 도착할 GoldHud 위치입니다.")]
    [SerializeField] private RectTransform goldHudTarget;
    [SerializeField] private Vector2 dustiumAppearOffset = new Vector2(90f, 190f);
    [SerializeField] private float dustiumAppearCurveHeight = 110f;
    [SerializeField, Min(0.01f)] private float dustiumAppearDuration = 0.25f;
    [Tooltip("이벤트 오브젝트 성공 연출을 먼저 보여준 뒤 Dustium 획득 연출을 시작하기까지 기다리는 시간입니다.")]
    [SerializeField, Min(0f)] private float dustiumVisualActionWaitDuration = 0.6f;
    [SerializeField, Min(0f)] private float dustiumValueHoldDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float dustiumFlyDuration = 0.55f;
    [SerializeField] private float dustiumFlyCurveHeight = 180f;
    [SerializeField, Min(0f)] private float dustiumFlyEndScale = 0.2f;

    [Header("Shop")]
    [SerializeField] private RestRoomShopPanel shopPanel;
    [Tooltip("Event_06의 오브젝트 연출을 먼저 보여준 뒤 상점 패널을 열기까지 기다리는 시간입니다.")]
    [SerializeField, Min(0f)] private float event06ShopOpenDelay = 0.6f;
    [Tooltip("Event_06 상점이 열리고 닫힐 때 EventRoom의 TITLE/선택지 UI가 페이드되는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float event06EventUiFadeDuration = 0.25f;
    private bool waitingForEvent06ShopClose;
    private bool pendingEvent06ShopOpen;
    private Coroutine event06ShopOpenRoutine;
    private Coroutine event06ResultUiRoutine;
    private string event06CloseVisualObjectId;
    private string event06CloseVisualActionId;
    private string event06NextEventId;

    [Header("Event Relic Selection")]
    [SerializeField] private EventEquippedRelicSelectionPanelUI equippedRelicSelectionPanel;

    [Header("Event Skill Awaken Selection")]
    [SerializeField] private EventSkillAwakenSelectionPanelUI skillAwakenSelectionPanel;
    [SerializeField, Min(0.01f)] private float skillAwakenScrollFadeDuration = 0.25f;

    [Header("Event Dice Roll")]
    [SerializeField] private EventDiceRollPresenter diceRollPresenter;
    [SerializeField, Min(0.01f)] private float diceUiFadeDuration = 0.25f;

    [Header("Event_01 Result")]
    [Tooltip("주사위 스탯 팝업이 끝난 뒤 Event_01_A/B/C 결과 Title로 넘어가기까지 기다리는 시간입니다.")]
    [SerializeField, Min(0f)] private float event01StatResultDelay = 0.9f;

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
    [SerializeField, SoundId(SoundCategory.Sfx)] private string acquireSfxId = AudioIds.Sfx.RelicChoiceAcquire;

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
    private Coroutine eventRewardPanelDelayRoutine;
    private Coroutine dustiumAcquireRoutine;
    private int pendingDustiumAcquireAmount;
    private bool hasDustiumAcquireOriginalState;
    private Vector3 dustiumAcquireOriginalWorldPosition;
    private Vector3 dustiumAcquireOriginalLocalScale;
    private readonly List<BattleRewardData> pendingEventRewards = new();
    private readonly EventChoiceSessionState eventChoiceSessionState = new();
    private readonly List<EventData> persistentEventChoices = new();
    private readonly List<EventChoiceEquippedRelicCost> equippedRelicCostOptions = new();
    private readonly List<EventSkillAwakenSelectionPanelEntry> skillAwakenOptions = new();
    private EventData pendingEquippedRelicCostChoice;
    private bool isSelectingEquippedRelicCost;
    private EventData pendingSkillAwakenChoice;
    private bool isSelectingSkillAwaken;
    private Coroutine diceRollRoutine;
    private Coroutine diceTransitionRoutine;
    private Coroutine event01ResultContinuationRoutine;
    private Coroutine event02ResultContinuationRoutine;
    private Coroutine event04ResultContinuationRoutine;
    private Coroutine event01RewardTransitionRoutine;
    private EventData pendingDiceChoice;
    private int[] pendingDiceFaces = System.Array.Empty<int>();
    private Coroutine event02ChestTransitionRoutine;
    private Coroutine event04ChoiceTransitionRoutine;
    private Coroutine event04ResultTitleFadeRoutine;
    private string pendingEvent01ResultEventId;
    private string pendingEvent01ResultMessage;
    private string pendingEvent02ResultEventId;
    private string pendingEvent02ResultMessage;
    private string pendingEvent04ResultEventId;
    private string pendingEvent04ResultMessage;
    private EventData activeRewardChoice;
    private CanvasGroup eventTitleCanvasGroup;
    private Coroutine eventChoiceScrollMoveRoutine;
    private Coroutine skillAwakenTransitionRoutine;
    private Coroutine skillAwakenResultRoutine;
    private bool hasPendingSkillAwakenResult;
    private EventData pendingSkillAwakenResultChoice;
    private EventChoiceExecutionResult pendingSkillAwakenExecutionResult;
    private EventChoiceSkillAwakenTarget pendingSkillAwakenResultTarget;
    private CanvasGroup eventChoiceScrollCanvasGroup;
    private CanvasGroup eventChoiceGradationCanvasGroup;
    private Coroutine terminalChoiceFadeRoutine;
    private bool waitForEventEntranceReveal;

    private void Awake()
    {
        EnsureReferences();
        ApplyBackgroundSorting();
        BindNextButton();
        CacheRelicFlyRootOriginalState();
        EnsureDustiumAcquireReferences();
        CacheDustiumAcquireOriginalState();
        ResetDustiumAcquireVisual();
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

        HideDiceRollPresenterImmediate();
        StopEventRewardPanelDelay();
        StopDustiumAcquireAnimation(true);
        pendingDustiumAcquireAmount = 0;
        StopEvent01RewardTransition();
        StopEvent02ChestTransition();
        StopEvent04ChoiceTransition();
        StopEvent04ResultTitleFade();
        ClearPendingEvent01ResultContinuation();
        ClearPendingEvent02ResultContinuation();
        ClearPendingEvent04ResultContinuation();
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
        persistentEventChoices.Clear();
        ClearEquippedRelicCostSelection();
        ClearSkillAwakenSelection();
        eventChoiceSessionState.AwakenedSkillTargets.Clear();
        SetNextButtonVisible(false);
        waitForEventEntranceReveal = true;
        ResetEventChoiceScrollViewPosition();
        ResetTerminalChoiceVisuals();

        if (TryStartDataEventMode())
            return;

        SetDataEventRootVisible(false);
        SetChestRootVisible(true);
        BindChestEvents();
    }

    private void OnDisable()
    {
        UnbindChestEvents();
        UnbindEvent06ShopClose();

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);

        if (relicAcquireRoutine != null)
        {
            StopCoroutine(relicAcquireRoutine);
            relicAcquireRoutine = null;
        }

        HideDiceRollPresenterImmediate();
        StopEventRewardPanelDelay();
        StopDustiumAcquireAnimation(true);
        pendingDustiumAcquireAmount = 0;
        StopEvent01RewardTransition();
        StopEvent02ChestTransition();
        StopEvent04ChoiceTransition();
        StopEvent04ResultTitleFade();
        ClearPendingEvent01ResultContinuation();
        ClearPendingEvent02ResultContinuation();
        ClearPendingEvent04ResultContinuation();
        StopEventChoiceScrollViewAnimation();
        StopSkillAwakenResultRoutine();
        StopTerminalChoiceFade();
        HideRelicHoverInfo();
        HideRelicFlyObjects();
        ClearChoiceSlots();
        SetDataEventRootVisible(false);
        isDataEventActive = false;
        isEventRewardPanelOpen = false;
        pendingEventRewards.Clear();
        persistentEventChoices.Clear();
        ClearEquippedRelicCostSelection();
        ClearSkillAwakenSelection();
        eventChoiceSessionState.AwakenedSkillTargets.Clear();
        SetNextButtonVisible(false);
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

            if (isSelectingEquippedRelicCost)
            {
                CancelEquippedRelicCostSelection();
                return;
            }

            if (isSelectingSkillAwaken)
            {
                CancelSkillAwakenSelection();
                return;
            }

            // Event_06_A는 상점 종료 후 표시되는 최종 결과 단계입니다.
            // 상점 전용 코루틴 상태와 관계없이 Next를 누르면 즉시 현재 노드를 완료하고 지도로 복귀합니다.
            if (IsEvent06TitleOnlyTerminal(currentEventDefinition?.EventId))
            {
                HideDiceRollPresenterImmediate();
                SetEventTitleVisible(false);
                CompleteCurrentNode();
                ReturnToMap(() => SetNextButtonVisible(false));
                return;
            }

            if (!isEventResolved)
            {
                if (!EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(currentEventDefinition))
                    return;

                isEventResolved = true;
            }

            HideDiceRollPresenterImmediate();

            if (IsEvent05CommitTerminal(currentEventDefinition?.EventId))
            {
                if (dustiumAcquireRoutine == null)
                    dustiumAcquireRoutine = StartCoroutine(CommitEvent05AccumulatedDustiumAndExit());
                return;
            }

            if (pendingEventRewards.Count > 0 && TryOpenPendingEventRewardPanel(false))
                return;

            // 종료 전환 이미지보다 이벤트 타이틀이 앞에 보이지 않도록 먼저 숨깁니다.
            SetEventTitleVisible(false);

            CompleteCurrentNode();
            ReturnToMap(() => SetNextButtonVisible(false));
            return;
        }

        if (!isChestOpened)
            return;

        if (chestOpenButton != null && chestOpenButton.IsAwaitingRewardSelection && !isRelicClaimed)
            return;

        CompleteCurrentNode();

        HideDiceRollPresenterImmediate();
        ReturnToMap(() => SetNextButtonVisible(false));
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
            relicHoverDescText.text = GameDataLocalization.RelicEffectDescription(relicData);

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
        // 같은 데이터 이벤트를 다시 조우해도 이전 연출 상태가 남지 않도록 먼저 초기화합니다.
        StopEvent01RewardTransition();
        StopEvent02ChestTransition();
        StopEvent04ChoiceTransition();
        StopEvent04ResultTitleFade();
        if (diceTransitionRoutine != null)
        {
            StopCoroutine(diceTransitionRoutine);
            diceTransitionRoutine = null;
        }
        HideDiceRollPresenterImmediate();
        StopEventRewardPanelDelay();
        ClearPendingEvent01ResultContinuation();
        ClearPendingEvent02ResultContinuation();
        ClearPendingEvent04ResultContinuation();
        isEventRewardPanelOpen = false;
        pendingEventRewards.Clear();

        waitForEventEntranceReveal = true;
        ResetEventChoiceScrollViewPosition();
        ResetEventChoiceScrollViewVisualState();
        ResetTerminalChoiceVisuals();
        ClearChoiceSlots();
        persistentEventChoices.Clear();
        currentEventDefinition = null;
        isDataEventActive = false;
        isEventResolved = false;
        ClearEquippedRelicCostSelection();
        ClearSkillAwakenSelection();
        eventChoiceSessionState.AwakenedSkillTargets.Clear();

        // Choice 실행 후 다음 Event의 선택지를 표시하는 checkpoint는 Map node의 최초 EventId가 아니라
        // Resume에 저장된 다음 EventId를 기준으로 definition을 먼저 구성해야 한다.
        if (SaveSystem.Instance != null &&
            SaveSystem.Instance.TryGetPendingResumeData(out ResumeData resume) &&
            resume != null &&
            (resume.Phase == ResumePhase.EventDice ||
             (resume.Phase == ResumePhase.EventChoice &&
              resume.Presentation == ResumePresentation.ChoiceList)) &&
            !string.IsNullOrWhiteSpace(resume.EventId))
        {
            pendingEventId = EventIdUtility.Normalize(resume.EventId);
        }

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
        if (TryRestorePendingEventResume())
            return true;

        if (!HasPendingEventResume())
            SaveEventResume(ResumePhase.EventEntry, null, default, null);
        return true;
    }

    private bool TryRestorePendingEventResume()
    {
        if (SaveSystem.Instance == null ||
            !SaveSystem.Instance.TryGetPendingResumeData(out ResumeData resume) ||
            resume == null ||
            !string.Equals(EventIdUtility.Normalize(resume.EventId),
                EventIdUtility.Normalize(currentEventDefinition?.EventId), System.StringComparison.Ordinal))
        {
            return false;
        }

        Debug.Log($"[EventResume] Restore phase:{resume.Phase} event:{resume.EventId} current:{currentEventDefinition?.EventId} choice:{resume.SelectedChoiceId}", this);

        // EventEntry는 LoadEventDefinition이 이미 정상 입력 화면을 구성했다.
        // 복원 중 autosave만 막고, 이후 사용자의 선택/Dice checkpoint는 저장 가능해야 한다.
        if (resume.Phase == ResumePhase.EventEntry)
        {
            SaveSystem.Instance.ClearPendingResumeData();
            SaveSystem.Instance.CompleteCheckpointAutosaveRestore();
            return true;
        }

        if (resume.Phase == ResumePhase.EventDice)
        {
            if (!int.TryParse(resume.SelectedChoiceId, out int order))
            {
                Debug.LogWarning($"[EventResume] Dice restore skipped: invalid ChoiceOrder '{resume.SelectedChoiceId}'.", this);
                return false;
            }

            EventData choice = FindChoiceByOrder(GetCurrentVisibleChoices(), order);
            if (choice == null || resume.DiceFaces == null || resume.DiceFaces.Length == 0)
            {
                Debug.LogWarning($"[EventResume] Dice restore skipped: choice:{choice != null}, faces:{resume.DiceFaces?.Length ?? 0}.", this);
                return false;
            }

            EnsureDiceRollPresenter();
            if (diceRollPresenter == null)
            {
                Debug.LogWarning("[EventResume] Dice restore skipped: presenter missing.", this);
                return false;
            }

            ResetEventResumeTransientPresentation();
            SetChoiceSlotsInteractable(false);
            SetNextButtonVisible(false);
            diceRollPresenter.ShowResolved(
                resume.DiceFaces,
                BuildDiceDetailText(choice, SumDiceFaces(resume.DiceFaces)),
                () => ExecuteEventChoice(choice, forcedDiceFaces: resume.DiceFaces));
            Debug.Log($"[EventResume] Dice restored: presenterActive:{diceRollPresenter.gameObject.activeInHierarchy} faces:{string.Join(",", resume.DiceFaces)}.", this);
            SaveSystem.Instance.ClearPendingResumeData();
            // 복원 자체는 저장하지 않되, 이후 사용자의 Roll/확인 입력은 새 checkpoint를 만들 수 있어야 한다.
            SaveSystem.Instance.CompleteCheckpointAutosaveRestore();
            return true;
        }

        if (resume.Phase == ResumePhase.EventChoice && resume.ChoiceResultApplied)
        {
            // ChoiceList는 이미 다음 EventDefinition을 LoadEventDefinition에서 bind한 입력 화면이다.
            // 결과 presentation baseline을 적용하면 choice scroll이 닫히므로 별도 복원한다.
            if (resume.Presentation == ResumePresentation.ChoiceList)
            {
                isEventResolved = false;
                BindChoiceSlots(ResolveSavedVisibleChoices(resume));
                SetChoiceSlotsInteractable(true);
                SetNextButtonVisible(EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(currentEventDefinition));
                SaveSystem.Instance.ClearPendingResumeData();
                SaveSystem.Instance.CompleteCheckpointAutosaveRestore();
                return true;
            }

            ResetEventResumeTransientPresentation();

            if (resume.Presentation == ResumePresentation.Shop)
            {
                RestRoomShopPanel restoredShop = ResolveShopPanel();
                if (restoredShop == null || resume.ShopGoods == null || resume.ShopGoods.Count == 0)
                    return false;

                shopPanel = restoredShop;
                UnbindEvent06ShopClose();
                shopPanel.Closed += OnEvent06ShopClosed;
                waitingForEvent06ShopClose = true;
                event06NextEventId = EventIdUtility.Normalize(resume.NextEventId);
                restoredShop.OpenSavedStock(resume.ShopGoods);
                SaveSystem.Instance.ClearPendingResumeData();
                SaveSystem.Instance.CompleteCheckpointAutosaveRestore();
                return true;
            }

            pendingEventRewards.Clear();
            if (resume.PendingRewards != null)
            {
                for (int i = 0; i < resume.PendingRewards.Count; i++)
                {
                    BattleRewardSaveData reward = resume.PendingRewards[i];
                    if (reward != null)
                    {
                        pendingEventRewards.Add(new BattleRewardData
                        {
                            Type = reward.Type,
                            RewardId = reward.RewardId,
                            Amount = reward.Amount
                        });
                    }
                }
            }

            isEventResolved = true;
            SetChoiceSlotsInteractable(false);
            SetNextButtonVisible(false);
            HideEventChoicePresentation();
            if (pendingEventRewards.Count > 0)
                TryOpenPendingEventRewardPanel(false, false);
            else
            {
                if (eventResultText != null)
                    eventResultText.text = resume.ResultMessage ?? string.Empty;
                SetNextButtonVisible(true);
            }

            SaveSystem.Instance.ClearPendingResumeData();
            return true;
        }

        return false;
    }

    private void HideEventChoicePresentation()
    {
        ClearChoiceSlots();
        if (eventChoiceScrollView != null)
            eventChoiceScrollView.gameObject.SetActive(false);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(false);
    }

    private void ResetEventResumeTransientPresentation()
    {
        StopEventRewardPanelDelay();
        HideEventChoicePresentation();
        HideDiceRollPresenterImmediate();
        ClearEquippedRelicCostSelection();
        ClearSkillAwakenSelection();
        isEventRewardPanelOpen = false;
        SetNextButtonVisible(false);
        if (eventResultText != null)
            eventResultText.text = string.Empty;
    }

    private static bool HasPendingEventResume()
    {
        if (SaveSystem.Instance == null ||
            !SaveSystem.Instance.TryGetPendingResumeData(out ResumeData resume))
        {
            return false;
        }

        return resume.Phase == ResumePhase.EventEntry ||
               resume.Phase == ResumePhase.EventChoice ||
               resume.Phase == ResumePhase.EventDice;
    }

    private void LoadEventDefinition(EventDefinition definition, string resultMessage)
    {
        if (definition == null)
            return;

        EnsureDataEventReferences();
        HideDiceRollPresenterImmediate();

        currentEventDefinition = definition;
        pendingEventId = definition.EventId;
        isDataEventActive = true;
        isEventResolved = false;
        ClearEquippedRelicCostSelection();
        ClearSkillAwakenSelection();

        SetDataEventRootVisible(true);
        SetNextButtonVisible(EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(definition));
        ResetTerminalChoiceVisuals();

        if (eventNameText != null)
            eventNameText.text = string.IsNullOrWhiteSpace(definition.EventName)
                ? definition.EventId
                : definition.EventName;

        if (eventTitleText != null)
            eventTitleText.text = ResolveEventTitle(definition);

        if (eventResultText != null)
            eventResultText.text = resultMessage ?? string.Empty;

        if (IsEvent01TitleOnlyTerminal(definition.EventId) || IsEvent02TitleOnlyTerminal(definition.EventId) ||
            IsEvent04TitleOnlyTerminal(definition.EventId) || IsEvent05TitleOnlyTerminal(definition.EventId) ||
            IsEvent06TitleOnlyTerminal(definition.EventId) || IsEvent08TitleOnlyTerminal(definition.EventId))
        {
            // 결과 Title만 보여주는 종료 상태입니다.
            if (IsEvent08TitleOnlyTerminal(definition.EventId))
                persistentEventChoices.Clear();

            BindChoiceSlots(null);
            isEventResolved = true;
            SetNextButtonVisible(true);
            SetEventTitleVisible(true);
            return;
        }

        BindChoiceSlots(GetCurrentVisibleChoices());
    }

    private static bool IsEvent01TitleOnlyTerminal(string eventId)
    {
        string normalized = EventIdUtility.Normalize(eventId);
        return normalized == "Event_01_A" || normalized == "Event_01_B" || normalized == "Event_01_C";
    }


    private static bool IsEvent02TitleOnlyTerminal(string eventId)
    {
        string normalized = EventIdUtility.Normalize(eventId);
        return normalized == "Event_02_B" || normalized == "Event_02_C" ||
               normalized == "Event_02_D" || normalized == "Event_02_E" ||
               normalized == "Event_02_G" || normalized == "Event_02_H" ||
               normalized == "Event_02_I";
    }

    private static bool IsEvent04TitleOnlyTerminal(string eventId)
    {
        string normalized = EventIdUtility.Normalize(eventId);
        return normalized == "Event_04_A" || normalized == "Event_04_B" ||
               normalized == "Event_04_C" || normalized == "Event_04_D";
    }

    private static bool IsEvent06TitleOnlyTerminal(string eventId)
    {
        return EventIdUtility.Normalize(eventId) == "Event_06_A";
    }

    private static bool IsEvent08TitleOnlyTerminal(string eventId)
    {
        string normalized = EventIdUtility.Normalize(eventId);
        return normalized == "Event_08_C" || normalized == "Event_08_D";
    }

    private static bool IsEvent05TitleOnlyTerminal(string eventId)
    {
        string normalized = EventIdUtility.Normalize(eventId);
        return normalized == "Event_05_C" || normalized == "Event_05_D" || normalized == "Event_05_E";
    }

    private static bool IsEvent05CommitTerminal(string eventId)
    {
        string normalized = EventIdUtility.Normalize(eventId);
        return normalized == "Event_05_C" || normalized == "Event_05_E";
    }

    private static string ResolveEventTitle(EventDefinition definition)
    {
        if (definition == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(definition.Title))
            return definition.Title;

        return EventIdUtility.Normalize(definition.EventId) switch
        {
            "Event_01_A" => "“조금이나마 도움이 되기를 바랍니다. 부디 조심해서 사용해 주세요.”",
            "Event_01_B" => "“상처가 조금은 나아졌군요. 이 힘이 당신들의 여정에 보탬이 되기를 바랍니다.”",
            "Event_01_C" => "“당신들의 생명에 축복이 머물기를. 앞으로의 길이 조금은 덜 고되기를 바랍니다.”",
            "Event_02_A" => "한 번 더 손을 뻗을 수 있을 것 같다.",
            "Event_02_B" => "의식 도구에 남아 있던 힘이 몸 안으로 스며든다.",
            "Event_02_C" => "흔적을 헤집자 안쪽에서 강한 힘을 머금은 유물이 모습을 드러냈다.",
            "Event_02_D" => "의식 도구에 손을 대는 순간 불길한 기운이 역류한다.",
            "Event_02_E" => "흔적을 훼손한 순간 억눌려 있던 힘이 폭발한다.",
            "Event_02_G" => "더 이상 손대지 않기로 하고, 조용히 자리를 떠난다.",
            "Event_02_H" => "뜻밖의 수확을 얻었다. 더 이상 손댈 것은 없어 보인다.",
            "Event_02_I" => "예상치 못한 폭발에 휘말렸다.",
            "Event_04_A" => "“좋아. 대가는 충분하군. 약속한 물건은 여기 있다.”",
            "Event_04_B" => "“제법 운이 좋았군. 이 물건은 네 몫이다.”",
            "Event_04_C" => "“나쁘지 않은 거래였어.”",
            "Event_04_D" => "“값을 치를 차례다. 모자란 몫은 생명으로 받아가겠다.”",
            _ => string.Empty
        };
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

        if (EventChoiceExecutionService.RequiresEquippedRelicCostSelection(choice))
        {
            BeginEquippedRelicCostSelectionWithTransition(choice);
            return;
        }

        if (EventChoiceExecutionService.RequiresSkillAwakenSelection(choice))
        {
            BeginSkillAwakenSelection(choice);
            return;
        }

        if (IsDiceChoice(choice) && TryBeginDiceRollChoice(choice))
            return;

        if (ShouldFadeEvent01RewardChoiceUi(choice))
        {
            BeginEvent01RewardChoiceWithTransition(choice);
            return;
        }

        if (ShouldTransitionEvent02ChestChoice(choice))
        {
            BeginEvent02ChestChoiceTransition(choice);
            return;
        }

        if (ShouldFadeEvent04ChoiceUi(choice))
        {
            BeginEvent04ChoiceWithTransition(choice);
            return;
        }

        ExecuteEventChoice(choice);
    }

    private static bool ShouldFadeEvent01RewardChoiceUi(EventData choice)
    {
        if (choice == null)
            return false;

        return EventIdUtility.Normalize(choice.EventId) == "Event_01" &&
               choice.ChoiceOrder == 1 &&
               SameToken(choice.ResultType, "GainRandom") &&
               SameToken(choice.ResultTarget, "유물") &&
               EventIdUtility.Normalize(choice.NextEventId) == "Event_01_A";
    }

    private void BeginEvent01RewardChoiceWithTransition(EventData choice)
    {
        if (choice == null || !isActiveAndEnabled)
            return;

        StopEvent01RewardTransition();
        SetChoiceSlotsInteractable(false);
        SetNextButtonVisible(false);
        event01RewardTransitionRoutine = StartCoroutine(FadeOutEventChoiceUiForEvent01Reward(choice));
    }

    private IEnumerator FadeOutEventChoiceUiForEvent01Reward(EventData choice)
    {
        yield return FadeOutEventChoiceUiForDice();

        event01RewardTransitionRoutine = null;
        if (choice != null && isActiveAndEnabled)
            ExecuteEventChoice(choice);
    }

    private void StopEvent01RewardTransition()
    {
        if (event01RewardTransitionRoutine == null)
            return;

        StopCoroutine(event01RewardTransitionRoutine);
        event01RewardTransitionRoutine = null;
    }

    private static bool ShouldFadeEvent04ChoiceUi(EventData choice)
    {
        if (choice == null)
            return false;

        return EventIdUtility.Normalize(choice.EventId) == "Event_04" && !IsDiceChoice(choice);
    }

    private void BeginEvent04ChoiceWithTransition(EventData choice)
    {
        if (choice == null || !isActiveAndEnabled)
            return;

        StopEvent04ChoiceTransition();
        SetChoiceSlotsInteractable(false);
        SetNextButtonVisible(false);
        event04ChoiceTransitionRoutine = StartCoroutine(FadeOutEvent04ChoiceUiThenExecute(choice));
    }

    private IEnumerator FadeOutEvent04ChoiceUiThenExecute(EventData choice)
    {
        yield return FadeOutEventChoiceUiForDice();

        event04ChoiceTransitionRoutine = null;
        if (choice != null && isActiveAndEnabled)
            ExecuteEventChoice(choice);
    }

    private void StopEvent04ChoiceTransition()
    {
        if (event04ChoiceTransitionRoutine == null)
            return;

        StopCoroutine(event04ChoiceTransitionRoutine);
        event04ChoiceTransitionRoutine = null;
    }

    private static bool ShouldTransitionEvent02ChestChoice(EventData choice)
    {
        if (choice == null)
            return false;

        return EventIdUtility.Normalize(choice.EventId) == "Event_02" &&
               choice.ChoiceOrder == 1 &&
               SameToken(choice.ResultType, "Gain") &&
               SameToken(choice.ResultTarget, "레드 더스티움") &&
               EventIdUtility.Normalize(choice.NextEventId) == "Event_02_A";
    }

    private void BeginEvent02ChestChoiceTransition(EventData choice)
    {
        if (choice == null || !isActiveAndEnabled)
            return;

        StopEvent02ChestTransition();
        SetChoiceSlotsInteractable(false);
        SetNextButtonVisible(false);
        event02ChestTransitionRoutine = StartCoroutine(TransitionEvent02ChestChoice(choice));
    }

    private IEnumerator TransitionEvent02ChestChoice(EventData choice)
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();
        EnsureEventTitleCanvasGroup();

        if (eventChoiceScrollView != null && !eventChoiceScrollView.gameObject.activeSelf)
            eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null && !eventChoiceGradation.gameObject.activeSelf)
            eventChoiceGradation.gameObject.SetActive(true);
        if (eventTitleText != null && !eventTitleText.gameObject.activeSelf)
            eventTitleText.gameObject.SetActive(true);

        CanvasGroup scrollGroup = eventChoiceScrollCanvasGroup;
        CanvasGroup gradationGroup = eventChoiceGradationCanvasGroup;
        CanvasGroup titleGroup = eventTitleCanvasGroup;
        if (scrollGroup != null)
        {
            scrollGroup.interactable = false;
            scrollGroup.blocksRaycasts = false;
        }
        if (gradationGroup != null)
        {
            gradationGroup.interactable = false;
            gradationGroup.blocksRaycasts = false;
        }
        if (titleGroup != null)
        {
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
        }

        float duration = Mathf.Max(0.01f, diceUiFadeDuration);
        float elapsed = 0f;
        float scrollStart = scrollGroup != null ? scrollGroup.alpha : 1f;
        float gradationStart = gradationGroup != null ? gradationGroup.alpha : 1f;
        float titleStart = titleGroup != null ? titleGroup.alpha : 1f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (scrollGroup != null) scrollGroup.alpha = Mathf.Lerp(scrollStart, 0f, t);
            if (gradationGroup != null) gradationGroup.alpha = Mathf.Lerp(gradationStart, 0f, t);
            if (titleGroup != null) titleGroup.alpha = Mathf.Lerp(titleStart, 0f, t);
            yield return null;
        }

        if (scrollGroup != null) scrollGroup.alpha = 0f;
        if (gradationGroup != null) gradationGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (eventChoiceScrollView != null) eventChoiceScrollView.gameObject.SetActive(false);
        if (eventChoiceGradation != null) eventChoiceGradation.gameObject.SetActive(false);
        if (eventTitleText != null) eventTitleText.gameObject.SetActive(false);

        // 선택 결과에 레드 더스티움 연출이 포함되면 Event_02_A 로드는
        // Dustium -> GoldHud 연출이 끝난 뒤 ContinueAfterExecutedChoiceCore에서 이루어집니다.
        ExecuteEventChoice(choice);

        if (dustiumAcquireRoutine != null)
            yield return dustiumAcquireRoutine;

        if (!isActiveAndEnabled || !isDataEventActive ||
            currentEventDefinition == null ||
            EventIdUtility.Normalize(currentEventDefinition.EventId) != "Event_02_A")
        {
            event02ChestTransitionRoutine = null;
            yield break;
        }

        // Event_02_A는 종료 상태가 아니므로 NextButton을 띄우지 않습니다.
        SetNextButtonVisible(false);

        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();
        EnsureEventTitleCanvasGroup();
        if (eventChoiceScrollView != null)
            eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(true);
        if (eventTitleText != null)
            eventTitleText.gameObject.SetActive(true);

        scrollGroup = eventChoiceScrollCanvasGroup;
        gradationGroup = eventChoiceGradationCanvasGroup;
        titleGroup = eventTitleCanvasGroup;
        if (scrollGroup != null) scrollGroup.alpha = 0f;
        if (gradationGroup != null) gradationGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (scrollGroup != null) scrollGroup.alpha = t;
            if (gradationGroup != null) gradationGroup.alpha = t;
            if (titleGroup != null) titleGroup.alpha = t;
            yield return null;
        }

        if (scrollGroup != null)
        {
            scrollGroup.alpha = 1f;
            scrollGroup.interactable = true;
            scrollGroup.blocksRaycasts = true;
        }
        if (gradationGroup != null)
        {
            gradationGroup.alpha = 1f;
            gradationGroup.interactable = false;
            gradationGroup.blocksRaycasts = false;
        }
        if (titleGroup != null)
        {
            titleGroup.alpha = 1f;
            titleGroup.interactable = true;
            titleGroup.blocksRaycasts = true;
        }

        event02ChestTransitionRoutine = null;
    }

    private void StopEvent02ChestTransition()
    {
        if (event02ChestTransitionRoutine == null)
            return;

        StopCoroutine(event02ChestTransitionRoutine);
        event02ChestTransitionRoutine = null;
    }

    private void ExecuteEventChoice(
        EventData choice,
        EventChoiceEquippedRelicCost selectedEquippedRelicCost = default,
        EventChoiceSkillAwakenTarget selectedSkillAwakenTarget = default,
        int[] forcedDiceFaces = null)
    {
        SetChoiceSlotsInteractable(false);

        int accumulatedRemnantBeforeChoice = eventChoiceSessionState.AccumulatedRemnant;

        EventChoiceExecutionResult result;
        activeRewardChoice = choice;
        try
        {
            result = EventChoiceExecutionService.Execute(
                choice,
                CreateExecutionContext(selectedEquippedRelicCost, selectedSkillAwakenTarget, forcedDiceFaces));
        }
        finally
        {
            activeRewardChoice = null;
        }

        if (!result.Accepted)
        {
            if (eventResultText != null)
                eventResultText.text = result.ResultMessage;

            ClearEquippedRelicCostSelection();
            ClearSkillAwakenSelection();
            BindChoiceSlots(GetCurrentVisibleChoices());
            if (forcedDiceFaces != null || ShouldFadeEvent01RewardChoiceUi(choice))
                RestoreEventChoiceUiAfterDice();
            return;
        }

        ClearEquippedRelicCostSelection();

        // Execute가 성공한 시점에는 비용·확률·보상 후보가 Runtime에 확정되어 있다.
        // checkpoint는 아래에서 실제 다음 presentation이 확정된 직후에만 만든다.
        PersistEventRuntime();

        bool playSkillAwakenResult =
            selectedSkillAwakenTarget.IsValid &&
            EventChoiceExecutionService.RequiresSkillAwakenSelection(choice);

        if (playSkillAwakenResult)
        {
            hasPendingSkillAwakenResult = true;
            pendingSkillAwakenResultChoice = choice;
            pendingSkillAwakenExecutionResult = result;
            pendingSkillAwakenResultTarget = selectedSkillAwakenTarget;
            ClearSkillAwakenSelection();

            if (skillAwakenSelectionPanel == null || !skillAwakenSelectionPanel.IsOpen)
                StartPendingSkillAwakenResult();
            return;
        }

        ClearSkillAwakenSelection();

        if (IsEvent05MiningChoice(choice) && result.Accepted)
        {
            if (dustiumAcquireRoutine != null)
                StopCoroutine(dustiumAcquireRoutine);

            dustiumAcquireRoutine = StartCoroutine(
                PlayEvent05AccumulatedDustiumThenContinue(
                    choice,
                    result,
                    accumulatedRemnantBeforeChoice,
                    forcedDiceFaces));
            return;
        }

        if (IsEvent05DeferredExitChoice(choice) && result.Accepted)
        {
            if (dustiumAcquireRoutine != null)
                StopCoroutine(dustiumAcquireRoutine);

            dustiumAcquireRoutine = StartCoroutine(
                PlayEvent05ExitVisualThenContinue(choice, result, forcedDiceFaces));
            return;
        }

        ContinueAfterExecutedChoice(choice, result, forcedDiceFaces);
    }

    private void ContinueAfterExecutedChoice(
        EventData choice,
        EventChoiceExecutionResult result,
        int[] resolvedDiceFaces = null)
    {
        // 이벤트 결과 오브젝트 연출을 항상 보상 UI보다 먼저 시작합니다.
        bool playedEvent06OpenVisual = false;
        if (EventIdUtility.Normalize(choice?.EventId) == "Event_06" && result.Accepted)
        {
            // OpenPanel은 EventChoiceExecutionService에서 먼저 처리되므로,
            // 상점 종료에 필요한 다음 이벤트/되돌림 연출 정보는 실행 결과가 확정된 여기서 저장합니다.
            event06NextEventId = EventIdUtility.Normalize(result.NextEventId);
            // 상점이 닫힐 때는 별도의 failure 액션을 쓰지 않고,
            // 방금 재생한 success 애니메이션 자체를 역재생합니다.
            event06CloseVisualObjectId = NormalizeVisualId(choice.SuccessVisualObjectId);
            event06CloseVisualActionId = NormalizeVisualId(choice.SuccessVisualActionId);

            string visualObjectId = NormalizeVisualId(choice.SuccessVisualObjectId);
            string visualActionId = NormalizeVisualId(choice.SuccessVisualActionId);
            if (!string.IsNullOrEmpty(visualObjectId) && !string.IsNullOrEmpty(visualActionId))
            {
                PlayVisualActionById(visualObjectId, visualActionId);
                playedEvent06OpenVisual = true;
            }
        }

        if (!playedEvent06OpenVisual)
            PlayVisualAction(result);

        if (pendingEvent06ShopOpen)
            StartEvent06ShopOpenAfterVisualAction(playedEvent06OpenVisual || result.HasVisualAction);

        if (pendingDustiumAcquireAmount > 0)
        {
            if (dustiumAcquireRoutine != null)
                StopCoroutine(dustiumAcquireRoutine);

            dustiumAcquireRoutine = StartCoroutine(
                PlayPendingDustiumAcquireThenContinue(choice, result, resolvedDiceFaces));
            return;
        }

        ContinueAfterExecutedChoiceCore(choice, result, resolvedDiceFaces);
    }

    private void ContinueAfterExecutedChoiceCore(
        EventData choice,
        EventChoiceExecutionResult result,
        int[] resolvedDiceFaces = null)
    {
        TryShowEventStatResultPopup(choice, result, resolvedDiceFaces);

        // Event_06 상점은 패널이 닫힐 때까지 이벤트 진행을 보류합니다.
        // Close 알림을 받으면 Event_06_A 결과 Title로 전환합니다.
        if (waitingForEvent06ShopClose)
        {
            SetNextButtonVisible(false);
            SetChoiceSlotsInteractable(false);
            return;
        }

        if (TryQueueEvent01ResultContinuation(choice, result, resolvedDiceFaces))
            return;

        if (TryQueueEvent02ResultContinuation(choice, result, resolvedDiceFaces))
            return;

        if (TryQueueEvent04ResultContinuation(choice, result, resolvedDiceFaces))
            return;

        bool shouldCompleteAfterFailedChoice =
            EventRoomRewardFlowUtility.ShouldCompleteAfterFailedChoice(choice, result);
        bool hasContinuingEvent = false;

        if (!shouldCompleteAfterFailedChoice &&
            !string.IsNullOrWhiteSpace(result.NextEventId) &&
            DataManager.Instance != null &&
            DataManager.Instance.EventDatabase != null &&
            DataManager.Instance.EventDatabase.TryGetEvent(result.NextEventId, out EventDefinition nextDefinition) &&
            nextDefinition != null)
        {
            hasContinuingEvent = true;
            LoadEventDefinition(nextDefinition, result.ResultMessage);
            SaveEventResume(
                ResumePhase.EventChoice,
                choice,
                result,
                resolvedDiceFaces,
                ResumePresentation.ChoiceList,
                nextDefinition.EventId,
                result.ResultMessage);

            if (resolvedDiceFaces != null)
                RestoreEventChoiceUiAfterDice();

            return;
        }

        if (!shouldCompleteAfterFailedChoice && !string.IsNullOrWhiteSpace(result.NextEventId))
        {
            Debug.LogWarning(
                $"[EventRoomController] Next event '{result.NextEventId}' not found. Treating this choice as terminal.",
                this);
        }

        BeginTerminalChoiceExitVisuals();

        if (eventResultText != null)
            eventResultText.text = result.ResultMessage;

        isEventResolved = true;

        if (EventRoomRewardFlowUtility.ShouldOpenPendingRewards(result, pendingEventRewards.Count, hasContinuingEvent) &&
            TryOpenPendingEventRewardPanel(true))
        {
            return;
        }

        SetNextButtonVisible(true);
        SaveEventResume(
            ResumePhase.EventChoice,
            choice,
            result,
            resolvedDiceFaces,
            ResumePresentation.ResultOnly,
            null,
            result.ResultMessage);
    }

    private bool TryQueueEvent01ResultContinuation(
        EventData choice,
        EventChoiceExecutionResult result,
        IReadOnlyList<int> resolvedDiceFaces)
    {
        string nextEventId = EventIdUtility.Normalize(result.NextEventId);
        if (!IsEvent01TitleOnlyTerminal(nextEventId))
            return false;

        pendingEvent01ResultEventId = nextEventId;
        pendingEvent01ResultMessage = result.ResultMessage ?? string.Empty;
        SetNextButtonVisible(false);
        SetChoiceSlotsInteractable(false);

        // 1번 선택지는 유물 RewardPanel이 닫힌 뒤 결과 Title을 보여줍니다.
        if (pendingEventRewards.Count > 0 && TryOpenPendingEventRewardPanel(true))
            return true;

        // 2/3번 선택지는 BattleDamageTextPopupUI 연출이 끝난 뒤 B/C Title로 넘어갑니다.
        float delay = resolvedDiceFaces != null ? Mathf.Max(0f, event01StatResultDelay) : 0f;
        QueueEvent01ResultContinuation(delay);
        return true;
    }

    private void QueueEvent01ResultContinuation(float delay)
    {
        if (event01ResultContinuationRoutine != null)
            StopCoroutine(event01ResultContinuationRoutine);

        event01ResultContinuationRoutine = StartCoroutine(CompletePendingEvent01ResultContinuationAfterDelay(delay));
    }

    private IEnumerator CompletePendingEvent01ResultContinuationAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        event01ResultContinuationRoutine = null;
        CompletePendingEvent01ResultContinuation();
    }

    private bool CompletePendingEvent01ResultContinuation()
    {
        string nextEventId = pendingEvent01ResultEventId;
        string resultMessage = pendingEvent01ResultMessage;
        pendingEvent01ResultEventId = string.Empty;
        pendingEvent01ResultMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(nextEventId) ||
            DataManager.Instance == null ||
            DataManager.Instance.EventDatabase == null ||
            !DataManager.Instance.EventDatabase.TryGetEvent(nextEventId, out EventDefinition nextDefinition) ||
            nextDefinition == null)
        {
            return false;
        }

        LoadEventDefinition(nextDefinition, resultMessage);
        return true;
    }

    private void ClearPendingEvent01ResultContinuation()
    {
        if (event01ResultContinuationRoutine != null)
        {
            StopCoroutine(event01ResultContinuationRoutine);
            event01ResultContinuationRoutine = null;
        }

        pendingEvent01ResultEventId = string.Empty;
        pendingEvent01ResultMessage = string.Empty;
    }

    private bool TryQueueEvent02ResultContinuation(
        EventData choice,
        EventChoiceExecutionResult result,
        IReadOnlyList<int> resolvedDiceFaces)
    {
        string nextEventId = EventIdUtility.Normalize(result.NextEventId);
        if (!IsEvent02TitleOnlyTerminal(nextEventId))
            return false;

        pendingEvent02ResultEventId = nextEventId;
        pendingEvent02ResultMessage = result.ResultMessage ?? string.Empty;
        SetNextButtonVisible(false);
        SetChoiceSlotsInteractable(false);

        // 유물 보상은 RewardPanel이 완전히 닫힌 뒤 결과 Title로 넘어갑니다.
        if (pendingEventRewards.Count > 0 && TryOpenPendingEventRewardPanel(true))
            return true;

        // 스탯 성공/실패는 BattleDamageTextPopupUI 연출 뒤 B/D 또는 C/E Title을 표시합니다.
        float delay = resolvedDiceFaces != null ? Mathf.Max(0f, event01StatResultDelay) : 0f;
        QueueEvent02ResultContinuation(delay);
        return true;
    }

    private void QueueEvent02ResultContinuation(float delay)
    {
        if (event02ResultContinuationRoutine != null)
            StopCoroutine(event02ResultContinuationRoutine);

        event02ResultContinuationRoutine = StartCoroutine(CompletePendingEvent02ResultContinuationAfterDelay(delay));
    }

    private IEnumerator CompletePendingEvent02ResultContinuationAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        event02ResultContinuationRoutine = null;
        CompletePendingEvent02ResultContinuation();
    }

    private bool CompletePendingEvent02ResultContinuation()
    {
        string nextEventId = pendingEvent02ResultEventId;
        string resultMessage = pendingEvent02ResultMessage;
        pendingEvent02ResultEventId = string.Empty;
        pendingEvent02ResultMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(nextEventId) ||
            DataManager.Instance == null ||
            DataManager.Instance.EventDatabase == null ||
            !DataManager.Instance.EventDatabase.TryGetEvent(nextEventId, out EventDefinition nextDefinition) ||
            nextDefinition == null)
        {
            return false;
        }

        LoadEventDefinition(nextDefinition, resultMessage);
        return true;
    }

    private void ClearPendingEvent02ResultContinuation()
    {
        if (event02ResultContinuationRoutine != null)
        {
            StopCoroutine(event02ResultContinuationRoutine);
            event02ResultContinuationRoutine = null;
        }

        pendingEvent02ResultEventId = string.Empty;
        pendingEvent02ResultMessage = string.Empty;
    }

    private bool TryQueueEvent04ResultContinuation(
        EventData choice,
        EventChoiceExecutionResult result,
        IReadOnlyList<int> resolvedDiceFaces)
    {
        string nextEventId = EventIdUtility.Normalize(result.NextEventId);
        if (!IsEvent04TitleOnlyTerminal(nextEventId))
            return false;

        pendingEvent04ResultEventId = nextEventId;
        pendingEvent04ResultMessage = result.ResultMessage ?? string.Empty;
        SetNextButtonVisible(false);
        SetChoiceSlotsInteractable(false);

        // 유물 획득/교환은 RewardPanel이 닫힌 뒤 A/B/C 결과 Title을 표시합니다.
        if (pendingEventRewards.Count > 0 && TryOpenPendingEventRewardPanel(true))
            return true;

        // 주사위 실패는 생명력 감소 팝업이 끝난 뒤 D 결과 Title을 표시합니다.
        float delay = resolvedDiceFaces != null ? Mathf.Max(0f, event01StatResultDelay) : 0f;
        QueueEvent04ResultContinuation(delay);
        return true;
    }

    private void QueueEvent04ResultContinuation(float delay)
    {
        if (event04ResultContinuationRoutine != null)
            StopCoroutine(event04ResultContinuationRoutine);

        event04ResultContinuationRoutine = StartCoroutine(CompletePendingEvent04ResultContinuationAfterDelay(delay));
    }

    private IEnumerator CompletePendingEvent04ResultContinuationAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        event04ResultContinuationRoutine = null;
        CompletePendingEvent04ResultContinuation();
    }

    private bool CompletePendingEvent04ResultContinuation()
    {
        string nextEventId = pendingEvent04ResultEventId;
        string resultMessage = pendingEvent04ResultMessage;
        pendingEvent04ResultEventId = string.Empty;
        pendingEvent04ResultMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(nextEventId) ||
            DataManager.Instance == null ||
            DataManager.Instance.EventDatabase == null ||
            !DataManager.Instance.EventDatabase.TryGetEvent(nextEventId, out EventDefinition nextDefinition) ||
            nextDefinition == null)
        {
            return false;
        }

        LoadEventDefinition(nextDefinition, resultMessage);
        StartEvent04ResultTitleFade();
        return true;
    }

    private void StartEvent04ResultTitleFade()
    {
        StopEvent04ResultTitleFade();
        if (isActiveAndEnabled)
            event04ResultTitleFadeRoutine = StartCoroutine(FadeInEvent04ResultTitle());
    }

    private IEnumerator FadeInEvent04ResultTitle()
    {
        EnsureEventTitleCanvasGroup();
        if (eventTitleText == null)
        {
            event04ResultTitleFadeRoutine = null;
            yield break;
        }

        eventTitleText.gameObject.SetActive(true);
        CanvasGroup titleGroup = eventTitleCanvasGroup;
        if (titleGroup == null)
        {
            event04ResultTitleFadeRoutine = null;
            yield break;
        }

        titleGroup.alpha = 0f;
        titleGroup.interactable = false;
        titleGroup.blocksRaycasts = false;

        float duration = Mathf.Max(0.01f, diceUiFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            titleGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        titleGroup.alpha = 1f;
        titleGroup.interactable = true;
        titleGroup.blocksRaycasts = true;
        event04ResultTitleFadeRoutine = null;
    }

    private void StopEvent04ResultTitleFade()
    {
        if (event04ResultTitleFadeRoutine == null)
            return;

        StopCoroutine(event04ResultTitleFadeRoutine);
        event04ResultTitleFadeRoutine = null;
    }

    private void ClearPendingEvent04ResultContinuation()
    {
        if (event04ResultContinuationRoutine != null)
        {
            StopCoroutine(event04ResultContinuationRoutine);
            event04ResultContinuationRoutine = null;
        }

        pendingEvent04ResultEventId = string.Empty;
        pendingEvent04ResultMessage = string.Empty;
    }

    [Header("Event Stat Popup")]
    [SerializeField, Min(1f)] private float eventStatPopupFontSize = 48f;

    private void TryShowEventStatResultPopup(
        EventData choice,
        EventChoiceExecutionResult result,
        IReadOnlyList<int> resolvedDiceFaces)
    {
        TryShowEvent07StatGainPopup(choice);

        if (choice == null || resolvedDiceFaces == null || !IsDiceChoice(choice))
            return;

        Transform centerPoint = FindEventAllyPoint1();
        if (centerPoint == null)
            return;

        Canvas eventCanvas = GetComponentInParent<Canvas>(true);
        if (eventCanvas == null || !eventCanvas.gameObject.activeInHierarchy)
            eventCanvas = GetComponentInChildren<Canvas>(true);

        int diceRoll = SumDiceFaces(resolvedDiceFaces);

        if (result.Succeeded)
        {
            if (SameToken(choice.ResultType, "RollTable"))
            {
                string tableId = choice.ResultValue?.Trim();
                if (SameToken(tableId, "RT001"))
                {
                    int amount = diceRoll <= 8 ? 3 : diceRoll <= 15 ? 5 : 10;
                    BattleDamageTextPopupUI.ShowEventHealthRecovery(centerPoint, amount, eventCanvas, eventStatPopupFontSize);
                    return;
                }

                if (SameToken(tableId, "RT002"))
                {
                    int amount = diceRoll <= 8 ? 2 : diceRoll <= 15 ? 4 : 8;
                    BattleDamageTextPopupUI.ShowEventMaxHealthGain(centerPoint, amount, eventCanvas, eventStatPopupFontSize);
                    return;
                }
            }

            if (SameToken(choice.ResultType, "Modify") &&
                (Contains(choice.ResultTarget, "코스트 회복량") || Contains(choice.ResultTarget, "마나 재생량")) &&
                TryParseSignedValue(choice.ResultValue, out int manaRegenAmount) && manaRegenAmount > 0)
            {
                BattleDamageTextPopupUI.ShowEventManaRegenGain(centerPoint, manaRegenAmount, eventCanvas, eventStatPopupFontSize);
            }

            return;
        }

        Match healthLossMatch = Regex.Match(
            choice.FailResult ?? string.Empty,
            @"현재\s*체력\s*([+-]?\d+)",
            RegexOptions.CultureInvariant);

        if (healthLossMatch.Success &&
            int.TryParse(healthLossMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int healthLoss))
        {
            healthLoss = Mathf.Abs(healthLoss);
            if (healthLoss > 0)
                BattleDamageTextPopupUI.ShowEventHealthLoss(centerPoint, healthLoss, eventCanvas, eventStatPopupFontSize);
        }
    }

    private void TryShowEvent07StatGainPopup(EventData choice)
    {
        if (choice == null || EventIdUtility.Normalize(choice.EventId) != "Event_07")
            return;

        if (!TryParseSignedValue(choice.ResultValue, out int amount) || amount <= 0)
            return;

        Transform centerPoint = FindEventAllyPoint1();
        if (centerPoint == null)
        {
            Debug.LogWarning("[EventRoomController] Event_07 팝업 위치 EventAllyPoint1을 찾지 못했습니다.", this);
            return;
        }

        Canvas eventCanvas = GetComponentInParent<Canvas>(true);
        if (eventCanvas == null || !eventCanvas.gameObject.activeInHierarchy)
            eventCanvas = GetComponentInChildren<Canvas>(true);

        if (Contains(choice.ResultTarget, "최대 체력") || Contains(choice.ResultTarget, "최대 생명력"))
        {
            BattleDamageTextPopupUI.ShowEventMaxHealthGain(centerPoint, amount, eventCanvas, eventStatPopupFontSize);
            return;
        }

        if (Contains(choice.ResultTarget, "최대 코스트") || Contains(choice.ResultTarget, "최대 마나"))
            BattleDamageTextPopupUI.ShowEventMaxManaGain(centerPoint, amount, eventCanvas, eventStatPopupFontSize);
    }

    private Transform FindEventAllyPoint1()
    {
        Transform point = FindChildRecursive(transform, "EventAllyPoint1");
        if (point != null && point.gameObject.activeInHierarchy)
            return point;

        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null &&
                candidate.gameObject.activeInHierarchy &&
                candidate.name == "EventAllyPoint1")
            {
                return candidate;
            }
        }

        return null;
    }

    private bool TryBeginDiceRollChoice(EventData choice)
    {
        EnsureDiceRollPresenter();
        if (diceRollPresenter == null || !isActiveAndEnabled)
            return false;

        if (diceTransitionRoutine != null)
            StopCoroutine(diceTransitionRoutine);

        SetChoiceSlotsInteractable(false);
        SetNextButtonVisible(false);
        pendingDiceChoice = choice;
        pendingDiceFaces = RollThreeSixSidedDiceFaces();
        diceTransitionRoutine = StartCoroutine(OpenDiceRollPresenterWithTransition(choice));
        return true;
    }

    private IEnumerator OpenDiceRollPresenterWithTransition(EventData choice)
    {
        yield return FadeOutEventChoiceUiForDice();

        if (choice == null || diceRollPresenter == null)
        {
            diceTransitionRoutine = null;
            RestoreEventChoiceUiAfterDice();
            yield break;
        }

        diceRollPresenter.PrepareForInteractiveUse();
        if (!diceRollPresenter.IsReady)
        {
            Debug.LogWarning("[EventRoomController] EventDiceRollPresenter의 주사위 이미지 또는 RollButton 참조가 준비되지 않았습니다.", this);
            diceRollPresenter.HideImmediate();
            diceTransitionRoutine = null;
            RestoreEventChoiceUiAfterDice();
            yield break;
        }

        int[] diceFaces = pendingDiceFaces;
        int diceRoll = SumDiceFaces(diceFaces);
        string detailText = BuildDiceDetailText(choice, diceRoll);
        diceRollPresenter.ShowInteractive(
            diceFaces,
            detailText,
            () =>
            {
                diceTransitionRoutine = null;
                pendingDiceChoice = null;
                pendingDiceFaces = System.Array.Empty<int>();
                ExecuteEventChoice(choice, forcedDiceFaces: diceFaces);
            },
            () => SaveEventResume(ResumePhase.EventDice, choice, default, diceFaces, diceRollResolved: true));

        diceTransitionRoutine = null;
    }

    private IEnumerator FadeOutEventChoiceUiForDice()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();
        EnsureEventTitleCanvasGroup();

        CanvasGroup scrollGroup = eventChoiceScrollCanvasGroup;
        CanvasGroup gradationGroup = eventChoiceGradationCanvasGroup;
        CanvasGroup titleGroup = eventTitleCanvasGroup;

        if (eventChoiceScrollView != null && !eventChoiceScrollView.gameObject.activeSelf)
            eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null && !eventChoiceGradation.gameObject.activeSelf)
            eventChoiceGradation.gameObject.SetActive(true);
        if (eventTitleText != null && !eventTitleText.gameObject.activeSelf)
            eventTitleText.gameObject.SetActive(true);

        if (scrollGroup != null)
        {
            scrollGroup.interactable = false;
            scrollGroup.blocksRaycasts = false;
        }
        if (gradationGroup != null)
        {
            gradationGroup.interactable = false;
            gradationGroup.blocksRaycasts = false;
        }
        if (titleGroup != null)
        {
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
        }

        float duration = Mathf.Max(0.01f, diceUiFadeDuration);
        float elapsed = 0f;
        float scrollStart = scrollGroup != null ? scrollGroup.alpha : 1f;
        float gradationStart = gradationGroup != null ? gradationGroup.alpha : 1f;
        float titleStart = titleGroup != null ? titleGroup.alpha : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (scrollGroup != null) scrollGroup.alpha = Mathf.Lerp(scrollStart, 0f, t);
            if (gradationGroup != null) gradationGroup.alpha = Mathf.Lerp(gradationStart, 0f, t);
            if (titleGroup != null) titleGroup.alpha = Mathf.Lerp(titleStart, 0f, t);
            yield return null;
        }

        if (scrollGroup != null) scrollGroup.alpha = 0f;
        if (gradationGroup != null) gradationGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (eventChoiceScrollView != null) eventChoiceScrollView.gameObject.SetActive(false);
        if (eventChoiceGradation != null) eventChoiceGradation.gameObject.SetActive(false);
        if (eventTitleText != null) eventTitleText.gameObject.SetActive(false);
    }

    private void RestoreEventChoiceUiAfterDice()
    {
        if (!isActiveAndEnabled || !isDataEventActive || isEventResolved)
            return;

        if (diceTransitionRoutine != null)
            StopCoroutine(diceTransitionRoutine);

        diceTransitionRoutine = StartCoroutine(FadeInEventChoiceUiAfterDice());
    }

    private IEnumerator FadeInEventChoiceUiAfterDice()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();
        EnsureEventTitleCanvasGroup();

        if (eventChoiceScrollView != null)
            eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(true);
        if (eventTitleText != null)
            eventTitleText.gameObject.SetActive(true);

        CanvasGroup scrollGroup = eventChoiceScrollCanvasGroup;
        CanvasGroup gradationGroup = eventChoiceGradationCanvasGroup;
        CanvasGroup titleGroup = eventTitleCanvasGroup;
        if (scrollGroup != null) scrollGroup.alpha = 0f;
        if (gradationGroup != null) gradationGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;

        float duration = Mathf.Max(0.01f, diceUiFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (scrollGroup != null) scrollGroup.alpha = t;
            if (gradationGroup != null) gradationGroup.alpha = t;
            if (titleGroup != null) titleGroup.alpha = t;
            yield return null;
        }

        if (scrollGroup != null)
        {
            scrollGroup.alpha = 1f;
            scrollGroup.interactable = true;
            scrollGroup.blocksRaycasts = true;
        }
        if (gradationGroup != null)
        {
            gradationGroup.alpha = 1f;
            gradationGroup.interactable = false;
            gradationGroup.blocksRaycasts = false;
        }
        if (titleGroup != null)
        {
            titleGroup.alpha = 1f;
            titleGroup.interactable = true;
            titleGroup.blocksRaycasts = true;
        }

        BindChoiceSlots(GetCurrentVisibleChoices());
        diceTransitionRoutine = null;
    }

    private void EnsureEventTitleCanvasGroup()
    {
        if (eventTitleText == null)
            return;

        if (eventTitleCanvasGroup == null || eventTitleCanvasGroup.gameObject != eventTitleText.gameObject)
        {
            eventTitleCanvasGroup = eventTitleText.GetComponent<CanvasGroup>();
            if (eventTitleCanvasGroup == null)
                eventTitleCanvasGroup = eventTitleText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private string BuildDiceDetailText(EventData choice, int diceRoll)
    {
        if (choice == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(choice.SuccessCondition))
            return IsDiceSuccess(diceRoll, choice.SuccessCondition) ? "성공" : "실패";

        if (!SameToken(choice.ResultType, "RollTable"))
            return BuildResultSummary(choice);

        string tableId = choice.ResultValue?.Trim();
        if (SameToken(tableId, "RT001"))
        {
            int amount = diceRoll <= 8 ? 3 : diceRoll <= 15 ? 5 : 10;
            return $"생명력 회복량 +{amount}";
        }

        if (SameToken(tableId, "RT002"))
        {
            int amount = diceRoll <= 8 ? 2 : diceRoll <= 15 ? 4 : 8;
            return $"최대 생명력 증가량 +{amount}";
        }

        return BuildResultSummary(choice);
    }

    private static int SumDiceFaces(IReadOnlyList<int> diceFaces)
    {
        if (diceFaces == null)
            return 0;

        int total = 0;
        for (int i = 0; i < diceFaces.Count; i++)
            total += Mathf.Clamp(diceFaces[i], 1, 6);
        return total;
    }

    private void StopDiceRollRoutine()
    {
        if (diceRollRoutine == null)
            return;

        StopCoroutine(diceRollRoutine);
        diceRollRoutine = null;
    }

    private void HideDiceRollPresenterImmediate()
    {
        StopDiceRollRoutine();
        HideDiceRollPresenterOnly();
    }

    private void HideDiceRollPresenterOnly()
    {
        diceRollPresenter?.HideImmediate();
    }

    private void BeginEquippedRelicCostSelectionWithTransition(EventData choice)
    {
        if (choice == null || !isActiveAndEnabled)
            return;

        StopEvent04ChoiceTransition();
        SetChoiceSlotsInteractable(false);
        SetNextButtonVisible(false);
        event04ChoiceTransitionRoutine = StartCoroutine(FadeOutEventChoiceUiThenOpenEquippedRelicSelection(choice));
    }

    private IEnumerator FadeOutEventChoiceUiThenOpenEquippedRelicSelection(EventData choice)
    {
        yield return FadeOutEventChoiceUiForDice();

        event04ChoiceTransitionRoutine = null;
        if (choice != null && isActiveAndEnabled)
            BeginEquippedRelicCostSelection(choice);
    }

    private void RestoreEventChoiceUiAfterRelicSelection()
    {
        if (!isActiveAndEnabled || !isDataEventActive || isEventResolved)
            return;

        StopEvent04ChoiceTransition();
        event04ChoiceTransitionRoutine = StartCoroutine(FadeInEventChoiceUiAfterRelicSelection());
    }

    private IEnumerator FadeInEventChoiceUiAfterRelicSelection()
    {
        yield return FadeInEventChoiceUiAfterDice();
        event04ChoiceTransitionRoutine = null;
    }

    private void BeginEquippedRelicCostSelection(EventData choice)
    {
        RefreshEquippedRelicCostOptions();

        if (equippedRelicCostOptions.Count == 0)
        {
            if (eventResultText != null)
                eventResultText.text = "장착 중인 유물이 없습니다.";

            ClearEquippedRelicCostSelection();
            BindChoiceSlots(GetCurrentVisibleChoices());
            RestoreEventChoiceUiAfterRelicSelection();
            return;
        }

        EnsureEquippedRelicSelectionPanel();
        if (equippedRelicSelectionPanel == null)
        {
            if (eventResultText != null)
                eventResultText.text = "장착 유물 선택 패널을 열 수 없습니다.";

            ClearEquippedRelicCostSelection();
            BindChoiceSlots(GetCurrentVisibleChoices());
            RestoreEventChoiceUiAfterRelicSelection();
            return;
        }

        pendingEquippedRelicCostChoice = choice;
        isSelectingEquippedRelicCost = true;
        SetNextButtonVisible(false);
        SetChoiceSlotsInteractable(false);

        if (eventResultText != null)
            eventResultText.text = "삭제할 장착 유물을 선택하세요.";

        bool openedSelectionPanel = equippedRelicSelectionPanel.Open(
            equippedRelicCostOptions,
            CreateEquippedRelicSelectionEntry,
            OnEquippedRelicCostSelected,
            CancelEquippedRelicCostSelection);
        if (!openedSelectionPanel)
        {
            if (eventResultText != null)
                eventResultText.text = "장착 유물 선택 패널이 씬에 올바르게 배치되지 않았습니다.";

            ClearEquippedRelicCostSelection();
            BindChoiceSlots(GetCurrentVisibleChoices());
            SetNextButtonVisible(EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(currentEventDefinition));
            RestoreEventChoiceUiAfterRelicSelection();
        }
    }

    private void CancelEquippedRelicCostSelection()
    {
        ClearEquippedRelicCostSelection();
        BindChoiceSlots(GetCurrentVisibleChoices());
        SetNextButtonVisible(EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(currentEventDefinition));

        if (eventResultText != null)
            eventResultText.text = string.Empty;

        RestoreEventChoiceUiAfterRelicSelection();
    }

    private bool OnEquippedRelicCostSelected(EventChoiceEquippedRelicCost cost)
    {
        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return false;

        if (!isSelectingEquippedRelicCost || pendingEquippedRelicCostChoice == null)
            return false;

        ExecuteEventChoice(pendingEquippedRelicCostChoice, cost);
        return true;
    }

    private void RefreshEquippedRelicCostOptions()
    {
        equippedRelicCostOptions.Clear();

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId))
                continue;

            RelicEquipService.EnsureRelicSlots(character);

            for (int slotIndex = 0; slotIndex < character.EquippedRelicIds.Length; slotIndex++)
            {
                // 연성제 슬롯은 일반 유물 교환 대상이 아닙니다.
                if (slotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex)
                    continue;

                string relicId = character.EquippedRelicIds[slotIndex]?.Trim();
                if (string.IsNullOrWhiteSpace(relicId))
                    continue;

                equippedRelicCostOptions.Add(new EventChoiceEquippedRelicCost(
                    character.CharacterId,
                    slotIndex,
                    relicId));
            }
        }
    }

    private void ClearEquippedRelicCostSelection()
    {
        isSelectingEquippedRelicCost = false;
        pendingEquippedRelicCostChoice = null;
        equippedRelicCostOptions.Clear();

        if (equippedRelicSelectionPanel != null)
            equippedRelicSelectionPanel.Close();
    }

    private EventEquippedRelicSelectionPanelEntry CreateEquippedRelicSelectionEntry(
        EventChoiceEquippedRelicCost cost)
    {
        return new EventEquippedRelicSelectionPanelEntry(
            cost,
            GetCharacterDisplayName(cost.CharacterId),
            GetRelicSlotDisplayName(cost.RelicSlotIndex),
            GetRelicDisplayName(cost.RelicId),
            GetRelicSprite(cost.RelicId));
    }

    private void BeginSkillAwakenSelection(EventData choice)
    {
        RefreshSkillAwakenOptions();

        if (skillAwakenOptions.Count == 0)
        {
            if (eventResultText != null)
                eventResultText.text = "강화 가능한 장착 기억이 없습니다.";

            ClearSkillAwakenSelection();
            BindChoiceSlots(GetCurrentVisibleChoices());
            return;
        }

        EnsureSkillAwakenSelectionPanel();
        if (skillAwakenSelectionPanel == null)
        {
            if (eventResultText != null)
                eventResultText.text = "기억 강화 선택 패널을 찾을 수 없습니다.";

            ClearSkillAwakenSelection();
            BindChoiceSlots(GetCurrentVisibleChoices());
            return;
        }

        pendingSkillAwakenChoice = choice;
        isSelectingSkillAwaken = true;
        SetNextButtonVisible(false);
        SetChoiceSlotsInteractable(false);

        if (eventResultText != null)
            eventResultText.text = "강화할 장착 기억을 선택하세요.";

        if (skillAwakenTransitionRoutine != null)
            StopCoroutine(skillAwakenTransitionRoutine);

        skillAwakenTransitionRoutine = StartCoroutine(OpenSkillAwakenPanelWithTransition());
    }


    private IEnumerator OpenSkillAwakenPanelWithTransition()
    {
        yield return FadeOutEventChoiceScrollForSkillAwaken();

        if (!isSelectingSkillAwaken || pendingSkillAwakenChoice == null || skillAwakenSelectionPanel == null)
        {
            skillAwakenTransitionRoutine = null;
            yield break;
        }

        // 기억 강화 선택 패널이 열리는 동안에는 기존 이벤트 제목을 숨깁니다.
        SetEventTitleVisible(false);

        bool openedSelectionPanel = skillAwakenSelectionPanel.Open(
            skillAwakenOptions,
            OnSkillAwakenSelected,
            CancelSkillAwakenSelection,
            OnSkillAwakenPanelClosed);

        if (!openedSelectionPanel)
        {
            SetEventTitleVisible(true);
            if (eventResultText != null)
                eventResultText.text = "기억 강화 선택 패널이 씬에 올바르게 배치되지 않았습니다.";

            ClearSkillAwakenSelection();
            BindChoiceSlots(GetCurrentVisibleChoices());
            SetNextButtonVisible(EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(currentEventDefinition));
            RestoreEventChoiceScrollAfterSkillAwaken();
        }

        skillAwakenTransitionRoutine = null;
    }

    private IEnumerator FadeOutEventChoiceScrollForSkillAwaken()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();

        if (eventChoiceScrollView == null || eventChoiceScrollCanvasGroup == null)
            yield break;

        if (!eventChoiceScrollView.gameObject.activeSelf)
            eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null && !eventChoiceGradation.gameObject.activeSelf)
            eventChoiceGradation.gameObject.SetActive(true);

        eventChoiceScrollCanvasGroup.interactable = false;
        eventChoiceScrollCanvasGroup.blocksRaycasts = false;

        float scrollStart = eventChoiceScrollCanvasGroup.alpha;
        float gradationStart = eventChoiceGradationCanvasGroup != null ? eventChoiceGradationCanvasGroup.alpha : 0f;
        yield return FadeEventChoiceScrollAndGradation(
            scrollStart, 0f,
            gradationStart, 0f,
            skillAwakenScrollFadeDuration);

        eventChoiceScrollView.gameObject.SetActive(false);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(false);
    }

    private void OnSkillAwakenPanelClosed()
    {
        if (hasPendingSkillAwakenResult)
        {
            // 성공/실패 결과 연출이 끝나고 다음 이벤트가 로드될 때까지 제목을 숨긴 상태로 유지합니다.
            StartPendingSkillAwakenResult();
            return;
        }

        // 선택 취소 등 결과 연출 없이 패널만 닫힌 경우에는 현재 이벤트 제목을 다시 표시합니다.
        SetEventTitleVisible(true);
        RestoreEventChoiceScrollAfterSkillAwaken();
    }

    private void SetEventTitleVisible(bool visible)
    {
        if (!visible)
        {
            if (eventTitleText != null)
                eventTitleText.gameObject.SetActive(false);
            return;
        }

        ShowEventTitleImmediate();
    }

    private void ShowEventTitleImmediate()
    {
        if (eventTitleText == null)
            return;

        eventTitleText.gameObject.SetActive(true);
        EnsureEventTitleCanvasGroup();
        if (eventTitleCanvasGroup != null)
        {
            eventTitleCanvasGroup.alpha = 1f;
            eventTitleCanvasGroup.interactable = true;
            eventTitleCanvasGroup.blocksRaycasts = true;
        }
    }

    private void StartPendingSkillAwakenResult()
    {
        if (!hasPendingSkillAwakenResult || !isActiveAndEnabled)
            return;

        if (skillAwakenResultRoutine != null)
        {
            StopCoroutine(skillAwakenResultRoutine);
            skillAwakenResultRoutine = null;
        }

        skillAwakenResultRoutine = StartCoroutine(PlayPendingSkillAwakenResultRoutine());
    }

    private IEnumerator PlayPendingSkillAwakenResultRoutine()
    {
        EventData choice = pendingSkillAwakenResultChoice;
        EventChoiceExecutionResult result = pendingSkillAwakenExecutionResult;
        EventChoiceSkillAwakenTarget target = pendingSkillAwakenResultTarget;

        if (skillAwakenSelectionPanel != null)
            yield return skillAwakenSelectionPanel.PlayResultSkill(target, result.Succeeded);

        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultChoice = null;
        pendingSkillAwakenExecutionResult = default;
        pendingSkillAwakenResultTarget = default;
        skillAwakenResultRoutine = null;

        ContinueAfterExecutedChoice(choice, result);

        if (!isEventResolved && isDataEventActive)
            RestoreEventChoiceScrollAfterSkillAwaken();
    }

    private void StopSkillAwakenResultRoutine()
    {
        if (skillAwakenResultRoutine != null)
        {
            StopCoroutine(skillAwakenResultRoutine);
            skillAwakenResultRoutine = null;
        }

        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultChoice = null;
        pendingSkillAwakenExecutionResult = default;
        pendingSkillAwakenResultTarget = default;
    }

    private void RestoreEventChoiceScrollAfterSkillAwaken()
    {
        if (!isActiveAndEnabled || !isDataEventActive || isEventResolved)
            return;

        if (skillAwakenTransitionRoutine != null)
            StopCoroutine(skillAwakenTransitionRoutine);

        skillAwakenTransitionRoutine = StartCoroutine(FadeInEventChoiceScrollAfterSkillAwaken());
    }

    private IEnumerator FadeInEventChoiceScrollAfterSkillAwaken()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();

        if (eventChoiceScrollView == null || eventChoiceScrollCanvasGroup == null)
        {
            skillAwakenTransitionRoutine = null;
            yield break;
        }

        eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(true);

        eventChoiceScrollCanvasGroup.alpha = 0f;
        eventChoiceScrollCanvasGroup.interactable = false;
        eventChoiceScrollCanvasGroup.blocksRaycasts = false;
        if (eventChoiceGradationCanvasGroup != null)
            eventChoiceGradationCanvasGroup.alpha = 0f;

        yield return FadeEventChoiceScrollAndGradation(
            0f, 1f,
            0f, 1f,
            skillAwakenScrollFadeDuration);

        eventChoiceScrollCanvasGroup.interactable = true;
        eventChoiceScrollCanvasGroup.blocksRaycasts = true;
        skillAwakenTransitionRoutine = null;
    }

    private void EnsureEventChoiceScrollCanvasGroup()
    {
        EnsureEventChoiceScrollViewReference();
        if (eventChoiceScrollView == null)
            return;

        if (eventChoiceScrollCanvasGroup == null || eventChoiceScrollCanvasGroup.gameObject != eventChoiceScrollView.gameObject)
        {
            eventChoiceScrollCanvasGroup = eventChoiceScrollView.GetComponent<CanvasGroup>();
            if (eventChoiceScrollCanvasGroup == null)
                eventChoiceScrollCanvasGroup = eventChoiceScrollView.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void EnsureEventChoiceGradationCanvasGroup()
    {
        EnsureEventChoiceGradationReference();
        if (eventChoiceGradation == null)
            return;

        if (eventChoiceGradationCanvasGroup == null || eventChoiceGradationCanvasGroup.gameObject != eventChoiceGradation.gameObject)
        {
            eventChoiceGradationCanvasGroup = eventChoiceGradation.GetComponent<CanvasGroup>();
            if (eventChoiceGradationCanvasGroup == null)
                eventChoiceGradationCanvasGroup = eventChoiceGradation.gameObject.AddComponent<CanvasGroup>();
        }

        eventChoiceGradationCanvasGroup.interactable = false;
        eventChoiceGradationCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeEventChoiceScrollAndGradation(
        float scrollFrom, float scrollTo,
        float gradationFrom, float gradationTo,
        float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        if (eventChoiceScrollCanvasGroup != null)
            eventChoiceScrollCanvasGroup.alpha = scrollFrom;
        if (eventChoiceGradationCanvasGroup != null)
            eventChoiceGradationCanvasGroup.alpha = gradationFrom;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));

            if (eventChoiceScrollCanvasGroup != null)
                eventChoiceScrollCanvasGroup.alpha = Mathf.Lerp(scrollFrom, scrollTo, t);
            if (eventChoiceGradationCanvasGroup != null)
                eventChoiceGradationCanvasGroup.alpha = Mathf.Lerp(gradationFrom, gradationTo, t);

            yield return null;
        }

        if (eventChoiceScrollCanvasGroup != null)
            eventChoiceScrollCanvasGroup.alpha = scrollTo;
        if (eventChoiceGradationCanvasGroup != null)
            eventChoiceGradationCanvasGroup.alpha = gradationTo;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        group.alpha = to;
    }

    private void CancelSkillAwakenSelection()
    {
        ClearSkillAwakenSelection();
        BindChoiceSlots(GetCurrentVisibleChoices());
        SetNextButtonVisible(EventRoomRewardFlowUtility.CanSkipUnresolvedEvent(currentEventDefinition));

        if (eventResultText != null)
            eventResultText.text = string.Empty;
    }

    private bool OnSkillAwakenSelected(EventChoiceSkillAwakenTarget target)
    {
        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return false;

        if (!isSelectingSkillAwaken || pendingSkillAwakenChoice == null)
            return false;

        ExecuteEventChoice(
            pendingSkillAwakenChoice,
            default,
            target);
        return true;
    }

    private void RefreshSkillAwakenOptions()
    {
        skillAwakenOptions.Clear();

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId))
                continue;

            SkillInventoryEquipService.EnsureEquippedSkillArray(character);
            AddSkillAwakenOption(
                character,
                EventChoiceSkillSlotKind.Passive,
                -1,
                character.PassiveSkillId);
            AddSkillAwakenOption(
                character,
                EventChoiceSkillSlotKind.Unique,
                0,
                character.UniqueSkillId);
            AddSkillAwakenOption(
                character,
                EventChoiceSkillSlotKind.Ability,
                1,
                character.AbilitySkillId);

            if (character.EquippedSkillIds == null)
                continue;

            for (int slotIndex = 2; slotIndex < character.EquippedSkillIds.Length; slotIndex++)
            {
                AddSkillAwakenOption(
                    character,
                    EventChoiceSkillSlotKind.Equipped,
                    slotIndex,
                    character.EquippedSkillIds[slotIndex]);
            }
        }
    }

    private void AddSkillAwakenOption(
        CharacterRuntimeData character,
        EventChoiceSkillSlotKind slotKind,
        int slotIndex,
        string skillId)
    {
        if (character == null || string.IsNullOrWhiteSpace(character.CharacterId) || string.IsNullOrWhiteSpace(skillId))
            return;

        if (!TryGetUpgradeableSkill(skillId, out string normalizedSkillId, out string upgradeSkillId))
            return;

        EventChoiceSkillAwakenTarget target = new(
            character.CharacterId,
            slotKind,
            slotIndex,
            normalizedSkillId,
            upgradeSkillId);
        DataManager.Instance.SkillDatabase.TryGet(normalizedSkillId, out SkillMasterData currentSkill);

        skillAwakenOptions.Add(new EventSkillAwakenSelectionPanelEntry(
            target,
            GetCharacterDisplayName(character.CharacterId),
            GetSkillSlotDisplayName(slotKind, slotIndex),
            GetSkillDisplayName(normalizedSkillId),
            GetSkillDisplayName(upgradeSkillId),
            GetSkillSprite(normalizedSkillId, currentSkill)));
    }

    private void ClearSkillAwakenSelection()
    {
        isSelectingSkillAwaken = false;
        pendingSkillAwakenChoice = null;
        skillAwakenOptions.Clear();

        if (skillAwakenSelectionPanel != null)
            skillAwakenSelectionPanel.Close();
    }

    private bool HasAnyUpgradeableEquippedSkill()
    {
        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null)
                continue;

            SkillInventoryEquipService.EnsureEquippedSkillArray(character);

            if (TryGetUpgradeableSkill(character.PassiveSkillId, out _, out _) ||
                TryGetUpgradeableSkill(character.UniqueSkillId, out _, out _) ||
                TryGetUpgradeableSkill(character.AbilitySkillId, out _, out _))
            {
                return true;
            }

            if (character.EquippedSkillIds == null)
                continue;

            for (int slotIndex = 2; slotIndex < character.EquippedSkillIds.Length; slotIndex++)
            {
                if (TryGetUpgradeableSkill(character.EquippedSkillIds[slotIndex], out _, out _))
                    return true;
            }
        }

        return false;
    }

    private bool TryUpgradeSelectedSkill(
        EventChoiceSkillAwakenTarget target,
        out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!target.IsValid)
        {
            resultMessage = "강화할 기억을 선택해야 합니다.";
            return false;
        }

        CharacterRuntimeStore characterStore = DataManager.Instance?.CharacterRuntimeStore;
        if (characterStore == null ||
            !characterStore.TryGet(target.CharacterId, out CharacterRuntimeData character) ||
            character == null)
        {
            resultMessage = "선택한 캐릭터를 찾을 수 없습니다.";
            return false;
        }

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);

        if (!TryReadSkillFromAwakenTarget(character, target, out string currentSkillId) ||
            !string.Equals(currentSkillId?.Trim(), target.SkillId, System.StringComparison.Ordinal))
        {
            resultMessage = "선택한 기억 장착 상태가 변경되었습니다.";
            return false;
        }

        if (!TryGetUpgradeableSkill(currentSkillId, out _, out string upgradeSkillId) ||
            !string.Equals(upgradeSkillId, target.UpgradeSkillId, System.StringComparison.Ordinal))
        {
            resultMessage = "선택한 기억은 강화할 수 없습니다.";
            return false;
        }

        if (!TryApplySelectedSkillUpgrade(character, target, upgradeSkillId))
        {
            resultMessage = "선택한 기억을 강화하지 못했습니다.";
            return false;
        }

        characterStore.AddOrUpdate(character);
        EquippedSkillPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();

        resultMessage = $"기억 강화: {GetSkillDisplayName(upgradeSkillId)}";
        return true;
    }


    private bool TryRemoveFailedSelectedSkill(
        EventChoiceSkillAwakenTarget target,
        out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!target.IsValid)
        {
            resultMessage = "소멸시킬 기억을 찾지 못했습니다.";
            return false;
        }

        CharacterRuntimeStore characterStore = DataManager.Instance?.CharacterRuntimeStore;
        if (characterStore == null ||
            !characterStore.TryGet(target.CharacterId, out CharacterRuntimeData character) ||
            character == null)
        {
            resultMessage = "선택한 캐릭터를 찾을 수 없습니다.";
            return false;
        }

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);
        if (!TryRemoveCurrentSkill(character, target))
        {
            resultMessage = "실패한 선택 기억을 제거하지 못했습니다.";
            return false;
        }

        characterStore.AddOrUpdate(character);
        EquippedSkillPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();
        resultMessage = $"선택한 기억이 소멸했습니다: {GetSkillDisplayName(target.SkillId)}";
        return true;
    }

    private static bool TryRemoveCurrentSkill(
        CharacterRuntimeData character,
        EventChoiceSkillAwakenTarget target)
    {
        if (character == null || !target.IsValid)
            return false;

        switch (target.SlotKind)
        {
            case EventChoiceSkillSlotKind.Passive:
                if (!IsSameId(character.PassiveSkillId, target.SkillId))
                    return false;

                character.PassiveSkillId = string.Empty;
                return true;

            case EventChoiceSkillSlotKind.Unique:
                return ClearCurrentSpecialSkill(
                    character,
                    target.SkillId,
                    ref character.UniqueSkillId,
                    0);

            case EventChoiceSkillSlotKind.Ability:
                return ClearCurrentSpecialSkill(
                    character,
                    target.SkillId,
                    ref character.AbilitySkillId,
                    1);

            case EventChoiceSkillSlotKind.Equipped:
                if (character.EquippedSkillIds == null ||
                    target.SlotIndex < 0 ||
                    target.SlotIndex >= character.EquippedSkillIds.Length ||
                    !IsSameId(character.EquippedSkillIds[target.SlotIndex], target.SkillId))
                {
                    return false;
                }

                character.EquippedSkillIds[target.SlotIndex] = string.Empty;
                return true;

            default:
                return false;
        }
    }

    private static bool ClearCurrentSpecialSkill(
        CharacterRuntimeData character,
        string skillId,
        ref string specialSkillId,
        int mirroredSlotIndex)
    {
        bool removed = false;

        if (IsSameId(specialSkillId, skillId))
        {
            specialSkillId = string.Empty;
            removed = true;
        }

        if (character.EquippedSkillIds != null &&
            mirroredSlotIndex >= 0 &&
            mirroredSlotIndex < character.EquippedSkillIds.Length &&
            IsSameId(character.EquippedSkillIds[mirroredSlotIndex], skillId))
        {
            character.EquippedSkillIds[mirroredSlotIndex] = string.Empty;
            removed = true;
        }

        return removed;
    }

    private bool TryRollbackAwakenedSkills(
        IReadOnlyList<EventChoiceSkillAwakenTarget> targets,
        out string resultMessage)
    {
        resultMessage = string.Empty;

        if (targets == null || targets.Count == 0)
            return true;

        CharacterRuntimeStore characterStore = DataManager.Instance?.CharacterRuntimeStore;
        if (characterStore == null)
        {
            resultMessage = "이번 이벤트로 얻은 기억을 제거하지 못했습니다.";
            return false;
        }

        int removedCount = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            EventChoiceSkillAwakenTarget target = targets[i];
            if (!target.IsValid ||
                !characterStore.TryGet(target.CharacterId, out CharacterRuntimeData character) ||
                character == null)
            {
                continue;
            }

            SkillInventoryEquipService.EnsureEquippedSkillArray(character);
            if (!TryRemoveAwakenedSkill(character, target))
                continue;

            removedCount++;
            characterStore.AddOrUpdate(character);
        }

        if (removedCount > 0)
        {
            EquippedSkillPanelUI.RefreshAll();
            SkillInventoryPanelUI.RefreshAll();
            resultMessage = $"이번 이벤트로 얻은 기억 {removedCount}개를 잃었습니다.";
        }

        return true;
    }

    private static bool TryRemoveAwakenedSkill(
        CharacterRuntimeData character,
        EventChoiceSkillAwakenTarget target)
    {
        if (character == null || !target.IsValid)
            return false;

        switch (target.SlotKind)
        {
            case EventChoiceSkillSlotKind.Passive:
                if (!IsSameId(character.PassiveSkillId, target.UpgradeSkillId))
                    return false;

                character.PassiveSkillId = string.Empty;
                return true;

            case EventChoiceSkillSlotKind.Unique:
                return ClearSpecialAwakenedSkill(
                    character,
                    target,
                    ref character.UniqueSkillId,
                    0);

            case EventChoiceSkillSlotKind.Ability:
                return ClearSpecialAwakenedSkill(
                    character,
                    target,
                    ref character.AbilitySkillId,
                    1);

            case EventChoiceSkillSlotKind.Equipped:
                if (character.EquippedSkillIds == null ||
                    target.SlotIndex < 0 ||
                    target.SlotIndex >= character.EquippedSkillIds.Length ||
                    !IsSameId(character.EquippedSkillIds[target.SlotIndex], target.UpgradeSkillId))
                {
                    return false;
                }

                character.EquippedSkillIds[target.SlotIndex] = string.Empty;
                return true;

            default:
                return false;
        }
    }

    private static bool ClearSpecialAwakenedSkill(
        CharacterRuntimeData character,
        EventChoiceSkillAwakenTarget target,
        ref string specialSkillId,
        int mirroredSlotIndex)
    {
        bool removed = false;

        if (IsSameId(specialSkillId, target.UpgradeSkillId))
        {
            specialSkillId = string.Empty;
            removed = true;
        }

        if (character.EquippedSkillIds != null &&
            mirroredSlotIndex >= 0 &&
            mirroredSlotIndex < character.EquippedSkillIds.Length &&
            IsSameId(character.EquippedSkillIds[mirroredSlotIndex], target.UpgradeSkillId))
        {
            character.EquippedSkillIds[mirroredSlotIndex] = string.Empty;
            removed = true;
        }

        return removed;
    }

    private bool TryApplySelectedSkillUpgrade(
        CharacterRuntimeData character,
        EventChoiceSkillAwakenTarget target,
        string upgradeSkillId)
    {
        switch (target.SlotKind)
        {
            case EventChoiceSkillSlotKind.Passive:
                if (!IsSameId(character.PassiveSkillId, target.SkillId))
                    return false;

                character.PassiveSkillId = upgradeSkillId;
                return true;

            case EventChoiceSkillSlotKind.Unique:
                if (!IsSameId(character.UniqueSkillId, target.SkillId))
                    return false;

                character.UniqueSkillId = upgradeSkillId;
                ReplaceMirroredEquippedSkill(character, 0, target.SkillId, upgradeSkillId);
                return true;

            case EventChoiceSkillSlotKind.Ability:
                if (!IsSameId(character.AbilitySkillId, target.SkillId))
                    return false;

                character.AbilitySkillId = upgradeSkillId;
                ReplaceMirroredEquippedSkill(character, 1, target.SkillId, upgradeSkillId);
                return true;

            case EventChoiceSkillSlotKind.Equipped:
                if (character.EquippedSkillIds == null ||
                    target.SlotIndex < 0 ||
                    target.SlotIndex >= character.EquippedSkillIds.Length ||
                    !IsSameId(character.EquippedSkillIds[target.SlotIndex], target.SkillId))
                {
                    return false;
                }

                character.EquippedSkillIds[target.SlotIndex] = upgradeSkillId;
                return true;

            default:
                return false;
        }
    }

    private static void ReplaceMirroredEquippedSkill(
        CharacterRuntimeData character,
        int slotIndex,
        string currentSkillId,
        string upgradeSkillId)
    {
        if (character?.EquippedSkillIds == null ||
            slotIndex < 0 ||
            slotIndex >= character.EquippedSkillIds.Length ||
            !IsSameId(character.EquippedSkillIds[slotIndex], currentSkillId))
        {
            return;
        }

        character.EquippedSkillIds[slotIndex] = upgradeSkillId;
    }

    private static bool TryReadSkillFromAwakenTarget(
        CharacterRuntimeData character,
        EventChoiceSkillAwakenTarget target,
        out string skillId)
    {
        skillId = string.Empty;

        if (character == null)
            return false;

        switch (target.SlotKind)
        {
            case EventChoiceSkillSlotKind.Passive:
                skillId = character.PassiveSkillId;
                return true;

            case EventChoiceSkillSlotKind.Unique:
                skillId = character.UniqueSkillId;
                return true;

            case EventChoiceSkillSlotKind.Ability:
                skillId = character.AbilitySkillId;
                return true;

            case EventChoiceSkillSlotKind.Equipped:
                if (character.EquippedSkillIds == null ||
                    target.SlotIndex < 0 ||
                    target.SlotIndex >= character.EquippedSkillIds.Length)
                {
                    return false;
                }

                skillId = character.EquippedSkillIds[target.SlotIndex];
                return true;

            default:
                return false;
        }
    }

    private bool TryGetUpgradeableSkill(
        string skillId,
        out string normalizedSkillId,
        out string upgradeSkillId)
    {
        normalizedSkillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
        upgradeSkillId = string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedSkillId) ||
            SkillRarityUtility.IsUpgradeSkillVariant(normalizedSkillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            !DataManager.Instance.SkillDatabase.TryGet(normalizedSkillId, out SkillMasterData skill) ||
            !SkillRarityUtility.CanUpgrade(skill) ||
            !SkillRarityUtility.TryGetPairedVariantId(normalizedSkillId, out upgradeSkillId) ||
            string.IsNullOrWhiteSpace(upgradeSkillId) ||
            !DataManager.Instance.SkillDatabase.TryGet(upgradeSkillId, out _))
        {
            normalizedSkillId = string.Empty;
            upgradeSkillId = string.Empty;
            return false;
        }

        upgradeSkillId = upgradeSkillId.Trim();
        return true;
    }

    private IReadOnlyList<EventData> GetCurrentVisibleChoices()
    {
        if (currentEventDefinition == null)
            return null;

        string currentEventId = EventIdUtility.Normalize(currentEventDefinition.EventId);

        if (currentEventId == "Event_05" || currentEventId == "Event_05_A" || currentEventId == "Event_05_B")
            return BuildEvent05VisibleChoices(currentEventId);

        IReadOnlyList<EventData> merged = EventChoiceSequenceUtility.MergeChoices(
            currentEventDefinition.Choices,
            persistentEventChoices);

        if (currentEventId != "Event_02_A" ||
            DataManager.Instance == null ||
            DataManager.Instance.EventDatabase == null ||
            !DataManager.Instance.EventDatabase.TryGetEvent("Event_02_F", out EventDefinition exitDefinition) ||
            exitDefinition?.Choices == null)
        {
            return merged;
        }

        // Event_02_A에서는 '한 번 더 연다.'와 Event_02_F의 '자리를 떠난다.'를 함께 표시합니다.
        List<EventData> visible = merged != null ? new List<EventData>(merged) : new List<EventData>();
        for (int i = 0; i < exitDefinition.Choices.Count; i++)
        {
            EventData exitChoice = exitDefinition.Choices[i];
            if (exitChoice == null)
                continue;

            bool alreadyAdded = visible.Exists(x => x != null &&
                x.ChoiceOrder == exitChoice.ChoiceOrder &&
                string.Equals(x.ChoiceName?.Trim(), exitChoice.ChoiceName?.Trim(), System.StringComparison.Ordinal));
            if (!alreadyAdded)
                visible.Add(exitChoice);
        }

        visible.Sort((a, b) => (a?.ChoiceOrder ?? int.MaxValue).CompareTo(b?.ChoiceOrder ?? int.MaxValue));
        return visible;
    }

    private IReadOnlyList<EventData> BuildEvent05VisibleChoices(string currentEventId)
    {
        if (DataManager.Instance?.EventDatabase == null ||
            !DataManager.Instance.EventDatabase.TryGetEvent("Event_05", out EventDefinition baseDefinition) ||
            baseDefinition?.Choices == null)
        {
            return currentEventDefinition.Choices;
        }

        List<EventData> visible = new();

        if (currentEventDefinition.Choices != null)
        {
            for (int i = 0; i < currentEventDefinition.Choices.Count; i++)
            {
                EventData stageChoice = currentEventDefinition.Choices[i];
                if (stageChoice == null || stageChoice.ChoiceOrder < 1 || stageChoice.ChoiceOrder > 2)
                    continue;

                // 각 단계(Event_05 / A / B)의 채굴 판정과 보상은 GameData에 작성된 값을 그대로 사용합니다.
                visible.Add(stageChoice);
            }
        }

        // 최초 진입에서는 그만두기를 표시하지 않습니다.
        // 2/3번째 시도에서는 Event_05의 3번 선택지를 재사용하되 실제 지급은 C의 Next에서 처리합니다.
        if (currentEventId == "Event_05_A" || currentEventId == "Event_05_B")
        {
            EventData exitChoice = FindChoiceByOrder(baseDefinition.Choices, 3);
            if (exitChoice != null)
                visible.Add(CreateEvent05DeferredExitChoice(exitChoice, currentEventId));
        }

        visible.Sort((a, b) => (a?.ChoiceOrder ?? int.MaxValue).CompareTo(b?.ChoiceOrder ?? int.MaxValue));
        return visible;
    }

    private static EventData FindChoiceByOrder(IReadOnlyList<EventData> choices, int choiceOrder)
    {
        if (choices == null)
            return null;

        for (int i = 0; i < choices.Count; i++)
        {
            EventData choice = choices[i];
            if (choice != null && choice.ChoiceOrder == choiceOrder)
                return choice;
        }

        return null;
    }

    private static EventData CreateEvent05DeferredExitChoice(EventData source, string eventId)
    {
        EventData choice = CloneEventChoice(source);
        choice.EventId = eventId;
        choice.ResultType = string.Empty;
        choice.ResultTarget = string.Empty;
        choice.ResultValue = string.Empty;
        return choice;
    }

    private static EventData CloneEventChoice(EventData source)
    {
        if (source == null)
            return null;

        return new EventData
        {
            EventId = source.EventId,
            EventName = source.EventName,
            Title = source.Title,
            ChoiceOrder = source.ChoiceOrder,
            ChoiceName = source.ChoiceName,
            ChoiceDesc = source.ChoiceDesc,
            UnavailableChoiceDesc = source.UnavailableChoiceDesc,
            ChoiceType = source.ChoiceType,
            SelectCondition = source.SelectCondition,
            CostType = source.CostType,
            CostTarget = source.CostTarget,
            CostValue = source.CostValue,
            SuccessCondition = source.SuccessCondition,
            ResultType = source.ResultType,
            ResultTarget = source.ResultTarget,
            ResultValue = source.ResultValue,
            SuccessRate = source.SuccessRate,
            FailResult = source.FailResult,
            NextEventId = source.NextEventId,
            FailNextEventId = source.FailNextEventId,
            PersistAcrossNextEvent = source.PersistAcrossNextEvent,
            SuccessVisualObjectId = source.SuccessVisualObjectId,
            SuccessVisualActionId = source.SuccessVisualActionId,
            FailureVisualObjectId = source.FailureVisualObjectId,
            FailureVisualActionId = source.FailureVisualActionId
        };
    }

    private string GetCharacterDisplayName(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return "캐릭터";

        string normalizedId = characterId.Trim();
        if (DataManager.Instance?.CharacterDatabase != null &&
            DataManager.Instance.CharacterDatabase.TryGet(normalizedId, out CharacterMasterData character) &&
            character != null)
        {
            string displayName = GameDataLocalization.CharacterName(character);
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return normalizedId;
    }

    private string GetRelicDisplayName(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return "유물";

        string normalizedId = relicId.Trim();
        if (DataManager.Instance?.RelicDatabase != null &&
            DataManager.Instance.RelicDatabase.TryGet(normalizedId, out RelicData relic) &&
            relic != null)
        {
            string displayName = GameDataLocalization.RelicName(relic);
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return normalizedId;
    }

    private string GetSkillDisplayName(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return "기억";

        string normalizedId = skillId.Trim();
        if (DataManager.Instance?.SkillDatabase != null &&
            DataManager.Instance.SkillDatabase.TryGet(normalizedId, out SkillMasterData skill) &&
            skill != null)
        {
            string displayName = GameDataLocalization.SkillName(skill);
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return normalizedId;
    }

    private static string GetRelicSlotDisplayName(int slotIndex)
    {
        return slotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex
            ? "액티브 유물 슬롯"
            : $"유물 슬롯 {slotIndex + 1}";
    }

    private static string GetSkillSlotDisplayName(EventChoiceSkillSlotKind slotKind, int slotIndex)
    {
        return slotKind switch
        {
            EventChoiceSkillSlotKind.Passive => "본능 기억",
            EventChoiceSkillSlotKind.Unique => "발현 기억",
            EventChoiceSkillSlotKind.Ability => "구현 기억",
            EventChoiceSkillSlotKind.Equipped => $"장착 기억 {slotIndex + 1}",
            _ => "장착 기억"
        };
    }

    private static string GetSkillTypeRewardDisplayName(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Attack => "공격",
            SkillType.Buff => "버프",
            SkillType.Debuff => "디버프",
            _ => "기억"
        };
    }

    private void PlayVisualAction(EventChoiceExecutionResult result)
    {
        if (!result.HasVisualAction)
            return;

        PlayVisualActionById(result.VisualObjectId, result.VisualActionId);
    }

    private void PlayVisualActionById(string visualObjectId, string visualActionId)
    {
        visualObjectId = NormalizeVisualId(visualObjectId);
        visualActionId = NormalizeVisualId(visualActionId);
        if (string.IsNullOrEmpty(visualObjectId) || string.IsNullOrEmpty(visualActionId))
            return;

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);
        if (sceneController != null)
        {
            if (!sceneController.TryPlaySharedMapVisualAction(visualObjectId, visualActionId))
            {
                Debug.LogWarning(
                    $"[EventRoomController] Visual action not found: {visualObjectId}/{visualActionId}",
                    this);
            }

            return;
        }

        MapVisualController visualController = GetComponent<MapVisualController>();
        if (visualController == null)
            visualController = GetComponentInParent<MapVisualController>();
        if (visualController == null)
            visualController = GetComponentInChildren<MapVisualController>(true);

        if (visualController == null)
        {
            Debug.LogWarning(
                $"[EventRoomController] MapVisualController not found for visual action: {visualObjectId}/{visualActionId}",
                this);
            return;
        }

        if (!visualController.TryPlayAction(visualObjectId, visualActionId))
        {
            Debug.LogWarning(
                $"[EventRoomController] Visual action not found: {visualObjectId}/{visualActionId}",
                this);
        }
    }

    private void ReverseVisualActionById(string visualObjectId, string visualActionId)
    {
        visualObjectId = NormalizeVisualId(visualObjectId);
        visualActionId = NormalizeVisualId(visualActionId);
        if (string.IsNullOrEmpty(visualObjectId) || string.IsNullOrEmpty(visualActionId))
            return;

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);
        if (sceneController != null)
        {
            if (!sceneController.TryReverseSharedMapVisualAction(visualObjectId, visualActionId))
            {
                Debug.LogWarning(
                    $"[EventRoomController] Visual action could not be reversed: {visualObjectId}/{visualActionId}",
                    this);
            }

            return;
        }

        MapVisualController visualController = GetComponent<MapVisualController>();
        if (visualController == null)
            visualController = GetComponentInParent<MapVisualController>();
        if (visualController == null)
            visualController = GetComponentInChildren<MapVisualController>(true);

        if (visualController == null || !visualController.TryReverseAction(visualObjectId, visualActionId))
        {
            Debug.LogWarning(
                $"[EventRoomController] Visual action could not be reversed: {visualObjectId}/{visualActionId}",
                this);
        }
    }

    private string ResolveChoice(EventData choice, out string nextEventId)
    {
        nextEventId = string.Empty;

        int diceRoll = 0;
        bool success = true;
        List<string> messages = new();

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
                if (Contains(choice.ResultTarget, "최대 체력") || Contains(choice.ResultTarget, "최대 생명력"))
                {
                    int count = ModifyPartyMaxHp(amount);
                    return $"파티 전원 최대 체력 {amount:+#;-#;0} 적용 ({count}명)";
                }

                if (Contains(choice.ResultTarget, "코스트 회복") || Contains(choice.ResultTarget, "마나 회복"))
                {
                    int count = ModifyPartyCostRecovery(amount);
                    return $"파티 마나 회복량 {amount:+#;-#;0} 적용 ({count}명)";
                }

                if (Contains(choice.ResultTarget, "최대 코스트") || Contains(choice.ResultTarget, "최대 마나"))
                {
                    int count = ModifyPartyMaxCost(amount);
                    return $"파티 최대 마나 {amount:+#;-#;0} 적용 ({count}명)";
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
            {
                CacheEvent06CloseVisualAction(choice);
                return "상점 패널을 열었습니다.";
            }

            return BuildResultSummary(choice);
        }

        if (SameToken(resultType, "EndEvent"))
            return "이벤트를 종료합니다.";

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

            character.RunMaxHPBonus += amount;
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

            character.RunMaxCostBonus += amount;
            character.MaxCost = Mathf.Max(0, character.MaxCost + amount);
            character.CurrentCost = Mathf.Clamp(character.CurrentCost + Mathf.Max(0, amount), 0, character.MaxCost);
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
        RestRoomShopPanel targetShopPanel = ResolveShopPanel();

        if (targetShopPanel == null)
            return false;

        if (EventIdUtility.Normalize(currentEventDefinition?.EventId) == "Event_06")
        {
            UnbindEvent06ShopClose();
            shopPanel = targetShopPanel;
            shopPanel.Closed += OnEvent06ShopClosed;
            waitingForEvent06ShopClose = true;
            pendingEvent06ShopOpen = true;
            SetNextButtonVisible(false);
            SetChoiceSlotsInteractable(false);
            return true;
        }

        targetShopPanel.Open();
        return true;
    }

    private void StartEvent06ShopOpenAfterVisualAction(bool hasVisualAction)
    {
        if (!pendingEvent06ShopOpen || shopPanel == null)
            return;

        if (event06ShopOpenRoutine != null)
            StopCoroutine(event06ShopOpenRoutine);

        event06ShopOpenRoutine = StartCoroutine(
            OpenEvent06ShopAfterVisualAction(hasVisualAction ? event06ShopOpenDelay : 0f));
    }

    private IEnumerator OpenEvent06ShopAfterVisualAction(float delay)
    {
        pendingEvent06ShopOpen = false;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (waitingForEvent06ShopClose && shopPanel != null)
        {
            // 상점 패널의 페이드 인과 동시에 기존 EventRoom TITLE/선택지 UI를 부드럽게 숨깁니다.
            shopPanel.Open();
            SaveEventResume(ResumePhase.EventChoice, null, default, null, ResumePresentation.Shop);
            yield return FadeOutEvent06EventUi();
        }

        event06ShopOpenRoutine = null;
    }

    private IEnumerator FadeOutEvent06EventUi()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();
        EnsureEventTitleCanvasGroup();

        CanvasGroup scrollGroup = eventChoiceScrollCanvasGroup;
        CanvasGroup gradationGroup = eventChoiceGradationCanvasGroup;
        CanvasGroup titleGroup = eventTitleCanvasGroup;

        if (scrollGroup != null)
        {
            scrollGroup.interactable = false;
            scrollGroup.blocksRaycasts = false;
        }

        if (titleGroup != null)
        {
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
        }

        float duration = Mathf.Max(0.01f, event06EventUiFadeDuration);
        float elapsed = 0f;
        float scrollStart = scrollGroup != null ? scrollGroup.alpha : 1f;
        float gradationStart = gradationGroup != null ? gradationGroup.alpha : 1f;
        float titleStart = titleGroup != null ? titleGroup.alpha : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (scrollGroup != null) scrollGroup.alpha = Mathf.Lerp(scrollStart, 0f, t);
            if (gradationGroup != null) gradationGroup.alpha = Mathf.Lerp(gradationStart, 0f, t);
            if (titleGroup != null) titleGroup.alpha = Mathf.Lerp(titleStart, 0f, t);
            yield return null;
        }

        if (scrollGroup != null) scrollGroup.alpha = 0f;
        if (gradationGroup != null) gradationGroup.alpha = 0f;
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (eventChoiceScrollView != null) eventChoiceScrollView.gameObject.SetActive(false);
        if (eventChoiceGradation != null) eventChoiceGradation.gameObject.SetActive(false);
        if (eventTitleText != null) eventTitleText.gameObject.SetActive(false);
    }

    private void CacheEvent06CloseVisualAction(EventData choice)
    {
        if (choice == null || EventIdUtility.Normalize(choice.EventId) != "Event_06")
            return;

        event06CloseVisualObjectId = NormalizeVisualId(choice.SuccessVisualObjectId);
        event06CloseVisualActionId = NormalizeVisualId(choice.SuccessVisualActionId);
        event06NextEventId = EventIdUtility.Normalize(choice.NextEventId);
    }

    private static string NormalizeVisualId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private void OnEvent06ShopClosed()
    {
        if (!waitingForEvent06ShopClose)
            return;

        string nextEventId = event06NextEventId;
        string closeVisualObjectId = event06CloseVisualObjectId;
        string closeVisualActionId = event06CloseVisualActionId;
        UnbindEvent06ShopClose();

        if (event06ResultUiRoutine != null)
            StopCoroutine(event06ResultUiRoutine);

        event06ResultUiRoutine = StartCoroutine(ShowEvent06ResultAfterShopClose(
            nextEventId, closeVisualObjectId, closeVisualActionId));
    }

    private IEnumerator ShowEvent06ResultAfterShopClose(
        string nextEventId,
        string closeVisualObjectId,
        string closeVisualActionId)
    {
        if (string.IsNullOrWhiteSpace(nextEventId) ||
            DataManager.Instance == null ||
            DataManager.Instance.EventDatabase == null ||
            !DataManager.Instance.EventDatabase.TryGetEvent(nextEventId, out EventDefinition resultDefinition) ||
            resultDefinition == null)
        {
            Debug.LogWarning(
                $"[EventRoomController] Event_06 shop result event '{nextEventId}' was not found.",
                this);
            event06ResultUiRoutine = null;
            yield break;
        }

        // GameData의 NextEventId를 그대로 따라 결과 TITLE/NextButton 상태로 전환합니다.
        LoadEventDefinition(resultDefinition, string.Empty);

        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceGradationReference();
        EnsureEventTitleCanvasGroup();

        if (eventChoiceScrollView != null)
            eventChoiceScrollView.gameObject.SetActive(false);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(false);

        if (eventTitleText != null)
            eventTitleText.gameObject.SetActive(true);

        CanvasGroup titleGroup = eventTitleCanvasGroup;
        if (titleGroup != null)
        {
            titleGroup.alpha = 0f;
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
        }

        // 결과 TITLE/NextButton이 나타나는 시점에 상점을 열 때 사용했던
        // 동일한 NPC 애니메이션을 끝에서 처음으로 역재생합니다.
        ReverseVisualActionById(closeVisualObjectId, closeVisualActionId);
        SetNextButtonVisible(true);

        float duration = Mathf.Max(0.01f, event06EventUiFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (titleGroup != null) titleGroup.alpha = t;
            yield return null;
        }

        if (titleGroup != null)
        {
            titleGroup.alpha = 1f;
            titleGroup.interactable = true;
            titleGroup.blocksRaycasts = true;
        }

        event06ResultUiRoutine = null;
    }

    private void UnbindEvent06ShopClose()
    {
        waitingForEvent06ShopClose = false;
        pendingEvent06ShopOpen = false;

        if (event06ShopOpenRoutine != null)
        {
            StopCoroutine(event06ShopOpenRoutine);
            event06ShopOpenRoutine = null;
        }

        if (event06ResultUiRoutine != null)
        {
            StopCoroutine(event06ResultUiRoutine);
            event06ResultUiRoutine = null;
        }

        if (shopPanel != null)
            shopPanel.Closed -= OnEvent06ShopClosed;

        event06CloseVisualObjectId = string.Empty;
        event06CloseVisualActionId = string.Empty;
        event06NextEventId = string.Empty;
    }

    private RestRoomShopPanel ResolveShopPanel()
    {
        if (shopPanel != null)
            return shopPanel;

        shopPanel = GetComponentInChildren<RestRoomShopPanel>(true);

        if (shopPanel != null)
            return shopPanel;

        if (dataEventRoot != null)
            shopPanel = dataEventRoot.GetComponentInChildren<RestRoomShopPanel>(true);

        if (shopPanel != null)
            return shopPanel;

        shopPanel = Object.FindFirstObjectByType<RestRoomShopPanel>(FindObjectsInactive.Include);
        return shopPanel;
    }

    private EventChoiceExecutionContext CreateExecutionContext(
        EventChoiceEquippedRelicCost selectedEquippedRelicCost = default,
        EventChoiceSkillAwakenTarget selectedSkillAwakenTarget = default,
        int[] forcedDiceFaces = null)
    {
        BattleRuntimeData battleRuntime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();

        return new EventChoiceExecutionContext
        {
            BattleRuntime = battleRuntime,
            PartyCharacters = CollectPartyCharacters(),
            SessionState = eventChoiceSessionState,
            RollDiceFaces = forcedDiceFaces != null ? () => forcedDiceFaces : null,
            GrantRandomRelic = TryQueueRandomRelicReward,
            GrantRandomSkill = TryQueueRandomSkillReward,
            UpgradeRandomSkill = TryUpgradeRandomSkill,
            GrantRemnant = TryQueueRemnantReward,
            RevokeRemnant = RevokeQueuedRemnantReward,
            SelectedEquippedRelicCost = selectedEquippedRelicCost,
            RevokeEquippedRelic = TryRevokeEquippedRelicCost,
            SelectedSkillAwakenTarget = selectedSkillAwakenTarget,
            UpgradeSelectedSkill = TryUpgradeSelectedSkill,
            RollbackAwakenedSkills = TryRollbackAwakenedSkills,
            RemoveFailedSelectedSkill = TryRemoveFailedSelectedSkill,
            OfferFilteredSkillRewards = TryQueueFilteredSkillRewards,
            HasUpgradeableEquippedSkill = HasAnyUpgradeableEquippedSkill,
            OpenShop = TryOpenShopPanel,
            RefreshRemnantHud = BattleGoldHudUI.RefreshAll,
            SuppressRewardResultMessages = true
        };
    }

    private void SaveEventResume(
        ResumePhase phase,
        EventData choice,
        EventChoiceExecutionResult result,
        int[] diceFaces,
        ResumePresentation presentation = ResumePresentation.None,
        string eventIdOverride = null,
        string resultMessage = null,
        bool diceRollResolved = false)
    {
        MapRuntimeData map = DataManager.Instance?.MapRuntimeStore?.Get();
        var resume = new ResumeData
        {
            Phase = phase,
            NodeIndex = map != null ? map.CurrentNodeIndex : -1,
            MapId = map?.CurrentMapId,
            EventId = EventIdUtility.Normalize(eventIdOverride ?? currentEventDefinition?.EventId ?? pendingEventId),
            SelectedChoiceId = choice != null ? choice.ChoiceOrder.ToString() : string.Empty,
            ChoiceResultApplied = phase == ResumePhase.EventChoice,
            NextEventId = !string.IsNullOrWhiteSpace(result.NextEventId) ? result.NextEventId : event06NextEventId,
            Presentation = presentation != ResumePresentation.None
                ? presentation
                : phase == ResumePhase.EventEntry ? ResumePresentation.ChoiceList
                : phase == ResumePhase.EventDice ? ResumePresentation.DiceResolved
                : ResumePresentation.ResultOnly,
            ResultMessage = resultMessage ?? result.ResultMessage,
            ChanceSucceeded = result.Succeeded,
            DiceFaces = diceFaces ?? System.Array.Empty<int>(),
            DiceRollResolved = diceRollResolved
        };

        for (int i = 0; i < pendingEventRewards.Count; i++)
        {
            BattleRewardData reward = pendingEventRewards[i];
            if (reward == null) continue;
            resume.PendingRewards.Add(new BattleRewardSaveData { Type = reward.Type, RewardId = reward.RewardId, Amount = reward.Amount });
        }

        if (resume.Presentation == ResumePresentation.ChoiceList)
        {
            IReadOnlyList<EventData> visibleChoices = GetCurrentVisibleChoices();
            if (visibleChoices != null)
            {
                for (int i = 0; i < visibleChoices.Count; i++)
                {
                    EventData visibleChoice = visibleChoices[i];
                    if (visibleChoice == null) continue;
                    resume.VisibleChoices.Add(new EventChoiceReferenceSaveData
                    {
                        EventId = EventIdUtility.Normalize(visibleChoice.EventId),
                        ChoiceOrder = visibleChoice.ChoiceOrder
                    });
                }
            }
        }

        if (presentation == ResumePresentation.Shop && shopPanel != null)
            resume.ShopGoods.AddRange(shopPanel.CaptureResumeStock());

        SaveSystem.Instance?.SaveCheckpoint(resume);
    }

    private IReadOnlyList<EventData> ResolveSavedVisibleChoices(ResumeData resume)
    {
        if (resume?.VisibleChoices == null || resume.VisibleChoices.Count == 0 ||
            DataManager.Instance?.EventDatabase == null)
        {
            return GetCurrentVisibleChoices();
        }

        var choices = new List<EventData>();
        for (int i = 0; i < resume.VisibleChoices.Count; i++)
        {
            EventChoiceReferenceSaveData reference = resume.VisibleChoices[i];
            if (reference == null || string.IsNullOrWhiteSpace(reference.EventId)) continue;
            if (!DataManager.Instance.EventDatabase.TryGetEvent(reference.EventId, out EventDefinition definition) ||
                definition?.Choices == null) continue;

            EventData choice = FindChoiceByOrder(definition.Choices, reference.ChoiceOrder);
            if (choice != null)
                choices.Add(choice);
        }

        return choices.Count > 0 ? choices : GetCurrentVisibleChoices();
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

        // 레드 더스티움은 RewardPanel에 넣지 않고 Dustium -> GoldHud 전용 연출로 처리합니다.
        pendingDustiumAcquireAmount += safeAmount;
        resultMessage = $"레드 더스티움 {safeAmount} 획득";
        return true;
    }

    private void RevokeQueuedRemnantReward(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
            return;

        pendingDustiumAcquireAmount = Mathf.Max(0, pendingDustiumAcquireAmount - safeAmount);
    }

    private IEnumerator PlayPendingDustiumAcquireThenContinue(
        EventData choice,
        EventChoiceExecutionResult result,
        int[] resolvedDiceFaces)
    {
        int amount = Mathf.Max(0, pendingDustiumAcquireAmount);
        pendingDustiumAcquireAmount = 0;

        // 상자 열기 같은 SuccessVisualAction이 먼저 보이도록 기다린 뒤 Dustium 연출을 시작합니다.
        if (result.HasVisualAction && dustiumVisualActionWaitDuration > 0f)
            yield return new WaitForSecondsRealtime(dustiumVisualActionWaitDuration);

        if (amount > 0)
            yield return PlayDustiumAcquireAnimation(amount);

        ApplyDustiumAmount(amount);
        dustiumAcquireRoutine = null;

        if (isActiveAndEnabled)
            ContinueAfterExecutedChoiceCore(choice, result, resolvedDiceFaces);
    }

    private static bool IsEvent05MiningChoice(EventData choice)
    {
        if (choice == null || choice.ChoiceOrder < 1 || choice.ChoiceOrder > 2)
            return false;

        string eventId = EventIdUtility.Normalize(choice.EventId);
        return eventId == "Event_05" || eventId == "Event_05_A" || eventId == "Event_05_B";
    }

    private static bool IsEvent05DeferredExitChoice(EventData choice)
    {
        if (choice == null || choice.ChoiceOrder != 3)
            return false;

        string eventId = EventIdUtility.Normalize(choice.EventId);
        return eventId == "Event_05_A" || eventId == "Event_05_B";
    }

    private IEnumerator PlayEvent05AccumulatedDustiumThenContinue(
        EventData choice,
        EventChoiceExecutionResult result,
        int accumulatedBeforeChoice,
        int[] resolvedDiceFaces)
    {
        // Event_05는 광맥/선택지 연출을 먼저 보여준 뒤 누적 더스티움 UI를 갱신합니다.
        // 일반 ContinueAfterExecutedChoice를 사용하면 PlayVisualAction이 더스티움 표시 뒤에 실행되므로
        // 이 이벤트만 여기서 연출 순서를 명시적으로 제어합니다.
        PersistEventRuntime();
        PlayVisualAction(result);

        if (result.HasVisualAction && dustiumVisualActionWaitDuration > 0f)
            yield return new WaitForSecondsRealtime(dustiumVisualActionWaitDuration);

        EnsureDustiumAcquireReferences();
        CacheDustiumAcquireOriginalState();

        if (result.Succeeded && eventChoiceSessionState.AccumulatedRemnant > accumulatedBeforeChoice)
        {
            int total = Mathf.Max(0, eventChoiceSessionState.AccumulatedRemnant);
            yield return ShowOrUpdateEvent05AccumulatedDustium(total);
        }
        else if (!result.Succeeded && accumulatedBeforeChoice > 0)
        {
            // 첫 채굴부터 실패한 경우에는 아직 생성된 누적 더스티움이 없으므로
            // 0 VALUE 오브젝트를 새로 만들지 않습니다. 이전 성공분이 있을 때만 0 -> 소실 연출을 재생합니다.
            yield return LoseEvent05AccumulatedDustium();
        }

        dustiumAcquireRoutine = null;
        if (isActiveAndEnabled)
            ContinueAfterExecutedChoiceCore(choice, result, resolvedDiceFaces);
    }

    private IEnumerator PlayEvent05ExitVisualThenContinue(
        EventData choice,
        EventChoiceExecutionResult result,
        int[] resolvedDiceFaces)
    {
        // '그만둔다'는 선택 즉시 종료하지 않고 선택 연출 후 Event_05_C 결과 Title을 반드시 표시합니다.
        PersistEventRuntime();
        PlayVisualAction(result);

        if (result.HasVisualAction && dustiumVisualActionWaitDuration > 0f)
            yield return new WaitForSecondsRealtime(dustiumVisualActionWaitDuration);

        dustiumAcquireRoutine = null;
        if (isActiveAndEnabled)
            ContinueAfterExecutedChoiceCore(choice, result, resolvedDiceFaces);
    }

    private IEnumerator ShowOrUpdateEvent05AccumulatedDustium(int total)
    {
        if (dustiumAcquireRoot == null)
            yield break;

        bool wasVisible = dustiumAcquireRoot.gameObject.activeSelf;
        ResetDustiumAcquireTransform();
        dustiumAcquireRoot.localScale = dustiumAcquireOriginalLocalScale;
        dustiumAcquireRoot.gameObject.SetActive(true);

        if (dustiumAcquireValueText != null)
        {
            dustiumAcquireValueText.gameObject.SetActive(true);
            dustiumAcquireValueText.text = Mathf.Max(0, total).ToString(CultureInfo.InvariantCulture);
        }

        if (!wasVisible)
        {
            Vector3 endPosition = dustiumAcquireOriginalWorldPosition;
            Vector3 startPosition = endPosition + new Vector3(dustiumAppearOffset.x, dustiumAppearOffset.y, 0f);
            dustiumAcquireRoot.position = startPosition;

            float duration = Mathf.Max(0.01f, dustiumAppearDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
                position += Vector3.up * (4f * dustiumAppearCurveHeight * t * (1f - t));
                dustiumAcquireRoot.position = position;
                yield return null;
            }

            dustiumAcquireRoot.position = endPosition;
        }
        else
        {
            Vector3 baseScale = dustiumAcquireOriginalLocalScale;
            Vector3 peakScale = baseScale * 1.12f;
            float duration = Mathf.Max(0.08f, dustiumAppearDuration * 0.6f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                dustiumAcquireRoot.localScale = Vector3.Lerp(baseScale, peakScale, pulse);
                yield return null;
            }
            dustiumAcquireRoot.localScale = baseScale;
        }
    }

    private IEnumerator LoseEvent05AccumulatedDustium()
    {
        if (dustiumAcquireRoot == null)
            yield break;

        ResetDustiumAcquireTransform();
        dustiumAcquireRoot.localScale = dustiumAcquireOriginalLocalScale;
        dustiumAcquireRoot.gameObject.SetActive(true);

        if (dustiumAcquireValueText != null)
        {
            dustiumAcquireValueText.gameObject.SetActive(true);
            dustiumAcquireValueText.text = "0";
        }

        if (dustiumValueHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(dustiumValueHoldDuration);

        ResetDustiumAcquireVisual();
    }

    private IEnumerator CommitEvent05AccumulatedDustiumAndExit()
    {
        int amount = Mathf.Max(0, eventChoiceSessionState.AccumulatedRemnant);
        SetNextButtonVisible(false);

        if (amount > 0)
        {
            EnsureDustiumAcquireReferences();
            CacheDustiumAcquireOriginalState();

            if (dustiumAcquireRoot != null && goldHudTarget != null)
            {
                ResetDustiumAcquireTransform();
                dustiumAcquireRoot.localScale = dustiumAcquireOriginalLocalScale;
                dustiumAcquireRoot.gameObject.SetActive(true);

                if (dustiumAcquireValueText != null)
                {
                    dustiumAcquireValueText.gameObject.SetActive(true);
                    dustiumAcquireValueText.text = amount.ToString(CultureInfo.InvariantCulture);
                }

                if (dustiumValueHoldDuration > 0f)
                    yield return new WaitForSecondsRealtime(dustiumValueHoldDuration);

                Vector3 startPosition = dustiumAcquireRoot.position;
                Vector3 endPosition = goldHudTarget.position;
                Vector3 startScale = dustiumAcquireRoot.localScale;
                Vector3 endScale = startScale * Mathf.Max(0f, dustiumFlyEndScale);
                float duration = Mathf.Max(0.01f, dustiumFlyDuration);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
                    position += Vector3.up * (4f * dustiumFlyCurveHeight * t * (1f - t));
                    dustiumAcquireRoot.position = position;
                    dustiumAcquireRoot.localScale = Vector3.Lerp(startScale, endScale, eased);
                    yield return null;
                }
            }

            eventChoiceSessionState.AccumulatedRemnant = 0;
            ApplyDustiumAmount(amount);
            PersistEventRuntime();
        }

        ResetDustiumAcquireVisual();
        dustiumAcquireRoutine = null;

        if (!isActiveAndEnabled)
            yield break;

        SetEventTitleVisible(false);
        CompleteCurrentNode();
        ReturnToMap();
    }

    private IEnumerator PlayDustiumAcquireAnimation(int amount)
    {
        EnsureDustiumAcquireReferences();
        CacheDustiumAcquireOriginalState();

        if (dustiumAcquireRoot == null || goldHudTarget == null)
            yield break;

        ResetDustiumAcquireTransform();

        Vector3 appearEndPosition = dustiumAcquireOriginalWorldPosition;
        Vector3 appearStartPosition = appearEndPosition + new Vector3(dustiumAppearOffset.x, dustiumAppearOffset.y, 0f);
        dustiumAcquireRoot.position = appearStartPosition;
        dustiumAcquireRoot.localScale = dustiumAcquireOriginalLocalScale;
        dustiumAcquireRoot.gameObject.SetActive(true);

        if (dustiumAcquireValueText != null)
        {
            dustiumAcquireValueText.gameObject.SetActive(true);
            dustiumAcquireValueText.text = $"+{Mathf.Max(0, amount)}";
        }

        float appearDuration = Mathf.Max(0.01f, dustiumAppearDuration);
        float appearElapsed = 0f;
        while (appearElapsed < appearDuration)
        {
            appearElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(appearElapsed / appearDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 position = Vector3.Lerp(appearStartPosition, dustiumAcquireOriginalWorldPosition, eased);
            position += Vector3.up * (4f * dustiumAppearCurveHeight * t * (1f - t));
            dustiumAcquireRoot.position = position;
            yield return null;
        }

        dustiumAcquireRoot.position = appearEndPosition;

        if (dustiumValueHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(dustiumValueHoldDuration);

        if (dustiumAcquireValueText != null)
            dustiumAcquireValueText.gameObject.SetActive(false);

        Vector3 startPosition = dustiumAcquireRoot.position;
        Vector3 endPosition = goldHudTarget.position;
        Vector3 startScale = dustiumAcquireRoot.localScale;
        Vector3 endScale = startScale * Mathf.Max(0f, dustiumFlyEndScale);
        float duration = Mathf.Max(0.01f, dustiumFlyDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, eased);
            position += Vector3.up * (4f * dustiumFlyCurveHeight * t * (1f - t));
            dustiumAcquireRoot.position = position;
            dustiumAcquireRoot.localScale = Vector3.Lerp(startScale, endScale, eased);

            yield return null;
        }

        dustiumAcquireRoot.position = endPosition;
        dustiumAcquireRoot.localScale = endScale;
        dustiumAcquireRoot.gameObject.SetActive(false);
        ResetDustiumAcquireTransform();
    }

    private void ApplyDustiumAmount(int amount)
    {
        if (amount <= 0 || DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return;

        BattleRuntimeData battleRuntime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        if (battleRuntime == null)
            return;

        battleRuntime.Remnant = Mathf.Max(0, battleRuntime.Remnant + amount);
        DataManager.Instance.BattleRuntimeStore.Set(battleRuntime);

        // BattleGoldHudUI가 현재 표시값에서 새 보유량까지 자체 숫자 애니메이션을 진행합니다.
        BattleGoldHudUI.RefreshAll();
    }

    private void StopDustiumAcquireAnimation(bool resetVisual)
    {
        if (dustiumAcquireRoutine != null)
        {
            StopCoroutine(dustiumAcquireRoutine);
            dustiumAcquireRoutine = null;
        }

        if (resetVisual)
            ResetDustiumAcquireVisual();
    }

    private void ResetDustiumAcquireVisual()
    {
        EnsureDustiumAcquireReferences();
        ResetDustiumAcquireTransform();

        if (dustiumAcquireValueText != null)
            dustiumAcquireValueText.gameObject.SetActive(false);

        if (dustiumAcquireRoot != null)
            dustiumAcquireRoot.gameObject.SetActive(false);
    }

    private void ResetDustiumAcquireTransform()
    {
        if (dustiumAcquireRoot == null || !hasDustiumAcquireOriginalState)
            return;

        dustiumAcquireRoot.position = dustiumAcquireOriginalWorldPosition;
        dustiumAcquireRoot.localScale = dustiumAcquireOriginalLocalScale;
    }

    private void CacheDustiumAcquireOriginalState()
    {
        if (hasDustiumAcquireOriginalState || dustiumAcquireRoot == null)
            return;

        dustiumAcquireOriginalWorldPosition = dustiumAcquireRoot.position;
        dustiumAcquireOriginalLocalScale = dustiumAcquireRoot.localScale;
        hasDustiumAcquireOriginalState = true;
    }

    private bool TryRevokeEquippedRelicCost(
        EventChoiceEquippedRelicCost cost,
        out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!cost.IsValid)
        {
            resultMessage = "삭제할 장착 유물을 선택해야 합니다.";
            return false;
        }

        CharacterRuntimeStore characterStore = DataManager.Instance?.CharacterRuntimeStore;
        if (characterStore == null ||
            !characterStore.TryGet(cost.CharacterId, out CharacterRuntimeData character) ||
            character == null)
        {
            resultMessage = "선택한 캐릭터를 찾을 수 없습니다.";
            return false;
        }

        RelicEquipService.EnsureRelicSlots(character);

        if (character.EquippedRelicIds == null ||
            cost.RelicSlotIndex < 0 ||
            cost.RelicSlotIndex >= character.EquippedRelicIds.Length)
        {
            resultMessage = "선택한 유물 슬롯을 찾을 수 없습니다.";
            return false;
        }

        string currentRelicId = character.EquippedRelicIds[cost.RelicSlotIndex]?.Trim();
        if (!string.Equals(currentRelicId, cost.RelicId, System.StringComparison.Ordinal))
        {
            resultMessage = "선택한 장착 유물이 이미 변경되었습니다.";
            return false;
        }

        character.EquippedRelicIds[cost.RelicSlotIndex] = null;
        RemoveActiveRelicUseEntries(character, cost.RelicId);

        resultMessage = $"유물 {GetRelicDisplayName(cost.RelicId)} 삭제";
        return true;
    }

    private static void RemoveActiveRelicUseEntries(CharacterRuntimeData character, string relicId)
    {
        if (character?.ActiveRelicUses == null)
            return;

        string targetRelicId = relicId?.Trim();
        for (int i = character.ActiveRelicUses.Count - 1; i >= 0; i--)
        {
            ActiveRelicUseRuntimeData entry = character.ActiveRelicUses[i];
            if (entry == null ||
                string.Equals(entry.RelicId?.Trim(), targetRelicId, System.StringComparison.Ordinal))
            {
                character.ActiveRelicUses.RemoveAt(i);
            }
        }
    }


    private static bool ContainsAnyToken(string value, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(value) || tokens == null)
            return false;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (!string.IsNullOrWhiteSpace(token) &&
                value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryQueueRandomRelicReward(out string resultMessage)
    {
        resultMessage = string.Empty;

        bool requireEpic = activeRewardChoice != null &&
            ContainsAnyToken(activeRewardChoice.ResultTarget, "에픽", "Epic");

        if (!TryPickRandomAvailableRelic(out ChestRelicReward reward, requireEpic ? RelicRarity.Epic : (RelicRarity?)null))
        {
            resultMessage = requireEpic ? "획득 가능한 에픽 유물이 없습니다." : "획득 가능한 유물이 없습니다.";
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

    private bool TryQueueFilteredSkillRewards(
        EventChoiceSkillRewardFilter filter,
        int count,
        out string resultMessage)
    {
        resultMessage = string.Empty;
        int rewardCount = Mathf.Max(0, count);

        if (rewardCount <= 0)
        {
            resultMessage = "제시할 기억 개수가 올바르지 않습니다.";
            return false;
        }

        List<SkillMasterData> candidates = CollectAvailableSkillRewardCandidates(filter);
        if (candidates.Count < rewardCount)
        {
            resultMessage = $"획득 가능한 {GetSkillRewardFilterDisplayName(filter)} 기억이 부족합니다.";
            return false;
        }

        for (int i = 0; i < rewardCount; i++)
        {
            int selectedIndex = BattleRandom.Range(0, candidates.Count);
            SkillMasterData skill = candidates[selectedIndex];
            candidates.RemoveAt(selectedIndex);

            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            string skillId = skill.SkillId.Trim();
            QueueEventReward(EventRoomRewardFlowUtility.CreateSkillReward(
                skill,
                GetSkillSprite(skillId, skill)));
        }

        resultMessage = $"{GetSkillRewardFilterDisplayName(filter)} 기억 {rewardCount}개 제시";
        return true;
    }

    private static string GetSkillRewardFilterDisplayName(EventChoiceSkillRewardFilter filter)
    {
        return filter switch
        {
            EventChoiceSkillRewardFilter.Attack => "공격",
            EventChoiceSkillRewardFilter.Buff => "버프",
            EventChoiceSkillRewardFilter.Debuff => "디버프",
            EventChoiceSkillRewardFilter.CommonToRare => "일반~레어",
            EventChoiceSkillRewardFilter.Epic => "에픽",
            _ => ""
        };
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

        RecordDiscoveryService.RegisterSkill(DataManager.Instance, skillId);
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

    private bool TryPickRandomAvailableRelic(
        out ChestRelicReward reward,
        RelicRarity? requiredRarity = null)
    {
        reward = default;

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
            return false;

        IReadOnlyList<RelicData> allRelics = DataManager.Instance.RelicDatabase.GetAll();
        List<RelicData> candidates = ChestRelicRewardService.GetChestRewardCandidates(
            allRelics,
            CollectUnavailableRelicIds());

        if (requiredRarity.HasValue)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                RelicData candidate = candidates[i];
                if (candidate == null ||
                    !RelicRarityUtility.TryParseChestRarity(candidate.Rarity, out RelicRarity rarity) ||
                    rarity != requiredRarity.Value)
                {
                    candidates.RemoveAt(i);
                }
            }
        }

        if (candidates.Count == 0)
            return false;

        RelicData selected = candidates[BattleRandom.Range(0, candidates.Count)];
        if (selected == null || !RelicRarityUtility.TryParseChestRarity(selected.Rarity, out RelicRarity selectedRarity))
            return false;

        reward = new ChestRelicReward(selected, selectedRarity);
        return reward.IsValid;
    }

    private bool TryPickRandomAvailableSkill(out SkillMasterData selectedSkill)
    {
        selectedSkill = null;

        List<SkillMasterData> candidates = CollectAvailableSkillRewardCandidates();
        if (candidates.Count == 0)
            return false;

        selectedSkill = candidates[BattleRandom.Range(0, candidates.Count)];
        return selectedSkill != null;
    }

    private List<SkillMasterData> CollectAvailableSkillRewardCandidates(
        EventChoiceSkillRewardFilter filter)
    {
        List<SkillMasterData> candidates = CollectAvailableSkillRewardCandidates();

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            SkillMasterData skill = candidates[i];
            if (skill == null || !MatchesSkillRewardFilter(skill, filter))
                candidates.RemoveAt(i);
        }

        return candidates;
    }

    private static bool MatchesSkillRewardFilter(
        SkillMasterData skill,
        EventChoiceSkillRewardFilter filter)
    {
        if (skill == null)
            return false;

        return filter switch
        {
            EventChoiceSkillRewardFilter.Attack => skill.SkillType == SkillType.Attack,
            EventChoiceSkillRewardFilter.Buff => skill.SkillType == SkillType.Buff,
            EventChoiceSkillRewardFilter.Debuff => skill.SkillType == SkillType.Debuff,
            EventChoiceSkillRewardFilter.CommonToRare =>
                skill.Rarity == SkillRarity.Common || skill.Rarity == SkillRarity.Rare,
            EventChoiceSkillRewardFilter.Epic => skill.Rarity == SkillRarity.Epic,
            _ => false
        };
    }

    private List<SkillMasterData> CollectAvailableSkillRewardCandidates(
        SkillType? requiredSkillType = null)
    {
        List<SkillMasterData> candidates = new();

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
            return candidates;

        List<SkillMasterData> allSkills = DataManager.Instance.SkillDatabase.GetAll();
        if (allSkills == null || allSkills.Count == 0)
            return candidates;

        HashSet<string> unavailableIds = CollectUnavailableSkillIds();

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillMasterData skill = allSkills[i];

            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            string skillId = skill.SkillId.Trim();

            if (skill.Category != Category.Core)
                continue;

            if (requiredSkillType.HasValue && skill.SkillType != requiredSkillType.Value)
                continue;

            if (!SkillRarityUtility.IsBaseSkillVariant(skillId))
                continue;

            if (unavailableIds.Contains(skillId))
                continue;

            candidates.Add(skill);
        }

        return candidates;
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

    private bool TryOpenPendingEventRewardPanel(bool delayOpening = false, bool saveCheckpoint = true)
    {
        if (pendingEventRewards.Count <= 0)
            return false;

        EnsureRewardPanelReference();

        if (rewardPanel == null)
        {
            Debug.LogWarning("[EventRoomController] Shared BattleRewardPanelUI not found for event rewards.");
            return false;
        }

        isEventRewardPanelOpen = true;
        SetNextButtonVisible(false);
        SetChoiceSlotsInteractable(false);

        List<BattleRewardData> rewards = new(pendingEventRewards);
        if (saveCheckpoint)
        {
            SaveEventResume(
                ResumePhase.EventChoice,
                activeRewardChoice,
                default,
                null,
                ResumePresentation.RewardPanel);
        }
        pendingEventRewards.Clear();

        if (delayOpening && eventRewardPanelOpenDelay > 0f && isActiveAndEnabled)
        {
            StopEventRewardPanelDelay();
            eventRewardPanelDelayRoutine =
                StartCoroutine(OpenPendingEventRewardPanelAfterDelay(rewards));
            return true;
        }

        OpenPendingEventRewardPanelNow(rewards);
        return true;
    }

    private IEnumerator OpenPendingEventRewardPanelAfterDelay(List<BattleRewardData> rewards)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, eventRewardPanelOpenDelay));

        eventRewardPanelDelayRoutine = null;
        OpenPendingEventRewardPanelNow(rewards);
    }

    private void OpenPendingEventRewardPanelNow(List<BattleRewardData> rewards)
    {
        eventRewardPanelDelayRoutine = null;
        rewardPanel.Open(rewards, OnEventRewardPanelCompleted);
    }

    private void StopEventRewardPanelDelay()
    {
        if (eventRewardPanelDelayRoutine == null)
            return;

        StopCoroutine(eventRewardPanelDelayRoutine);
        eventRewardPanelDelayRoutine = null;
    }

    private void OnEventRewardPanelCompleted()
    {
        StopEventRewardPanelDelay();
        isEventRewardPanelOpen = false;
        pendingEventRewards.Clear();
        PersistEventRuntime();
        HideDiceRollPresenterImmediate();

        if (CompletePendingEvent01ResultContinuation())
            return;

        if (CompletePendingEvent02ResultContinuation())
            return;

        if (CompletePendingEvent04ResultContinuation())
            return;

        SetNextButtonVisible(true);
    }

    private void EnsureRewardPanelReference()
    {
        if (rewardPanel != null)
            return;

        rewardPanel = GetComponentInChildren<BattleRewardPanelUI>(true);

        if (rewardPanel == null)
            rewardPanel = Object.FindFirstObjectByType<BattleRewardPanelUI>(FindObjectsInactive.Include);
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

    private static bool IsSameId(string value, string targetId)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               !string.IsNullOrWhiteSpace(targetId) &&
               string.Equals(value.Trim(), targetId.Trim(), System.StringComparison.Ordinal);
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

    private int[] RollThreeSixSidedDiceFaces()
    {
        return new[]
        {
            BattleRandom.Range(1, 7),
            BattleRandom.Range(1, 7),
            BattleRandom.Range(1, 7)
        };
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

    private static bool IsDiceChoice(EventData choice)
    {
        return choice != null && SameToken(choice.ChoiceType, "Dice");
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
        EnsureEquippedRelicSelectionPanel();
        EnsureSkillAwakenSelectionPanel();
        EnsureDiceRollPresenter();
        EnsureDustiumAcquireReferences();
    }

    private void EnsureEquippedRelicSelectionPanel()
    {
        if (equippedRelicSelectionPanel != null)
            return;

        Transform searchRoot = dataEventRoot != null ? dataEventRoot.transform : transform;
        equippedRelicSelectionPanel =
            searchRoot.GetComponentInChildren<EventEquippedRelicSelectionPanelUI>(true);
    }

    private void EnsureSkillAwakenSelectionPanel()
    {
        if (skillAwakenSelectionPanel != null)
            return;

        Transform searchRoot = dataEventRoot != null ? dataEventRoot.transform : transform;
        skillAwakenSelectionPanel =
            searchRoot.GetComponentInChildren<EventSkillAwakenSelectionPanelUI>(true);
    }

    private void EnsureDiceRollPresenter()
    {
        if (diceRollPresenter != null)
            return;

        Transform searchRoot = dataEventRoot != null ? dataEventRoot.transform : transform;
        diceRollPresenter = searchRoot.GetComponentInChildren<EventDiceRollPresenter>(true);

        if (diceRollPresenter != null)
            diceRollPresenter.transform.SetAsLastSibling();
    }

    private void EnsureDustiumAcquireReferences()
    {
        Transform searchRoot = dataEventRoot != null ? dataEventRoot.transform : transform;

        if (dustiumAcquireRoot == null)
        {
            Transform dustium = FindChildRecursive(searchRoot, "Dustium");
            if (dustium != null)
                dustiumAcquireRoot = dustium as RectTransform;
        }

        if (dustiumAcquireRoot != null && dustiumAcquireValueText == null)
        {
            Transform value = FindChildRecursive(dustiumAcquireRoot, "Value");
            if (value != null)
                dustiumAcquireValueText = value.GetComponent<TMP_Text>();
        }

        if (goldHudTarget == null)
        {
            Transform target = FindChildRecursive(null, "GoldHud");
            if (target == null)
                target = FindChildRecursive(null, "GoldHub");
            if (target != null)
                goldHudTarget = target as RectTransform;
        }
    }

    private void BeginTerminalChoiceExitVisuals()
    {
        StopTerminalChoiceFade();
        SetChoiceSlotsInteractable(false);

        if (eventTitleText != null)
            eventTitleText.gameObject.SetActive(false);

        terminalChoiceFadeRoutine = StartCoroutine(FadeOutTerminalChoiceSlots());
    }

    private IEnumerator FadeOutTerminalChoiceSlots()
    {
        EnsureChoiceSlots();

        List<CanvasGroup> groups = new();
        if (choiceSlots != null)
        {
            for (int i = 0; i < choiceSlots.Length; i++)
            {
                EventChoiceSlotUI slot = choiceSlots[i];
                if (slot == null || !slot.gameObject.activeInHierarchy)
                    continue;

                CanvasGroup group = slot.GetComponent<CanvasGroup>();
                if (group == null)
                    group = slot.gameObject.AddComponent<CanvasGroup>();

                group.interactable = false;
                group.blocksRaycasts = false;
                groups.Add(group);
            }
        }

        if (groups.Count == 0)
        {
            terminalChoiceFadeRoutine = null;
            yield break;
        }

        float duration = Mathf.Max(0.01f, terminalChoiceFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - t;

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] != null)
                    groups[i].alpha = alpha;
            }

            yield return null;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i] != null)
                groups[i].alpha = 0f;
        }

        terminalChoiceFadeRoutine = null;
    }

    private void ResetTerminalChoiceVisuals()
    {
        StopTerminalChoiceFade();
        EnsureChoiceSlots();

        if (eventTitleText != null)
            eventTitleText.gameObject.SetActive(!waitForEventEntranceReveal);

        if (choiceSlots == null)
            return;

        for (int i = 0; i < choiceSlots.Length; i++)
        {
            EventChoiceSlotUI slot = choiceSlots[i];
            if (slot == null)
                continue;

            CanvasGroup group = slot.GetComponent<CanvasGroup>();
            if (group == null)
                continue;

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private void StopTerminalChoiceFade()
    {
        if (terminalChoiceFadeRoutine == null)
            return;

        StopCoroutine(terminalChoiceFadeRoutine);
        terminalChoiceFadeRoutine = null;
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

        EnsureEventChoiceScrollViewReference();

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
        EnsureDustiumAcquireReferences();
        CacheDustiumAcquireOriginalState();
    }

    private void EnsureEventChoiceScrollViewReference()
    {
        if (eventChoiceScrollView != null)
            return;

        Transform scrollViewTransform = FindChildRecursive(transform, "Scroll View");
        if (scrollViewTransform != null)
            eventChoiceScrollView = scrollViewTransform as RectTransform;
    }

    private void EnsureEventChoiceGradationReference()
    {
        if (eventChoiceGradation != null)
            return;

        Transform gradationTransform = FindChildRecursive(transform, "Gradation");
        if (gradationTransform != null)
            eventChoiceGradation = gradationTransform as RectTransform;
    }

    private void ResetEventChoiceScrollViewPosition()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceGradationReference();
        StopEventChoiceScrollViewAnimation();

        if (eventChoiceScrollView != null)
        {
            Vector2 position = eventChoiceScrollView.anchoredPosition;
            position.y = eventChoiceScrollStartY;
            eventChoiceScrollView.anchoredPosition = position;
        }

        if (eventChoiceGradation != null)
        {
            Vector2 gradationPosition = eventChoiceGradation.anchoredPosition;
            gradationPosition.y = eventChoiceGradationStartY;
            eventChoiceGradation.anchoredPosition = gradationPosition;
        }
    }

    private void ResetEventChoiceScrollViewVisualState()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();

        if (eventChoiceScrollView == null)
            return;

        eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(true);

        if (eventChoiceScrollCanvasGroup == null)
            return;

        eventChoiceScrollCanvasGroup.alpha = 1f;
        eventChoiceScrollCanvasGroup.interactable = true;
        eventChoiceScrollCanvasGroup.blocksRaycasts = true;

        if (eventChoiceGradationCanvasGroup != null)
        {
            eventChoiceGradationCanvasGroup.alpha = 1f;
            eventChoiceGradationCanvasGroup.interactable = false;
            eventChoiceGradationCanvasGroup.blocksRaycasts = false;
        }

        EnsureEventTitleCanvasGroup();
        if (eventTitleCanvasGroup != null)
        {
            eventTitleCanvasGroup.alpha = 1f;
            eventTitleCanvasGroup.interactable = true;
            eventTitleCanvasGroup.blocksRaycasts = true;
        }
    }

    public void PlayEventChoiceEntranceAnimation()
    {
        if (!isActiveAndEnabled || !isDataEventActive)
            return;

        waitForEventEntranceReveal = false;

        if (eventTitleText != null)
            eventTitleText.gameObject.SetActive(true);

        StartEventChoiceScrollViewAnimation();
    }

    private void StartEventChoiceScrollViewAnimation()
    {
        EnsureEventChoiceScrollViewReference();
        EnsureEventChoiceGradationReference();
        StopEventChoiceScrollViewAnimation();

        if (eventChoiceScrollView == null || !isActiveAndEnabled)
            return;

        eventChoiceScrollMoveRoutine = StartCoroutine(MoveEventChoiceScrollViewRoutine());
    }

    private void StopEventChoiceScrollViewAnimation()
    {
        if (eventChoiceScrollMoveRoutine == null)
            return;

        StopCoroutine(eventChoiceScrollMoveRoutine);
        eventChoiceScrollMoveRoutine = null;
    }

    private IEnumerator MoveEventChoiceScrollViewRoutine()
    {
        EnsureEventChoiceScrollCanvasGroup();
        EnsureEventChoiceGradationCanvasGroup();

        eventChoiceScrollView.gameObject.SetActive(true);
        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(true);

        if (eventChoiceScrollCanvasGroup != null)
        {
            eventChoiceScrollCanvasGroup.alpha = 0f;
            eventChoiceScrollCanvasGroup.interactable = false;
            eventChoiceScrollCanvasGroup.blocksRaycasts = false;
        }

        if (eventChoiceGradationCanvasGroup != null)
        {
            eventChoiceGradationCanvasGroup.alpha = 0f;
            eventChoiceGradationCanvasGroup.interactable = false;
            eventChoiceGradationCanvasGroup.blocksRaycasts = false;
        }

        Vector2 position = eventChoiceScrollView.anchoredPosition;
        position.y = eventChoiceScrollStartY;
        eventChoiceScrollView.anchoredPosition = position;

        if (eventChoiceGradation != null)
        {
            Vector2 gradationPosition = eventChoiceGradation.anchoredPosition;
            gradationPosition.y = eventChoiceGradationStartY;
            eventChoiceGradation.anchoredPosition = gradationPosition;
        }

        Canvas.ForceUpdateCanvases();
        yield return null;

        float duration = Mathf.Max(0.01f, eventChoiceScrollMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            position = eventChoiceScrollView.anchoredPosition;
            position.y = Mathf.Lerp(eventChoiceScrollStartY, eventChoiceScrollEndY, easedT);
            eventChoiceScrollView.anchoredPosition = position;

            if (eventChoiceGradation != null)
            {
                Vector2 gradationPosition = eventChoiceGradation.anchoredPosition;
                gradationPosition.y = Mathf.Lerp(eventChoiceGradationStartY, eventChoiceGradationEndY, easedT);
                eventChoiceGradation.anchoredPosition = gradationPosition;
            }

            if (eventChoiceScrollCanvasGroup != null)
                eventChoiceScrollCanvasGroup.alpha = easedT;
            if (eventChoiceGradationCanvasGroup != null)
                eventChoiceGradationCanvasGroup.alpha = easedT;

            yield return null;
        }

        position = eventChoiceScrollView.anchoredPosition;
        position.y = eventChoiceScrollEndY;
        eventChoiceScrollView.anchoredPosition = position;

        if (eventChoiceGradation != null)
        {
            Vector2 gradationPosition = eventChoiceGradation.anchoredPosition;
            gradationPosition.y = eventChoiceGradationEndY;
            eventChoiceGradation.anchoredPosition = gradationPosition;
        }

        if (eventChoiceScrollCanvasGroup != null)
        {
            eventChoiceScrollCanvasGroup.alpha = 1f;
            eventChoiceScrollCanvasGroup.interactable = true;
            eventChoiceScrollCanvasGroup.blocksRaycasts = true;
        }

        if (eventChoiceGradationCanvasGroup != null)
            eventChoiceGradationCanvasGroup.alpha = 1f;

        eventChoiceScrollMoveRoutine = null;
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

        // NextButton이 표시되는 시점에는 선택지 배경 그라데이션이 남지 않도록 정리합니다.
        if (visible)
            HideEventChoiceGradationImmediate();
    }

    private void HideEventChoiceGradationImmediate()
    {
        EnsureEventChoiceGradationCanvasGroup();

        if (eventChoiceGradationCanvasGroup != null)
        {
            eventChoiceGradationCanvasGroup.alpha = 0f;
            eventChoiceGradationCanvasGroup.interactable = false;
            eventChoiceGradationCanvasGroup.blocksRaycasts = false;
        }

        if (eventChoiceGradation != null)
            eventChoiceGradation.gameObject.SetActive(false);
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
        ReturnToMap(null);
    }

    private void ReturnToMap(System.Action onCovered)
    {
        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap(onCovered);
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

        AudioManager.Instance.PlaySfx(acquireSfxId);
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
        SaveSystem.Instance?.ClearBattleRoomResumeState();
        SaveSystem.Instance?.SaveCheckpoint();

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
