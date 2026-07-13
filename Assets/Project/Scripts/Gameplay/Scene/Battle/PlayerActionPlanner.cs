using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;

namespace Relic.Gameplay.Battle
{
    public class PlayerActionPlanner : MonoBehaviour
    {
        private const string MoveSkillLevelOneId = "S_Move_1";
        private const string MoveSkillLevelTwoId = "S_Move_2";

        [Header("Timeline")]
        [SerializeField] private BattleTimelineManager timelineManager;

        [Header("Timeline UI")]
        [SerializeField] private BattleTimelineUI timelineUI;
        [SerializeField] private SkillSlotUI[] skillSlots;

        [Header("Range Preview")]
        [SerializeField] private Transform playerGridRoot;
        [SerializeField] private string gridNamePrefix = "Grid_";
        [SerializeField] private Color rangePreviewColor = new(0.3f, 0.8f, 1f, 0.7f);

        private DirectionSelectUI directionSelectUI;

        private readonly PlayerActionSelectionData currentSelection = new();
        private readonly Dictionary<SkillSlotUI, List<PlannedSkillEntry>> plannedSkillsBySlot = new();
        private readonly HashSet<int> currentValidTargetGridIndexes = new();

        private SkillSlotUI currentSlot;
        private CharacterSelectButtonUI currentCharacter;
        private SkillMasterData currentSkillData;
        private ReferenceResource currentPreviewResourceType = ReferenceResource.Cost;
        private bool isMoveGridTargetSelectionActive;

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
            ResolveDirectionSelectUI(character);

            RefreshResourcePreview();
            ClearRangePreview();
        }

        public void SelectSkill(SkillMasterData skillData)
        {
            if (skillData == null)
                return;

            ClearPendingTargetSelection();

            if (currentSlot == null || currentCharacter == null)
                return;

            if (!TryGetActorRuntimeData(currentCharacter.CharacterId, out CharacterRuntimeData actorRuntimeData))
                return;

            if (!SkillCostCalculator.TryGetPreviewPayAmount(actorRuntimeData, skillData, out int previewCost))
            {
                Debug.Log($"[CostCheck] Skill:{skillData.SkillId} / Preview 자원 부족");
                return;
            }

            currentSkillData = skillData;
            currentPreviewResourceType = skillData.ReferenceResource;
            currentSelection.SkillId = skillData.SkillId;

            RefreshResourcePreview();

            switch (skillData.RangeType)
            {
                case RangeType.None:
                    ConfirmAction(previewCost);
                    return;

                case RangeType.Selection:
                    if (TryGetValidatedRangeData(skillData, out SkillRangeData gridRangeData))
                    {
                        ShowGridRangePreview(gridRangeData);
                        SetGridSkillTargetMode(true);
                        SetMoveGridTargetSelectionActive(IsMoveSkill(skillData));
                    }
                    return;

                case RangeType.Direction:
                    if (TryGetValidatedRangeData(skillData, out SkillRangeData directionRangeData))
                    {
                        ShowGridRangePreview(directionRangeData);

                        if (directionSelectUI != null && TryGetCurrentCharacterGridIndex(out int originGridIndex))
                            directionSelectUI.Show(originGridIndex);
                    }
                    return;
            }
        }

        public void SelectTargetDirection(SkillDirection direction)
        {
            if (currentSkillData == null || currentSkillData.RangeType != RangeType.Direction)
                return;

            currentSelection.Direction = direction;

            if (directionSelectUI != null)
                directionSelectUI.Hide();

            ClearRangePreview();
            ConfirmAction();
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
            ClearRangePreview();
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

            if (slot.HasOwnerCharacter &&
                TryGetActorRuntimeData(slot.OwnerCharacter.CharacterId, out CharacterRuntimeData runtimeData))
            {
                SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(removedEntry.SkillId);
                RemoveReservedSkillCost(runtimeData, skillData, removedEntry.PreviewCost);
            }

            plannedList.RemoveAt(iconIndex);
            slot.RemoveSkillAt(iconIndex);

            if (timelineManager != null)
                timelineManager.RemoveAction(x => x.ActionId == removedEntry.ActionId);

            if (plannedList.Count == 0)
                plannedSkillsBySlot.Remove(slot);

            RefreshTimelineUI();
            RefreshResourcePreview();
        }

        public void SelectTargetUnit(string targetRuntimeId)
        {
            currentSelection.TargetRuntimeId = targetRuntimeId;
            ClearRangePreview();
            ConfirmAction();
        }

        public void SelectTargetGrid(int gridIndex)
        {
            if (currentSkillData == null)
                return;

            if (currentSkillData.RangeType != RangeType.Selection)
                return;

            if (!currentValidTargetGridIndexes.Contains(gridIndex))
                return;

            currentSelection.TargetGridIndex = gridIndex;

            ClearRangePreview();
            ConfirmAction();
        }

