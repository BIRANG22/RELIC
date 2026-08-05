using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSkillReservationController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BattleGridEffectController gridEffectController;
    [SerializeField] private RangePreview rangePreview;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleTimelineController timelineController;

    [Header("Skill List Panel")]
    [SerializeField] private SkillListPanel skillListPanel;
    [SerializeField] private bool keepSkillListOpenAfterReservationClick = true;
    [SerializeField] private int keepSkillListOpenIgnoreFrames = 1;

    [Header("Reservation SFX")]
    [Tooltip("스킬 또는 이동 행동이 타임라인에 정상 등록되었을 때 재생할 효과음입니다.")]
    [SerializeField] private SfxType reservationConfirmSfx = SfxType.SkillReserve;
    [Tooltip("행동 등록 효과음의 볼륨 배율입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float reservationConfirmSfxVolume = 1f;

    [Header("Range Highlight Material")]
    [Tooltip("이동 스킬의 선택 가능 그리드에만 사용하는 머테리얼입니다. 비워두면 기존 하이라이트 머테리얼을 사용합니다.")]
    [SerializeField] private Material moveHighlightMaterial;

    [Header("Move Hover Ping")]
    [Tooltip("이동 가능한 그리드에 마우스를 올렸을 때 고정해서 표시할 기본 핑 이미지입니다.")]
    [FormerlySerializedAs("moveHoverPingSprite")]
    [SerializeField] private Sprite moveHoverPingBaseSprite;
    [FormerlySerializedAs("moveHoverPingOffset")]
    [SerializeField] private Vector3 moveHoverPingBaseOffset = Vector3.zero;
    [Tooltip("기본 핑 이미지의 크기입니다. 1은 원본 크기입니다.")]
    [FormerlySerializedAs("moveHoverPingScale")]
    [Min(0f)]
    [SerializeField] private float moveHoverPingBaseScale = 0.5f;
    [FormerlySerializedAs("moveHoverPingSortingOrder")]
    [SerializeField] private int moveHoverPingBaseSortingOrder = 10;

    [Tooltip("기본 핑과 함께 표시되며 위아래로 둥둥 움직이는 보조 핑 이미지입니다.")]
    [SerializeField] private Sprite moveHoverPingFloatingSprite;
    [SerializeField] private Vector3 moveHoverPingFloatingOffset = Vector3.zero;
    [Tooltip("보조 핑 이미지의 크기입니다. 1은 원본 크기입니다.")]
    [Min(0f)]
    [SerializeField] private float moveHoverPingFloatingScale = 0.5f;
    [SerializeField] private int moveHoverPingFloatingSortingOrder = 11;
    [Tooltip("보조 핑이 기준 위치에서 위아래로 움직이는 거리입니다.")]
    [Min(0f)]
    [SerializeField] private float moveHoverPingFloatHeight = 0.15f;
    [Tooltip("보조 핑이 위아래로 움직이는 속도입니다.")]
    [Min(0f)]
    [SerializeField] private float moveHoverPingFloatSpeed = 2f;

    [Header("Move Hover Cost Text")]
    [Tooltip("이동 코스트 텍스트에 사용할 TMP 폰트입니다. 비워두면 TMP 기본 폰트를 사용합니다.")]
    [SerializeField] private TMP_FontAsset moveHoverCostFont;
    [SerializeField] private Vector3 moveHoverCostTextOffset = new Vector3(0f, 0.35f, 0f);
    [Min(0f)]
    [SerializeField] private float moveHoverCostFontSize = 4f;
    [SerializeField] private Color moveHoverCostTextColor = Color.white;
    [SerializeField] private int moveHoverCostSortingOrder = 12;


    [Header("Nocturn Portal Preview")]
    [Tooltip("녹턴이 포탈로 이동할 그리드에 표시할 이미지입니다.")]
    [SerializeField] private Sprite nocturnPortalIndicatorSprite;
    [Tooltip("그리드 중심에서 포탈 예고 이미지에 추가할 위치 오프셋입니다.")]
    [SerializeField] private Vector3 nocturnPortalIndicatorOffset = Vector3.zero;
    [Tooltip("포탈 예고 이미지의 크기입니다. 1은 원본 크기입니다.")]
    [Min(0f)]
    [SerializeField] private float nocturnPortalIndicatorScale = 1f;
    [Tooltip("포탈 예고 이미지의 정렬 순서 오프셋입니다.")]
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
    private SpriteRenderer moveHoverPingBaseInstance;
    private SpriteRenderer moveHoverPingFloatingInstance;
    private TextMeshPro moveHoverCostTextInstance;
    private int moveHoverPingGridIndex = -1;
    private Vector3 moveHoverPingFloatingBasePosition;
    private float moveHoverPingFloatStartTime;


    private sealed class NocturnPortalIndicatorEntry
    {
        public SpriteRenderer Renderer;
        public int ReferenceCount;
    }

    private readonly Dictionary<string, NocturnPortalIndicatorEntry> nocturnPortalIndicators = new();

    private int currentMoveDistancePerCommand = 1;
    private int currentMoveReservationCapacity = 1;

    private const string MoveSkillLevelOneId = "S_Move_1";
    private const string MoveSkillLevelTwoId = "S_Move_2";
    private const string GeneralSelectionRangeId = "Range_24";
    private const string MoveHoverPingSortingLayerName = "Unit";
    private const float MoveHoverPingYSortMultiplier = 100f;
    private const int MoveHoverPingDefaultSortingOffset = 10;
    private const int MoveHoverPingLegacyFrontSortingOrderThreshold = 1000;

    private void OnEnable()
    {
        if (gridManager != null)
        {
            gridManager.OnCellClicked += HandleCellClicked;
            gridManager.OnCellHovered += HandleCellHovered;
            gridManager.OnCellHoverExited += HandleCellHoverExited;
        }
    }

    private void OnDisable()
    {
        ClearNocturnPortalDestinationIndicators();
        HideMoveHoverPing();
        SetGridTargetMonsterVisualActive(false);

        if (gridManager != null)
        {
            gridManager.OnCellClicked -= HandleCellClicked;
            gridManager.OnCellHovered -= HandleCellHovered;
            gridManager.OnCellHoverExited -= HandleCellHoverExited;
        }
    }

    private void Update()
    {
        HandleGridSelectionCancelInput();
        UpdateMoveHoverPingFloatingAnimation();
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

        ClearPreview();
    }

    private void UpdateMoveHoverPingFloatingAnimation()
    {
        if (moveHoverPingFloatingInstance == null ||
            !moveHoverPingFloatingInstance.gameObject.activeSelf)
        {
            return;
        }

        float elapsedTime = Time.unscaledTime - moveHoverPingFloatStartTime;
        float floatingOffsetY = Mathf.Sin(elapsedTime * Mathf.Max(0f, moveHoverPingFloatSpeed)) *
                                Mathf.Max(0f, moveHoverPingFloatHeight);

        moveHoverPingFloatingInstance.transform.position =
            moveHoverPingFloatingBasePosition + Vector3.up * floatingOffsetY;
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

    public void ShowSkillHoverRangePreview(
        CharacterRuntimeData userRuntime,
        SkillMasterData skillData,
        int slotIndex)
    {
        if (rangePreview == null)
            return;

        rangePreview.ClearRangeOnly();

        if (userRuntime == null || skillData == null)
            return;

        if (IsMoveSkillSelectionActive())
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

        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(userRuntime, skillData);

        if (skillData.RangeType == RangeType.Direction)
        {
            rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
                casterGridIndex,
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

    public void ClearSkillHoverRangePreview()
    {
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
            ShowBattleWarning("선택된 캐릭터가 없습니다.");
            return;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
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
            SetGridTargetMonsterVisualActive(isMoveSkill);

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
                ShowBattleWarning("스킬 범위 정보를 찾을 수 없습니다.");
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
            ShowBattleWarning("선택 가능한 그리드 범위를 찾을 수 없습니다.");
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

            ShowBattleWarning("이동에 필요한 Cost가 부족합니다.");
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
            ShowBattleWarning("선택 가능한 칸이 없습니다.");

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
        if (cell == null)
            return;

        if (IsMoveSkillSelectionActive())
        {
            if (currentMoveSelectableIndices.Contains(cell.Index))
                ShowMoveHoverPing(cell.Index);
            else
                HideMoveHoverPing();

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
            if (cell == null || cell.Index == moveHoverPingGridIndex)
                HideMoveHoverPing();

            return;
        }

        if (!IsGeneralSelectionSkillActive())
            return;

        if (rangePreview != null)
            rangePreview.ClearRangeOnly();
    }

    private void ShowMoveHoverPing(int gridIndex)
    {
        if (gridManager == null ||
            (moveHoverPingBaseSprite == null && moveHoverPingFloatingSprite == null))
        {
            HideMoveHoverPing();
            return;
        }

        if (!currentMoveSelectableIndices.Contains(gridIndex))
        {
            HideMoveHoverPing();
            return;
        }

        Vector3 gridWorldPosition = gridManager.GetWorldPositionByIndex(gridIndex);

        if (moveHoverPingBaseSprite != null)
        {
            moveHoverPingBaseInstance = EnsureMoveHoverPingRenderer(
                moveHoverPingBaseInstance,
                "Move Hover Ping Base");

            moveHoverPingBaseInstance.sprite = moveHoverPingBaseSprite;
            ApplyMoveHoverYSort(
                moveHoverPingBaseInstance,
                gridWorldPosition.y,
                moveHoverPingBaseSortingOrder,
                0);
            moveHoverPingBaseInstance.transform.position = gridWorldPosition + moveHoverPingBaseOffset;
            moveHoverPingBaseInstance.transform.localScale =
                Vector3.one * Mathf.Max(0f, moveHoverPingBaseScale);
            moveHoverPingBaseInstance.gameObject.SetActive(true);
        }
        else if (moveHoverPingBaseInstance != null)
        {
            moveHoverPingBaseInstance.gameObject.SetActive(false);
        }

        if (moveHoverPingFloatingSprite != null)
        {
            moveHoverPingFloatingInstance = EnsureMoveHoverPingRenderer(
                moveHoverPingFloatingInstance,
                "Move Hover Ping Floating");

            moveHoverPingFloatingInstance.sprite = moveHoverPingFloatingSprite;
            ApplyMoveHoverYSort(
                moveHoverPingFloatingInstance,
                gridWorldPosition.y,
                moveHoverPingFloatingSortingOrder,
                1);
            moveHoverPingFloatingInstance.transform.localScale =
                Vector3.one * Mathf.Max(0f, moveHoverPingFloatingScale);

            moveHoverPingFloatingBasePosition = gridWorldPosition + moveHoverPingFloatingOffset;
            moveHoverPingFloatingInstance.transform.position = moveHoverPingFloatingBasePosition;
            moveHoverPingFloatStartTime = Time.unscaledTime;
            moveHoverPingFloatingInstance.gameObject.SetActive(true);
        }
        else if (moveHoverPingFloatingInstance != null)
        {
            moveHoverPingFloatingInstance.gameObject.SetActive(false);
        }

        ShowMoveHoverCostText(gridIndex, gridWorldPosition);
        moveHoverPingGridIndex = gridIndex;
    }

    private void ShowMoveHoverCostText(int gridIndex, Vector3 gridWorldPosition)
    {
        int moveCost = GetMoveHoverCost(gridIndex);

        if (moveCost < 0)
        {
            if (moveHoverCostTextInstance != null)
                moveHoverCostTextInstance.gameObject.SetActive(false);

            return;
        }

        moveHoverCostTextInstance = EnsureMoveHoverCostText();
        moveHoverCostTextInstance.text = moveCost.ToString();
        moveHoverCostTextInstance.fontSize = Mathf.Max(0f, moveHoverCostFontSize);
        moveHoverCostTextInstance.color = moveHoverCostTextColor;
        moveHoverCostTextInstance.transform.position = gridWorldPosition + moveHoverCostTextOffset;
        ApplyMoveHoverYSort(
            moveHoverCostTextInstance.renderer,
            gridWorldPosition.y,
            moveHoverCostSortingOrder,
            2);
        moveHoverCostTextInstance.gameObject.SetActive(true);
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

    private TextMeshPro EnsureMoveHoverCostText()
    {
        if (moveHoverCostTextInstance != null)
            return moveHoverCostTextInstance;

        GameObject textObject = new GameObject("Move Hover Cost Text");
        textObject.transform.SetParent(transform, false);

        moveHoverCostTextInstance = textObject.AddComponent<TextMeshPro>();
        moveHoverCostTextInstance.alignment = TextAlignmentOptions.Center;
        moveHoverCostTextInstance.textWrappingMode = TextWrappingModes.NoWrap;

        if (moveHoverCostFont != null)
            moveHoverCostTextInstance.font = moveHoverCostFont;

        return moveHoverCostTextInstance;
    }

    private SpriteRenderer EnsureMoveHoverPingRenderer(
        SpriteRenderer currentRenderer,
        string objectName)
    {
        if (currentRenderer != null)
            return currentRenderer;

        GameObject pingObject = new GameObject(objectName);
        pingObject.transform.SetParent(transform, false);
        return pingObject.AddComponent<SpriteRenderer>();
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

        if (moveHoverPingBaseInstance != null)
            moveHoverPingBaseInstance.gameObject.SetActive(false);

        if (moveHoverPingFloatingInstance != null)
            moveHoverPingFloatingInstance.gameObject.SetActive(false);

        if (moveHoverCostTextInstance != null)
            moveHoverCostTextInstance.gameObject.SetActive(false);
    }


    public void ShowNocturnPortalDestinationIndicator(string runtimeId, int destinationGridIndex)
    {
        if (gridManager == null || nocturnPortalIndicatorSprite == null || destinationGridIndex < 0)
            return;

        string key = BuildNocturnPortalIndicatorKey(runtimeId, destinationGridIndex);

        if (nocturnPortalIndicators.TryGetValue(key, out NocturnPortalIndicatorEntry existing) &&
            existing != null && existing.Renderer != null)
        {
            existing.ReferenceCount++;
            ApplyNocturnPortalIndicatorTransform(existing.Renderer, destinationGridIndex);
            existing.Renderer.gameObject.SetActive(true);
            return;
        }

        GameObject indicatorObject = new GameObject(
            $"Nocturn Portal Destination {runtimeId}_{destinationGridIndex}");
        indicatorObject.transform.SetParent(transform, false);

        SpriteRenderer renderer = indicatorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = nocturnPortalIndicatorSprite;
        ApplyNocturnPortalIndicatorTransform(renderer, destinationGridIndex);

        nocturnPortalIndicators[key] = new NocturnPortalIndicatorEntry
        {
            Renderer = renderer,
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

        if (entry.Renderer != null)
            Destroy(entry.Renderer.gameObject);
    }

    public void ClearNocturnPortalDestinationIndicators()
    {
        foreach (KeyValuePair<string, NocturnPortalIndicatorEntry> pair in nocturnPortalIndicators)
        {
            if (pair.Value != null && pair.Value.Renderer != null)
                Destroy(pair.Value.Renderer.gameObject);
        }

        nocturnPortalIndicators.Clear();
    }

    private void ApplyNocturnPortalIndicatorTransform(
        SpriteRenderer renderer,
        int destinationGridIndex)
    {
        if (renderer == null || gridManager == null)
            return;

        Vector3 gridWorldPosition = gridManager.GetWorldPositionByIndex(destinationGridIndex);
        renderer.sprite = nocturnPortalIndicatorSprite;
        renderer.transform.position = gridWorldPosition + nocturnPortalIndicatorOffset;
        renderer.transform.localScale = Vector3.one * Mathf.Max(0f, nocturnPortalIndicatorScale);
        ApplyMoveHoverYSort(
            renderer,
            gridWorldPosition.y,
            nocturnPortalIndicatorSortingOrder,
            3);
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
        if (cell == null || currentSkillData == null)
            return;

        if (currentSkillData.RangeType != RangeType.Selection)
            return;

        if (!IsMoveSkill(currentSkillData))
        {
            if (!currentGeneralSelectionSelectableIndices.Contains(cell.Index))
            {
                ShowBattleWarning("선택할 수 없는 칸입니다.");
                Debug.LogWarning($"[PlayerSkillReservationController] 일반 선택 스킬의 사용 가능 범위를 벗어났습니다: {cell.name}");
                return;
            }

            ConfirmSelectionReservation(cell.Index);
            return;
        }

        if (!currentMoveSelectableIndices.Contains(cell.Index))
        {
            ShowBattleWarning("선택할 수 없는 칸입니다.");
            Debug.LogWarning($"[PlayerSkillReservationController] 선택 가능한 이동 칸이 아닙니다: {cell.name}");
            return;
        }

        ConfirmMoveReservation(cell.Index);
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
            ClearPreview();
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

        ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();
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

        List<int> rangeIndices = BattleRangeCalculator.GetDirectionRangeIndices(
            currentCasterGridIndex,
            BattleEquipmentEffectService.GetEffectiveRangeId(currentUserRuntime, currentSkillData),
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        PlayerReservedCommand command = new PlayerReservedCommand(currentUserRuntime, currentSkillData);
        command.SetDirectionResult(direction, rangeIndices, rangeIndices);

        ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();
    }

    private void ConfirmMoveReservation(int selectedGridIndex)
    {
        if (!CanConfirmReservation())
            return;

        bool isCurrentCasterGridIndex = IsCurrentCasterGridIndex(selectedGridIndex);
        HashSet<int> blockedDestinationGridIndices = BuildKnownOtherPlayerDestinationGridIndices();

        if (!isCurrentCasterGridIndex &&
            blockedDestinationGridIndices.Contains(selectedGridIndex))
        {
            ShowBattleWarning("다른 캐릭터가 있는 위치로는 이동할 수 없습니다.");
            return;
        }

        List<List<Vector2Int>> pathCandidates;

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
            if (!selfFlipConfirmed || !RefreshContinuousGridSelection())
                ClearPreview();
            return;
        }

        List<Vector2Int> moveOffsets = GetFirstReservableMovePath(pathCandidates);

        if (moveOffsets == null || moveOffsets.Count <= 0)
        {
            ShowBattleWarning("이동할 수 없는 위치입니다.");
            return;
        }

        if (!CanReserveMovePathWithEffectiveCost(moveOffsets))
        {
            ShowBattleWarning("선택한 위치까지 이동할 Cost가 부족합니다.");
            return;
        }

        List<PlayerReservedCommand> commands = BuildMoveReservationCommands(
            selectedGridIndex,
            moveOffsets
        );

        if (commands.Count <= 0)
        {
            ShowBattleWarning("이동 예약을 만들 수 없습니다.");
            return;
        }

        bool confirmed = ConfirmCommands(commands);

        KeepSkillListOpenForThisClick();
        if (!confirmed || !RefreshContinuousGridSelection())
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
            if (skillData.SkillId == MoveSkillLevelTwoId)
                return 2;

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
            if (currentSkillData.SkillId == MoveSkillLevelTwoId)
                return 2;

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
            !BattleActionOrderUtility.HasSwift(currentSkillData);

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

        ConfirmCommand(command);
        KeepSkillListOpenForThisClick();
        ClearPreview();
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
        SetGridTargetMonsterVisualActive(isMoveSkill);

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
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
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
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
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
            reservationConfirmSfx,
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
            ShowBattleWarning("선택된 캐릭터가 없습니다.");
            return false;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (currentSlotIndex < 0)
        {
            ShowBattleWarning("타임라인 슬롯을 먼저 선택해주세요.");
            return false;
        }

        return CanUseRangeData();
    }

    private bool CanUseRangeData()
    {
        if (gridManager == null)
        {
            ShowBattleWarning("전투 그리드를 찾을 수 없습니다.");
            return false;
        }

        if (currentSkillData == null)
        {
            ShowBattleWarning("예약할 스킬 정보가 없습니다.");
            return false;
        }

        if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null)
        {
            ShowBattleWarning("스킬 범위 데이터를 찾을 수 없습니다.");
            return false;
        }

        return true;
    }

    private void SetGridTargetMonsterVisualActive(bool active)
    {
        if (isGridTargetMonsterVisualActive == active)
            return;

        isGridTargetMonsterVisualActive = active;
        MonsterUnit.SetAllReservationVisualState(active);
    }

    private bool IsMoveSkill(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        if (skillData.Category == Category.Move)
            return true;

        if (skillData.TimelineNotation == TimelineActionType.Move)
            return true;

        return skillData.SkillId == MoveSkillLevelOneId ||
               skillData.SkillId == MoveSkillLevelTwoId;
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

    public void ClearPreview()
    {
        HideMoveHoverPing();
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
