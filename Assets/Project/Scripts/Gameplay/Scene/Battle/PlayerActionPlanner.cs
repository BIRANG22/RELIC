using UnityEngine;
using Relic.Gameplay.Data;

namespace Relic.Gameplay.Battle
{
    public class PlayerActionPlanner : MonoBehaviour
    {
        [Header("Timeline")]
        [SerializeField] private BattleTimelineManager timelineManager;

        [Header("Timeline UI")]
        [SerializeField] private BattleTimelineUI timelineUI;
        [SerializeField] private SkillSlotUI[] skillSlots;

        private readonly PlayerActionSelectionData currentSelection = new();

        private int playerActionOrder = 100;

        private SkillSlotUI currentSlot;
        private CharacterSelectButtonUI currentCharacter;
        private SkillMasterData currentSkillData;
        public void SelectPlayer(CharacterSelectButtonUI character)
        {
            if (character == null)
                return;

            currentCharacter = character;

            currentSelection.Clear();
            currentSelection.PlayerRuntimeId = character.CharacterId;

            Debug.Log($"[PlayerActionPlanner] Player 선택: {character.CharacterId}");
        }

        public void SelectSkill(SkillMasterData skillData)
        {
            if (skillData == null)
                return;

            if (currentSlot == null)
            {
                Debug.LogWarning("[PlayerActionPlanner] 슬롯이 선택되지 않았습니다.");
                return;
            }

            if (currentCharacter == null)
            {
                Debug.LogWarning("[PlayerActionPlanner] 캐릭터가 선택되지 않았습니다.");
                return;
            }

            currentSkillData = skillData;

            currentSelection.SkillId = skillData.SkillId;
            currentSelection.ActionType = skillData.TimelineNotation;

            Debug.Log(
                $"[PlayerActionPlanner] Skill 선택: {skillData.SkillId} / Target:{skillData.Target}"
            );

            if (skillData.Target == TargetType.PlayerParty ||
                skillData.Target == TargetType.EnemyParty)
            {
                ConfirmAction();
                return;
            }

            if (skillData.Target == TargetType.Self)
            {
                Debug.Log("[PlayerActionPlanner] Self 스킬입니다. 방향/칸 선택을 기다립니다.");
            }
        }

        public void SelectSlot(SkillSlotUI slot)
        {
            if (slot == null)
                return;

            if (currentSlot != null)
                currentSlot.SetSelected(false);

            currentSlot = slot;
            currentSlot.SetSelected(true);

            Debug.Log($"[PlayerActionPlanner] 슬롯 선택: {slot.name}");
        }

        public void SelectTargetUnit(string targetRuntimeId)
        {
            currentSelection.TargetRuntimeId = targetRuntimeId;
            ConfirmAction();
        }

        public void SelectTargetGrid(int gridIndex)
        {
            currentSelection.TargetGridIndex = gridIndex;
            ConfirmAction();
        }

        private void ConfirmAction()
        {
            if (currentSlot == null)
                return;

            if (currentCharacter == null)
                return;

            if (currentSkillData == null)
                return;

            int slotIndex = GetCurrentSlotIndex();

            if (slotIndex < 0)
            {
                Debug.LogWarning("[PlayerActionPlanner] 현재 슬롯 인덱스를 찾지 못했습니다.");
                return;
            }

            bool added = currentSlot.AddSkill(currentSkillData.Icon);

            if (!added)
                return;

            if (!currentSlot.HasOwnerCharacter)
            {
                currentSlot.SetOwnerCharacter(currentCharacter);
            }

            TimelineActionData action = new TimelineActionData
            {
                ActionId = System.Guid.NewGuid().ToString(),
                ActorType = BattleActorType.Player,
                ActorRuntimeId = currentSelection.PlayerRuntimeId,
                SkillId = currentSelection.SkillId,
                ActionType = currentSelection.ActionType,
                TargetRuntimeId = currentSelection.TargetRuntimeId,
                TargetGridIndex = currentSelection.TargetGridIndex,

                SlotIndex = slotIndex,

                Order = slotIndex
            };

            timelineManager.AddAction(action);

            if (timelineUI != null)
            {
                timelineUI.Refresh(timelineManager.GetActions());
            }

            Debug.Log(
                $"[PlayerActionPlanner] Action 확정: {action.ActorRuntimeId} / {action.SkillId} / Slot:{slotIndex}"
            );

            currentSelection.ClearActionOnly();
            currentSkillData = null;
        }

        private int GetCurrentSlotIndex()
        {
            if (currentSlot == null || skillSlots == null)
                return -1;

            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] == currentSlot)
                    return i;
            }

            return -1;
        }
    }
}