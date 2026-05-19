using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Battle
{
    public class BattleTimelineUI : MonoBehaviour
    {
        [SerializeField] private TimelineSlotUI[] slots;

        public void Refresh(IReadOnlyList<TimelineActionData> actions)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].Clear();
            }

            int count = Mathf.Min(actions.Count, slots.Length);

            for (int i = 0; i < count; i++)
            {
                TimelineActionData action = actions[i];

                // 여기서 나중에 Actor 아이콘, Skill 타입 아이콘 연결
                Debug.Log(
                    $"[BattleTimelineUI] Slot {i}: {action.ActorType} / {action.ActorRuntimeId} / {action.SkillId}"
                );
            }
        }
    }
}