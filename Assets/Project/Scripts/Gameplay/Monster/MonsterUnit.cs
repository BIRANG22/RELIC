using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class MonsterUnit : MonoBehaviour
    {
        public MonsterRuntimeData RuntimeData { get; private set; }

        private MonsterAIBase ai;
        private MonsterHUDSlot hud;

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

            gameObject.name =
                $"{runtimeData.Name}_{runtimeData.RuntimeId}";
        }

        public void BindHUD(MonsterHUDSlot hud)
        {
            this.hud = hud;

            if (this.hud != null)
            {
                this.hud.Bind(RuntimeData);
                this.hud.SetFollowTarget(transform);
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