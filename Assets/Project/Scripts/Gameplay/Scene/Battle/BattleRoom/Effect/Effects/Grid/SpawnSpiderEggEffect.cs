using Relic.Gameplay.Data;
using UnityEngine;

public class SpawnSpiderEggEffect : BattleEffectBase
{
    private const string SpiderEggGridEffectId = "GR_spider_egg";
    private const int DefaultPushDamage = 5;

    public override string EffectId => "E_Spawn_Spider_Egg";

    protected override void Apply(BattleEffectContext context)
    {
        int requestedGridIndex = ResolveTargetGridIndex(context);
        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (requestedGridIndex < 0 || controller == null || context?.GridManager == null)
            return;

        // 예약 이후 위치가 바뀌었다면 장애물이나 유닛을 밀어내지 않고
        // 아라벨라 주변의 비어 있는 다른 그리드를 찾아 고치를 생성합니다.
        int placementIndex = FindAvailableEggGrid(context, controller, requestedGridIndex);
        if (placementIndex >= 0)
            controller.TryPlaceEffect(placementIndex, SpiderEggGridEffectId);
    }

    private static int FindAvailableEggGrid(
        BattleEffectContext context,
        BattleGridEffectController controller,
        int requestedGridIndex)
    {
        GridManager grid = context.GridManager;
        if (IsFreeEggGrid(requestedGridIndex, grid, controller))
            return requestedGridIndex;

        if (context.MonsterCaster == null || context.MonsterCaster.OccupiedGridIndices == null)
            return -1;

        Vector2Int requestedCoord = grid.IndexToCoord(requestedGridIndex);
        int bestGridIndex = -1;
        int bestDistance = int.MaxValue;
        System.Collections.Generic.HashSet<int> visited = new();

        foreach (int occupiedIndex in context.MonsterCaster.OccupiedGridIndices)
        {
            if (occupiedIndex < 0)
                continue;

            Vector2Int origin = grid.IndexToCoord(occupiedIndex);
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2Int candidate = origin + new Vector2Int(x, y);
                    if (!grid.IsValidCoord(candidate))
                        continue;

                    int index = grid.CoordToIndex(candidate);
                    if (!visited.Add(index) || !IsFreeEggGrid(index, grid, controller))
                        continue;

                    int distance = Mathf.Abs(candidate.x - requestedCoord.x) +
                                   Mathf.Abs(candidate.y - requestedCoord.y);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestGridIndex = index;
                    }
                }
            }
        }

        return bestGridIndex;
    }

    private static bool IsFreeEggGrid(int gridIndex, GridManager grid, BattleGridEffectController controller)
    {
        return gridIndex >= 0 && grid.GetCellByIndex(gridIndex) != null &&
               !BattleOccupancyService.IsOccupiedByAnyUnit(gridIndex) &&
               !controller.HasEffect(gridIndex) && !controller.IsBlocked(gridIndex);
    }

    private static int ResolveTargetGridIndex(BattleEffectContext context)
    {
        if (context?.MonsterCommand != null)
            return context.MonsterCommand.RangeOriginGridIndex;

        if (context?.PlayerCommand != null)
            return context.PlayerCommand.SelectedGridIndex;

        return -1;
    }

    private static bool TryPushCharacterFromEggGrid(
        BattleEffectContext context,
        BattleGridEffectController controller,
        BattleCharacter character)
    {
        if (context == null ||
            context.GridManager == null ||
            controller == null ||
            character == null ||
            character.RuntimeData == null ||
            character.CurrentGridIndex < 0)
        {
            return false;
        }

        Vector2Int preferredOffset = ResolvePreferredPushOffset(context, character);
        Vector2Int[] offsets = BuildPushOffsets(preferredOffset);

        for (int i = 0; i < offsets.Length; i++)
        {
            if (TryMoveCharacter(character, offsets[i], context.GridManager, controller))
            {
                BattleEffectUtility.StatusDamagePlayer(character, ResolvePushDamage());
                return true;
            }
        }

        return false;
    }

    private static Vector2Int ResolvePreferredPushOffset(
        BattleEffectContext context,
        BattleCharacter character)
    {
        Vector2Int lastMoveOffset = character.RuntimeData.LastMoveOffset;

        if (lastMoveOffset != Vector2Int.zero)
            return lastMoveOffset;

        if (context.MonsterCaster != null && context.MonsterCaster.MainGridIndex >= 0)
        {
            Vector2Int characterCoord = context.GridManager.IndexToCoord(character.CurrentGridIndex);
            Vector2Int monsterCoord = context.GridManager.IndexToCoord(context.MonsterCaster.MainGridIndex);
            Vector2Int delta = characterCoord - monsterCoord;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && delta.x != 0)
                return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

            if (delta.y != 0)
                return new Vector2Int(0, delta.y > 0 ? 1 : -1);
        }

        return Vector2Int.right;
    }

    private static Vector2Int[] BuildPushOffsets(Vector2Int preferredOffset)
    {
        Vector2Int[] result = new Vector2Int[4];
        int index = 0;

        AddUniqueOffset(result, ref index, NormalizeOffset(preferredOffset));
        AddUniqueOffset(result, ref index, Vector2Int.right);
        AddUniqueOffset(result, ref index, Vector2Int.left);
        AddUniqueOffset(result, ref index, Vector2Int.up);
        AddUniqueOffset(result, ref index, Vector2Int.down);

        return result;
    }

    private static Vector2Int NormalizeOffset(Vector2Int offset)
    {
        if (offset.x > 0)
            return Vector2Int.right;

        if (offset.x < 0)
            return Vector2Int.left;

        if (offset.y > 0)
            return Vector2Int.up;

        if (offset.y < 0)
            return Vector2Int.down;

        return Vector2Int.right;
    }

    private static void AddUniqueOffset(Vector2Int[] offsets, ref int index, Vector2Int offset)
    {
        if (offsets == null || index >= offsets.Length)
            return;

        for (int i = 0; i < index; i++)
        {
            if (offsets[i] == offset)
                return;
        }

        offsets[index] = offset;
        index++;
    }

    private static bool TryMoveCharacter(
        BattleCharacter character,
        Vector2Int offset,
        GridManager gridManager,
        BattleGridEffectController controller)
    {
        Vector2Int currentCoord = gridManager.IndexToCoord(character.CurrentGridIndex);
        Vector2Int targetCoord = currentCoord + offset;

        if (!gridManager.IsValidCoord(targetCoord))
            return false;

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetGridIndex, character.CharacterId))
            return false;

        if (controller.IsBlocked(targetGridIndex))
            return false;

        character.SetGridIndex(targetGridIndex);
        character.RuntimeData.SetLastMoveOffset(offset);
        character.transform.position = gridManager.GetWorldPositionByIndex(targetGridIndex);
        UpdatePartyGridIndex(character.CharacterId, targetGridIndex);
        return true;
    }

    private static void UpdatePartyGridIndex(string characterId, int gridIndex)
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.PartyRuntimeStore == null ||
            string.IsNullOrWhiteSpace(characterId))
        {
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetCharacterId(i) != characterId)
                continue;

            partyStore.SetCurrentGridIndex(i, gridIndex);
            return;
        }
    }

    private static int ResolvePushDamage()
    {
        GridEffectData data = DataManager.Instance?.GridEffectDatabase?.Get(SpiderEggGridEffectId);
        return data != null && data.CountRate > 0
            ? data.CountRate
            : DefaultPushDamage;
    }
}
