using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public class MonsterUnit : MonoBehaviour
    {
        public MonsterRuntimeData RuntimeData { get; private set; }

        private MonsterAIBase ai;

        public void Initialize(MonsterRuntimeData runtimeData)
        {
            RuntimeData = runtimeData;

            ai = MonsterAIFactory.Create(runtimeData.MonsterId);

            gameObject.name =
                $"{runtimeData.Name}_{runtimeData.RuntimeId}";
        }

        public string SelectSkill(BattleContext context)
        {
            if (ai == null)
            {
                Debug.LogWarning(
                    $"[MonsterUnit] AI ¾øÀ½: {RuntimeData.MonsterId}"
                );

                return null;
            }

            return ai.SelectSkill(RuntimeData, context);
        }
    }
}