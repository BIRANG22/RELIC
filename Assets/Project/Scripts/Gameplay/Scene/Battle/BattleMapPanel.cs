using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleMapPanel : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private MapViewSpawner mapViewSpawner;
    [SerializeField] private ScrollRect mapScrollRect;
    [Header("Owner")]
    [SerializeField] private BattleSceneController battleSceneController;

    private MapRuntimeStore runtimeStore;
    private MapRuntimeData runtime;

    public void Open(MapRuntimeData mapRuntime)
    {
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

        if (runtime.IsRunInitialized &&
            runtime.GeneratedNodes != null &&
            runtime.GeneratedNodes.Count > 0)
        {
            return;
        }

        List<MapData> mapPool = DataManager.Instance.MapDatabase.GetAll();

        Debug.Log($"[BattleMapPanel] Chapter: {runtime.SelectedChapterId}, Stage: {runtime.CurrentStage}");

        ProceduralMapGenerator generator = new();

        runtime.GeneratedNodes = generator.Generate(
            mapPool,
            runtime.SelectedChapterId,
            runtime.CurrentStage
        );

        runtime.IsRunInitialized = true;

        runtimeStore.Set(runtime);
    }

    private void SpawnMapView()
    {
        if (mapViewSpawner == null)
        {
            Debug.LogWarning("[BattleMapPanel] MapViewSpawner가 연결되지 않았습니다.");
            return;
        }

        mapViewSpawner.Spawn(runtime.GeneratedNodes, OnNodeClicked);

        Canvas.ForceUpdateCanvases();

        if (mapScrollRect != null)
            mapScrollRect.verticalNormalizedPosition = 0f;
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