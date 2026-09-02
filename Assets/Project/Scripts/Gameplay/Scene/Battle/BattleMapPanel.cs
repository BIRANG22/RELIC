using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleMapPanel : MonoBehaviour
{
    private const string MapDataResolutionVersion = "MapDataV2";

    [Header("View")]
    [SerializeField] private MapViewSpawner mapViewSpawner;
    [SerializeField] private ScrollRect mapScrollRect;
    [SerializeField] private BattleNextNodeSelectionPanel nextNodeSelectionPanel;
    [SerializeField] private BattleMapPartyInfoPresenter partyInfoPresenter;
    [SerializeField] private BattleMapNodeInfoPresenter nodeInfoPresenter;
    [Header("Owner")]
    [SerializeField] private BattleSceneController battleSceneController;
    [Header("Generation")]
    [SerializeField] private ManualBattleMapTemplate manualMapTemplate;
    [SerializeField] private EventMapRandomExclusionSettings eventMapRandomExclusionSettings = new();
    private MapRuntimeStore runtimeStore;
    private MapRuntimeData runtime;
    private bool isNextNodeSelectionProcessing;

    private void Awake()
    {
        EnsureNextNodeSelectionPanel();
        EnsurePartyInfoPresenter();
        EnsureNodeInfoPresenter();
    }

    public void Open(MapRuntimeData mapRuntime)
    {
        isNextNodeSelectionProcessing = false;
        gameObject.SetActive(true);
        Prepare(mapRuntime);
        SpawnMapView();
        ShowCurrentNodeInfo();
        partyInfoPresenter?.RefreshFromRuntime();

        if (nextNodeSelectionPanel != null)
            nextNodeSelectionPanel.Open(runtime, OnNextNodeSelected);
    }

    public void Prepare(MapRuntimeData mapRuntime)
    {
        runtimeStore = DataManager.Instance.MapRuntimeStore;
        runtime = mapRuntime;
        EnsureMapGenerated();

        if (runtime?.IsManualMapTemplate != true)
            BattleMapLayoutUtility.ApplyHorizontalLayout(runtime?.GeneratedNodes);

        runtimeStore?.Set(runtime);
    }

    public void Close()
    {
        if (nextNodeSelectionPanel != null)
            nextNodeSelectionPanel.Close();

        gameObject.SetActive(false);
    }

    private void EnsureMapGenerated()
    {
        if (runtime == null)
        {
            Debug.LogWarning("[BattleMapPanel] MapRuntimeData가 없습니다.");
            return;
        }

        string manualTemplateKey = manualMapTemplate != null
            ? manualMapTemplate.GetRuntimeKey()
            : string.Empty;
        string randomExclusionKey = eventMapRandomExclusionSettings != null
            ? eventMapRandomExclusionSettings.GetRuntimeKey()
            : string.Empty;
        string generationKey = BuildGenerationKey(manualTemplateKey, randomExclusionKey);

        if (!BattleMapRuntimeGenerationPolicy.ShouldRegenerate(runtime, generationKey))
        {
            return;
        }

        List<MapData> mapPool = DataManager.Instance.MapDatabase.GetAll();

        Debug.Log($"[BattleMapPanel] Stage: {runtime.CurrentStage}");

        BattleMapGenerationResult generationResult = BattleMapGenerationResolver.GenerateResult(
            mapPool,
            runtime.SelectedChapterId,
            runtime.CurrentStage,
            manualMapTemplate,
            eventMapRandomExclusionSettings
        );
        runtime.GeneratedNodes = generationResult.Nodes;
        runtime.IsManualMapTemplate = generationResult.UsedManualTemplate;
        runtime.ManualMapTemplateKey = generationResult.UsedManualTemplate
            ? manualTemplateKey
            : string.Empty;
        runtime.MapGenerationKey = generationKey;

        runtime.IsRunInitialized = true;
        BattleMapRuntimeGenerationPolicy.ResetProgressForRegeneratedMap(runtime);

        runtimeStore.Set(runtime);
    }

    private static string BuildGenerationKey(string manualTemplateKey, string randomExclusionKey)
    {
        bool hasManualTemplate = !string.IsNullOrWhiteSpace(manualTemplateKey);
        bool hasRandomExclusion = !string.IsNullOrWhiteSpace(randomExclusionKey);

        if (!hasManualTemplate && !hasRandomExclusion)
            return $"BattleMapGeneration:{MapDataResolutionVersion}";

        if (!hasRandomExclusion)
            return $"BattleMapGeneration:{MapDataResolutionVersion}:{manualTemplateKey.Trim()}";

        if (!hasManualTemplate)
            return $"BattleMapGeneration:{MapDataResolutionVersion}:{randomExclusionKey.Trim()}";

        return $"BattleMapGeneration:{MapDataResolutionVersion}:{manualTemplateKey.Trim()}|{randomExclusionKey.Trim()}";
    }

    private void SpawnMapView()
    {
        if (mapViewSpawner == null)
        {
            Debug.LogWarning("[BattleMapPanel] MapViewSpawner가 연결되지 않았습니다.");
            return;
        }

        mapViewSpawner.Spawn(runtime.GeneratedNodes, OnNodeClicked, OnNodeHovered, OnNodeHoverExited);

        Canvas.ForceUpdateCanvases();

        // Map의 가로 위치와 폭은 MapViewSpawner가 단독으로 관리합니다.
        // BattleMapPanel에서는 ScrollRect content 위치를 다시 계산하지 않습니다.
    }
    private List<GeneratedMapNodeData> FindClickableNextNodes()
    {
        List<GeneratedMapNodeData> result = new();

        if (runtime == null || runtime.GeneratedNodes == null)
            return result;

        for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
        {
            GeneratedMapNodeData node = runtime.GeneratedNodes[i];

            if (node == null)
                continue;

            if (MapRuntimeProgressUtility.IsNodeClickableFromCurrentProgress(runtime, node))
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
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (battleSceneController == null)
        {
            Debug.LogWarning("[BattleMapPanel] BattleSceneController가 연결되지 않았습니다.");
            return;
        }

        battleSceneController.OnMapNodeSelected(nodeData);
    }

    private void OnNextNodeSelected(int nodeIndex)
    {
        if (isNextNodeSelectionProcessing)
            return;

        if (battleSceneController == null)
        {
            Debug.LogWarning("[BattleMapPanel] BattleSceneController가 연결되지 않았습니다.");
            return;
        }

        isNextNodeSelectionProcessing = true;

        // 선택지 클릭 직후 방 전환을 시작하지 않고, 지도상의 실제 노드에
        // X 표시 애니메이션을 끝까지 재생한 다음 기존 선택 흐름을 실행합니다.
        if (mapViewSpawner != null &&
            mapViewSpawner.PlayNodeCheckAnimation(nodeIndex,
                () => battleSceneController.OnMapNodeSelectedByIndex(nodeIndex)))
        {
            return;
        }

        // 지도 노드 뷰를 찾을 수 없는 예외 상황에서는 기존 동작으로 안전하게 진행합니다.
        battleSceneController.OnMapNodeSelectedByIndex(nodeIndex);
    }

    private void EnsureNextNodeSelectionPanel()
    {
        if (nextNodeSelectionPanel == null)
            nextNodeSelectionPanel = GetComponentInChildren<BattleNextNodeSelectionPanel>(true);

        if (nextNodeSelectionPanel == null)
        {
            GameObject panelObject = new("NextNodeSelectionRoot", typeof(RectTransform),
                typeof(BattleNextNodeSelectionPanel));
            panelObject.transform.SetParent(transform, false);

            RectTransform panelRect = (RectTransform)panelObject.transform;
            panelRect.anchorMin = new Vector2(0.77f, 0f);
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(12f, 24f);
            panelRect.offsetMax = new Vector2(-20f, -46f);
            nextNodeSelectionPanel = panelObject.GetComponent<BattleNextNodeSelectionPanel>();
        }

    }

    private void EnsurePartyInfoPresenter()
    {
        if (partyInfoPresenter == null)
            partyInfoPresenter = GetComponentInChildren<BattleMapPartyInfoPresenter>(true);
    }

    private void EnsureNodeInfoPresenter()
    {
        if (nodeInfoPresenter == null)
            nodeInfoPresenter = GetComponentInChildren<BattleMapNodeInfoPresenter>(true);
    }

    private void OnNodeHovered(GeneratedMapNodeData node, Sprite icon)
    {
        nodeInfoPresenter?.Show(node, icon);
    }

    private void OnNodeHoverExited()
    {
        // 최근에 호버한 노드 정보를 그대로 유지합니다.
    }

    private void ShowCurrentNodeInfo()
    {
        EnsureNodeInfoPresenter();
        if (nodeInfoPresenter == null) return;

        GeneratedMapNodeData node = MapRuntimeProgressUtility.FindCurrentNode(runtime)
            ?? MapRuntimeProgressUtility.FindStartNode(runtime);
        if (node == null) return;

        Sprite icon = null;
        MapNodeIconDatabase iconDatabase = DataManager.Instance?.MapNodeIconDatabase;
        iconDatabase?.TryGetIcon(node.Type, out icon);
        nodeInfoPresenter.Show(node, icon);
    }
}

public static class BattleMapScrollUtility
{
    public static float CalculateContentWidth(
        float minNodeX,
        float maxNodeX,
        float viewportWidth,
        float horizontalPadding)
    {
        float nodeSpan = Mathf.Max(0f, maxNodeX - minNodeX);
        return Mathf.Max(viewportWidth, nodeSpan + horizontalPadding);
    }

    public static float CalculateAnchoredX(
        float currentNodeX,
        float minNodeX,
        float contentWidth,
        float viewportWidth)
    {
        float desiredX = -(currentNodeX - minNodeX);
        float minimumX = -Mathf.Max(0f, contentWidth - viewportWidth);
        return Mathf.Clamp(desiredX, minimumX, 0f);
    }
}
