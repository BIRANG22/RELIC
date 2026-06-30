using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTimelineController : MonoBehaviour
{
    [Header("Timeline")]
    [Tooltip("이전 구조 호환용입니다. 비어 있지 않으면 TimelineBar1로 사용합니다.")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI;
    [Tooltip("홀수 턴에 예약 표시를 담당하는 TimelineBar입니다.")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI1;
    [Tooltip("짝수 턴에 예약 표시를 담당하는 TimelineBar입니다.")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI2;
    [SerializeField] private ReserveTurnSlotUI[] reserveSlots;

    [Header("Reservation Preview")]
    [SerializeField] private PlayerSkillReservationController playerSkillReservationController;

    [Header("MoveGhostPreview")]
    [SerializeField] private MoveGhostPreview moveGhostPreview;

    [Header("Grid")]
    [SerializeField] private GridManager gridManager;

    [Header("Selected Slot Text")]
    [SerializeField] private TMP_Text selectedSlotValueText;
    [SerializeField] private string emptySelectedSlotText = "-";
    [SerializeField] private bool autoFindSelectedSlotValueText = true;

    [Header("Selected Slot Effect")]
    [SerializeField] private Transform selectedSlotEffect;
    [SerializeField] private bool autoFindSelectedSlotEffect = true;
    [SerializeField] private string selectedSlotEffectObjectName = "Effect";
    [SerializeField] private float selectedSlotEffectRotateStepZ = -60f;
    [SerializeField] private float selectedSlotEffectDuration = 0.08f;
    [SerializeField] private bool useUnscaledTimeForSelectedSlotEffect = true;

    [Header("Selected Slot Gear Effects")]
    [SerializeField] private Transform selectedSlotLargeGearEffect;
    [SerializeField] private bool autoFindSelectedSlotLargeGearEffect = false;
    [SerializeField] private string selectedSlotLargeGearEffectObjectName = "LargeGear";
    [SerializeField] private float selectedSlotLargeGearRotateStepZ = 30f;
    [SerializeField] private Transform selectedSlotSmallGearEffect;
    [SerializeField] private bool autoFindSelectedSlotSmallGearEffect = false;
    [SerializeField] private string selectedSlotSmallGearEffectObjectName = "SmallGear";
    [SerializeField] private float selectedSlotSmallGearRotateStepZ = -90f;

    [Header("Selected Slot Effect SFX")]
    [SerializeField] private bool playSelectedSlotEffectSfx = true;
    [SerializeField] private SfxType selectedSlotEffectSfxType = SfxType.BattleTimelineSlotRotate;
    [SerializeField, Range(0f, 1f)] private float selectedSlotEffectSfxVolume = 1f;

    [Header("Slot Selection Lock")]
    [SerializeField] private bool showWarningWhenSlotSelectionLocked = false;
    [SerializeField] private string slotSelectionLockedMessage = "턴 진행 중에는 슬롯을 선택할 수 없습니다.";

    [Header("Auto Slot Selection")]
    [SerializeField] private bool autoSelectFirstSlotWhenInputReady = true;
    [SerializeField] private int defaultSlotIndex = 0;

    [Header("Character Selection Camera Focus")]
    [SerializeField] private bool focusCameraOnCharacterSelect = true;
    [SerializeField] private bool focusCameraOnlyWhenInputReady = true;
    [SerializeField] private bool refocusSameCharacter = false;

    [Header("Keyboard Input")]
    [SerializeField] private bool enableNumberKeySlotSelection = true;
    [SerializeField] private BattleTurnExecutor turnExecutor;

    [Header("Timeline Bar Slide")]
    [SerializeField] private bool playTimelineSlotSlide = true;
    [Tooltip("이전 구조 호환용입니다. 비어 있지 않으면 TimelineBar1 이동 대상으로 사용합니다.")]
    [SerializeField] private RectTransform timelineBarSlideTarget;
    [Tooltip("홀수 턴에 사용하는 TimelineBar1 이동 대상입니다.")]
    [SerializeField] private RectTransform timelineBarSlideTarget1;
    [Tooltip("짝수 턴에 사용하는 TimelineBar2 이동 대상입니다.")]
    [SerializeField] private RectTransform timelineBarSlideTarget2;
    [Tooltip("대기 중인 TimelineBar를 현재 TimelineBar 오른쪽에 이어붙일 X 거리입니다.")]
    [SerializeField] private float standbyTimelineBarOffsetX = 1420f;
    [Tooltip("5슬롯까지 모두 진행된 뒤 현재 TimelineBar가 도착해야 하는 X 위치입니다. 기본 위치 X=0 기준입니다. 5슬롯 종료 위치에서 이 값까지 추가 이동합니다.")]
    [SerializeField] private float completedTurnTimelineBarPositionX = -1420f;
    [Tooltip("턴엔드 버튼을 누른 직후, 1번 슬롯이 시작되기 전에 TimelineBar가 먼저 왼쪽으로 이동하는 거리입니다. 1번 슬롯에서만 한 번 적용됩니다.")]
    [SerializeField] private float firstSlotEndTurnTimelineLineSlideAmountX = -60f;
    [SerializeField] private float timelineSlotSlideDuration = 0.18f;
    [Tooltip("TurnMark와 Use_skill의 4프레임 갈림 애니메이션이 눈에 보이도록, 갈림 연출과 함께 이동할 때 사용하는 최소 이동 시간입니다.")]
    [SerializeField] private float grindTimelineSlideDuration = 0.32f;
    [SerializeField] private bool useUnscaledTimeForTimelineSlotSlide = false;

    [Header("Timeline Sprite Grind Animation")]
    [SerializeField] private BattleTimelineSpriteAnimationController timelineSpriteAnimationController;
    [SerializeField] private bool autoFindTimelineSpriteAnimationController = true;
    [Tooltip("각 슬롯이 시작될 때 TurnMark가 갈리면서 TimelineBar 전체가 왼쪽으로 이동하는 거리입니다.")]
    [SerializeField] private float slotStartTimelineLineSlideAmountX = -50f;
    [Tooltip("해당 슬롯의 첫 번째 Use_skill이 갈릴 때 전체 타임라인 라인이 왼쪽으로 이동하는 거리입니다.")]
    [SerializeField] private float firstUseSkillTimelineLineSlideAmountX = -45f;
    [Tooltip("해당 슬롯의 두 번째 이후 Use_skill이 갈릴 때 전체 타임라인 라인이 왼쪽으로 이동하는 거리입니다.")]
    [SerializeField] private float additionalUseSkillTimelineLineSlideAmountX = -40f;

    [Header("End Button Hover Rotation")]
    [SerializeField] private bool playEndButtonHoverRotation = true;
    [SerializeField] private bool autoBindEndButtonHoverRotationTarget = true;
    [SerializeField] private RectTransform endButtonHoverRotationTarget;
    [SerializeField] private string endButtonHoverRotationTargetName = "EndButton";
    [SerializeField] private float endButtonHoverRotationOffsetZ = -45f;
    [SerializeField] private float endButtonHoverRotationDuration = 0.12f;
    [SerializeField] private bool useUnscaledTimeForEndButtonHoverRotation = true;
    [SerializeField] private bool keepHoverUntilMouseLeavesEndGearBounds = true;
    [SerializeField] private float endButtonHoverBoundsPadding = 8f;

    [Header("End Button Hover Linked Gears")]
    [SerializeField] private bool autoBindEndButtonHoverLinkedGears = true;
    [SerializeField] private RectTransform endButtonHoverSmallGearRotationTarget;
    [SerializeField] private string endButtonHoverSmallGearRotationTargetName = "EndButtonSmallGear";
    [SerializeField] private float endButtonHoverSmallGearRotationOffsetZ = 60f;
    [SerializeField] private RectTransform endButtonHoverLargeGearRotationTarget;
    [SerializeField] private string endButtonHoverLargeGearRotationTargetName = "EndButtonLargeGear";
    [SerializeField] private float endButtonHoverLargeGearRotationOffsetZ = -30f;

    [Header("End Button Hover SFX")]
    [SerializeField] private bool playEndButtonHoverSfx = true;
    [SerializeField] private SfxType endButtonHoverSfxType = SfxType.BattleEndButtonHover;
    [SerializeField, Range(0f, 1f)] private float endButtonHoverSfxVolume = 1f;

    [Header("Timeline Slot Slide SFX")]
    [SerializeField] private bool playTimelineSlotSlideSfx = true;
    [SerializeField] private SfxType timelineSlotSlideSfxType = SfxType.BattleTimelineSlotSlide;
    [SerializeField, Range(0f, 1f)] private float timelineSlotSlideSfxVolume = 1f;

    [Header("Total Used Cost Text")]
    [SerializeField] private TMP_Text totalUsedCostText;
    [SerializeField] private bool autoFindTotalUsedCostText = true;
    [SerializeField] private string totalUsedCostTextObjectName = "useCOST";
    [SerializeField] private string totalUsedCostFormat = "{0}";

    private int activeSlotIndex = -1;
    private CharacterRuntimeData selectedCharacter;
    private SkillMasterData selectedSkill;
    private int reservationVersion;
    private Coroutine selectedSlotEffectRoutine;
    private Coroutine timelineSlotSlideRoutine;
    private Coroutine timelineSlideGearRotationRoutine;
    private Coroutine endButtonHoverRotationRoutine;
    private bool isSlotSelectionLocked;
    private bool isEndButtonHovering;
    private float endButtonRotationBeforeHoverZ;
    private float endButtonSmallGearRotationBeforeHoverZ;
    private float endButtonLargeGearRotationBeforeHoverZ;
    private Vector2 timelineBar1OriginalAnchoredPosition;
    private Vector2 timelineBar2OriginalAnchoredPosition;
    private bool timelineBarOriginalPositionCaptured;
    private float resolvedStandbyTimelineBarOffsetX = 1420f;
    private bool completedTimelineBarPositionApplied;
    private int timelineSlotSlideStepIndex;
    private int activeTimelineBarIndex;
    private string lastCameraFocusedCharacterId;

    private readonly List<MonsterReservedCommand>[] monsterCommandsBySlot =
        new List<MonsterReservedCommand>[5];

    public int SlotCount => reserveSlots != null ? reserveSlots.Length : 0;
    public int ActiveSlotIndex => activeSlotIndex;
    public int ReservationVersion => reservationVersion;
    public CharacterRuntimeData SelectedCharacter => selectedCharacter;


    private void OnValidate()
    {
        // Unity는 스크립트 기본값이 바뀌어도 이미 씬/프리팹에 저장된 Inspector 값을 유지합니다.
        // 이전 수정본에서 남은 1335 / -1440 값은 현재 구조의 기준값인 1420 / -1420으로 자동 보정합니다.
        if (Mathf.Approximately(standbyTimelineBarOffsetX, 1335f) || standbyTimelineBarOffsetX <= 0f)
            standbyTimelineBarOffsetX = 1420f;

        if (Mathf.Approximately(completedTurnTimelineBarPositionX, -1440f) || completedTurnTimelineBarPositionX >= 0f)
            completedTurnTimelineBarPositionX = -1420f;

        resolvedStandbyTimelineBarOffsetX = Mathf.Abs(standbyTimelineBarOffsetX);
    }

    private void Awake()
    {
        InitializeMonsterCommandSlots();
        AutoFindSelectedSlotValueTextIfNeeded();
        AutoFindSelectedSlotEffectIfNeeded();
        AutoFindSelectedSlotGearEffectsIfNeeded();
        AutoFindTimelineBarsIfNeeded();
        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();
        PrepareTimelineBarsForActiveTurn(false);
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        AutoBindEndButtonHoverRotationTargetIfNeeded();
        AutoBindEndButtonHoverLinkedGearTargetsIfNeeded();
        AutoFindTotalUsedCostTextIfNeeded();
        BindEndButtonHoverRotationEventsIfNeeded();

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        RefreshSelectedSlotValueText();
        RefreshTotalUsedCostText();

        InitTimelineBars();

        if (reserveSlots != null)
        {
            for (int i = 0; i < reserveSlots.Length; i++)
            {
                if (reserveSlots[i] != null)
                    reserveSlots[i].Init(this, i);
            }
        }

        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    private void Update()
    {
        HandleNumberKeySlotSelectionInput();
        HandleEndButtonHoverOutsidePolling();
    }

    private void HandleNumberKeySlotSelectionInput()
    {
        if (!enableNumberKeySlotSelection)
            return;

        if (!isActiveAndEnabled)
            return;

        if (IsTypingInputFieldSelected())
            return;

        int slotIndex = GetPressedNumberSlotIndex();

        if (slotIndex < 0)
            return;

        if (reserveSlots == null || slotIndex >= reserveSlots.Length || reserveSlots[slotIndex] == null)
            return;

        if (isSlotSelectionLocked)
            return;

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return;

        OnTimelineSlotClicked(slotIndex);
    }

    private int GetPressedNumberSlotIndex()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            return 0;

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            return 1;

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            return 2;

        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            return 3;

        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            return 4;

        return -1;
    }

    private bool IsTypingInputFieldSelected()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
            return false;

        if (selectedObject.GetComponent<TMPro.TMP_InputField>() != null)
            return true;

        if (selectedObject.GetComponent<InputField>() != null)
            return true;

        return false;
    }

    public void SelectCharacter(CharacterRuntimeData runtimeData)
    {
        selectedCharacter = runtimeData;
        ApplySelectedCharacterScaleFeedback(runtimeData);
        TryFocusCameraOnSelectedCharacter(runtimeData);
    }

    public void ClearCharacterSelectionFromSkillList(CharacterRuntimeData runtimeData)
    {
        if (runtimeData != null &&
            selectedCharacter != null &&
            selectedCharacter != runtimeData &&
            selectedCharacter.CharacterId != runtimeData.CharacterId)
        {
            return;
        }

        selectedCharacter = null;
        selectedSkill = null;
        lastCameraFocusedCharacterId = null;
        ApplySelectedCharacterScaleFeedbackById(null);

        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
            cameraController.StartReturnDefault();
    }

    private void ApplySelectedCharacterScaleFeedback(CharacterRuntimeData runtimeData)
    {
        string selectedCharacterId = runtimeData != null ? runtimeData.CharacterId : null;

        ApplySelectedCharacterScaleFeedbackById(selectedCharacterId);
    }

    public void SetSelectedCharacterScaleFeedbackActive(bool active)
    {
        if (!active || selectedCharacter == null)
        {
            ApplySelectedCharacterScaleFeedbackById(null);
            return;
        }

        ApplySelectedCharacterScaleFeedbackById(selectedCharacter.CharacterId);
    }

    private void ApplySelectedCharacterScaleFeedbackById(string selectedCharacterId)
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null)
                continue;

            bool selected = !string.IsNullOrWhiteSpace(selectedCharacterId) &&
                            character.CharacterId == selectedCharacterId;

            character.SetSelectionScaleFeedback(selected);
        }
    }

    private void TryFocusCameraOnSelectedCharacter(CharacterRuntimeData runtimeData)
    {
        TryFocusCameraOnSelectedCharacter(runtimeData, false);
    }

    public void RefocusCurrentSelectedCharacterWhenInputReady()
    {
        TryFocusCameraOnSelectedCharacter(selectedCharacter, true);
    }

    private void TryFocusCameraOnSelectedCharacter(CharacterRuntimeData runtimeData, bool forceRefocus)
    {
        if (!focusCameraOnCharacterSelect)
            return;

        if (runtimeData == null || string.IsNullOrWhiteSpace(runtimeData.CharacterId))
        {
            lastCameraFocusedCharacterId = null;
            return;
        }

        if (!forceRefocus && !refocusSameCharacter && lastCameraFocusedCharacterId == runtimeData.CharacterId)
            return;

        if (focusCameraOnlyWhenInputReady)
        {
            if (turnExecutor == null)
                turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

            if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
                return;
        }

        BattleCameraController cameraController = BattleCameraController.Instance;

        if (cameraController == null)
            return;

        Transform focusTarget = FindBattleCharacterTransform(runtimeData.CharacterId);

        if (focusTarget == null)
            return;

        cameraController.FocusOnCharacterSelection(focusTarget);
        lastCameraFocusedCharacterId = runtimeData.CharacterId;
    }

    private Transform FindBattleCharacterTransform(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null)
                continue;

            if (character.CharacterId != characterId)
                continue;

            return character.transform;
        }

        return null;
    }

    public void SelectSkill(SkillMasterData skillData)
    {
        selectedSkill = skillData;
        TryStartSkillReservation();
    }

    public void ClearMonsterCommands()
    {
        if (monsterCommandsBySlot == null)
            return;

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] != null)
                monsterCommandsBySlot[i].Clear();
        }

        RefreshTimeline();
    }

    public void OnTimelineSlotClicked(int slotIndex)
    {
        if (isSlotSelectionLocked)
        {
            if (showWarningWhenSlotSelectionLocked)
                ShowBattleWarning(slotSelectionLockedMessage);

            return;
        }

        int previousSlotIndex = activeSlotIndex;
        activeSlotIndex = slotIndex;

        SetActiveTimelineSlotVisual(activeSlotIndex);

        RefreshSelectedSlotValueText();
        PlaySelectedSlotEffect(previousSlotIndex, activeSlotIndex);
        TryStartSkillReservation();
    }

    public void ClearSelectedSlotSelection()
    {
        activeSlotIndex = -1;
        selectedSkill = null;

        SetActiveTimelineSlotVisual(activeSlotIndex);

        RefreshSelectedSlotValueText();
    }

    public void SetSlotSelectionLocked(bool locked)
    {
        isSlotSelectionLocked = locked;

        if (locked)
            CancelEndButtonHoverRotationIfNeeded();
    }

    public void SelectDefaultSlotWhenInputReady()
    {
        if (!autoSelectFirstSlotWhenInputReady)
            return;

        if (isSlotSelectionLocked)
            return;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return;

        int slotIndex = Mathf.Clamp(defaultSlotIndex, 0, reserveSlots.Length - 1);

        if (reserveSlots[slotIndex] == null)
            return;

        int previousSlotIndex = activeSlotIndex;
        activeSlotIndex = slotIndex;
        selectedSkill = null;

        SetActiveTimelineSlotVisual(activeSlotIndex);

        RefreshSelectedSlotValueText();
        PlaySelectedSlotEffect(previousSlotIndex, activeSlotIndex);
    }

    public IEnumerator SlideTimelineSlotsLeftOneStepRoutine()
    {
        yield return SlideTimelineSlotsLeftOneStepRoutine(timelineSlotSlideStepIndex);
    }

    public IEnumerator SlideTimelineSlotsLeftOneStepRoutine(int completedSlotIndex)
    {
        yield return SlideTimelineSlotsLeftThroughSlotRoutine(completedSlotIndex);
    }

    public IEnumerator SlideTimelineSlotsLeftThroughSlotRoutine(int lastSlotIndexInclusive)
    {
        if (!playTimelineSlotSlide)
            yield break;

        AutoFindTimelineBarsIfNeeded();
        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();
        AutoFindTimelineSpriteAnimationControllerIfNeeded();

        if (!HasTimelineSlotSlideTargets())
            yield break;

        BattleTimelineBarUI activeBarForSlide = GetActiveTimelineBarUI();
        BattleTimelineBarUI standbyBarForSlide = GetStandbyTimelineBarUI();

        if (activeBarForSlide != null)
            activeBarForSlide.SetEmptyUseSkillSlotsVisible(false);

        if (standbyBarForSlide != null && standbyBarForSlide != activeBarForSlide)
            standbyBarForSlide.SetEmptyUseSkillSlotsVisible(false);

        int slideSlotCount = GetTimelineSlideSlotCount();
        int startSlotIndex = Mathf.Clamp(timelineSlotSlideStepIndex, 0, slideSlotCount);
        int endSlotIndex = Mathf.Clamp(lastSlotIndexInclusive, -1, slideSlotCount - 1);

        if (endSlotIndex < startSlotIndex)
            yield break;

        for (int completedSlotIndex = startSlotIndex; completedSlotIndex <= endSlotIndex; completedSlotIndex++)
        {
            PlayTimelineSlotSlideSfx();

            bool isEmptySlot = IsTimelineSlotEmpty(completedSlotIndex);
            yield return PlayTimelineTurnMarkAnimationAndLineSlideRoutine(completedSlotIndex, isEmptySlot);
        }

        timelineSlotSlideStepIndex = Mathf.Clamp(endSlotIndex + 1, 0, slideSlotCount);

        // 5번 슬롯의 TurnMark가 갈렸다고 해서 턴 라인을 바로 완료 위치로 보내면,
        // 5번 슬롯에 등록된 Use_skill들이 개별적으로 갈리기 전에 한 번에 이동해 보입니다.
        // 완료 위치 보정은 BattleTurnExecutor가 모든 슬롯/스킬 처리를 끝낸 뒤 호출합니다.
    }

    public IEnumerator PlayTimelineTurnMarkAnimationRoutine(int slotIndex)
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        ConfigureTimelineSpriteAnimationRootForActiveBar();

        if (timelineSpriteAnimationController == null)
            yield break;

        // TurnMark 프레임만 재생합니다. 실제 라인 이동은 PlayTimelineTurnMarkAnimationAndLineSlideRoutine에서 함께 처리합니다.
        PlayTimelineSlideGearRotation(1);
        yield return timelineSpriteAnimationController.PlayTurnMarkRoutine(slotIndex);
    }

    private IEnumerator PlayTimelineTurnMarkAnimationAndLineSlideRoutine(int slotIndex, bool isEmptySlot)
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        ConfigureTimelineSpriteAnimationRootForActiveBar();

        // 턴 엔드 직후 1번 슬롯이 진행될 때만 라인을 먼저 -60 이동합니다.
        // 2번 슬롯부터는 이 선행 이동 없이 슬롯 시작 이동만 진행합니다.
        if (slotIndex == 0 && !Mathf.Approximately(firstSlotEndTurnTimelineLineSlideAmountX, 0f))
        {
            PlayTimelineSlideGearRotation(1);
            yield return MoveAllTimelineSlotSlideTargetsByOffsetRoutine(firstSlotEndTurnTimelineLineSlideAmountX);
        }

        // 슬롯 시작 시 TurnMark 애니메이션을 먼저 보여주고, 그 다음 TimelineBar 전체를 이동합니다.
        // 스킬이 없는 슬롯은 Use_skill 1~5칸까지 한 번에 이동해서 다음 슬롯 직전까지 보냅니다.
        float animationDuration = GetTurnMarkGrindDuration();

        if (timelineSpriteAnimationController != null)
            yield return timelineSpriteAnimationController.PlayTurnMarkRoutine(slotIndex);

        float lineSlideAmountX = isEmptySlot
            ? GetFullUseSkillTimelineLineSlideAmountX()
            : slotStartTimelineLineSlideAmountX;

        PlayTimelineSlideGearRotation(1, animationDuration);
        yield return MoveAllTimelineSlotSlideTargetsByOffsetRoutine(lineSlideAmountX, animationDuration);
    }

    public IEnumerator MoveTimelineBarsToCompletedTurnPositionRoutine()
    {
        if (completedTimelineBarPositionApplied)
            yield break;

        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();
        RectTransform standbyTarget = GetStandbyTimelineBarSlideTarget();

        if (activeTarget == null)
            yield break;

        Vector2 basePosition = GetTimelineBarBasePosition();
        float completedX = basePosition.x + completedTurnTimelineBarPositionX;
        float standbyX = basePosition.x;
        float offsetX = completedX - activeTarget.anchoredPosition.x;

        // 이미 완료 위치에 도착했거나 지나친 경우에는 추가 이동을 재생하지 않습니다.
        // active는 -1420, standby는 0으로 위치만 보정합니다.
        bool alreadyAtOrPastCompletedPosition = completedTurnTimelineBarPositionX < 0f
            ? activeTarget.anchoredPosition.x <= completedX + 0.01f
            : activeTarget.anchoredPosition.x >= completedX - 0.01f;

        if (!alreadyAtOrPastCompletedPosition && !Mathf.Approximately(offsetX, 0f))
            yield return MoveAllTimelineSlotSlideTargetsByOffsetRoutine(offsetX, timelineSlotSlideDuration, true);

        activeTarget.anchoredPosition = new Vector2(completedX, activeTarget.anchoredPosition.y);

        if (standbyTarget != null && standbyTarget != activeTarget)
            standbyTarget.anchoredPosition = new Vector2(standbyX, standbyTarget.anchoredPosition.y);

        completedTimelineBarPositionApplied = true;
    }

    private int GetTimelineSlideSlotCount()
    {
        if (reserveSlots != null && reserveSlots.Length > 0)
            return reserveSlots.Length;

        return 5;
    }

    private bool IsTimelineSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0)
            return true;

        return GetPlayerCommandCount(slotIndex) + GetMonsterCommandCount(slotIndex) <= 0;
    }

    private float GetFullUseSkillTimelineLineSlideAmountX()
    {
        return slotStartTimelineLineSlideAmountX +
               firstUseSkillTimelineLineSlideAmountX +
               additionalUseSkillTimelineLineSlideAmountX * 4f;
    }

    public IEnumerator PlayTimelineActionAnimationsRoutine(int slotIndex, int startOrderIndex, int count, bool fillRemainingUseSkillLine = false)
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        ConfigureTimelineSpriteAnimationRootForActiveBar();

        int safeStartOrderIndex = Mathf.Clamp(startOrderIndex, 0, 5);
        int safeCount = Mathf.Max(0, count);

        for (int i = 0; i < safeCount; i++)
        {
            int orderIndex = safeStartOrderIndex + i;

            if (orderIndex < 0 || orderIndex >= 5)
                yield break;

            // Use_skill 애니메이션을 먼저 보여주고, 그 다음 TimelineBar 전체를 이동합니다.
            float animationDuration = GetUseSkillGrindDuration();

            if (timelineSpriteAnimationController != null)
                yield return timelineSpriteAnimationController.PlayUseSkillRoutine(slotIndex, orderIndex);

            float lineSlideAmountX = orderIndex == 0
                ? firstUseSkillTimelineLineSlideAmountX
                : additionalUseSkillTimelineLineSlideAmountX;

            if (fillRemainingUseSkillLine && i == safeCount - 1)
                lineSlideAmountX += GetRemainingUseSkillTimelineLineSlideAmountX(orderIndex);

            PlayTimelineSlideGearRotation(1, animationDuration);
            yield return MoveAllTimelineSlotSlideTargetsByOffsetRoutine(lineSlideAmountX, animationDuration);
        }
    }


    private float GetTurnMarkGrindDuration()
    {
        float frameDuration = timelineSpriteAnimationController != null
            ? timelineSpriteAnimationController.GetTurnMarkAnimationDuration()
            : 0f;

        return GetGrindTimelineSlideDuration(frameDuration);
    }

    private float GetUseSkillGrindDuration()
    {
        float frameDuration = timelineSpriteAnimationController != null
            ? timelineSpriteAnimationController.GetUseSkillAnimationDuration()
            : 0f;

        return GetGrindTimelineSlideDuration(frameDuration);
    }

    private float GetGrindTimelineSlideDuration(float frameAnimationDuration)
    {
        return Mathf.Max(0.01f, timelineSlotSlideDuration, grindTimelineSlideDuration, frameAnimationDuration);
    }

    private float GetRemainingUseSkillTimelineLineSlideAmountX(int lastPlayedOrderIndex)
    {
        int remainingUseSkillCount = Mathf.Clamp(4 - lastPlayedOrderIndex, 0, 4);
        return additionalUseSkillTimelineLineSlideAmountX * remainingUseSkillCount;
    }


    public IEnumerator ResetTimelineSlotsToOriginalPositionRoutine()
    {
        AutoFindTimelineBarsIfNeeded();
        AutoBindTimelineSlotSlideTargetsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();

        if (!HasTimelineSlotSlideTargets())
            yield break;

        yield return MoveTimelineSlotSlideTargetsToOriginalOneByOneRoutine();
        timelineSlotSlideStepIndex = 0;

        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        if (timelineSpriteAnimationController != null)
        {
            RectTransform bar1 = timelineBarSlideTarget1;
            RectTransform bar2 = timelineBarSlideTarget2;

            if (bar1 != null)
            {
                timelineSpriteAnimationController.SetAnimationRoot(bar1);
                timelineSpriteAnimationController.ResetTurnMarksForNextTurn();
            }

            if (bar2 != null && bar2 != bar1)
            {
                timelineSpriteAnimationController.SetAnimationRoot(bar2);
                timelineSpriteAnimationController.ResetTurnMarksForNextTurn();
            }

            ConfigureTimelineSpriteAnimationRootForActiveBar();
        }
    }

    private void AutoBindEndButtonHoverRotationTargetIfNeeded()
    {
        if (!autoBindEndButtonHoverRotationTarget)
            return;

        if (endButtonHoverRotationTarget != null)
            return;

        endButtonHoverRotationTarget = FindRectTransformByName(transform, endButtonHoverRotationTargetName);

        if (endButtonHoverRotationTarget != null)
            return;

        Transform searchRoot = GetTimelineSearchRoot();
        endButtonHoverRotationTarget = FindRectTransformByName(searchRoot, endButtonHoverRotationTargetName);

        if (endButtonHoverRotationTarget != null)
            return;

        BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

        if (foundTimelineBar != null)
            endButtonHoverRotationTarget = FindRectTransformByName(foundTimelineBar.transform, endButtonHoverRotationTargetName);
    }

    private void AutoBindEndButtonHoverLinkedGearTargetsIfNeeded()
    {
        if (!autoBindEndButtonHoverLinkedGears)
            return;

        endButtonHoverSmallGearRotationTarget = AutoBindEndButtonHoverLinkedGearTargetIfNeeded(
            endButtonHoverSmallGearRotationTarget,
            endButtonHoverSmallGearRotationTargetName
        );

        endButtonHoverLargeGearRotationTarget = AutoBindEndButtonHoverLinkedGearTargetIfNeeded(
            endButtonHoverLargeGearRotationTarget,
            endButtonHoverLargeGearRotationTargetName
        );
    }

    private RectTransform AutoBindEndButtonHoverLinkedGearTargetIfNeeded(RectTransform currentTarget, string targetName)
    {
        if (currentTarget != null)
            return currentTarget;

        if (string.IsNullOrEmpty(targetName))
            return null;

        RectTransform found = FindRectTransformByName(transform, targetName);

        if (found != null)
            return found;

        Transform searchRoot = GetTimelineSearchRoot();
        found = FindRectTransformByName(searchRoot, targetName);

        if (found != null)
            return found;

        BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

        if (foundTimelineBar == null)
            return null;

        return FindRectTransformByName(foundTimelineBar.transform, targetName);
    }

    private RectTransform FindRectTransformByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;

        Transform found = FindChildRecursive(root, targetName);

        if (found == null)
            return null;

        return found.GetComponent<RectTransform>();
    }

    private void BindEndButtonHoverRotationEventsIfNeeded()
    {
        if (!playEndButtonHoverRotation)
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();
        AutoBindEndButtonHoverLinkedGearTargetsIfNeeded();

        if (endButtonHoverRotationTarget == null)
            return;

        EndButtonHoverRotationRelay relay = endButtonHoverRotationTarget.GetComponent<EndButtonHoverRotationRelay>();

        if (relay == null)
            relay = endButtonHoverRotationTarget.gameObject.AddComponent<EndButtonHoverRotationRelay>();

        relay.Initialize(this);
    }

    private bool IsEndButtonHoverRotationAllowed()
    {
        if (!playEndButtonHoverRotation)
            return false;

        if (!isActiveAndEnabled)
            return false;

        if (isSlotSelectionLocked)
            return false;

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return false;

        return true;
    }

    private void CancelEndButtonHoverRotationIfNeeded()
    {
        if (!isEndButtonHovering)
            return;

        isEndButtonHovering = false;

        if (endButtonHoverRotationRoutine != null)
        {
            StopCoroutine(endButtonHoverRotationRoutine);
            endButtonHoverRotationRoutine = null;
        }

        PlayEndButtonHoverRotationTo(
            endButtonRotationBeforeHoverZ,
            endButtonSmallGearRotationBeforeHoverZ,
            endButtonLargeGearRotationBeforeHoverZ
        );
    }

    private void OnEndButtonHoverEnter()
    {
        if (!IsEndButtonHoverRotationAllowed())
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();
        AutoBindEndButtonHoverLinkedGearTargetsIfNeeded();

        if (endButtonHoverRotationTarget == null)
            return;

        if (isEndButtonHovering)
            return;

        isEndButtonHovering = true;
        endButtonRotationBeforeHoverZ = GetTransformRotationZ(endButtonHoverRotationTarget);
        endButtonSmallGearRotationBeforeHoverZ = GetTransformRotationZ(endButtonHoverSmallGearRotationTarget);
        endButtonLargeGearRotationBeforeHoverZ = GetTransformRotationZ(endButtonHoverLargeGearRotationTarget);

        float targetRotationZ = endButtonRotationBeforeHoverZ + endButtonHoverRotationOffsetZ;
        float smallGearTargetRotationZ = endButtonSmallGearRotationBeforeHoverZ + endButtonHoverSmallGearRotationOffsetZ;
        float largeGearTargetRotationZ = endButtonLargeGearRotationBeforeHoverZ + endButtonHoverLargeGearRotationOffsetZ;

        PlayEndButtonHoverSfx();
        PlayEndButtonHoverRotationTo(targetRotationZ, smallGearTargetRotationZ, largeGearTargetRotationZ);
    }

    private void OnEndButtonHoverExit()
    {
        if (!playEndButtonHoverRotation)
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();
        AutoBindEndButtonHoverLinkedGearTargetsIfNeeded();

        if (endButtonHoverRotationTarget == null)
            return;

        if (!isEndButtonHovering)
            return;

        if (keepHoverUntilMouseLeavesEndGearBounds && IsPointerInsideEndButtonHoverBounds())
            return;

        EndEndButtonHoverRotation();
    }

    private void HandleEndButtonHoverOutsidePolling()
    {
        if (!isEndButtonHovering)
            return;

        if (!keepHoverUntilMouseLeavesEndGearBounds)
            return;

        if (IsPointerInsideEndButtonHoverBounds())
            return;

        EndEndButtonHoverRotation();
    }

    private void EndEndButtonHoverRotation()
    {
        if (!isEndButtonHovering)
            return;

        isEndButtonHovering = false;
        PlayEndButtonHoverRotationTo(
            endButtonRotationBeforeHoverZ,
            endButtonSmallGearRotationBeforeHoverZ,
            endButtonLargeGearRotationBeforeHoverZ
        );
    }

    private bool IsPointerInsideEndButtonHoverBounds()
    {
        Vector2 screenPosition = Input.mousePosition;

        return IsScreenPositionInsideRectTransformBounds(endButtonHoverRotationTarget, screenPosition) ||
               IsScreenPositionInsideRectTransformBounds(endButtonHoverSmallGearRotationTarget, screenPosition) ||
               IsScreenPositionInsideRectTransformBounds(endButtonHoverLargeGearRotationTarget, screenPosition);
    }

    private bool IsScreenPositionInsideRectTransformBounds(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (rectTransform == null)
            return false;

        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        Camera canvasCamera = GetCanvasCamera(rectTransform);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 cornerScreenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCorners[i]);
            minX = Mathf.Min(minX, cornerScreenPosition.x);
            minY = Mathf.Min(minY, cornerScreenPosition.y);
            maxX = Mathf.Max(maxX, cornerScreenPosition.x);
            maxY = Mathf.Max(maxY, cornerScreenPosition.y);
        }

        float padding = Mathf.Max(0f, endButtonHoverBoundsPadding);
        return screenPosition.x >= minX - padding &&
               screenPosition.x <= maxX + padding &&
               screenPosition.y >= minY - padding &&
               screenPosition.y <= maxY + padding;
    }

    private Camera GetCanvasCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void PlayEndButtonHoverRotationTo(float targetRotationZ, float smallGearTargetRotationZ, float largeGearTargetRotationZ)
    {
        if (endButtonHoverRotationTarget == null)
            return;

        if (endButtonHoverRotationRoutine != null)
            StopCoroutine(endButtonHoverRotationRoutine);

        endButtonHoverRotationRoutine = StartCoroutine(
            RotateEndButtonHoverToRoutine(targetRotationZ, smallGearTargetRotationZ, largeGearTargetRotationZ)
        );
    }

    private IEnumerator RotateEndButtonHoverToRoutine(float targetRotationZ, float smallGearTargetRotationZ, float largeGearTargetRotationZ)
    {
        float duration = Mathf.Max(0.01f, endButtonHoverRotationDuration);
        float elapsed = 0f;
        float startRotationZ = GetTransformRotationZ(endButtonHoverRotationTarget);
        float smallGearStartRotationZ = GetTransformRotationZ(endButtonHoverSmallGearRotationTarget);
        float largeGearStartRotationZ = GetTransformRotationZ(endButtonHoverLargeGearRotationTarget);

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForEndButtonHoverRotation ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            SetTransformRotationZ(endButtonHoverRotationTarget, Mathf.LerpAngle(startRotationZ, targetRotationZ, easedT));
            SetTransformRotationZ(endButtonHoverSmallGearRotationTarget, Mathf.LerpAngle(smallGearStartRotationZ, smallGearTargetRotationZ, easedT));
            SetTransformRotationZ(endButtonHoverLargeGearRotationTarget, Mathf.LerpAngle(largeGearStartRotationZ, largeGearTargetRotationZ, easedT));

            yield return null;
        }

        SetTransformRotationZ(endButtonHoverRotationTarget, targetRotationZ);
        SetTransformRotationZ(endButtonHoverSmallGearRotationTarget, smallGearTargetRotationZ);
        SetTransformRotationZ(endButtonHoverLargeGearRotationTarget, largeGearTargetRotationZ);
        endButtonHoverRotationRoutine = null;
    }

    private void PlayEndButtonHoverSfx()
    {
        if (!playEndButtonHoverSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(endButtonHoverSfxType, endButtonHoverSfxVolume);
    }

    private void PlayTimelineSlotSlideSfx()
    {
        if (!playTimelineSlotSlideSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(timelineSlotSlideSfxType, timelineSlotSlideSfxVolume);
    }

    private void PlayTimelineSlideGearRotation(int completedStepCount, float durationOverride = -1f)
    {
        if (completedStepCount <= 0)
            return;

        AutoFindSelectedSlotEffectIfNeeded();
        AutoFindSelectedSlotGearEffectsIfNeeded();
        AutoBindEndButtonHoverRotationTargetIfNeeded();
        AutoBindEndButtonHoverLinkedGearTargetsIfNeeded();

        bool hasTarget =
            selectedSlotEffect != null ||
            selectedSlotLargeGearEffect != null ||
            selectedSlotSmallGearEffect != null ||
            endButtonHoverRotationTarget != null ||
            endButtonHoverSmallGearRotationTarget != null ||
            endButtonHoverLargeGearRotationTarget != null;

        if (!hasTarget)
            return;

        if (selectedSlotEffect != null && !selectedSlotEffect.gameObject.activeSelf)
            selectedSlotEffect.gameObject.SetActive(true);

        if (selectedSlotLargeGearEffect != null && !selectedSlotLargeGearEffect.gameObject.activeSelf)
            selectedSlotLargeGearEffect.gameObject.SetActive(true);

        if (selectedSlotSmallGearEffect != null && !selectedSlotSmallGearEffect.gameObject.activeSelf)
            selectedSlotSmallGearEffect.gameObject.SetActive(true);

        float mainTargetZ = GetTransformRotationZ(selectedSlotEffect) + selectedSlotEffectRotateStepZ * completedStepCount;
        float largeGearTargetZ = GetTransformRotationZ(selectedSlotLargeGearEffect) + selectedSlotLargeGearRotateStepZ * completedStepCount;
        float smallGearTargetZ = GetTransformRotationZ(selectedSlotSmallGearEffect) + selectedSlotSmallGearRotateStepZ * completedStepCount;

        float endButtonTargetZ = GetTransformRotationZ(endButtonHoverRotationTarget) + endButtonHoverRotationOffsetZ * completedStepCount;
        float endButtonSmallGearTargetZ = GetTransformRotationZ(endButtonHoverSmallGearRotationTarget) + endButtonHoverSmallGearRotationOffsetZ * completedStepCount;
        float endButtonLargeGearTargetZ = GetTransformRotationZ(endButtonHoverLargeGearRotationTarget) + endButtonHoverLargeGearRotationOffsetZ * completedStepCount;

        if (isEndButtonHovering)
        {
            endButtonRotationBeforeHoverZ += endButtonHoverRotationOffsetZ * completedStepCount;
            endButtonSmallGearRotationBeforeHoverZ += endButtonHoverSmallGearRotationOffsetZ * completedStepCount;
            endButtonLargeGearRotationBeforeHoverZ += endButtonHoverLargeGearRotationOffsetZ * completedStepCount;
        }

        if (selectedSlotEffectRoutine != null)
        {
            StopCoroutine(selectedSlotEffectRoutine);
            selectedSlotEffectRoutine = null;
        }

        if (endButtonHoverRotationRoutine != null)
        {
            StopCoroutine(endButtonHoverRotationRoutine);
            endButtonHoverRotationRoutine = null;
        }

        if (timelineSlideGearRotationRoutine != null)
            StopCoroutine(timelineSlideGearRotationRoutine);

        timelineSlideGearRotationRoutine = StartCoroutine(PlayTimelineSlideGearRotationRoutine(
            mainTargetZ,
            largeGearTargetZ,
            smallGearTargetZ,
            endButtonTargetZ,
            endButtonSmallGearTargetZ,
            endButtonLargeGearTargetZ,
            durationOverride
        ));
    }

    private IEnumerator PlayTimelineSlideGearRotationRoutine(
        float mainTargetZ,
        float largeGearTargetZ,
        float smallGearTargetZ,
        float endButtonTargetZ,
        float endButtonSmallGearTargetZ,
        float endButtonLargeGearTargetZ,
        float durationOverride)
    {
        float duration = durationOverride > 0f
            ? Mathf.Max(0.01f, durationOverride)
            : Mathf.Max(0.01f, timelineSlotSlideDuration);
        float elapsed = 0f;

        float mainStartZ = GetTransformRotationZ(selectedSlotEffect);
        float largeGearStartZ = GetTransformRotationZ(selectedSlotLargeGearEffect);
        float smallGearStartZ = GetTransformRotationZ(selectedSlotSmallGearEffect);
        float endButtonStartZ = GetTransformRotationZ(endButtonHoverRotationTarget);
        float endButtonSmallGearStartZ = GetTransformRotationZ(endButtonHoverSmallGearRotationTarget);
        float endButtonLargeGearStartZ = GetTransformRotationZ(endButtonHoverLargeGearRotationTarget);

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForTimelineSlotSlide ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            SetTransformRotationZ(selectedSlotEffect, Mathf.LerpAngle(mainStartZ, mainTargetZ, easedT));
            SetTransformRotationZ(selectedSlotLargeGearEffect, Mathf.LerpAngle(largeGearStartZ, largeGearTargetZ, easedT));
            SetTransformRotationZ(selectedSlotSmallGearEffect, Mathf.LerpAngle(smallGearStartZ, smallGearTargetZ, easedT));
            SetTransformRotationZ(endButtonHoverRotationTarget, Mathf.LerpAngle(endButtonStartZ, endButtonTargetZ, easedT));
            SetTransformRotationZ(endButtonHoverSmallGearRotationTarget, Mathf.LerpAngle(endButtonSmallGearStartZ, endButtonSmallGearTargetZ, easedT));
            SetTransformRotationZ(endButtonHoverLargeGearRotationTarget, Mathf.LerpAngle(endButtonLargeGearStartZ, endButtonLargeGearTargetZ, easedT));

            yield return null;
        }

        SetTransformRotationZ(selectedSlotEffect, mainTargetZ);
        SetTransformRotationZ(selectedSlotLargeGearEffect, largeGearTargetZ);
        SetTransformRotationZ(selectedSlotSmallGearEffect, smallGearTargetZ);
        SetTransformRotationZ(endButtonHoverRotationTarget, endButtonTargetZ);
        SetTransformRotationZ(endButtonHoverSmallGearRotationTarget, endButtonSmallGearTargetZ);
        SetTransformRotationZ(endButtonHoverLargeGearRotationTarget, endButtonLargeGearTargetZ);
        timelineSlideGearRotationRoutine = null;
    }

    private float GetTransformRotationZ(Transform target)
    {
        if (target == null)
            return 0f;

        return NormalizeAngle(target.localEulerAngles.z);
    }

    private void SetTransformRotationZ(Transform target, float zRotation)
    {
        if (target == null)
            return;

        Vector3 eulerAngles = target.localEulerAngles;
        eulerAngles.z = zRotation;
        target.localEulerAngles = eulerAngles;
    }

    private sealed class EndButtonHoverRotationRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BattleTimelineController owner;

        public void Initialize(BattleTimelineController controller)
        {
            owner = controller;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.OnEndButtonHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.OnEndButtonHoverExit();
        }
    }

    private void AutoFindTotalUsedCostTextIfNeeded()
    {
        if (!autoFindTotalUsedCostText)
            return;

        if (totalUsedCostText != null)
            return;

        Transform searchRoot = GetTimelineSearchRoot();
        Transform found = FindChildRecursive(searchRoot, totalUsedCostTextObjectName);

        if (found == null)
        {
            BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

            if (foundTimelineBar != null)
                found = FindChildRecursive(foundTimelineBar.transform, totalUsedCostTextObjectName);
        }

        if (found == null)
            return;

        totalUsedCostText = found.GetComponent<TMP_Text>();
    }

    private void RefreshTotalUsedCostText()
    {
        AutoFindTotalUsedCostTextIfNeeded();

        if (totalUsedCostText == null)
            return;

        int totalUsedCost = CalculateTotalReservedCost();
        string format = string.IsNullOrEmpty(totalUsedCostFormat) ? "{0}" : totalUsedCostFormat;
        totalUsedCostText.text = string.Format(format, totalUsedCost);
    }

    private int CalculateTotalReservedCost()
    {
        if (reserveSlots == null || reserveSlots.Length <= 0)
            return 0;

        int totalCost = 0;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null)
                    continue;

                totalCost += Mathf.Max(0, command.Cost);
            }
        }

        return totalCost;
    }

    private void AutoFindSelectedSlotValueTextIfNeeded()
    {
        if (!autoFindSelectedSlotValueText)
            return;

        if (selectedSlotValueText != null)
            return;

        Transform searchRoot = GetTimelineSearchRoot();
        Transform found = FindChildRecursive(searchRoot, "Value_text");

        if (found == null)
        {
            BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

            if (foundTimelineBar != null)
                found = FindChildRecursive(foundTimelineBar.transform, "Value_text");
        }

        if (found == null)
            return;

        selectedSlotValueText = found.GetComponent<TMP_Text>();
    }

    private void AutoFindSelectedSlotEffectIfNeeded()
    {
        if (!autoFindSelectedSlotEffect)
            return;

        if (selectedSlotEffect != null)
            return;

        selectedSlotEffect = FindTimelineChildByName(selectedSlotEffectObjectName);
    }

    private void AutoFindSelectedSlotGearEffectsIfNeeded()
    {
        if (autoFindSelectedSlotLargeGearEffect && selectedSlotLargeGearEffect == null)
            selectedSlotLargeGearEffect = FindTimelineChildByName(selectedSlotLargeGearEffectObjectName);

        if (autoFindSelectedSlotSmallGearEffect && selectedSlotSmallGearEffect == null)
            selectedSlotSmallGearEffect = FindTimelineChildByName(selectedSlotSmallGearEffectObjectName);
    }


    private void AutoFindTimelineSpriteAnimationControllerIfNeeded()
    {
        if (!autoFindTimelineSpriteAnimationController)
            return;

        if (timelineSpriteAnimationController != null)
            return;

        BattleTimelineBarUI activeTimelineBarUI = GetActiveTimelineBarUI();

        if (activeTimelineBarUI != null)
        {
            timelineSpriteAnimationController = activeTimelineBarUI.GetComponentInParent<BattleTimelineSpriteAnimationController>(true);

            if (timelineSpriteAnimationController == null)
                timelineSpriteAnimationController = activeTimelineBarUI.GetComponentInChildren<BattleTimelineSpriteAnimationController>(true);
        }

        if (timelineSpriteAnimationController == null)
            timelineSpriteAnimationController = GetComponentInParent<BattleTimelineSpriteAnimationController>(true);

        if (timelineSpriteAnimationController == null)
            timelineSpriteAnimationController = GetComponentInChildren<BattleTimelineSpriteAnimationController>(true);

        if (timelineSpriteAnimationController == null)
            timelineSpriteAnimationController = FindFirstObjectByType<BattleTimelineSpriteAnimationController>(FindObjectsInactive.Include);
    }

    private Transform FindTimelineChildByName(string childName)
    {
        if (string.IsNullOrEmpty(childName))
            return null;

        Transform searchRoot = GetTimelineSearchRoot();
        Transform found = FindChildRecursive(searchRoot, childName);

        if (found != null)
            return found;

        BattleTimelineBarUI foundTimelineBar = FindFirstObjectByType<BattleTimelineBarUI>(FindObjectsInactive.Include);

        if (foundTimelineBar != null)
            found = FindChildRecursive(foundTimelineBar.transform, childName);

        return found;
    }

    private void AutoFindTimelineBarsIfNeeded()
    {
        if (timelineBarUI1 == null && timelineBarUI != null)
            timelineBarUI1 = timelineBarUI;

        if (timelineBarSlideTarget1 == null && timelineBarSlideTarget != null)
            timelineBarSlideTarget1 = timelineBarSlideTarget;

        Transform searchRoot = transform;

        if (timelineBarUI1 == null)
            timelineBarUI1 = FindTimelineBarUIByName(searchRoot, "TimelineBar1");

        if (timelineBarUI2 == null)
            timelineBarUI2 = FindTimelineBarUIByName(searchRoot, "TimelineBar2");

        if (timelineBarUI1 == null)
            timelineBarUI1 = FindTimelineBarUIByName(searchRoot, "TimelineBar");

        if (timelineBarSlideTarget1 == null && timelineBarUI1 != null)
            timelineBarSlideTarget1 = timelineBarUI1.GetComponent<RectTransform>();

        if (timelineBarSlideTarget2 == null && timelineBarUI2 != null)
            timelineBarSlideTarget2 = timelineBarUI2.GetComponent<RectTransform>();

        if (timelineBarUI == null)
            timelineBarUI = timelineBarUI1;
    }

    private BattleTimelineBarUI FindTimelineBarUIByName(Transform searchRoot, string objectName)
    {
        Transform found = FindChildRecursive(searchRoot, objectName);

        if (found == null)
            return null;

        BattleTimelineBarUI barUI = found.GetComponent<BattleTimelineBarUI>();

        if (barUI == null)
            barUI = found.gameObject.AddComponent<BattleTimelineBarUI>();

        return barUI;
    }

    private void InitTimelineBars()
    {
        AutoFindTimelineBarsIfNeeded();

        if (timelineBarUI1 != null)
            timelineBarUI1.Init(this);

        if (timelineBarUI2 != null && timelineBarUI2 != timelineBarUI1)
            timelineBarUI2.Init(this);

        SetActiveTimelineSlotVisual(activeSlotIndex);
    }

    private BattleTimelineBarUI GetActiveTimelineBarUI()
    {
        AutoFindTimelineBarsIfNeeded();

        if (activeTimelineBarIndex == 0)
            return timelineBarUI1 != null ? timelineBarUI1 : timelineBarUI2;

        return timelineBarUI2 != null ? timelineBarUI2 : timelineBarUI1;
    }

    private BattleTimelineBarUI GetStandbyTimelineBarUI()
    {
        AutoFindTimelineBarsIfNeeded();

        if (activeTimelineBarIndex == 0)
            return timelineBarUI2;

        return timelineBarUI1;
    }

    private RectTransform GetActiveTimelineBarSlideTarget()
    {
        AutoFindTimelineBarsIfNeeded();

        if (activeTimelineBarIndex == 0)
            return timelineBarSlideTarget1 != null ? timelineBarSlideTarget1 : timelineBarSlideTarget2;

        return timelineBarSlideTarget2 != null ? timelineBarSlideTarget2 : timelineBarSlideTarget1;
    }

    private RectTransform GetStandbyTimelineBarSlideTarget()
    {
        AutoFindTimelineBarsIfNeeded();

        if (activeTimelineBarIndex == 0)
            return timelineBarSlideTarget2;

        return timelineBarSlideTarget1;
    }

    private RectTransform[] GetTimelineBarSlideTargets()
    {
        AutoFindTimelineBarsIfNeeded();

        if (timelineBarSlideTarget1 != null && timelineBarSlideTarget2 != null)
            return new[] { timelineBarSlideTarget1, timelineBarSlideTarget2 };

        RectTransform singleTarget = GetActiveTimelineBarSlideTarget();
        return singleTarget != null ? new[] { singleTarget } : System.Array.Empty<RectTransform>();
    }

    private void SetActiveTimelineSlotVisual(int slotIndex)
    {
        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        BattleTimelineBarUI standbyBar = GetStandbyTimelineBarUI();

        if (activeBar != null)
        {
            activeBar.SetActiveTimelineSlot(slotIndex);
            activeBar.SetTurnMarkChildrenVisible(true);
        }

        if (standbyBar != null && standbyBar != activeBar)
        {
            standbyBar.SetActiveTimelineSlot(-1);
            standbyBar.SetTurnMarkChildrenVisible(false);
        }
    }

    private void AutoBindTimelineSlotSlideTargetsIfNeeded()
    {
        AutoFindTimelineBarsIfNeeded();
    }

    private void CaptureTimelineSlotOriginalPositionsIfNeeded()
    {
        AutoFindTimelineBarsIfNeeded();

        if (timelineBarOriginalPositionCaptured)
            return;

        if (timelineBarSlideTarget1 != null)
            timelineBar1OriginalAnchoredPosition = timelineBarSlideTarget1.anchoredPosition;

        if (timelineBarSlideTarget2 != null)
            timelineBar2OriginalAnchoredPosition = timelineBarSlideTarget2.anchoredPosition;
        else if (timelineBarSlideTarget1 != null)
            timelineBar2OriginalAnchoredPosition = timelineBar1OriginalAnchoredPosition + new Vector2(standbyTimelineBarOffsetX, 0f);

        // 두 TimelineBar의 실제 RectTransform width나 현재 배치값을 기준으로 간격을 다시 계산하지 않습니다.
        // Inspector에서 지정한 Standby Timeline Bar Offset X 값만 사용해야
        // 0 / 1420 위치를 번갈아 쓰는 구조가 흔들리지 않습니다.
        resolvedStandbyTimelineBarOffsetX = Mathf.Abs(standbyTimelineBarOffsetX);

        if (resolvedStandbyTimelineBarOffsetX <= 0.01f)
            resolvedStandbyTimelineBarOffsetX = 1420f;

        timelineBarOriginalPositionCaptured = true;
    }

    private Vector2 GetTimelineBarBasePosition()
    {
        if (timelineBarSlideTarget1 != null)
            return timelineBar1OriginalAnchoredPosition;

        return timelineBar2OriginalAnchoredPosition;
    }

    private void PrepareTimelineBarsForActiveTurn(bool swapActiveBar)
    {
        AutoFindTimelineBarsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();

        if (swapActiveBar && timelineBarSlideTarget1 != null && timelineBarSlideTarget2 != null)
            activeTimelineBarIndex = activeTimelineBarIndex == 0 ? 1 : 0;

        completedTimelineBarPositionApplied = false;

        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();
        RectTransform standbyTarget = GetStandbyTimelineBarSlideTarget();

        Vector2 activeBasePosition = GetTimelineBarBasePosition();

        if (activeTarget != null)
            activeTarget.anchoredPosition = activeBasePosition;

        if (standbyTarget != null && standbyTarget != activeTarget)
            standbyTarget.anchoredPosition = activeBasePosition + new Vector2(resolvedStandbyTimelineBarOffsetX, 0f);

        ConfigureTimelineSpriteAnimationRootForActiveBar();
        SetActiveTimelineSlotVisual(activeSlotIndex);

        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        BattleTimelineBarUI standbyBar = GetStandbyTimelineBarUI();

        if (activeBar != null)
            activeBar.SetEmptyUseSkillSlotsVisible(true);

        if (standbyBar != null && standbyBar != activeBar)
            standbyBar.SetEmptyUseSkillSlotsVisible(false);
    }

    private void ConfigureTimelineSpriteAnimationRootForActiveBar()
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();

        if (timelineSpriteAnimationController == null)
            return;

        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();
        timelineSpriteAnimationController.SetAnimationRoot(activeTarget != null ? activeTarget : null);
    }

    private bool HasTimelineSlotSlideTargets()
    {
        return GetTimelineBarSlideTargets().Length > 0;
    }

    private IEnumerator MoveAllTimelineSlotSlideTargetsByOffsetRoutine(float offsetX, float durationOverride = -1f, bool allowCompletedClamp = false)
    {
        RectTransform[] targets = GetTimelineBarSlideTargets();

        if (targets == null || targets.Length <= 0 || Mathf.Approximately(offsetX, 0f))
            yield break;

        // 두 개의 TimelineBar가 0 / 1420 위치를 번갈아 쓰는 구조에서는
        // 진행 중인 바가 완료 위치(-1420)에 도착한 뒤 추가 보정 이동이 들어가면 안 됩니다.
        // 그래서 모든 라인 이동 요청은 완료 위치를 넘지 않도록 항상 한 번 클램프합니다.
        float appliedOffsetX = ClampTimelineBarOffsetToCompletedPosition(offsetX);

        if (Mathf.Approximately(appliedOffsetX, 0f))
            yield break;

        Vector2[] startPositions = new Vector2[targets.Length];
        Vector2[] targetPositions = new Vector2[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            startPositions[i] = targets[i].anchoredPosition;
            targetPositions[i] = startPositions[i] + new Vector2(appliedOffsetX, 0f);
        }

        yield return MoveTimelineBarsToPositionsRoutine(targets, startPositions, targetPositions, durationOverride);
    }

    private float ClampTimelineBarOffsetToCompletedPosition(float offsetX)
    {
        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();

        if (activeTarget == null || Mathf.Approximately(offsetX, 0f))
            return offsetX;

        Vector2 basePosition = GetTimelineBarBasePosition();
        float completedX = basePosition.x + completedTurnTimelineBarPositionX;
        float currentX = activeTarget.anchoredPosition.x;
        float targetX = currentX + offsetX;

        if (completedTurnTimelineBarPositionX < 0f && targetX < completedX)
            return completedX - currentX;

        if (completedTurnTimelineBarPositionX > 0f && targetX > completedX)
            return completedX - currentX;

        return offsetX;
    }

    private IEnumerator MoveTimelineSlotSlideTargetsToOriginalOneByOneRoutine()
    {
        PrepareTimelineBarsForActiveTurn(true);
        yield break;
    }

    private IEnumerator MoveTimelineBarsToPositionsRoutine(
        RectTransform[] targets,
        Vector2[] startPositions,
        Vector2[] targetPositions,
        float durationOverride = -1f)
    {
        if (timelineSlotSlideRoutine != null)
            StopCoroutine(timelineSlotSlideRoutine);

        timelineSlotSlideRoutine = StartCoroutine(MoveTimelineBarsCoroutine(
            targets,
            startPositions,
            targetPositions,
            durationOverride));

        yield return timelineSlotSlideRoutine;
    }

    private IEnumerator MoveTimelineBarsCoroutine(
        RectTransform[] targets,
        Vector2[] startPositions,
        Vector2[] targetPositions,
        float durationOverride = -1f)
    {
        if (targets == null || startPositions == null || targetPositions == null)
            yield break;

        float duration = durationOverride > 0f
            ? Mathf.Max(0.01f, durationOverride)
            : Mathf.Max(0.01f, timelineSlotSlideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForTimelineSlotSlide ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || i >= startPositions.Length || i >= targetPositions.Length)
                    continue;

                targets[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], easedT);
            }

            yield return null;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null || i >= targetPositions.Length)
                continue;

            targets[i].anchoredPosition = targetPositions[i];
        }

        timelineSlotSlideRoutine = null;
    }

    private Transform GetTimelineSearchRoot()
    {
        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();

        if (activeBar != null)
            return activeBar.transform;

        return transform;
    }

    private void PlaySelectedSlotEffect(int previousSlotIndex, int currentSlotIndex)
    {
        AutoFindSelectedSlotEffectIfNeeded();
        AutoFindSelectedSlotGearEffectsIfNeeded();

        if (selectedSlotEffect == null &&
            selectedSlotLargeGearEffect == null &&
            selectedSlotSmallGearEffect == null)
        {
            return;
        }

        int rotateDirection = GetSelectedSlotEffectRotateDirection(previousSlotIndex, currentSlotIndex);

        if (rotateDirection == 0)
            return;

        if (selectedSlotEffect != null && !selectedSlotEffect.gameObject.activeSelf)
            selectedSlotEffect.gameObject.SetActive(true);

        if (selectedSlotLargeGearEffect != null && !selectedSlotLargeGearEffect.gameObject.activeSelf)
            selectedSlotLargeGearEffect.gameObject.SetActive(true);

        if (selectedSlotSmallGearEffect != null && !selectedSlotSmallGearEffect.gameObject.activeSelf)
            selectedSlotSmallGearEffect.gameObject.SetActive(true);

        PlaySelectedSlotEffectSfx();

        if (selectedSlotEffectRoutine != null)
            StopCoroutine(selectedSlotEffectRoutine);

        selectedSlotEffectRoutine = StartCoroutine(PlaySelectedSlotEffectRoutine(rotateDirection));
    }

    private void PlaySelectedSlotEffectSfx()
    {
        if (!playSelectedSlotEffectSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(selectedSlotEffectSfxType, selectedSlotEffectSfxVolume);
    }

    private int GetSelectedSlotEffectRotateDirection(int previousSlotIndex, int currentSlotIndex)
    {
        if (currentSlotIndex < 0)
            return 0;

        if (previousSlotIndex < 0)
            return 1;

        if (currentSlotIndex > previousSlotIndex)
            return 1;

        if (currentSlotIndex < previousSlotIndex)
            return -1;

        return 0;
    }

    private IEnumerator PlaySelectedSlotEffectRoutine(int rotateDirection)
    {
        float duration = Mathf.Max(0.01f, selectedSlotEffectDuration);
        float elapsed = 0f;

        float mainStartZ = GetTransformRotationZ(selectedSlotEffect);
        float mainTargetZ = mainStartZ + selectedSlotEffectRotateStepZ * rotateDirection;

        float largeGearStartZ = GetTransformRotationZ(selectedSlotLargeGearEffect);
        float largeGearTargetZ = largeGearStartZ + selectedSlotLargeGearRotateStepZ * rotateDirection;

        float smallGearStartZ = GetTransformRotationZ(selectedSlotSmallGearEffect);
        float smallGearTargetZ = smallGearStartZ + selectedSlotSmallGearRotateStepZ * rotateDirection;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForSelectedSlotEffect ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            SetTransformRotationZ(selectedSlotEffect, Mathf.LerpAngle(mainStartZ, mainTargetZ, easedT));
            SetTransformRotationZ(selectedSlotLargeGearEffect, Mathf.LerpAngle(largeGearStartZ, largeGearTargetZ, easedT));
            SetTransformRotationZ(selectedSlotSmallGearEffect, Mathf.LerpAngle(smallGearStartZ, smallGearTargetZ, easedT));
            yield return null;
        }

        SetTransformRotationZ(selectedSlotEffect, mainTargetZ);
        SetTransformRotationZ(selectedSlotLargeGearEffect, largeGearTargetZ);
        SetTransformRotationZ(selectedSlotSmallGearEffect, smallGearTargetZ);
        selectedSlotEffectRoutine = null;
    }

    private float GetSelectedSlotEffectRotationZ()
    {
        AutoFindSelectedSlotEffectIfNeeded();

        if (selectedSlotEffect == null)
            return 0f;

        return NormalizeAngle(selectedSlotEffect.localEulerAngles.z);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle <= -180f)
            angle += 360f;

        return angle;
    }

    private void SetSelectedSlotEffectRotation(float zRotation)
    {
        AutoFindSelectedSlotEffectIfNeeded();

        if (selectedSlotEffect == null)
            return;

        Vector3 eulerAngles = selectedSlotEffect.localEulerAngles;
        eulerAngles.z = zRotation;
        selectedSlotEffect.localEulerAngles = eulerAngles;
    }

    private void RefreshSelectedSlotValueText()
    {
        AutoFindSelectedSlotValueTextIfNeeded();

        if (selectedSlotValueText == null)
            return;

        if (activeSlotIndex < 0)
            selectedSlotValueText.text = emptySelectedSlotText;
        else
            selectedSlotValueText.text = (activeSlotIndex + 1).ToString();
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

    public void ShowSkillHoverRangePreview(CharacterRuntimeData runtimeData, SkillMasterData skillData)
    {
        if (runtimeData == null || skillData == null)
            return;

        if (activeSlotIndex < 0)
            return;

        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        if (playerSkillReservationController == null)
            return;

        playerSkillReservationController.ShowSkillHoverRangePreview(
            runtimeData,
            skillData,
            activeSlotIndex
        );
    }

    public void ClearSkillHoverRangePreview()
    {
        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        if (playerSkillReservationController != null)
            playerSkillReservationController.ClearSkillHoverRangePreview();
    }

    public void CancelSkillReservationPreviewFromSkillList(CharacterRuntimeData runtimeData)
    {
        if (runtimeData != null &&
            selectedCharacter != null &&
            selectedCharacter != runtimeData &&
            selectedCharacter.CharacterId != runtimeData.CharacterId)
        {
            return;
        }

        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        if (playerSkillReservationController != null)
            playerSkillReservationController.ClearPreview();

        selectedSkill = null;
    }

    private void TryStartSkillReservation()
    {
        if (activeSlotIndex < 0)
        {
            if (selectedSkill != null)
                ShowBattleWarning("타임라인 슬롯을 먼저 선택해주세요.");

            return;
        }

        if (selectedCharacter == null && selectedSkill == null)
        {
            ShowBattleWarning("캐릭터와 스킬을 먼저 선택해주세요.");
            return;
        }

        if (selectedCharacter == null)
        {
            ShowBattleWarning("캐릭터를 먼저 선택해주세요.");
            return;
        }

        if (selectedSkill == null)
            return;

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("타임라인 슬롯이 없습니다.");
            selectedSkill = null;
            return;
        }

        if (activeSlotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            selectedSkill = null;
            return;
        }

        ReserveTurnSlotUI slot = reserveSlots[activeSlotIndex];

        if (slot == null)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            selectedSkill = null;
            return;
        }

        PlayerReservedCommand costCheckCommand =
            new PlayerReservedCommand(selectedCharacter, selectedSkill);
        bool canMergeMoveCommand =
            CanMergeMoveCommandInSlot(activeSlotIndex, selectedCharacter, selectedSkill);

        if (!canMergeMoveCommand)
        {
            PrepareCommandForReservation(activeSlotIndex, costCheckCommand);

            string blockReason = GetReserveBlockReason(costCheckCommand);
            if (!string.IsNullOrEmpty(blockReason))
            {
                ShowBattleWarning(blockReason);
                selectedSkill = null;
                return;
            }
        }

        if (!slot.CanAcceptCharacter(selectedCharacter))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            selectedSkill = null;
            return;
        }

        if (!canMergeMoveCommand && !CanAddPlayerCommandToSlot(activeSlotIndex))
        {
            ShowCombinedSlotCapacityWarning();
            selectedSkill = null;
            return;
        }

        int casterGridIndex = GetPreviewGridIndexAtSlotEnd(selectedCharacter, activeSlotIndex);

        if (casterGridIndex < 0)
        {
            ShowBattleWarning("캐릭터 위치를 찾을 수 없습니다.");
            Debug.LogWarning($"[BattleTimelineController] 캐릭터 위치를 찾을 수 없습니다: {selectedCharacter.CharacterId}");
            selectedSkill = null;
            return;
        }

        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        if (playerSkillReservationController == null)
        {
            ShowBattleWarning("스킬 예약 컨트롤러를 찾을 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] PlayerSkillReservationController가 없습니다.");
            selectedSkill = null;
            return;
        }

        BattleDirection casterDirection = GetPreviewDirection(selectedCharacter, activeSlotIndex);

        playerSkillReservationController.StartReservation(
            selectedCharacter,
            selectedSkill,
            casterGridIndex,
            activeSlotIndex,
            casterDirection,
            GetSelectedCharacterSprite()
        );
    }

    public bool ConfirmPlayerCommand(int slotIndex, PlayerReservedCommand command)
    {
        if (command == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("타임라인 슬롯이 없습니다.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            return false;
        }

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
        {
            ShowBattleWarning("선택한 타임라인 슬롯을 사용할 수 없습니다.");
            return false;
        }

        if (!slot.CanAcceptCharacter(command.UserRuntime))
        {
            ShowBattleWarning("이 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            Debug.LogWarning("[BattleTimelineController] 이 타임라인 슬롯에는 이미 다른 캐릭터의 행동이 예약되어 있습니다.");
            return false;
        }

        if (TryHandleMoveCommandMerge(slot, command, out bool mergeSucceeded))
        {
            if (mergeSucceeded)
                selectedSkill = null;

            return mergeSucceeded;
        }

        PrepareCommandForReservation(slotIndex, command);

        string blockReason = GetReserveBlockReason(command);
        if (!string.IsNullOrEmpty(blockReason))
        {
            ShowBattleWarning(blockReason);
            return false;
        }

        if (!CanAddPlayerCommandToSlot(slotIndex))
        {
            ShowCombinedSlotCapacityWarning();
            return false;
        }

        bool added = slot.AddCommand(command);

        if (!added)
        {
            ShowBattleWarning("스킬을 예약할 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] 예약 슬롯이 가득 찼습니다.");
            return false;
        }

        RecalculateAllReservedCosts();

        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();
        selectedSkill = null;

        return true;
    }

    private bool TryHandleMoveCommandMerge(
        ReserveTurnSlotUI slot,
        PlayerReservedCommand command,
        out bool mergeSucceeded)
    {
        mergeSucceeded = false;

        if (!IsMoveCommand(command) || slot == null || slot.Commands == null)
            return false;

        PlayerReservedCommand existingMoveCommand =
            FindMoveCommandInSlot(slot, command.CharacterId);

        if (existingMoveCommand == null)
            return false;

        if (!IsSelfFlipMoveCommand(command))
            return false;

        if (!CanReserveMergedMoveCommand(existingMoveCommand, command, out string blockReason))
        {
            ShowBattleWarning(blockReason);
            return true;
        }

        existingMoveCommand.MergeMoveReservation(command);

        RecalculateAllReservedCosts();
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        mergeSucceeded = true;
        return true;
    }

    private bool IsSelfFlipMoveCommand(PlayerReservedCommand command)
    {
        return IsMoveCommand(command) &&
               command.ReservedMoveGridIndex >= 0 &&
               command.MoveOffset == Vector2Int.zero;
    }

    private PlayerReservedCommand FindMoveCommandInSlot(
        ReserveTurnSlotUI slot,
        string characterId)
    {
        if (slot == null || slot.Commands == null || string.IsNullOrWhiteSpace(characterId))
            return null;

        for (int i = slot.Commands.Count - 1; i >= 0; i--)
        {
            PlayerReservedCommand command = slot.Commands[i];

            if (!IsMoveCommand(command))
                continue;

            if (command.CharacterId == characterId)
                return command;
        }

        return null;
    }

    private bool CanMergeMoveCommandInSlot(
        int slotIndex,
        CharacterRuntimeData runtime,
        SkillMasterData skillData)
    {
        if (runtime == null || skillData == null || reserveSlots == null)
            return false;

        if (skillData.Category != Category.Move)
            return false;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        return FindMoveCommandInSlot(slot, runtime.CharacterId) != null;
    }

    private bool CanReserveMergedMoveCommand(
        PlayerReservedCommand existingCommand,
        PlayerReservedCommand nextCommand,
        out string blockReason)
    {
        blockReason = string.Empty;

        if (existingCommand == null || nextCommand == null || nextCommand.UserRuntime == null)
            return false;

        CharacterRuntimeData runtime = nextCommand.UserRuntime;
        int mergedMoveDistance =
            Mathf.Max(0, existingCommand.PlannedMoveDistance) +
            Mathf.Max(0, nextCommand.PlannedMoveDistance);
        int moveDistancePerCost = Mathf.Max(
            1,
            nextCommand.MoveDistancePerCost > 0
                ? nextCommand.MoveDistancePerCost
                : existingCommand.MoveDistancePerCost);
        int mergedCost = PlayerReservedCommand.CalculateMoveCost(
            mergedMoveDistance,
            moveDistancePerCost);
        int additionalCost = Mathf.Max(0, mergedCost - Mathf.Max(0, existingCommand.Cost));

        if (runtime.CanReserveCost(additionalCost))
            return true;

        blockReason = BuildShortageMessage(
            "Cost",
            additionalCost,
            runtime.CurrentCost - runtime.ReservedCost);
        return false;
    }

    public bool ConfirmPlayerCommands(
        int slotIndex,
        IReadOnlyList<PlayerReservedCommand> commands)
    {
        if (commands == null || commands.Count <= 0)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        int addedCount = 0;

        for (int i = 0; i < commands.Count; i++)
        {
            if (ConfirmPlayerCommand(slotIndex, commands[i]))
            {
                addedCount++;
                continue;
            }

            RollbackLastPlayerCommands(slotIndex, addedCount);
            return false;
        }

        return true;
    }

    private void RollbackLastPlayerCommands(int slotIndex, int rollbackCount)
    {
        if (rollbackCount <= 0 || reserveSlots == null)
            return;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return;

        for (int i = 0; i < rollbackCount; i++)
        {
            int removeIndex = slot.Commands != null
                ? slot.Commands.Count - 1
                : -1;

            if (removeIndex < 0)
                break;

            if (slot.RemoveCommandAt(removeIndex, out PlayerReservedCommand removedCommand))
                RemoveReservedCosts(removedCommand);
        }

        reservationVersion++;
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();
    }

    public int GetRemainingPlayerCommandCapacity(int slotIndex)
    {
        if (reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return 0;

        if (reserveSlots[slotIndex] == null)
            return 0;

        return GetRemainingCombinedCommandCapacity(slotIndex);
    }

    private bool CanAddPlayerCommandToSlot(int slotIndex)
    {
        if (reserveSlots == null)
            return false;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return false;

        return slot.CanAddCommand() && GetRemainingCombinedCommandCapacity(slotIndex) > 0;
    }

    private int GetRemainingCombinedCommandCapacity(int slotIndex)
    {
        if (slotIndex < 0)
            return 0;

        int playerCommandCount = GetPlayerCommandCount(slotIndex);
        int monsterCommandCount = GetMonsterCommandCount(slotIndex);

        return Mathf.Max(
            0,
            ReserveTurnSlotUI.MaxCommandCount - playerCommandCount - monsterCommandCount);
    }

    private int GetPlayerCommandCount(int slotIndex)
    {
        if (reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return 0;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        return slot != null ? slot.CommandCount : 0;
    }

    private int GetMonsterCommandCount(int slotIndex)
    {
        InitializeMonsterCommandSlots();

        if (slotIndex < 0 || slotIndex >= monsterCommandsBySlot.Length)
            return 0;

        List<MonsterReservedCommand> commands = monsterCommandsBySlot[slotIndex];

        return commands != null ? commands.Count : 0;
    }

    private void ShowCombinedSlotCapacityWarning()
    {
        ShowBattleWarning("한 슬롯에는 몬스터 행동과 캐릭터 행동을 합쳐 최대 5개만 예약할 수 있습니다.");
    }

    public int GetPreviewReservationCostValue(
        CharacterRuntimeData runtimeData,
        SkillMasterData skillData)
    {
        if (skillData == null)
            return 0;

        PlayerReservedCommand command = new(runtimeData, skillData);

        if (HasActiveReservationSlot())
            PrepareCommandForReservation(activeSlotIndex, command);

        return GetReservationCostValue(command, skillData.ReferenceResource);
    }

    private bool HasActiveReservationSlot()
    {
        return reserveSlots != null &&
               activeSlotIndex >= 0 &&
               activeSlotIndex < reserveSlots.Length;
    }

    private int GetReservationCostValue(
        PlayerReservedCommand command,
        ReferenceResource resource)
    {
        if (command == null)
            return 0;

        switch (resource)
        {
            case ReferenceResource.HP:
                return command.HPCost;

            case ReferenceResource.Cost:
            case ReferenceResource.MovePoint:
                return command.Cost;

            case ReferenceResource.UniqueResource:
                return command.ResourceCost;

            default:
                return 0;
        }
    }

    public IReadOnlyList<PlayerReservedCommand> GetPlayerCommands(int slotIndex)
    {
        if (reserveSlots == null)
            return null;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return null;

        if (reserveSlots[slotIndex] == null)
            return null;

        return reserveSlots[slotIndex].Commands;
    }

    public IReadOnlyList<MonsterReservedCommand> GetMonsterCommands(int slotIndex)
    {
        InitializeMonsterCommandSlots();

        if (slotIndex < 0 || slotIndex >= monsterCommandsBySlot.Length)
            return null;

        return monsterCommandsBySlot[slotIndex];
    }

    public void PreparePreviewCommandForReservation(int slotIndex, PlayerReservedCommand command)
    {
        PrepareCommandForReservation(slotIndex, command);
    }

    public int GetPreviewGridIndexAtSlotEnd(CharacterRuntimeData runtimeData, int targetSlotIndex)
    {
        if (runtimeData == null)
            return -1;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return GetPreviewGridIndexBeforeCommand(runtimeData, targetSlotIndex, int.MaxValue);

        int safeSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);

        return GetPreviewGridIndexBeforeCommand(runtimeData, safeSlotIndex, int.MaxValue);
    }

    private void PrepareCommandForReservation(int slotIndex, PlayerReservedCommand command)
    {
        if (command == null)
            return;

        bool isFirstMoveCommand =
            BattleEquipmentEffectService.IsMoveCommand(command) &&
            !HasEarlierMoveCommand(command.UserRuntime, slotIndex);

        bool isLastTimelineSlot =
            reserveSlots != null &&
            slotIndex == reserveSlots.Length - 1;

        int duplicateSkillReservationCountInSlot =
            CountEarlierSameSkillReservationsInSlot(command, slotIndex);

        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            command,
            slotIndex,
            isFirstMoveCommand,
            isLastTimelineSlot,
            duplicateSkillReservationCountInSlot);
    }

    private bool HasEarlierMoveCommand(CharacterRuntimeData runtime, int targetSlotIndex)
    {
        if (runtime == null || reserveSlots == null || reserveSlots.Length <= 0)
            return false;

        int safeTargetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);

        for (int slotIndex = 0; slotIndex <= safeTargetSlotIndex; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtime.CharacterId)
                    continue;

                if (BattleEquipmentEffectService.IsMoveCommand(command))
                    return true;
            }
        }

        return false;
    }

    private int CountEarlierSameSkillReservationsInSlot(
        PlayerReservedCommand targetCommand,
        int targetSlotIndex)
    {
        if (targetCommand == null || reserveSlots == null || reserveSlots.Length <= 0)
            return 0;

        string targetKey = GetDuplicateSkillReservationKey(targetCommand);
        if (string.IsNullOrEmpty(targetKey))
            return 0;

        int safeTargetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);
        ReserveTurnSlotUI slot = reserveSlots[safeTargetSlotIndex];

        if (slot == null || slot.Commands == null)
            return 0;

        int count = 0;

        for (int i = 0; i < slot.Commands.Count; i++)
        {
            PlayerReservedCommand command = slot.Commands[i];

            if (command == null)
                continue;

            if (GetDuplicateSkillReservationKey(command) == targetKey)
                count++;
        }

        return count;
    }

    private void RecalculateAllReservedCosts()
    {
        reservationVersion++;

        List<CharacterRuntimeData> runtimes = CollectReservedRuntimes();

        for (int i = 0; i < runtimes.Count; i++)
            runtimes[i].ClearReservedCosts();

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return;

        HashSet<string> firstMoveAppliedCharacterIds = new();

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            bool isLastTimelineSlot = slotIndex == reserveSlots.Length - 1;
            Dictionary<string, int> duplicateSkillReservationCounts = new();

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                bool isMoveContinuationCommand = command.IsMoveContinuationCommand;
                bool isMoveCommand = BattleEquipmentEffectService.IsMoveCommand(command);
                bool isFirstMoveCommand =
                    isMoveCommand &&
                    !isMoveContinuationCommand &&
                    !firstMoveAppliedCharacterIds.Contains(command.CharacterId);
                int duplicateSkillReservationCountInSlot =
                    GetAndIncrementDuplicateSkillReservationCount(
                        duplicateSkillReservationCounts,
                        command);

                BattleEquipmentEffectService.ApplyReservationCostModifiers(
                    command,
                    slotIndex,
                    isFirstMoveCommand,
                    isLastTimelineSlot,
                    duplicateSkillReservationCountInSlot);

                AddReservedCosts(command);

                if (isMoveCommand &&
                    !isMoveContinuationCommand &&
                    !firstMoveAppliedCharacterIds.Contains(command.CharacterId))
                {
                    firstMoveAppliedCharacterIds.Add(command.CharacterId);
                }
            }
        }
    }

    private int GetAndIncrementDuplicateSkillReservationCount(
        Dictionary<string, int> duplicateSkillReservationCounts,
        PlayerReservedCommand command)
    {
        if (duplicateSkillReservationCounts == null || command == null)
            return 0;

        string key = GetDuplicateSkillReservationKey(command);
        if (string.IsNullOrEmpty(key))
            return 0;

        duplicateSkillReservationCounts.TryGetValue(key, out int currentCount);
        duplicateSkillReservationCounts[key] = currentCount + 1;

        return currentCount;
    }

    private string GetDuplicateSkillReservationKey(PlayerReservedCommand command)
    {
        if (command == null ||
            command.IsMoveContinuationCommand ||
            string.IsNullOrEmpty(command.CharacterId) ||
            string.IsNullOrEmpty(command.SkillId))
        {
            return string.Empty;
        }

        return $"{command.CharacterId}:{command.SkillId}";
    }

    private List<CharacterRuntimeData> CollectReservedRuntimes()
    {
        List<CharacterRuntimeData> runtimes = new();

        if (reserveSlots == null)
            return runtimes;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
                AddRuntimeIfNeeded(runtimes, slot.Commands[i] != null ? slot.Commands[i].UserRuntime : null);
        }

        return runtimes;
    }

    private void AddRuntimeIfNeeded(List<CharacterRuntimeData> runtimes, CharacterRuntimeData runtime)
    {
        if (runtimes == null || runtime == null)
            return;

        for (int i = 0; i < runtimes.Count; i++)
        {
            if (runtimes[i] != null && runtimes[i].CharacterId == runtime.CharacterId)
                return;
        }

        runtimes.Add(runtime);
    }

    private void AddReservedCosts(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null)
            return;

        command.UserRuntime.AddReservedHP(command.HPCost);
        command.UserRuntime.AddReservedCost(command.Cost);
        command.UserRuntime.AddReservedResource(command.ResourceCost);
        command.UserRuntime.AddReservedShield(command.ShieldCost);
    }

    private bool CanReserveCommand(PlayerReservedCommand command)
    {
        return string.IsNullOrEmpty(GetReserveBlockReason(command));
    }

    private string GetReserveBlockReason(PlayerReservedCommand command)
    {
        if (command == null)
            return "예약할 스킬 정보가 없습니다.";

        if (command.UserRuntime == null)
            return "선택된 캐릭터가 없습니다.";

        CharacterRuntimeData runtime = command.UserRuntime;

        if (command.SkillData != null &&
            command.SkillData.ResourceCostType == ResourceCostType.AllCurrent)
        {
            int minRequired = BattleEquipmentEffectService.GetAllCurrentMinimumCost(
                runtime,
                command.SkillData,
                command.SkillData.ResourceCostValue);

            if (command.ResourceCost < minRequired)
            {
                Debug.LogWarning(
                    $"[BattleTimelineController] AllCurrent 자원 부족 / " +
                    $"Character:{runtime.CharacterId} / " +
                    $"Skill:{command.SkillId} / " +
                    $"Cost:{command.ResourceCost} / " +
                    $"MinRequired:{minRequired}"
                );

                return $"{GetCostLabel(command.SkillData.ReferenceResource)}이 부족합니다. 필요:{minRequired} / 보유:{command.ResourceCost}";
            }
        }

        string shortageMessage = GetShortageMessage(runtime, command);
        if (!string.IsNullOrEmpty(shortageMessage))
            return shortageMessage;

        return string.Empty;
    }

    private string GetShortageMessage(CharacterRuntimeData runtime, PlayerReservedCommand command)
    {
        if (runtime == null || command == null)
            return "예약할 스킬 정보가 없습니다.";

        if (!runtime.CanReserveHP(command.HPCost))
            return BuildShortageMessage("HP", command.HPCost, runtime.CurrentHP - runtime.ReservedHPCost);

        if (!runtime.CanReserveCost(command.Cost))
            return BuildShortageMessage("Cost", command.Cost, runtime.CurrentCost - runtime.ReservedCost);

        if (!runtime.CanReserveResource(command.ResourceCost))
            return BuildShortageMessage("고유자원", command.ResourceCost, runtime.CurrentResource - runtime.ReservedResourceCost);

        if (!runtime.CanReserveShield(command.ShieldCost))
            return BuildShortageMessage("방어도", command.ShieldCost, runtime.CurrentShield - runtime.ReservedShieldCost);

        return string.Empty;
    }

    private string BuildShortageMessage(string label, int required, int available)
    {
        int safeAvailable = Mathf.Max(0, available);
        return $"{label}이 부족합니다. 필요:{required} / 보유:{safeAvailable}";
    }

    private string GetCostLabel(ReferenceResource resource)
    {
        switch (resource)
        {
            case ReferenceResource.HP:
                return "HP";

            case ReferenceResource.Cost:
            case ReferenceResource.MovePoint:
                return "Cost";

            case ReferenceResource.UniqueResource:
                return "고유자원";

            default:
                return "자원";
        }
    }

    public int GetPreviewGridIndex(CharacterRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return -1;

        int gridIndex = GetCurrentBattleCharacterGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            gridIndex = GetRuntimeStartGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            return -1;

        if (reserveSlots == null)
            return gridIndex;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtimeData.CharacterId)
                    continue;

                if (command.ReservedMoveGridIndex >= 0)
                    gridIndex = command.EffectiveMoveGridIndex;
            }
        }

        return gridIndex;
    }

    public bool TryGetLastMoveGhostPreviewResult(
        CharacterRuntimeData runtimeData,
        out int gridIndex,
        out BattleDirection direction)
    {
        gridIndex = -1;
        direction = runtimeData != null
            ? runtimeData.Direction
            : BattleDirection.Right;

        if (runtimeData == null || reserveSlots == null)
            return false;

        PlayerReservedCommand lastMoveCommand = null;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (!IsGhostMoveCommandForRuntime(command, runtimeData.CharacterId))
                    continue;

                lastMoveCommand = command;
            }
        }

        if (lastMoveCommand == null)
            return false;

        gridIndex = lastMoveCommand.PreviewMoveGridIndex;
        direction = lastMoveCommand.PreviewMoveDirection;
        return gridIndex >= 0;
    }

    public BattleDirection GetPreviewDirection(CharacterRuntimeData runtimeData, int targetSlotIndex)
    {
        if (runtimeData == null)
            return BattleDirection.Right;

        BattleDirection direction = runtimeData.Direction;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return direction;

        if (targetSlotIndex < 0)
            return direction;

        int lastSlotIndex = Mathf.Clamp(targetSlotIndex, 0, reserveSlots.Length - 1);

        for (int slotIndex = 0; slotIndex <= lastSlotIndex; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtimeData.CharacterId)
                    continue;

                direction = GetDirectionAfterCommand(direction, command);
            }
        }

        return direction;
    }

    private BattleDirection GetDirectionAfterCommand(
        BattleDirection currentDirection,
        PlayerReservedCommand command)
    {
        if (command == null)
            return currentDirection;

        if (command.ReservedMoveGridIndex < 0)
            return currentDirection;

        return command.Direction;
    }

    private int GetCurrentBattleCharacterGridIndex(string characterId)
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            if (characters[i].CharacterId != characterId)
                continue;

            return characters[i].CurrentGridIndex;
        }

        return -1;
    }

    private int GetRuntimeStartGridIndex(string characterId)
    {
        if (DataManager.Instance == null)
            return -1;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int slotIndex = 0; slotIndex < partyStore.MaxPartyCountValue; slotIndex++)
        {
            if (partyStore.GetCharacterId(slotIndex) == characterId)
                return partyStore.GetSpawnGridIndex(slotIndex);
        }

        return -1;
    }

    private Sprite GetSelectedCharacterSprite()
    {
        BattleCharacter[] battleCharacters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < battleCharacters.Length; i++)
        {
            BattleCharacter battleCharacter = battleCharacters[i];

            if (battleCharacter == null || battleCharacter.RuntimeData == null)
                continue;

            if (battleCharacter.RuntimeData.CharacterId != selectedCharacter.CharacterId)
                continue;

            SpriteRenderer spriteRenderer = battleCharacter.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                return spriteRenderer.sprite;
        }

        return null;
    }

    private void InitializeMonsterCommandSlots()
    {
        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] == null)
                monsterCommandsBySlot[i] = new List<MonsterReservedCommand>();
        }
    }

    public void AddMonsterCommand(int slotIndex, MonsterReservedCommand command)
    {
        InitializeMonsterCommandSlots();

        if (command == null)
            return;

        int resolvedSlotIndex = ResolveMonsterSlotIndex(slotIndex, command);

        if (resolvedSlotIndex < 0)
        {
            Debug.LogWarning(
                $"[BattleTimelineController] 몬스터 행동을 넣을 슬롯이 없습니다. " +
                $"Monster:{command.RuntimeId} / Skill:{command.SkillId}"
            );
            return;
        }

        monsterCommandsBySlot[resolvedSlotIndex].Add(command);
        reservationVersion++;

        RefreshTimeline();
    }

    private int ResolveMonsterSlotIndex(int preferredSlotIndex, MonsterReservedCommand command)
    {
        if (command == null)
            return -1;

        int monsterSlotCount = GetMonsterReservationSlotCount();

        if (monsterSlotCount <= 0)
            return -1;

        if (preferredSlotIndex < 0)
            preferredSlotIndex = 0;

        if (preferredSlotIndex >= monsterSlotCount)
            preferredSlotIndex = monsterSlotCount - 1;

        if (CanMonsterUseSlot(preferredSlotIndex, command.RuntimeId))
            return preferredSlotIndex;

        for (int i = preferredSlotIndex + 1; i < monsterSlotCount; i++)
        {
            if (CanMonsterUseSlot(i, command.RuntimeId))
                return i;
        }

        for (int i = preferredSlotIndex - 1; i >= 0; i--)
        {
            if (CanMonsterUseSlot(i, command.RuntimeId))
                return i;
        }

        return -1;
    }

    private bool CanMonsterUseSlot(int slotIndex, string runtimeId)
    {
        if (slotIndex < 0 || slotIndex >= GetMonsterReservationSlotCount())
            return false;

        if (string.IsNullOrWhiteSpace(runtimeId))
            return false;

        if (GetRemainingCombinedCommandCapacity(slotIndex) <= 0)
            return false;

        List<MonsterReservedCommand> commands = monsterCommandsBySlot[slotIndex];

        if (commands == null || commands.Count <= 0)
            return true;

        for (int i = 0; i < commands.Count; i++)
        {
            MonsterReservedCommand command = commands[i];

            if (command == null || command.RuntimeId != runtimeId)
                return false;
        }

        return true;
    }

    private int GetMonsterReservationSlotCount()
    {
        if (monsterCommandsBySlot == null)
            return 0;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return monsterCommandsBySlot.Length;

        return Mathf.Min(reserveSlots.Length, monsterCommandsBySlot.Length);
    }

    public void ClearMonsterReservations()
    {
        InitializeMonsterCommandSlots();

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
            monsterCommandsBySlot[i].Clear();

        reservationVersion++;
        RefreshTimeline();
    }

    public void RemoveCommand(int slotIndex, int orderIndex)
    {
        if (reserveSlots == null)
            return;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return;

        bool removed = slot.RemoveCommandAt(orderIndex, out PlayerReservedCommand removedCommand);

        if (!removed)
            return;

        RemoveReservedCosts(removedCommand);

        if (IsMoveCommand(removedCommand))
            RemoveFollowingMoveCommands(slot, orderIndex, removedCommand.CharacterId);


        RecalculateAllReservedCosts();
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        Debug.Log($"[BattleTimelineController] 예약 취소 / Slot:{slotIndex} / Order:{orderIndex}");
    }

    private bool IsMoveCommand(PlayerReservedCommand command)
    {
        return command != null && command.ReservedMoveGridIndex >= 0;
    }

    private void RemoveFollowingMoveCommands(
        ReserveTurnSlotUI slot,
        int startIndex,
        string characterId)
    {
        if (slot == null || slot.Commands == null)
            return;

        for (int i = slot.Commands.Count - 1; i >= startIndex; i--)
        {
            PlayerReservedCommand command = slot.Commands[i];

            if (command == null)
                continue;

            if (command.CharacterId != characterId)
                continue;

            if (!IsMoveCommand(command))
                continue;

            if (slot.RemoveCommandAt(i, out PlayerReservedCommand removedCommand))
                RemoveReservedCosts(removedCommand);
        }
    }

    public void ClearAllReservations()
    {
        ClearSelectedSlotSelection();
        reservationVersion++;

        if (reserveSlots != null)
        {
            for (int i = 0; i < reserveSlots.Length; i++)
            {
                if (reserveSlots[i] == null)
                    continue;

                var commands = reserveSlots[i].Commands;

                for (int j = commands.Count - 1; j >= 0; j--)
                {
                    if (reserveSlots[i].RemoveCommandAt(j, out PlayerReservedCommand removedCommand))
                        RemoveReservedCosts(removedCommand);
                }

                reserveSlots[i].Clear();
            }
        }

        ClearAllMonsterCommandsWithoutRefresh();

        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
    }

    public int ApplyBlockedMoveCostRefunds()
    {
        int totalRefund = 0;

        if (reserveSlots == null)
            return totalRefund;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (!IsMoveCommand(command))
                    continue;

                int refund = command.ApplyBlockedMoveCostRefund();

                if (refund <= 0)
                    continue;

                totalRefund += refund;
                Debug.Log(
                    $"[BattleTimelineController] Move Cost refund / " +
                    $"Character:{command.CharacterId} / Refund:{refund}"
                );
            }
        }

        if (totalRefund > 0)
            RefreshPlayerHUDs();

        return totalRefund;
    }

    private void ClearAllMonsterCommandsWithoutRefresh()
    {
        InitializeMonsterCommandSlots();

        if (monsterCommandsBySlot == null)
            return;

        for (int i = 0; i < monsterCommandsBySlot.Length; i++)
        {
            if (monsterCommandsBySlot[i] != null)
                monsterCommandsBySlot[i].Clear();
        }
    }

    private void RemoveReservedCosts(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null)
            return;

        command.UserRuntime.RemoveReservedHP(command.HPCost);

        if (!command.MoveCostConsumed)
            command.UserRuntime.RemoveReservedCost(command.Cost);

        command.UserRuntime.RemoveReservedResource(command.ResourceCost);
        command.UserRuntime.RemoveReservedShield(command.ShieldCost);
    }

    private void RefreshReservationSimulation()
    {
        if (gridManager == null)
            return;

        BattleActionSimulationService simulator = new(gridManager);
        simulator.Simulate(this);
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    private void RefreshTimeline()
    {
        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        BattleTimelineBarUI standbyBar = GetStandbyTimelineBarUI();

        if (activeBar != null)
            activeBar.Refresh(reserveSlots, monsterCommandsBySlot);
        else
        {
            ShowBattleWarning("타임라인 UI를 찾을 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] active timelineBarUI가 없습니다.");
        }

        if (standbyBar != null && standbyBar != activeBar)
        {
            standbyBar.Clear();
            standbyBar.SetTurnMarkChildrenVisible(false);
            standbyBar.SetEmptyUseSkillSlotsVisible(false);
        }

        if (activeBar != null)
        {
            activeBar.SetTurnMarkChildrenVisible(true);
            activeBar.SetEmptyUseSkillSlotsVisible(true);
        }

        RefreshTotalUsedCostText();
    }

    private void RefreshPlayerHUDs()
    {
        PlayerHUDSlot[] hudSlots = FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < hudSlots.Length; i++)
        {
            if (hudSlots[i] != null)
                hudSlots[i].Refresh();
        }
    }

    public int GetPreviewGridIndexBeforeCommand(
    CharacterRuntimeData runtimeData,
    int targetSlotIndex,
    int targetPlayerCommandIndex)
    {
        if (runtimeData == null)
            return -1;

        int gridIndex = GetCurrentBattleCharacterGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            gridIndex = GetRuntimeStartGridIndex(runtimeData.CharacterId);

        if (gridIndex < 0)
            return -1;

        if (reserveSlots == null)
            return gridIndex;

        for (int slotIndex = 0; slotIndex <= targetSlotIndex; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (command.UserRuntime.CharacterId != runtimeData.CharacterId)
                    continue;

                if (slotIndex == targetSlotIndex && i >= targetPlayerCommandIndex)
                    break;

                if (command.ReservedMoveGridIndex >= 0)
                    gridIndex = command.EffectiveMoveGridIndex;
            }
        }

        return gridIndex;
    }

    private void RefreshMoveGhostPreview()
    {
        if (moveGhostPreview == null)
            moveGhostPreview = FindFirstObjectByType<MoveGhostPreview>(FindObjectsInactive.Include);

        if (moveGhostPreview == null)
            return;

        if (reserveSlots == null)
        {
            moveGhostPreview.ClearAll();
            return;
        }

        Dictionary<string, PlayerReservedCommand> lastMoveCommands =
            GetLastMoveGhostCommandsByCharacter();

        foreach (var pair in lastMoveCommands)
        {
            PlayerReservedCommand command = pair.Value;

            if (command == null || command.UserRuntime == null)
                continue;

            Sprite sprite = GetCharacterSprite(command.UserRuntime.CharacterId);

            moveGhostPreview.Show(
                command.UserRuntime.CharacterId,
                sprite,
                command.PreviewMoveGridIndex,
                command.PreviewMoveDirection
            );
        }

        moveGhostPreview.ClearExcept(lastMoveCommands.Keys);
    }

    private Dictionary<string, PlayerReservedCommand> GetLastMoveGhostCommandsByCharacter()
    {
        Dictionary<string, PlayerReservedCommand> result = new();

        if (reserveSlots == null)
            return result;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (command == null || command.UserRuntime == null)
                    continue;

                if (!IsGhostMoveCommandForRuntime(command, command.UserRuntime.CharacterId))
                    continue;

                result[command.UserRuntime.CharacterId] = command;
            }
        }

        return result;
    }

    private bool IsGhostMoveCommandForRuntime(PlayerReservedCommand command, string characterId)
    {
        if (command == null || command.UserRuntime == null)
            return false;

        if (command.ReservedMoveGridIndex < 0 || command.PreviewMoveGridIndex < 0)
            return false;

        return command.UserRuntime.CharacterId == characterId;
    }

    private Sprite GetCharacterSprite(string characterId)
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.CharacterId != characterId)
                continue;

            SpriteRenderer spriteRenderer =
                character.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                return spriteRenderer.sprite;
        }

        return null;
    }
}
