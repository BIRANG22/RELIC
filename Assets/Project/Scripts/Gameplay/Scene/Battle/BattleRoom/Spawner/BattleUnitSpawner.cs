using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnitSpawner : MonoBehaviour
{
    [Header("Grid Root")]
    [SerializeField] private Transform gridRoot;

    [Header("Spawn Root")]
    [SerializeField] private Transform playerRoot;

    [Header("Spawn Setting")]
    [SerializeField] private string gridNamePrefix = "Grid_";
    [SerializeField] private string spawnPointName = "Point";
    [SerializeField] private int playerGridCount = 15;
    [SerializeField] private float unitSpawnZOffset = 0.01f;
    [SerializeField] private float unitSpawnYOffset = 0.1f;

    public Transform PlayerRoot => playerRoot;

    public List<CharacterRuntimeData> SpawnFromRuntimeData(bool useCurrentGridPositions = false)
    {
        List<CharacterRuntimeData> spawnedRuntimes = new();

        var dm = DataManager.Instance;
        var partyStore = dm.PartyRuntimeStore;

        for (int slotIndex = 0; slotIndex < partyStore.MaxPartyCountValue; slotIndex++)
        {
            string characterId = partyStore.GetCharacterId(slotIndex);
            int gridIndex = useCurrentGridPositions ? partyStore.GetCurrentGridIndex(slotIndex) : partyStore.GetSpawnGridIndex(slotIndex);
            if (useCurrentGridPositions && (gridIndex < 0 || gridIndex >= playerGridCount))
                gridIndex = partyStore.GetSpawnGridIndex(slotIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (gridIndex < 0 || gridIndex >= playerGridCount)
            {
                Debug.LogWarning($"[BattleUnitSpawner] Invalid grid index. Slot: {slotIndex}, Grid: {gridIndex}");
                continue;
            }

            if (!dm.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
            {
                Debug.LogWarning($"[BattleUnitSpawner] CharacterRuntimeData ����: {characterId}");
                continue;
            }

            CharacterMasterData characterData = dm.CharacterDatabase.Get(characterId);

            if (characterData == null)
            {
                Debug.LogWarning($"[BattleUnitSpawner] CharacterData ����: {characterId}");
                continue;
            }

            if (characterData.BattlePrefab == null)
            {
                Debug.LogWarning($"[BattleUnitSpawner] BattlePrefab ����: {characterId}");
                continue;
            }

            GameObject unit = SpawnCharacterUnit(characterData, runtimeData, gridIndex);

            if (unit == null)
                continue;

            spawnedRuntimes.Add(runtimeData);
        }

        return spawnedRuntimes;
    }

    public void SpawnSingleFromRuntimeData(int slotIndex)
    {
        var dm = DataManager.Instance;
        var partyStore = dm.PartyRuntimeStore;

        string characterId = partyStore.GetCharacterId(slotIndex);
        int gridIndex = partyStore.GetSpawnGridIndex(slotIndex);

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (gridIndex < 0 || gridIndex >= playerGridCount)
        {
            Debug.LogWarning($"[BattleUnitSpawner] Invalid grid index. Slot: {slotIndex}, Grid: {gridIndex}");
            return;
        }

        if (!dm.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
        {
            Debug.LogWarning($"[BattleUnitSpawner] CharacterRuntimeData ����: {characterId}");
            return;
        }

        CharacterMasterData characterData = dm.CharacterDatabase.Get(characterId);

        if (characterData == null)
        {
            Debug.LogWarning($"[BattleUnitSpawner] CharacterData ����: {characterId}");
            return;
        }

        if (characterData.BattlePrefab == null)
        {
            Debug.LogWarning($"[BattleUnitSpawner] BattlePrefab ����: {characterId}");
            return;
        }

        GameObject unit = SpawnCharacterUnit(characterData, runtimeData, gridIndex);

        if (unit == null)
            return;

        Debug.Log($"[BattleUnitSpawner] Spawn Single Slot {slotIndex}: {characterId} -> Grid_{gridIndex:00}");
    }

    private GameObject SpawnCharacterUnit(
        CharacterMasterData characterData,
        CharacterRuntimeData runtimeData,
        int gridIndex)
    {
        Transform spawnGrid = FindGridByIndex(gridIndex);

        if (spawnGrid == null)
        {
            Debug.LogWarning($"[BattleUnitSpawner] Grid ������Ʈ ����: Grid_{gridIndex:00}");
            return null;
        }

        Transform spawnPoint = FindSpawnPoint(spawnGrid);

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : spawnGrid.position;

        spawnPosition.z += unitSpawnZOffset;
        spawnPosition.y += unitSpawnYOffset;

        GameObject unit = Instantiate(
            characterData.BattlePrefab,
            spawnPosition,
            Quaternion.identity,
            playerRoot
        );

        BattleCharacter battleCharacter = unit.GetComponent<BattleCharacter>();

        if (battleCharacter != null)
        {
            battleCharacter.Initialize(runtimeData);
            battleCharacter.SetGridIndex(gridIndex);

            // �� �����濡���� ���� �������� ���� ���� ���¸� ������� �ʽ��ϴ�.
            // ȭ���� �ٶ󺸴� ����� RuntimeData.Direction�� �׻� ���������� ����ȭ�մϴ�.
            BattleUnitFacing facing = unit.GetComponent<BattleUnitFacing>();
            if (facing != null)
            {
                facing.FaceRight(true);
                runtimeData.Direction = facing.GetBattleDirection();
            }
            else
            {
                runtimeData.Direction = BattleDirection.Right;
            }
        }

        return unit;
    }

    private Transform FindGridByIndex(int gridIndex)
    {
        if (gridRoot == null)
        {
            Debug.LogError("[BattleUnitSpawner] gridRoot�� ������� �ʾҽ��ϴ�.");
            return null;
        }

        string gridName = $"{gridNamePrefix}{gridIndex:00}";

        Transform child = gridRoot.Find(gridName);

        if (child != null)
            return child;

        foreach (Transform t in gridRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == gridName)
                return t;
        }

        return null;
    }

    private Transform FindSpawnPoint(Transform grid)
    {
        Transform point = grid.Find(spawnPointName);

        if (point != null)
            return point;

        Debug.LogWarning($"[BattleUnitSpawner] {grid.name} �ȿ� {spawnPointName} ������Ʈ�� �����ϴ�. Grid ��ġ�� ��� ����մϴ�.");
        return null;
    }
}
