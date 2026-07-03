using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIDissolveReveal : MonoBehaviour
{
    private const int DefaultGridWidth = 7;
    private const int DefaultGridHeight = 5;

    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private string revealProperty = "_Reveal";
    [SerializeField] private string directionProperty = "_Direction";

    [SerializeField, Range(0f, 1f)] private float hiddenReveal = 0f;
    [SerializeField, Range(0f, 1f)] private float shownReveal = 0.5f;

    [Header("Info Content Alignment")]
    [SerializeField] private HorizontalOrVerticalLayoutGroup infoContentLayout;
    [SerializeField] private RectTransform monsterInfoPanel;
    [SerializeField] private MonsterInfoPanelUI monsterInfoPanelUI;
    [SerializeField] private string autoMonsterInfoPanelName = "MonsterInfoPanel";
    [SerializeField] private TextAnchor leftAlignment = TextAnchor.UpperLeft;
    [SerializeField] private TextAnchor rightAlignment = TextAnchor.UpperRight;
    [SerializeField] private float leftPanelPositionX = 0f;
    [SerializeField] private float rightPanelPositionX = 1200f;

    [Header("Battle Monster Reveal")]
    [SerializeField] private bool enableKeyboardDebugInput;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool deactivateSelfWhenHidden = true;

    [Header("Battle UI Close Policy")]
    [SerializeField] private bool closeSkillListPanelWhenShown = true;
    [SerializeField] private SkillListPanel skillListPanel;
    [SerializeField, Min(1)] private int gridWidth = DefaultGridWidth;
    [SerializeField, Min(1)] private int gridHeight = DefaultGridHeight;
    [SerializeField] private GameObject[] objectsEnabledWhileVisible;
    [SerializeField]
    private string[] autoEnableObjectNames =
    {
        "DissolveCamera",
        "DissolvePanelCanvas"
    };

    [Header("Info Panel Click Policy")]
    [SerializeField] private CanvasGroup infoPanelCanvasGroup;
    [SerializeField] private string autoInfoPanelRootName = "DissolvePanelCanvas";
    [SerializeField] private bool disableInfoPanelClickInteraction = true;
    [SerializeField] private bool blockInfoPanelRaycasts;

    [Header("Render Output Front Sorting")]
    [SerializeField] private bool bringVisibleObjectsToFront = true;
    [SerializeField] private bool forceRenderOutputCanvasSorting = true;
    [SerializeField] private int renderOutputCanvasSortingOrder = 30000;
    [SerializeField] private string autoRenderOutputRootName = "RawImage(RT)";
    [SerializeField] private Canvas renderOutputCanvas;

    private Material runtimeMaterial;
    private Coroutine routine;
    private readonly Vector3[] worldCorners = new Vector3[4];
    private GameObject[] cachedObjectsEnabledWhileVisible;
    private bool initialized;
    private bool isVisible;
    private bool lastRevealFromLeft = true;
    private bool showInProgress;
    private int inputSuppressedUntilFrame = -1;

    private void Awake()
    {
        InitializeIfNeeded();

        if (hideOnAwake && !showInProgress)
            HideImmediate();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        if (targetRawImage == null)
            targetRawImage = GetComponent<RawImage>();

        if (targetRawImage != null)
            targetRawImage.raycastTarget = false;

        if (targetRawImage != null && targetRawImage.material != null)
        {
            runtimeMaterial = Instantiate(targetRawImage.material);
            runtimeMaterial.name = targetRawImage.material.name + " (Runtime)";
            targetRawImage.material = runtimeMaterial;

            runtimeMaterial.SetFloat(revealProperty, hiddenReveal);
        }

        ResolveControlledObjects();
        ApplyInfoPanelClickPolicy();

        initialized = true;
    }

    private void ResolveControlledObjects()
    {
        List<GameObject> objects = new();
        GameObject alwaysActiveRenderRoot = ResolveAlwaysActiveRenderRoot();

        KeepAlwaysActiveRenderRootOn();
        AddUniqueObjects(objects, objectsEnabledWhileVisible, alwaysActiveRenderRoot);

        if (autoEnableObjectNames != null)
        {
            for (int i = 0; i < autoEnableObjectNames.Length; i++)
            {
                GameObject found = FindSceneGameObject(autoEnableObjectNames[i]);
                AddUniqueObject(objects, found, alwaysActiveRenderRoot);
            }
        }

        cachedObjectsEnabledWhileVisible = objects.ToArray();
    }

    private void ApplyInfoPanelClickPolicy()
    {
        if (!disableInfoPanelClickInteraction)
            return;

        RectTransform panel = ResolveInfoPanelClickRoot();
        if (panel == null)
            return;

        if (infoPanelCanvasGroup == null)
            infoPanelCanvasGroup = panel.GetComponent<CanvasGroup>();

        if (infoPanelCanvasGroup == null)
            infoPanelCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();

        infoPanelCanvasGroup.interactable = false;
        infoPanelCanvasGroup.blocksRaycasts = blockInfoPanelRaycasts;

        Selectable[] selectables = panel.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null)
                selectables[i].interactable = false;
        }
    }

    private void Update()
    {
        if (enableKeyboardDebugInput)
            HandleKeyboardDebugInput();

        HandleHideClickInput();
    }

    private void HandleKeyboardDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ShowFromLeft();

        if (Input.GetKeyDown(KeyCode.RightArrow))
            ShowFromRight();

        if (Input.GetKeyDown(KeyCode.UpArrow))
            HideToLeft();

        if (Input.GetKeyDown(KeyCode.DownArrow))
            HideToRight();
    }

    private void HandleHideClickInput()
    {
        if (!isVisible)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (Time.frameCount <= inputSuppressedUntilFrame)
            return;

        bool pointerOverUI = IsPointerOverUI();
        if (!pointerOverUI && IsScreenPointOverAnyMonster(Input.mousePosition))
            return;

        HideToLastRevealSide();
    }

    public static void ShowForMonsterClick(MonsterUnit monster)
    {
        if (monster == null)
            return;

        UIDissolveReveal reveal = FindBestReveal();
        if (reveal == null)
            return;

        reveal.ShowForMonster(monster);
    }

    public void ShowForMonster(MonsterUnit monster)
    {
        if (monster == null)
            return;

        bool panelOnLeft = ShouldRevealFromLeft(monster.MainGridIndex);
        BindMonsterInfoPanel(monster);
        ShowForGridIndex(monster.MainGridIndex);
        FocusMonsterInfoCamera(monster.transform, panelOnLeft);
    }

    public void ShowForGridIndex(int gridIndex)
    {
        if (ShouldRevealFromLeft(gridIndex))
            ShowFromLeft();
        else
            ShowFromRight();
    }

    public bool ShouldRevealFromLeft(int gridIndex)
    {
        return ShouldRevealFromLeft(gridIndex, gridWidth, gridHeight);
    }

    public static bool ShouldRevealFromLeft(int gridIndex, int gridWidth, int gridHeight)
    {
        if (gridIndex < 0)
            return true;

        int resolvedWidth = Mathf.Max(1, gridWidth);
        int resolvedHeight = Mathf.Max(1, gridHeight);
        int centerColumnStartGridIndex = resolvedHeight * (resolvedWidth / 2);

        return gridIndex >= centerColumnStartGridIndex;
    }

    public void ShowFromLeft()
    {
        lastRevealFromLeft = true;
        SetDirection(0f);
        ShowWithContentAlignment(false);
    }

    public void ShowFromRight()
    {
        lastRevealFromLeft = false;
        SetDirection(1f);
        ShowWithContentAlignment(true);
    }

    public void HideToLeft()
    {
        SetDirection(0f);
        Hide();
    }

    public void HideToRight()
    {
        SetDirection(1f);
        Hide();
    }

    public void Show()
    {
        ShowWithContentAlignment(!lastRevealFromLeft);
    }

    private void ShowWithContentAlignment(bool? alignRight)
    {
        showInProgress = true;
        InitializeIfNeeded();

        try
        {
            CloseSkillListPanelIfNeeded();

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            SetControlledObjectsActive(true);

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            ApplyFrontSorting();

            if (alignRight.HasValue)
                ApplyContentAlignment(alignRight.Value);

            isVisible = true;
            inputSuppressedUntilFrame = Time.frameCount;

            Play(hiddenReveal, shownReveal, false);
        }
        finally
        {
            showInProgress = false;
        }
    }

    public void Hide()
    {
        InitializeIfNeeded();
        bool wasVisible = isVisible;
        isVisible = false;

        if (wasVisible)
            ReturnMonsterInfoCameraFocus();

        if (!gameObject.activeInHierarchy)
        {
            HideImmediate();
            return;
        }

        Play(shownReveal, hiddenReveal, true);
    }

    private void CloseSkillListPanelIfNeeded()
    {
        if (!closeSkillListPanelWhenShown)
            return;

        if (skillListPanel == null)
            skillListPanel = FindFirstObjectByType<SkillListPanel>(FindObjectsInactive.Include);

        if (skillListPanel == null)
            return;

        if (!skillListPanel.IsOpen())
            return;

        skillListPanel.Close();
    }

    public void HideImmediate()
    {
        InitializeIfNeeded();
        bool wasVisible = isVisible;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        isVisible = false;

        if (wasVisible)
            ReturnMonsterInfoCameraFocus();

        if (runtimeMaterial != null)
            runtimeMaterial.SetFloat(revealProperty, hiddenReveal);

        ResetRenderOutputCanvasSorting();
        SetControlledObjectsActive(false);

        if (deactivateSelfWhenHidden && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void Play(float from, float to, bool deactivateWhenDone)
    {
        InitializeIfNeeded();

        if (runtimeMaterial == null)
        {
            if (deactivateWhenDone)
                HideImmediate();

            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(RevealRoutine(from, to, deactivateWhenDone));
    }

    private IEnumerator RevealRoutine(float from, float to, bool deactivateWhenDone)
    {
        float time = 0f;
        runtimeMaterial.SetFloat(revealProperty, from);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            float value = Mathf.Lerp(from, to, t);
            runtimeMaterial.SetFloat(revealProperty, value);

            yield return null;
        }

        runtimeMaterial.SetFloat(revealProperty, to);
        routine = null;

        if (deactivateWhenDone)
            HideImmediate();
    }

    private void SetDirection(float direction)
    {
        InitializeIfNeeded();

        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat(directionProperty, direction);
    }

    private void HideToLastRevealSide()
    {
        if (lastRevealFromLeft)
            HideToLeft();
        else
            HideToRight();
    }

    private void FocusMonsterInfoCamera(Transform target, bool panelOnLeft)
    {
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
            cameraController.FocusMonsterInfoWithPanelSide(target, panelOnLeft);
    }

    private void ReturnMonsterInfoCameraFocus()
    {
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
            cameraController.ReturnDefaultFromMonsterInfoFocus();
    }

    private void SetControlledObjectsActive(bool active)
    {
        GameObject alwaysActiveRenderRoot = ResolveAlwaysActiveRenderRoot();
        KeepAlwaysActiveRenderRootOn();

        if (cachedObjectsEnabledWhileVisible == null)
            return;

        for (int i = 0; i < cachedObjectsEnabledWhileVisible.Length; i++)
        {
            GameObject controlledObject = cachedObjectsEnabledWhileVisible[i];
            if (controlledObject == null)
                continue;

            if (controlledObject == alwaysActiveRenderRoot)
            {
                controlledObject.SetActive(true);
                continue;
            }

            controlledObject.SetActive(active);
        }
    }

    private void KeepAlwaysActiveRenderRootOn()
    {
        GameObject alwaysActiveRenderRoot = ResolveAlwaysActiveRenderRoot();
        if (alwaysActiveRenderRoot != null && !alwaysActiveRenderRoot.activeSelf)
            alwaysActiveRenderRoot.SetActive(true);
    }

    private GameObject ResolveAlwaysActiveRenderRoot()
    {
        if (targetRawImage != null &&
            targetRawImage.transform.parent != null &&
            targetRawImage.transform.parent.gameObject != gameObject)
        {
            return targetRawImage.transform.parent.gameObject;
        }

        GameObject autoRoot = FindSceneGameObject(autoRenderOutputRootName);
        return autoRoot != gameObject ? autoRoot : null;
    }

    private void ApplyFrontSorting()
    {
        if (bringVisibleObjectsToFront)
        {
            MoveToLastSibling(gameObject);

            if (targetRawImage != null)
            {
                MoveToLastSibling(targetRawImage.gameObject);

                if (targetRawImage.transform.parent != null)
                    MoveToLastSibling(targetRawImage.transform.parent.gameObject);
            }

            if (cachedObjectsEnabledWhileVisible != null)
            {
                for (int i = 0; i < cachedObjectsEnabledWhileVisible.Length; i++)
                    MoveToLastSibling(cachedObjectsEnabledWhileVisible[i]);
            }
        }

        if (!forceRenderOutputCanvasSorting)
            return;

        Canvas canvas = ResolveRenderOutputCanvas();
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = renderOutputCanvasSortingOrder;
    }

    private void ResetRenderOutputCanvasSorting()
    {
        if (!forceRenderOutputCanvasSorting)
            return;

        Canvas canvas = ResolveRenderOutputCanvas();
        if (canvas == null)
            return;

        canvas.overrideSorting = false;
        canvas.sortingOrder = 0;
    }

    private Canvas ResolveRenderOutputCanvas()
    {
        if (renderOutputCanvas != null)
            return renderOutputCanvas;

        GameObject renderRoot = ResolveRenderOutputRoot();
        if (renderRoot == null)
            return null;

        renderOutputCanvas = renderRoot.GetComponent<Canvas>();
        if (renderOutputCanvas == null)
            renderOutputCanvas = renderRoot.AddComponent<Canvas>();

        return renderOutputCanvas;
    }

    private GameObject ResolveRenderOutputRoot()
    {
        if (targetRawImage != null && targetRawImage.transform.parent != null)
            return targetRawImage.transform.parent.gameObject;

        GameObject autoRoot = FindSceneGameObject(autoRenderOutputRootName);
        if (autoRoot != null)
            return autoRoot;

        return targetRawImage != null ? targetRawImage.gameObject : gameObject;
    }

    private static void MoveToLastSibling(GameObject target)
    {
        if (target != null && target.transform.parent != null)
            target.transform.SetAsLastSibling();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return false;
    }

    private bool IsScreenPointOverAnyMonster(Vector2 screenPoint)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPoint);
        RaycastHit2D[] rayHits = Physics2D.GetRayIntersectionAll(ray);

        for (int i = 0; i < rayHits.Length; i++)
        {
            Collider2D hitCollider = rayHits[i].collider;
            if (hitCollider != null && hitCollider.GetComponentInParent<MonsterUnit>() != null)
                return true;
        }

        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPoint);
        Collider2D[] overlapHits = Physics2D.OverlapPointAll(new Vector2(worldPoint.x, worldPoint.y));

        for (int i = 0; i < overlapHits.Length; i++)
        {
            Collider2D hitCollider = overlapHits[i];
            if (hitCollider != null && hitCollider.GetComponentInParent<MonsterUnit>() != null)
                return true;
        }

        return false;
    }

    private void AlignContentLeft()
    {
        ApplyContentAlignment(false);
    }

    private void AlignContentRight()
    {
        ApplyContentAlignment(true);
    }

    private void ApplyContentAlignment(bool alignRight)
    {
        RectTransform panel = ResolveMonsterInfoPanel();
        if (panel == null)
            return;

        Vector2 anchoredPosition = panel.anchoredPosition;
        anchoredPosition.x = alignRight ? rightPanelPositionX : leftPanelPositionX;
        panel.anchoredPosition = anchoredPosition;

        if (infoContentLayout != null)
            infoContentLayout.childAlignment = alignRight ? rightAlignment : leftAlignment;
    }

    private void BindMonsterInfoPanel(MonsterUnit monster)
    {
        MonsterInfoPanelUI panelUI = ResolveMonsterInfoPanelUI();
        if (panelUI == null)
            return;

        panelUI.Bind(monster);
    }

    private MonsterInfoPanelUI ResolveMonsterInfoPanelUI()
    {
        if (monsterInfoPanelUI != null)
            return monsterInfoPanelUI;

        RectTransform panel = ResolveMonsterInfoPanel();
        if (panel != null)
        {
            monsterInfoPanelUI = panel.GetComponent<MonsterInfoPanelUI>();
            if (monsterInfoPanelUI == null)
                monsterInfoPanelUI = panel.GetComponentInChildren<MonsterInfoPanelUI>(true);
        }

        if (monsterInfoPanelUI == null)
        {
            GameObject autoPanel = FindSceneGameObject(autoMonsterInfoPanelName);
            if (autoPanel != null)
                monsterInfoPanelUI = autoPanel.GetComponentInChildren<MonsterInfoPanelUI>(true);
        }

        return monsterInfoPanelUI;
    }

    private RectTransform ResolveMonsterInfoPanel()
    {
        if (monsterInfoPanel != null)
            return monsterInfoPanel;

        GameObject autoPanel = FindSceneGameObject(autoMonsterInfoPanelName);
        if (autoPanel != null)
        {
            monsterInfoPanel = autoPanel.GetComponent<RectTransform>();

            if (infoContentLayout == null)
                infoContentLayout = autoPanel.GetComponent<HorizontalOrVerticalLayoutGroup>();

            if (monsterInfoPanel != null)
                return monsterInfoPanel;
        }

        if (infoContentLayout == null)
            return null;

        monsterInfoPanel = infoContentLayout.GetComponent<RectTransform>();
        return monsterInfoPanel;
    }

    private RectTransform ResolveInfoPanelClickRoot()
    {
        RectTransform panel = ResolveMonsterInfoPanel();
        if (panel != null)
            return panel;

        GameObject autoRoot = FindSceneGameObject(autoInfoPanelRootName);
        return autoRoot != null ? autoRoot.GetComponent<RectTransform>() : null;
    }

    private Bounds GetBoundsInPanel(RectTransform panel, RectTransform rect)
    {
        rect.GetWorldCorners(worldCorners);

        Vector3 min = panel.InverseTransformPoint(worldCorners[0]);
        Vector3 max = min;

        for (int i = 1; i < worldCorners.Length; i++)
        {
            Vector3 point = panel.InverseTransformPoint(worldCorners[i]);
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Bounds bounds = new();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    private static UIDissolveReveal FindBestReveal()
    {
        UIDissolveReveal[] reveals =
            FindObjectsByType<UIDissolveReveal>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        if (reveals == null || reveals.Length <= 0)
            return null;

        UIDissolveReveal fallback = null;
        for (int i = 0; i < reveals.Length; i++)
        {
            UIDissolveReveal reveal = reveals[i];
            if (reveal == null)
                continue;

            if (reveal.gameObject.activeInHierarchy)
                return reveal;

            if (fallback == null)
                fallback = reveal;
        }

        return fallback;
    }

    private static void AddUniqueObjects(
        List<GameObject> objects,
        GameObject[] candidates,
        GameObject excludedObject = null)
    {
        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Length; i++)
            AddUniqueObject(objects, candidates[i], excludedObject);
    }

    private static void AddUniqueObject(
        List<GameObject> objects,
        GameObject candidate,
        GameObject excludedObject = null)
    {
        if (objects == null || candidate == null || candidate == excludedObject)
            return;

        if (!objects.Contains(candidate))
            objects.Add(candidate);
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return null;

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            if (root.name == objectName)
                return root;

            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
