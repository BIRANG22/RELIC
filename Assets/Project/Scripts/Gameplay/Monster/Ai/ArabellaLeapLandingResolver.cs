using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    // Adapter at the existing scene/runtime boundary. The policy receives only a snapshot.
    public static class ArabellaLeapLandingResolver
    {
        public static int Resolve(MonsterUnit caster, GridManager grid, int preferred = -1)
        {
            if (caster == null || grid == null || caster.MainGridIndex < 0) return -1;
            bool[] blocked = new bool[grid.Width * grid.Height];
            BattleGridEffectController effects = Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);
            for (int i = 0; i < blocked.Length; i++)
                blocked[i] = grid.GetCellByIndex(i) == null || (effects != null && effects.IsBlocked(i));

            foreach (MonsterUnit monster in Object.FindObjectsByType<MonsterUnit>(FindObjectsSortMode.None))
            {
                if (monster == caster || monster.RuntimeData?.IsDead == true) continue;
                foreach (int index in monster.OccupiedGridIndices)
                    if (index >= 0 && index < blocked.Length) blocked[index] = true;
            }

            List<ArabellaLeapLandingPolicy.Target> targets = new();
            foreach (BattleCharacter character in Object.FindObjectsByType<BattleCharacter>(FindObjectsSortMode.None))
            {
                int index = character.CurrentGridIndex;
                if (index < 0 || index >= blocked.Length) continue;
                blocked[index] = true;
                CharacterRuntimeData runtime = character.RuntimeData;
                if (runtime == null || runtime.IsDead) continue;
                bool bound = false;
                if (runtime.StatusEffects != null)
                    foreach (StatusEffectRuntimeData status in runtime.StatusEffects)
                        if (status != null && status.Stack > 0 && status.EffectId == "E_Bondage") { bound = true; break; }
                targets.Add(new ArabellaLeapLandingPolicy.Target(index,
                    runtime.Direction == BattleDirection.Right,
                    !bound && !BattleEquipmentEffectService.IsForcedMoveImmune(runtime)));
            }
            int[] followupHits = new int[blocked.Length];
            RangeDatabase ranges = DataManager.Instance?.RangeDatabase;
            string rangeId = DataManager.Instance?.MonsterSkillDatabase?.Get("S_Monster_35")?.RangeId;
            if (ranges != null && !string.IsNullOrWhiteSpace(rangeId))
            {
                for (int i = 0; i < blocked.Length; i++)
                {
                    if (blocked[i]) continue;
                    foreach (bool facingRight in new[] { false, true })
                    {
                        List<int> attack = MonsterSkillRangeService.BuildRangeGridIndices(
                            caster, rangeId, grid, facingRight, i, ranges);
                        int count = 0;
                        foreach (ArabellaLeapLandingPolicy.Target target in targets)
                            if (attack != null && attack.Contains(target.GridIndex)) count++;
                        followupHits[i] = Mathf.Max(followupHits[i], count);
                    }
                }
            }
            return ArabellaLeapLandingPolicy.Resolve(grid.Width, grid.Height, caster.MainGridIndex,
                caster.OccupiedGridIndices, blocked, targets, preferred, followupHits);
        }
    }
}
