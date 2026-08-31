using Relic.Gameplay.Battle;
using System;
using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Monster
{
    public class MonsterUnit : MonoBehaviour
    {
        [Header("Click Collider")]
        [SerializeField] private bool autoAddClickCollider2D = true;
        [SerializeField] private Vector2 fallbackColliderSize = new Vector2(1f, 1f);
        [Header("Timeline Hover Highlight")]
        [SerializeField] private GameObject timelineHoverHighlightObject;

        [Header("Mouse Hover Attack Range")]
        [SerializeField] private bool showAttackRangeOnHover = true;

        [Header("Reservation Visual")]
        [SerializeField] private bool dimMonsterDuringMoveTargetSelection = true;
        [SerializeField, Range(0f, 1f)] private float reservationAlpha = 0.45f;
        [SerializeField] private bool disableInteractionDuringReservationVisual = true;

        [Header("Status Hover Tooltip")]
        [FormerlySerializedAs("showStatusTooltipOnClick")]
        [SerializeField] private bool showStatusTooltipOnHover = true;
        [SerializeField] private UnitStatusEffectTooltipUI statusTooltipUI;

        [Header("Effect HUD")]
        [SerializeField, Min(0f)] private float effectHudVisibleDuration = 1.5f;

        private bool isStatusTooltipHovering;

        public MonsterRuntimeData RuntimeData { get; private set; }

        private MonsterAIBase ai;
        private bool aiEnabled = true;
        private MonsterHUDSlot hud;
        private Collider2D clickCollider2D;
        private Coroutine temporaryHUDRoutine;
        private bool isTemporaryHUDVisible;
        private MaterialPropertyBlock reservationPropertyBlock;
        private bool reservationVisualActive;
        private readonly Dictionary<SpriteRenderer, float> originalSpriteRendererAlphas = new();
        private RangePreview hoverRangePreview;
        private GridManager hoverGridManager;
        private PlayerSkillReservationController reservationController;
        private bool isAttackRangePreviewVisible;

        private static MonsterUnit selectedMonster;
        private static MonsterUnit infoSelectedMonster;
        private static int selectedMonsterClickFrame = -1000;

        public static event Action<MonsterUnit> MonsterInfoSelectionChanged;
        public static MonsterUnit CurrentInfoSelectedMonster => infoSelectedMonster;

        private readonly List<int> occupiedGridIndices = new();
        public IReadOnlyList<int> OccupiedGridIndices => occupiedGridIndices;

        public bool IsSelected => selectedMonster == this;

        public int MainGridIndex
        {
            get
            {
                if (occupiedGridIndices.Count <= 0)
                    return -1;

                return occupiedGridIndices[0];
            }
        }

        public void Initialize(MonsterRuntimeData runtimeData)
        {
            RuntimeData = runtimeData;

            if (RuntimeData != null)
                ai = MonsterAIFactory.Create(RuntimeData.MonsterId);

            EnsureClickCollider2D();

            RefreshRuntimeDisplayName();
        }


        public void RefreshRuntimeDisplayName()
        {
            if (RuntimeData == null)
                return;

            string displayName = RuntimeData.GetDisplayName();
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = RuntimeData.MonsterId;

            gameObject.name = $"{displayName}_{RuntimeData.RuntimeId}";

            if (hud != null)
                hud.Refresh();
        }

        public MonsterAIPlan CreateAIPlan(
            BattleContext context,
            GridManager gridManager)
        {
            if (!aiEnabled)
                return new MonsterAIPlan();

            if (ai == null)
            {
                Debug.LogWarning($"[MonsterUnit] AI 없음: {RuntimeData?.MonsterId}");
                return new MonsterAIPlan();
            }

            return ai.CreatePlan(this, context, gridManager);
        }

        public void SetAIEnabled(bool enabled)
        {
            aiEnabled = enabled;
        }

        public string SelectSkill(BattleContext context)
        {
            if (ai == null)
            {
                Debug.LogWarning($"[MonsterUnit] AI 없음: {RuntimeData?.MonsterId}");
                return null;
            }

            return ai.SelectSkill(RuntimeData, context);
        }

        public void DestroyHUD()
        {
            if (hud != null)
            {
                Destroy(hud.gameObject);
                hud = null;
            }
        }

        public Vector2Int SelectMoveOffset(
            BattleContext context,
            GridManager gridManager,
            int moveAmount)
        {
            if (ai == null)
                return Vector2Int.left * moveAmount;

            return ai.SelectMoveOffset(this, context, gridManager, moveAmount);
        }

        public void SetTimelineHoverHighlight(bool active)
        {
            if (timelineHoverHighlightObject != null)
                timelineHoverHighlightObject.SetActive(active);
        }

        public void SetOccupiedCells(List<int> cells)
        {
            occupiedGridIndices.Clear();

            if (cells == null)
                return;

            for (int i = 0; i < cells.Count; i++)
            {
                if (!occupiedGridIndices.Contains(cells[i]))
                    occupiedGridIndices.Add(cells[i]);
            }
        }

        public bool ContainsGridIndex(int gridIndex)
        {
            return occupiedGridIndices.Contains(gridIndex);
        }

        public void MoveOccupiedCells(Vector2Int moveOffset, GridManager gridManager)
        {
            if (gridManager == null)
                return;

            for (int i = 0; i < occupiedGridIndices.Count; i++)
            {
                Vector2Int coord = gridManager.IndexToCoord(occupiedGridIndices[i]);
                Vector2Int moved = coord + moveOffset;

                occupiedGridIndices[i] = gridManager.CoordToIndex(moved);
            }
        }

        private void OnDisable()
        {
            HideAttackRangePreview();
            HideTemporaryHUD();

            if (selectedMonster == this)
            {
                SetSelected(false);
                selectedMonster = null;
            }

            if (infoSelectedMonster == this)
            {
                infoSelectedMonster = null;
                MonsterInfoSelectionChanged?.Invoke(null);
            }

            HideStatusHoverTooltip();
        }

        private void OnDestroy()
        {
            HideAttackRangePreview();
            HideTemporaryHUD();

            if (selectedMonster == this)
            {
                SetSelected(false);
                selectedMonster = null;
            }

            if (infoSelectedMonster == this)
            {
                infoSelectedMonster = null;
                MonsterInfoSelectionChanged?.Invoke(null);
            }

            HideStatusHoverTooltip();
        }

        private void Update()
        {
            UpdateStatusHoverTooltipPosition();

            if (selectedMonster != this)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            if (Time.frameCount <= selectedMonsterClickFrame + 1)
                return;

            if (IsPointerOverUI())
                return;

            if (IsScreenPointOverAnyMonster(Input.mousePosition))
                return;

            if (infoSelectedMonster == this)
                ClearMonsterInfoSelection();
            else
                DeselectCurrentMonster();
        }

        private void UpdateStatusHoverTooltipPosition()
        {
            if (!isStatusTooltipHovering || statusTooltipUI == null)
                return;

            UnitStatusEffectTooltipSide tooltipSide = GetStatusTooltipSide();
            statusTooltipUI.UpdatePosition(GetStatusTooltipScreenPosition(tooltipSide), tooltipSide);
        }

        private void ShowStatusHoverTooltip()
        {
            if (!showStatusTooltipOnHover)
                return;

            if (RuntimeData == null || RuntimeData.StatusEffects == null || RuntimeData.StatusEffects.Count <= 0)
            {
                HideStatusHoverTooltip();
                return;
            }

            if (statusTooltipUI == null)
                statusTooltipUI = UnitStatusEffectTooltipUI.GetOrCreate();

            if (statusTooltipUI == null)
                return;

            UnitStatusEffectTooltipSide tooltipSide = GetStatusTooltipSide();
            Vector2 screenPosition = GetStatusTooltipScreenPosition(tooltipSide);
            statusTooltipUI.Show(this, RuntimeData.StatusEffects, screenPosition, tooltipSide);
        }

        private UnitStatusEffectTooltipSide GetStatusTooltipSide()
        {
            // 몬스터 본체 호버 툴팁은 항상 몬스터 Collider2D의 오른쪽에 표시합니다.
            return UnitStatusEffectTooltipSide.Right;
        }

        private Vector2 GetStatusTooltipScreenPosition(UnitStatusEffectTooltipSide side)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return Input.mousePosition;

            Bounds bounds;
            Collider2D collider = GetClickCollider2D();
            if (collider != null)
                bounds = collider.bounds;
            else if (!TryGetRendererBounds(out bounds))
                bounds = new Bounds(transform.position, Vector3.zero);

            float anchorX = bounds.max.x;
            Vector3 worldPosition = new Vector3(anchorX, bounds.center.y, bounds.center.z);
            return mainCamera.WorldToScreenPoint(worldPosition);
        }

        private void HideStatusHoverTooltip()
        {
            isStatusTooltipHovering = false;

            if (statusTooltipUI == null)
                return;

            statusTooltipUI.Hide(this);
        }

        private void OnMouseDown()
        {
            if (UIPanelButton.IsMenuPanelOpen)
                return;

            if (reservationVisualActive)
                return;

            if (RuntimeData == null || RuntimeData.IsDead)
                return;

            if (IsPointerOverUI())
                return;

            MonsterUnit previousInfoSelectedMonster = infoSelectedMonster;

            SelectThisMonster();

            if (previousInfoSelectedMonster != null &&
                previousInfoSelectedMonster != this)
            {
                previousInfoSelectedMonster.SetSelected(false);
                previousInfoSelectedMonster.HideAttackRangePreview();
                previousInfoSelectedMonster.HideStatusHoverTooltip();
            }

            infoSelectedMonster = this;
            BattleTimelineController.ClearCurrentCharacterSelection();

            BattleCameraController cameraController = BattleCameraController.Instance;
            if (cameraController != null)
                cameraController.FocusOnCharacterSelection(transform, MainGridIndex);

            // 클릭으로 선택한 몬스터는 마우스가 빠져도 HUD와 행동 범위를 유지합니다.
            SetSelected(true);
            ShowAttackRangePreview();

            MonsterInfoSelectionChanged?.Invoke(this);
        }

        private void OnMouseEnter()
        {
            if (UIPanelButton.IsMenuPanelOpen)
                return;

            if (reservationVisualActive)
                return;

            if (RuntimeData == null || RuntimeData.IsDead)
                return;

            if (IsPointerOverUI())
                return;

            if (infoSelectedMonster == null || infoSelectedMonster == this)
            {
                SelectThisMonster();
            }
            else if (hud != null)
            {
                // 다른 몬스터가 클릭 선택되어 있어도 현재 호버한 몬스터의 HUD는 임시로 함께 표시합니다.
                hud.Show();
            }

            ShowAttackRangePreview();
            isStatusTooltipHovering = true;
            ShowStatusHoverTooltip();
        }

        private void OnMouseExit()
        {
            isStatusTooltipHovering = false;
            HideStatusHoverTooltip();

            if (UIPanelButton.IsMenuPanelOpen)
                return;

            if (reservationVisualActive)
                return;

            // 클릭 선택된 몬스터는 호버가 끝나도 HUD와 행동 범위를 유지합니다.
            if (infoSelectedMonster == this)
            {
                SetSelected(true);
                ShowAttackRangePreview();
                return;
            }

            HideAttackRangePreview();

            if (infoSelectedMonster != null)
            {
                if (!isTemporaryHUDVisible && hud != null)
                    hud.Hide();

                // 다른 몬스터를 잠깐 호버했다면 클릭 선택된 몬스터의 범위를 다시 표시합니다.
                infoSelectedMonster.SetSelected(true);
                infoSelectedMonster.ShowAttackRangePreview();
                return;
            }

            if (selectedMonster != this)
                return;

            DeselectCurrentMonster();
        }


        private void ShowAttackRangePreview()
        {
            HideAttackRangePreview();

            if (!showAttackRangeOnHover || RuntimeData == null || RuntimeData.IsDead)
                return;

            if (string.IsNullOrWhiteSpace(RuntimeData.AttackRangeId))
                return;

            FindHoverRangeReferences();

            if (hoverRangePreview == null || hoverGridManager == null)
                return;

            if (reservationController != null && reservationController.IsSkillSelectionActive())
                return;

            if (DataManager.Instance == null || DataManager.Instance.RangeDatabase == null)
                return;

            bool facingRight = RuntimeData.Direction == BattleDirection.Right;
            List<int> rangeIndices = MonsterSkillRangeService.BuildRangeGridIndices(
                this,
                RuntimeData.AttackRangeId,
                hoverGridManager,
                facingRight,
                MainGridIndex,
                DataManager.Instance.RangeDatabase);

            if (rangeIndices.Count <= 0)
                return;

            hoverRangePreview.ShowRangeCells(rangeIndices);
            isAttackRangePreviewVisible = true;
        }

        private void HideAttackRangePreview()
        {
            if (!isAttackRangePreviewVisible)
                return;

            if (hoverRangePreview != null)
                hoverRangePreview.ClearRangeOnly();

            isAttackRangePreviewVisible = false;
        }

        private void FindHoverRangeReferences()
        {
            if (hoverRangePreview == null)
                hoverRangePreview = UnityEngine.Object.FindFirstObjectByType<RangePreview>(FindObjectsInactive.Include);

            if (hoverGridManager == null)
                hoverGridManager = UnityEngine.Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);

            if (reservationController == null)
            {
                reservationController = UnityEngine.Object.FindFirstObjectByType<PlayerSkillReservationController>(
                    FindObjectsInactive.Include);
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
                return false;

            if (EventSystem.current.IsPointerOverGameObject())
                return true;

            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

            return false;
        }

        private bool IsScreenPointOverAnyMonster(Vector2 screenPoint)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return false;

            Ray ray = mainCamera.ScreenPointToRay(screenPoint);
            RaycastHit2D[] rayHits = Physics2D.GetRayIntersectionAll(ray);

            for (int i = 0; i < rayHits.Length; i++)
            {
                Collider2D hitCollider = rayHits[i].collider;
                if (hitCollider == null)
                    continue;

                if (hitCollider.GetComponentInParent<MonsterUnit>() != null)
                    return true;
            }

            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPoint);
            Vector2 overlapPoint = new Vector2(worldPoint.x, worldPoint.y);
            Collider2D[] overlapHits = Physics2D.OverlapPointAll(overlapPoint);

            for (int i = 0; i < overlapHits.Length; i++)
            {
                Collider2D hitCollider = overlapHits[i];
                if (hitCollider == null)
                    continue;

                if (hitCollider.GetComponentInParent<MonsterUnit>() != null)
                    return true;
            }

            return false;
        }

        public void ShowAttackRangePreviewFromTimeline()
        {
            ShowAttackRangePreview();
        }

        public void HideAttackRangePreviewFromTimeline()
        {
            if (infoSelectedMonster == this)
            {
                ShowAttackRangePreview();
                return;
            }

            HideAttackRangePreview();

            if (infoSelectedMonster != null && infoSelectedMonster != this)
                infoSelectedMonster.ShowAttackRangePreview();
        }

        public void SelectForInfoFromTimeline()
        {
            if (UIPanelButton.IsMenuPanelOpen)
                return;

            if (reservationVisualActive)
                return;

            if (RuntimeData == null || RuntimeData.IsDead)
                return;

            MonsterUnit previousInfoSelectedMonster = infoSelectedMonster;

            SelectThisMonster();

            if (previousInfoSelectedMonster != null &&
                previousInfoSelectedMonster != this)
            {
                previousInfoSelectedMonster.SetSelected(false);
                previousInfoSelectedMonster.HideAttackRangePreview();
                previousInfoSelectedMonster.HideStatusHoverTooltip();
            }

            infoSelectedMonster = this;
            BattleTimelineController.ClearCurrentCharacterSelection();

            BattleCameraController cameraController = BattleCameraController.Instance;
            if (cameraController != null)
                cameraController.FocusOnCharacterSelection(transform, MainGridIndex);

            SetSelected(true);
            ShowAttackRangePreview();
            MonsterInfoSelectionChanged?.Invoke(this);
        }

        public static void ClearMonsterInfoSelection()
        {
            if (infoSelectedMonster == null)
                return;

            MonsterUnit previous = infoSelectedMonster;
            infoSelectedMonster = null;

            previous.HideAttackRangePreview();

            if (selectedMonster == previous)
                DeselectCurrentMonster();
            else
                previous.SetSelected(false);

            MonsterInfoSelectionChanged?.Invoke(null);
        }

        private static void DeselectCurrentMonster()
        {
            if (selectedMonster == null)
                return;

            MonsterUnit monster = selectedMonster;
            selectedMonster = null;

            monster.SetSelected(false);
            monster.HideStatusHoverTooltip();
        }

        private void EnsureClickCollider2D()
        {
            clickCollider2D = GetComponentInChildren<Collider2D>();

            if (clickCollider2D != null || !autoAddClickCollider2D)
                return;

            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = false;

            if (TryGetRendererBounds(out Bounds bounds))
            {
                Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
                Vector3 lossyScale = transform.lossyScale;

                float scaleX = Mathf.Approximately(lossyScale.x, 0f) ? 1f : Mathf.Abs(lossyScale.x);
                float scaleY = Mathf.Approximately(lossyScale.y, 0f) ? 1f : Mathf.Abs(lossyScale.y);

                boxCollider.offset = new Vector2(localCenter.x, localCenter.y);
                boxCollider.size = new Vector2(
                    Mathf.Max(0.01f, bounds.size.x / scaleX),
                    Mathf.Max(0.01f, bounds.size.y / scaleY)
                );
            }
            else
            {
                boxCollider.offset = Vector2.zero;
                boxCollider.size = fallbackColliderSize;
            }

            clickCollider2D = boxCollider;
        }

        private bool TryGetRendererBounds(out Bounds bounds)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
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

            return hasBounds;
        }

        public Collider2D GetClickCollider2D()
        {
            if (clickCollider2D == null)
                EnsureClickCollider2D();

            return clickCollider2D;
        }

        public void BindHUD(MonsterHUDSlot hud)
        {
            this.hud = hud;

            if (this.hud != null)
            {
                this.hud.Bind(RuntimeData);
                this.hud.SetFollowTarget(transform, GetClickCollider2D());
                this.hud.Hide();
            }
        }

        public void HideHUDIfNotSelected()
        {
            if (IsSelected)
                return;

            if (hud != null)
                hud.Hide();
        }

        public void SetReservationVisualState(bool reservation)
        {
            SetReservationVisualState(reservation, true);
        }

        public void SetReservationVisualState(bool reservation, bool dimVisual)
        {
            reservationVisualActive = reservation;

            if (reservation)
            {
                if (selectedMonster == this)
                    DeselectCurrentMonster();

                HideTemporaryHUD();
                HideStatusHoverTooltip();
            }

            float alpha = reservation && dimVisual && dimMonsterDuringMoveTargetSelection ? reservationAlpha : 1f;

            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];

                if (spriteRenderer == null)
                    continue;

                if (!originalSpriteRendererAlphas.TryGetValue(spriteRenderer, out float originalAlpha))
                {
                    originalAlpha = spriteRenderer.color.a;
                    originalSpriteRendererAlphas.Add(spriteRenderer, originalAlpha);
                }

                Color color = spriteRenderer.color;
                color.a = reservation && dimVisual && dimMonsterDuringMoveTargetSelection
                    ? originalAlpha * reservationAlpha
                    : originalAlpha;
                spriteRenderer.color = color;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null || renderer is SpriteRenderer)
                    continue;

                SetRendererAlpha(renderer, alpha);
            }

            Collider2D collider = GetClickCollider2D();
            if (collider != null)
                collider.enabled = !reservation || !disableInteractionDuringReservationVisual;
        }

        public static void SetAllReservationVisualState(bool reservation)
        {
            SetAllReservationVisualState(reservation, true);
        }

        public static void SetAllReservationVisualState(bool reservation, bool dimVisual)
        {
            MonsterUnit[] monsters =
                UnityEngine.Object.FindObjectsByType<MonsterUnit>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] != null)
                    monsters[i].SetReservationVisualState(reservation, dimVisual);
            }
        }

        private void SetRendererAlpha(Renderer renderer, float alpha)
        {
            Material sharedMaterial = renderer.sharedMaterial;

            if (sharedMaterial == null)
                return;

            string colorProperty = null;

            if (sharedMaterial.HasProperty("_BaseColor"))
                colorProperty = "_BaseColor";
            else if (sharedMaterial.HasProperty("_Color"))
                colorProperty = "_Color";

            if (string.IsNullOrEmpty(colorProperty))
                return;

            if (reservationPropertyBlock == null)
                reservationPropertyBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(reservationPropertyBlock);

            Color color = sharedMaterial.GetColor(colorProperty);
            color.a = alpha;
            reservationPropertyBlock.SetColor(colorProperty, color);

            renderer.SetPropertyBlock(reservationPropertyBlock);
        }

        public void SelectThisMonster()
        {
            if (selectedMonster != null && selectedMonster != this)
            {
                selectedMonster.SetSelected(false);
                selectedMonster.HideStatusHoverTooltip();
            }

            selectedMonster = this;
            selectedMonsterClickFrame = Time.frameCount;

            SetSelected(true);
        }

        public void SetSelected(bool selected)
        {
            if (hud == null)
                return;

            if (selected)
            {
                hud.Show();
            }
            else if (!isTemporaryHUDVisible)
            {
                hud.Hide();
            }
        }

        public void ShowHUD()
        {
            if (hud == null)
                return;

            hud.Show();
        }

        public void RefreshHUD()
        {
            if (hud == null)
                return;

            hud.Refresh();
        }

        public void ShowAndRefreshHUD()
        {
            if (hud == null)
                return;

            hud.Show();
            hud.Refresh();
        }

        public void ShowTemporaryHUD(float duration)
        {
            if (hud == null)
                return;

            if (temporaryHUDRoutine != null)
            {
                StopCoroutine(temporaryHUDRoutine);
                temporaryHUDRoutine = null;
            }

            isTemporaryHUDVisible = true;
            ShowAndRefreshHUD();

            if (duration > 0f)
                temporaryHUDRoutine = StartCoroutine(HideTemporaryHUDAfterDelay(duration));
        }

        public void ShowTemporaryHUDForEffect()
        {
            ShowTemporaryHUD(Mathf.Max(1.5f, effectHudVisibleDuration));
        }

        public void HideTemporaryHUD()
        {
            if (temporaryHUDRoutine != null)
            {
                StopCoroutine(temporaryHUDRoutine);
                temporaryHUDRoutine = null;
            }

            isTemporaryHUDVisible = false;

            if (!IsSelected && hud != null)
                hud.Hide();
        }

        public static void ShowTemporaryHUDsInRange(
            IReadOnlyCollection<int> rangeGridIndices,
            float duration)
        {
            HideAllTemporaryHUDs();

            if (rangeGridIndices == null || rangeGridIndices.Count <= 0)
                return;

            HashSet<int> rangeSet = rangeGridIndices as HashSet<int> ??
                                    new HashSet<int>(rangeGridIndices);

            MonsterUnit[] monsters =
                UnityEngine.Object.FindObjectsByType<MonsterUnit>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < monsters.Length; i++)
            {
                MonsterUnit monster = monsters[i];

                if (monster == null || monster.RuntimeData == null)
                    continue;

                if (monster.RuntimeData.IsDead)
                    continue;

                if (!monster.OccupiesAnyGrid(rangeSet))
                    continue;

                monster.ShowTemporaryHUD(duration);
            }
        }

        public static void HideAllTemporaryHUDs()
        {
            MonsterUnit[] monsters =
                UnityEngine.Object.FindObjectsByType<MonsterUnit>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] != null)
                    monsters[i].HideTemporaryHUD();
            }
        }

        private IEnumerator HideTemporaryHUDAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);

            temporaryHUDRoutine = null;
            HideTemporaryHUD();
        }

        private bool OccupiesAnyGrid(ISet<int> rangeGridIndices)
        {
            if (rangeGridIndices == null || rangeGridIndices.Count <= 0)
                return false;

            for (int i = 0; i < occupiedGridIndices.Count; i++)
            {
                if (rangeGridIndices.Contains(occupiedGridIndices[i]))
                    return true;
            }

            return false;
        }
    }
}
