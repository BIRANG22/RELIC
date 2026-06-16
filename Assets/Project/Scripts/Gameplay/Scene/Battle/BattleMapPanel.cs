using Relic.Gameplay.Data;
using System.Collections;
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
    [Header("Scroll Focus")]
    [SerializeField] private float selectedNodeViewportYRatio = 0.3f;
    [SerializeField] private float focusDelay = 0.02f;

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

        StartCoroutine(FocusCurrentNodeRoutine());
    }

    private IEnumerator FocusCurrentNodeRoutine()
    {
        yield return null;
        yield return new WaitForSecondsRealtime(focusDelay);

        FocusCurrentNode();
    }

    private void FocusCurrentNode()
    {
        if (mapScrollRect == null || runtime == null || runtime.GeneratedNodes == null)
            return;

        if (mapScrollRect.content == null || mapScrollRect.viewport == null)
            return;

        List<GeneratedMapNodeData> focusNodes = FindClickableNextNodes();

        if (focusNodes == null || focusNodes.Count <= 0)
            return;

        float focusY = 0f;

        for (int i = 0; i < focusNodes.Count; i++)
            focusY += focusNodes[i].Position.y;

        focusY /= focusNodes.Count;

        RectTransform content = mapScrollRect.content;
        RectTransform viewport = mapScrollRect.viewport;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        if (contentHeight <= viewportHeight)
            return;

        float desiredViewportY =
            Mathf.Lerp(
                -viewportHeight * 0.5f,
                viewportHeight * 0.5f,
                selectedNodeViewportYRatio
            );

        Vector2 anchoredPosition = content.anchoredPosition;

        anchoredPosition.y = -focusY + desiredViewportY;

        float maxY = contentHeight - viewportHeight;
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 0f, maxY);

        content.anchoredPosition = anchoredPosition;

        Debug.Log(
            $"[MapFocus] FocusY:{focusY} / DesiredViewportY:{desiredViewportY} / AnchoredY:{anchoredPosition.y}"
        );
    }
    private List<GeneratedMapNodeData> FindClickableNextNodes()
    {
        List<GeneratedMapNodeData> result = new();

        if (runtime == null || runtime.GeneratedNodes == null)
            return result;

        if (runtime.CurrentNodeIndex < 0)
        {
            for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
            {
                GeneratedMapNodeData node = runtime.GeneratedNodes[i];

                if (node != null && node.Type == "Start")
                    result.Add(node);
            }

            return result;
        }

        GeneratedMapNodeData currentNode = FindNodeByIndex(runtime.CurrentNodeIndex);

        if (currentNode == null)
            return result;

        for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
        {
            GeneratedMapNodeData node = runtime.GeneratedNodes[i];

            if (node == null)
                continue;

            if (currentNode.NextNodeIndices.Contains(node.NodeIndex))
                result.Add(node);
        }

        return result;
    }

    private GeneratedMapNodeData FindNodeByIndex(int nodeIndex)
    {
        if (runtime == null || runtime.GeneratedNodes == null)
            return null;

        for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
        {
            GeneratedMapNodeData node = runtime.GeneratedNodes[i];

            if (node == null)
                continue;

            if (node.NodeIndex == nodeIndex)
                return node;
        }

        return null;
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