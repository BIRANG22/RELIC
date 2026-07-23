using Relic.Gameplay.Data;
using UnityEngine;

public class SpawnSpiderEggEffect : BattleEffectBase
{
    private const string SpiderEggGridEffectId = "GR_spider_egg";
    private const int DefaultPushDamage = 5;

    public override string EffectId => "E_Spawn_Spider_Egg";

    protected override void Apply(BattleEffectContext context)
    {
        int gridIndex = ResolveTargetGridIndex(context);

        if (gridIndex < 0)
            return;

        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (controller == null || controller.HasEffect(gridIndex))
            return;

        if (BattleOccupancyService.IsOccupiedByMonster(gridIndex))
            return;

        if (BattleOccupancyService.TryGetCharacterAtGrid(gridIndex, out BattleCharacter character) &&
            !TryPushCharacterFromEggGrid(context, controller, character))
        {
            return;
        }

        controller.TryPlaceEffect(gridIndex, SpiderEggGridEffectId);
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
