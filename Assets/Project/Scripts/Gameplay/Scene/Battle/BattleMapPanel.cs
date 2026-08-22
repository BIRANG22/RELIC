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
    [SerializeField] private BattleNextNodeSelectionPanel nextNodeSelectionPanel;
    [SerializeField] private BattleMapPartyInfoPresenter partyInfoPresenter;
    [SerializeField] private BattleMapNodeInfoPresenter nodeInfoPresenter;
    [Header("Owner")]
    [SerializeField] private BattleSceneController battleSceneController;
    [Header("Generation")]
    [SerializeField] private ManualBattleMapTemplate manualMapTemplate;
    [SerializeField] private EventMapRandomExclusionSettings eventMapRandomExclusionSettings = new();
    [Header("Scroll Focus")]
    [SerializeField] private float horizontalContentPadding = 40f;
    [SerializeField] private float focusDelay = 0.02f;

    private MapRuntimeStore runtimeStore;
    private MapRuntimeData runtime;

    private void Awake()
    {
        EnsureNextNodeSelectionPanel();
        EnsurePartyInfoPresenter();
        EnsureNodeInfoPresenter();
    }

    public void Open(MapRuntimeData mapRuntime)
    {
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
            return string.Empty;

        if (!hasRandomExclusion)
            return manualTemplateKey.Trim();

        if (!hasManualTemplate)
            return randomExclusionKey.Trim();

        return $"BattleMapGeneration:{manualTemplateKey.Trim()}|{randomExclusionKey.Trim()}";
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
        ConfigureScrollContentWidth();

        StartCoroutine(FocusCurrentNodeRoutine());
    }

    private void ConfigureScrollContentWidth()
    {
        if (mapScrollRect == null || mapScrollRect.content == null ||
            mapScrollRect.viewport == null || runtime?.GeneratedNodes == null ||
            runtime.GeneratedNodes.Count == 0)
        {
            return;
        }

        GetHorizontalNodeBounds(out float minNodeX, out float maxNodeX);

        Vector2 size = mapScrollRect.content.sizeDelta;
        size.x = BattleMapScrollUtility.CalculateContentWidth(
            minNodeX,
            maxNodeX,
            mapScrollRect.viewport.rect.width,
            horizontalContentPadding);
        mapScrollRect.content.sizeDelta = size;

        Vector2 position = mapScrollRect.content.anchoredPosition;
        position.x = 0f;
        mapScrollRect.content.anchoredPosition = position;
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

        GeneratedMapNodeData currentNode = FindNodeByIndex(runtime.CurrentNodeIndex);
        if (currentNode == null)
            return;

        RectTransform content = mapScrollRect.content;
        RectTransform viewport = mapScrollRect.viewport;
        GetHorizontalNodeBounds(out float minNodeX, out _);

        Vector2 anchoredPosition = content.anchoredPosition;
        anchoredPosition.x = BattleMapScrollUtility.CalculateAnchoredX(
            currentNode.Position.x,
            minNodeX,
            content.rect.width,
            viewport.rect.width);
        mapScrollRect.StopMovement();
        content.anchoredPosition = anchoredPosition;

        Debug.Log(
            $"[MapFocus] CurrentNode:{currentNode.NodeIndex} / NodeX:{currentNode.Position.x} / AnchoredX:{anchoredPosition.x}"
        );
    }

    private void GetHorizontalNodeBounds(out float minNodeX, out float maxNodeX)
    {
        minNodeX = float.MaxValue;
        maxNodeX = float.MinValue;

        for (int i = 0; i < runtime.GeneratedNodes.Count; i++)
        {
            GeneratedMapNodeData node = runtime.GeneratedNodes[i];
            if (node == null)
                continue;

            minNodeX = Mathf.Min(minNodeX, node.Position.x);
            maxNodeX = Mathf.Max(maxNodeX, node.Position.x);
        }

        if (minNodeX == float.MaxValue)
            minNodeX = 0f;
        if (maxNodeX == float.MinValue)
            maxNodeX = minNodeX;
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
        if (battleSceneController == null)
        {
            Debug.LogWarning("[BattleMapPanel] BattleSceneController가 연결되지 않았습니다.");
            return;
        }

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
