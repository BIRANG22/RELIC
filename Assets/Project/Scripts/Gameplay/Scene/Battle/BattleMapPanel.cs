using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class BattleMapPanel : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private MapViewSpawner mapViewSpawner;

    [Header("Owner")]
    [SerializeField] private BattleSceneController battleSceneController;

    private MapRuntimeStore runtimeStore;
    private MapRuntimeData runtime;

    public void Open(MapRuntimeData mapRuntime)
    {
        Debug.Log("[BattleMapPanel] Open 호출됨");

        gameObject.SetActive(true);

        runtimeStore = DataManager.Instance.MapRuntimeStore;
        runtime = mapRuntime;

        EnsureMapGenerated();
        SpawnMapView();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void EnsureMapGenerated()
    {
        if (runtime == null)
        {
            Debug.LogWarning("[BattleMapPanel] MapRuntimeData가 없습니다.");
            return;
        }

        if (runtime.IsRunInitialized)
            return;

        List<MapData> mapPool = DataManager.Instance.MapDatabase.GetAll();

        ProceduralMapGenerator generator = new();

        runtime.GeneratedNodes = generator.Generate(
            mapPool,
            runtime.SelectedChapterId,
            runtime.CurrentStage
        );

        runtime.IsRunInitialized = true;

        runtimeStore.Set(runtime);

        Debug.Log($"[BattleMapPanel] Procedural Map Generated: {runtime.GeneratedNodes.Count}");
    }

    private void SpawnMapView()
    {
        if (mapViewSpawner == null)
        {
            Debug.LogWarning("[BattleMapPanel] MapViewSpawner가 연결되지 않았습니다.");
            return;
        }

        mapViewSpawner.Spawn(runtime.GeneratedNodes, OnNodeClicked);
    }

    private void OnNodeClicked(GeneratedMapNodeData nodeData)
    {
        if (battleSceneController == null)
        {
            Debug.LogWarning("[BattleMapPanel] BattleSceneController가 연결되지 않았습니다.");
            return;
        }

        battleSceneController.OnMapNodeSelected(nodeData);
    }
}