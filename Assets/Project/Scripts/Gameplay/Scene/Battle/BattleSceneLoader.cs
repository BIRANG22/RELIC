using System.Collections;
using UnityEngine;

public class BattleSceneLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleUnitSpawner unitSpawner;
    [SerializeField] private BattleMonsterSpawner monsterSpawner;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private BattlePlacementController placementController;

    [Header("Debug")]
    [SerializeField] private bool createDebugDataIfEmpty = true;
    [SerializeField] private BattleDebugDataProvider debugDataProvider;

    private IEnumerator Start()
    {
        Debug.Log("[BattleSceneLoader] Start");

        SetLoading(true);

        yield return null;

        if (DataManager.Instance == null)
        {
            Debug.LogError("[BattleSceneLoader] DataManager가 없습니다.");
            yield break;
        }

        Debug.Log("[BattleSceneLoader] DataManager found");

        DataManager.Instance.Initialize();

        yield return null;

        Debug.Log($"[BattleSceneLoader] HasAnyCharacter: {DataManager.Instance.PartyRuntimeStore.HasAnyCharacter}");

        PrintPartyData();

        if (!DataManager.Instance.PartyRuntimeStore.HasAnyCharacter)
        {
            Debug.Log("[BattleSceneLoader] Party empty. Create debug data.");

            if (!createDebugDataIfEmpty)
            {
                Debug.LogError("[BattleSceneLoader] 파티 데이터가 없습니다.");
                yield break;
            }

            if (debugDataProvider == null)
            {
                Debug.LogError("[BattleSceneLoader] DebugDataProvider가 없습니다.");
                yield break;
            }

            debugProviderCheck();

            debugDataProvider.CreateDebugData();

            Debug.Log("[BattleSceneLoader] Debug data created");

            PrintPartyData();
        }

        yield return null;

        Debug.Log("[BattleSceneLoader] Spawn start");

        if (placementController != null && placementController.NeedsPlacement())
        {
            placementController.BeginPlacement();
        }
        else
        {
            unitSpawner.SpawnFromRuntimeData();
        }

        SpawnMonstersFromBattleMap();

        Debug.Log("[BattleSceneLoader] Spawn end");

        yield return null;

        SetLoading(false);

        Debug.Log("[BattleSceneLoader] Battle scene ready.");
    }

    private void PrintPartyData()
    {
        var party = DataManager.Instance.PartyRuntimeStore;

        Debug.Log("[BattleSceneLoader] Party Data");

        for (int i = 0; i < party.MaxPartyCountValue; i++)
        {
            Debug.Log($"Slot {i}: {party.GetCharacterId(i)} / Grid {party.GetGridIndex(i)}");
        }
    }

    private void SpawnMonstersFromBattleMap()
    {
        var mapRuntime = DataManager.Instance.MapRuntimeStore.Get();

        if (mapRuntime == null || string.IsNullOrWhiteSpace(mapRuntime.CurrentMapId))
        {
            Debug.LogError("[BattleSceneLoader] CurrentMapId가 없습니다.");
            return;
        }

        var mapData = DataManager.Instance.MapDatabase.Get(mapRuntime.CurrentMapId);

        if (mapData == null || string.IsNullOrWhiteSpace(mapData.BattleMapId))
        {
            Debug.LogError($"[BattleSceneLoader] BattleMapId가 없습니다. MapId: {mapRuntime.CurrentMapId}");
            return;
        }

        var spawns = DataManager.Instance.BattleMapDatabase.GetSpawns(mapData.BattleMapId);

        Debug.Log($"[BattleSceneLoader] CurrentMapId: {mapRuntime.CurrentMapId}");
        Debug.Log($"[BattleSceneLoader] BattleMapId: {mapData.BattleMapId}");

        foreach (var spawn in spawns)
        {
            Debug.Log($"Spawn Monster: {spawn.MonsterId}, Cells: {string.Join(", ", spawn.GetOccupiedCells())}");
        }

        foreach (var spawnData in spawns)
        {
            monsterSpawner.Spawn(spawnData);
        }
    }

    private void debugProviderCheck()
    {
        Debug.Log($"[BattleSceneLoader] Debug Provider: {debugDataProvider.name}");
    }

    private void SetLoading(bool value)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(value);
    }
}