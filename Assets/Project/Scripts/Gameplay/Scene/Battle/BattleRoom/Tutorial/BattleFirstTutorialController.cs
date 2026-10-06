using System;
using System.Collections;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 첫 번째 튜토리얼 전투(Map_27)의 전투 조작 안내를 진행합니다.
/// TutorialMode 아래의 FocusOverlay / HighlightFrame / TutorialTooltip을 자동으로 찾아 사용합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleFirstTutorialController : MonoBehaviour
{
    private enum TutorialStep
    {
        None,
        MonsterInfo,
        WaitMonsterInfoClose,
        Move,
        MoveGrid,
        Skill01Turn1,
        SkillRegisteredPreviewTurn1,
        EndTurn1,
        WaitSecondTurn,
        Skill01Turn2,
        SkillRegisteredPreviewTurn2,
        EndTurn2,
        Completed
    }

    public static BattleFirstTutorialController Instance { get; private set; }

    public bool IsRunning => isRunning && currentStep != TutorialStep.Completed;
    public bool IsTutorialRootActive => gameObject.activeInHierarchy;
    public int CurrentStepIndex => GetVisibleStepIndex(currentStep);
    public bool IsConfigured => focusOverlayRoot != null && topOverlay != null && bottomOverlay != null && leftOverlay != null && rightOverlay != null;

    public static bool IsInputRestricted => Instance != null && Instance.IsRunning;
    public static bool CanSelectMove => !IsInputRestricted || Instance.currentStep == TutorialStep.Move;
    public static bool CanSelectSkill01 => !IsInputRestricted ||
                                           Instance.currentStep == TutorialStep.Skill01Turn1 ||
                                           Instance.currentStep == TutorialStep.Skill01Turn2;
    public static bool CanExecuteEndTurn => !IsInputRestricted ||
                                            Instance.currentStep == TutorialStep.EndTurn1 ||
                                            Instance.currentStep == TutorialStep.EndTurn2;

    // 첫 번째 타임라인 등록 위치를 안내하는 동안에는 등록된 행동을 클릭해 제거할 수 없습니다.
    // 이 잠금은 1턴의 2초 안내 구간에서만 활성화됩니다.
    public static bool IsTimelineRegisteredSkillPreviewLocked =>
        Instance != null &&
        Instance.IsRunning &&
        Instance.currentStep == TutorialStep.SkillRegisteredPreviewTurn1;

    [Header("Auto Find Roots")]
    [SerializeField] private RectTransform focusOverlayRoot;
    [SerializeField] private RectTransform topOverlay;
    [SerializeField] private RectTransform bottomOverlay;
    [SerializeField] private RectTransform leftOverlay;
    [SerializeField] private RectTransform rightOverlay;
    [SerializeField] private RectTransform highlightFrame;
    [SerializeField] private RectTransform highlightLine;
    [SerializeField] private RectTransform tutorialTooltip;
    [SerializeField] private TMP_Text tutorialText;

    [Header("Focus")]
    [SerializeField, Min(0f)] private float focusPadding = 18f;
    [SerializeField, Min(0f)] private float highlightPadding = 8f;
    [SerializeField, Min(0f)] private float slideDuration = 0.28f;
    [SerializeField] private bool useUnscaledTime = true;

    // 전투 1의 첫 몬스터는 튜토리얼 전용 고정 배치이므로 월드->Canvas 자동 변환을 사용하지 않습니다.
    // TutorialMode / FocusOverlay의 기준 해상도(1920x1080) 로컬 좌표입니다.
    private static readonly Vector2 Battle1MonsterFocusCenter = new Vector2(250f, 30f);
    private static readonly Vector2 Battle1MonsterFocusSize = new Vector2(175.6307f, 175.5015f);

    [Header("Highlight Pulse")]
    [SerializeField, Min(1f)] private float highlightPulseMinScale = 1.02f;
    [SerializeField, Min(1f)] private float highlightPulseMaxScale = 1.08f;
    [SerializeField, Min(0.05f)] private float highlightPulseHalfDuration = 0.55f;

    [Header("Tooltip")]
    [SerializeField, Min(0f)] private float tooltipOffset = 28f;
    [SerializeField, Min(0f)] private float tooltipScreenPadding = 20f;

    [Header("Timeline Reservation Preview")]
    [Tooltip("스킬 예약 직후 타임라인에 등록된 Skill_Image를 짧게 보여주는 시간입니다.")]
    [SerializeField, Min(0.05f)] private float registeredSkillPreviewDuration = 2f;
    [Tooltip("Skill_Image가 활성화될 때까지 기다리는 최대 시간입니다.")]
    [SerializeField, Min(0.05f)] private float registeredSkillFindTimeout = 0.35f;

    [Header("Battle Tooltip Sorting")]
    [SerializeField, Min(1)] private int battleTooltipSortingOffset = 5;

    private BattleTurnExecutor turnExecutor;
    private BattleCharacterPanelUI characterPanelUI;
    private BattleTimelineController battleTimelineController;
    private RectTransform moveTarget;
    private RectTransform skill01Target;
    private RectTransform endButtonTarget;
    private GridCell grid16;
    private MonsterUnit tutorialMonster;
    private BattleCharacter tutorialCharacter;

    private TutorialStep currentStep;
    private bool isRunning;
    private int secondTurnSkill01SelectCount;
    private Coroutine focusAnimationCoroutine;
    private Coroutine highlightPulseCoroutine;
    private Coroutine restoreCharacterCoroutine;
    private Coroutine registeredSkillPreviewCoroutine;
    private string currentLocalizationKey;
    private string currentFallbackText;
    private Canvas battleSkillTooltipCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveUiReferences();
        HideFocusImmediate();
    }

    private void OnEnable()
    {
        MonsterUnit.MonsterInfoSelectionChanged -= HandleMonsterInfoSelectionChanged;
        MonsterUnit.MonsterInfoSelectionChanged += HandleMonsterInfoSelectionChanged;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        BattleTurnExecutor.PlayerTurnReturned += HandlePlayerTurnReturned;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        MonsterUnit.MonsterInfoSelectionChanged -= HandleMonsterInfoSelectionChanged;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (focusAnimationCoroutine != null)
        {
            StopCoroutine(focusAnimationCoroutine);
            focusAnimationCoroutine = null;
        }

        if (restoreCharacterCoroutine != null)
        {
            StopCoroutine(restoreCharacterCoroutine);
            restoreCharacterCoroutine = null;
        }

        if (registeredSkillPreviewCoroutine != null)
        {
            StopCoroutine(registeredSkillPreviewCoroutine);
            registeredSkillPreviewCoroutine = null;
        }

        StopHighlightPulse();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        TryStartTutorialIfNeeded();
    }

    private void Update()
    {
        if (!isRunning)
        {
            TryStartTutorialIfNeeded();
            return;
        }

        if (currentStep == TutorialStep.WaitMonsterInfoClose &&
            MonsterUnit.CurrentInfoSelectedMonster == null)
        {
            TryRestoreCharacterThenBeginMove();
        }
    }

    public bool TryStartTutorialIfNeeded()
    {
        if (isRunning || currentStep == TutorialStep.Completed)
            return false;

        if (!IsFirstTutorialBattle())
            return false;

        ResolveRuntimeReferences();
        ResolveUiReferences();
        ResolveBattleTargets();

        if (!CanBeginTutorial())
            return false;

        isRunning = true;
        BeginMonsterInfoStep();
        return true;
    }

    public bool StartTutorial()
    {
        if (!IsFirstTutorialBattle())
            return false;

        currentStep = TutorialStep.None;
        isRunning = false;
        return TryStartTutorialIfNeeded();
    }

    public bool StartPreviewTutorial() => StartTutorial();
    public void AdvanceStep() { }
    public void GoToPreviousPage() { }
    public void GoToNextPage() { }

    public void CloseTutorial()
    {
        isRunning = false;
        currentStep = TutorialStep.Completed;
        StopRegisteredSkillPreview();
        HideFocusImmediate();
    }

    public static void ResetAutoTutorialRunState()
    {
        if (Instance == null)
            return;

        Instance.isRunning = false;
        Instance.currentStep = TutorialStep.None;
        Instance.secondTurnSkill01SelectCount = 0;
        Instance.StopRegisteredSkillPreview();
        Instance.HideFocusImmediate();
    }

    public static bool TryHandleEscapeIfOpen() => false;

    public static bool CanClickMonster(MonsterUnit monster)
    {
        if (!IsInputRestricted)
            return true;

        return Instance.currentStep == TutorialStep.MonsterInfo &&
               monster != null &&
               monster == Instance.tutorialMonster;
    }

    public static bool CanClickGrid(int gridIndex)
    {
        if (!IsInputRestricted)
            return true;

        return Instance.currentStep == TutorialStep.MoveGrid && gridIndex == 16;
    }

    public static void NotifyMoveSelected()
    {
        if (!IsInputRestricted || Instance.currentStep != TutorialStep.Move)
            return;

        Instance.BeginMoveGridStep();
    }

    public static void NotifyGridClicked(int gridIndex)
    {
        if (!IsInputRestricted || Instance.currentStep != TutorialStep.MoveGrid || gridIndex != 16)
            return;

        Instance.BeginSkill01Turn1Step();
    }

    public static void NotifySkill01Selected()
    {
        if (!IsInputRestricted)
            return;

        if (Instance.currentStep == TutorialStep.Skill01Turn1)
        {
            Instance.BeginRegisteredSkillPreview(false);
            return;
        }

        if (Instance.currentStep != TutorialStep.Skill01Turn2)
            return;

        Instance.secondTurnSkill01SelectCount++;
        if (Instance.secondTurnSkill01SelectCount >= 2)
            Instance.BeginEndTurn2Step();
        else
            Instance.BeginSkill01Turn2Step(false);
    }

    public static void NotifyEndTurnRequested()
    {
        if (!IsInputRestricted)
            return;

        if (Instance.currentStep == TutorialStep.EndTurn1)
        {
            Instance.currentStep = TutorialStep.WaitSecondTurn;
            Instance.HideFocusImmediate();
            return;
        }

        if (Instance.currentStep == TutorialStep.EndTurn2)
        {
            Instance.isRunning = false;
            Instance.currentStep = TutorialStep.Completed;
            Instance.HideFocusImmediate();
        }
    }

    private void StopRegisteredSkillPreview()
    {
        if (registeredSkillPreviewCoroutine == null)
            return;

        StopCoroutine(registeredSkillPreviewCoroutine);
        registeredSkillPreviewCoroutine = null;
    }

    private void HandleMonsterInfoSelectionChanged(MonsterUnit monster)
    {
        if (!isRunning)
            return;

        if (currentStep == TutorialStep.MonsterInfo && monster == tutorialMonster)
        {
            currentStep = TutorialStep.WaitMonsterInfoClose;
            HideFocusImmediate();
            return;
        }

        if (currentStep == TutorialStep.WaitMonsterInfoClose && monster == null)
            TryRestoreCharacterThenBeginMove();
    }

    private void TryRestoreCharacterThenBeginMove()
    {
        if (restoreCharacterCoroutine != null || currentStep != TutorialStep.WaitMonsterInfoClose)
            return;

        restoreCharacterCoroutine = StartCoroutine(RestoreCharacterThenBeginMoveRoutine());
    }

    private IEnumerator RestoreCharacterThenBeginMoveRoutine()
    {
        // MonsterInfoPanel이 닫힌 같은 프레임의 선택 해제/패널 갱신이 모두 끝난 뒤
        // 마지막으로 선택했던 캐릭터를 다시 선택합니다.
        yield return null;

        ResolveRuntimeReferences();
        if (battleTimelineController != null)
            battleTimelineController.RestoreLastSelectedCharacterAfterMonsterInfo();

        float waited = 0f;
        const float maxWait = 3f;

        // 캐릭터 선택 복원과 BattleCharacterPanel/BattleSlot 상승 애니메이션이
        // 모두 끝난 뒤에 Move 포커스를 시작합니다.
        while (waited < maxWait && currentStep == TutorialStep.WaitMonsterInfoClose)
        {
            ResolveRuntimeReferences();

            bool characterRestored = battleTimelineController != null &&
                                     battleTimelineController.SelectedCharacter != null;
            bool panelReady = characterPanelUI != null &&
                              characterPanelUI.IsAtReservationPosition;

            if (characterRestored && panelReady)
                break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        restoreCharacterCoroutine = null;

        if (currentStep == TutorialStep.WaitMonsterInfoClose)
            BeginMoveStep();
    }

    private void HandlePlayerTurnReturned()
    {
        if (!isRunning || currentStep != TutorialStep.WaitSecondTurn)
            return;

        StartCoroutine(BeginSecondTurnWhenReadyRoutine());
    }

    private IEnumerator BeginSecondTurnWhenReadyRoutine()
    {
        // PlayerTurnReturned는 다음 예약 턴 준비가 시작됐다는 신호입니다.
        // BattleCharacterPanel/BattleSlot의 상승 애니메이션은 같은 프레임에 끝나지 않으므로
        // 실제 예약 위치까지 모두 올라온 뒤 Skill01 포커스를 표시합니다.
        yield return null;

        float waited = 0f;
        const float maxWait = 3f;

        while (waited < maxWait && currentStep == TutorialStep.WaitSecondTurn)
        {
            ResolveRuntimeReferences();

            bool inputReady = turnExecutor != null && turnExecutor.CanAcceptPlayerInput;
            bool characterRestored = battleTimelineController != null &&
                                     battleTimelineController.SelectedCharacter != null;
            bool panelAndBattleSlotReady = characterPanelUI != null &&
                                           characterPanelUI.IsAtReservationPosition;

            if (inputReady && characterRestored && panelAndBattleSlotReady)
                break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (currentStep == TutorialStep.WaitSecondTurn)
            BeginSkill01Turn2Step();
    }

    private void BeginMonsterInfoStep()
    {
        ResolveBattleTargets();
        if (tutorialMonster == null)
            return;

        currentStep = TutorialStep.MonsterInfo;

        // 첫 튜토리얼 몬스터는 위치가 고정되어 있습니다.
        // 월드 오브젝트의 Sprite/Collider/Pivot 구조에 따라 Canvas 변환값이 흔들리지 않도록
        // 포커스 구멍과 HighlightFrame 모두 동일한 고정 Canvas 좌표를 사용합니다.
        Rect monsterRect = new Rect(
            Battle1MonsterFocusCenter - Battle1MonsterFocusSize * 0.5f,
            Battle1MonsterFocusSize);

        ShowFocus(
            Expand(monsterRect, focusPadding),
            Expand(monsterRect, highlightPadding),
            LocalizationKeys.Tutorial.Battle1MonsterInfo,
            "변이체를 클릭하여 정보를 파악할 수 있습니다.");
    }

    private void BeginMoveStep()
    {
        ResolveBattleTargets();
        if (moveTarget == null)
            return;

        currentStep = TutorialStep.Move;
        ShowUiFocus(
            moveTarget,
            LocalizationKeys.Tutorial.Battle1Move,
            "클릭하거나 A를 눌러 이동을 선택합니다.");
    }

    private void BeginMoveGridStep()
    {
        ResolveBattleTargets();
        if (grid16 == null)
            return;

        currentStep = TutorialStep.MoveGrid;

        Rect revealRect;
        Rect highlightRect;
        if (!TryGetWorldRect(grid16.gameObject, out highlightRect))
            return;

        revealRect = highlightRect;
        if (tutorialCharacter != null && TryGetWorldRect(tutorialCharacter.gameObject, out Rect characterRect))
            revealRect = Union(revealRect, characterRect);

        ShowFocus(
            Expand(revealRect, focusPadding),
            Expand(highlightRect, highlightPadding),
            LocalizationKeys.Tutorial.Battle1MoveGrid,
            "그리드를 클릭하여 이동을 진행합니다.");
    }

    private void BeginSkill01Turn1Step()
    {
        ResolveBattleTargets();
        if (skill01Target == null)
            return;

        currentStep = TutorialStep.Skill01Turn1;
        ShowUiFocus(
            skill01Target,
            LocalizationKeys.Tutorial.Battle1Skill01,
            "클릭하거나 Q를 사용하여 공격을 예약합니다.");
    }

    private void BeginRegisteredSkillPreview(bool secondTurn)
    {
        if (registeredSkillPreviewCoroutine != null)
            StopCoroutine(registeredSkillPreviewCoroutine);

        currentStep = secondTurn
            ? TutorialStep.SkillRegisteredPreviewTurn2
            : TutorialStep.SkillRegisteredPreviewTurn1;

        registeredSkillPreviewCoroutine = StartCoroutine(RegisteredSkillPreviewRoutine(secondTurn));
    }

    private IEnumerator RegisteredSkillPreviewRoutine(bool secondTurn)
    {
        // SelectSkill 호출 직후 같은 프레임에는 Timeline UI의 Skill_Image 갱신이
        // 아직 끝나지 않았을 수 있으므로 짧게 기다리면서 현재 슬롯의 마지막 등록 이미지를 찾습니다.
        RectTransform registeredSkillImage = null;
        float waited = 0f;
        float timeout = Mathf.Max(0.05f, registeredSkillFindTimeout);

        while (waited < timeout && registeredSkillImage == null)
        {
            registeredSkillImage = FindLatestRegisteredSkillImage();
            if (registeredSkillImage != null)
                break;

            waited += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (registeredSkillImage != null)
        {
            ShowUiFocus(
                registeredSkillImage,
                string.Empty,
                "예약한 행동은 타임라인에 등록됩니다.",
                true);

            float duration = Mathf.Max(0.05f, registeredSkillPreviewDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        registeredSkillPreviewCoroutine = null;

        if (!isRunning)
            yield break;

        if (!secondTurn)
        {
            if (currentStep == TutorialStep.SkillRegisteredPreviewTurn1)
                BeginEndTurn1Step();
            yield break;
        }

        if (currentStep != TutorialStep.SkillRegisteredPreviewTurn2)
            yield break;

        if (secondTurnSkill01SelectCount >= 2)
            BeginEndTurn2Step();
        else
            BeginSkill01Turn2Step(false);
    }

    private RectTransform FindLatestRegisteredSkillImage()
    {
        ResolveRuntimeReferences();
        if (battleTimelineController == null)
            return null;

        Transform timelineRoot = FindChildRecursive(battleTimelineController.transform, "TimelineBar");
        if (timelineRoot == null)
            timelineRoot = battleTimelineController.transform;

        int slotIndex = Mathf.Clamp(battleTimelineController.ActiveSlotIndex, 0, 4);
        Transform slot = FindChildRecursive(timelineRoot, "TimelineSlot" + (slotIndex + 1).ToString("00"));
        if (slot == null)
            slot = FindChildRecursive(battleTimelineController.transform, "TimelineSlot" + (slotIndex + 1).ToString("00"));

        if (slot == null)
            return null;

        // 한 슬롯 안에서는 Order01 -> Order05 순으로 등록되므로 뒤에서부터 찾으면
        // 방금 등록된 행동의 Skill_Image를 가장 먼저 얻을 수 있습니다.
        for (int orderNumber = 5; orderNumber >= 1; orderNumber--)
        {
            Transform order = FindChildRecursive(slot, "Order" + orderNumber.ToString("00"));
            if (order == null || !order.gameObject.activeInHierarchy)
                continue;

            Transform useSkill = FindChildRecursive(order, "Use_skill");
            if (useSkill == null || !useSkill.gameObject.activeInHierarchy)
                continue;

            Transform skillImageTransform = FindChildRecursive(useSkill, "Skill_Image");
            if (skillImageTransform == null || !skillImageTransform.gameObject.activeInHierarchy)
                continue;

            Image image = skillImageTransform.GetComponent<Image>();
            if (image != null && image.enabled && image.sprite != null && image.color.a > 0.001f)
                return skillImageTransform as RectTransform;
        }

        return null;
    }

    private void BeginEndTurn1Step()
    {
        ResolveBattleTargets();
        if (endButtonTarget == null)
            return;

        currentStep = TutorialStep.EndTurn1;
        ShowUiFocus(
            endButtonTarget,
            LocalizationKeys.Tutorial.Battle1EndTurn,
            "턴 엔드 버튼 또는 Space를 눌러 예약을 종료합니다.");
    }

    private void BeginSkill01Turn2Step()
    {
        BeginSkill01Turn2Step(true);
    }

    private void BeginSkill01Turn2Step(bool resetSelectionCount)
    {
        ResolveBattleTargets();
        if (skill01Target == null)
            return;

        if (resetSelectionCount)
            secondTurnSkill01SelectCount = 0;

        currentStep = TutorialStep.Skill01Turn2;
        ShowUiFocus(
            skill01Target,
            LocalizationKeys.Tutorial.Battle1Skill01Repeat,
            secondTurnSkill01SelectCount <= 0
                ? "같은 공격을 연속으로 예약할 수 있습니다. 공격을 두 번 예약해 보세요."
                : "같은 공격을 한 번 더 예약해 보세요.");
    }

    private void BeginEndTurn2Step()
    {
        ResolveBattleTargets();
        if (endButtonTarget == null)
            return;

        currentStep = TutorialStep.EndTurn2;
        ShowUiFocus(
            endButtonTarget,
            LocalizationKeys.Tutorial.Battle1EndTurnRepeat,
            "턴 엔드 버튼 또는 Space를 눌러 예약을 종료합니다.");
    }

    private bool CanBeginTutorial()
    {
        if (!IsConfigured)
            return false;

        if (turnExecutor == null || !turnExecutor.CanAcceptPlayerInput)
            return false;

        return moveTarget != null && skill01Target != null && endButtonTarget != null && grid16 != null && tutorialMonster != null;
    }

    private void ResolveRuntimeReferences()
    {
        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        if (characterPanelUI == null)
            characterPanelUI = FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Include);

        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private void ResolveBattleTargets()
    {
        ResolveRuntimeReferences();

        if (characterPanelUI != null)
        {
            Transform active = FindChildRecursive(characterPanelUI.transform, "Active");
            moveTarget = FindChildRecursive(active, "Move") as RectTransform;
            skill01Target = FindChildRecursive(active, "Skill01") as RectTransform;
        }

        if (turnExecutor != null && turnExecutor.EndTurnButton != null)
            endButtonTarget = turnExecutor.EndTurnButton.transform as RectTransform;

        GridCell[] cells = FindObjectsByType<GridCell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null && cells[i].Index == 16)
            {
                grid16 = cells[i];
                break;
            }
        }

        MonsterUnit[] monsters = FindObjectsByType<MonsterUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            tutorialMonster = monster;
            break;
        }

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        tutorialCharacter = null;
        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];
            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            if (character.CurrentGridIndex == 11)
            {
                tutorialCharacter = character;
                break;
            }

            if (tutorialCharacter == null)
                tutorialCharacter = character;
        }
    }

    private void ResolveUiReferences()
    {
        if (focusOverlayRoot == null)
            focusOverlayRoot = FindChildRecursive(transform, "FocusOverlay") as RectTransform;

        if (focusOverlayRoot != null)
        {
            topOverlay = topOverlay != null ? topOverlay : FindChildRecursive(focusOverlayRoot, "Top") as RectTransform;
            bottomOverlay = bottomOverlay != null ? bottomOverlay : FindChildRecursive(focusOverlayRoot, "Bottom") as RectTransform;
            leftOverlay = leftOverlay != null ? leftOverlay : FindChildRecursive(focusOverlayRoot, "Left") as RectTransform;
            rightOverlay = rightOverlay != null ? rightOverlay : FindChildRecursive(focusOverlayRoot, "Right") as RectTransform;
        }

        if (highlightFrame == null)
            highlightFrame = FindChildRecursive(transform, "HighlightFrame") as RectTransform;

        if (highlightFrame != null && highlightLine == null)
            highlightLine = FindChildRecursive(highlightFrame, "Line") as RectTransform;

        if (tutorialTooltip == null)
            tutorialTooltip = FindChildRecursive(transform, "TutorialTooltip") as RectTransform;

        if (tutorialTooltip != null && tutorialText == null)
        {
            Transform textTransform = FindChildRecursive(tutorialTooltip, "Text");
            if (textTransform != null)
                tutorialText = textTransform.GetComponent<TMP_Text>();
        }

        SetGraphicRaycastState(topOverlay, true);
        SetGraphicRaycastState(bottomOverlay, true);
        SetGraphicRaycastState(leftOverlay, true);
        SetGraphicRaycastState(rightOverlay, true);
        SetGraphicRaycastState(highlightFrame, false);
        SetGraphicRaycastState(tutorialTooltip, false);
        EnsureTutorialCanvasAboveBattleHud();
        EnsureBattleSkillTooltipAboveFocus();
    }

    private void EnsureTutorialCanvasAboveBattleHud()
    {
        Canvas tutorialCanvas = GetComponent<Canvas>();
        if (tutorialCanvas == null)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int battleHudOrder = int.MinValue;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == tutorialCanvas)
                continue;

            if (canvas.name == "BattleHUDCanvas")
                battleHudOrder = Mathf.Max(battleHudOrder, canvas.sortingOrder);
        }

        if (battleHudOrder == int.MinValue)
            return;

        tutorialCanvas.overrideSorting = true;
        tutorialCanvas.sortingOrder = Mathf.Max(tutorialCanvas.sortingOrder, battleHudOrder + 10);
    }

    private void EnsureBattleSkillTooltipAboveFocus()
    {
        Canvas tutorialCanvas = GetComponent<Canvas>();
        if (tutorialCanvas == null)
            return;

        if (characterPanelUI == null)
            characterPanelUI = FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Include);

        if (characterPanelUI == null)
            return;

        Transform tooltipTransform = FindChildRecursive(characterPanelUI.transform, "TooltipPanel");
        if (tooltipTransform == null)
            return;

        if (battleSkillTooltipCanvas == null)
            battleSkillTooltipCanvas = tooltipTransform.GetComponent<Canvas>();

        if (battleSkillTooltipCanvas == null)
            battleSkillTooltipCanvas = tooltipTransform.gameObject.AddComponent<Canvas>();

        battleSkillTooltipCanvas.overrideSorting = true;
        battleSkillTooltipCanvas.sortingOrder = tutorialCanvas.sortingOrder + Mathf.Max(10, battleTooltipSortingOffset);
    }

    private void ShowUiFocus(RectTransform target, string localizationKey, string fallback, bool tooltipOnRight = false)
    {
        if (target == null || !TryGetUiRect(target, out Rect rect))
            return;

        ShowFocus(Expand(rect, focusPadding), Expand(rect, highlightPadding), localizationKey, fallback, tooltipOnRight);
    }

    private void ShowWorldFocus(GameObject target, string localizationKey, string fallback)
    {
        if (target == null || !TryGetWorldRect(target, out Rect rect))
            return;

        ShowFocus(Expand(rect, focusPadding), Expand(rect, highlightPadding), localizationKey, fallback);
    }

    private void ShowFocus(Rect revealRect, Rect highlightRect, string localizationKey, string fallback, bool tooltipOnRight = false)
    {
        ResolveUiReferences();
        if (!IsConfigured)
            return;

        EnsureBattleSkillTooltipAboveFocus();
        currentLocalizationKey = localizationKey;
        currentFallbackText = fallback;
        RefreshTutorialText();

        if (focusAnimationCoroutine != null)
            StopCoroutine(focusAnimationCoroutine);

        focusAnimationCoroutine = StartCoroutine(AnimateFocusPanelsRoutine(revealRect));
        SetRect(highlightFrame, highlightRect);
        SyncHighlightLineToFrame();

        if (highlightFrame != null)
            highlightFrame.gameObject.SetActive(true);

        StartHighlightPulse();

        if (tutorialTooltip != null)
        {
            tutorialTooltip.gameObject.SetActive(true);
            PositionTooltip(revealRect, tooltipOnRight);
        }
    }


    private void StartHighlightPulse()
    {
        StopHighlightPulse();

        if (highlightLine == null || !highlightLine.gameObject.activeInHierarchy)
            return;

        highlightPulseCoroutine = StartCoroutine(HighlightPulseRoutine());
    }

    private void StopHighlightPulse()
    {
        if (highlightPulseCoroutine != null)
        {
            StopCoroutine(highlightPulseCoroutine);
            highlightPulseCoroutine = null;
        }

        if (highlightLine != null)
            highlightLine.localScale = Vector3.one;
    }

    private void SyncHighlightLineToFrame()
    {
        if (highlightFrame == null || highlightLine == null)
            return;

        // Line 자체의 Width / Height도 현재 선택 대상의 실제 화면 크기에 맞춥니다.
        // highlightFrame은 이미 highlightPadding이 적용된 Rect이므로 Line은 타겟보다 약간 크게 표시됩니다.
        highlightLine.anchorMin = highlightLine.anchorMax = new Vector2(0.5f, 0.5f);
        highlightLine.pivot = new Vector2(0.5f, 0.5f);
        highlightLine.anchoredPosition = Vector2.zero;
        highlightLine.sizeDelta = new Vector2(
            Mathf.Max(0f, highlightFrame.rect.width),
            Mathf.Max(0f, highlightFrame.rect.height));
        highlightLine.localScale = Vector3.one;
    }

    private IEnumerator HighlightPulseRoutine()
    {
        float minScale = Mathf.Max(1f, highlightPulseMinScale);
        float maxScale = Mathf.Max(minScale, highlightPulseMaxScale);
        float halfDuration = Mathf.Max(0.05f, highlightPulseHalfDuration);

        while (highlightFrame != null && highlightFrame.gameObject.activeInHierarchy)
        {
            yield return AnimateHighlightLineScale(minScale, maxScale, halfDuration);
            yield return AnimateHighlightLineScale(maxScale, minScale, halfDuration);
        }

        highlightPulseCoroutine = null;
    }

    private IEnumerator AnimateHighlightLineScale(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (highlightLine == null)
                yield break;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            float scale = Mathf.LerpUnclamped(from, to, t);
            highlightLine.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        if (highlightLine != null)
            highlightLine.localScale = new Vector3(to, to, 1f);
    }

    private IEnumerator AnimateFocusPanelsRoutine(Rect holeRect)
    {
        focusOverlayRoot.gameObject.SetActive(true);
        topOverlay.gameObject.SetActive(true);
        bottomOverlay.gameObject.SetActive(true);
        leftOverlay.gameObject.SetActive(true);
        rightOverlay.gameObject.SetActive(true);
        SetOverlayFinalRects(holeRect, out Rect topRect, out Rect bottomRect, out Rect leftRect, out Rect rightRect);

        Rect rootRect = focusOverlayRoot.rect;
        Vector2 topFinal = topRect.center;
        Vector2 bottomFinal = bottomRect.center;
        Vector2 leftFinal = leftRect.center;
        Vector2 rightFinal = rightRect.center;

        SetRect(topOverlay, topRect);
        SetRect(bottomOverlay, bottomRect);
        SetRect(leftOverlay, leftRect);
        SetRect(rightOverlay, rightRect);

        Vector2 topStart = topFinal + Vector2.up * rootRect.height;
        Vector2 bottomStart = bottomFinal + Vector2.down * rootRect.height;
        Vector2 leftStart = leftFinal + Vector2.left * rootRect.width;
        Vector2 rightStart = rightFinal + Vector2.right * rootRect.width;

        topOverlay.anchoredPosition = topStart;
        bottomOverlay.anchoredPosition = bottomStart;
        leftOverlay.anchoredPosition = leftStart;
        rightOverlay.anchoredPosition = rightStart;

        float duration = Mathf.Max(0f, slideDuration);
        if (duration <= 0f)
        {
            topOverlay.anchoredPosition = topFinal;
            bottomOverlay.anchoredPosition = bottomFinal;
            leftOverlay.anchoredPosition = leftFinal;
            rightOverlay.anchoredPosition = rightFinal;
            focusAnimationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            topOverlay.anchoredPosition = Vector2.LerpUnclamped(topStart, topFinal, t);
            bottomOverlay.anchoredPosition = Vector2.LerpUnclamped(bottomStart, bottomFinal, t);
            leftOverlay.anchoredPosition = Vector2.LerpUnclamped(leftStart, leftFinal, t);
            rightOverlay.anchoredPosition = Vector2.LerpUnclamped(rightStart, rightFinal, t);
            yield return null;
        }

        topOverlay.anchoredPosition = topFinal;
        bottomOverlay.anchoredPosition = bottomFinal;
        leftOverlay.anchoredPosition = leftFinal;
        rightOverlay.anchoredPosition = rightFinal;
        focusAnimationCoroutine = null;
    }

    private void SetOverlayFinalRects(Rect holeRect, out Rect topRect, out Rect bottomRect, out Rect leftRect, out Rect rightRect)
    {
        Rect rootRect = focusOverlayRoot.rect;
        holeRect.xMin = Mathf.Clamp(holeRect.xMin, rootRect.xMin, rootRect.xMax);
        holeRect.xMax = Mathf.Clamp(holeRect.xMax, rootRect.xMin, rootRect.xMax);
        holeRect.yMin = Mathf.Clamp(holeRect.yMin, rootRect.yMin, rootRect.yMax);
        holeRect.yMax = Mathf.Clamp(holeRect.yMax, rootRect.yMin, rootRect.yMax);

        topRect = Rect.MinMaxRect(rootRect.xMin, holeRect.yMax, rootRect.xMax, rootRect.yMax);
        bottomRect = Rect.MinMaxRect(rootRect.xMin, rootRect.yMin, rootRect.xMax, holeRect.yMin);
        leftRect = Rect.MinMaxRect(rootRect.xMin, holeRect.yMin, holeRect.xMin, holeRect.yMax);
        rightRect = Rect.MinMaxRect(holeRect.xMax, holeRect.yMin, rootRect.xMax, holeRect.yMax);
    }

    private void PositionTooltip(Rect targetRect, bool placeOnRight = false)
    {
        if (tutorialTooltip == null || focusOverlayRoot == null)
            return;

        Rect rootRect = focusOverlayRoot.rect;
        Vector2 size = tutorialTooltip.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            size = tutorialTooltip.sizeDelta;

        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        // 튜토리얼 설명창은 화면 왼쪽에 고정하지 않고 항상 현재 포커스 대상의 왼쪽에 둡니다.
        // 스킬/이동 TooltipPanel은 보통 대상의 위/오른쪽에 표시되므로 서로 겹치는 것도 피할 수 있습니다.
        Vector2 position = placeOnRight
            ? new Vector2(targetRect.xMax + tooltipOffset + halfW, targetRect.center.y)
            : new Vector2(targetRect.xMin - tooltipOffset - halfW, targetRect.center.y);

        position.x = Mathf.Clamp(
            position.x,
            rootRect.xMin + halfW + tooltipScreenPadding,
            rootRect.xMax - halfW - tooltipScreenPadding);
        position.y = Mathf.Clamp(
            position.y,
            rootRect.yMin + halfH + tooltipScreenPadding,
            rootRect.yMax - halfH - tooltipScreenPadding);

        tutorialTooltip.anchorMin = tutorialTooltip.anchorMax = new Vector2(0.5f, 0.5f);
        tutorialTooltip.pivot = new Vector2(0.5f, 0.5f);
        tutorialTooltip.anchoredPosition = position;
    }

    private bool TryGetUiRect(RectTransform target, out Rect rect)
    {
        rect = default;
        if (target == null || focusOverlayRoot == null)
            return false;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;

        return TryBuildLocalRectFromWorldCorners(corners, targetCamera, out rect);
    }

    private bool TryGetWorldRect(GameObject target, out Rect rect)
    {
        rect = default;
        if (target == null || focusOverlayRoot == null)
            return false;

        Camera camera = Camera.main;
        if (camera == null)
            return false;

        // 월드 몬스터는 Canvas 좌표를 직접 사용할 수 없습니다.
        // 실제 화면에 그려지는 몬스터 SpriteRenderer의 월드 Bounds를 Screen 좌표로 투영한 뒤
        // FocusOverlay의 로컬 Canvas 좌표로 다시 변환합니다. Collider2D는 프리팹 피벗/자동 생성
        // 위치 때문에 시각적인 몬스터와 어긋날 수 있으므로 포커스 영역 계산에는 사용하지 않습니다.
        MonsterUnit monster = target.GetComponent<MonsterUnit>();
        if (monster == null)
            monster = target.GetComponentInParent<MonsterUnit>();

        if (monster != null && TryGetVisibleSpriteBounds(monster.gameObject, out Bounds spriteBounds))
            return TryGetBoundsRect(spriteBounds, camera, out rect);

        if (!TryGetWorldBounds(target, out Bounds bounds))
            return false;

        return TryGetBoundsRect(bounds, camera, out rect);
    }

    private bool TryGetBoundsRect(Bounds bounds, Camera sourceCamera, out Rect rect)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new(min.x, min.y, min.z),
            new(min.x, min.y, max.z),
            new(min.x, max.y, min.z),
            new(min.x, max.y, max.z),
            new(max.x, min.y, min.z),
            new(max.x, min.y, max.z),
            new(max.x, max.y, min.z),
            new(max.x, max.y, max.z)
        };

        return TryBuildLocalRectFromWorldCorners(corners, sourceCamera, out rect);
    }

    private static bool TryGetVisibleSpriteBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        SpriteRenderer[] sprites = target.GetComponentsInChildren<SpriteRenderer>(false);
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sprite = sprites[i];
            if (sprite == null || !sprite.enabled || sprite.sprite == null || sprite.color.a <= 0.001f)
                continue;

            if (!hasBounds)
            {
                bounds = sprite.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sprite.bounds);
            }
        }

        return hasBounds;
    }

    private bool TryBuildLocalRectFromWorldCorners(Vector3[] corners, Camera sourceCamera, out Rect rect)
    {
        rect = default;
        if (corners == null || corners.Length == 0 || focusOverlayRoot == null)
            return false;

        Canvas overlayCanvas = focusOverlayRoot.GetComponentInParent<Canvas>();
        Camera overlayCamera = overlayCanvas != null && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? overlayCanvas.worldCamera
            : null;

        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, corners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(focusOverlayRoot, screen, overlayCamera, out Vector2 local))
                continue;

            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        if (float.IsInfinity(min.x) || float.IsInfinity(min.y))
            return false;

        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private static bool TryGetWorldBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return true;

        Collider[] colliders = target.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        Collider2D[] colliders2D = target.GetComponentsInChildren<Collider2D>(false);
        for (int i = 0; i < colliders2D.Length; i++)
        {
            Collider2D collider = colliders2D[i];
            if (collider == null || !collider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private static Rect Expand(Rect rect, float padding)
    {
        float safePadding = Mathf.Max(0f, padding);
        return Rect.MinMaxRect(
            rect.xMin - safePadding,
            rect.yMin - safePadding,
            rect.xMax + safePadding,
            rect.yMax + safePadding);
    }

    private static Rect Union(Rect a, Rect b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    private static void SetRect(RectTransform target, Rect rect)
    {
        if (target == null)
            return;

        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.sizeDelta = new Vector2(Mathf.Max(0f, rect.width), Mathf.Max(0f, rect.height));
        target.anchoredPosition = rect.center;
    }

    private void HideFocusImmediate()
    {
        StopHighlightPulse();

        if (focusAnimationCoroutine != null)
        {
            StopCoroutine(focusAnimationCoroutine);
            focusAnimationCoroutine = null;
        }

        if (restoreCharacterCoroutine != null)
        {
            StopCoroutine(restoreCharacterCoroutine);
            restoreCharacterCoroutine = null;
        }

        if (focusOverlayRoot != null)
            focusOverlayRoot.gameObject.SetActive(false);
        if (highlightFrame != null)
            highlightFrame.gameObject.SetActive(false);
        if (tutorialTooltip != null)
            tutorialTooltip.gameObject.SetActive(false);

        currentLocalizationKey = string.Empty;
        currentFallbackText = string.Empty;
    }

    private void RefreshTutorialText()
    {
        if (tutorialText == null)
            return;

        tutorialText.text = GameLocalization.Get(currentLocalizationKey, currentFallbackText);
    }

    private void HandleLocaleChanged(Locale _)
    {
        if (!isRunning || string.IsNullOrWhiteSpace(currentLocalizationKey))
            return;

        RefreshTutorialText();
    }

    private static void SetGraphicRaycastState(RectTransform root, bool raycastTarget)
    {
        if (root == null)
            return;

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = raycastTarget;
        }
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool IsFirstTutorialBattle()
    {
        DataManager dataManager = DataManager.Instance;
        BattleRuntimeData battle = dataManager?.BattleRuntimeStore?.Get();
        MapRuntimeData map = dataManager?.MapRuntimeStore?.Get();

        return battle?.IsTutorialBattle == true &&
               map != null &&
               string.Equals(map.CurrentMapId, TutorialBattleEntrySetup.FirstTutorialMapId, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetVisibleStepIndex(TutorialStep step)
    {
        return step switch
        {
            TutorialStep.MonsterInfo or TutorialStep.WaitMonsterInfoClose => 1,
            TutorialStep.Move => 2,
            TutorialStep.MoveGrid => 3,
            TutorialStep.Skill01Turn1 => 4,
            TutorialStep.EndTurn1 => 5,
            TutorialStep.Skill01Turn2 => 6,
            TutorialStep.EndTurn2 => 7,
            _ => -1
        };
    }
}
