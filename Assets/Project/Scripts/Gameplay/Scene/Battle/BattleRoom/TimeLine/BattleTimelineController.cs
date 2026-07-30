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
    [Tooltip("?댁쟾 援ъ“ ?명솚?⑹엯?덈떎. 鍮꾩뼱 ?덉? ?딆쑝硫?TimelineBar1濡??ъ슜?⑸땲??")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI;
    [Tooltip("????댁뿉 ?덉빟 ?쒖떆瑜??대떦?섎뒗 TimelineBar?낅땲??")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI1;
    [Tooltip("吏앹닔 ?댁뿉 ?덉빟 ?쒖떆瑜??대떦?섎뒗 TimelineBar?낅땲??")]
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
    [SerializeField] private string slotSelectionLockedMessage = "??吏꾪뻾 以묒뿉???щ’???좏깮?????놁뒿?덈떎.";

    [Header("Auto Slot Selection")]
    [SerializeField] private bool autoSelectFirstSlotWhenInputReady = true;
    [SerializeField] private int defaultSlotIndex = 0;

    [Header("Character Selection Camera Focus")]
    [SerializeField] private bool focusCameraOnCharacterSelect = true;
    [SerializeField] private bool focusCameraOnlyWhenInputReady = true;
    [SerializeField] private bool refocusSameCharacter = false;

    [Header("Keyboard Input")]
    [SerializeField] private bool enableKeyboardSlotMoveInput = true;
    [SerializeField] private BattleTurnExecutor turnExecutor;

    [Header("Timeline Bar Slide")]
    [SerializeField] private bool playTimelineSlotSlide = true;
    [Tooltip("?댁쟾 援ъ“ ?명솚?⑹엯?덈떎. 鍮꾩뼱 ?덉? ?딆쑝硫?TimelineBar1 ?대룞 ??곸쑝濡??ъ슜?⑸땲??")]
    [SerializeField] private RectTransform timelineBarSlideTarget;
    [Tooltip("????댁뿉 ?ъ슜?섎뒗 TimelineBar1 ?대룞 ??곸엯?덈떎.")]
    [SerializeField] private RectTransform timelineBarSlideTarget1;
    [Tooltip("吏앹닔 ?댁뿉 ?ъ슜?섎뒗 TimelineBar2 ?대룞 ??곸엯?덈떎.")]
    [SerializeField] private RectTransform timelineBarSlideTarget2;
    [Tooltip("?湲?以묒씤 TimelineBar瑜??꾩옱 TimelineBar ?ㅻⅨ履쎌뿉 ?댁뼱遺숈씪 X 嫄곕━?낅땲??")]
    [SerializeField] private float standbyTimelineBarOffsetX = 1420f;
    [Tooltip("5?щ’源뚯? 紐⑤몢 吏꾪뻾?????꾩옱 TimelineBar媛 ?꾩갑?댁빞 ?섎뒗 X ?꾩튂?낅땲?? 湲곕낯 ?꾩튂 X=0 湲곗??낅땲?? 5?щ’ 醫낅즺 ?꾩튂?먯꽌 ??媛믨퉴吏 異붽? ?대룞?⑸땲??")]
    [SerializeField] private float completedTurnTimelineBarPositionX = -1420f;
    [Tooltip("?댁뿏??踰꾪듉???꾨Ⅸ 吏곹썑, 1踰??щ’???쒖옉?섍린 ?꾩뿉 TimelineBar媛 癒쇱? ?쇱そ?쇰줈 ?대룞?섎뒗 嫄곕━?낅땲?? 1踰??щ’?먯꽌留???踰??곸슜?⑸땲??")]
    [SerializeField] private float firstSlotEndTurnTimelineLineSlideAmountX = -60f;
    [SerializeField] private float timelineSlotSlideDuration = 0.18f;
    [Tooltip("TurnMark? Use_skill??4?꾨젅??媛덈┝ ?좊땲硫붿씠?섏씠 ?덉뿉 蹂댁씠?꾨줉, 媛덈┝ ?곗텧怨??④퍡 ?대룞?????ъ슜?섎뒗 理쒖냼 ?대룞 ?쒓컙?낅땲??")]
    [SerializeField] private float grindTimelineSlideDuration = 0.32f;
    [SerializeField] private bool useUnscaledTimeForTimelineSlotSlide = false;

    [Header("Timeline Sprite Grind Animation")]
    [SerializeField] private BattleTimelineSpriteAnimationController timelineSpriteAnimationController;
    [SerializeField] private bool autoFindTimelineSpriteAnimationController = true;
    [Tooltip("媛??щ’???쒖옉????TurnMark媛 媛덈━硫댁꽌 TimelineBar ?꾩껜媛 ?쇱そ?쇰줈 ?대룞?섎뒗 嫄곕━?낅땲??")]
    [SerializeField] private float slotStartTimelineLineSlideAmountX = -50f;
    [Tooltip("?대떦 ?щ’??泥?踰덉㎏ Use_skill??媛덈┫ ???꾩껜 ??꾨씪???쇱씤???쇱そ?쇰줈 ?대룞?섎뒗 嫄곕━?낅땲??")]
    [SerializeField] private float firstUseSkillTimelineLineSlideAmountX = -45f;
    [Tooltip("?대떦 ?щ’????踰덉㎏ ?댄썑 Use_skill??媛덈┫ ???꾩껜 ??꾨씪???쇱씤???쇱そ?쇰줈 ?대룞?섎뒗 嫄곕━?낅땲??")]
    [SerializeField] private float additionalUseSkillTimelineLineSlideAmountX = -40f;

    [Header("Timeline Grind VFX")]
    [SerializeField] private GameObject timelineGrindVfxPrefab;
    [SerializeField] private Vector3 timelineGrindVfxPosition = new(-6.0f, -3.6f, 0f);
    [SerializeField] private float timelineGrindVfxLifeTime = 2f;

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
    private int playerLockedSlotIndex = -1;
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

    private readonly List<PlayerReservationHistoryEntry> playerReservationHistory = new();
    private readonly HashSet<int> networkViewedSlotIndices = new();

    public int SlotCount => reserveSlots != null ? reserveSlots.Length : 0;
    public int ActiveSlotIndex => activeSlotIndex;
    public int ReservationVersion => reservationVersion;
    public CharacterRuntimeData SelectedCharacter => selectedCharacter;
    public int PlayerLockedSlotIndex => playerLockedSlotIndex;
    public bool HasPlayerLockedSlot => playerLockedSlotIndex >= 0;


    private void OnValidate()
    {
        // Unity???ㅽ겕由쏀듃 湲곕낯媛믪씠 諛붾뚯뼱???대? ???꾨━?뱀뿉 ??λ맂 Inspector 媛믪쓣 ?좎??⑸땲??
        // ?댁쟾 ?섏젙蹂몄뿉???⑥? 1335 / -1440 媛믪? ?꾩옱 援ъ“??湲곗?媛믪씤 1420 / -1420?쇰줈 ?먮룞 蹂댁젙?⑸땲??
        if (Mathf.Approximately(standbyTimelineBarOffsetX, 1335f) || standbyTimelineBarOffsetX <= 0f)
            standbyTimelineBarOffsetX = 1420f;

        if (Mathf.Approximately(completedTurnTimelineBarPositionX, -1440f) || completedTurnTimelineBarPositionX >= 0f)
            completedTurnTimelineBarPositionX = -1420f;

        if (timelineGrindVfxLifeTime < 0f)
            timelineGrindVfxLifeTime = 0f;

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
        HandleKeyboardSlotMoveInput();
        HandleKeyboardUndoReservationInput();
        HandleEndButtonHoverOutsidePolling();
    }

    private void HandleKeyboardSlotMoveInput()
    {
        if (!enableKeyboardSlotMoveInput)
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (!isActiveAndEnabled)
            return;

        if (IsTypingInputFieldSelected())
            return;

        int direction = 0;

        if (Input.GetKeyDown(KeyCode.D))
            direction = 1;
        else if (Input.GetKeyDown(KeyCode.A))
            direction = -1;

        if (direction == 0)
            return;

        MoveSelectedTimelineSlot(direction);
    }

    private void HandleKeyboardUndoReservationInput()
    {
        if (!isActiveAndEnabled)
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (IsTypingInputFieldSelected())
            return;

        if (!Input.GetKeyDown(KeyCode.Q))
            return;

        UndoLastPlayerReservation();
    }

    public void UndoLastPlayerReservation()
    {
        if (isSlotSelectionLocked)
            return;

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return;

        PruneInvalidPlayerReservationHistory();

        if (playerReservationHistory.Count <= 0)
        {
            ShowBattleWarning("?섎룎由??덉빟???놁뒿?덈떎.");
            return;
        }

        PlayerReservationHistoryEntry entry = playerReservationHistory[playerReservationHistory.Count - 1];
        playerReservationHistory.RemoveAt(playerReservationHistory.Count - 1);

        RemovePlayerReservationEntry(entry, true);
    }

    public void MoveSelectedTimelineSlot(int direction)
    {
        if (direction == 0)
            return;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return;

        if (isSlotSelectionLocked)
            return;

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return;

        int slotCount = reserveSlots.Length;
        int currentIndex = activeSlotIndex >= 0
            ? activeSlotIndex
            : Mathf.Clamp(defaultSlotIndex, 0, slotCount - 1);

        int nextIndex = currentIndex + direction;

        while (nextIndex < 0)
            nextIndex += slotCount;

        while (nextIndex >= slotCount)
            nextIndex -= slotCount;

        int safety = 0;
        while (safety < slotCount && !IsTimelineSlotSelectable(nextIndex))
        {
            nextIndex += direction;

            while (nextIndex < 0)
                nextIndex += slotCount;

            while (nextIndex >= slotCount)
                nextIndex -= slotCount;

            safety++;
        }

        if (!IsTimelineSlotSelectable(nextIndex))
            return;

        OnTimelineSlotClicked(nextIndex);
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
        bool isChangingCharacter =
            runtimeData != null &&
            (selectedCharacter == null ||
             selectedCharacter.CharacterId != runtimeData.CharacterId);

        selectedCharacter = runtimeData;

        if (isChangingCharacter)
            TryAutoSelectSlotForCharacter(runtimeData);

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

        BattleCharacter focusCharacter = FindBattleCharacter(runtimeData.CharacterId);

        if (focusCharacter == null)
            return;

        cameraController.FocusOnCharacterSelection(focusCharacter.transform, focusCharacter.CurrentGridIndex);
        lastCameraFocusedCharacterId = runtimeData.CharacterId;
    }

    private BattleCharacter FindBattleCharacter(string characterId)
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

            return character;
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

        if (SteamBattleStateSynchronizer.TryHandleTimelineSlotClicked(this, slotIndex))
            return;

        if (IsPlayerSlotLocked(slotIndex))
        {
            ShowPlayerLockedSlotWarning();
            return;
        }

        SetActiveTimelineSlot(slotIndex, true);
    }

    public bool SelectTimelineSlotFromNetwork(int slotIndex, bool tryStartReservation)
    {
        return SetActiveTimelineSlot(slotIndex, tryStartReservation);
    }

    private bool SetActiveTimelineSlot(int slotIndex, bool tryStartReservation)
    {
        if (reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        if (IsPlayerSlotLocked(slotIndex))
            return false;

        if (reserveSlots[slotIndex] == null)
            return false;

        int previousSlotIndex = activeSlotIndex;
        activeSlotIndex = slotIndex;

        SetActiveTimelineSlotVisual(activeSlotIndex);

        RefreshSelectedSlotValueText();
        PlaySelectedSlotEffect(previousSlotIndex, activeSlotIndex);

        if (tryStartReservation)
            TryStartSkillReservation();

        return true;
    }

    private bool TryAutoSelectSlotForCharacter(CharacterRuntimeData runtimeData)
    {
        if (runtimeData == null)
            return false;

        if (!CanAutoSelectSlotForCharacter())
            return false;

        TimelineAutoSlotState[] slotStates = BuildAutoSlotStates(runtimeData);
        int targetSlotIndex =
            TimelineAutoSlotSelectionUtility.FindBestSlot(slotStates, activeSlotIndex);

        if (targetSlotIndex < 0 || targetSlotIndex == activeSlotIndex)
            return false;

        return SetActiveTimelineSlot(targetSlotIndex, false);
    }

    private bool CanAutoSelectSlotForCharacter()
    {
        if (isSlotSelectionLocked)
            return false;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return false;

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        if (turnExecutor != null && !turnExecutor.CanAcceptPlayerInput)
            return false;

        return true;
    }

    private TimelineAutoSlotState[] BuildAutoSlotStates(CharacterRuntimeData runtimeData)
    {
        if (reserveSlots == null)
            return System.Array.Empty<TimelineAutoSlotState>();

        TimelineAutoSlotState[] result = new TimelineAutoSlotState[reserveSlots.Length];
        string characterId = runtimeData != null ? runtimeData.CharacterId : null;

        for (int i = 0; i < reserveSlots.Length; i++)
        {
            ReserveTurnSlotUI slot = reserveSlots[i];

            bool isSelectable = IsTimelineSlotSelectable(i);

            result[i] = new TimelineAutoSlotState(
                isSelectable,
                isSelectable && slot.CommandCount <= 0,
                isSelectable && slot.CanAcceptCharacter(runtimeData),
                isSelectable && CanAddPlayerCommandToSlot(i),
                isSelectable && HasPlayerCommandForCharacter(slot, characterId)
            );
        }

        return result;
    }

    private bool HasPlayerCommandForCharacter(ReserveTurnSlotUI slot, string characterId)
    {
        if (slot == null || slot.Commands == null || string.IsNullOrWhiteSpace(characterId))
            return false;

        for (int i = 0; i < slot.Commands.Count; i++)
        {
            PlayerReservedCommand command = slot.Commands[i];

            if (command == null || command.UserRuntime == null)
                continue;

            if (command.UserRuntime.CharacterId == characterId)
                return true;
        }

        return false;
    }

    public void ClearSelectedSlotSelection()
    {
        activeSlotIndex = -1;
        selectedSkill = null;

        SetActiveTimelineSlotVisual(activeSlotIndex);

        RefreshSelectedSlotValueText();
    }

    public void StopTimelineMotionEffects()
    {
        if (selectedSlotEffectRoutine != null)
        {
            StopCoroutine(selectedSlotEffectRoutine);
            selectedSlotEffectRoutine = null;
        }

        if (timelineSlideGearRotationRoutine != null)
        {
            StopCoroutine(timelineSlideGearRotationRoutine);
            timelineSlideGearRotationRoutine = null;
        }

        if (endButtonHoverRotationRoutine != null)
        {
            StopCoroutine(endButtonHoverRotationRoutine);
            endButtonHoverRotationRoutine = null;
        }

        isEndButtonHovering = false;
    }

    public void SetSlotSelectionLocked(bool locked)
    {
        isSlotSelectionLocked = locked;

        if (locked)
            CancelEndButtonHoverRotationIfNeeded();
    }

    public void SetPlayerLockedSlot(int slotIndex)
    {
        int normalizedSlotIndex = IsValidReserveSlotIndex(slotIndex) ? slotIndex : -1;

        if (playerLockedSlotIndex == normalizedSlotIndex)
        {
            RefreshPlayerLockedSlotVisuals();
            return;
        }

        playerLockedSlotIndex = normalizedSlotIndex;

        if (IsPlayerSlotLocked(activeSlotIndex))
        {
            activeSlotIndex = FindFirstSelectableTimelineSlot(activeSlotIndex);
            selectedSkill = null;

            if (playerSkillReservationController != null)
                playerSkillReservationController.ClearPreview();

            SetActiveTimelineSlotVisual(activeSlotIndex);
            RefreshSelectedSlotValueText();
        }

        RefreshPlayerLockedSlotVisuals();
    }

    public void ClearPlayerLockedSlot()
    {
        SetPlayerLockedSlot(-1);
    }

    public bool IsPlayerSlotLocked(int slotIndex)
    {
        return IsPlayerSlotLocked(slotIndex, false);
    }

    private bool IsPlayerSlotLocked(int slotIndex, bool ignoreNetworkViewedSlotLock)
    {
        return (playerLockedSlotIndex >= 0 && slotIndex == playerLockedSlotIndex) ||
               (!ignoreNetworkViewedSlotLock && networkViewedSlotIndices.Contains(slotIndex));
    }

    public void SetNetworkViewedSlots(IReadOnlyList<int> slotIndices)
    {
        networkViewedSlotIndices.Clear();

        if (slotIndices != null)
        {
            for (int i = 0; i < slotIndices.Count; i++)
            {
                if (IsValidReserveSlotIndex(slotIndices[i]))
                    networkViewedSlotIndices.Add(slotIndices[i]);
            }
        }

        if (IsPlayerSlotLocked(activeSlotIndex))
        {
            activeSlotIndex = FindFirstSelectableTimelineSlot(activeSlotIndex);

            SetActiveTimelineSlotVisual(activeSlotIndex);
            RefreshSelectedSlotValueText();
        }

        RefreshPlayerLockedSlotVisuals();
    }

    private bool IsValidReserveSlotIndex(int slotIndex)
    {
        return reserveSlots != null && slotIndex >= 0 && slotIndex < reserveSlots.Length;
    }

    private bool IsTimelineSlotSelectable(int slotIndex)
    {
        return IsValidReserveSlotIndex(slotIndex) &&
               reserveSlots[slotIndex] != null &&
               !IsPlayerSlotLocked(slotIndex);
    }

    private int FindFirstSelectableTimelineSlot(int preferredSlotIndex)
    {
        if (reserveSlots == null || reserveSlots.Length <= 0)
            return -1;

        int startIndex = preferredSlotIndex >= 0 && preferredSlotIndex < reserveSlots.Length
            ? preferredSlotIndex
            : Mathf.Clamp(defaultSlotIndex, 0, reserveSlots.Length - 1);

        for (int offset = 0; offset < reserveSlots.Length; offset++)
        {
            int slotIndex = (startIndex + offset) % reserveSlots.Length;

            if (IsTimelineSlotSelectable(slotIndex))
                return slotIndex;
        }

        return -1;
    }

    private void RefreshPlayerLockedSlotVisuals()
    {
        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        BattleTimelineBarUI standbyBar = GetStandbyTimelineBarUI();
        int visualLockedSlotIndex = playerLockedSlotIndex;

        if (visualLockedSlotIndex < 0)
        {
            foreach (int slotIndex in networkViewedSlotIndices)
            {
                visualLockedSlotIndex = slotIndex;
                break;
            }
        }

        if (activeBar != null)
            activeBar.SetPlayerLockedSlot(visualLockedSlotIndex);

        if (standbyBar != null && standbyBar != activeBar)
            standbyBar.SetPlayerLockedSlot(-1);
    }

    private void ShowPlayerLockedSlotWarning()
    {
        ShowBattleWarning("선택할 수 없는 슬롯입니다.");
    }
    public void SelectDefaultSlotWhenInputReady()
    {
        if (!autoSelectFirstSlotWhenInputReady)
            return;

        if (isSlotSelectionLocked)
            return;

        if (reserveSlots == null || reserveSlots.Length <= 0)
            return;

        int slotIndex = FindFirstSelectableTimelineSlot(
            Mathf.Clamp(defaultSlotIndex, 0, reserveSlots.Length - 1));

        if (slotIndex < 0)
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

        // 5踰??щ’??TurnMark媛 媛덈졇?ㅺ퀬 ?댁꽌 ???쇱씤??諛붾줈 ?꾨즺 ?꾩튂濡?蹂대궡硫?
        // 5踰??щ’???깅줉??Use_skill?ㅼ씠 媛쒕퀎?곸쑝濡?媛덈━湲??꾩뿉 ??踰덉뿉 ?대룞??蹂댁엯?덈떎.
        // ?꾨즺 ?꾩튂 蹂댁젙? BattleTurnExecutor媛 紐⑤뱺 ?щ’/?ㅽ궗 泥섎━瑜??앸궦 ???몄텧?⑸땲??
    }

    public IEnumerator PlayTimelineTurnMarkAnimationRoutine(int slotIndex)
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        ConfigureTimelineSpriteAnimationRootForActiveBar();

        if (timelineSpriteAnimationController == null)
            yield break;

        // TurnMark ?꾨젅?꾨쭔 ?ъ깮?⑸땲?? ?ㅼ젣 ?쇱씤 ?대룞? PlayTimelineTurnMarkAnimationAndLineSlideRoutine?먯꽌 ?④퍡 泥섎━?⑸땲??
        PlayTimelineSlideGearRotation(1);
        SpawnTimelineGrindVfx();
        yield return timelineSpriteAnimationController.PlayTurnMarkRoutine(slotIndex);
    }

    private IEnumerator PlayTimelineTurnMarkAnimationAndLineSlideRoutine(int slotIndex, bool isEmptySlot)
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        ConfigureTimelineSpriteAnimationRootForActiveBar();

        // ???붾뱶 吏곹썑 1踰??щ’??吏꾪뻾???뚮쭔 ?쇱씤??癒쇱? -60 ?대룞?⑸땲??
        // 2踰??щ’遺?곕뒗 ???좏뻾 ?대룞 ?놁씠 ?щ’ ?쒖옉 ?대룞留?吏꾪뻾?⑸땲??
        if (slotIndex == 0 && !Mathf.Approximately(firstSlotEndTurnTimelineLineSlideAmountX, 0f))
        {
            PlayTimelineSlideGearRotation(1);
            yield return MoveAllTimelineSlotSlideTargetsByOffsetRoutine(firstSlotEndTurnTimelineLineSlideAmountX);
        }

        // ?щ’ ?쒖옉 ??TurnMark ?좊땲硫붿씠?섏쓣 癒쇱? 蹂댁뿬二쇨퀬, 洹??ㅼ쓬 TimelineBar ?꾩껜瑜??대룞?⑸땲??
        // ?ㅽ궗???녿뒗 ?щ’? Use_skill 1~5移멸퉴吏 ??踰덉뿉 ?대룞?댁꽌 ?ㅼ쓬 ?щ’ 吏곸쟾源뚯? 蹂대깄?덈떎.
        float animationDuration = GetTurnMarkGrindDuration();

        if (timelineSpriteAnimationController != null)
        {
            SpawnTimelineGrindVfx();
            yield return timelineSpriteAnimationController.PlayTurnMarkRoutine(slotIndex);
        }

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

        // ?대? ?꾨즺 ?꾩튂???꾩갑?덇굅??吏?섏튇 寃쎌슦?먮뒗 異붽? ?대룞???ъ깮?섏? ?딆뒿?덈떎.
        // active??-1420, standby??0?쇰줈 ?꾩튂留?蹂댁젙?⑸땲??
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

            // Use_skill ?좊땲硫붿씠?섏쓣 癒쇱? 蹂댁뿬二쇨퀬, 洹??ㅼ쓬 TimelineBar ?꾩껜瑜??대룞?⑸땲??
            float animationDuration = GetUseSkillGrindDuration();

            if (timelineSpriteAnimationController != null)
            {
                SpawnTimelineGrindVfx();
                yield return timelineSpriteAnimationController.PlayUseSkillRoutine(slotIndex, orderIndex);
            }

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

    private GameObject SpawnTimelineGrindVfx()
    {
        if (timelineGrindVfxPrefab == null)
            return null;

        GameObject vfx = Instantiate(
            timelineGrindVfxPrefab,
            timelineGrindVfxPosition,
            Quaternion.identity);

        if (timelineGrindVfxLifeTime > 0f)
            Destroy(vfx, timelineGrindVfxLifeTime);

        return vfx;
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

        // ??TimelineBar???ㅼ젣 RectTransform width???꾩옱 諛곗튂媛믪쓣 湲곗??쇰줈 媛꾧꺽???ㅼ떆 怨꾩궛?섏? ?딆뒿?덈떎.
        // Inspector?먯꽌 吏?뺥븳 Standby Timeline Bar Offset X 媛믩쭔 ?ъ슜?댁빞
        // 0 / 1420 ?꾩튂瑜?踰덇컝???곕뒗 援ъ“媛 ?붾뱾由ъ? ?딆뒿?덈떎.
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



    public void ResetTimelineBarsForNewBattleRoom()
    {
        AutoFindTimelineBarsIfNeeded();
        CaptureTimelineSlotOriginalPositionsIfNeeded();

        if (timelineSlotSlideRoutine != null)
        {
            StopCoroutine(timelineSlotSlideRoutine);
            timelineSlotSlideRoutine = null;
        }

        if (timelineSlideGearRotationRoutine != null)
        {
            StopCoroutine(timelineSlideGearRotationRoutine);
            timelineSlideGearRotationRoutine = null;
        }

        activeTimelineBarIndex = 0;
        completedTimelineBarPositionApplied = false;
        timelineSlotSlideStepIndex = 0;

        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();
        RectTransform standbyTarget = GetStandbyTimelineBarSlideTarget();
        Vector2 basePosition = GetTimelineBarBasePosition();

        if (activeTarget != null)
            activeTarget.anchoredPosition = basePosition;

        if (standbyTarget != null && standbyTarget != activeTarget)
            standbyTarget.anchoredPosition = basePosition + new Vector2(resolvedStandbyTimelineBarOffsetX, 0f);

        ConfigureTimelineSpriteAnimationRootForActiveBar();

        if (timelineSpriteAnimationController != null)
            timelineSpriteAnimationController.ResetTimelineSpritesForNextTurn();

        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        BattleTimelineBarUI standbyBar = GetStandbyTimelineBarUI();

        if (activeBar != null)
        {
            activeBar.SetActiveTimelineSlot(activeSlotIndex);
            activeBar.SetTurnMarkChildrenVisible(true);
            activeBar.SetEmptyUseSkillSlotsVisible(true);
        }

        if (standbyBar != null && standbyBar != activeBar)
        {
            standbyBar.Clear();
            standbyBar.SetPlayerLockedSlot(-1);
            standbyBar.SetActiveTimelineSlot(-1);
            standbyBar.SetTurnMarkChildrenVisible(false);
            standbyBar.SetEmptyUseSkillSlotsVisible(false);
        }

        RefreshTimeline();
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

        // ??媛쒖쓽 TimelineBar媛 0 / 1420 ?꾩튂瑜?踰덇컝???곕뒗 援ъ“?먯꽌??
        // 吏꾪뻾 以묒씤 諛붽? ?꾨즺 ?꾩튂(-1420)???꾩갑????異붽? 蹂댁젙 ?대룞???ㅼ뼱媛硫????⑸땲??
        // 洹몃옒??紐⑤뱺 ?쇱씤 ?대룞 ?붿껌? ?꾨즺 ?꾩튂瑜??섏? ?딅룄濡???긽 ??踰??대옩?꾪빀?덈떎.
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
                ShowBattleWarning("??꾨씪???щ’??癒쇱? ?좏깮?댁＜?몄슂.");

            return;
        }

        if (selectedCharacter == null && selectedSkill == null)
        {
            ShowBattleWarning("罹먮┃?곗? ?ㅽ궗??癒쇱? ?좏깮?댁＜?몄슂.");
            return;
        }

        if (selectedCharacter == null)
        {
            ShowBattleWarning("罹먮┃?곕? 癒쇱? ?좏깮?댁＜?몄슂.");
            return;
        }

        if (selectedSkill == null)
            return;

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("??꾨씪???щ’???놁뒿?덈떎.");
            selectedSkill = null;
            return;
        }

        if (activeSlotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("?좏깮????꾨씪???щ’???ъ슜?????놁뒿?덈떎.");
            selectedSkill = null;
            return;
        }

        ReserveTurnSlotUI slot = reserveSlots[activeSlotIndex];

        if (slot == null)
        {
            ShowBattleWarning("?좏깮????꾨씪???щ’???ъ슜?????놁뒿?덈떎.");
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
            ShowBattleWarning("???щ’?먮뒗 ?대? ?ㅻⅨ 罹먮┃?곗쓽 ?됰룞???덉빟?섏뼱 ?덉뒿?덈떎.");
            Debug.LogWarning("[BattleTimelineController] ????꾨씪???щ’?먮뒗 ?대? ?ㅻⅨ 罹먮┃?곗쓽 ?됰룞???덉빟?섏뼱 ?덉뒿?덈떎.");
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
            ShowBattleWarning("罹먮┃???꾩튂瑜?李얠쓣 ???놁뒿?덈떎.");
            Debug.LogWarning($"[BattleTimelineController] 罹먮┃???꾩튂瑜?李얠쓣 ???놁뒿?덈떎: {selectedCharacter.CharacterId}");
            selectedSkill = null;
            return;
        }

        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        if (playerSkillReservationController == null)
        {
            ShowBattleWarning("?ㅽ궗 ?덉빟 而⑦듃濡ㅻ윭瑜?李얠쓣 ???놁뒿?덈떎.");
            Debug.LogWarning("[BattleTimelineController] PlayerSkillReservationController媛 ?놁뒿?덈떎.");
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
        if (SteamBattleStateSynchronizer.TryHandlePlayerCommandReservation(
                this,
                slotIndex,
                command,
                out bool networkAccepted))
        {
            return networkAccepted;
        }

        return ConfirmPlayerCommandFromNetwork(slotIndex, command);
    }

    public bool ConfirmPlayerCommandFromNetwork(int slotIndex, PlayerReservedCommand command)
    {
        return ConfirmPlayerCommandFromNetwork(slotIndex, command, false);
    }

    public bool ConfirmPlayerCommandFromNetwork(
        int slotIndex,
        PlayerReservedCommand command,
        bool ignoreNetworkViewedSlotLock)
    {
        if (command == null)
        {
            ShowBattleWarning("?덉빟???ㅽ궗 ?뺣낫媛 ?놁뒿?덈떎.");
            return false;
        }

        if (reserveSlots == null || reserveSlots.Length <= 0)
        {
            ShowBattleWarning("??꾨씪???щ’???놁뒿?덈떎.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
        {
            ShowBattleWarning("?좏깮????꾨씪???щ’???ъ슜?????놁뒿?덈떎.");
            return false;
        }

        if (IsPlayerSlotLocked(slotIndex, ignoreNetworkViewedSlotLock))
        {
            ShowPlayerLockedSlotWarning();
            return false;
        }

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
        {
            ShowBattleWarning("?좏깮????꾨씪???щ’???ъ슜?????놁뒿?덈떎.");
            return false;
        }

        if (!slot.CanAcceptCharacter(command.UserRuntime))
        {
            ShowBattleWarning("???щ’?먮뒗 ?대? ?ㅻⅨ 罹먮┃?곗쓽 ?됰룞???덉빟?섏뼱 ?덉뒿?덈떎.");
            Debug.LogWarning("[BattleTimelineController] ????꾨씪???щ’?먮뒗 ?대? ?ㅻⅨ 罹먮┃?곗쓽 ?됰룞???덉빟?섏뼱 ?덉뒿?덈떎.");
            return false;
        }

        string equipmentBlockReason = GetEquipmentReservationBlockReason(command, slotIndex);
        if (!string.IsNullOrEmpty(equipmentBlockReason))
        {
            ShowBattleWarning(equipmentBlockReason);
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

        if (!CanAddPlayerCommandToSlot(slotIndex, ignoreNetworkViewedSlotLock))
        {
            ShowCombinedSlotCapacityWarning();
            return false;
        }

        bool added = slot.AddCommand(command);

        if (!added)
        {
            ShowBattleWarning("?ㅽ궗???덉빟?????놁뒿?덈떎.");
            Debug.LogWarning("[BattleTimelineController] ?덉빟 ?щ’??媛??李쇱뒿?덈떎.");
            return false;
        }

        BattleEquipmentEffectService.TryApplyAndConsumeSpiderWebMoveCostPenalty(command);
        RecordPlayerReservation(slotIndex, command);

        RecalculateAllReservedCosts();

        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();
        selectedSkill = null;

        return true;
    }

    public bool AddPlayerCommandFromNetworkSnapshot(int slotIndex, PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        if (reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null ||
            !slot.CanAcceptCharacter(command.UserRuntime) ||
            !slot.CanAddCommand())
        {
            return false;
        }

        PrepareCommandForReservation(slotIndex, command);

        if (!slot.AddCommand(command))
            return false;

        RecordPlayerReservation(slotIndex, command);
        return true;
    }

    public void FinalizeNetworkSnapshotReservations()
    {
        RecalculateAllReservedCosts();
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();
        selectedSkill = null;
    }

    private string GetEquipmentReservationBlockReason(
        PlayerReservedCommand command,
        int slotIndex)
    {
        CharacterRuntimeData runtime = command?.UserRuntime;

        if (runtime == null)
            return string.Empty;

        if (BattleEquipmentEffectService.IsSlotBlockedByEquipment(runtime, slotIndex))
            return "이 슬롯에는 스킬을 등록할 수 없습니다.";

        int maxSlotCount = BattleEquipmentEffectService.GetMaxRegistrableSlotCount(runtime);

        if (maxSlotCount == int.MaxValue)
            return string.Empty;

        int occupiedSlotCount = CountPlayerOccupiedSlots(runtime.CharacterId, command);
        bool targetSlotAlreadyOccupied = HasPlayerCommandInSlot(runtime.CharacterId, slotIndex, command);

        if (!targetSlotAlreadyOccupied)
            occupiedSlotCount++;

        return occupiedSlotCount > maxSlotCount
            ? $"스킬을 등록할 수 있는 슬롯은 {maxSlotCount}개까지입니다."
            : string.Empty;
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
        RecordPlayerReservation(GetSlotIndexOf(slot), existingMoveCommand);

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
            ShowBattleWarning("?덉빟???ㅽ궗 ?뺣낫媛 ?놁뒿?덈떎.");
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
            {
                RemoveReservedCosts(removedCommand);
                RemovePlayerReservationHistoryEntries(removedCommand);
            }
        }

        reservationVersion++;
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();
    }

    private void RecordPlayerReservation(int slotIndex, PlayerReservedCommand command)
    {
        if (command == null)
            return;

        if (slotIndex < 0)
            return;

        RemovePlayerReservationHistoryEntries(command);
        playerReservationHistory.Add(new PlayerReservationHistoryEntry(slotIndex, command));
    }

    private int GetSlotIndexOf(ReserveTurnSlotUI slot)
    {
        if (slot == null || reserveSlots == null)
            return -1;

        for (int i = 0; i < reserveSlots.Length; i++)
        {
            if (reserveSlots[i] == slot)
                return i;
        }

        return -1;
    }

    private void PruneInvalidPlayerReservationHistory()
    {
        for (int i = playerReservationHistory.Count - 1; i >= 0; i--)
        {
            if (!IsPlayerReservationEntryValid(playerReservationHistory[i]))
                playerReservationHistory.RemoveAt(i);
        }
    }

    private bool IsPlayerReservationEntryValid(PlayerReservationHistoryEntry entry)
    {
        if (entry == null || entry.Command == null)
            return false;

        if (reserveSlots == null || entry.SlotIndex < 0 || entry.SlotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[entry.SlotIndex];

        if (slot == null || slot.Commands == null)
            return false;

        for (int i = 0; i < slot.Commands.Count; i++)
        {
            if (slot.Commands[i] == entry.Command)
                return true;
        }

        return false;
    }

    private void RemovePlayerReservationHistoryEntries(PlayerReservedCommand command)
    {
        if (command == null)
            return;

        for (int i = playerReservationHistory.Count - 1; i >= 0; i--)
        {
            if (playerReservationHistory[i] != null && playerReservationHistory[i].Command == command)
                playerReservationHistory.RemoveAt(i);
        }
    }

    private void RemovePlayerReservationHistoryEntriesInSlot(int slotIndex)
    {
        for (int i = playerReservationHistory.Count - 1; i >= 0; i--)
        {
            if (playerReservationHistory[i] != null && playerReservationHistory[i].SlotIndex == slotIndex)
                playerReservationHistory.RemoveAt(i);
        }
    }

    private bool RemovePlayerReservationEntry(PlayerReservationHistoryEntry entry, bool showLog)
    {
        if (!IsPlayerReservationEntryValid(entry))
            return false;

        ReserveTurnSlotUI slot = reserveSlots[entry.SlotIndex];

        for (int i = slot.Commands.Count - 1; i >= 0; i--)
        {
            if (slot.Commands[i] != entry.Command)
                continue;

            bool removed = slot.RemoveCommandAt(i, out PlayerReservedCommand removedCommand);

            if (!removed)
                return false;

            RemoveReservedCosts(removedCommand);
            RemovePlayerReservationHistoryEntries(removedCommand);

            if (IsMoveCommand(removedCommand))
                RemoveFollowingMoveCommands(slot, i, removedCommand.CharacterId);

            RecalculateAllReservedCosts();
            RefreshReservationSimulation();
            RefreshTimeline();
            RefreshPlayerHUDs();
            RefreshMoveGhostPreview();

            if (showLog)
                Debug.Log($"[BattleTimelineController] 留덉?留??덉빟 ?섎룎由?/ Slot:{entry.SlotIndex} / Order:{i}");

            return true;
        }

        return false;
    }

    public int GetRemainingPlayerCommandCapacity(int slotIndex)
    {
        if (reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return 0;

        if (IsPlayerSlotLocked(slotIndex))
            return 0;

        if (reserveSlots[slotIndex] == null)
            return 0;

        return GetRemainingCombinedCommandCapacity(slotIndex);
    }

    private bool CanAddPlayerCommandToSlot(
        int slotIndex,
        bool ignoreNetworkViewedSlotLock = false)
    {
        if (reserveSlots == null)
            return false;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        if (IsPlayerSlotLocked(slotIndex, ignoreNetworkViewedSlotLock))
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
        ShowBattleWarning("???щ’?먮뒗 紐ъ뒪???됰룞怨?罹먮┃???됰룞???⑹퀜 理쒕? 5媛쒕쭔 ?덉빟?????덉뒿?덈떎.");
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

    public int CountPlayerOccupiedSlots(string characterId)
    {
        return CountPlayerOccupiedSlots(characterId, null);
    }

    public int CountPlayerEmptySlots(string characterId)
    {
        if (reserveSlots == null || reserveSlots.Length <= 0)
            return 0;

        return Mathf.Max(0, reserveSlots.Length - CountPlayerOccupiedSlots(characterId));
    }

    public int GetPlayerEmptySlotMask(string characterId)
    {
        if (reserveSlots == null || reserveSlots.Length <= 0)
            return 0;

        int mask = 0;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            if (!HasPlayerCommandInSlot(characterId, slotIndex, null))
                mask |= 1 << slotIndex;
        }

        return mask;
    }

    public int CountPlayerAttackSkillCommands(string characterId)
    {
        if (reserveSlots == null || reserveSlots.Length <= 0)
            return 0;

        int count = 0;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int i = 0; i < slot.Commands.Count; i++)
            {
                PlayerReservedCommand command = slot.Commands[i];

                if (!IsPlayerCommandForCharacter(command, characterId, null))
                    continue;

                if (command.SkillData != null && command.SkillData.SkillType == SkillType.Attack)
                    count++;
            }
        }

        return count;
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
        bool isFirstSkillInSlot = !HasEarlierCommandInSlot(
            command.UserRuntime,
            slotIndex,
            command);
        bool hadEarlierMoveInSlot = HasEarlierMoveCommandInSlot(
            command.UserRuntime,
            slotIndex,
            command);
        int sameSlotMoveCostBeforeCommand = GetEarlierMoveCostInSlot(
            command.UserRuntime,
            slotIndex,
            command);

        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            command,
            slotIndex,
            isFirstMoveCommand,
            isLastTimelineSlot,
            isFirstSkillInSlot,
            hadEarlierMoveInSlot,
            sameSlotMoveCostBeforeCommand);
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

    private bool HasEarlierCommandInSlot(
        CharacterRuntimeData runtime,
        int slotIndex,
        PlayerReservedCommand currentCommand)
    {
        return FindEarlierCommandInSlot(runtime, slotIndex, currentCommand, _ => true) != null;
    }

    private bool HasEarlierMoveCommandInSlot(
        CharacterRuntimeData runtime,
        int slotIndex,
        PlayerReservedCommand currentCommand)
    {
        return FindEarlierCommandInSlot(
            runtime,
            slotIndex,
            currentCommand,
            BattleEquipmentEffectService.IsMoveCommand) != null;
    }

    private int GetEarlierMoveCostInSlot(
        CharacterRuntimeData runtime,
        int slotIndex,
        PlayerReservedCommand currentCommand)
    {
        if (runtime == null || reserveSlots == null || slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return 0;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null || slot.Commands == null)
            return 0;

        int totalCost = 0;

        for (int i = 0; i < slot.Commands.Count; i++)
        {
            PlayerReservedCommand candidate = slot.Commands[i];

            if (candidate == currentCommand)
                break;

            if (!IsSameRuntimeCommand(runtime, candidate))
                continue;

            if (!BattleEquipmentEffectService.IsMoveCommand(candidate))
                continue;

            totalCost += Mathf.Max(0, candidate.Cost);
        }

        return totalCost;
    }

    private PlayerReservedCommand FindEarlierCommandInSlot(
        CharacterRuntimeData runtime,
        int slotIndex,
        PlayerReservedCommand currentCommand,
        System.Predicate<PlayerReservedCommand> predicate)
    {
        if (runtime == null ||
            reserveSlots == null ||
            slotIndex < 0 ||
            slotIndex >= reserveSlots.Length)
        {
            return null;
        }

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null || slot.Commands == null)
            return null;

        for (int i = 0; i < slot.Commands.Count; i++)
        {
            PlayerReservedCommand candidate = slot.Commands[i];

            if (candidate == currentCommand)
                return null;

            if (!IsSameRuntimeCommand(runtime, candidate))
                continue;

            if (predicate == null || predicate(candidate))
                return candidate;
        }

        return null;
    }

    private static bool IsSameRuntimeCommand(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command)
    {
        if (runtime == null || command == null || command.UserRuntime == null)
            return false;

        return command.UserRuntime.CharacterId == runtime.CharacterId;
    }

    private int CountPlayerOccupiedSlots(
        string characterId,
        PlayerReservedCommand ignoreCommand)
    {
        if (reserveSlots == null || reserveSlots.Length <= 0)
            return 0;

        int count = 0;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            if (HasPlayerCommandInSlot(characterId, slotIndex, ignoreCommand))
                count++;
        }

        return count;
    }

    private bool HasPlayerCommandInSlot(
        string characterId,
        int slotIndex,
        PlayerReservedCommand ignoreCommand)
    {
        if (reserveSlots == null ||
            slotIndex < 0 ||
            slotIndex >= reserveSlots.Length ||
            string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null || slot.Commands == null)
            return false;

        for (int i = 0; i < slot.Commands.Count; i++)
        {
            if (IsPlayerCommandForCharacter(slot.Commands[i], characterId, ignoreCommand))
                return true;
        }

        return false;
    }

    private static bool IsPlayerCommandForCharacter(
        PlayerReservedCommand command,
        string characterId,
        PlayerReservedCommand ignoreCommand)
    {
        if (command == null || command == ignoreCommand || command.UserRuntime == null)
            return false;

        return command.UserRuntime.CharacterId == characterId;
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
                bool isFirstSkillInSlot = !HasEarlierCommandInSlot(
                    command.UserRuntime,
                    slotIndex,
                    command);
                bool hadEarlierMoveInSlot = HasEarlierMoveCommandInSlot(
                    command.UserRuntime,
                    slotIndex,
                    command);
                int sameSlotMoveCostBeforeCommand = GetEarlierMoveCostInSlot(
                    command.UserRuntime,
                    slotIndex,
                    command);

                BattleEquipmentEffectService.ApplyReservationCostModifiers(
                    command,
                    slotIndex,
                    isFirstMoveCommand,
                    isLastTimelineSlot,
                    isFirstSkillInSlot,
                    hadEarlierMoveInSlot,
                    sameSlotMoveCostBeforeCommand);

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
            return "?덉빟???ㅽ궗 ?뺣낫媛 ?놁뒿?덈떎.";

        if (command.UserRuntime == null)
            return "?좏깮??罹먮┃?곌? ?놁뒿?덈떎.";

        CharacterRuntimeData runtime = command.UserRuntime;

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
        return $"{label}가 부족합니다. 필요:{required} / 보유:{safeAvailable}";
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
                return "怨좎쑀?먯썝";

            default:
                return "?먯썝";
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
                $"[BattleTimelineController] 紐ъ뒪???됰룞???ｌ쓣 ?щ’???놁뒿?덈떎. " +
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
        if (SteamBattleStateSynchronizer.TryHandleRemoveCommand(
                this,
                slotIndex,
                orderIndex,
                out _))
        {
            return;
        }

        RemoveCommandFromNetwork(slotIndex, orderIndex);
    }

    public bool RemoveCommandFromNetwork(int slotIndex, int orderIndex)
    {
        if (reserveSlots == null)
            return false;

        if (slotIndex < 0 || slotIndex >= reserveSlots.Length)
            return false;

        ReserveTurnSlotUI slot = reserveSlots[slotIndex];

        if (slot == null)
            return false;

        bool removed = slot.RemoveCommandAt(orderIndex, out PlayerReservedCommand removedCommand);

        if (!removed)
            return false;

        RemoveReservedCosts(removedCommand);
        RemovePlayerReservationHistoryEntries(removedCommand);

        if (IsMoveCommand(removedCommand))
            RemoveFollowingMoveCommands(slot, orderIndex, removedCommand.CharacterId);


        RecalculateAllReservedCosts();
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        Debug.Log($"[BattleTimelineController] ?덉빟 痍⑥냼 / Slot:{slotIndex} / Order:{orderIndex}");
        return true;
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
            {
                RemoveReservedCosts(removedCommand);
                RemovePlayerReservationHistoryEntries(removedCommand);
            }
        }
    }

    public void ClearAllReservations()
    {
        ClearSelectedSlotSelection();
        ClearPlayerLockedSlot();
        playerReservationHistory.Clear();
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
        BattleWarningUI.ShowMessage(NormalizeBattleWarningMessage(message));
    }

    private string NormalizeBattleWarningMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "현재 상태에서는 예약할 수 없습니다.";

        if (!LooksLikeBrokenKorean(message))
            return message;

        if (message.Contains("HP"))
            return "HP가 부족합니다.";

        if (message.Contains("Cost"))
            return "Cost가 부족합니다.";

        return "현재 상태에서는 예약할 수 없습니다.";
    }

    private bool LooksLikeBrokenKorean(string message)
    {
        return message.Contains("??") ||
               message.Contains("袁") ||
               message.Contains("筌") ||
               message.Contains("揶") ||
               message.Contains("醫") ||
               message.Contains("癒") ||
               message.Contains("됰") ||
               message.Contains("덈") ||
               message.Contains("뼄");
    }

    private void RefreshTimeline()
    {
        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        BattleTimelineBarUI standbyBar = GetStandbyTimelineBarUI();

        if (activeBar != null)
        {
            activeBar.Refresh(reserveSlots, monsterCommandsBySlot);
            activeBar.SetPlayerLockedSlot(playerLockedSlotIndex);
        }
        else
        {
            ShowBattleWarning("??꾨씪??UI瑜?李얠쓣 ???놁뒿?덈떎.");
            Debug.LogWarning("[BattleTimelineController] active timelineBarUI媛 ?놁뒿?덈떎.");
        }

        if (standbyBar != null && standbyBar != activeBar)
        {
            standbyBar.Clear();
            standbyBar.SetPlayerLockedSlot(-1);
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
    private sealed class PlayerReservationHistoryEntry
    {
        public PlayerReservationHistoryEntry(int slotIndex, PlayerReservedCommand command)
        {
            SlotIndex = slotIndex;
            Command = command;
        }

        public int SlotIndex { get; }
        public PlayerReservedCommand Command { get; }
    }

}

public readonly struct TimelineAutoSlotState
{
    public TimelineAutoSlotState(
        bool exists,
        bool isEmpty,
        bool canAcceptCharacter,
        bool canAddCommand,
        bool hasSelectedCharacterCommand)
    {
        Exists = exists;
        IsEmpty = isEmpty;
        CanAcceptCharacter = canAcceptCharacter;
        CanAddCommand = canAddCommand;
        HasSelectedCharacterCommand = hasSelectedCharacterCommand;
    }

    public bool Exists { get; }
    public bool IsEmpty { get; }
    public bool CanAcceptCharacter { get; }
    public bool CanAddCommand { get; }
    public bool HasSelectedCharacterCommand { get; }

    public bool CanUseAsEmptySlot =>
        Exists && IsEmpty && CanAcceptCharacter && CanAddCommand;

    public bool CanUseAsSelectedCharacterSlot =>
        Exists && !IsEmpty && CanAcceptCharacter && CanAddCommand && HasSelectedCharacterCommand;
}

public static class TimelineAutoSlotSelectionUtility
{
    public static int FindBestSlot(IReadOnlyList<TimelineAutoSlotState> slots, int currentSlotIndex)
    {
        if (slots == null || slots.Count <= 0)
            return -1;

        int safeCurrentSlotIndex = currentSlotIndex >= 0 && currentSlotIndex < slots.Count
            ? currentSlotIndex
            : -1;

        int beforeCurrent = FindFirstEmptySlot(slots, 0, safeCurrentSlotIndex - 1);
        if (beforeCurrent >= 0)
            return beforeCurrent;

        if (safeCurrentSlotIndex >= 0 && slots[safeCurrentSlotIndex].CanUseAsEmptySlot)
            return safeCurrentSlotIndex;

        int afterCurrentStart = safeCurrentSlotIndex >= 0
            ? safeCurrentSlotIndex + 1
            : 0;
        int afterCurrent = FindFirstEmptySlot(slots, afterCurrentStart, slots.Count - 1);
        if (afterCurrent >= 0)
            return afterCurrent;

        return FindFirstSelectedCharacterSlot(slots);
    }

    private static int FindFirstEmptySlot(
        IReadOnlyList<TimelineAutoSlotState> slots,
        int startIndex,
        int endIndex)
    {
        if (slots == null || slots.Count <= 0)
            return -1;

        int safeStartIndex = Mathf.Clamp(startIndex, 0, slots.Count - 1);
        int safeEndIndex = Mathf.Clamp(endIndex, -1, slots.Count - 1);

        if (safeEndIndex < safeStartIndex)
            return -1;

        for (int i = safeStartIndex; i <= safeEndIndex; i++)
        {
            if (slots[i].CanUseAsEmptySlot)
                return i;
        }

        return -1;
    }

    private static int FindFirstSelectedCharacterSlot(IReadOnlyList<TimelineAutoSlotState> slots)
    {
        if (slots == null)
            return -1;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].CanUseAsSelectedCharacterSlot)
                return i;
        }

        return -1;
    }
}
