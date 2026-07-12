using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapCategoryHighlightController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private MapViewSpawner mapViewSpawner;
    [SerializeField] private RectTransform mapArea;

    [Header("Category Buttons")]
    [SerializeField] private Button bossButton;
    [SerializeField] private Button eliteButton;
    [SerializeField] private Button battleButton;
    [SerializeField] private Button restButton;
    [SerializeField] private Button eventButton;
    [SerializeField] private Button startButton;

    [Header("Selected Category Color")]
    [SerializeField] private Color selectedButtonColor = new Color32(0x4D, 0x68, 0xDF, 0xFF);

    private readonly List<RaycastResult> raycastResults = new();
    private readonly Dictionary<Graphic, Color> originalGraphicColors = new();

    private Canvas rootCanvas;
    private Button selectedButton;

    private void Awake()
    {
        FindReferencesIfNeeded();
        CaptureButtonColors();
        RegisterButtonEvents();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();

        // 카테고리 버튼을 누른 경우 버튼 이벤트에서 강조를 변경합니다.
        if (IsPointerOverCategoryButton(pointerPosition))
            return;

        // 맵 영역 안에서 노드를 누르거나 맵을 드래그할 때는 강조를 유지합니다.
        if (IsPointerInsideMapArea(pointerPosition))
            return;

        // 맵 영역 밖을 눌렀을 때만 선택한 카테고리 강조를 해제합니다.
        ClearHighlight();
    }

    private void FindReferencesIfNeeded()
    {
        if (mapViewSpawner == null)
            mapViewSpawner = FindFirstObjectByType<MapViewSpawner>();

        if (mapArea == null)
            mapArea = FindRectTransformByName("MapArea");

        // 이전 오브젝트 이름을 사용 중인 씬도 대응합니다.
        if (mapArea == null)
            mapArea = FindRectTransformByName("Map_area");

        if (mapArea != null)
            rootCanvas = mapArea.GetComponentInParent<Canvas>();

        if (bossButton == null)
            bossButton = FindChildButton("BossImg");

        if (eliteButton == null)
            eliteButton = FindChildButton("EliteImg");

        if (battleButton == null)
            battleButton = FindChildButton("BattleImg");

        if (restButton == null)
            restButton = FindChildButton("RestImg");

        if (eventButton == null)
            eventButton = FindChildButton("EventImg");

        if (startButton == null)
            startButton = FindChildButton("StartImg");
    }

    private RectTransform FindRectTransformByName(string objectName)
    {
        RectTransform[] rectTransforms = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < rectTransforms.Length; i++)
        {
            if (rectTransforms[i].name == objectName)
                return rectTransforms[i];
        }

        return null;
    }

    private Button FindChildButton(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != objectName)
                continue;

            return children[i].GetComponent<Button>();
        }

        return null;
    }

    private void CaptureButtonColors()
    {
        CaptureButtonColor(bossButton);
        CaptureButtonColor(eliteButton);
        CaptureButtonColor(battleButton);
        CaptureButtonColor(restButton);
        CaptureButtonColor(eventButton);
        CaptureButtonColor(startButton);
    }

    private void CaptureButtonColor(Button targetButton)
    {
        if (targetButton == null)
            return;

        // 버튼 아이콘뿐 아니라 자식 텍스트를 포함한 모든 UI 그래픽의 원래 색을 저장합니다.
        Graphic[] graphics = targetButton.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];

            if (graphic != null && !originalGraphicColors.ContainsKey(graphic))
                originalGraphicColors.Add(graphic, graphic.color);
        }
    }

    private void RegisterButtonEvents()
    {
        AddListener(bossButton, ShowBossNodes);
        AddListener(eliteButton, ShowEliteNodes);
        AddListener(battleButton, ShowBattleNodes);
        AddListener(restButton, ShowRestNodes);
        AddListener(eventButton, ShowEventNodes);
        AddListener(startButton, ShowStartNodes);
    }

    private void UnregisterButtonEvents()
    {
        RemoveListener(bossButton, ShowBossNodes);
        RemoveListener(eliteButton, ShowEliteNodes);
        RemoveListener(battleButton, ShowBattleNodes);
        RemoveListener(restButton, ShowRestNodes);
        RemoveListener(eventButton, ShowEventNodes);
        RemoveListener(startButton, ShowStartNodes);
    }

    private void AddListener(Button targetButton, UnityEngine.Events.UnityAction action)
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(action);
    }

    private void RemoveListener(Button targetButton, UnityEngine.Events.UnityAction action)
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(action);
    }

    private void ShowBossNodes()
    {
        HighlightCategory("Boss", bossButton);
    }

    private void ShowEliteNodes()
    {
        HighlightCategory("Elite", eliteButton);
    }

    private void ShowBattleNodes()
    {
        HighlightCategory("Common", battleButton);
    }

    private void ShowRestNodes()
    {
        HighlightCategory("Rest", restButton);
    }

    private void ShowEventNodes()
    {
        // 맵 데이터에서 이벤트 노드 타입은 Event가 아니라 Special입니다.
        HighlightCategory("Special", eventButton);
    }

    private void ShowStartNodes()
    {
        HighlightCategory("Start", startButton);
    }

    private void HighlightCategory(string nodeType, Button categoryButton)
    {
        if (mapViewSpawner != null)
            mapViewSpawner.HighlightCategory(nodeType);

        SetSelectedButton(categoryButton);
    }

    private void SetSelectedButton(Button categoryButton)
    {
        RestoreAllButtonColors();
        selectedButton = categoryButton;

        if (selectedButton == null)
            return;

        Graphic[] graphics = selectedButton.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].color = selectedButtonColor;
        }
    }

    private void RestoreAllButtonColors()
    {
        foreach (KeyValuePair<Graphic, Color> pair in originalGraphicColors)
        {
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        }
    }

    private void ClearHighlight()
    {
        if (mapViewSpawner != null)
            mapViewSpawner.ClearCategoryHighlight();

        selectedButton = null;
        RestoreAllButtonColors();
    }

    /// <summary>
    /// 방 클리어 후 지도가 다시 생성될 때 남아 있는 카테고리 선택 색상을 초기화합니다.
    /// </summary>
    public void ResetHighlightForMapRefresh()
    {
        selectedButton = null;
        RestoreAllButtonColors();
    }

    private bool IsPointerInsideMapArea(Vector2 pointerPosition)
    {
        if (mapArea == null)
            return false;

        Camera eventCamera = null;

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = rootCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            mapArea,
            pointerPosition,
            eventCamera
        );
    }

    private bool IsPointerOverCategoryButton(Vector2 pointerPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            Transform hitTransform = raycastResults[i].gameObject.transform;

            if (IsButtonTransform(hitTransform, bossButton) ||
                IsButtonTransform(hitTransform, eliteButton) ||
                IsButtonTransform(hitTransform, battleButton) ||
                IsButtonTransform(hitTransform, restButton) ||
                IsButtonTransform(hitTransform, eventButton) ||
                IsButtonTransform(hitTransform, startButton))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsButtonTransform(Transform hitTransform, Button targetButton)
    {
        return targetButton != null &&
               hitTransform != null &&
               (hitTransform == targetButton.transform || hitTransform.IsChildOf(targetButton.transform));
    }
}
