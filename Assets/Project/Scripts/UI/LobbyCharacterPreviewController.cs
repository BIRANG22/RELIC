using UnityEngine;

public class LobbyCharacterPreviewController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject lobbyMainPanel;
    [SerializeField] private GameObject characterSettingPanel;

    [Header("Preview Root")]
    [SerializeField] private GameObject previewRoot;

    [Header("Character Preview Objects")]
    [SerializeField] private GameObject character1Preview;
    [SerializeField] private GameObject character2Preview;
    [SerializeField] private GameObject character3Preview;

    [Header("Settings")]
    [SerializeField] private int defaultCharacterIndex = 0;

    [Header("Preview Canvas Sorting")]
    [Tooltip("CharacterPreviewCanvas가 CharacterSettingPanel보다 앞에 표시되도록 패널 Canvas 기준으로 정렬합니다.")]
    [SerializeField] private Canvas previewCanvas;
    [SerializeField] private int sortingOrderOffset = 1;

    private int currentCharacterIndex;
    private bool previewCanvasStateCached;
    private bool originalOverrideSorting;
    private int originalSortingOrder;
    private int originalSortingLayerId;
    private RenderMode originalRenderMode;
    private Camera originalWorldCamera;
    private float originalPlaneDistance;
    private int originalTargetDisplay;

    private void Awake()
    {
        currentCharacterIndex = Mathf.Clamp(defaultCharacterIndex, 0, 2);
        ResolvePreviewCanvas();
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        RefreshByPanelState();
    }

    public void ShowCharacter(int characterIndex)
    {
        currentCharacterIndex = Mathf.Clamp(characterIndex, 0, 2);
        Refresh();
    }

    public void ShowCharacter1()
    {
        ShowCharacter(0);
    }

    public void ShowCharacter2()
    {
        ShowCharacter(1);
    }

    public void ShowCharacter3()
    {
        ShowCharacter(2);
    }

    public void Refresh()
    {
        bool shouldShow = IsCharacterSettingPanelOpen();

        SetActiveIfNeeded(previewRoot, shouldShow);

        if (!shouldShow)
        {
            SetActiveIfNeeded(character1Preview, false);
            SetActiveIfNeeded(character2Preview, false);
            SetActiveIfNeeded(character3Preview, false);
            return;
        }

        SetActiveIfNeeded(character1Preview, currentCharacterIndex == 0);
        SetActiveIfNeeded(character2Preview, currentCharacterIndex == 1);
        SetActiveIfNeeded(character3Preview, currentCharacterIndex == 2);
    }

    private void RefreshByPanelState()
    {
        bool shouldShow = IsCharacterSettingPanelOpen();

        if (previewRoot != null && previewRoot.activeSelf != shouldShow)
            Refresh();
    }

    private bool IsCharacterSettingPanelOpen()
    {
        if (characterSettingPanel == null)
            return false;

        if (!characterSettingPanel.activeInHierarchy)
            return false;

        // CharacterSettingPanel은 이제 Position 화면 위의 모달이므로
        // LobbyViewStateController의 CharacterSelection 상태 여부와 무관하게 표시합니다.
        return true;
    }

    private void ResolvePreviewCanvas()
    {
        if (previewCanvas != null)
            return;

        if (previewRoot != null)
            previewCanvas = previewRoot.GetComponent<Canvas>();

        if (previewCanvas == null)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null || candidate.name != "CharacterPreviewCanvas")
                    continue;

                previewCanvas = candidate.GetComponent<Canvas>();
                if (previewCanvas != null)
                    break;
            }
        }
    }

    private void ApplyPreviewCanvasSorting(bool visible)
    {
        ResolvePreviewCanvas();
        if (previewCanvas == null)
            return;

        if (!previewCanvasStateCached)
        {
            originalOverrideSorting = previewCanvas.overrideSorting;
            originalSortingOrder = previewCanvas.sortingOrder;
            originalSortingLayerId = previewCanvas.sortingLayerID;
            originalRenderMode = previewCanvas.renderMode;
            originalWorldCamera = previewCanvas.worldCamera;
            originalPlaneDistance = previewCanvas.planeDistance;
            originalTargetDisplay = previewCanvas.targetDisplay;
            previewCanvasStateCached = true;
        }

        if (!visible)
        {
            RestorePreviewCanvasState();
            return;
        }

        Canvas settingCanvas = characterSettingPanel != null
            ? characterSettingPanel.GetComponent<Canvas>()
            : null;

        previewCanvas.overrideSorting = true;

        if (settingCanvas != null)
        {
            Canvas referenceCanvas = settingCanvas.rootCanvas != null
                ? settingCanvas.rootCanvas
                : settingCanvas;

            previewCanvas.renderMode = referenceCanvas.renderMode;
            previewCanvas.targetDisplay = referenceCanvas.targetDisplay;

            if (referenceCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                previewCanvas.worldCamera = referenceCanvas.worldCamera;
                previewCanvas.planeDistance = referenceCanvas.planeDistance;
            }
            else if (referenceCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                previewCanvas.worldCamera = null;
            }

            previewCanvas.sortingLayerID = settingCanvas.sortingLayerID;
            previewCanvas.sortingOrder = settingCanvas.sortingOrder + Mathf.Max(1, sortingOrderOffset);
        }
        else
        {
            previewCanvas.sortingOrder = originalSortingOrder + Mathf.Max(1, sortingOrderOffset);
        }
    }

    private void OnDisable()
    {
        RestorePreviewCanvasState();
    }

    private void RestorePreviewCanvasState()
    {
        if (!previewCanvasStateCached || previewCanvas == null)
            return;

        previewCanvas.overrideSorting = originalOverrideSorting;
        previewCanvas.sortingOrder = originalSortingOrder;
        previewCanvas.sortingLayerID = originalSortingLayerId;
        previewCanvas.renderMode = originalRenderMode;
        previewCanvas.targetDisplay = originalTargetDisplay;
        previewCanvas.worldCamera = originalWorldCamera;
        previewCanvas.planeDistance = originalPlaneDistance;
    }

    private static void SetActiveIfNeeded(GameObject target, bool active)
    {
        if (target == null)
            return;

        if (target.activeSelf != active)
            target.SetActive(active);
    }
}