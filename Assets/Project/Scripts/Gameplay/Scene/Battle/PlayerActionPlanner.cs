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

        private readonly PlayerActionSelectionData currentSelection = new();

        private int playerActionOrder = 100;

        public void SelectPlayer(string playerRuntimeId)
        {
            currentSelection.Clear();
            currentSelection.PlayerRuntimeId = playerRuntimeId;

            Debug.Log($"[PlayerActionPlanner] Player 선택: {playerRuntimeId}");
        }

        public void SelectSkill(SkillMasterData skillData)
        {
            if (skillData == null)
            {
                Debug.LogWarning("[PlayerActionPlanner] skillData가 null입니다.");
                return;
            }

            if (!currentSelection.HasPlayer)
            {
                Debug.LogWarning("[PlayerActionPlanner] 플레이어가 선택되지 않았습니다.");
                return;
            }

            currentSelection.SkillId = skillData.SkillId;

            currentSelection.ActionType = skillData.TimelineNotation;

            Debug.Log($"[PlayerActionPlanner] Skill 선택: {skillData.SkillId}");

            if (skillData.Target == TargetType.PlayerParty ||
                skillData.Target == TargetType.EnemyParty)
            {
                ConfirmAction();
                return;
            }

            if (skillData.Target == TargetType.Self)
            {
                Debug.Log("[PlayerActionPlanner] Self 스킬입니다. 방향/칸 선택을 기다립니다.");
                return;
            }
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
            if (!currentSelection.HasPlayer)
            {
                Debug.LogWarning("[PlayerActionPlanner] 플레이어가 없습니다.");
                return;
            }

            if (!currentSelection.HasSkill)
            {
                Debug.LogWarning("[PlayerActionPlanner] 스킬이 선택되지 않았습니다.");
                return;
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
                Order = playerActionOrder++
            };

            timelineManager.AddAction(action);

            if (timelineUI != null)
                timelineUI.Refresh(timelineManager.GetActions());

            Debug.Log(
                $"[PlayerActionPlanner] Action 확정: {action.ActorRuntimeId} / {action.SkillId}"
            );

            currentSelection.Clear();
        }
    }
}