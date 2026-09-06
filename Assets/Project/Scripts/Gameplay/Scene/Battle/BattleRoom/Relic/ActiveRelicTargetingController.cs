using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class ActiveRelicTargetingController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BattleGridEffectController gridEffectController;
    [SerializeField] private Color targetPreviewColor = new(0.2f, 0.9f, 1f, 1f);
    [SerializeField, Range(0.05f, 1f)] private float gridEffectPreviewAlpha = 0.45f;

    private ActiveRelicService service;
    private SkillListPanel owner;
    private Action onTargetingSucceeded;
    private CharacterRuntimeData pendingRuntime;
    private ActiveRelicAvailability pendingAvailability;
    private bool isTargeting;
    private bool previousGridVisible;
    private string pendingGridEffectId;
    private GameObject gridEffectPreview;
    private int gridEffectPreviewIndex = -1;

    public bool IsTargeting => isTargeting;

    private void Update()
    {
        if (!isTargeting)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            CancelTargeting();
    }

    private void OnDisable()
    {
        CancelTargeting();
    }


    public bool BeginTargeting(
        ActiveRelicService activeRelicService,
        CharacterRuntimeData runtime,
        ActiveRelicAvailability availability,
        Action succeededCallback = null)
    {
        bool started = BeginTargeting(
            null,
            activeRelicService,
            runtime,
            availability);

        if (started)
            onTargetingSucceeded = succeededCallback;

        return started;
    }

    public bool BeginTargeting(
        SkillListPanel ownerPanel,
        ActiveRelicService activeRelicService,
        CharacterRuntimeData runtime,
        ActiveRelicAvailability availability)
    {
        EnsureReferences();

        if (gridManager == null ||
            activeRelicService == null ||
            runtime == null ||
            availability == null ||
            !availability.RequiresTarget)
        {
            return false;
        }

        CancelTargeting();

        owner = ownerPanel;
        service = activeRelicService;
        pendingRuntime = runtime;
        pendingAvailability = availability;
        previousGridVisible = gridManager.IsGridVisible;
        isTargeting = true;

        gridManager.SetGridVisible(true);
        gridManager.OnCellClicked += HandleCellClicked;

        pendingGridEffectId = ActiveRelicEffectResolver.ResolveGridEffectId(availability.RelicData);
        if (!string.IsNullOrWhiteSpace(pendingGridEffectId))
        {
            gridManager.OnCellHovered += HandleCellHovered;
            gridManager.OnCellHoverExited += HandleCellHoverExited;
        }

        gridManager.ShowExecutionRange(BuildPreviewCells(), targetPreviewColor);
        MonsterUnit.SetAllReservationVisualState(true);
        return true;
    }

    public void CancelTargeting()
    {
        if (!isTargeting && gridManager == null)
            return;

        bool wasTargeting = isTargeting;

        if (gridManager != null)
        {
            gridManager.OnCellClicked -= HandleCellClicked;
            gridManager.OnCellHovered -= HandleCellHovered;
            gridManager.OnCellHoverExited -= HandleCellHoverExited;
            gridManager.ClearExecutionRange();

            if (isTargeting)
                gridManager.SetGridVisible(previousGridVisible);
        }

        ClearGridEffectPreview();

        if (wasTargeting)
            MonsterUnit.SetAllReservationVisualState(false);

        isTargeting = false;
        owner = null;
        onTargetingSucceeded = null;
        service = null;
        pendingRuntime = null;
        pendingAvailability = null;
        pendingGridEffectId = null;
    }

    private void HandleCellHovered(GridCell cell)
    {
        ClearGridEffectPreview();

        if (!isTargeting ||
            cell == null ||
            string.IsNullOrWhiteSpace(pendingGridEffectId) ||
            gridEffectController == null)
        {
            return;
        }

        int gridIndex = cell.Index;

        if (BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex) ||
            gridEffectController.HasEffect(gridIndex) ||
            gridEffectController.IsBlocked(gridIndex))
        {
            return;
        }

        if (gridEffectController.TryCreatePlacementPreview(
                gridIndex,
                pendingGridEffectId,
                gridEffectPreviewAlpha,
                out gridEffectPreview))
        {
            gridEffectPreviewIndex = gridIndex;
        }
    }

    private void HandleCellHoverExited(GridCell cell)
    {
        if (cell == null || cell.Index == gridEffectPreviewIndex)
            ClearGridEffectPreview();
    }

    private void ClearGridEffectPreview()
    {
        gridEffectPreviewIndex = -1;

        if (gridEffectPreview == null)
            return;

        if (Application.isPlaying)
            Destroy(gridEffectPreview);
        else
            DestroyImmediate(gridEffectPreview);

        gridEffectPreview = null;
    }

    private void HandleCellClicked(GridCell cell)
    {
        if (!isTargeting || cell == null || service == null)
            return;

        owner?.IgnoreOutsideCloseForFrames(2);

        ActiveRelicUseResult result = service.TryUseTarget(
            pendingRuntime,
            cell.Index,
            gridManager,
            gridEffectController);

        if (result.Succeeded)
        {
            SkillListPanel targetOwner = owner;
            Action succeededCallback = onTargetingSucceeded;
            CancelTargeting();
            targetOwner?.Refresh();
            succeededCallback?.Invoke();
            return;
        }

        BattleWarningUI.ShowMessage(result.Message);
    }

    private List<int> BuildPreviewCells()
    {
        List<int> cells = new();

        if (gridManager == null || pendingAvailability == null)
            return cells;

        if (pendingAvailability.TargetMode == ActiveRelicTargetMode.AllyGrid)
        {
            AddAllyCells(cells);
            return cells;
        }

        if (pendingAvailability.TargetMode == ActiveRelicTargetMode.EnemyGrid)
        {
            AddEnemyCells(cells);
            return cells;
        }

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                int gridIndex = gridManager.CoordToIndex(new Vector2Int(x, y));
                cells.Add(gridIndex);
            }
        }

        return cells;
    }

    private void AddAllyCells(List<int> cells)
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null ||
                character.RuntimeData == null ||
                character.RuntimeData.IsDead ||
                character.CurrentGridIndex < 0)
            {
                continue;
            }

            if (character.CharacterId == pendingRuntime?.CharacterId)
                continue;

            cells.Add(character.CurrentGridIndex);
        }
    }

    private void AddEnemyCells(List<int> cells)
    {
        MonsterUnit[] monsters = FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null ||
                monster.RuntimeData == null ||
                monster.RuntimeData.IsDead ||
                monster.OccupiedGridIndices == null)
            {
                continue;
            }

            for (int j = 0; j < monster.OccupiedGridIndices.Count; j++)
                cells.Add(monster.OccupiedGridIndices[j]);
        }
    }

    private void EnsureReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);

        if (gridEffectController == null)
            gridEffectController = FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
    }
}
