using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using Relic.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerSkillReservationController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BattleGridEffectController gridEffectController;
    [SerializeField] private RangePreview rangePreview;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private MovePathPreview movePathPreview;
    [SerializeField] private BattleTimelineController timelineController;

    [Header("Skill List Panel")]
    [SerializeField] private SkillListPanel skillListPanel;
    [SerializeField] private bool keepSkillListOpenAfterReservationClick = true;
    [SerializeField] private int keepSkillListOpenIgnoreFrames = 1;

    [Header("Reservation SFX")]
    [Tooltip("스킬 또는 이동 행동이 타임라인에 정상 등록되었을 때 재생할 효과음입니다.")]
    [SerializeField] private string reservationConfirmSfxId = "SkillReserve";
    [Tooltip("행동 등록 효과음의 볼륨 배율입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float reservationConfirmSfxVolume = 1f;

    [Header("Range Highlight Material")]
    [Tooltip("이동 스킬의 선택 가능 그리드에만 사용하는 머테리얼입니다. 비워두면 기존 하이라이트 머테리얼을 사용합니다.")]
    [SerializeField] private Material moveHighlightMaterial;

    [Header("Move Hover Ping")]
    [Tooltip("이동 가능한 그리드에 마우스를 올렸을 때 표시할 월드 VFX 프리팹입니다.")]
    [SerializeField] private GameObject moveHoverPingVfxPrefab;
    [Tooltip("그리드 중심을 기준으로 VFX에 적용할 위치 오프셋입니다.")]
    [SerializeField] private Vector3 moveHoverPingVfxOffset = Vector3.zero;
    [Tooltip("VFX 프리팹에 적용할 크기 배율입니다.")]
    [Min(0f)]
    [SerializeField] private float moveHoverPingVfxScale = 1f;
    [Tooltip("VFX 렌더러의 Y 정렬 기준 오프셋입니다.")]
    [SerializeField] private int moveHoverPingVfxSortingOrder = 10;

    [Header("Move Hover Cost")]
    [Tooltip("이동 코스트 앞에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite moveHoverCostIcon;
    [Tooltip("코스트 아이콘 뒤에 표시할 배경 이미지입니다.")]
    [SerializeField] private Sprite moveHoverCostIconBackground;
    [Tooltip("숫자 0~9를 순서대로 지정합니다. 여러 자리 숫자도 지원합니다.")]
    [SerializeField] private Sprite[] moveHoverCostDigits = new Sprite[10];
    [Tooltip("그리드 중앙 기준 코스트 표시 전체 위치입니다.")]
    [SerializeField] private Vector3 moveHoverCostTextOffset = new Vector3(0f, 0.35f, 0f);
    [Tooltip("코스트 아이콘의 개별 X/Y 위치입니다.")]
    [SerializeField] private Vector2 moveHoverCostIconOffset = Vector2.zero;
    [Tooltip("숫자 이미지 묶음의 개별 X/Y 위치입니다.")]
    [SerializeField] private Vector2 moveHoverCostNumberOffset = Vector2.zero;
    [Tooltip("코스트 아이콘 배경의 개별 X/Y 위치입니다.")]
    [SerializeField] private Vector2 moveHoverCostBackgroundOffset = Vector2.zero;
    [SerializeField] private Vector2 moveHoverCostIconSize = new Vector2(0.28f, 0.28f);
    [SerializeField] private Vector2 moveHoverCostBackgroundSize = new Vector2(0.34f, 0.34f);
    [SerializeField] private Vector2 moveHoverCostDigitSize = new Vector2(0.2f, 0.28f);
    [SerializeField, Min(0f)] private float moveHoverCostIconGap = 0.06f;
    [SerializeField, Min(0f)] private float moveHoverCostDigitGap = 0.01f;
    [SerializeField] private Color moveHoverCostIconColor = new Color32(0x3F, 0x6F, 0xBB, 0xFF);
    [SerializeField] private Color moveHoverCostBackgroundColor = Color.white;
    [SerializeField] private Color moveHoverCostDigitColor = new Color32(0x3F, 0x6F, 0xBB, 0xFF);
    [SerializeField] private int moveHoverCostSortingOrder = 12;

    [Header("Nocturn Portal Preview")]
    [Tooltip("그리드 중심에서 포털 예고 프리팹에 추가할 위치 오프셋입니다.")]
    [SerializeField] private Vector3 nocturnPortalIndicatorOffset = Vector3.zero;
    [Tooltip("포털 예고 프리팹 렌더러의 정렬 순서 오프셋입니다.")]
    [SerializeField] private int nocturnPortalIndicatorSortingOrder = 13;

    [Header("Range Highlight Colors")]
    [SerializeField] private Color moveHighlightColor = new Color(0.698f, 0.698f, 0.243f, 1f);
    [SerializeField] private Color powerHighlightColor = new Color(0.243f, 0.318f, 0.698f, 1f);
    [SerializeField] private Color attackHighlightColor = new Color(0.698f, 0.243f, 0.271f, 1f);
    [SerializeField] private Color skillHighlightColor = new Color(0.686f, 0.243f, 0.698f, 1f);

    private CharacterRuntimeData currentUserRuntime;
    private SkillMasterData currentSkillData;
    private int currentSlotIndex = -1;
    private int currentCasterGridIndex = -1;
    private BattleDirection currentCasterDirection = BattleDirection.Right;
    private Sprite currentCasterSprite;

    private readonly List<int> currentMoveSelectableIndices = new();
    private readonly List<int> currentGeneralSelectionSelectableIndices = new();
    private readonly Dictionary<int, List<List<Vector2Int>>> currentMovePathCandidatesByTargetIndex = new();
    private bool isGridTargetMonsterVisualActive;
    private bool isForcedDirectionalSkillHoverPathVisible;
    private GameObject moveHoverPingVfxInstance;
    private GameObject moveHoverCostImagesRoot;
    private SpriteRenderer moveHoverCostIconRenderer;
    private SpriteRenderer moveHoverCostBackgroundRenderer;
    private readonly List<SpriteRenderer> moveHoverCostDigitRenderers = new();
    private int moveHoverPingGridIndex = -1;
    private MoveDragPathState moveDragPathState;
    private bool moveDragActive;


    private sealed class NocturnPortalIndicatorEntry
    {
        public GameObject Instance;
        public int ReferenceCount;
    }

    private readonly Dictionary<string, NocturnPortalIndicatorEntry> nocturnPortalIndicators = new();

    private int currentMoveDistancePerCommand = 1;
    private int currentMoveReservationCapacity = 1;

    private const string MoveSkillLevelOneId = "S_Move_1";
    private const string NocturnPortalPreviewGridEffectId = "GR_nocturn_portal_preview";
    private const string NocturnPortalVfxLayerName = "VFX";
    private const float NocturnPortalPreviewLifeTime = 9999f;
    private const string GeneralSelectionRangeId = "Range_b_24";
    private const string MoveHoverPingSortingLayerName = "Unit";
    private const float MoveHoverPingYSortMultiplier = 100f;
    private const int MoveHoverPingDefaultSortingOffset = 10;
    private const int MoveHoverPingLegacyFrontSortingOrderThreshold = 1000;
    private static readonly Color ForcedDirectionalMovePathColor = new Color32(0xB0, 0x24, 0x24, 0xFF);

    private int lastRightClickSkillCancelFrame = -1;

    // Keep the timeline visible, but let world-grid clicks pass through it while moving.
    private CanvasGroup battleSlotRaycastGroup;
    private bool battleSlotOriginalBlocksRaycasts;
    private bool battleSlotRaycastOverrideActive;


    public bool WasSkillSelectionCancelledByRightClickThisFrame =>
        lastRightClickSkillCancelFrame == Time.frameCount;

    private void OnEnable()
    {
        if (gridManager != null)
        {
            gridManager.OnCellClicked += HandleCellClicked;
            gridManager.OnCellPointerDown += HandleCellPointerDown;
            gridManager.OnCellPointerUp += HandleCellPointerUp;
            gridManager.OnCellHovered += HandleCellHovered;
            gridManager.OnCellHoverExited += HandleCellHoverExited;
        }
    }

    private void OnDisable()
    {
        moveDragPathState = null;
        moveDragActive = false;
        RestoreBattleSlotRaycasts();
        ClearNocturnPortalDestinationIndicators();
        HideMoveHoverPing();
        HideMovePathPreview();
        SetGridTargetMonsterVisualActive(false);

        if (gridManager != null)
        {
            gridManager.OnCellClicked -= HandleCellClicked;
            gridManager.OnCellPointerDown -= HandleCellPointerDown;
            gridManager.OnCellPointerUp -= HandleCellPointerUp;
            gridManager.OnCellHovered -= HandleCellHovered;
            gridManager.OnCellHoverExited -= HandleCellHoverExited;
        }
    }

    private void Update()
    {
        UpdateBattleSlotRaycasts();

        // OnMouseUp은 포인터가 다른 오브젝트 위에서 놓이면 호출되지 않을 수 있습니다.
        if (moveDragActive && Input.GetMouseButtonUp(0))
        {
            if (IsGridPointerInteractionBlocked())
                CancelMoveDragReservation();
            else
                FinalizeMoveDragReservation();
        }

        if (IsGridPointerInteractionBlocked())
        {
            HideBlockedGridPointerVisuals();
            return;
        }

        HandleGridSelectionCancelInput();
    }

    private void UpdateBattleSlotRaycasts()
    {
        bool isMoving = IsMoveSkillSelectionActive() && !UIPanelButton.IsMenuPanelOpen;
        if (!isMoving)
        {
            RestoreBattleSlotRaycasts();
            return;
        }

        if (battleSlotRaycastGroup == null)
        {
            BattleTimelineController timeline = timelineController != null
                ? timelineController
                : FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
            if (timeline == null)
                return;

            Transform root = timeline.transform.root;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name != "BattleSlot" ||
                    all[i].GetComponentInParent<Canvas>() == null)
                    continue;
                battleSlotRaycastGroup = all[i].GetComponent<CanvasGroup>();
                if (battleSlotRaycastGroup == null)
                    battleSlotRaycastGroup = all[i].gameObject.AddComponent<CanvasGroup>();
                break;
            }
        }

        if (battleSlotRaycastGroup == null || battleSlotRaycastOverrideActive)
            return;

        battleSlotOriginalBlocksRaycasts = battleSlotRaycastGroup.blocksRaycasts;
        battleSlotRaycastGroup.blocksRaycasts = false;
        battleSlotRaycastOverrideActive = true;
    }

    private void RestoreBattleSlotRaycasts()
    {
        if (battleSlotRaycastOverrideActive && battleSlotRaycastGroup != null)
            battleSlotRaycastGroup.blocksRaycasts = battleSlotOriginalBlocksRaycasts;
        battleSlotRaycastOverrideActive = false;
    }

    private bool IsGridPointerInteractionBlocked()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return true;

        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private void HideBlockedGridPointerVisuals()
    {
        HideMoveHoverPing();

        // 스킬 아이콘 위에 마우스를 올린 동안에는 EventSystem이 UI 위 포인터로 판정합니다.
        // 일반 이동용 Move Path는 숨기되, Range_Advance / Range_Retreat 스킬의
        // 호버 범위로 표시 중인 Move Path는 UI 위에서도 유지합니다.
        if (!isForcedDirectionalSkillHoverPathVisible)
            HideMovePathPreview();

        if (IsGeneralSelectionSkillActive() && rangePreview != null)
            rangePreview.ClearRangeOnly();
    }

    private void HandleGridSelectionCancelInput()
    {
        if (currentSkillData == null ||
            currentSkillData.RangeType != RangeType.Selection)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(1))
            return;

        lastRightClickSkillCancelFrame = Time.frameCount;
        // 그리드뿐 아니라 타임라인의 selectedSkill도 함께 해제합니다.
        // 그렇지 않으면 다음 이동 버튼 클릭이 재선택 대신 토글 해제로 처리됩니다.
        if (timelineController == null)
            EnsureTimelineController();

        if (timelineController == null || !timelineController.CancelPendingActionSelection())
            ClearPreview();
    }

    private void EnsureSkillListPanel()
    {
        if (skillListPanel != null)
            return;

        skillListPanel = FindFirstObjectByType<SkillListPanel>(FindObjectsInactive.Include);
    }

    private void EnsureTimelineController()
    {
        if (timelineController != null)
            return;

        timelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private void KeepSkillListOpenForThisClick()
    {
        if (!keepSkillListOpenAfterReservationClick)
            return;

        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        skillListPanel.IgnoreOutsideCloseForFrames(keepSkillListOpenIgnoreFrames);
    }

    private void RestoreHoveredSkillRangeAfterReservation()
    {
        EnsureSkillListPanel();

        if (skillListPanel == null)
            return;

        StartCoroutine(RestoreHoveredSkillRangeAfterReservationRoutine());
    }

    private IEnumerator RestoreHoveredSkillRangeAfterReservationRoutine()
    {
        // 예약 완료 시 기존 범위가 지워진 뒤 UI 클릭 처리가 끝난 다음 프레임에
        // 마우스가 여전히 같은 스킬 위에 있다면 호버 범위를 다시 표시합니다.
        yield return null;

        EnsureSkillListPanel();
        skillListPanel?.RestoreSelectedSkillHoverRangeIfPointerOver();
    }

    public void ShowSkillHoverRangePreview(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData,
        int slotIndex)
    {
        // 호버 프리뷰는 현재 선택 중인 프리뷰를 잠시 완전히 덮어쓴다.
        // Selection/Direction이 서로 다른 Highlight 머테리얼을 사용하므로
        // 기존 방향 셀까지 모두 지운 뒤 호버한 스킬의 머테리얼로 다시 표시해야 한다.
        if (rangePreview != null)
            rangePreview.ClearAll();

        HideMovePathPreview();

        if (userRuntime == null || skillData == null)
            return;

        if (gridManager == null || DataManager.Instance == null || DataManager.Instance.RangeDatabase == null)
            return;

        EnsureTimelineController();

        int casterGridIndex = FindCurrentCharacterGridIndex(userRuntime);
        BattleDirection casterDirection = userRuntime.Direction;

        if (timelineController != null && slotIndex >= 0)
        {
            int previewGridIndex = timelineController.GetPreviewGridIndexAtSlotEnd(userRuntime, slotIndex);

            if (previewGridIndex >= 0)
                casterGridIndex = previewGridIndex;

            casterDirection = timelineController.GetPreviewDirection(userRuntime, slotIndex);
        }

        if (casterGridIndex < 0)
            return;

        string effectiveRangeId =
            BattleEquipmentEffectService.GetEffectiveRangeId(userRuntime, skillData);

        if (TryBuildForcedDirectionalMovePreview(
                userRuntime,
                skillData,
                casterGridIndex,
                casterDirection,
                slotIndex,
                out List<Vector2Int> forcedMovePath,
                out _))
        {
            EnsureMovePathPreview();

            if (movePathPreview != null)
            {
                movePathPreview.ShowFullPath(
                    casterGridIndex,
                    forcedMovePath,
                    ForcedDirectionalMovePathColor);
                isForcedDirectionalSkillHoverPathVisible = forcedMovePath.Count > 0;
            }

            return;
        }

        List<int> rangeIndices = new();

        if (IsMoveSkill(skillData))
        {
            int moveDistancePerCommand = GetMoveDistancePerCommandForPreview(
                userRuntime,
                skillData);
            int moveReservationCapacity = Mathf.Max(0, userRuntime.PreviewCost);

            rangeIndices = GetMoveRangeIndices(
                casterGridIndex,
                moveReservationCapacity,
                moveDistancePerCommand,
                gridManager);

            if (rangeIndices.Count <= 0)
                return;

            rangePreview.ShowDirectionCells(
                rangeIndices,
                moveHighlightColor,
                moveHighlightMaterial);

            return;
        }

        if (skillData.RangeType == RangeType.Selection)
        {
            rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                casterGridIndex,
                GeneralSelectionRangeId,
                DataManager.Instance.RangeDatabase,
                gridManager
            );

            if (rangeIndices.Count <= 0)
                return;

            rangePreview.ShowDirectionCells(
                rangeIndices,
                moveHighlightColor,
                moveHighlightMaterial
            );

            return;
        }

        string rangeId = effectiveRangeId;

        if (BattleRangeCalculator.IsPartyRangeId(rangeId))
        {
            if (timelineController != null && slotIndex >= 0)
                rangeIndices = timelineController.GetAlivePartyPreviewGridIndicesAtSlotEnd(slotIndex);

            if (rangeIndices.Count <= 0)
                rangeIndices.Add(casterGridIndex);

            rangePreview.ShowRangeCells(rangeIndices, GetHighlightColor(skillData));
            return;
        }

        if (skillData.RangeType == RangeType.Direction)
        {
            int rangeOriginGridIndex = GetDirectionalAttackPreviewOriginGridIndex(
                casterGridIndex,
                casterDirection,
                skillData);

            rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                rangeOriginGridIndex,
                rangeId,
                casterDirection,
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }

        if (rangeIndices.Count <= 0)
            return;

        rangePreview.ShowRangeCells(rangeIndices, GetHighlightColor(skillData));
    }

    private bool TryBuildForcedDirectionalMovePreview(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData,
        int casterGridIndex,
        BattleDirection casterDirection,
        int slotIndex,
        out List<Vector2Int> moveSteps,
        out int destinationGridIndex)
    {
        moveSteps = null;
        destinationGridIndex = casterGridIndex;

        if (userRuntime == null || skillData == null || gridManager == null || casterGridIndex < 0)
            return false;

        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(userRuntime, skillData);

        if (!TryGetForcedDirectionalMoveDefinition(
                rangeId,
                casterDirection,
                out Vector2Int step,
                out int distance))
        {
            return false;
        }

        moveSteps = new List<Vector2Int>(distance);
        HashSet<int> blockedGridIndices = BuildForcedDirectionalMovePreviewBlockedGridIndices(
            userRuntime,
            slotIndex);

        Vector2Int currentCoord = gridManager.IndexToCoord(casterGridIndex);

        for (int i = 0; i < distance; i++)
        {
            Vector2Int nextCoord = currentCoord + step;

            if (!gridManager.IsValidCoord(nextCoord))
                break;

            int nextGridIndex = gridManager.CoordToIndex(nextCoord);

            if (blockedGridIndices.Contains(nextGridIndex))
                break;

            moveSteps.Add(step);
            currentCoord = nextCoord;
            destinationGridIndex = nextGridIndex;
        }

        return true;
    }

    private static bool TryGetForcedDirectionalMoveDefinition(
        string rangeId,
        BattleDirection casterDirection,
        out Vector2Int step,
        out int distance)
    {
        step = Vector2Int.zero;
        distance = 0;

        if (string.IsNullOrWhiteSpace(rangeId))
            return false;

        const string advancePrefix = "Range_Advance";
        const string retreatPrefix = "Range_Retreat";

        bool isRetreat;
        string distanceText;

        if (rangeId.StartsWith(advancePrefix, StringComparison.OrdinalIgnoreCase))
        {
            isRetreat = false;
            distanceText = rangeId.Substring(advancePrefix.Length);
        }
        else if (rangeId.StartsWith(retreatPrefix, StringComparison.OrdinalIgnoreCase))
        {
            isRetreat = true;
            distanceText = rangeId.Substring(retreatPrefix.Length);
        }
        else
        {
            return false;
        }

        if (!int.TryParse(distanceText, out distance) || distance < 1 || distance > 6)
            return false;

        step = casterDirection == BattleDirection.Right
            ? Vector2Int.right
            : Vector2Int.left;

        if (isRetreat)
            step = -step;

        return true;
    }

    private HashSet<int> BuildForcedDirectionalMovePreviewBlockedGridIndices(
        CharacterRuntimeData userRuntime,
        int slotIndex)
    {
        HashSet<int> blockedGridIndices = new();

        if (gridManager == null)
            return blockedGridIndices;

        EnsureTimelineController();

        string selfCharacterId = userRuntime != null
            ? userRuntime.CharacterId
            : null;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            if (!string.IsNullOrWhiteSpace(selfCharacterId) &&
                character.CharacterId == selfCharacterId)
            {
                continue;
            }

            int occupiedGridIndex = character.CurrentGridIndex;

            if (timelineController != null && slotIndex >= 0)
            {
                int projectedGridIndex = timelineController.GetPreviewGridIndexAtSlotEnd(
                    character.RuntimeData,
                    slotIndex);

                if (projectedGridIndex >= 0)
                    occupiedGridIndex = projectedGridIndex;
            }

            if (IsValidMoveDestinationGridIndex(occupiedGridIndex))
                blockedGridIndices.Add(occupiedGridIndex);
        }

        if (timelineController != null && slotIndex >= 0)
        {
            BattleActionSimulationService simulationService = new(gridManager);
            bool includeCurrentSlotMonsterCommands = !BattleActionOrderUtility.HasSwift(userRuntime);
            HashSet<int> projectedMonsterGridIndices =
                simulationService.GetProjectedMonsterOccupiedGridIndices(
                    timelineController,
                    slotIndex,
                    includeCurrentSlotMonsterCommands);

            if (projectedMonsterGridIndices != null)
            {
                foreach (int gridIndex in projectedMonsterGridIndices)
                {
                    if (IsValidMoveDestinationGridIndex(gridIndex))
                        blockedGridIndices.Add(gridIndex);
                }
            }
        }
        else
        {
            AddCurrentMonsterOccupiedGridIndices(blockedGridIndices);
        }

        AddBlockedGridEffectIndices(blockedGridIndices);
        return blockedGridIndices;
    }

    public void ClearSkillHoverRangePreview()
    {
        HideMovePathPreview();

        if (rangePreview == null)
            return;

        // 실제로 그리드 선택을 진행 중일 때는 선택 가능 범위를 유지하고,
        // 스킬 아이콘 호버로만 표시한 범위는 Highlight까지 완전히 해제한다.
        if (IsMoveSkillSelectionActive() || IsGeneralSelectionSkillActive())
            RestoreCurrentSelectionPreview();
        else
            rangePreview.ClearAll();
    }

    public void StartReservation(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData,
        int casterGridIndex,
        int slotIndex,
        Sprite casterSprite = null)
    {
        BattleDirection casterDirection = userRuntime != null
            ? userRuntime.Direction
            : BattleDirection.Right;

        StartReservation(
            userRuntime,
            skillData,
            casterGridIndex,
            slotIndex,
            casterDirection,
            casterSprite
        );
    }

    private void RestoreCurrentSelectionPreview()
    {
        if (rangePreview == null)
            return;

        if (IsMoveSkillSelectionActive())
        {
            rangePreview.ShowDirectionCells(
                currentMoveSelectableIndices,
                GetHighlightColor(currentSkillData),
                moveHighlightMaterial);
            return;
        }

        if (IsGeneralSelectionSkillActive())
        {
            rangePreview.ShowDirectionCells(
                currentGeneralSelectionSelectableIndices,
                moveHighlightColor,
                moveHighlightMaterial);
            return;
        }

        rangePreview.ClearAll();
    }

    public void StartReservation(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData,
        int casterGridIndex,
        int slotIndex,
        BattleDirection casterDirection,
        Sprite casterSprite = null)
    {
        MonsterUnit.HideAllTemporaryHUDs();
        ClearPreview();

        ResolveTimelinePreviewCasterState(
            userRuntime,
            slotIndex,
            ref casterGridIndex,
            ref casterDirection);

        currentUserRuntime = userRuntime;
        currentSkillData = skillData;
        currentCasterGridIndex = casterGridIndex;
        currentSlotIndex = slotIndex;
        currentCasterDirection = casterDirection;
        currentCasterSprite = casterSprite;

        if (currentUserRuntime == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.CharacterNotSelected));
            return;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SkillReservationMissing));
            return;
        }

        if (!IsMoveSkill(currentSkillData) &&
            IsAllRangeSkill(currentUserRuntime, currentSkillData))
        {
            SetGridTargetMonsterVisualActive(false);
            ConfirmAllRangeReservation();
            return;
        }

        if (currentSkillData.RangeType == RangeType.Direction)
        {
            SetGridTargetMonsterVisualActive(false);
            ConfirmDirectionReservation(currentCasterDirection);
            return;
        }

        if (currentSkillData.RangeType == RangeType.Selection)
        {
            bool isMoveSkill = IsMoveSkill(currentSkillData);
            SetGridTargetMonsterVisualActive(true, isMoveSkill);

            if (isMoveSkill)
            {
                PreviewMoveSelectableCells();
            }
            else if (CanUseRangeData())
            {
                PreviewGeneralSelectionSelectableCells();
            }
            else
            {
                ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SkillRangeMissing));
            }

            return;
        }

        SetGridTargetMonsterVisualActive(false);
        ConfirmDirectReservation();
    }

    private void ResolveTimelinePreviewCasterState(
        CharacterRuntimeData userRuntime,
        int slotIndex,
        ref int casterGridIndex,
        ref BattleDirection casterDirection)
    {
        if (userRuntime == null || slotIndex < 0)
            return;

        EnsureTimelineController();

        if (timelineController == null)
            return;

        int previewGridIndex =
            timelineController.GetPreviewGridIndexAtSlotEnd(userRuntime, slotIndex);

        if (previewGridIndex >= 0)
            casterGridIndex = previewGridIndex;

        casterDirection = timelineController.GetPreviewDirection(userRuntime, slotIndex);
    }

    private void RefreshCurrentCasterStateFromTimelinePreview()
    {
        if (currentUserRuntime == null || currentSlotIndex < 0)
            return;

        EnsureTimelineController();

        if (timelineController == null)
            return;

        int previewGridIndex =
            timelineController.GetPreviewGridIndexAtSlotEnd(currentUserRuntime, currentSlotIndex);

        if (previewGridIndex >= 0)
            currentCasterGridIndex = previewGridIndex;

        currentCasterDirection =
            timelineController.GetPreviewDirection(currentUserRuntime, currentSlotIndex);
    }

    private void PreviewGeneralSelectionSelectableCells()
    {
        currentGeneralSelectionSelectableIndices.Clear();

        if (!IsGeneralSelectionSkillActive() || !CanUseRangeData())
            return;

        RefreshCurrentCasterStateFromTimelinePreview();

        currentGeneralSelectionSelectableIndices.AddRange(
            BattleRangeCalculator.GetSelectionRangeIndices(
                currentCasterGridIndex,
                GeneralSelectionRangeId,
                DataManager.Instance.RangeDatabase,
                gridManager
            )
        );

        if (currentGeneralSelectionSelectableIndices.Count <= 0)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SelectableGridRangeMissing));
            return;
        }

        if (rangePreview != null)
        {
            rangePreview.ShowDirectionCells(
                currentGeneralSelectionSelectableIndices,
                moveHighlightColor,
                moveHighlightMaterial
            );
        }
    }

    private void PreviewMoveSelectableCells()
    {
        currentMoveSelectableIndices.Clear();
        currentGeneralSelectionSelectableIndices.Clear();
        currentMovePathCandidatesByTargetIndex.Clear();

        if (!CanUseRangeData())
            return;

        RefreshCurrentCasterStateFromTimelinePreview();

        currentMoveDistancePerCommand = GetMoveDistancePerCommand();
        currentMoveReservationCapacity = GetMoveReservationCapacity();

        HashSet<int> blockedDestinationGridIndices = BuildKnownOtherPlayerDestinationGridIndices();

        AddCurrentCasterSelfFlipCandidate(blockedDestinationGridIndices);

        if (currentMoveReservationCapacity <= 0)
        {
            if (currentMoveSelectableIndices.Count > 0 && rangePreview != null)
                rangePreview.ShowDirectionCells(
                    currentMoveSelectableIndices,
                    GetHighlightColor(currentSkillData),
                    moveHighlightMaterial);

            if (currentMoveSelectableIndices.Count > 0)
                return;

            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.InsufficientMoveCost));
            return;
        }

        List<int> rangeIndices = GetMoveRangeIndices(
            currentCasterGridIndex,
            currentMoveReservationCapacity,
            currentMoveDistancePerCommand,
            gridManager
        );

        HashSet<int> currentBlockedGridIndices = BuildCurrentMoveBlockedGridIndices();
        HashSet<int> projectedBlockedGridIndices = BuildProjectedMoveBlockedGridIndices();

        for (int i = 0; i < rangeIndices.Count; i++)
        {
            int index = rangeIndices[i];

            if (blockedDestinationGridIndices.Contains(index))
                continue;

            List<List<Vector2Int>> pathCandidates = BuildPreferredMovePathCandidates(
                index,
                currentBlockedGridIndices,
                projectedBlockedGridIndices);

            if (pathCandidates.Count <= 0)
                continue;

            currentMovePathCandidatesByTargetIndex[index] = pathCandidates;

            if (!currentMoveSelectableIndices.Contains(index))
                currentMoveSelectableIndices.Add(index);
        }

        if (currentMoveSelectableIndices.Count <= 0)
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.NoSelectableGrid));

        if (rangePreview != null)
            rangePreview.ShowDirectionCells(
                currentMoveSelectableIndices,
                GetHighlightColor(currentSkillData),
                moveHighlightMaterial);
    }

    private void AddCurrentCasterSelfFlipCandidate(ISet<int> blockedDestinationGridIndices)
    {
        RefreshCurrentCasterStateFromTimelinePreview();

        if (!IsCurrentCasterGridIndexValid())
            return;

        if (blockedDestinationGridIndices != null &&
            blockedDestinationGridIndices.Contains(currentCasterGridIndex))
        {
            return;
        }

        currentMovePathCandidatesByTargetIndex[currentCasterGridIndex] =
            BuildSelfFlipMovePathCandidates();

        if (!currentMoveSelectableIndices.Contains(currentCasterGridIndex))
            currentMoveSelectableIndices.Add(currentCasterGridIndex);
    }

    private bool IsCurrentCasterGridIndexValid()
    {
        return IsValidMoveDestinationGridIndex(currentCasterGridIndex);
    }

    private bool IsCurrentCasterGridIndex(int gridIndex)
    {
        RefreshCurrentCasterStateFromTimelinePreview();
        return gridIndex >= 0 && gridIndex == currentCasterGridIndex;
    }

    private static List<List<Vector2Int>> BuildSelfFlipMovePathCandidates()
    {
        return new List<List<Vector2Int>>
        {
            new List<Vector2Int> { Vector2Int.zero }
        };
    }

    private void HandleCellHovered(GridCell cell)
    {
        if (IsGridPointerInteractionBlocked())
        {
            HideBlockedGridPointerVisuals();
            return;
        }

        if (cell == null)
            return;

        if (IsMoveSkillSelectionActive())
        {
            if (moveDragActive)
            {
                ExtendMoveDragPath(cell);
                return;
            }

            if (currentMoveSelectableIndices.Contains(cell.Index))
            {
                ShowMoveHoverPing(cell.Index);
                ShowMovePathPreview(cell.Index);
            }
            else
            {
                HideMoveHoverPing();
                HideMovePathPreview();
            }

            return;
        }

        if (!IsGeneralSelectionSkillActive())
            return;

        if (currentGeneralSelectionSelectableIndices.Contains(cell.Index))
            ShowSelectionRangeAt(cell.Index);
        else if (rangePreview != null)
            rangePreview.ClearRangeOnly();
    }

    private void HandleCellHoverExited(GridCell cell)
    {
        if (IsMoveSkillSelectionActive())
        {
            if (moveDragActive)
                return;

            if (cell == null || cell.Index == moveHoverPingGridIndex)
            {
                HideMoveHoverPing();
                HideMovePathPreview();
            }

            return;
        }

        if (!IsGeneralSelectionSkillActive())
            return;

        if (rangePreview != null)
            rangePreview.ClearRangeOnly();
    }

    private void ShowMoveHoverPing(int gridIndex, int explicitMoveCost = -1)
    {
        if (gridManager == null || !currentMoveSelectableIndices.Contains(gridIndex))
        {
            HideMoveHoverPing();
            return;
        }

        // 같은 그리드에 머무는 동안 VFX를 반복 생성하거나 재생하지 않습니다.
        if (moveHoverPingGridIndex == gridIndex && explicitMoveCost < 0)
            return;

        HideMoveHoverPing();
        Vector3 gridWorldPosition = gridManager.GetWorldPositionByIndex(gridIndex);

        if (moveHoverPingVfxPrefab != null)
        {
            moveHoverPingVfxInstance = Instantiate(
                moveHoverPingVfxPrefab,
                gridWorldPosition + moveHoverPingVfxOffset,
                moveHoverPingVfxPrefab.transform.rotation,
                transform);
            moveHoverPingVfxInstance.name = "Move Hover Ping VFX";
            moveHoverPingVfxInstance.transform.localScale =
                moveHoverPingVfxPrefab.transform.localScale * Mathf.Max(0f, moveHoverPingVfxScale);

            // 프리팹 내부 렌더러 사이의 정렬 순서는 유지합니다.
            Renderer[] renderers = moveHoverPingVfxInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                int relativeOrder = renderer.sortingOrder;
                ApplyMoveHoverYSort(renderer, gridWorldPosition.y,
                    moveHoverPingVfxSortingOrder + relativeOrder, 0);
            }

            moveHoverPingVfxInstance.SetActive(true);
        }

        // VFX와 독립적으로 아이콘 및 숫자 이미지를 표시합니다.
        ShowMoveHoverCostDisplay(gridIndex, gridWorldPosition, explicitMoveCost);
        moveHoverPingGridIndex = gridIndex;
    }

    private void ShowMoveHoverCostDisplay(
        int gridIndex,
        Vector3 gridWorldPosition,
        int explicitMoveCost = -1)
    {
        int moveCost = explicitMoveCost >= 0 ? explicitMoveCost : GetMoveHoverCost(gridIndex);
        if (moveCost < 0)
        {
            if (moveHoverCostImagesRoot != null)
                moveHoverCostImagesRoot.SetActive(false);
            return;
        }

        EnsureMoveHoverCostDisplay();
        moveHoverCostImagesRoot.transform.position = gridWorldPosition + moveHoverCostTextOffset;
        moveHoverCostImagesRoot.SetActive(true);

        string digits = moveCost.ToString();
        float digitWidth = Mathf.Max(0f, moveHoverCostDigitSize.x);
        float digitGap = Mathf.Max(0f, moveHoverCostDigitGap);
        float numberWidth = digits.Length * digitWidth + Mathf.Max(0, digits.Length - 1) * digitGap;
        bool hasIcon = moveHoverCostIcon != null;
        float iconWidth = hasIcon ? Mathf.Max(0f, moveHoverCostIconSize.x) : 0f;
        float gap = hasIcon ? Mathf.Max(0f, moveHoverCostIconGap) : 0f;
        float left = -(iconWidth + gap + numberWidth) * 0.5f;
        float iconX = left + iconWidth * 0.5f;

        moveHoverCostIconRenderer.gameObject.SetActive(hasIcon);
        if (hasIcon)
            ConfigureMoveHoverCostSprite(moveHoverCostIconRenderer, moveHoverCostIcon,
                moveHoverCostIconColor, moveHoverCostIconSize,
                iconX + moveHoverCostIconOffset.x, moveHoverCostIconOffset.y,
                gridWorldPosition.y);

        bool showBackground = hasIcon && moveHoverCostIconBackground != null;
        moveHoverCostBackgroundRenderer.gameObject.SetActive(showBackground);
        if (showBackground)
            ConfigureMoveHoverCostSprite(moveHoverCostBackgroundRenderer, moveHoverCostIconBackground,
                moveHoverCostBackgroundColor, moveHoverCostBackgroundSize,
                iconX + moveHoverCostIconOffset.x + moveHoverCostBackgroundOffset.x,
                moveHoverCostIconOffset.y + moveHoverCostBackgroundOffset.y,
                gridWorldPosition.y, -1);

        EnsureMoveHoverCostDigitCount(digits.Length);
        for (int i = 0; i < moveHoverCostDigitRenderers.Count; i++)
        {
            SpriteRenderer digitRenderer = moveHoverCostDigitRenderers[i];
            if (i >= digits.Length)
            {
                digitRenderer.gameObject.SetActive(false);
                continue;
            }

            int digit = digits[i] - '0';
            Sprite digitSprite = moveHoverCostDigits != null && digit >= 0 &&
                                 digit < moveHoverCostDigits.Length ? moveHoverCostDigits[digit] : null;
            digitRenderer.gameObject.SetActive(digitSprite != null);
            if (digitSprite == null)
                continue;

            float x = left + iconWidth + gap + digitWidth * (i + 0.5f) + digitGap * i;
            ConfigureMoveHoverCostSprite(digitRenderer, digitSprite,
                moveHoverCostDigitColor, moveHoverCostDigitSize,
                x + moveHoverCostNumberOffset.x, moveHoverCostNumberOffset.y,
                gridWorldPosition.y);
        }
    }

    private void EnsureMoveHoverCostDisplay()
    {
        if (moveHoverCostImagesRoot != null)
            return;

        moveHoverCostImagesRoot = new GameObject("Move Hover Cost Display");
        moveHoverCostImagesRoot.transform.SetParent(transform, false);

        GameObject backgroundObject = new GameObject("Cost Icon Background");
        backgroundObject.transform.SetParent(moveHoverCostImagesRoot.transform, false);
        moveHoverCostBackgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();

        GameObject iconObject = new GameObject("Cost Icon");
        iconObject.transform.SetParent(moveHoverCostImagesRoot.transform, false);
        moveHoverCostIconRenderer = iconObject.AddComponent<SpriteRenderer>();
    }

    private void EnsureMoveHoverCostDigitCount(int count)
    {
        while (moveHoverCostDigitRenderers.Count < count)
        {
            GameObject digitObject = new GameObject("Cost Digit " + moveHoverCostDigitRenderers.Count);
            digitObject.transform.SetParent(moveHoverCostImagesRoot.transform, false);
            moveHoverCostDigitRenderers.Add(digitObject.AddComponent<SpriteRenderer>());
        }
    }

    private void ConfigureMoveHoverCostSprite(SpriteRenderer renderer, Sprite sprite,
        Color color, Vector2 size, float localX, float localY, float gridWorldY, int sortingOffset = 0)
    {
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.transform.localPosition = new Vector3(localX, localY, 0f);
        if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
            renderer.transform.localScale = new Vector3(
                Mathf.Max(0f, size.x) / sprite.bounds.size.x,
                Mathf.Max(0f, size.y) / sprite.bounds.size.y, 1f);
        ApplyMoveHoverYSort(renderer, gridWorldY, moveHoverCostSortingOrder + sortingOffset, 2);
        renderer.gameObject.SetActive(true);
    }

    private int GetMoveHoverCost(int gridIndex)
    {
        if (gridIndex == currentCasterGridIndex)
            return 0;

        if (!currentMovePathCandidatesByTargetIndex.TryGetValue(gridIndex, out List<List<Vector2Int>> pathCandidates))
            pathCandidates = BuildPreferredMovePathCandidates(gridIndex);

        List<Vector2Int> movePath = GetFirstReservableMovePath(pathCandidates);
        return movePath == null ? -1 : GetEffectiveMoveReservationCost(movePath);
    }

    private static void ApplyMoveHoverYSort(
        Renderer renderer,
        float sortingWorldY,
        int configuredSortingOffset,
        int fallbackOffset)
    {
        if (renderer == null)
            return;

        renderer.sortingLayerName = MoveHoverPingSortingLayerName;
        renderer.sortingOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(
            sortingWorldY,
            MoveHoverPingYSortMultiplier,
            ResolveMoveHoverSortingOffset(configuredSortingOffset, fallbackOffset));
    }

    private static int ResolveMoveHoverSortingOffset(
        int configuredSortingOffset,
        int fallbackOffset)
    {
        if (Mathf.Abs(configuredSortingOffset) >= MoveHoverPingLegacyFrontSortingOrderThreshold)
            return MoveHoverPingDefaultSortingOffset + Mathf.Max(0, fallbackOffset);

        return configuredSortingOffset;
    }

    private void HideMoveHoverPing()
    {
        moveHoverPingGridIndex = -1;

        if (moveHoverPingVfxInstance != null)
        {
            Destroy(moveHoverPingVfxInstance);
            moveHoverPingVfxInstance = null;
        }

        if (moveHoverCostImagesRoot != null)
            moveHoverCostImagesRoot.SetActive(false);
    }

    private void ShowMovePathPreview(int gridIndex)
    {
        isForcedDirectionalSkillHoverPathVisible = false;
        EnsureMovePathPreview();

        if (movePathPreview == null || gridManager == null)
            return;

        if (gridIndex == currentCasterGridIndex)
        {
            movePathPreview.Clear();
            return;
        }

        if (!currentMovePathCandidatesByTargetIndex.TryGetValue(gridIndex, out List<List<Vector2Int>> pathCandidates))
            pathCandidates = BuildPreferredMovePathCandidates(gridIndex);

        List<Vector2Int> movePath = GetFirstReservableMovePath(pathCandidates);

        if (movePath == null || movePath.Count <= 0)
        {
            movePathPreview.Clear();
            return;
        }

        movePathPreview.ShowPath(currentCasterGridIndex, movePath);
    }

    private void ShowMovePathPreview(IReadOnlyList<Vector2Int> movePath)
    {
        isForcedDirectionalSkillHoverPathVisible = false;
        EnsureMovePathPreview();

        if (movePathPreview == null)
            return;

        if (movePath == null || movePath.Count <= 0)
        {
            movePathPreview.Clear();
            return;
        }

        movePathPreview.ShowPath(currentCasterGridIndex, movePath);
    }

    private void HideMovePathPreview()
    {
        isForcedDirectionalSkillHoverPathVisible = false;

        if (movePathPreview != null)
            movePathPreview.Clear();
    }

    private void EnsureMovePathPreview()
    {
        if (movePathPreview == null)
            movePathPreview = GetComponent<MovePathPreview>();

        if (movePathPreview == null || !movePathPreview.CanRenderPath)
        {
            MovePathPreview[] previews = FindObjectsByType<MovePathPreview>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < previews.Length; i++)
            {
                MovePathPreview preview = previews[i];

                if (preview == null || !preview.CanRenderPath)
                    continue;

                movePathPreview = preview;
                break;
            }
        }

        if (movePathPreview != null)
            movePathPreview.BindGridManager(gridManager);
    }


    public void ShowNocturnPortalDestinationIndicator(string runtimeId, int destinationGridIndex)
    {
        if (gridManager == null || destinationGridIndex < 0)
            return;

        string key = BuildNocturnPortalIndicatorKey(runtimeId, destinationGridIndex);

        if (nocturnPortalIndicators.TryGetValue(key, out NocturnPortalIndicatorEntry existing) &&
            existing != null && existing.Instance != null)
        {
            existing.ReferenceCount++;
            existing.Instance.SetActive(true);
            return;
        }

        if (!TryGetNocturnPortalPreviewData(out GridEffectData previewData))
        {
            Debug.LogWarning(
                $"[PlayerSkillReservationController] GridEffect 데이터에서 {NocturnPortalPreviewGridEffectId}를 찾을 수 없습니다.");
            return;
        }

        Vector3 gridWorldPosition = gridManager.GetWorldPositionByIndex(destinationGridIndex);
        BattleVfxEntry previewEntry = CreateNocturnPortalPreviewEntry(
            previewData,
            DataManager.Instance?.GridEffectSpriteDatabase,
            nocturnPortalIndicatorSortingOrder);
        int renderLayer = LayerMask.NameToLayer(NocturnPortalVfxLayerName);

        if (previewEntry == null ||
            !BattleWorldVfxRenderer.TrySpawnDetached(
                previewEntry,
                gridWorldPosition + nocturnPortalIndicatorOffset,
                renderLayer,
                gameObject.layer,
                NocturnPortalPreviewLifeTime,
                vfx => ConfigureNocturnPortalPreviewVfx(vfx, renderLayer),
                out BattleWorldVfxHandle handle) ||
            handle == null)
        {
            Debug.LogWarning(
                $"[PlayerSkillReservationController] Failed to spawn grid effect preview: {NocturnPortalPreviewGridEffectId}");
            return;
        }

        GameObject indicatorObject = handle.gameObject;
        indicatorObject.name = $"Nocturn Portal Destination {runtimeId}_{destinationGridIndex}";

        // 녹턴의 이동 예정 위치 VFX도 일반 그리드 효과와 같은 툴팁 경로를 사용합니다.
        GridEffectHoverTarget.Attach(
            indicatorObject,
            previewData.GridEffectID,
            new Vector2(1f, 1f));

        nocturnPortalIndicators[key] = new NocturnPortalIndicatorEntry
        {
            Instance = indicatorObject,
            ReferenceCount = 1
        };
    }

    public void HideNocturnPortalDestinationIndicator(string runtimeId, int destinationGridIndex)
    {
        string key = BuildNocturnPortalIndicatorKey(runtimeId, destinationGridIndex);

        if (!nocturnPortalIndicators.TryGetValue(key, out NocturnPortalIndicatorEntry entry) ||
            entry == null)
        {
            return;
        }

        entry.ReferenceCount--;

        if (entry.ReferenceCount > 0)
            return;

        nocturnPortalIndicators.Remove(key);

        if (entry.Instance != null)
            Destroy(entry.Instance);
    }

    public void ClearNocturnPortalDestinationIndicators()
    {
        foreach (KeyValuePair<string, NocturnPortalIndicatorEntry> pair in nocturnPortalIndicators)
        {
            if (pair.Value != null && pair.Value.Instance != null)
                Destroy(pair.Value.Instance);
        }

        nocturnPortalIndicators.Clear();
    }

    private static bool TryGetNocturnPortalPreviewData(out GridEffectData data)
    {
        data = null;

        GridEffectDatabase database = DataManager.Instance?.GridEffectDatabase;

        return database != null &&
               database.TryGet(NocturnPortalPreviewGridEffectId, out data) &&
               data != null;
    }

    private static BattleVfxEntry CreateNocturnPortalPreviewEntry(
        GridEffectData previewData,
        GridEffectSpriteDatabase database,
        int sortingOrderOffset)
    {
        if (previewData == null ||
            string.IsNullOrWhiteSpace(previewData.GridEffectID) ||
            database == null ||
            !database.TryGetPrefab(previewData.GridEffectID, out GameObject prefab) ||
            prefab == null)
        {
            return null;
        }

        return new BattleVfxEntry
        {
            prefab = prefab,
            renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture,
            proxyBlendMode = BattleVfxProxyBlendMode.Alpha,
            proxySortingLayerName = MoveHoverPingSortingLayerName,
            proxySortingOrderOffset = sortingOrderOffset,
            proxyYMultiplier = MoveHoverPingYSortMultiplier
        };
    }

    private static void ConfigureNocturnPortalPreviewVfx(GameObject vfx, int renderLayer)
    {
        if (vfx == null)
            return;

        vfx.SetActive(true);

        if (renderLayer >= 0)
            SetNocturnPortalPreviewLayerRecursively(vfx, renderLayer);

        if (vfx.GetComponent<BattleVfxPlaybackPauseController>() == null)
            vfx.AddComponent<BattleVfxPlaybackPauseController>();
    }

    private static void SetNocturnPortalPreviewLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
            SetNocturnPortalPreviewLayerRecursively(child.gameObject, layer);
    }

    private static string BuildNocturnPortalIndicatorKey(
        string runtimeId,
        int destinationGridIndex)
    {
        return $"{runtimeId ?? string.Empty}:{destinationGridIndex}";
    }

    private bool IsGeneralSelectionSkillActive()
    {
        return currentSkillData != null &&
               currentSkillData.RangeType == RangeType.Selection &&
               !IsMoveSkill(currentSkillData);
    }

    private void ShowSelectionRangeAt(int selectedGridIndex)
    {
        if (!IsGeneralSelectionSkillActive() || !CanUseRangeData())
            return;

        List<int> rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
            selectedGridIndex,
            BattleEquipmentEffectService.GetEffectiveRangeId(currentUserRuntime, currentSkillData),
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        if (rangePreview != null)
            rangePreview.ShowRangeCells(rangeIndices, GetHighlightColor(currentSkillData));
    }

    private void HandleCellClicked(GridCell cell)
    {
        if (IsGridPointerInteractionBlocked())
        {
            HideBlockedGridPointerVisuals();
            return;
        }

        if (cell == null || currentSkillData == null)
            return;

        if (currentSkillData.RangeType != RangeType.Selection)
            return;

        if (!IsMoveSkill(currentSkillData))
        {
            if (!currentGeneralSelectionSelectableIndices.Contains(cell.Index))
            {
                ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.GridNotSelectable));
                Debug.LogWarning($"[PlayerSkillReservationController] 일반 선택 스킬의 사용 가능 범위를 벗어났습니다: {cell.name}");
                return;
            }

            ConfirmSelectionReservation(cell.Index);
            return;
        }

        if (moveDragActive)
            return;

        if (!currentMoveSelectableIndices.Contains(cell.Index))
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.GridNotSelectable));
            Debug.LogWarning($"[PlayerSkillReservationController] 선택 가능한 이동 칸이 아닙니다: {cell.name}");
            return;
        }

        HideMovePathPreview();
        ConfirmMoveReservation(cell.Index);
    }

    private void HandleCellPointerDown(GridCell cell)
    {
        if (cell == null || IsGridPointerInteractionBlocked() || !IsMoveSkillSelectionActive())
            return;

        if (!currentMoveSelectableIndices.Contains(cell.Index))
            return;

        List<Vector2Int> moveOffsets;
        if (cell.Index == currentCasterGridIndex)
        {
            moveOffsets = new List<Vector2Int> { Vector2Int.zero };
        }
        else
        {
            if (!currentMovePathCandidatesByTargetIndex.TryGetValue(
                    cell.Index,
                    out List<List<Vector2Int>> pathCandidates))
            {
                pathCandidates = BuildPreferredMovePathCandidates(cell.Index);
            }

            moveOffsets = GetFirstReservableMovePath(pathCandidates);
        }

        if (moveOffsets == null || moveOffsets.Count <= 0 || gridManager == null)
            return;

        Vector2Int casterCoord = gridManager.IndexToCoord(currentCasterGridIndex);
        List<Vector2Int> coordinates = BuildMoveDragCoordinates(casterCoord, moveOffsets);
        moveDragPathState = new MoveDragPathState(coordinates);
        moveDragActive = true;
        RefreshMoveDragPreview();
    }

    private void HandleCellPointerUp(GridCell cell)
    {
        if (!moveDragActive)
            return;

        if (IsGridPointerInteractionBlocked())
            CancelMoveDragReservation();
        else
            FinalizeMoveDragReservation();
    }

    private void ExtendMoveDragPath(GridCell cell)
    {
        if (cell == null || gridManager == null || moveDragPathState == null)
            return;

        Vector2Int coordinate = gridManager.IndexToCoord(cell.Index);
        bool isVisited = MoveDragPathContainsCoordinate(coordinate);
        bool canAppend = isVisited || CanAppendMoveDragCoordinate(cell.Index, coordinate);

        if (moveDragPathState.TryVisit(coordinate, canAppend))
            RefreshMoveDragPreview();
    }

    private bool CanAppendMoveDragCoordinate(int gridIndex, Vector2Int coordinate)
    {
        if (gridManager == null || !gridManager.IsValidCoord(coordinate))
            return false;

        if (BuildKnownOtherPlayerDestinationGridIndices().Contains(gridIndex))
            return false;

        List<Vector2Int> coordinates = new List<Vector2Int>(moveDragPathState.Coordinates)
        {
            coordinate
        };
        List<Vector2Int> moveOffsets = BuildMoveDragOffsets(coordinates);
        return GetRequiredMoveReservationCount(moveOffsets, currentMoveDistancePerCommand) <=
               currentMoveReservationCapacity &&
               CanReserveMovePathWithEffectiveCost(moveOffsets) &&
               IsMovePathReservableAgainstCurrentOrProjectedState(gridIndex, moveOffsets);
    }

    private bool MoveDragPathContainsCoordinate(Vector2Int coordinate)
    {
        if (moveDragPathState == null)
            return false;

        for (int i = 0; i < moveDragPathState.Coordinates.Count; i++)
        {
            if (moveDragPathState.Coordinates[i] == coordinate)
                return true;
        }

        return false;
    }

    private void RefreshMoveDragPreview()
    {
        if (moveDragPathState == null || gridManager == null)
            return;

        List<Vector2Int> moveOffsets = BuildMoveDragOffsets(moveDragPathState.Coordinates);
        ShowMovePathPreview(moveOffsets);

        Vector2Int endpoint = moveDragPathState.Coordinates[moveDragPathState.Coordinates.Count - 1];
        int endpointIndex = gridManager.CoordToIndex(endpoint);
        ShowMoveHoverPing(endpointIndex, GetEffectiveMoveReservationCost(moveOffsets));
    }

    private void FinalizeMoveDragReservation()
    {
        MoveDragPathState finalizedState = moveDragPathState;
        moveDragPathState = null;
        moveDragActive = false;

        if (finalizedState == null || finalizedState.Coordinates.Count <= 0 || gridManager == null)
            return;

        List<Vector2Int> moveOffsets = BuildMoveDragOffsets(finalizedState.Coordinates);
        if (moveOffsets.Count <= 0)
            moveOffsets.Add(Vector2Int.zero);

        Vector2Int endpoint = finalizedState.Coordinates[finalizedState.Coordinates.Count - 1];
        int endpointIndex = gridManager.CoordToIndex(endpoint);
        if (!IsSelfFlipMovePath(moveOffsets) && !IsMoveDragPathStillReservable(endpointIndex, moveOffsets))
        {
            HideMoveHoverPing();
            HideMovePathPreview();
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.MoveDestinationInvalid));
            return;
        }

        ConfirmMoveReservation(endpointIndex, moveOffsets);
    }

    private void CancelMoveDragReservation()
    {
        moveDragPathState = null;
        moveDragActive = false;
        HideMoveHoverPing();
        HideMovePathPreview();
    }

    private bool IsMoveDragPathStillReservable(
        int targetGridIndex,
        IReadOnlyList<Vector2Int> moveOffsets)
    {
        if (gridManager == null || moveOffsets == null || moveOffsets.Count <= 0)
            return false;

        List<Vector2Int> path = new(moveOffsets);
        if (BuildKnownOtherPlayerDestinationGridIndices().Contains(targetGridIndex))
            return false;

        return IsMovePathReservableAgainstCurrentOrProjectedState(targetGridIndex, path);
    }

    private bool IsMovePathReservableAgainstCurrentOrProjectedState(
        int targetGridIndex,
        IReadOnlyList<Vector2Int> moveOffsets)
    {
        List<Vector2Int> path = moveOffsets != null
            ? new List<Vector2Int>(moveOffsets)
            : null;

        return IsMovePathReservable(
                   currentCasterGridIndex,
                   path,
                   gridManager,
                   BuildCurrentMoveBlockedGridIndices(),
                   targetGridIndex,
                   true) ||
               IsMovePathReservable(
                   currentCasterGridIndex,
                   path,
                   gridManager,
                   BuildProjectedMoveBlockedGridIndices(),
                   targetGridIndex,
                   true);
    }

    private static List<Vector2Int> BuildMoveDragCoordinates(
        Vector2Int start,
        IReadOnlyList<Vector2Int> moveOffsets)
    {
        List<Vector2Int> coordinates = new() { start };
        Vector2Int current = start;

        for (int i = 0; i < moveOffsets.Count; i++)
        {
            Vector2Int offset = moveOffsets[i];
            while (offset.x != 0)
            {
                current += new Vector2Int(Math.Sign(offset.x), 0);
                coordinates.Add(current);
                offset.x -= Math.Sign(offset.x);
            }

            while (offset.y != 0)
            {
                current += new Vector2Int(0, Math.Sign(offset.y));
                coordinates.Add(current);
                offset.y -= Math.Sign(offset.y);
            }
        }

        return coordinates;
    }

    private static List<Vector2Int> BuildMoveDragOffsets(IReadOnlyList<Vector2Int> coordinates)
    {
        List<Vector2Int> offsets = new();
        for (int i = 1; i < coordinates.Count; i++)
            offsets.Add(coordinates[i] - coordinates[i - 1]);
        return offsets;
    }

    private void ConfirmSelectionReservation(int selectedGridIndex)
    {
        if (!CanConfirmReservation() || !CanUseRangeData())
            return;

        List<int> rangeIndices = BattleRangeCalculator.GetSelectionRangeIndices(
            selectedGridIndex,
            BattleEquipmentEffectService.GetEffectiveRangeId(currentUserRuntime, currentSkillData),
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetSelectionAreaResult(
            currentCasterDirection,
            selectedGridIndex,
            rangeIndices
        );

        bool confirmed = ConfirmCommand(command);
        KeepSkillListOpenForThisClick();

        if (!confirmed || !RefreshContinuousGridSelection())
        {
            ClearPreview();
            return;
        }

        StartCoroutine(RestoreSelectionRangeAfterReservation(selectedGridIndex));
    }

    private IEnumerator RestoreSelectionRangeAfterReservation(int selectedGridIndex)
    {
        // 예약 완료 순간 기존 범위를 한 번 지우고, 다음 프레임에 같은 칸의 범위를 다시 표시합니다.
        // 마우스를 움직이지 않아도 연속 등록 가능한 범위를 바로 확인할 수 있습니다.
        if (rangePreview != null)
            rangePreview.ClearRangeOnly();

        yield return null;

        if (!IsGeneralSelectionSkillActive() ||
            !currentGeneralSelectionSelectableIndices.Contains(selectedGridIndex))
        {
            yield break;
        }

        ShowSelectionRangeAt(selectedGridIndex);
    }

    private void ConfirmAllRangeReservation()
    {
        if (!CanConfirmReservation() || gridManager == null)
            return;

        List<int> rangeIndices = BattleRangeCalculator.GetAllGridIndices(gridManager);
        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);

        if (currentSkillData.RangeType == RangeType.Selection)
        {
            command.SetSelectionAreaResult(
                currentCasterDirection,
                currentCasterGridIndex,
                rangeIndices
            );
        }
        else
        {
            command.SetDirectionResult(
                currentCasterDirection,
                rangeIndices,
                rangeIndices
            );
        }

        bool confirmed = ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();

        if (confirmed)
            RestoreHoveredSkillRangeAfterReservation();
    }

    private static bool IsAllRangeSkill(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(userRuntime, skillData);
        return BattleRangeCalculator.IsAllRangeId(rangeId);
    }

    private void ConfirmDirectionReservation(BattleDirection direction)
    {
        if (!CanConfirmReservation())
            return;

        int rangeOriginGridIndex = GetDirectionalAttackPreviewOriginGridIndex(
            currentCasterGridIndex,
            direction,
            currentSkillData);

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            rangeOriginGridIndex,
            BattleEquipmentEffectService.GetEffectiveRangeId(currentUserRuntime, currentSkillData),
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetDirectionResult(direction, rangeIndices, rangeIndices);

        bool confirmed = ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();

        if (confirmed)
            RestoreHoveredSkillRangeAfterReservation();
    }

    private int GetDirectionalAttackPreviewOriginGridIndex(
        int casterGridIndex,
        BattleDirection direction,
        SkillMasterData skillData)
    {
        if (gridManager == null || casterGridIndex < 0 || skillData == null)
            return casterGridIndex;

        if (TryBuildForcedDirectionalMovePreview(
                currentUserRuntime,
                skillData,
                casterGridIndex,
                direction,
                currentSlotIndex,
                out _,
                out int forcedDestinationGridIndex))
        {
            return forcedDestinationGridIndex;
        }

        if (!TryGetDirectionalMoveBeforeFirstDamage(skillData, out int signedDistance))
            return casterGridIndex;

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int forward = direction == BattleDirection.Right
            ? Vector2Int.right
            : Vector2Int.left;
        Vector2Int previewCoord = casterCoord + forward * signedDistance;

        // 예약/호버 단계에서는 전진이 성공한다고 가정한 위치에서 공격 범위를 보여줍니다.
        // 실제 실행 시에는 BattleActionRunner가 이동 성공/실패 후 실제 위치에서 범위를 다시 계산합니다.
        if (!gridManager.IsValidCoord(previewCoord))
            return casterGridIndex;

        return gridManager.CoordToIndex(previewCoord);
    }

    private static bool TryGetDirectionalMoveBeforeFirstDamage(
        SkillMasterData skillData,
        out int signedDistance)
    {
        signedDistance = 0;

        if (skillData == null || skillData.RangeType != RangeType.Direction)
            return false;

        if (skillData.EffectEntries != null && skillData.EffectEntries.Count > 0)
        {
            for (int i = 0; i < skillData.EffectEntries.Count; i++)
            {
                SkillEffectEntry entry = skillData.EffectEntries[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                    continue;

                string effectId = entry.EffectId.Trim();

                if (IsDamageEffectForPreview(effectId))
                    return false;

                if (string.Equals(effectId, "E_Move", StringComparison.Ordinal) &&
                    TryGetExplicitEffectValue(skillData, i, out signedDistance) &&
                    signedDistance != 0)
                {
                    return true;
                }
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(skillData.EffectIds))
            return false;

        string[] effectIds = skillData.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            string effectId = effectIds[i].Trim();

            if (IsDamageEffectForPreview(effectId))
                return false;

            if (string.Equals(effectId, "E_Move", StringComparison.Ordinal) &&
                TryGetExplicitEffectValue(skillData, i, out signedDistance) &&
                signedDistance != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetExplicitEffectValue(
        SkillMasterData skillData,
        int effectIndex,
        out int value)
    {
        value = 0;

        if (skillData == null ||
            effectIndex < 0 ||
            string.IsNullOrWhiteSpace(skillData.ValueRate))
        {
            return false;
        }

        string[] values = skillData.ValueRate.Split(';');

        if (effectIndex >= values.Length || string.IsNullOrWhiteSpace(values[effectIndex]))
            return false;

        return int.TryParse(values[effectIndex].Trim(), out value);
    }

    private static bool IsDamageEffectForPreview(string effectId)
    {
        return string.Equals(effectId, "E_Strike", StringComparison.Ordinal) ||
               string.Equals(effectId, "E_Pierce", StringComparison.Ordinal) ||
               string.Equals(effectId, "E_StrikeByTargetPoison", StringComparison.Ordinal);
    }

    private void ConfirmMoveReservation(
        int selectedGridIndex,
        IReadOnlyList<Vector2Int> explicitMoveOffsets = null)
    {
        if (!CanConfirmReservation())
            return;

        bool isCurrentCasterGridIndex = IsCurrentCasterGridIndex(selectedGridIndex);
        HashSet<int> blockedDestinationGridIndices = BuildKnownOtherPlayerDestinationGridIndices();

        if (!isCurrentCasterGridIndex &&
            blockedDestinationGridIndices.Contains(selectedGridIndex))
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.MoveDestinationOccupied));
            return;
        }

        List<List<Vector2Int>> pathCandidates = null;

        if (isCurrentCasterGridIndex)
        {
            pathCandidates = BuildSelfFlipMovePathCandidates();
        }
        else if (!currentMovePathCandidatesByTargetIndex.TryGetValue(
            selectedGridIndex,
            out pathCandidates))
        {
            pathCandidates = BuildPreferredMovePathCandidates(selectedGridIndex);
        }

        if (isCurrentCasterGridIndex)
        {
            List<PlayerReservedCommand> selfFlipCommands = BuildMoveReservationCommands(
                selectedGridIndex,
                new List<Vector2Int> { Vector2Int.zero }
            );

            if (selfFlipCommands.Count <= 0)
                return;

            bool selfFlipConfirmed = ConfirmCommands(selfFlipCommands);

            KeepSkillListOpenForThisClick();
            // A completed move is a single action selection. Do not keep move mode active.
            if (selfFlipConfirmed)
                ClearPreview();
            return;
        }

        List<Vector2Int> moveOffsets = explicitMoveOffsets != null
            ? new List<Vector2Int>(explicitMoveOffsets)
            : GetFirstReservableMovePath(pathCandidates);

        if (moveOffsets == null || moveOffsets.Count <= 0)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.MoveDestinationInvalid));
            return;
        }

        if (!CanReserveMovePathWithEffectiveCost(moveOffsets))
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.InsufficientMoveCost));
            return;
        }

        List<PlayerReservedCommand> commands = BuildMoveReservationCommands(
            selectedGridIndex,
            moveOffsets
        );

        if (commands.Count <= 0)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.MoveReservationFailed));
            return;
        }

        bool confirmed = ConfirmCommands(commands);

        KeepSkillListOpenForThisClick();
        // After a successful move reservation, exit move selection immediately.
        // An invalid/failed move keeps its selection so the player can retry.
        if (confirmed)
            ClearPreview();
    }

    private List<Vector2Int> GetFirstReservableMovePath(List<List<Vector2Int>> pathCandidates)
    {
        if (pathCandidates == null)
            return null;

        for (int i = 0; i < pathCandidates.Count; i++)
        {
            List<Vector2Int> path = pathCandidates[i];

            if (path == null || path.Count <= 0)
                continue;

            if (!CanReserveMovePathWithEffectiveCost(path))
                continue;

            return path;
        }

        return null;
    }

    private List<List<Vector2Int>> BuildPreferredMovePathCandidates(
        int targetGridIndex,
        ISet<int> currentBlockedGridIndices = null,
        ISet<int> projectedBlockedGridIndices = null)
    {
        List<List<Vector2Int>> result = new();

        currentBlockedGridIndices ??= BuildCurrentMoveBlockedGridIndices();
        projectedBlockedGridIndices ??= BuildProjectedMoveBlockedGridIndices();

        List<Vector2Int> path = ChooseReservableMovePath(
            currentCasterGridIndex,
            targetGridIndex,
            currentMoveDistancePerCommand,
            currentMoveReservationCapacity,
            gridManager,
            currentBlockedGridIndices,
            projectedBlockedGridIndices,
            true);

        if (path != null &&
            path.Count > 0 &&
            CanReserveMovePathWithEffectiveCost(path))
        {
            result.Add(path);
        }

        return result;
    }

    private bool CanReserveMovePathWithEffectiveCost(IReadOnlyList<Vector2Int> moveOffsets)
    {
        if (IsSelfFlipMovePath(moveOffsets))
            return true;

        int effectiveCost = GetEffectiveMoveReservationCost(moveOffsets);

        if (effectiveCost < 0)
            return false;

        return currentUserRuntime != null && currentUserRuntime.CanReserveCost(effectiveCost);
    }

    private int GetEffectiveMoveReservationCost(IReadOnlyList<Vector2Int> moveOffsets)
    {
        if (currentUserRuntime == null ||
            currentSkillData == null ||
            moveOffsets == null ||
            moveOffsets.Count <= 0)
        {
            return -1;
        }

        PlayerReservedCommand previewCommand =
            new PlayerReservedCommand(currentUserRuntime, currentSkillData);

        previewCommand.SetMoveReservationCost(
            GetMoveStepDistance(moveOffsets),
            currentMoveDistancePerCommand);

        EnsureTimelineController();

        if (timelineController != null && currentSlotIndex >= 0)
            timelineController.PreparePreviewCommandForReservation(currentSlotIndex, previewCommand);

        return previewCommand.Cost;
    }

    private static bool IsSelfFlipMovePath(IReadOnlyList<Vector2Int> moveOffsets)
    {
        return moveOffsets != null &&
               moveOffsets.Count == 1 &&
               moveOffsets[0] == Vector2Int.zero;
    }

    private int GetMoveReservationCapacity()
    {
        if (currentUserRuntime == null)
            return 0;

        return Mathf.Max(0, currentUserRuntime.PreviewCost);
    }

    private int GetMoveCommandSlotCapacity()
    {
        EnsureTimelineController();

        if (timelineController == null)
            return ReserveTurnSlotUI.MaxCommandCount;

        return timelineController.GetRemainingPlayerCommandCapacity(currentSlotIndex);
    }

    private int GetMoveDistancePerCommandForPreview(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData)
    {
        int distance = Mathf.Max(0, skillData != null ? skillData.GridMove : 0);

        if (distance > 0)
            return distance;

        if (skillData != null)
        {
            if (skillData.SkillId == MoveSkillLevelOneId)
                return 1;
        }

        if (DataManager.Instance == null ||
            DataManager.Instance.RangeDatabase == null ||
            skillData == null)
        {
            return 1;
        }

        string rangeId =
            BattleEquipmentEffectService.GetEffectiveRangeId(userRuntime, skillData);

        if (IsAllMoveRangeId(rangeId))
            return 1;

        return 1;
    }

    private int GetMoveDistancePerCommand()
    {
        int distance = Mathf.Max(0, currentSkillData != null ? currentSkillData.GridMove : 0);

        if (distance > 0)
            return distance;

        if (currentSkillData != null)
        {
            if (currentSkillData.SkillId == MoveSkillLevelOneId)
                return 1;
        }

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null || currentSkillData == null)
            return 1;

        string rangeId =
            BattleEquipmentEffectService.GetEffectiveRangeId(currentUserRuntime, currentSkillData);

        if (IsAllMoveRangeId(rangeId))
            return 1;

        if (!DataManager.Instance.RangeDatabase.TryGet(rangeId, out SkillRangeData rangeData))
            return 1;

        if (rangeData == null || rangeData.Positions == null)
            return 1;

        for (int i = 0; i < rangeData.Positions.Count; i++)
        {
            Vector2Int offset = rangeData.Positions[i];
            distance = Mathf.Max(distance, GetMoveDistance(offset));
        }

        return Mathf.Max(1, distance);
    }

    private List<PlayerReservedCommand> BuildMoveReservationCommands(
        int selectedGridIndex,
        IReadOnlyList<Vector2Int> moveOffsets)
    {
        List<PlayerReservedCommand> commands = new();

        if (moveOffsets == null || moveOffsets.Count <= 0 || gridManager == null)
            return commands;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentCasterGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return commands;

        Vector2Int totalMoveOffset = GetTotalMoveOffset(moveOffsets);
        Vector2Int targetCoord = currentCoord + totalMoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            return new List<PlayerReservedCommand>();

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        if (targetGridIndex != selectedGridIndex)
            return new List<PlayerReservedCommand>();

        BattleDirection direction = GetDirectionAfterMoveSteps(
            currentCasterDirection,
            moveOffsets);

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetSelectionResult(
            direction,
            selectedGridIndex,
            new List<int> { selectedGridIndex },
            totalMoveOffset
        );
        command.SetMoveReservationCost(
            GetMoveStepDistance(moveOffsets),
            currentMoveDistancePerCommand
        );
        command.SetVisualMoveResult(
            selectedGridIndex,
            totalMoveOffset,
            moveOffsets
        );

        commands.Add(command);

        return commands;
    }

    private static Vector2Int GetTotalMoveOffset(IReadOnlyList<Vector2Int> moveSteps)
    {
        Vector2Int total = Vector2Int.zero;

        if (moveSteps == null)
            return total;

        for (int i = 0; i < moveSteps.Count; i++)
            total += moveSteps[i];

        return total;
    }

    private HashSet<int> BuildCurrentMoveBlockedGridIndices()
    {
        HashSet<int> blockedGridIndices = BuildCurrentCharacterMoveBlockedGridIndices();

        AddCurrentMonsterOccupiedGridIndices(blockedGridIndices);
        AddBlockedGridEffectIndices(blockedGridIndices);

        return blockedGridIndices;
    }

    private HashSet<int> BuildProjectedMoveBlockedGridIndices()
    {
        HashSet<int> blockedGridIndices = BuildCurrentCharacterMoveBlockedGridIndices();

        AddProjectedMonsterOccupiedGridIndices(blockedGridIndices);
        AddBlockedGridEffectIndices(blockedGridIndices);

        return blockedGridIndices;
    }

    private HashSet<int> BuildCurrentCharacterMoveBlockedGridIndices()
    {
        HashSet<int> blockedGridIndices = new();

        if (gridManager == null)
            return blockedGridIndices;

        string selfCharacterId = currentUserRuntime != null
            ? currentUserRuntime.CharacterId
            : null;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                int gridIndex = gridManager.CoordToIndex(new Vector2Int(x, y));

                if (BattleOccupancyService.IsOccupiedByCharacter(gridIndex, selfCharacterId))
                    blockedGridIndices.Add(gridIndex);
            }
        }

        return blockedGridIndices;
    }

    private void AddProjectedMonsterOccupiedGridIndices(HashSet<int> blockedGridIndices)
    {
        if (blockedGridIndices == null)
            return;

        EnsureTimelineController();

        if (timelineController == null || gridManager == null || currentSlotIndex < 0)
        {
            AddCurrentMonsterOccupiedGridIndices(blockedGridIndices);
            return;
        }

        BattleActionSimulationService simulationService = new(gridManager);
        bool includeCurrentSlotMonsterCommands =
            !BattleActionOrderUtility.HasSwift(currentUserRuntime);

        HashSet<int> projectedGridIndices =
            simulationService.GetProjectedMonsterOccupiedGridIndices(
                timelineController,
                currentSlotIndex,
                includeCurrentSlotMonsterCommands);

        if (projectedGridIndices == null || projectedGridIndices.Count <= 0)
        {
            AddCurrentMonsterOccupiedGridIndices(blockedGridIndices);
            return;
        }

        foreach (int gridIndex in projectedGridIndices)
        {
            if (IsValidMoveDestinationGridIndex(gridIndex))
                blockedGridIndices.Add(gridIndex);
        }
    }

    private void AddCurrentMonsterOccupiedGridIndices(HashSet<int> blockedGridIndices)
    {
        if (blockedGridIndices == null)
            return;

        MonsterUnit[] monsters = FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null)
                continue;

            if (monster.RuntimeData != null && monster.RuntimeData.IsDead)
                continue;

            IReadOnlyList<int> occupiedGridIndices = monster.OccupiedGridIndices;

            if (occupiedGridIndices == null)
                continue;

            for (int j = 0; j < occupiedGridIndices.Count; j++)
            {
                int gridIndex = occupiedGridIndices[j];

                if (IsValidMoveDestinationGridIndex(gridIndex))
                    blockedGridIndices.Add(gridIndex);
            }
        }
    }

    private void AddBlockedGridEffectIndices(HashSet<int> blockedGridIndices)
    {
        if (blockedGridIndices == null)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null || controller.State == null)
            return;

        IReadOnlyList<Relic.Gameplay.Battle.BattleGridEffectPlacement> placements =
            controller.State.GetPlacements();

        for (int i = 0; i < placements.Count; i++)
        {
            int gridIndex = placements[i].GridIndex;

            if (IsValidMoveDestinationGridIndex(gridIndex) && controller.IsBlocked(gridIndex))
                blockedGridIndices.Add(gridIndex);
        }
    }

    private BattleGridEffectController ResolveGridEffectController()
    {
        if (gridEffectController != null)
            return gridEffectController;

        gridEffectController = FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include
        );

        return gridEffectController;
    }

    private HashSet<int> BuildKnownOtherPlayerDestinationGridIndices()
    {
        HashSet<int> blockedGridIndices = new();

        string selfCharacterId = currentUserRuntime != null
            ? currentUserRuntime.CharacterId
            : null;

        EnsureTimelineController();
        AddKnownOtherPlayerDestinationsFromScene(blockedGridIndices, selfCharacterId);
        AddKnownOtherPlayerDestinationsFromPartyStore(blockedGridIndices, selfCharacterId);
        AddBlockedGridEffectIndices(blockedGridIndices);

        return blockedGridIndices;
    }

    private void AddKnownOtherPlayerDestinationsFromScene(
        HashSet<int> blockedGridIndices,
        string selfCharacterId)
    {
        if (blockedGridIndices == null)
            return;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            AddKnownOtherPlayerDestination(
                blockedGridIndices,
                character.RuntimeData,
                character.CurrentGridIndex,
                selfCharacterId
            );
        }
    }

    private void AddKnownOtherPlayerDestinationsFromPartyStore(
        HashSet<int> blockedGridIndices,
        string selfCharacterId)
    {
        if (blockedGridIndices == null || DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;

        if (partyStore == null || characterStore == null)
            return;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!characterStore.TryGet(characterId, out CharacterRuntimeData runtime))
                continue;

            int fallbackGridIndex = partyStore.GetCurrentGridIndex(i);

            if (fallbackGridIndex < 0)
                fallbackGridIndex = partyStore.GetSpawnGridIndex(i);

            AddKnownOtherPlayerDestination(
                blockedGridIndices,
                runtime,
                fallbackGridIndex,
                selfCharacterId
            );
        }
    }

    private void AddKnownOtherPlayerDestination(
        HashSet<int> blockedGridIndices,
        CharacterRuntimeData runtime,
        int fallbackGridIndex,
        string selfCharacterId)
    {
        if (blockedGridIndices == null || runtime == null)
            return;

        if (!string.IsNullOrWhiteSpace(selfCharacterId) &&
            runtime.CharacterId == selfCharacterId)
        {
            return;
        }

        int gridIndex = -1;

        if (timelineController != null && currentSlotIndex >= 0)
            gridIndex = timelineController.GetPreviewGridIndexAtSlotEnd(runtime, currentSlotIndex);

        if (gridIndex < 0)
            gridIndex = fallbackGridIndex;

        if (!IsValidMoveDestinationGridIndex(gridIndex))
            return;

        blockedGridIndices.Add(gridIndex);
    }

    private bool IsValidMoveDestinationGridIndex(int gridIndex)
    {
        if (gridIndex < 0)
            return false;

        if (gridManager == null)
            return true;

        return gridManager.IsValidCoord(gridManager.IndexToCoord(gridIndex));
    }

    private void ApplyVisualMovePath(List<PlayerReservedCommand> commands)
    {
        if (commands == null || commands.Count <= 1 || gridManager == null)
            return;

        Vector2Int visualCurrentCoord = gridManager.IndexToCoord(currentCasterGridIndex);

        for (int i = 0; i < commands.Count; i++)
        {
            PlayerReservedCommand current = commands[i];

            if (current == null)
                continue;

            Vector2Int visualOffset = current.MoveOffset;
            List<Vector2Int> visualMoveSteps = null;

            if (i + 1 < commands.Count)
            {
                PlayerReservedCommand next = commands[i + 1];

                if (CanMergeToDiagonal(current.MoveOffset, next.MoveOffset))
                {
                    visualOffset = current.MoveOffset + next.MoveOffset;
                    visualMoveSteps = new List<Vector2Int>
                    {
                        current.MoveOffset,
                        next.MoveOffset
                    };

                    next.SetSkipMoveVisual(true);

                    i++;
                }
            }

            Vector2Int visualTargetCoord = visualCurrentCoord + visualOffset;

            if (!gridManager.IsValidCoord(visualTargetCoord))
                continue;

            int visualTargetGridIndex = gridManager.CoordToIndex(visualTargetCoord);

            if (visualMoveSteps != null)
            {
                current.SetVisualMoveResult(
                    visualTargetGridIndex,
                    visualOffset,
                    visualMoveSteps
                );
            }

            visualCurrentCoord = visualTargetCoord;
        }
    }

    private bool CanMergeToDiagonal(Vector2Int a, Vector2Int b)
    {
        bool aHorizontal = a.x != 0 && a.y == 0;
        bool aVertical = a.x == 0 && a.y != 0;

        bool bHorizontal = b.x != 0 && b.y == 0;
        bool bVertical = b.x == 0 && b.y != 0;

        return (aHorizontal && bVertical) || (aVertical && bHorizontal);
    }

    public static List<int> GetMoveRangeIndices(
        int casterGridIndex,
        int maxMoveDistance,
        GridManager gridManager)
    {
        return GetMoveRangeIndices(casterGridIndex, maxMoveDistance, 1, gridManager);
    }

    public static List<int> GetMoveRangeIndices(
        int casterGridIndex,
        int reservationCapacity,
        int moveDistancePerCommand,
        GridManager gridManager)
    {
        List<int> result = new();

        if (gridManager == null || reservationCapacity <= 0)
            return result;

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);

        if (!gridManager.IsValidCoord(casterCoord))
            return result;

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                Vector2Int offset = coord - casterCoord;

                if (GetRequiredMoveReservationCount(offset, safeDistancePerCommand) > reservationCapacity)
                    continue;

                result.Add(gridManager.CoordToIndex(coord));
            }
        }

        return result;
    }

    public static int GetRequiredMoveReservationCount(
        Vector2Int moveOffset,
        int moveDistancePerCommand)
    {
        if (moveOffset == Vector2Int.zero)
            return 1;

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);
        return Mathf.CeilToInt(GetMoveDistance(moveOffset) / (float)safeDistancePerCommand);
    }

    public static int GetRequiredMoveReservationCount(
        IReadOnlyList<Vector2Int> moveSteps,
        int moveDistancePerCommand)
    {
        if (moveSteps == null || moveSteps.Count <= 0)
            return 0;

        if (moveSteps.Count == 1 && moveSteps[0] == Vector2Int.zero)
            return 1;

        int totalDistance = 0;

        for (int i = 0; i < moveSteps.Count; i++)
            totalDistance += GetMoveDistance(moveSteps[i]);

        if (totalDistance <= 0)
            return 0;

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);
        return Mathf.CeilToInt(totalDistance / (float)safeDistancePerCommand);
    }

    public static List<Vector2Int> BuildMoveReservationOffsets(
        Vector2Int moveOffset,
        int moveDistancePerCommand)
    {
        List<Vector2Int> result = new();

        if (moveOffset == Vector2Int.zero)
        {
            result.Add(Vector2Int.zero);
            return result;
        }

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);
        AddAxisMoveOffsets(result, moveOffset.x, safeDistancePerCommand, true);
        AddAxisMoveOffsets(result, moveOffset.y, safeDistancePerCommand, false);

        return result;
    }

    public static List<List<Vector2Int>> GetReservableMovePathCandidates(
        int casterGridIndex,
        int targetGridIndex,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> blockedGridIndices = null,
        bool allowBlockedTargetGridIndex = false)
    {
        List<List<Vector2Int>> result = new();

        if (gridManager == null || reservationCapacity <= 0)
            return result;

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

        if (!gridManager.IsValidCoord(casterCoord) ||
            !gridManager.IsValidCoord(targetCoord))
            return result;

        Vector2Int moveOffset = targetCoord - casterCoord;
        List<List<Vector2Int>> directPathCandidates =
            BuildMoveStepPathCandidates(moveOffset);

        for (int i = 0; i < directPathCandidates.Count; i++)
        {
            TryAddReservableMoveStepPath(
                result,
                casterGridIndex,
                directPathCandidates[i],
                moveDistancePerCommand,
                reservationCapacity,
                gridManager,
                blockedGridIndices,
                targetGridIndex,
                allowBlockedTargetGridIndex
            );
        }

        if (result.Count > 0)
            return result;

        List<Vector2Int> shortestPath = FindShortestReservableMoveStepPath(
            casterGridIndex,
            targetGridIndex,
            moveDistancePerCommand,
            reservationCapacity,
            gridManager,
            blockedGridIndices,
            allowBlockedTargetGridIndex
        );

        TryAddReservableMoveStepPath(
            result,
            casterGridIndex,
            shortestPath,
            moveDistancePerCommand,
            reservationCapacity,
            gridManager,
            blockedGridIndices,
            targetGridIndex,
            allowBlockedTargetGridIndex
        );

        return result;
    }

    public static List<Vector2Int> ChooseReservableMovePath(
        int casterGridIndex,
        int targetGridIndex,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> currentBlockedGridIndices,
        ISet<int> projectedBlockedGridIndices,
        bool allowBlockedTargetGridIndex = false)
    {
        List<Vector2Int> currentPath = GetFirstMovePath(
            GetReservableMovePathCandidates(
                casterGridIndex,
                targetGridIndex,
                moveDistancePerCommand,
                reservationCapacity,
                gridManager,
                currentBlockedGridIndices,
                allowBlockedTargetGridIndex));

        List<Vector2Int> projectedPath = GetFirstMovePath(
            GetReservableMovePathCandidates(
                casterGridIndex,
                targetGridIndex,
                moveDistancePerCommand,
                reservationCapacity,
                gridManager,
                projectedBlockedGridIndices,
                allowBlockedTargetGridIndex));

        if (currentPath == null)
            return projectedPath != null ? new List<Vector2Int>(projectedPath) : null;

        if (projectedPath == null)
            return new List<Vector2Int>(currentPath);

        int currentDistance = GetMoveStepDistance(currentPath);
        int projectedDistance = GetMoveStepDistance(projectedPath);

        return projectedDistance <= currentDistance
            ? new List<Vector2Int>(projectedPath)
            : new List<Vector2Int>(currentPath);
    }

    private static List<Vector2Int> GetFirstMovePath(List<List<Vector2Int>> pathCandidates)
    {
        if (pathCandidates == null)
            return null;

        for (int i = 0; i < pathCandidates.Count; i++)
        {
            List<Vector2Int> path = pathCandidates[i];

            if (path != null && path.Count > 0)
                return path;
        }

        return null;
    }

    private static void TryAddReservableMoveStepPath(
        List<List<Vector2Int>> result,
        int casterGridIndex,
        List<Vector2Int> path,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        int targetGridIndex,
        bool allowBlockedTargetGridIndex)
    {
        if (result == null || path == null || path.Count <= 0)
            return;

        if (GetRequiredMoveReservationCount(path, moveDistancePerCommand) > reservationCapacity)
            return;

        if (!IsMovePathReservable(
            casterGridIndex,
            path,
            gridManager,
            blockedGridIndices,
            targetGridIndex,
            allowBlockedTargetGridIndex))
        {
            return;
        }

        for (int i = 0; i < result.Count; i++)
        {
            if (IsSamePath(result[i], path))
                return;
        }

        result.Add(new List<Vector2Int>(path));
    }

    private static List<List<Vector2Int>> BuildMoveStepPathCandidates(Vector2Int moveOffset)
    {
        List<List<Vector2Int>> candidates = new();

        if (moveOffset == Vector2Int.zero)
        {
            candidates.Add(new List<Vector2Int> { Vector2Int.zero });
            return candidates;
        }

        List<Vector2Int> xFirst = new();
        AddUnitAxisMoveSteps(xFirst, moveOffset.x, true);
        AddUnitAxisMoveSteps(xFirst, moveOffset.y, false);

        List<Vector2Int> yFirst = new();
        AddUnitAxisMoveSteps(yFirst, moveOffset.y, false);
        AddUnitAxisMoveSteps(yFirst, moveOffset.x, true);

        if (xFirst.Count > 0)
            candidates.Add(xFirst);

        if (yFirst.Count > 0 && !IsSamePath(xFirst, yFirst))
            candidates.Add(yFirst);

        return candidates;
    }

    private static void AddUnitAxisMoveSteps(
        List<Vector2Int> result,
        int amount,
        bool horizontal)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            Vector2Int offset = horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step);

            result.Add(offset);
            remaining -= step;
        }
    }

    private static List<Vector2Int> FindShortestReservableMoveStepPath(
        int casterGridIndex,
        int targetGridIndex,
        int moveDistancePerCommand,
        int reservationCapacity,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        bool allowBlockedTargetGridIndex)
    {
        if (gridManager == null || reservationCapacity <= 0)
            return null;

        if (casterGridIndex == targetGridIndex)
            return new List<Vector2Int> { Vector2Int.zero };

        Vector2Int casterCoord = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGridIndex);

        if (!gridManager.IsValidCoord(casterCoord) ||
            !gridManager.IsValidCoord(targetCoord))
        {
            return null;
        }

        if (!allowBlockedTargetGridIndex &&
            blockedGridIndices != null &&
            blockedGridIndices.Contains(targetGridIndex))
        {
            return null;
        }

        int maxStepCount = Mathf.Max(1, moveDistancePerCommand) * reservationCapacity;
        Queue<int> open = new();
        Dictionary<int, int> parentByIndex = new();
        Dictionary<int, Vector2Int> stepByIndex = new();
        Dictionary<int, int> distanceByIndex = new();

        open.Enqueue(casterGridIndex);
        parentByIndex[casterGridIndex] = -1;
        distanceByIndex[casterGridIndex] = 0;

        while (open.Count > 0)
        {
            int currentIndex = open.Dequeue();
            int currentDistance = distanceByIndex[currentIndex];

            if (currentDistance >= maxStepCount)
                continue;

            Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
            List<Vector2Int> directions = GetOrderedMoveDirections(currentCoord, targetCoord);

            for (int i = 0; i < directions.Count; i++)
            {
                Vector2Int step = directions[i];
                Vector2Int nextCoord = currentCoord + step;

                if (!gridManager.IsValidCoord(nextCoord))
                    continue;

                int nextIndex = gridManager.CoordToIndex(nextCoord);

                if (parentByIndex.ContainsKey(nextIndex))
                    continue;

                bool isBlockedTarget =
                    allowBlockedTargetGridIndex &&
                    nextIndex == targetGridIndex;

                if (blockedGridIndices != null &&
                    blockedGridIndices.Contains(nextIndex) &&
                    !isBlockedTarget)
                {
                    continue;
                }

                parentByIndex[nextIndex] = currentIndex;
                stepByIndex[nextIndex] = step;
                distanceByIndex[nextIndex] = currentDistance + 1;

                if (nextIndex == targetGridIndex)
                    return ReconstructMoveStepPath(targetGridIndex, parentByIndex, stepByIndex);

                open.Enqueue(nextIndex);
            }
        }

        return null;
    }

    private static List<Vector2Int> ReconstructMoveStepPath(
        int targetGridIndex,
        Dictionary<int, int> parentByIndex,
        Dictionary<int, Vector2Int> stepByIndex)
    {
        List<Vector2Int> reversedPath = new();
        int currentIndex = targetGridIndex;

        while (parentByIndex.TryGetValue(currentIndex, out int parentIndex) &&
               parentIndex >= 0)
        {
            reversedPath.Add(stepByIndex[currentIndex]);
            currentIndex = parentIndex;
        }

        reversedPath.Reverse();
        return reversedPath;
    }

    private static List<Vector2Int> GetOrderedMoveDirections(
        Vector2Int currentCoord,
        Vector2Int targetCoord)
    {
        List<Vector2Int> directions = new();
        Vector2Int delta = targetCoord - currentCoord;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            AddPreferredHorizontalDirection(directions, delta.x);
            AddPreferredVerticalDirection(directions, delta.y);
        }
        else
        {
            AddPreferredVerticalDirection(directions, delta.y);
            AddPreferredHorizontalDirection(directions, delta.x);
        }

        AddDirectionIfMissing(directions, Vector2Int.right);
        AddDirectionIfMissing(directions, Vector2Int.left);
        AddDirectionIfMissing(directions, Vector2Int.up);
        AddDirectionIfMissing(directions, Vector2Int.down);

        return directions;
    }

    private static void AddPreferredHorizontalDirection(List<Vector2Int> directions, int deltaX)
    {
        if (deltaX > 0)
            AddDirectionIfMissing(directions, Vector2Int.right);
        else if (deltaX < 0)
            AddDirectionIfMissing(directions, Vector2Int.left);
    }

    private static void AddPreferredVerticalDirection(List<Vector2Int> directions, int deltaY)
    {
        if (deltaY > 0)
            AddDirectionIfMissing(directions, Vector2Int.up);
        else if (deltaY < 0)
            AddDirectionIfMissing(directions, Vector2Int.down);
    }

    private static void AddDirectionIfMissing(List<Vector2Int> directions, Vector2Int direction)
    {
        for (int i = 0; i < directions.Count; i++)
        {
            if (directions[i] == direction)
                return;
        }

        directions.Add(direction);
    }

    private static bool IsMovePathReservable(
        int casterGridIndex,
        List<Vector2Int> path,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        int targetGridIndex,
        bool allowBlockedTargetGridIndex)
    {
        if (gridManager == null || path == null || path.Count <= 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(casterGridIndex);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int offset = path[i];

            if (offset == Vector2Int.zero)
                continue;

            if (!TryApplyReservableMoveStep(
                ref currentCoord,
                offset,
                gridManager,
                blockedGridIndices,
                targetGridIndex,
                allowBlockedTargetGridIndex,
                i == path.Count - 1))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryApplyReservableMoveStep(
        ref Vector2Int currentCoord,
        Vector2Int offset,
        GridManager gridManager,
        ISet<int> blockedGridIndices,
        int targetGridIndex,
        bool allowBlockedTargetGridIndex,
        bool isLastPathOffset)
    {
        if (gridManager == null)
            return false;

        if (offset.x != 0 && offset.y != 0)
            return false;

        int stepCount = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);

        if (stepCount <= 0)
            return true;

        Vector2Int unitStep = Vector2Int.zero;

        if (offset.x != 0)
            unitStep.x = offset.x > 0 ? 1 : -1;
        else
            unitStep.y = offset.y > 0 ? 1 : -1;

        for (int step = 0; step < stepCount; step++)
        {
            currentCoord += unitStep;

            if (!gridManager.IsValidCoord(currentCoord))
                return false;

            int gridIndex = gridManager.CoordToIndex(currentCoord);

            bool isBlockedTarget =
                allowBlockedTargetGridIndex &&
                isLastPathOffset &&
                step == stepCount - 1 &&
                gridIndex == targetGridIndex;

            if (blockedGridIndices != null &&
                blockedGridIndices.Contains(gridIndex) &&
                !isBlockedTarget)
            {
                return false;
            }
        }

        return true;
    }

    private static void AddAxisMoveOffsets(
        List<Vector2Int> result,
        int amount,
        int moveDistancePerCommand,
        bool horizontal)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int stepMagnitude = Mathf.Min(Mathf.Abs(remaining), moveDistancePerCommand);
            int step = remaining > 0 ? stepMagnitude : -stepMagnitude;
            Vector2Int offset = horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step);

            result.Add(offset);
            remaining -= step;
        }
    }

    private static int GetMoveDistance(Vector2Int moveOffset)
    {
        return Mathf.Abs(moveOffset.x) + Mathf.Abs(moveOffset.y);
    }

    private static int GetMoveStepDistance(IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null)
            return 0;

        int total = 0;

        for (int i = 0; i < moveSteps.Count; i++)
            total += GetMoveDistance(moveSteps[i]);

        return total;
    }

    private static bool IsAllMoveRangeId(string rangeId)
    {
        return rangeId == "Range_All" || rangeId == "Rnage_All";
    }

    private static BattleDirection GetDirectionAfterMove(
        BattleDirection currentDirection,
        Vector2Int moveOffset)
    {
        if (moveOffset.x < 0)
            return BattleDirection.Left;

        if (moveOffset.x > 0)
            return BattleDirection.Right;

        if (moveOffset == Vector2Int.zero)
            return GetOppositeDirection(currentDirection);

        return currentDirection;
    }

    private static BattleDirection GetDirectionAfterMoveSteps(
        BattleDirection currentDirection,
        IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null || moveSteps.Count <= 0)
            return currentDirection;

        BattleDirection direction = currentDirection;

        for (int i = 0; i < moveSteps.Count; i++)
            direction = GetDirectionAfterMove(direction, moveSteps[i]);

        return direction;
    }

    private BattleDirection GetDirectionFromMove(int casterGridIndex, int selectedGridIndex)
    {
        Vector2Int caster = gridManager.IndexToCoord(casterGridIndex);
        Vector2Int selected = gridManager.IndexToCoord(selectedGridIndex);

        return GetDirectionAfterMove(currentCasterDirection, selected - caster);
    }

    private static BattleDirection GetOppositeDirection(BattleDirection direction)
    {
        return direction == BattleDirection.Right
            ? BattleDirection.Left
            : BattleDirection.Right;
    }

    private void ConfirmDirectReservation()
    {
        if (!CanConfirmReservation())
            return;

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);

        bool confirmed = ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();

        if (confirmed)
            RestoreHoveredSkillRangeAfterReservation();
    }


    private bool RefreshContinuousGridSelection()
    {
        if (currentUserRuntime == null ||
            currentSkillData == null ||
            currentSkillData.RangeType != RangeType.Selection ||
            currentSlotIndex < 0)
        {
            return false;
        }

        EnsureTimelineController();

        if (timelineController == null ||
            timelineController.GetRemainingPlayerCommandCapacity(currentSlotIndex) <= 0 ||
            !CanReserveCurrentSkillAgain())
        {
            return false;
        }

        HideMoveHoverPing();
        RefreshCurrentCasterStateFromTimelinePreview();

        bool isMoveSkill = IsMoveSkill(currentSkillData);
        SetGridTargetMonsterVisualActive(true, isMoveSkill);

        if (rangePreview != null)
            rangePreview.Clear();

        if (isMoveSkill)
        {
            PreviewMoveSelectableCells();
            return currentMoveSelectableIndices.Count > 0;
        }

        PreviewGeneralSelectionSelectableCells();
        return currentGeneralSelectionSelectableIndices.Count > 0;
    }

    private bool CanReserveCurrentSkillAgain()
    {
        if (currentUserRuntime == null || currentSkillData == null || currentUserRuntime.IsDead)
            return false;

        PlayerReservedCommand previewCommand =
            new PlayerReservedCommand(currentUserRuntime, currentSkillData);

        EnsureTimelineController();
        if (timelineController != null && currentSlotIndex >= 0)
            timelineController.PreparePreviewCommandForReservation(currentSlotIndex, previewCommand);

        return currentUserRuntime.PreviewHP > previewCommand.HPCost &&
               currentUserRuntime.PreviewCost >= previewCommand.Cost &&
               currentUserRuntime.PreviewResource >= previewCommand.ResourceCost &&
               currentUserRuntime.PreviewShield >= previewCommand.ShieldCost;
    }

    public void CancelSelectionWhenHoveringDifferentSkill(
        CharacterRuntimeData runtimeData,
        SkillMasterData hoveredSkillData)
    {
        if (currentSkillData == null ||
            currentSkillData.RangeType != RangeType.Selection ||
            hoveredSkillData == null)
        {
            return;
        }

        if (runtimeData != null &&
            currentUserRuntime != null &&
            runtimeData.CharacterId != currentUserRuntime.CharacterId)
        {
            return;
        }

        if (IsMoveSkill(hoveredSkillData))
            return;

        if (currentSkillData.SkillId == hoveredSkillData.SkillId)
            return;

        ClearPreview();
    }

    private bool ConfirmCommand(PlayerReservedCommand command)
    {
        EnsureTimelineController();

        if (timelineController == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.TimelineControllerMissing));
            return false;
        }

        bool confirmed = timelineController.ConfirmPlayerCommand(currentSlotIndex, command);

        if (confirmed)
        {
            ShowTemporaryMonsterHUDsForCommand(command);
            PlayReservationConfirmSfx();
        }

        return confirmed;
    }

    private bool ConfirmCommands(IReadOnlyList<PlayerReservedCommand> commands)
    {
        EnsureTimelineController();

        if (timelineController == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.TimelineControllerMissing));
            return false;
        }

        bool confirmed = timelineController.ConfirmPlayerCommands(currentSlotIndex, commands);

        if (confirmed)
        {
            ShowTemporaryMonsterHUDsForCommands(commands);
            PlayReservationConfirmSfx();
        }

        return confirmed;
    }

    private void PlayReservationConfirmSfx()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(
            reservationConfirmSfxId,
            reservationConfirmSfxVolume
        );
    }

    private void ShowTemporaryMonsterHUDsForCommands(IReadOnlyList<PlayerReservedCommand> commands)
    {
        if (commands == null)
            return;

        for (int i = 0; i < commands.Count; i++)
            ShowTemporaryMonsterHUDsForCommand(commands[i]);
    }

    private void ShowTemporaryMonsterHUDsForCommand(PlayerReservedCommand command)
    {
        if (command == null)
            return;

        if (command.ReservedMoveGridIndex >= 0)
            return;

        if (command.RangeGridIndices == null || command.RangeGridIndices.Count <= 0)
            return;

        MonsterUnit.ShowTemporaryHUDsInRange(command.RangeGridIndices, 1f);
    }

    private bool CanConfirmReservation()
    {
        if (currentUserRuntime == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.CharacterNotSelected));
            return false;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SkillReservationMissing));
            return false;
        }

        if (currentSlotIndex < 0)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SelectTimelineSlotFirst));
            return false;
        }

        return CanUseRangeData();
    }

    private bool CanUseRangeData()
    {
        if (gridManager == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.BattleGridMissing));
            return false;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SkillReservationMissing));
            return false;
        }

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null)
        {
            ShowBattleWarning(GameLocalization.Get(LocalizationKeys.Warning.SkillRangeMissing));
            return false;
        }

        return true;
    }

    private bool isGridTargetMonsterDimVisualActive;

    private void SetGridTargetMonsterVisualActive(bool active, bool dimVisual = true)
    {
        if (isGridTargetMonsterVisualActive == active &&
            (!active || isGridTargetMonsterDimVisualActive == dimVisual))
        {
            return;
        }

        isGridTargetMonsterVisualActive = active;
        isGridTargetMonsterDimVisualActive = active && dimVisual;

        MonsterUnit.SetAllReservationVisualState(
            active,
            isGridTargetMonsterDimVisualActive);
    }

    private bool IsMoveSkill(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        if (skillData.Category == Category.Move)
            return true;

        if (skillData.TimelineNotation == TimelineActionType.Move)
            return true;

        return skillData.SkillId == MoveSkillLevelOneId;
    }

    private int FindCurrentCharacterGridIndex(CharacterRuntimeData userRuntime)
    {
        if (userRuntime == null)
            return -1;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (character.RuntimeData.CharacterId != userRuntime.CharacterId)
                continue;

            return character.CurrentGridIndex;
        }

        return -1;
    }


    public bool IsSkillSelectionActive()
    {
        return currentSkillData != null;
    }

    public bool IsGridSelectionActive()
    {
        return currentSkillData != null &&
               currentSkillData.RangeType == RangeType.Selection;
    }

    public bool IsMoveSkillSelectionActive()
    {
        return currentSkillData != null && IsMoveSkill(currentSkillData);
    }

    public Color GetHighlightColorForSkill(SkillMasterData skillData)
    {
        return GetHighlightColor(skillData);
    }

    private Color GetHighlightColor(SkillMasterData skillData)
    {
        if (skillData == null)
            return skillHighlightColor;

        if (IsMoveSkill(skillData))
            return moveHighlightColor;

        switch (skillData.SkillType)
        {
            case SkillType.Buff:
                return powerHighlightColor;

            case SkillType.Attack:
                return attackHighlightColor;

            case SkillType.Debuff:
                return skillHighlightColor;

            default:
                return skillHighlightColor;
        }
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    public bool IsGridSelectionActiveFor(
        CharacterRuntimeData runtimeData,
        SkillMasterData skillData)
    {
        if (runtimeData == null || skillData == null || currentUserRuntime == null || currentSkillData == null)
            return false;

        if (currentSkillData.RangeType != RangeType.Selection)
            return false;

        return runtimeData.CharacterId == currentUserRuntime.CharacterId &&
               skillData.SkillId == currentSkillData.SkillId;
    }

    public void ClearPreview()
    {
        moveDragPathState = null;
        moveDragActive = false;
        HideMoveHoverPing();
        HideMovePathPreview();
        SetGridTargetMonsterVisualActive(false);

        currentUserRuntime = null;
        currentSkillData = null;
        currentSlotIndex = -1;
        currentCasterGridIndex = -1;
        currentCasterDirection = BattleDirection.Right;
        currentCasterSprite = null;
        currentMoveSelectableIndices.Clear();
        currentGeneralSelectionSelectableIndices.Clear();
        currentMovePathCandidatesByTargetIndex.Clear();
        currentMoveDistancePerCommand = 1;
        currentMoveReservationCapacity = 1;

        if (rangePreview != null)
            rangePreview.Clear();
    }

    public static List<List<Vector2Int>> BuildMoveReservationPathCandidates(
    Vector2Int moveOffset,
    int moveDistancePerCommand)
    {
        List<List<Vector2Int>> candidates = new();

        if (moveOffset == Vector2Int.zero)
        {
            candidates.Add(new List<Vector2Int> { Vector2Int.zero });
            return candidates;
        }

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);

        List<Vector2Int> xFirst = new();
        AddAxisMoveOffsets(xFirst, moveOffset.x, safeDistancePerCommand, true);
        AddAxisMoveOffsets(xFirst, moveOffset.y, safeDistancePerCommand, false);

        List<Vector2Int> yFirst = new();
        AddAxisMoveOffsets(yFirst, moveOffset.y, safeDistancePerCommand, false);
        AddAxisMoveOffsets(yFirst, moveOffset.x, safeDistancePerCommand, true);

        if (xFirst.Count > 0)
            candidates.Add(xFirst);

        if (yFirst.Count > 0 && !IsSamePath(xFirst, yFirst))
            candidates.Add(yFirst);

        return candidates;
    }

    private static bool IsSamePath(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a == null || b == null)
            return false;

        if (a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }
}
