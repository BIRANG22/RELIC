using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleMonsterSpawner : MonoBehaviour
{
    [Header("Grid Root")]
    [SerializeField] private Transform gridRoot;

    [Header("Spawn Root")]
    [SerializeField] private Transform monsterRoot;

    [Header("Spawn Setting")]
    [SerializeField] private string gridNamePrefix = "Grid_";
    [SerializeField] private string spawnPointName = "Point";
    [SerializeField] private float unitSpawnZOffset = 0.01f;

    public SpawnedMonsterResult Spawn(BattleMapData spawnData)
    {
        if (spawnData == null)
            return null;

        var dm = DataManager.Instance;

        MonsterMasterData monsterData = dm.MonsterDatabase.Get(spawnData.MonsterId);

        if (monsterData == null)
        {
            Debug.LogWarning($"[BattleMonsterSpawner] MonsterData 없음: {spawnData.MonsterId}");
            return null;
        }

        if (monsterData.BattlePrefab == null)
        {
            Debug.LogWarning($"[BattleMonsterSpawner] BattlePrefab 없음: {spawnData.MonsterId}");
            return null;
        }

        var cells = spawnData.GetOccupiedCells();

        if (cells.Count <= 0)
        {
            Debug.LogWarning($"[BattleMonsterSpawner] 점유칸 없음: {spawnData.MonsterId}");
            return null;
        }

        int mainCell = cells[0];

        Transform spawnGrid = FindGridByIndex(mainCell);

        if (spawnGrid == null)
        {
            Debug.LogWarning($"[BattleMonsterSpawner] Grid 오브젝트 없음: Grid_{mainCell:00}");
            return null;
        }

        Transform spawnPoint = FindSpawnPoint(spawnGrid);

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : spawnGrid.position;

        spawnPosition.z += unitSpawnZOffset;

        GameObject monster = Instantiate(
            monsterData.BattlePrefab,
            spawnPosition,
            Quaternion.identity,
            monsterRoot
        );

        string runtimeId = MonsterRuntimeIdGenerator.Create();

        MonsterRuntimeData runtimeData =
            new MonsterRuntimeData(runtimeId, monsterData);

        MonsterUnit monsterUnit = monster.GetComponent<MonsterUnit>();

        if (monsterUnit == null)
        {
            Debug.LogError($"[BattleMonsterSpawner] MonsterUnit 없음: {monster.name}");
            Destroy(monster);
            return null;
        }

        monsterUnit.Initialize(runtimeData);

        return new SpawnedMonsterResult
        {
            RuntimeData = runtimeData,
            MonsterTransform = monster.transform
        };
    }

    private Transform FindGridByIndex(int gridIndex)
    {
        if (gridRoot == null)
        {
            Debug.LogError("[BattleMonsterSpawner] gridRoot가 연결되지 않았습니다.");
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

        Debug.LogWarning($"[BattleMonsterSpawner] {grid.name} 안에 {spawnPointName} 오브젝트가 없습니다. Grid 위치를 대신 사용합니다.");
        return null;
    }
}