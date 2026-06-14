using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Relic.Gameplay.Monster
{
    public class MonsterUnit : MonoBehaviour
    {
        [Header("Click Collider")]
        [SerializeField] private bool autoAddClickCollider2D = true;
        [SerializeField] private Vector2 fallbackColliderSize = new Vector2(1f, 1f);

        public MonsterRuntimeData RuntimeData { get; private set; }

        private MonsterAIBase ai;
        private MonsterHUDSlot hud;
        private Collider2D clickCollider2D;

        private static MonsterUnit selectedMonster;

        private readonly List<int> occupiedGridIndices = new();
        public IReadOnlyList<int> OccupiedGridIndices => occupiedGridIndices;

        public bool IsSelected => selectedMonster == this;

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

        public void Initialize(MonsterRuntimeData runtimeData)
        {
            RuntimeData = runtimeData;
            ai = MonsterAIFactory.Create(runtimeData.MonsterId);
            EnsureClickCollider2D();

            gameObject.name =
                $"{runtimeData.Name}_{runtimeData.RuntimeId}";
        }

        private void OnMouseDown()
        {
            if (RuntimeData == null)
                return;

            if (IsPointerOverUI())
                return;

            SelectThisMonster();
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

        public void SelectThisMonster()
        {
            if (selectedMonster != null && selectedMonster != this)
                selectedMonster.SetSelected(false);

            selectedMonster = this;

            StartCoroutine(ShowSelectedHUDNextFrame());

            Debug.Log($"[MonsterUnit] Selected: {RuntimeData?.Name} / {RuntimeData?.RuntimeId}");
        }

        private IEnumerator ShowSelectedHUDNextFrame()
        {
            yield return null;

            SetSelected(true);
        }

        public void SetSelected(bool selected)
        {
            if (hud == null)
                return;

            if (selected)
                hud.Show();
            else
                hud.Hide();
        }

        public string SelectSkill(BattleContext context)
        {
            if (ai == null)
            {
                Debug.LogWarning($"[MonsterUnit] AI ¾øÀ½: {RuntimeData.MonsterId}");
                return null;
            }

            return ai.SelectSkill(RuntimeData, context);
        }

        public int MainGridIndex
        {
            get
            {
                if (occupiedGridIndices.Count <= 0)
                    return -1;

                return occupiedGridIndices[0];
            }
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

        public Vector2Int SelectMoveOffset(
            BattleContext context,
            GridManager gridManager,
            int moveAmount)
        {
            if (ai == null)
                return Vector2Int.left * moveAmount;

            return ai.SelectMoveOffset(this, context, gridManager, moveAmount);
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
    }
}
