using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
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
                this.hud.Hide();
            }
        }

        private void OnMouseDown()
        {
            SelectThisMonster();
        }

        private void SelectThisMonster()
        {
            if (selectedMonster != null && selectedMonster != this)
                selectedMonster.SetSelected(false);

            selectedMonster = this;
            SetSelected(true);

            Debug.Log($"[MonsterUnit] Selected: {RuntimeData?.Name} / {RuntimeData?.RuntimeId}");
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
    }
}