        private void ConfirmAction(int precomputedPreviewCost = -1)
        {
            if (currentSlot == null || currentCharacter == null || currentSkillData == null)
                return;

            int slotIndex = GetCurrentSlotIndex();
            if (slotIndex < 0)
                return;

            if (!TryGetActorRuntimeData(currentCharacter.CharacterId, out CharacterRuntimeData actorRuntimeData))
                return;

            int previewCost = precomputedPreviewCost;

            if (previewCost < 0)
            {
                if (!SkillCostCalculator.TryGetPreviewPayAmount(actorRuntimeData, currentSkillData, out previewCost))
                {
                    Debug.Log($"[CostCheck Confirm] Skill:{currentSkillData.SkillId} / Preview 자원 부족");
                    return;
                }
            }

            bool added = currentSlot.AddSkill(SkillIconUtility.GetSkillIcon(currentSkillData.SkillId));

            if (!added)
                return;

            if (!currentSlot.HasOwnerCharacter)
                currentSlot.SetOwnerCharacter(currentCharacter);

            AddReservedSkillCost(actorRuntimeData, currentSkillData, previewCost);

            TimelineActionData action = new TimelineActionData
            {
                ActionId = System.Guid.NewGuid().ToString(),
                ActorType = BattleActorType.Player,
                ActorRuntimeId = currentSelection.PlayerRuntimeId,
                SkillId = currentSelection.SkillId,
                ActionType = currentSelection.ActionType,
                TargetRuntimeId = currentSelection.TargetRuntimeId,
                TargetGridIndex = currentSelection.TargetGridIndex,
                Direction = currentSelection.Direction,
                SlotIndex = slotIndex,
                Order = slotIndex
            };

            timelineManager.AddAction(action);
            AddPlannedEntry(currentSlot, action.ActionId, currentSkillData.SkillId, previewCost);

            RefreshTimelineUI();
            RefreshResourcePreview();

            currentSelection.ClearActionOnly();
            currentSkillData = null;
            ClearRangePreview();
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

        private int GetReservedCostByCharacter(string actorCharacterId, ReferenceResource resourceType)
        {
            int sum = 0;

            foreach (var pair in plannedSkillsBySlot)
            {
                SkillSlotUI slot = pair.Key;
                List<PlannedSkillEntry> plannedList = pair.Value;

                if (slot == null || !slot.HasOwnerCharacter || slot.OwnerCharacter.CharacterId != actorCharacterId)
                    continue;

                foreach (PlannedSkillEntry entry in plannedList)
                {
                    SkillMasterData skillData = DataManager.Instance?.SkillDatabase?.Get(entry.SkillId);

                    if (skillData == null)
                        continue;

                    if (skillData.ReferenceResource == resourceType)
                        sum += entry.PreviewCost;
                }
            }

            return sum;
        }

        private bool TryGetValidatedRangeData(SkillMasterData skillData, out SkillRangeData rangeData)
        {
            rangeData = null;

            if (skillData == null || string.IsNullOrWhiteSpace(skillData.RangeId))
                return false;

            rangeData = DataManager.Instance?.RangeDatabase?.Get(skillData.RangeId);
            return rangeData != null;
        }

        private void ShowGridRangePreview(SkillRangeData rangeData)
        {
            ClearRangePreview();
            currentValidTargetGridIndexes.Clear();

            if (rangeData == null || currentCharacter == null)
                return;

            if (!TryGetCurrentCharacterGridIndex(out int originGridIndex))
                return;

            if (!TryGetGridCoord(originGridIndex, out int originX, out int originY))
                return;

            foreach (var offset in rangeData.Positions)
            {
                int x = originX + offset.x;
                int y = originY + offset.y;

                if (!TryGetGridIndex(x, y, out int index))
                    continue;

                currentValidTargetGridIndexes.Add(index);
                TintGridByIndex(index, rangePreviewColor);
            }
        }

        private bool TryGetCurrentCharacterGridIndex(out int gridIndex)
        {
            gridIndex = -1;

            string characterId = currentCharacter?.CharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            var slots = DataManager.Instance?.PartyRuntimeStore?.Slots;
            if (slots == null)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].CharacterId != characterId)
                    continue;

