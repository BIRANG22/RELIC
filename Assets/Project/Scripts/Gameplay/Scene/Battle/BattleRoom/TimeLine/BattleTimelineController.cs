using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTimelineController : MonoBehaviour
{
    private const int MaxMonsterCommandsPerSlot = 2;
    public static event System.Action<CharacterRuntimeData> CharacterSelectionChanged;

    [Header("Timeline")]
    [Tooltip("전투 구조 호환용 TimelineBar입니다. 비어 있으면 TimelineBar1을 사용합니다.")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI;
    [Tooltip("홀수 턴의 예약 표시를 담당하는 TimelineBar입니다.")]
    [SerializeField] private BattleTimelineBarUI timelineBarUI1;
    [Tooltip("짝수 턴의 예약 표시를 담당하는 TimelineBar입니다.")]
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
    [SerializeField, SoundId(SoundCategory.Sfx)] private string selectedSlotEffectSfxId = AudioIds.Sfx.BattleTimelineSlotRotate;
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
    [SerializeField] private bool enableKeyboardSlotMoveInput = true;
    [SerializeField] private BattleTurnExecutor turnExecutor;

    [Header("Timeline Bar Slide")]
    [SerializeField] private bool playTimelineSlotSlide = true;
    [Tooltip("전투 구조 호환용 이동 대상입니다. 비어 있으면 TimelineBar1 이동 대상을 사용합니다.")]
    [SerializeField] private RectTransform timelineBarSlideTarget;
    [Tooltip("홀수 턴에 사용하는 TimelineBar1 이동 대상입니다.")]
    [SerializeField] private RectTransform timelineBarSlideTarget1;
    [Tooltip("짝수 턴에 사용하는 TimelineBar2 이동 대상입니다.")]
    [SerializeField] private RectTransform timelineBarSlideTarget2;
    [Tooltip("대기 중인 TimelineBar를 현재 TimelineBar 오른쪽에 배치할 X 거리입니다.")]
    [SerializeField] private float standbyTimelineBarOffsetX = 1420f;
    [Tooltip("5개 슬롯이 모두 진행된 뒤 현재 TimelineBar가 도착하는 절대 X 위치입니다.")]
    [SerializeField] private float completedTurnTimelineBarPositionX = -1870f;
    [SerializeField] private float timelineSlotSlideDuration = 0.18f;
    [Tooltip("TurnMark와 Use_skill의 프레임 가림 애니메이션이 보이도록 가림 연출과 함께 이동할 때 사용하는 최소 시간입니다.")]
    [SerializeField] private float grindTimelineSlideDuration = 0.32f;
    [SerializeField] private bool useUnscaledTimeForTimelineSlotSlide = false;

    [Header("Timeline Sprite Grind Animation")]
    [SerializeField] private BattleTimelineSpriteAnimationController timelineSpriteAnimationController;
    [SerializeField] private bool autoFindTimelineSpriteAnimationController = true;

    [Header("Timeline Grind Positions")]
    [Tooltip("슬롯별 TurnMark / Order01~05 갈림 위치는 현재 타임라인 디자인의 절대 X 좌표를 사용합니다.")]
    [SerializeField] private float timelineBarStartPositionX = -240f;

    private static readonly float[][] OrderGrindPositions =
    {
        new[] { -380f, -430f, -480f, -530f, -580f },
        new[] { -710f, -760f, -810f, -860f, -910f },
        new[] { -1030f, -1080f, -1130f, -1180f, -1230f },
        new[] { -1350f, -1400f, -1450f, -1500f, -1500f },
        new[] { -1670f, -1720f, -1770f, -1820f, -1870f }
    };

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

    [Header("End Button Hover SFX")]
    [SerializeField] private bool playEndButtonHoverSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string endButtonHoverSfxId = AudioIds.Sfx.BattleEndButtonHover;
    [SerializeField, Range(0f, 1f)] private float endButtonHoverSfxVolume = 1f;

    [Header("Timeline Slot Slide SFX")]
    [SerializeField] private bool playTimelineSlotSlideSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string timelineSlotSlideSfxId = AudioIds.Sfx.BattleTimelineSlotSlide;
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
        // Unity 스크립트 기본값이 바뀌어도 이미 프리팹에 저장된 Inspector 값을 유지합니다.
        // 이전 수정본의 1335 / -1440 값을 현재 구조의 기준값인 1420 / -1420으로 자동 보정합니다.
        if (Mathf.Approximately(standbyTimelineBarOffsetX, 1335f) || standbyTimelineBarOffsetX <= 0f)
            standbyTimelineBarOffsetX = 1420f;

        // 현재 TimelineBar의 마지막 위치는 5슬롯 Order05의 절대 X = -1870입니다.
        // 이전 버전에서 저장된 완료 위치 값은 새 구조에 맞게 자동 보정합니다.
        if (!Mathf.Approximately(completedTurnTimelineBarPositionX, -1870f))
            completedTurnTimelineBarPositionX = -1870f;

        if (!Mathf.Approximately(timelineBarStartPositionX, -240f))
            timelineBarStartPositionX = -240f;

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
        AutoFindTotalUsedCostTextIfNeeded();
        BindEndButtonHoverRotationEventsIfNeeded();

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);

        RefreshSelectedSlotValueText();
        RefreshTotalUsedCostText();

        InitTimelineBars();
        AutoBindReserveSlotsFromTimelineBarIfNeeded();

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
        HandleCharacterSelectionOutsideGridClick();
    }

    private void HandleCharacterSelectionOutsideGridClick()
    {
        bool hasCharacterSelection = selectedCharacter != null;
        bool hasMonsterSelection = Relic.Gameplay.Monster.MonsterUnit.CurrentInfoSelectedMonster != null;

        if (!hasCharacterSelection && !hasMonsterSelection)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (IsPointerInsideBattleGrid(Input.mousePosition))
            return;

        if (IsPointerOverBattleWorldTarget(Input.mousePosition))
            return;

        if (hasCharacterSelection)
            ClearCharacterSelection();

        if (hasMonsterSelection)
            Relic.Gameplay.Monster.MonsterUnit.ClearMonsterInfoSelection();
    }

    private bool IsPointerOverBattleWorldTarget(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        float maxDistance = camera.farClipPlane;

        RaycastHit[] hits3D = Physics.RaycastAll(ray, maxDistance);
        for (int i = 0; i < hits3D.Length; i++)
        {
            Transform hitTransform = hits3D[i].transform;
            if (IsSelectionPreservingWorldTarget(hitTransform))
                return true;
        }

        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray, maxDistance);
        for (int i = 0; i < hits2D.Length; i++)
        {
            Transform hitTransform = hits2D[i].transform;
            if (IsSelectionPreservingWorldTarget(hitTransform))
                return true;
        }

        return false;
    }

    private static bool IsSelectionPreservingWorldTarget(Transform target)
    {
        if (target == null)
            return false;

        if (target.GetComponentInParent<BattleCharacter>() != null)
            return true;

        if (target.GetComponentInParent<MonsterUnit>() != null)
            return true;

        if (target.GetComponentInParent<GridCell>() != null)
            return true;

        return false;
    }

    private bool IsPointerInsideBattleGrid(Vector2 screenPosition)
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);

        if (gridManager == null)
            return false;

        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        float maxDistance = camera.farClipPlane;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell == null || !cell.gameObject.activeInHierarchy)
                    continue;

                Collider[] colliders = cell.GetComponentsInChildren<Collider>(false);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider cellCollider = colliders[i];
                    if (cellCollider == null || !cellCollider.enabled)
                        continue;

                    if (cellCollider.Raycast(ray, out _, maxDistance))
                        return true;
                }

                Collider2D[] colliders2D = cell.GetComponentsInChildren<Collider2D>(false);
                for (int i = 0; i < colliders2D.Length; i++)
                {
                    Collider2D cellCollider = colliders2D[i];
                    if (cellCollider == null || !cellCollider.enabled)
                        continue;

                    Plane cellPlane = new Plane(
                        cellCollider.transform.forward,
                        cellCollider.transform.position);

                    if (!cellPlane.Raycast(ray, out float enter))
                        continue;

                    Vector3 worldPoint = ray.GetPoint(enter);
                    if (cellCollider.OverlapPoint(worldPoint))
                        return true;
                }
            }
        }

        return false;
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
            ShowBattleWarning("되돌릴 예약이 없습니다.");
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
        if (runtimeData != null)
            Relic.Gameplay.Monster.MonsterUnit.ClearMonsterInfoSelection();

        bool isChangingCharacter =
            runtimeData != null &&
            (selectedCharacter == null ||
             selectedCharacter.CharacterId != runtimeData.CharacterId);

        bool selectionChanged =
            selectedCharacter != runtimeData ||
            (selectedCharacter != null && runtimeData != null &&
             selectedCharacter.CharacterId != runtimeData.CharacterId);

        selectedCharacter = runtimeData;

        if (isChangingCharacter)
            TryAutoSelectSlotForCharacter(runtimeData);

        ApplySelectedCharacterScaleFeedback(runtimeData);
        TryFocusCameraOnSelectedCharacter(runtimeData);

        if (selectionChanged)
            CharacterSelectionChanged?.Invoke(selectedCharacter);
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

        ClearCharacterSelection();
    }

    public void ClearCharacterSelection()
    {
        if (selectedCharacter == null && selectedSkill == null)
            return;

        CharacterRuntimeData previousCharacter = selectedCharacter;

        if (previousCharacter != null)
            CancelSkillReservationPreviewFromSkillList(previousCharacter);

        selectedCharacter = null;
        selectedSkill = null;
        lastCameraFocusedCharacterId = null;
        ApplySelectedCharacterScaleFeedbackById(null);

        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
            cameraController.StartReturnDefault();

        CharacterSelectionChanged?.Invoke(null);
    }

    public static void ClearCurrentCharacterSelection()
    {
        BattleTimelineController controller = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Exclude);
        controller?.ClearCharacterSelection();
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
        TryFocusCameraOnSelectedCharacter(runtimeData, false, false);
    }

    public void RefocusCurrentSelectedCharacterWhenInputReady()
    {
        TryFocusCameraOnSelectedCharacter(selectedCharacter, true, false);
    }

    public void RefocusCurrentSelectedCharacterForPanelRaise()
    {
        TryFocusCameraOnSelectedCharacter(selectedCharacter, true, true);
    }

    private void TryFocusCameraOnSelectedCharacter(
        CharacterRuntimeData runtimeData,
        bool forceRefocus,
        bool ignoreInputReady)
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

        if (focusCameraOnlyWhenInputReady && !ignoreInputReady)
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
        return SelectTimelineSlotFromNetwork(slotIndex, tryStartReservation, true);
    }

    public bool SelectTimelineSlotFromNetwork(
        int slotIndex,
        bool tryStartReservation,
        bool playSelectionEffect)
    {
        return SetActiveTimelineSlot(slotIndex, tryStartReservation, playSelectionEffect);
    }

    private bool SetActiveTimelineSlot(int slotIndex, bool tryStartReservation)
    {
        return SetActiveTimelineSlot(slotIndex, tryStartReservation, true);
    }

    private bool SetActiveTimelineSlot(
        int slotIndex,
        bool tryStartReservation,
        bool playSelectionEffect)
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

        if (playSelectionEffect)
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
    public void SelectDefaultSlotWhenInputReady(bool playSelectionEffect = true)
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

        if (playSelectionEffect)
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

        // 5번 슬롯의 TurnMark가 가려진 뒤 라인을 바로 완료 위치로 보내면
        // 5번 슬롯에 등록된 Use_skill들이 개별적으로 가려지기 전에 한 번에 이동해 보입니다.
        // 완료 위치 보정은 BattleTurnExecutor가 모든 슬롯과 연출 처리를 끝낸 뒤 호출합니다.
    }

    public IEnumerator PlayTimelineTurnMarkAnimationRoutine(int slotIndex)
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        ConfigureTimelineSpriteAnimationRootForActiveBar();

        if (timelineSpriteAnimationController == null)
            yield break;

        // TurnMark 프레임만 재생합니다. 실제 라인 이동은 PlayTimelineTurnMarkAnimationAndLineSlideRoutine에서 함께 처리합니다.
        PlayTimelineSlideGearRotation(1);
        SpawnTimelineGrindVfx();
        yield return timelineSpriteAnimationController.PlayTurnMarkRoutine(slotIndex);
    }

    private IEnumerator PlayTimelineTurnMarkAnimationAndLineSlideRoutine(int slotIndex, bool isEmptySlot)
    {
        AutoFindTimelineSpriteAnimationControllerIfNeeded();
        ConfigureTimelineSpriteAnimationRootForActiveBar();

        float animationDuration = GetTurnMarkGrindDuration();

        // 행동이 없는 슬롯은 TurnMark / 빈 Order를 전부 건너뛰고 해당 슬롯의 Order05까지 한 번에 이동합니다.
        // 행동이 있는 슬롯은 먼저 해당 슬롯의 TurnMark 위치까지 이동한 뒤 TurnMark 갈림 연출을 보여줍니다.
        float targetX = isEmptySlot
            ? GetOrderGrindPositionX(slotIndex, 4)
            : GetTurnMarkGrindPositionX(slotIndex);

        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        if (activeBar != null)
            activeBar.HideOwnerIconsForSlot(slotIndex);

        Coroutine turnMarkAnimation = null;

        if (timelineSpriteAnimationController != null)
        {
            SpawnTimelineGrindVfx();
            turnMarkAnimation = StartCoroutine(
                timelineSpriteAnimationController.PlayTurnMarkRoutine(slotIndex)
            );
        }

        yield return MoveTimelineSlotToGrindPositionRoutine(
            slotIndex,
            targetX,
            animationDuration
        );

        if (turnMarkAnimation != null)
            yield return turnMarkAnimation;
    }

    public IEnumerator MoveTimelineBarsToCompletedTurnPositionRoutine()
    {
        if (completedTimelineBarPositionApplied)
            yield break;

        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();

        if (activeTarget == null)
            yield break;

        float completedX = completedTurnTimelineBarPositionX;
        float offsetX = completedX - activeTarget.anchoredPosition.x;

        // 완료 위치까지도 활성 TimelineBar만 이동합니다.
        // Standby TimelineBar는 다음 턴 전환 시점까지 화면 밖의 대기 위치를 유지합니다.
        bool alreadyAtOrPastCompletedPosition = activeTarget.anchoredPosition.x <= completedX + 0.01f;

        if (!alreadyAtOrPastCompletedPosition && !Mathf.Approximately(offsetX, 0f))
            yield return MoveAllTimelineSlotSlideTargetsByOffsetRoutine(offsetX, timelineSlotSlideDuration, true);

        activeTarget.anchoredPosition = new Vector2(completedX, activeTarget.anchoredPosition.y);

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

            float animationDuration = GetUseSkillGrindDuration();
            float targetX = GetOrderGrindPositionX(slotIndex, orderIndex);

            // 각 Order가 지정된 X 위치에 도달한 뒤 Use_skill 갈림 연출을 재생합니다.
            yield return MoveTimelineSlotToGrindPositionRoutine(
                slotIndex,
                targetX,
                animationDuration
            );

            if (timelineSpriteAnimationController != null)
            {
                SpawnTimelineGrindVfx();
                yield return timelineSpriteAnimationController.PlayUseSkillRoutine(slotIndex, orderIndex);
            }

            // 마지막 실제 행동 뒤에 빈 Order가 남아 있으면 중간 정지 지점을 건너뛰고
            // 슬롯 완료 위치인 Order05(-580)까지 한 번에 이동합니다.
            if (fillRemainingUseSkillLine && i == safeCount - 1 && orderIndex < 4)
            {
                yield return MoveTimelineSlotToGrindPositionRoutine(
                    slotIndex,
                    GetOrderGrindPositionX(slotIndex, 4),
                    animationDuration
                );
            }
        }
    }

    private static float GetTurnMarkGrindPositionX(int slotIndex)
    {
        switch (Mathf.Clamp(slotIndex, 0, 4))
        {
            case 0: return -330f;
            case 1: return -650f;
            case 2: return -980f;
            case 3: return -1300f;
            case 4: return -1620f;
            default: return -1620f;
        }
    }

    private static float GetOrderGrindPositionX(int slotIndex, int orderIndex)
    {
        int safeSlotIndex = Mathf.Clamp(slotIndex, 0, OrderGrindPositions.Length - 1);
        int safeOrderIndex = Mathf.Clamp(orderIndex, 0, 4);
        return OrderGrindPositions[safeSlotIndex][safeOrderIndex];
    }

    private IEnumerator MoveTimelineSlotToGrindPositionRoutine(
        int slotIndex,
        float targetBarX,
        float duration)
    {
        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();

        if (activeTarget == null)
            yield break;

        // 갈림 위치는 TimelineBar 자체의 절대 anchoredPosition X를 사용합니다.
        // 슬롯별 좌표를 간격 계산으로 추정하지 않고 지정된 위치표를 그대로 적용합니다.
        float offsetX = targetBarX - activeTarget.anchoredPosition.x;

        // 갈림 연출 중 TimelineBar가 오른쪽으로 되돌아가는 상황은 허용하지 않습니다.
        if (offsetX >= -0.01f)
            yield break;

        PlayTimelineSlideGearRotation(1, duration);
        yield return MoveAllTimelineSlotSlideTargetsByOffsetRoutine(offsetX, duration);
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

        PlayEndButtonHoverRotationTo(endButtonRotationBeforeHoverZ);
    }

    private void OnEndButtonHoverEnter()
    {
        if (!IsEndButtonHoverRotationAllowed())
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();

        if (endButtonHoverRotationTarget == null)
            return;

        if (isEndButtonHovering)
            return;

        isEndButtonHovering = true;
        endButtonRotationBeforeHoverZ = GetTransformRotationZ(endButtonHoverRotationTarget);

        float targetRotationZ = endButtonRotationBeforeHoverZ + endButtonHoverRotationOffsetZ;

        PlayEndButtonHoverSfx();
        PlayEndButtonHoverRotationTo(targetRotationZ);
    }

    private void OnEndButtonHoverExit()
    {
        if (!playEndButtonHoverRotation)
            return;

        AutoBindEndButtonHoverRotationTargetIfNeeded();

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
        PlayEndButtonHoverRotationTo(endButtonRotationBeforeHoverZ);
    }

    private bool IsPointerInsideEndButtonHoverBounds()
    {
        Vector2 screenPosition = Input.mousePosition;

        return IsScreenPositionInsideRectTransformBounds(endButtonHoverRotationTarget, screenPosition);
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

    private void PlayEndButtonHoverRotationTo(float targetRotationZ)
    {
        if (endButtonHoverRotationTarget == null)
            return;

        if (endButtonHoverRotationRoutine != null)
            StopCoroutine(endButtonHoverRotationRoutine);

        endButtonHoverRotationRoutine = StartCoroutine(
            RotateEndButtonHoverToRoutine(targetRotationZ)
        );
    }

    private IEnumerator RotateEndButtonHoverToRoutine(float targetRotationZ)
    {
        float duration = Mathf.Max(0.01f, endButtonHoverRotationDuration);
        float elapsed = 0f;
        float startRotationZ = GetTransformRotationZ(endButtonHoverRotationTarget);

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForEndButtonHoverRotation ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            SetTransformRotationZ(endButtonHoverRotationTarget, Mathf.LerpAngle(startRotationZ, targetRotationZ, easedT));
            yield return null;
        }

        SetTransformRotationZ(endButtonHoverRotationTarget, targetRotationZ);
        endButtonHoverRotationRoutine = null;
    }

    private void PlayEndButtonHoverSfx()
    {
        if (!playEndButtonHoverSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(endButtonHoverSfxId, endButtonHoverSfxVolume);
    }

    private void PlayTimelineSlotSlideSfx()
    {
        if (!playTimelineSlotSlideSfx)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(timelineSlotSlideSfxId, timelineSlotSlideSfxVolume);
    }

    private void PlayTimelineSlideGearRotation(int completedStepCount, float durationOverride = -1f)
    {
        if (completedStepCount <= 0)
            return;

        AutoFindSelectedSlotEffectIfNeeded();
        AutoFindSelectedSlotGearEffectsIfNeeded();
        AutoBindEndButtonHoverRotationTargetIfNeeded();

        bool hasTarget =
            selectedSlotEffect != null ||
            selectedSlotLargeGearEffect != null ||
            selectedSlotSmallGearEffect != null ||
            endButtonHoverRotationTarget != null;

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

        if (isEndButtonHovering)
            endButtonRotationBeforeHoverZ += endButtonHoverRotationOffsetZ * completedStepCount;

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
            durationOverride
        ));
    }

    private IEnumerator PlayTimelineSlideGearRotationRoutine(
        float mainTargetZ,
        float largeGearTargetZ,
        float smallGearTargetZ,
        float endButtonTargetZ,
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

        while (elapsed < duration)
        {
            elapsed += useUnscaledTimeForTimelineSlotSlide ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            SetTransformRotationZ(selectedSlotEffect, Mathf.LerpAngle(mainStartZ, mainTargetZ, easedT));
            SetTransformRotationZ(selectedSlotLargeGearEffect, Mathf.LerpAngle(largeGearStartZ, largeGearTargetZ, easedT));
            SetTransformRotationZ(selectedSlotSmallGearEffect, Mathf.LerpAngle(smallGearStartZ, smallGearTargetZ, easedT));
            SetTransformRotationZ(endButtonHoverRotationTarget, Mathf.LerpAngle(endButtonStartZ, endButtonTargetZ, easedT));

            yield return null;
        }

        SetTransformRotationZ(selectedSlotEffect, mainTargetZ);
        SetTransformRotationZ(selectedSlotLargeGearEffect, largeGearTargetZ);
        SetTransformRotationZ(selectedSlotSmallGearEffect, smallGearTargetZ);
        SetTransformRotationZ(endButtonHoverRotationTarget, endButtonTargetZ);
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


    private void AutoBindReserveSlotsFromTimelineBarIfNeeded()
    {
        bool needsRebind = reserveSlots == null || reserveSlots.Length != 5;

        if (!needsRebind)
        {
            for (int i = 0; i < reserveSlots.Length; i++)
            {
                if (reserveSlots[i] == null)
                {
                    needsRebind = true;
                    break;
                }
            }
        }

        if (!needsRebind)
            return;

        AutoFindTimelineBarsIfNeeded();

        BattleTimelineBarUI sourceBar = timelineBarUI1 != null ? timelineBarUI1 : timelineBarUI2;
        if (sourceBar == null)
            return;

        ReserveTurnSlotUI[] resolvedSlots = sourceBar.GetOrCreateReserveSlots(this);
        if (resolvedSlots == null || resolvedSlots.Length == 0)
            return;

        reserveSlots = resolvedSlots;
        InitializeMonsterCommandSlots();
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

        // 진행 연출 중에는 현재 활성 TimelineBar만 이동합니다.
        // 대기 Bar까지 같이 움직이면 턴 종료 직전에 화면을 가로질러
        // 휙 지나가는 것처럼 보일 수 있습니다.
        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();
        return activeTarget != null ? new[] { activeTarget } : System.Array.Empty<RectTransform>();
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

        // 두 TimelineBar의 실제 RectTransform 너비나 현재 배치값을 기준으로 간격을 다시 계산하지 않습니다.
        // Inspector에서 지정한 Standby Timeline Bar Offset X 값만 사용해야
        // 0 / 1420 위치를 번갈아 쓰는 구조가 흔들리지 않습니다.
        resolvedStandbyTimelineBarOffsetX = Mathf.Abs(standbyTimelineBarOffsetX);

        if (resolvedStandbyTimelineBarOffsetX <= 0.01f)
            resolvedStandbyTimelineBarOffsetX = 1420f;

        timelineBarOriginalPositionCaptured = true;
    }

    private Vector2 GetTimelineBarBasePosition()
    {
        Vector2 basePosition = timelineBarSlideTarget1 != null
            ? timelineBar1OriginalAnchoredPosition
            : timelineBar2OriginalAnchoredPosition;

        basePosition.x = timelineBarStartPositionX;
        return basePosition;
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

        // 체인은 턴마다 초기화하지 않고, 새 전투방 최초 진입에서만 시작 위치로 맞춥니다.
        ResetChainLoopForBar(timelineBarUI1);
        if (timelineBarUI2 != null && timelineBarUI2 != timelineBarUI1)
            ResetChainLoopForBar(timelineBarUI2);

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

        BattleTimelineBarUI activeBar = GetActiveTimelineBarUI();
        RectTransform activeTarget = GetActiveTimelineBarSlideTarget();
        RectTransform standbyTarget = GetStandbyTimelineBarSlideTarget();

        Vector3[] chainWorldPositions = null;
        ChainLoopScroller activeChain = GetChainLoopScroller(activeBar);
        if (activeChain != null)
            chainWorldPositions = activeChain.CaptureWorldPositions();

        if (swapActiveBar && activeBar != null)
        {
            // 현재 슬롯 세트는 갈림 연출의 마지막 프레임/숨김 상태가 남아 있을 수 있습니다.
            // 이 세트가 곧 다음 Next 슬롯으로 재활용되므로 승격/교체 전에 원래 비주얼로 복구합니다.
            ConfigureTimelineSpriteAnimationRootForActiveBar();
            if (timelineSpriteAnimationController != null)
                timelineSpriteAnimationController.ResetTimelineSpritesForNextTurn();

            ReserveTurnSlotUI[] promotedSlots = activeBar.PromoteTrailingTimelineGroupsToCurrent(this);
            if (promotedSlots != null && promotedSlots.Length > 0)
                reserveSlots = promotedSlots;
        }

        completedTimelineBarPositionApplied = false;

        Vector2 activeBasePosition = GetTimelineBarBasePosition();
        if (activeTarget != null)
            activeTarget.anchoredPosition = activeBasePosition;

        if (standbyTarget != null && standbyTarget != activeTarget)
            standbyTarget.anchoredPosition = activeBasePosition + new Vector2(resolvedStandbyTimelineBarOffsetX, 0f);

        if (chainWorldPositions != null && activeChain != null)
            activeChain.RestoreWorldPositions(chainWorldPositions);

        ConfigureTimelineSpriteAnimationRootForActiveBar();

        // Current/Next 슬롯의 이름과 역할이 교체되면 animationRoot 자체는 같은 TimelineBar이므로
        // SetAnimationRoot만 호출해서는 SpriteAnimationController의 기존 Image 참조가 갱신되지 않습니다.
        // 반드시 새 TimelineSlot01~05를 다시 탐색한 뒤 Current 쪽만 원본 프레임으로 복구해야 합니다.
        // 그렇지 않으면 이전 Current(현재 Next)의 TurnMark/Use_skill이 계속 애니메이션 대상으로 남아
        // Next TurnMark에 갈림 프레임이 남고 비어 있는 Order 루트까지 다시 활성화됩니다.
        if (swapActiveBar && timelineSpriteAnimationController != null)
        {
            timelineSpriteAnimationController.RefreshTargets();
            timelineSpriteAnimationController.ResetTimelineSpritesForNextTurn();
        }

        SetActiveTimelineSlotVisual(activeSlotIndex);

        if (activeBar != null)
        {
            activeBar.SetEmptyUseSkillSlotsVisible(true);
            activeBar.SetTurnMarkChildrenVisible(true);
        }

        BattleTimelineBarUI standbyBar = GetStandbyTimelineBarUI();
        if (standbyBar != null && standbyBar != activeBar)
        {
            standbyBar.SetActiveTimelineSlot(-1);
            standbyBar.SetTurnMarkChildrenVisible(false);
            standbyBar.SetEmptyUseSkillSlotsVisible(false);
        }
    }

    private static ChainLoopScroller GetChainLoopScroller(BattleTimelineBarUI barUI)
    {
        if (barUI == null)
            return null;

        ChainLoopScroller scroller = barUI.GetComponent<ChainLoopScroller>();
        if (scroller != null)
            return scroller;

        return barUI.GetComponentInChildren<ChainLoopScroller>(true);
    }

    private static void ResetChainLoopForBar(BattleTimelineBarUI barUI)
    {
        ChainLoopScroller scroller = GetChainLoopScroller(barUI);
        if (scroller != null)
            scroller.ResetPositions();
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

        // 두 개의 TimelineBar가 0 / 1420 위치를 번갈아 쓰는 구조에서
        // 진행 중인 Bar가 완료 위치에 도착할 때 추가 보정 이동이 들어가면 안 됩니다.
        // 따라서 모든 라인 이동 요청은 완료 위치를 넘지 않도록 항상 한 번 제한합니다.
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

        float completedX = completedTurnTimelineBarPositionX;
        float currentX = activeTarget.anchoredPosition.x;
        float targetX = currentX + offsetX;

        if (targetX < completedX)
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

        AudioManager.Instance.PlaySfx(selectedSlotEffectSfxId, selectedSlotEffectSfxVolume);
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

    public void CancelGridSelectionWhenHoveringDifferentSkill(
        CharacterRuntimeData runtimeData,
        SkillMasterData skillData)
    {
        if (playerSkillReservationController == null)
            playerSkillReservationController = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);

        playerSkillReservationController?.CancelSelectionWhenHoveringDifferentSkill(
            runtimeData,
            skillData
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
                ShowBattleWarning("타임라인 슬롯을 먼저 선택해 주세요.");

            return;
        }

        if (selectedCharacter == null && selectedSkill == null)
            return;

        if (selectedCharacter == null)
        {
            ShowBattleWarning("캐릭터를 먼저 선택해 주세요.");
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

        if (IsPlayerSlotLocked(slotIndex, ignoreNetworkViewedSlotLock))
        {
            ShowPlayerLockedSlotWarning();
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
            ShowBattleWarning("스킬을 예약할 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] 예약 슬롯이 가득 찼습니다.");
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
            return GameLocalization.Get("battle.skill_slot_blocked", "이 슬롯에는 스킬을 등록할 수 없습니다.");

        int maxSlotCount = BattleEquipmentEffectService.GetMaxRegistrableSlotCount(runtime);

        if (maxSlotCount == int.MaxValue)
            return string.Empty;

        int occupiedSlotCount = CountPlayerOccupiedSlots(runtime.CharacterId, command);
        bool targetSlotAlreadyOccupied = HasPlayerCommandInSlot(runtime.CharacterId, slotIndex, command);

        if (!targetSlotAlreadyOccupied)
            occupiedSlotCount++;

        return occupiedSlotCount > maxSlotCount
            ? GameLocalization.Format("battle.skill_slot_limit", "스킬을 등록할 수 있는 슬롯은 {0}개까지입니다.", maxSlotCount)
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
                Debug.Log($"[BattleTimelineController] 마지막 예약 되돌리기 / Slot:{entry.SlotIndex} / Order:{i}");

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
            return GameLocalization.Get("battle.no_skill_to_reserve", "예약할 스킬 정보가 없습니다.");

        if (command.UserRuntime == null)
            return GameLocalization.Get("battle.no_character_selected", "선택한 캐릭터가 없습니다.");

        CharacterRuntimeData runtime = command.UserRuntime;

        string shortageMessage = GetShortageMessage(runtime, command);
        if (!string.IsNullOrEmpty(shortageMessage))
            return shortageMessage;

        return string.Empty;
    }

    private string GetShortageMessage(CharacterRuntimeData runtime, PlayerReservedCommand command)
    {
        if (runtime == null || command == null)
            return GameLocalization.Get("battle.no_skill_to_reserve", "예약할 스킬 정보가 없습니다.");

        if (!runtime.CanReserveHP(command.HPCost))
            return BuildShortageMessage("생명력", command.HPCost, runtime.CurrentHP - runtime.ReservedHPCost);

        if (!runtime.CanReserveCost(command.Cost))
            return BuildShortageMessage("마나", command.Cost, runtime.CurrentCost - runtime.ReservedCost);

        if (!runtime.CanReserveResource(command.ResourceCost))
            return BuildShortageMessage("카르마", command.ResourceCost, runtime.CurrentResource - runtime.ReservedResourceCost);

        if (!runtime.CanReserveShield(command.ShieldCost))
            return BuildShortageMessage(GameLocalization.Get("common.armor", "방어도"), command.ShieldCost, runtime.CurrentShield - runtime.ReservedShieldCost);

        return string.Empty;
    }

    private string BuildShortageMessage(string label, int required, int available)
    {
        int safeAvailable = Mathf.Max(0, available);
        string particle = label == "생명력" ? "이" : "가";
        return $"{label}{particle} 부족합니다. 필요:{required} / 보유:{safeAvailable}";
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
                return GameLocalization.Get("resource.unique", "고유자원");

            default:
                return GameLocalization.Get("common.resource", "자원");
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

                if (TryGetCommandPreviewMoveGridIndex(command, out int previewMoveGridIndex))
                    gridIndex = previewMoveGridIndex;
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

        if (commands.Count >= MaxMonsterCommandsPerSlot)
            return false;

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


    public int CancelMoveAndAttackReservations(CharacterRuntimeData runtime)
    {
        if (runtime == null || reserveSlots == null)
            return 0;

        int removedCount = 0;

        for (int slotIndex = 0; slotIndex < reserveSlots.Length; slotIndex++)
        {
            ReserveTurnSlotUI slot = reserveSlots[slotIndex];

            if (slot == null || slot.Commands == null)
                continue;

            for (int commandIndex = slot.Commands.Count - 1; commandIndex >= 0; commandIndex--)
            {
                PlayerReservedCommand command = slot.Commands[commandIndex];

                if (command == null || command.UserRuntime != runtime)
                    continue;

                bool isMove = IsMoveCommand(command) ||
                              (command.SkillData != null &&
                               command.SkillData.TimelineNotation == TimelineActionType.Move);
                bool isAttack = command.SkillData != null &&
                                (command.SkillData.SkillType == SkillType.Attack ||
                                 command.SkillData.TimelineNotation == TimelineActionType.Attack);

                if (!isMove && !isAttack)
                    continue;

                if (!slot.RemoveCommandAt(commandIndex, out PlayerReservedCommand removedCommand))
                    continue;

                RemoveReservedCosts(removedCommand);
                RemovePlayerReservationHistoryEntries(removedCommand);
                removedCount++;
            }
        }

        if (removedCount <= 0)
            return 0;

        reservationVersion++;
        RecalculateAllReservedCosts();
        RefreshReservationSimulation();
        RefreshTimeline();
        RefreshPlayerHUDs();
        RefreshMoveGhostPreview();

        Debug.Log($"[BattleTimelineController] 위치 변경으로 이동/공격 예약 취소 / Character:{runtime.CharacterId} / Count:{removedCount}");
        return removedCount;
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

        Debug.Log($"[BattleTimelineController] 예약 취소 / Slot:{slotIndex} / Order:{orderIndex}");
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
            return GameLocalization.Get("battle.cannot_reserve_now", "현재 상태에서는 예약할 수 없습니다.");

        if (!LooksLikeBrokenKorean(message))
            return message;

        if (message.Contains("HP"))
            return GameLocalization.Get("battle.hp_shortage", "생명력이 부족합니다.");

        if (message.Contains("Cost"))
            return GameLocalization.Get("battle.cost_shortage", "마나가 부족합니다.");

        return GameLocalization.Get("battle.cannot_reserve_now", "현재 상태에서는 예약할 수 없습니다.");
    }

    private bool LooksLikeBrokenKorean(string message)
    {
        return message.Contains("??") ||
               message.Contains("\uFFFD") ||
               message.Contains("?");
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
            ShowBattleWarning("타임라인 UI를 찾을 수 없습니다.");
            Debug.LogWarning("[BattleTimelineController] active timelineBarUI가 없습니다.");
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

                if (TryGetCommandPreviewMoveGridIndex(command, out int previewMoveGridIndex))
                    gridIndex = previewMoveGridIndex;
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

        if (command.PreviewMoveGridIndex < 0)
            return false;

        bool hasReservedMove = command.ReservedMoveGridIndex >= 0;
        bool hasSkillMovePreview =
            command.HasSimulatedResult &&
            command.SimulatedMoveGridIndex >= 0 &&
            command.SimulatedMoveOffset != Vector2Int.zero;

        if (!hasReservedMove && !hasSkillMovePreview)
            return false;

        return command.UserRuntime.CharacterId == characterId;
    }

    private static bool TryGetCommandPreviewMoveGridIndex(
        PlayerReservedCommand command,
        out int gridIndex)
    {
        gridIndex = -1;

        if (command == null)
            return false;

        if (command.HasSimulatedResult && command.SimulatedMoveGridIndex >= 0)
        {
            gridIndex = command.SimulatedMoveGridIndex;
            return true;
        }

        if (command.ReservedMoveGridIndex < 0)
            return false;

        gridIndex = command.EffectiveMoveGridIndex;
        return gridIndex >= 0;
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
