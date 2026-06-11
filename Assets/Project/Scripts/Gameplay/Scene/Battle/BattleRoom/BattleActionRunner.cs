using System.Collections;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
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
                ExecutePlayerMove(command);
            else
                ExecutePlayerSkill(command);
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand command = batch.MonsterCommands[i];

            if (command == null)
                continue;

            ExecuteMonsterCommand(command);
        }

        RefreshHUDs();

        yield return new WaitForSeconds(0.35f);
    }

    private void ExecutePlayerMove(PlayerReservedCommand command)
    {
        BattleCharacter character = FindBattleCharacter(command.CharacterId);

        if (character == null)
            return;

        int currentGridIndex = character.CurrentGridIndex;

        if (currentGridIndex < 0)
            return;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int targetCoord = currentCoord + command.MoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
            return;

        int targetGridIndex = gridManager.CoordToIndex(targetCoord);

        if (BattleOccupancyService.IsOccupiedByAnyUnit(targetGridIndex, command.CharacterId))
        {
            Debug.LogWarning(
                $"[BattleActionRunner] Player Move Blocked / {command.CharacterId} / To:{targetGridIndex}"
            );
            return;
        }

        Vector3 pos = gridManager.GetWorldPositionByIndex(targetGridIndex);

        character.transform.position = pos;
        character.SetGridIndex(targetGridIndex);
        UpdatePartyGridIndex(command.CharacterId, targetGridIndex);

        Debug.Log($"[BattleActionRunner] Player Move / {command.CharacterId} -> {targetGridIndex}");
    }

    private void ExecutePlayerSkill(PlayerReservedCommand command)
    {
        int damage = GetPlayerDamage(command);

        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (!IsMonsterInRange(monster, command))
                continue;

            monster.RuntimeData.TakeDamage(damage);

            Debug.Log(
                $"[BattleActionRunner] Player Hit Monster / " +
                $"{command.CharacterId} -> {monster.RuntimeData.Name} / Damage:{damage} / HP:{monster.RuntimeData.CurrentHp}"
            );
        }
    }

    private void ExecuteMonsterCommand(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return;

        Debug.Log(
            $"[MonsterCommand] Skill:{command.SkillId} / " +
            $"GridMove:{command.SkillData.GridMove} / " +
            $"MoveOffset:{command.MoveOffset} / " +
            $"RangeCount:{command.RangeGridIndices.Count}"
        );

        if (command.MoveOffset != Vector2Int.zero ||
            command.SkillData.TimelineNotation == TimelineActionType.Move)
        {
            ExecuteMonsterMove(command);
            return;
        }

        ExecuteMonsterSkill(command);
    }

    private void ExecuteMonsterMove(MonsterReservedCommand command)
    {
        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            return;

        int currentGridIndex = monster.MainGridIndex;

        if (currentGridIndex < 0)
            return;

        Vector2Int moveOffset = GetMonsterMoveOffset(command);

        if (moveOffset == Vector2Int.zero)
            return;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int occupiedIndex = monster.OccupiedGridIndices[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(occupiedIndex);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
            {
                Debug.LogWarning($"[BattleActionRunner] Monster Move Blocked / Out of Grid / {monster.RuntimeData.Name}");
                return;
            }

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
            {
                Debug.LogWarning($"[BattleActionRunner] Monster Move Blocked / {monster.RuntimeData.Name} / To:{targetIndex}");
                return;
            }
        }

        Vector2Int mainCoord = gridManager.IndexToCoord(currentGridIndex);
        Vector2Int movedMainCoord = mainCoord + moveOffset;
        int movedMainIndex = gridManager.CoordToIndex(movedMainCoord);

        Vector3 pos = gridManager.GetWorldPositionByIndex(movedMainIndex);

        monster.transform.position = pos;
        monster.MoveOccupiedCells(moveOffset, gridManager);

        Debug.Log($"[BattleActionRunner] Monster Move / {monster.RuntimeData.Name} -> {movedMainIndex}");
    }

    private void ExecuteMonsterSkill(MonsterReservedCommand command)
    {
        MonsterUnit monster = FindMonsterUnit(command.RuntimeId);

        if (monster == null)
            return;

        int damage = GetMonsterDamage(command);

        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (!command.RangeGridIndices.Contains(character.CurrentGridIndex))
                continue;

            character.RuntimeData.CurrentHealth =
                Mathf.Max(0, character.RuntimeData.CurrentHealth - damage);

            Debug.Log(
                $"[BattleActionRunner] Monster Hit Player / " +
                $"{monster.RuntimeData.Name} -> {character.CharacterId} / Damage:{damage} / HP:{character.RuntimeData.CurrentHealth}"
            );
        }
    }

    private Vector2Int GetMonsterMoveOffset(MonsterReservedCommand command)
    {
        if (command == null)
            return Vector2Int.zero;

        if (command.MoveOffset != Vector2Int.zero)
            return command.MoveOffset;

        int move = command.SkillData != null ? command.SkillData.GridMove : 0;

        if (move == 0)
            return Vector2Int.zero;

        return new Vector2Int(-1 * Mathf.Abs(move), 0);
    }

    private bool IsMonsterInRange(MonsterUnit monster, PlayerReservedCommand command)
    {
        if (monster == null || command == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            if (command.RangeGridIndices.Contains(monster.OccupiedGridIndices[i]))
                return true;
        }

        return false;
    }

    private int GetPlayerDamage(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return 1;

        int value = ParseFirstInt(command.SkillData.ValueRate);

        return Mathf.Max(1, value);
    }

    private int GetMonsterDamage(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return 1;

        int value = ParseFirstInt(command.SkillData.ValueRate);

        return Mathf.Max(1, value);
    }

    private int ParseFirstInt(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 1;

        string number = "";

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]) || text[i] == '-')
                number += text[i];
            else if (!string.IsNullOrEmpty(number))
                break;
        }

        if (int.TryParse(number, out int result))
            return result;

        return 1;
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

    private BattleCharacter FindBattleCharacter(string characterId)
    {
        BattleCharacter[] characters =
            Object.FindObjectsByType<BattleCharacter>(
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

    private MonsterUnit FindMonsterUnit(string runtimeId)
    {
        MonsterUnit[] monsters =
            Object.FindObjectsByType<MonsterUnit>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.RuntimeId == runtimeId)
                return monsters[i];
        }

        return null;
    }

    private void RefreshHUDs()
    {
        PlayerHUDSlot[] playerHuds =
            Object.FindObjectsByType<PlayerHUDSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < playerHuds.Length; i++)
        {
            if (playerHuds[i] != null)
                playerHuds[i].Refresh();
        }

        MonsterHUDSlot[] monsterHuds =
            Object.FindObjectsByType<MonsterHUDSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < monsterHuds.Length; i++)
        {
            if (monsterHuds[i] != null)
                monsterHuds[i].Refresh();
        }
    }
}