                gridIndex = slots[i].CurrentGridIndex;
                return gridIndex >= 0;
            }

            return false;
        }

        private bool TryGetGridCoord(int index, out int x, out int y)
        {
            const int width = 5;
            const int height = 3;
            x = index % width;
            y = index / width;
            return index >= 0 && x >= 0 && x < width && y >= 0 && y < height;
        }

        private bool TryGetGridIndex(int x, int y, out int index)
        {
            const int width = 5;
            const int height = 3;
            index = -1;

            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            index = y * width + x;
            return true;
        }

        private void SetGridSkillTargetMode(bool value)
        {
            if (playerGridRoot == null)
                return;

            BattleGridClickHandler[] handlers =
                playerGridRoot.GetComponentsInChildren<BattleGridClickHandler>(true);

            foreach (BattleGridClickHandler handler in handlers)
            {
                if (handler != null)
                    handler.SetSkillTargetMode(value);
            }
        }

        private void ClearRangePreview()
        {
            currentValidTargetGridIndexes.Clear();
            SetGridSkillTargetMode(false);
            SetMoveGridTargetSelectionActive(false);

            if (playerGridRoot == null)
                return;

            foreach (Transform t in playerGridRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith(gridNamePrefix))
                    continue;

                if (t.TryGetComponent<Renderer>(out var renderer))
                    renderer.SetPropertyBlock(null);
            }
        }

        private void SetMoveGridTargetSelectionActive(bool active)
        {
            if (isMoveGridTargetSelectionActive == active)
                return;

            isMoveGridTargetSelectionActive = active;
            MonsterUnit.SetAllReservationVisualState(active);
        }

        private bool IsMoveSkill(SkillMasterData skillData)
        {
            if (skillData == null)
                return false;

            if (skillData.Category == Category.Move)
                return true;

            if (skillData.TimelineNotation == TimelineActionType.Move)
                return true;

            return skillData.SkillId == MoveSkillLevelOneId ||
                   skillData.SkillId == MoveSkillLevelTwoId;
        }

        private void ClearPendingTargetSelection()
        {
            ClearRangePreview();

            if (directionSelectUI != null)
                directionSelectUI.Hide();
        }

        private void TintGridByIndex(int gridIndex, Color color)
        {
            if (playerGridRoot == null)
                return;

            string gridName = $"{gridNamePrefix}{gridIndex:00}";
            Transform grid = playerGridRoot.Find(gridName);

            if (grid == null)
            {
                foreach (Transform t in playerGridRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == gridName)
                    {
                        grid = t;
                        break;
                    }
                }
            }

            if (grid == null || !grid.TryGetComponent<Renderer>(out var renderer))
                return;

            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }

        private bool TryGetActorRuntimeData(string characterId, out CharacterRuntimeData runtimeData)
        {
            runtimeData = null;

            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            return DataManager.Instance != null &&
                   DataManager.Instance.CharacterRuntimeStore != null &&
                   DataManager.Instance.CharacterRuntimeStore.TryGet(characterId, out runtimeData);
        }

        private SkillResourcePreviewUI resourcePreviewUI;

        private void ResolveResourcePreviewUI(CharacterSelectButtonUI character)
        {
            resourcePreviewUI = null;

            if (character == null)
                return;

            resourcePreviewUI = character.GetComponentInChildren<SkillResourcePreviewUI>(true);

            if (resourcePreviewUI == null && character.BattleCharacter != null)
                resourcePreviewUI = character.BattleCharacter.GetComponentInChildren<SkillResourcePreviewUI>(true);
        }

        private void ResolveDirectionSelectUI(CharacterSelectButtonUI character)
        {
            directionSelectUI = null;

            if (character == null)
                return;

            directionSelectUI = character.GetComponentInChildren<DirectionSelectUI>(true);

            if (directionSelectUI == null && character.BattleCharacter != null)
                directionSelectUI = character.BattleCharacter.GetComponentInChildren<DirectionSelectUI>(true);

            if (directionSelectUI != null)
            {
                directionSelectUI.Bind(this);
                directionSelectUI.Hide();
            }
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

            int currentValue = SkillCostCalculator.GetCurrentResource(runtimeData, previewResourceType);
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

            return DataManager.Instance != null &&
                   DataManager.Instance.CharacterDatabase != null &&
                   DataManager.Instance.CharacterDatabase.TryGet(characterId, out masterData);
        }

        private static int GetMaxResourceByType(CharacterMasterData masterData, ReferenceResource type)
        {
            return type switch
            {
                ReferenceResource.HP => masterData.MaxHP,
                ReferenceResource.Cost => masterData.MaxCost,
                ReferenceResource.UniqueResource => masterData.MaxResource,
                ReferenceResource.MovePoint => masterData.MaxCost,
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

        private void AddReservedSkillCost(CharacterRuntimeData runtimeData, SkillMasterData skillData, int cost)
        {
            if (runtimeData == null || skillData == null || cost <= 0)
                return;

            switch (skillData.ReferenceResource)
            {
                case ReferenceResource.HP:
                    runtimeData.AddReservedHP(cost);
                    break;

                case ReferenceResource.Cost:
                case ReferenceResource.MovePoint:
                    runtimeData.AddReservedCost(cost);
                    break;

                case ReferenceResource.UniqueResource:
                    runtimeData.AddReservedResource(cost);
                    break;
            }

            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(runtimeData);
        }

        private void RemoveReservedSkillCost(CharacterRuntimeData runtimeData, SkillMasterData skillData, int cost)
        {
            if (runtimeData == null || skillData == null || cost <= 0)
                return;

            switch (skillData.ReferenceResource)
            {
                case ReferenceResource.HP:
                    runtimeData.RemoveReservedHP(cost);
                    break;

                case ReferenceResource.Cost:
                case ReferenceResource.MovePoint:
                    runtimeData.RemoveReservedCost(cost);
                    break;

                case ReferenceResource.UniqueResource:
                    runtimeData.RemoveReservedResource(cost);
                    break;
            }

            DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(runtimeData);
        }
    }
}
