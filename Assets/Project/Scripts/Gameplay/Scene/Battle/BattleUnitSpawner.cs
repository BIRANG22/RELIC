using UnityEngine;
using Relic.Gameplay.Data;

public class BattleUnitSpawner : MonoBehaviour
{
    [Header("Grid Root")]
    [SerializeField] private Transform gridRoot;

    [Header("Spawn Setting")]
    [SerializeField] private string gridNamePrefix = "Grid_";
    [SerializeField] private string spawnPointName = "Point";
    [SerializeField] private int playerGridCount = 15;
    [SerializeField] private float unitSpawnZOffset = 0.01f;

    public void SpawnFromRuntimeData()
    {
        var dm = DataManager.Instance;
        var partyStore = dm.PartyRuntimeStore;

        for (int slotIndex = 0; slotIndex < partyStore.MaxPartyCountValue; slotIndex++)
        {
            string characterId = partyStore.GetCharacterId(slotIndex);
            int gridIndex = partyStore.GetGridIndex(slotIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (gridIndex < 0 || gridIndex >= playerGridCount)
            {
                Debug.LogWarning($"[BattleUnitSpawner] Invalid grid index. Slot: {slotIndex}, Grid: {gridIndex}");
                continue;
            }

            if (!dm.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtimeData))
            {
                Debug.LogWarning($"[BattleUnitSpawner] CharacterRuntimeData 없음: {characterId}");
                continue;
            }

            CharacterMasterData characterData = dm.CharacterDatabase.Get(characterId);

            if (characterData == null)
            {
                Debug.LogWarning($"[BattleUnitSpawner] CharacterData 없음: {characterId}");
                continue;
            }

            if (characterData.BattlePrefab == null)
            {
                Debug.LogWarning($"[BattleUnitSpawner] BattlePrefab 없음: {characterId}");
                continue;
            }

            Transform spawnGrid = FindGridByIndex(gridIndex);

            if (spawnGrid == null)
            {
                Debug.LogWarning($"[BattleUnitSpawner] Grid 오브젝트 없음: Grid_{gridIndex:00}");
                continue;
            }

            Transform spawnPoint = FindSpawnPoint(spawnGrid);

            Vector3 spawnPosition = spawnPoint != null
                ? spawnPoint.position
                : spawnGrid.position;

            spawnPosition.z += unitSpawnZOffset;

            GameObject unit = Instantiate(
                characterData.BattlePrefab,
                spawnPosition,
                Quaternion.identity
            );

            BattleCharacter battleCharacter = unit.GetComponent<BattleCharacter>();

            if (battleCharacter != null)
                battleCharacter.Initialize(runtimeData);

            Debug.Log($"[BattleUnitSpawner] Spawn Slot {slotIndex}: {characterId} -> Grid_{gridIndex:00}");
        }
    }

    private Transform FindGridByIndex(int gridIndex)
    {
        if (gridRoot == null)
        {
            Debug.LogError("[BattleUnitSpawner] gridRoot가 연결되지 않았습니다.");
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

        Debug.LogWarning($"[BattleUnitSpawner] {grid.name} 안에 {spawnPointName} 오브젝트가 없습니다. Grid 위치를 대신 사용합니다.");
        return null;
    }
}