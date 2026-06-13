using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillListPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform panelRect;

    [Header("Content")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillListSlotUI skillSlotPrefab;

    [Header("Detail")]
    [SerializeField] private GameObject detailsBackground;
    [SerializeField] private RectTransform detailsBackgroundRect;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private bool alignDetailsToHoveredSkillLine = true;
    [SerializeField] private bool keepDetailInitialX = true;
    [SerializeField] private Vector2 detailOffsetFromHoveredSkillLine = Vector2.zero;

    [Header("Timeline")]
    [SerializeField] private BattleTimelineController battleTimelineController;

    [Header("Position")]
    [SerializeField] private bool useFixedAnchoredPosition = true;
    [SerializeField] private bool useInitialPositionAsFixedPosition = true;
    [SerializeField] private Vector2 fixedAnchoredPosition = Vector2.zero;
    [SerializeField] private Vector2 offsetFromHud = new Vector2(220f, 0f);

    [Header("Close")]
    [SerializeField] private bool closeWhenClickOutside = true;
    [SerializeField] private RectTransform[] keepOpenClickRoots;

    private readonly List<RectTransform> runtimeKeepOpenClickRoots = new();

    private CharacterRuntimeData currentRuntime;
    private bool hasCapturedInitialPosition;
    private bool hasCapturedInitialDetailPosition;
    private Vector2 initialDetailAnchoredPosition;
    private int ignoreOutsideCloseFrame = -1;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (detailsBackgroundRect == null && detailsBackground != null)
            detailsBackgroundRect = detailsBackground.GetComponent<RectTransform>();

        CaptureInitialPosition();
        CaptureInitialDetailPosition();

        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        HideSkillDetail();
        Close();
    }

    private void OnEnable()
    {
        CaptureInitialPosition();
        CaptureInitialDetailPosition();
    }

    private void Update()
    {
        if (!closeWhenClickOutside)
            return;

        if (!IsOpen())
            return;

        if (Time.frameCount <= ignoreOutsideCloseFrame)
            return;

        if (WasPointerPressedThisFrame(out Vector2 screenPosition))
        {
            if (!IsScreenPositionInsidePanelOrKeepOpenRoots(screenPosition))
                Close();
        }
    }

    public void Open(CharacterRuntimeData runtimeData)
    {
        Open(runtimeData, null);
    }

    public void Open(CharacterRuntimeData runtimeData, RectTransform hudRect)
    {
        currentRuntime = runtimeData;
        ignoreOutsideCloseFrame = Time.frameCount;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        if (battleTimelineController != null)
            battleTimelineController.SelectCharacter(currentRuntime);

        ApplyPanelPosition(hudRect);
        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        currentRuntime = null;
        Clear();
        HideSkillDetail();
    }

    public void RegisterKeepOpenClickRoot(RectTransform root)
    {
        if (root == null)
            return;

        if (!runtimeKeepOpenClickRoots.Contains(root))
            runtimeKeepOpenClickRoots.Add(root);
    }

    public void UnregisterKeepOpenClickRoot(RectTransform root)
    {
        if (root == null)
            return;

        runtimeKeepOpenClickRoots.Remove(root);
    }

    public void ClearRuntimeKeepOpenClickRoots()
    {
        runtimeKeepOpenClickRoots.Clear();
    }

    public void IgnoreOutsideCloseForCurrentFrame()
    {
        ignoreOutsideCloseFrame = Mathf.Max(ignoreOutsideCloseFrame, Time.frameCount);
    }

    public void IgnoreOutsideCloseForFrames(int frameCount)
    {
        int safeFrameCount = Mathf.Max(0, frameCount);
        ignoreOutsideCloseFrame = Mathf.Max(ignoreOutsideCloseFrame, Time.frameCount + safeFrameCount);
    }

    public void Refresh()
    {
        Clear();

        if (currentRuntime == null)
            return;

        AddSkillSlot(currentRuntime.MoveSkillId, true);
        AddSkillSlot(currentRuntime.UniqueSkillId, true);
        AddSkillSlot(currentRuntime.AbilitySkillId, true);

        if (currentRuntime.EquippedSkillIds != null)
        {
            if (currentRuntime.EquippedSkillIds.Length > 2)
                AddSkillSlot(currentRuntime.EquippedSkillIds[2], true);

            if (currentRuntime.EquippedSkillIds.Length > 3)
                AddSkillSlot(currentRuntime.EquippedSkillIds[3], true);
        }
    }

    private void AddSkillSlot(string skillId, bool interactable)
    {
        if (skillSlotPrefab == null || contentRoot == null)
            return;

        SkillListSlotUI slot = Instantiate(skillSlotPrefab, contentRoot);
        slot.Setup(this, skillId, interactable);
    }

    public void SelectSkill(string skillId)
    {
        if (currentRuntime == null)
        {
            Debug.LogWarning("[SkillListPanel] 선택된 캐릭터가 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
        {
            Debug.LogWarning("[SkillListPanel] SkillDatabase가 없습니다.");
            return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[SkillListPanel] SkillData 없음: {skillId}");
            return;
        }

        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        if (battleTimelineController == null)
        {
            Debug.LogWarning("[SkillListPanel] BattleTimelineController가 없습니다.");
            return;
        }

        battleTimelineController.SelectCharacter(currentRuntime);
        battleTimelineController.SelectSkill(skillData);
    }

    private void CaptureInitialPosition()
    {
        if (hasCapturedInitialPosition)
            return;

        if (panelRect == null)
            return;

        if (useInitialPositionAsFixedPosition)
            fixedAnchoredPosition = panelRect.anchoredPosition;

        hasCapturedInitialPosition = true;
    }

    private void CaptureInitialDetailPosition()
    {
        if (hasCapturedInitialDetailPosition)
            return;

        if (detailsBackgroundRect == null)
            return;

        initialDetailAnchoredPosition = detailsBackgroundRect.anchoredPosition;
        hasCapturedInitialDetailPosition = true;
    }

    private void ApplyPanelPosition(RectTransform hudRect)
    {
        if (panelRect == null)
            return;

        if (useFixedAnchoredPosition)
        {
            panelRect.anchoredPosition = fixedAnchoredPosition;
            return;
        }

        PositionToHud(hudRect);
    }

    private void PositionToHud(RectTransform hudRect)
    {
        if (panelRect == null || hudRect == null)
            return;

        RectTransform parentRect = panelRect.parent as RectTransform;

        if (parentRect == null)
            return;

        Canvas canvas = panelRect.GetComponentInParent<Canvas>();
        Camera uiCamera = GetCanvasCamera(canvas);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, hudRect.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        panelRect.anchoredPosition = localPoint + offsetFromHud;
    }

    public void ShowSkillDetail(string text)
    {
        ShowSkillDetail(text, null);
    }

    public void ShowSkillDetail(string text, RectTransform hoveredSkillRect)
    {
        if (detailsBackground != null)
            detailsBackground.SetActive(true);

        if (detailsText != null)
            detailsText.text = text;

        if (alignDetailsToHoveredSkillLine)
            AlignDetailToHoveredSkillLine(hoveredSkillRect);
    }

    public void HideSkillDetail()
    {
        if (detailsBackground != null)
            detailsBackground.SetActive(false);

        if (detailsText != null)
            detailsText.text = "";
    }

    private void AlignDetailToHoveredSkillLine(RectTransform hoveredSkillRect)
    {
        if (detailsBackgroundRect == null || hoveredSkillRect == null)
            return;

        RectTransform detailParentRect = detailsBackgroundRect.parent as RectTransform;

        if (detailParentRect == null)
            return;

        Canvas canvas = detailsBackgroundRect.GetComponentInParent<Canvas>();
        Camera uiCamera = GetCanvasCamera(canvas);
        Vector3[] corners = new Vector3[4];
        hoveredSkillRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                detailParentRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 targetPosition = detailsBackgroundRect.anchoredPosition;
        targetPosition.x = keepDetailInitialX ? initialDetailAnchoredPosition.x : localPoint.x;
        targetPosition.y = localPoint.y;
        targetPosition += detailOffsetFromHoveredSkillLine;
        detailsBackgroundRect.anchoredPosition = targetPosition;
    }

    private bool WasPointerPressedThisFrame(out Vector2 screenPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        screenPosition = Vector2.zero;
        return false;
    }

    private bool IsScreenPositionInsidePanelOrKeepOpenRoots(Vector2 screenPosition)
    {
        if (IsScreenPositionInsideRect(panelRect, screenPosition))
            return true;

        if (IsScreenPositionInsideRect(detailsBackgroundRect, screenPosition))
            return true;

        if (keepOpenClickRoots != null)
        {
            for (int i = 0; i < keepOpenClickRoots.Length; i++)
            {
                if (IsScreenPositionInsideRect(keepOpenClickRoots[i], screenPosition))
                    return true;
            }
        }

        for (int i = runtimeKeepOpenClickRoots.Count - 1; i >= 0; i--)
        {
            RectTransform root = runtimeKeepOpenClickRoots[i];

            if (root == null)
            {
                runtimeKeepOpenClickRoots.RemoveAt(i);
                continue;
            }

            if (IsScreenPositionInsideRect(root, screenPosition))
                return true;
        }

        return false;
    }

    private bool IsScreenPositionInsideRect(RectTransform targetRect, Vector2 screenPosition)
    {
        if (targetRect == null)
            return false;

        if (!targetRect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = targetRect.GetComponentInParent<Canvas>();
        Camera uiCamera = GetCanvasCamera(canvas);
        return RectTransformUtility.RectangleContainsScreenPoint(targetRect, screenPosition, uiCamera);
    }

    private Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private bool IsOpen()
    {
        if (panelRoot == null)
            return gameObject.activeInHierarchy;

        return panelRoot.activeInHierarchy;
    }

    private void Clear()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }
}
