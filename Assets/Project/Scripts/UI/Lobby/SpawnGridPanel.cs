using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class SpawnGridPanel : MonoBehaviour
{
    [SerializeField] private SpawnGridCell[] cells;

    [Header("Ready Panel Grid")]
    [Tooltip("Ready_Panel/Grid/Grid01~Grid15를 이름으로 자동 연결합니다.")]
    [SerializeField] private bool autoBindReadyPanelGrid = true;
    [Tooltip("Info_Panel에서 선택한 캐릭터를 Ready_Panel에 배치할 때는 패널 밖 클릭으로 선택을 해제하지 않습니다.")]
    [SerializeField] private bool clearSelectionWhenPointerPressedOutside = false;

    [Header("Party Slot Order Icon Objects")]
    [SerializeField] private GameObject[] partySlotOrderIconObjects;


    private int selectedPartySlotIndex = -1;
    private CharBtn pendingInfoPlacementButton;
    private CharBtn infoDragSourceButton;
    private int activeGridDragSourceIndex = -1;
    private readonly List<RaycastResult> pointerRaycastResults = new List<RaycastResult>();

    [Header("Drag Preview")]
    [Tooltip("캐릭터를 홀드해 옮길 때 표시되는 복사 이미지의 배율입니다.")]
    [SerializeField, Min(0.1f)] private float dragPreviewScale = 4f;
    [Tooltip("드래그 복사 이미지의 투명도입니다.")]
    [SerializeField, Range(0f, 1f)] private float dragPreviewAlpha = 0.6f;
    [Tooltip("Info_Panel / Ready_Panel(9010)보다 앞에 표시할 드래그 복사 Canvas의 Sorting Order입니다.")]
    [SerializeField] private int dragPreviewSortingOrder = 9015;
    [Tooltip("드래그 복사 이미지가 마우스보다 위에서 따라오도록 더할 UI 위치 오프셋입니다.")]
    [SerializeField] private Vector2 dragPreviewPositionOffset = new Vector2(0f, 120f);
    private RectTransform dragPreviewRect;
    private Image dragPreviewImage;
    private Canvas dragPreviewCanvas;

    private void Awake()
    {
        AutoBindReadyGridIfNeeded();
        InitializeCells();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        AutoBindReadyGridIfNeeded();
        InitializeCells();
        Refresh();
    }

    private void Update()
    {
        if (!clearSelectionWhenPointerPressedOutside)
            return;

        if (selectedPartySlotIndex < 0)
            return;

        if (!WasPointerPressedThisFrame())
            return;

        if (IsPointerOverThisDeployPanel())
            return;

        ClearSelection();
    }

    private void InitializeCells()
    {
        if (cells == null)
            return;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                cells[i].Init(this, i);
        }
    }

    private void AutoBindReadyGridIfNeeded()
    {
        if (!autoBindReadyPanelGrid)
            return;

        bool hasAnyAssignedCell = false;
        if (cells != null)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                {
                    hasAnyAssignedCell = true;
                    break;
                }
            }
        }

        if (hasAnyAssignedCell && cells.Length >= 15)
            return;

        Transform panelRoot = ResolveReadyPanelRoot();
        if (panelRoot == null)
            return;

        Transform gridRoot = FindDirectChild(panelRoot, "Grid") ?? FindChildRecursive(panelRoot, "Grid");
        if (gridRoot == null)
            return;

        SpawnGridCell[] bound = new SpawnGridCell[15];
        bool foundAny = false;

        for (int i = 0; i < bound.Length; i++)
        {
            string gridName = "Grid" + (i + 1).ToString("00");
            Transform grid = FindDirectChild(gridRoot, gridName);
            if (grid == null)
                continue;

            // Grid01~15에 사용자가 직접 붙인 SpawnGridCell만 사용합니다.
            // 플레이 중 AddComponent로 Inspector 대상 구조를 변경하지 않습니다.
            SpawnGridCell cell = grid.GetComponent<SpawnGridCell>();
            if (cell == null)
                continue;

            bound[i] = cell;
            foundAny = true;
        }

        if (foundAny)
            cells = bound;
    }

    private Transform ResolveReadyPanelRoot()
    {
        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.name, "Ready_Panel", StringComparison.OrdinalIgnoreCase))
                return current;
            current = current.parent;
        }

        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, "Ready_Panel");
            if (found != null)
                return found;
        }

        return transform;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, targetName, StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    public void ClearSelection()
    {
        if (selectedPartySlotIndex < 0)
            return;

        selectedPartySlotIndex = -1;
        Refresh();
    }

    private bool WasPointerPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    private bool IsPointerOverThisDeployPanel()
    {
        if (EventSystem.current == null)
            return false;

        Vector2 pointerPosition;

        if (Mouse.current != null)
        {
            pointerPosition = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null)
        {
            pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        pointerRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, pointerRaycastResults);

        for (int i = 0; i < pointerRaycastResults.Count; i++)
        {
            GameObject hitObject = pointerRaycastResults[i].gameObject;

            if (hitObject == null)
                continue;

            Transform hitTransform = hitObject.transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 이전 버전 호환용 메서드입니다.
    /// 시작 위치는 더 이상 자동 배치하지 않고 Info_Panel에서 캐릭터를 선택한 뒤
    /// Ready_Panel의 Grid01~15 중 원하는 칸을 직접 눌러 지정합니다.
    /// </summary>
    public void AutoPlacePartyIfNeeded()
    {
        Refresh();
    }

    public bool BeginInfoCharacterDrag(CharBtn sourceButton, PointerEventData eventData)
    {
        if (sourceButton == null || !sourceButton.IsInfoPanelReadyDragSource || sourceButton.IsLocked)
            return false;

        // Ready_Panel에 파티 3명이 모두 배치된 상태에서는
        // Info_Panel에서 새 캐릭터 홀드 드래그를 시작하지 않습니다.
        if (IsReadyPartyFull())
            return false;

        infoDragSourceButton = sourceButton;
        selectedPartySlotIndex = -1;

        if (sourceButton.TryGetReadyDragPreview(out Sprite previewSprite, out Vector2 previewSize))
            ShowDragPreview(previewSprite, previewSize, eventData);

        Refresh();
        return true;
    }

    public void EndInfoCharacterDrag(CharBtn sourceButton)
    {
        if (infoDragSourceButton != sourceButton)
            return;

        infoDragSourceButton = null;
        HideDragPreview();
        Refresh();
    }

    public bool BeginGridDrag(int sourceGridIndex, PointerEventData eventData)
    {
        int partySlotIndex = FindPartySlotByGridIndex(sourceGridIndex);
        if (partySlotIndex < 0)
            return false;

        infoDragSourceButton = null;
        activeGridDragSourceIndex = sourceGridIndex;
        selectedPartySlotIndex = partySlotIndex;

        string characterId = DataManager.Instance?.PartyRuntimeStore?.GetCharacterId(partySlotIndex);
        Vector2 previewSize = IsValidGridIndex(sourceGridIndex)
            ? cells[sourceGridIndex].GetCharacterImageRectSize()
            : new Vector2(96f, 96f);
        if (TryResolveBattleIdleFrameZero(characterId, out Sprite previewSprite))
            ShowDragPreview(previewSprite, previewSize, eventData);

        Refresh();
        return true;
    }

    public void EndGridDrag(int sourceGridIndex, PointerEventData eventData)
    {
        // 다른 Grid에 정상 드롭되었다면 HandleDrop에서 이미 드래그 상태를 종료합니다.
        if (activeGridDragSourceIndex != sourceGridIndex)
            return;

        int partySlotIndex = FindPartySlotByGridIndex(sourceGridIndex);
        bool pointerOverReadyGrid = IsPointerOverAnyReadyGrid(eventData);

        // Grid01~15 바깥으로 드롭한 경우에는 캐릭터 등록 자체를 해제합니다.
        // 다른 Grid 위에 드롭했지만 해당 칸이 이미 사용 중이어서 이동에 실패한 경우에는
        // 기존 배치를 그대로 유지합니다.
        if (!pointerOverReadyGrid && partySlotIndex >= 0)
            UnregisterPartySlot(partySlotIndex);

        activeGridDragSourceIndex = -1;
        selectedPartySlotIndex = -1;
        HideDragPreview();

        if (!pointerOverReadyGrid && partySlotIndex >= 0)
            RefreshAllInfoAndReadyViews();
        else
            Refresh();
    }

    public bool HandleDrop(int targetGridIndex, GameObject pointerDragObject)
    {
        if (DataManager.Instance?.PartyRuntimeStore == null || pointerDragObject == null)
            return false;

        CharBtn charButton = pointerDragObject.GetComponent<CharBtn>()
            ?? pointerDragObject.GetComponentInParent<CharBtn>();

        if (charButton != null && charButton.IsInfoPanelReadyDragSource)
            return TryDropInfoCharacter(charButton, targetGridIndex);

        SpawnGridCell sourceCell = pointerDragObject.GetComponent<SpawnGridCell>()
            ?? pointerDragObject.GetComponentInParent<SpawnGridCell>();

        if (sourceCell != null)
            return TryDropGridCell(sourceCell.GridIndex, targetGridIndex);

        return false;
    }

    private bool TryDropInfoCharacter(CharBtn sourceButton, int targetGridIndex)
    {
        if (sourceButton == null || !IsValidGridIndex(targetGridIndex))
            return false;

        if (FindPartySlotByGridIndex(targetGridIndex) >= 0)
            return false;

        if (!sourceButton.TryRegisterToReadyGrid(targetGridIndex, out _))
            return false;

        if (pendingInfoPlacementButton == sourceButton)
            pendingInfoPlacementButton = null;

        infoDragSourceButton = null;
        selectedPartySlotIndex = -1;
        HideDragPreview();
        RefreshAllInfoAndReadyViews();
        return true;
    }

    private bool TryDropGridCell(int sourceGridIndex, int targetGridIndex)
    {
        if (!IsValidGridIndex(sourceGridIndex) || !IsValidGridIndex(targetGridIndex))
            return false;

        int partySlotIndex = FindPartySlotByGridIndex(sourceGridIndex);
        if (partySlotIndex < 0)
            return false;

        int targetPartySlotIndex = FindPartySlotByGridIndex(targetGridIndex);
        if (targetPartySlotIndex >= 0 && targetPartySlotIndex != partySlotIndex)
            return false;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        if (!partyStore.SetSpawnGridIndex(partySlotIndex, targetGridIndex))
            return false;

        activeGridDragSourceIndex = -1;
        selectedPartySlotIndex = -1;
        HideDragPreview();
        Refresh();
        return true;
    }

    public void UpdateDragPreview(PointerEventData eventData)
    {
        if (dragPreviewRect == null || dragPreviewImage == null || !dragPreviewImage.gameObject.activeSelf)
            return;

        UpdateDragPreviewPosition(eventData);
    }

    private bool TryResolveBattleIdleFrameZero(string characterId, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        CharacterIconDatabase database = DataManager.Instance?.CharacterIconDatabase;
        if (database == null || !database.TryGetLobbyBattleIdleFrames(characterId.Trim(), out Sprite[] frames))
            return false;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null)
                continue;

            sprite = frames[i];
            return true;
        }

        return false;
    }

    private void ShowDragPreview(Sprite sprite, Vector2 size, PointerEventData eventData)
    {
        if (sprite == null)
        {
            HideDragPreview();
            return;
        }

        EnsureDragPreview();
        if (dragPreviewImage == null || dragPreviewRect == null)
            return;

        dragPreviewImage.sprite = sprite;
        dragPreviewImage.preserveAspect = true;
        dragPreviewImage.raycastTarget = false;
        Color previewColor = dragPreviewImage.color;
        previewColor.a = Mathf.Clamp01(dragPreviewAlpha);
        dragPreviewImage.color = previewColor;

        if (dragPreviewCanvas != null)
        {
            dragPreviewCanvas.overrideSorting = true;
            dragPreviewCanvas.sortingOrder = dragPreviewSortingOrder;
        }

        float scale = Mathf.Max(0.1f, dragPreviewScale);
        dragPreviewRect.sizeDelta = new Vector2(
            Mathf.Max(1f, size.x) * scale,
            Mathf.Max(1f, size.y) * scale);
        dragPreviewImage.gameObject.SetActive(true);
        dragPreviewRect.SetAsLastSibling();
        UpdateDragPreviewPosition(eventData);
    }

    private void EnsureDragPreview()
    {
        if (dragPreviewRect != null && dragPreviewImage != null && dragPreviewCanvas != null)
            return;

        Canvas sourceCanvas = GetComponentInParent<Canvas>();
        if (sourceCanvas == null)
            return;

        Canvas rootCanvas = sourceCanvas.rootCanvas != null ? sourceCanvas.rootCanvas : sourceCanvas;

        GameObject canvasObject = new GameObject(
            "ReadyCharacterDragPreviewCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup));
        // 런타임 전용 오브젝트를 Hierarchy/Inspector 선택 대상에서 숨겨
        // 플레이 중 생성/종료 시 Editor Inspector가 파괴된 대상을 붙잡는 오류를 방지합니다.
        canvasObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        RectTransform previewCanvasRect = canvasObject.GetComponent<RectTransform>();
        previewCanvasRect.SetParent(rootCanvas.transform, false);
        previewCanvasRect.anchorMin = Vector2.zero;
        previewCanvasRect.anchorMax = Vector2.one;
        previewCanvasRect.offsetMin = Vector2.zero;
        previewCanvasRect.offsetMax = Vector2.zero;
        previewCanvasRect.localScale = Vector3.one;
        previewCanvasRect.SetAsLastSibling();

        dragPreviewCanvas = canvasObject.GetComponent<Canvas>();
        dragPreviewCanvas.overrideSorting = true;
        dragPreviewCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        dragPreviewCanvas.sortingOrder = dragPreviewSortingOrder;
        dragPreviewCanvas.additionalShaderChannels = rootCanvas.additionalShaderChannels;

        CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.ignoreParentGroups = true;

        GameObject preview = new GameObject(
            "ReadyCharacterDragPreview",
            typeof(RectTransform),
            typeof(Image));
        preview.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        dragPreviewRect = preview.GetComponent<RectTransform>();
        dragPreviewRect.SetParent(previewCanvasRect, false);
        dragPreviewRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragPreviewRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragPreviewRect.pivot = new Vector2(0.5f, 0.5f);
        dragPreviewRect.localScale = Vector3.one;
        dragPreviewRect.SetAsLastSibling();

        dragPreviewImage = preview.GetComponent<Image>();
        dragPreviewImage.raycastTarget = false;
        dragPreviewImage.preserveAspect = true;
        dragPreviewImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(dragPreviewAlpha));
        preview.SetActive(false);
    }

    private void UpdateDragPreviewPosition(PointerEventData eventData)
    {
        if (eventData == null || dragPreviewRect == null || dragPreviewCanvas == null)
            return;

        RectTransform canvasRect = dragPreviewCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera eventCamera = dragPreviewCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (dragPreviewCanvas.worldCamera != null ? dragPreviewCanvas.worldCamera : eventData.pressEventCamera);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            dragPreviewRect.anchoredPosition = localPoint + dragPreviewPositionOffset;
        }
    }

    private void HideDragPreview()
    {
        if (dragPreviewImage != null)
            dragPreviewImage.gameObject.SetActive(false);
    }

    private bool IsValidGridIndex(int gridIndex)
    {
        return cells != null && gridIndex >= 0 && gridIndex < cells.Length && cells[gridIndex] != null;
    }

    public void OnClickCell(int gridIndex)
    {
        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        int clickedPartySlotIndex = FindPartySlotByGridIndex(gridIndex);

        // Info_Panel에서 아직 파티에 등록되지 않은 캐릭터를 고른 상태입니다.
        // 빈 Grid를 클릭하는 순간 캐릭터 등록과 시작 위치 저장을 한 번에 완료합니다.
        if (pendingInfoPlacementButton != null)
        {
            if (clickedPartySlotIndex >= 0)
                return;

            CharBtn sourceButton = pendingInfoPlacementButton;
            if (!sourceButton.TryRegisterToReadyGrid(gridIndex, out _))
                return;

            pendingInfoPlacementButton = null;
            selectedPartySlotIndex = -1;
            RefreshAllInfoAndReadyViews();
            return;
        }

        // 이미 등록된 캐릭터를 위치 변경 대상으로 선택한 경우에는 위치만 변경합니다.
        if (selectedPartySlotIndex >= 0)
        {
            if (clickedPartySlotIndex >= 0 && clickedPartySlotIndex != selectedPartySlotIndex)
                return;

            bool success = partyStore.SetSpawnGridIndex(selectedPartySlotIndex, gridIndex);
            if (!success)
                return;

            selectedPartySlotIndex = -1;
            RefreshAllInfoAndReadyViews();
            return;
        }

        // 배치된 Grid를 짧게 클릭하면 시작 위치뿐 아니라 파티 등록도 함께 해제합니다.
        // 따라서 Info_Panel의 Battle Idle / Relic / Compound도 즉시 미선택 상태로 돌아갑니다.
        if (clickedPartySlotIndex >= 0)
        {
            string characterId = partyStore.GetCharacterId(clickedPartySlotIndex);
            if (!string.IsNullOrWhiteSpace(characterId))
                LobbyCharacterEquipmentReleaseUtility.ReleaseAll(characterId);

            partyStore.ClearSlot(clickedPartySlotIndex);
            selectedPartySlotIndex = -1;
            RefreshAllInfoAndReadyViews();
            return;
        }

        ShowNoSelectedCharacterWarning();
    }

    public void SelectInfoCharacterForPlacement(CharBtn sourceButton)
    {
        if (sourceButton == null || sourceButton.IsLocked || !sourceButton.IsInfoPanelReadyDragSource)
            return;

        pendingInfoPlacementButton = sourceButton;
        selectedPartySlotIndex = -1;
        Refresh();
    }

    public void ClearInfoCharacterPlacementSelection(CharBtn sourceButton)
    {
        if (sourceButton != null && pendingInfoPlacementButton != sourceButton)
            return;

        pendingInfoPlacementButton = null;
        Refresh();
    }

    private void RefreshAllInfoAndReadyViews()
    {
        Refresh();

        CharPick[] pickers = FindObjectsByType<CharPick>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < pickers.Length; i++)
        {
            if (pickers[i] != null)
                pickers[i].RefreshFromPartyRuntime();
        }

        // Ready_Panel에서 캐릭터 등록이 해제된 직후에는 Info_Panel의 각 CharBtn도
        // 현재 PartyRuntimeStore를 직접 다시 확인해야 합니다.
        // CharPick 갱신 경로에 의존하면 버튼의 선택색이 이전 상태로 남을 수 있으므로
        // 모든 CharBtn의 파티 선택 상태를 명시적으로 다시 동기화합니다.
        CharBtn[] charButtons = FindObjectsByType<CharBtn>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < charButtons.Length; i++)
        {
            if (charButtons[i] != null)
                charButtons[i].RefreshSelectedPartyMarker();
        }

        LobbyInfoPanelUI.RefreshAll();
        LobbyEquipPanelUI.RefreshAllCharacterData();
        LobbyPartyCharacterSettingOpenButton.RefreshAll();
    }

    public void SelectPartySlotForPlacement(int partySlotIndex)
    {
        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        if (partySlotIndex < 0 || partySlotIndex >= partyStore.MaxPartyCountValue)
            return;

        if (string.IsNullOrWhiteSpace(partyStore.GetCharacterId(partySlotIndex)))
            return;

        selectedPartySlotIndex = partySlotIndex;
        Refresh();
    }

    public void ClearSelectionForPartySlot(int partySlotIndex)
    {
        if (selectedPartySlotIndex != partySlotIndex)
            return;

        selectedPartySlotIndex = -1;
        Refresh();
    }


    private bool IsReadyPartyFull()
    {
        PartyRuntimeStore partyStore = DataManager.Instance?.PartyRuntimeStore;
        if (partyStore == null)
            return false;

        int deployedCount = 0;
        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (string.IsNullOrWhiteSpace(partyStore.GetCharacterId(i)))
                continue;

            if (partyStore.GetSpawnGridIndex(i) < 0)
                continue;

            deployedCount++;
        }

        return deployedCount >= partyStore.MaxPartyCountValue;
    }

    private bool IsPointerOverAnyReadyGrid(PointerEventData eventData)
    {
        if (eventData == null || EventSystem.current == null)
            return false;

        PointerEventData raycastData = new PointerEventData(EventSystem.current)
        {
            position = eventData.position
        };

        pointerRaycastResults.Clear();
        EventSystem.current.RaycastAll(raycastData, pointerRaycastResults);

        for (int i = 0; i < pointerRaycastResults.Count; i++)
        {
            GameObject hitObject = pointerRaycastResults[i].gameObject;
            if (hitObject == null)
                continue;

            SpawnGridCell hitCell = hitObject.GetComponent<SpawnGridCell>()
                ?? hitObject.GetComponentInParent<SpawnGridCell>();

            if (hitCell != null && hitCell.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    private void UnregisterPartySlot(int partySlotIndex)
    {
        PartyRuntimeStore partyStore = DataManager.Instance?.PartyRuntimeStore;
        if (partyStore == null || partySlotIndex < 0 || partySlotIndex >= partyStore.MaxPartyCountValue)
            return;

        string characterId = partyStore.GetCharacterId(partySlotIndex);
        if (!string.IsNullOrWhiteSpace(characterId))
            LobbyCharacterEquipmentReleaseUtility.ReleaseAll(characterId);

        partyStore.ClearSlot(partySlotIndex);
        pendingInfoPlacementButton = null;
        infoDragSourceButton = null;
    }

    private void ShowNoSelectedCharacterWarning()
    {
        PartyRuntimeStore partyStore = DataManager.Instance != null
            ? DataManager.Instance.PartyRuntimeStore
            : null;

        bool hasPartyCharacter = partyStore != null && partyStore.HasAnyCharacter;
        string message = GameLocalization.Get(hasPartyCharacter
            ? LocalizationKeys.Warning.PartySelectDeployedCharacter
            : LocalizationKeys.Warning.PartySelectCharacter);

        if (SettingWarningUI.ShowMessage(message))
            return;

        Debug.LogWarning($"[SpawnGridPanel] {message}");
    }

    public bool TryGetPartySlotOrderIconObject(int partySlotIndex, out GameObject iconObject)
    {
        iconObject = null;

        if (partySlotOrderIconObjects == null)
            return false;

        if (partySlotIndex < 0 || partySlotIndex >= partySlotOrderIconObjects.Length)
            return false;

        iconObject = partySlotOrderIconObjects[partySlotIndex];
        return iconObject != null;
    }

    private int FindPartySlotByGridIndex(int gridIndex)
    {
        if (DataManager.Instance == null)
            return -1;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetSpawnGridIndex(i) == gridIndex)
                return i;
        }

        return -1;
    }

    public bool IsSelectedGrid(int gridIndex)
    {
        if (selectedPartySlotIndex < 0)
            return false;

        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return false;

        return DataManager.Instance.PartyRuntimeStore.GetSpawnGridIndex(selectedPartySlotIndex) == gridIndex;
    }

    /// <summary>
    /// Info_Panel에서 배치할 캐릭터가 선택되어 있을 때
    /// 아직 다른 캐릭터가 배치되지 않은 Grid만 배치 가능 상태로 표시합니다.
    /// </summary>
    public bool IsPlacementCandidateGrid(int gridIndex)
    {
        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return false;

        // Info_Panel에서 클릭으로 배치 대상을 고른 상태이거나 홀드 드래그 중이면
        // 캐릭터가 없는 Grid를 모두 배치 후보로 표시합니다.
        if (pendingInfoPlacementButton != null && pendingInfoPlacementButton.IsInfoPanelReadyDragSource)
            return FindPartySlotByGridIndex(gridIndex) < 0;

        if (infoDragSourceButton != null && infoDragSourceButton.IsInfoPanelReadyDragSource)
            return FindPartySlotByGridIndex(gridIndex) < 0;

        if (selectedPartySlotIndex < 0)
            return false;

        string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(selectedPartySlotIndex);
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        return FindPartySlotByGridIndex(gridIndex) < 0;
    }

    public void Refresh()
    {
        if (cells == null)
            return;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                cells[i].Refresh();
        }
    }
}
