using System.Collections.Generic;
using Relic.Gameplay.Data;
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

            foreach (TimelineActionData action in actions)
            {
                int slotIndex = action.SlotIndex;

                if (slotIndex < 0 || slotIndex >= slots.Length)
                {
                    Debug.LogWarning($"[BattleTimelineUI] 잘못된 SlotIndex: {slotIndex}");
                    continue;
                }

                TimelineSlotUI slot = slots[slotIndex];

                if (slot == null)
                    continue;

                if (action.ActorType == BattleActorType.Player)
                {
                    string characterId = action.ActorRuntimeId;

                    if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out Sprite ownerIcon))
                    {
                        slot.SetOwnerIcon(ownerIcon);
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleTimelineUI] 캐릭터 아이콘 없음: {characterId}");
                    }
                }

                string actionType = action.ActionType.ToString();

                if (DataManager.Instance.ActionTypeIconDatabase.TryGetIcon(actionType, out Sprite actionIcon))
                {
                    slot.AddActionTypeIcon(actionIcon);
                }
                else
                {
                    Debug.LogWarning($"[BattleTimelineUI] ActionType 아이콘 없음: {actionType}");
                }
            }
        }
    }
}