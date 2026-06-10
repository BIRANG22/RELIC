using System.Collections;
using UnityEngine;

public class BattleActionRunner
{
    private readonly GridManager gridManager;

    public BattleActionRunner(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public IEnumerator RunBatch(BattleActionBatch batch)
    {
        if (batch == null)
            yield break;

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand command = batch.PlayerCommands[i];

            if (command == null)
                continue;

            if (command.ReservedMoveGridIndex >= 0)
                ExecuteMove(command);
            else
                ExecutePlayerSkill(command);
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null)
                continue;

            Debug.Log($"[BattleActionRunner] Monster Action / {command.SkillId}");
        }

        yield return new WaitForSeconds(0.35f);
    }

    private void ExecuteMove(PlayerReservedCommand command)
    {
        BattleCharacter character = FindBattleCharacter(command.CharacterId);

        if (character == null)
            return;

        int currentGridIndex = character.CurrentGridIndex;

        if (currentGridIndex < 0)
        {
            Debug.LogWarning($"[BattleActionRunner] 이동 실패 / 현재 위치 없음 / Character:{command.CharacterId}");
            return;
        }

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int targetCoord = currentCoord + command.MoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
        {
            Debug.LogWarning($"[BattleActionRunner] 이동 실패 / 범위 밖 / Character:{command.CharacterId}");
            return;
        }

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetGridIndex, command.CharacterId))
        {
            Debug.LogWarning(
                $"[BattleActionRunner] 이동 실패 / Character:{command.CharacterId} / " +
                $"From:{currentGridIndex} / To:{targetGridIndex}"
            );

            return;
        }

        Vector3 pos = gridManager.GetWorldPositionByIndex(targetGridIndex);

        character.transform.position = pos;
        character.SetGridIndex(targetGridIndex);
        UpdatePartyGridIndex(command.CharacterId, targetGridIndex);

        Debug.Log($"[BattleActionRunner] Move / {command.CharacterId} -> Grid:{targetGridIndex}");
    }

    private void UpdatePartyGridIndex(string characterId, int gridIndex)
    {
        if (DataManager.Instance == null)
            return;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetCharacterId(i) != characterId)
                continue;

            partyStore.SetGridIndex(i, gridIndex);
            return;
        }
    }

    private void ExecutePlayerSkill(PlayerReservedCommand command)
    {
        Debug.Log($"[BattleActionRunner] Player Skill / {command.CharacterId} / {command.SkillId}");

        // 다음 단계:
        // command.RangeGridIndices 안에 있는 몬스터 찾기
        // EffectId 기준으로 데미지/버프/디버프 적용
    }

    private BattleCharacter FindBattleCharacter(string characterId)
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null && characters[i].CharacterId == characterId)
                return characters[i];
        }

        return null;
    }
}