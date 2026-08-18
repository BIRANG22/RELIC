using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    /// <summary>
    /// 모르트 AI
    /// - 가까운 캐릭터에게서 거리를 벌리면서 가로 직선 라인을 잡도록 8방향 중 1칸 이동합니다.
    /// - 같은 가로 라인에 캐릭터가 있으면 망령쇄도를 사용합니다.
    /// - 라인이 맞지 않으면 혼령폭발을 사용합니다.
    /// - 체력이 가장 높은 캐릭터에게 피의저주를 사용합니다.
    /// - 함께 싸우던 드라우그/바로우가 사망하면 1턴 뒤 사령술로 되살립니다.
    /// </summary>
    public class MortAI : MonsterAIBase
    {
        private const string MoveSkillId = "S_Monster_37";
        private const string LineAttackSkillId = "S_Monster_38";
        private const string AreaAttackSkillId = "S_Monster_39";
        private const string BleedSkillId = "S_Monster_40";
        private const string NecromancySkillId = "S_Monster_41";

        private static readonly List<Vector2Int> MoveOffsets = new()
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1)
        };

        public override string SelectSkill(MonsterRuntimeData monster, BattleContext context)
        {
            return LineAttackSkillId;
        }

        public override MonsterAIPlan CreatePlan(
            MonsterUnit monsterUnit,
            BattleContext context,
            GridManager gridManager)
        {
            MonsterAIPlan plan = new();

            if (monsterUnit == null || monsterUnit.RuntimeData == null || gridManager == null)
                return plan;

            MonsterRuntimeData runtime = monsterUnit.RuntimeData;
            int priority = 0;

            if (MortNecromancyTracker.HasReadySoldier(runtime.RuntimeId, runtime.TurnCount))
            {
                plan.Add(new MonsterAIAction(
                    NecromancySkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Front,
                    -1,
                    priority++));
            }

            Vector2Int moveOffset = ResolveTacticalMove(monsterUnit, gridManager);
            int attackOriginGridIndex = monsterUnit.MainGridIndex;

            if (moveOffset != Vector2Int.zero)
            {
                plan.Add(new MonsterAIAction(
                    MoveSkillId,
                    moveOffset,
                    MonsterAISlotPreference.Front,
                    110,
                    priority++));

                attackOriginGridIndex = GetProjectedMainGridIndex(monsterUnit, gridManager, moveOffset);
            }

            if (TryBuildLineAttack(
                    attackOriginGridIndex,
                    gridManager,
                    out BattleDirection lineDirection,
                    out List<int> lineRange))
            {
                plan.Add(new MonsterAIAction(
                    LineAttackSkillId,
                    Vector2Int.zero,
                    moveOffset != Vector2Int.zero
                        ? MonsterAISlotPreference.SameSlot
                        : MonsterAISlotPreference.Front,
                    moveOffset != Vector2Int.zero ? 110 : -1,
                    priority++,
                    attackOriginGridIndex,
                    true,
                    lineDirection,
                    false,
                    0,
                    lineRange));
            }
            else
            {
                BattleCharacter areaTarget = FindNearestPlayerFromGrid(attackOriginGridIndex, gridManager);

                if (areaTarget != null && areaTarget.CurrentGridIndex >= 0)
                {
                    plan.Add(new MonsterAIAction(
                        AreaAttackSkillId,
                        Vector2Int.zero,
                        moveOffset != Vector2Int.zero
                            ? MonsterAISlotPreference.SameSlot
                            : MonsterAISlotPreference.Front,
                        moveOffset != Vector2Int.zero ? 110 : -1,
                        priority++,
                        areaTarget.CurrentGridIndex));
                }
            }

            BattleCharacter bleedTarget = FindHighestHPPlayer();

            if (bleedTarget != null && bleedTarget.CurrentGridIndex >= 0)
            {
                plan.Add(new MonsterAIAction(
                    BleedSkillId,
                    Vector2Int.zero,
                    MonsterAISlotPreference.Back,
                    -1,
                    priority,
                    bleedTarget.CurrentGridIndex,
                    explicitRangeGridIndices: new List<int> { bleedTarget.CurrentGridIndex }));
            }

            return plan;
        }

        private Vector2Int ResolveTacticalMove(MonsterUnit monsterUnit, GridManager gridManager)
        {
            if (monsterUnit == null || monsterUnit.MainGridIndex < 0 || gridManager == null)
                return Vector2Int.zero;

            BattleCharacter nearest = FindNearestPlayer(monsterUnit, gridManager);

            if (nearest == null || nearest.CurrentGridIndex < 0)
                return Vector2Int.zero;

            Vector2Int current = gridManager.IndexToCoord(monsterUnit.MainGridIndex);
            Vector2Int nearestCoord = gridManager.IndexToCoord(nearest.CurrentGridIndex);
            int currentDistance = Manhattan(current, nearestCoord);

            Vector2Int bestOffset = Vector2Int.zero;
            int bestScore = int.MinValue;

            for (int i = 0; i < MoveOffsets.Count; i++)
            {
                Vector2Int offset = MoveOffsets[i];

                if (!CanMonsterMove(monsterUnit, gridManager, offset))
                    continue;

                Vector2Int moved = current + offset;
                int distance = Manhattan(moved, nearestCoord);
                bool hasHorizontalLine = HasAlivePlayerOnHorizontalLine(moved, gridManager);

                // 같은 가로 라인을 만들 수 있는 위치를 가장 우선하고,
                // 그 안에서는 가까운 캐릭터와 거리가 더 멀어지는 위치를 선택합니다.
                int score = (hasHorizontalLine ? 1000 : 0) + distance * 10;

                if (distance < currentDistance && !hasHorizontalLine)
                    score -= 500;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestOffset = offset;
            }

            return bestOffset;
        }

        private bool TryBuildLineAttack(
            int originGridIndex,
            GridManager gridManager,
            out BattleDirection direction,
            out List<int> rangeGridIndices)
        {
            direction = BattleDirection.Right;
            rangeGridIndices = new List<int>();

            if (originGridIndex < 0 || gridManager == null)
                return false;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            List<int> targets = FindCharacterTargetGridIndices();
            int nearestDistance = int.MaxValue;
            int horizontalSign = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                int targetGridIndex = targets[i];

                if (targetGridIndex < 0)
                    continue;

                Vector2Int target = gridManager.IndexToCoord(targetGridIndex);

                if (target.y != origin.y || target.x == origin.x)
                    continue;

                int distance = Mathf.Abs(target.x - origin.x);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                horizontalSign = target.x > origin.x ? 1 : -1;
            }

            if (horizontalSign == 0)
                return false;

            direction = horizontalSign > 0
                ? BattleDirection.Right
                : BattleDirection.Left;

            for (int x = origin.x + horizontalSign;
                 x >= 0 && x < gridManager.Width;
                 x += horizontalSign)
            {
                Vector2Int coord = new Vector2Int(x, origin.y);

                if (!gridManager.IsValidCoord(coord))
                    break;

                rangeGridIndices.Add(gridManager.CoordToIndex(coord));
            }

            return rangeGridIndices.Count > 0;
        }

        private BattleCharacter FindNearestPlayerFromGrid(int originGridIndex, GridManager gridManager)
        {
            if (originGridIndex < 0 || gridManager == null)
                return null;

            Vector2Int origin = gridManager.IndexToCoord(originGridIndex);
            BattleCharacter[] players = FindPlayers();
            BattleCharacter nearest = null;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);
                int distance = Manhattan(origin, playerCoord);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = player;
            }

            return nearest;
        }

        private bool HasAlivePlayerOnHorizontalLine(Vector2Int origin, GridManager gridManager)
        {
            BattleCharacter[] players = FindPlayers();

            for (int i = 0; i < players.Length; i++)
            {
                BattleCharacter player = players[i];

                if (!IsAlivePlayer(player) || player.CurrentGridIndex < 0)
                    continue;

                Vector2Int playerCoord = gridManager.IndexToCoord(player.CurrentGridIndex);

                if (playerCoord.y == origin.y && playerCoord.x != origin.x)
                    return true;
            }

            return false;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
