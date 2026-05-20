using System.Collections.Generic;
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
        private readonly Dictionary<SkillSlotUI, List<PlannedSkillEntry>> plannedSkillsBySlot = new();

        private SkillSlotUI currentSlot;
        private CharacterSelectButtonUI currentCharacter;
        private SkillMasterData currentSkillData;
        private ReferenceResource currentPreviewResourceType = ReferenceResource.Stamina;

        private sealed class PlannedSkillEntry
        {
            public string ActionId;
            public string SkillId;
            public int PreviewCost;
        }

        public void SelectPlayer(CharacterSelectButtonUI character)
        {
            if (character == null)
                return;

            currentCharacter = character;

            currentSelection.Clear();
            currentSelection.PlayerRuntimeId = character.CharacterId;

            ResolveResourcePreviewUI(character);
            RefreshResourcePreview();

            Debug.Log($"[PlayerActionPlanner] Player 선택: {character.CharacterId}");
        }

        public void SelectSkill(SkillMasterData skillData)
        {
            if (skillData == null)
                return;

            if (currentSlot == null)
            {
                Debug.LogWarning("[PlayerActionPlanner] 선택된 슬롯이 없습니다.");
                return;
            }

            if (currentCharacter == null)
            {
                Debug.LogWarning("[PlayerActionPlanner] 캐릭터가 선택되지 않았습니다.");
                return;
            }

            if (!TryGetActorRuntimeData(currentCharacter.CharacterId, out CharacterRuntimeData actorRuntimeData))
            {
                Debug.LogWarning($"[PlayerActionPlanner] CharacterRuntimeData 조회 실패: {currentCharacter.CharacterId}");
                return;
            }

            int reservedCost = GetReservedCostByCharacter(currentCharacter.CharacterId, skillData.ReferenceResource);
            int currentResource = SkillCostCalculator.GetCurrentResource(actorRuntimeData, skillData.ReferenceResource);
            int availableResource = currentResource - reservedCost;
            
            if (!CanPaySkillCostWithAvailable(skillData, availableResource, out int previewCost))
            {
                Debug.Log(
                    $"[CostCheck] " +
                    $"Skill:{skillData.SkillId}, " +
                    $"Resource:{skillData.ReferenceResource}, " +
                    $"CostType:{skillData.ResourceCostType}, " +
                    $"CostValue:{skillData.ResourceCostValue}, " +
                    $"Current:{currentResource}, " +
                    $"Reserved:{reservedCost}, " +
                    $"Available:{availableResource}"
                );
                return;
            }

            currentSkillData = skillData;
            currentPreviewResourceType = skillData.ReferenceResource;
            currentSelection.SkillId = skillData.SkillId;
            currentSelection.ActionType = skillData.TimelineNotation;

            RefreshResourcePreview();
            Debug.Log($"[PlayerActionPlanner] Skill 선택: {skillData.SkillId} / Target:{skillData.Target}");

            if (skillData.Target == TargetType.PlayerParty || skillData.Target == TargetType.EnemyParty)
            {
                ConfirmAction(previewCost);
                return;
            }

            if (skillData.Target == TargetType.Self)
            {
                ConfirmAction(previewCost);
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

            RefreshResourcePreview();
            Debug.Log($"[PlayerActionPlanner] 슬롯 선택: {slot.name}");
        }

        public void RemovePlannedSkill(SkillSlotUI slot, int iconIndex)
        {
            if (slot == null)
                return;

            if (!plannedSkillsBySlot.TryGetValue(slot, out List<PlannedSkillEntry> plannedList))
                return;

            if (iconIndex < 0 || iconIndex >= plannedList.Count)
                return;

            PlannedSkillEntry removedEntry = plannedList[iconIndex];
            plannedList.RemoveAt(iconIndex);
            slot.RemoveSkillAt(iconIndex);

            if (timelineManager != null)
            {
                timelineManager.RemoveAction(x => x.ActionId == removedEntry.ActionId);
            }

            if (plannedList.Count == 0)
            {
                plannedSkillsBySlot.Remove(slot);
            }

            RefreshTimelineUI();
            RefreshResourcePreview();
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

        private void ConfirmAction(int precomputedPreviewCost = -1)
        {
            if (currentSlot == null || currentCharacter == null || currentSkillData == null)
                return;

            int slotIndex = GetCurrentSlotIndex();
            if (slotIndex < 0)
                return;

            bool added = currentSlot.AddSkill(currentSkillData.Icon);
            if (!added)
                return;

            if (!currentSlot.HasOwnerCharacter)
                currentSlot.SetOwnerCharacter(currentCharacter);

            int previewCost = precomputedPreviewCost;
            if (previewCost < 0)
            {
                if (!TryGetActorRuntimeData(currentCharacter.CharacterId, out CharacterRuntimeData actorRuntimeData))
                {
                    currentSlot.RemoveSkillAt(currentSlot.SkillCount - 1);
                    return;
                }

                int reservedCost = GetReservedCostByCharacter(currentCharacter.CharacterId, currentSkillData.ReferenceResource);
                int currentResource = SkillCostCalculator.GetCurrentResource(actorRuntimeData, currentSkillData.ReferenceResource);
                int availableResource = currentResource - reservedCost;

                if (!CanPaySkillCostWithAvailable(currentSkillData, availableResource, out previewCost))
                {
                    currentSlot.RemoveSkillAt(currentSlot.SkillCount - 1);
                    return;
                }
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
            AddPlannedEntry(currentSlot, action.ActionId, currentSkillData.SkillId, previewCost);

            RefreshTimelineUI();
            RefreshResourcePreview();

            currentSelection.ClearActionOnly();
            currentSkillData = null;
        }

        private void AddPlannedEntry(SkillSlotUI slot, string actionId, string skillId, int previewCost)
        {
            if (!plannedSkillsBySlot.TryGetValue(slot, out List<PlannedSkillEntry> plannedList))
            {
                plannedList = new List<PlannedSkillEntry>();
                plannedSkillsBySlot[slot] = plannedList;
            }

            plannedList.Add(new PlannedSkillEntry
            {
                ActionId = actionId,
                SkillId = skillId,
                PreviewCost = previewCost
            });
        }

        private int GetReservedCost(SkillSlotUI slot, string actorCharacterId, ReferenceResource resourceType)
        {
            if (slot == null)
                return 0;

            if (!plannedSkillsBySlot.TryGetValue(slot, out List<PlannedSkillEntry> plannedList))
                return 0;

            int sum = 0;

            foreach (PlannedSkillEntry entry in plannedList)
            {
                if (string.IsNullOrWhiteSpace(entry.SkillId))
                    continue;

                SkillMasterData skillData = DataManager.Instance?.SkillDatabase?.Get(entry.SkillId);
                if (skillData == null)
                    continue;

                if (skillData.ReferenceResource != resourceType)
                    continue;

                if (!slot.HasOwnerCharacter || slot.OwnerCharacter.CharacterId != actorCharacterId)
                    continue;

                sum += entry.PreviewCost;
            }

            return sum;
        }

        private int GetReservedCostByCharacter(string actorCharacterId, ReferenceResource resourceType)
        {
            int sum = 0;

            foreach (var pair in plannedSkillsBySlot)
            {
                SkillSlotUI slot = pair.Key;
                List<PlannedSkillEntry> plannedList = pair.Value;

                if (slot == null)
                    continue;

                if (!slot.HasOwnerCharacter || slot.OwnerCharacter.CharacterId != actorCharacterId)
                    continue;

                foreach (PlannedSkillEntry entry in plannedList)
                {
                    if (string.IsNullOrWhiteSpace(entry.SkillId))
                        continue;

                    SkillMasterData skillData = DataManager.Instance?.SkillDatabase?.Get(entry.SkillId);
                    if (skillData == null)
                        continue;

                    if (skillData.ReferenceResource != resourceType)
                        continue;

                    sum += entry.PreviewCost;
                }
            }

            return sum;
        }

        private bool TryGetActorRuntimeData(string characterId, out CharacterRuntimeData runtimeData)
        {
            runtimeData = null;

            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            var dm = DataManager.Instance;
            if (dm == null || dm.CharacterRuntimeStore == null)
                return false;

            return dm.CharacterRuntimeStore.TryGet(characterId, out runtimeData);
        }

        private static bool CanPaySkillCostWithAvailable(
            SkillMasterData skillData,
            int availableResource,
            out int previewCost)
        {
            previewCost = 0;

            if (skillData == null)
                return false;

            switch (skillData.ResourceCostType)
            {
                case ResourceCostType.None:
                    previewCost = 0;
                    return true;

                case ResourceCostType.Fixed:
                    previewCost = skillData.ResourceCostValue;
                    return availableResource >= previewCost;

                case ResourceCostType.AllCurrent:
                    previewCost = availableResource;
                    return availableResource >= skillData.ResourceCostValue;

                default:
                    return false;
            }
        }

        private SkillResourcePreviewUI resourcePreviewUI;

        private void ResolveResourcePreviewUI(CharacterSelectButtonUI character)
        {
            if (character == null)
            {
                resourcePreviewUI = null;
                return;
            }

            resourcePreviewUI = character.GetComponentInChildren<SkillResourcePreviewUI>(true);

            if (resourcePreviewUI == null && character.BattleCharacter != null)
                resourcePreviewUI = character.BattleCharacter.GetComponentInChildren<SkillResourcePreviewUI>(true);

            if (resourcePreviewUI == null)
                Debug.LogWarning($"[PlayerActionPlanner] SkillResourcePreviewUI 자동 연결 실패: {character.name}");
        }

        private void RefreshResourcePreview()
        {
            if (resourcePreviewUI == null)
                return;

            if (currentSlot == null || currentCharacter == null)
            {
                resourcePreviewUI.SetPreview(0f);
                return;
            }

            if (!TryGetActorRuntimeData(currentCharacter.CharacterId, out CharacterRuntimeData runtimeData))
            {
                resourcePreviewUI.SetPreview(0f);
                return;
            }

            if (!TryGetCharacterMasterData(currentCharacter.CharacterId, out CharacterMasterData masterData))
            {
                resourcePreviewUI.SetPreview(0f);
                return;
            }

            ReferenceResource previewResourceType = currentPreviewResourceType;

            int currentValue = GetCurrentResourceByType(runtimeData, previewResourceType);
            int maxValue = GetMaxResourceByType(masterData, previewResourceType);
            int reserved = GetReservedCostByCharacter(currentCharacter.CharacterId, previewResourceType);
            int remain = Mathf.Max(0, currentValue - reserved);

            resourcePreviewUI.SetPreviewByValue(remain, maxValue);
        }

        private bool TryGetCharacterMasterData(string characterId, out CharacterMasterData masterData)
        {
            masterData = null;

            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            var dm = DataManager.Instance;
            if (dm == null || dm.CharacterDatabase == null)
                return false;

            return dm.CharacterDatabase.TryGet(characterId, out masterData);
        }

        private static int GetCurrentResourceByType(CharacterRuntimeData runtimeData, ReferenceResource type)
        {
            return type switch
            {
                ReferenceResource.Health => runtimeData.CurrentHealth,
                ReferenceResource.Stamina => runtimeData.CurrentStamina,
                ReferenceResource.UniqueResource => runtimeData.CurrentResource,
                ReferenceResource.MovePoint => runtimeData.CurrentMoveLevel,
                _ => 0
            };
        }

        private static int GetMaxResourceByType(CharacterMasterData masterData, ReferenceResource type)
        {
            return type switch
            {
                ReferenceResource.Health => masterData.MaxHealth,
                ReferenceResource.Stamina => masterData.MaxStamina,
                ReferenceResource.UniqueResource => masterData.MaxResource,
                ReferenceResource.MovePoint => masterData.MoveValue,
                _ => 0
            };
        }

        private void RefreshTimelineUI()
        {
            if (timelineUI != null && timelineManager != null)
                timelineUI.Refresh(timelineManager.GetActions());
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
