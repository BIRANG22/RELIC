using Relic.Gameplay.Data;
using System.Collections;
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

    [Header("Open / Close Animation")]
    [SerializeField] private bool useSlideAnimation = true;
    [SerializeField] private Vector2 closedAnchoredPosition = new Vector2(-600f, 700f);
    [SerializeField] private Vector2 openedAnchoredPosition = new Vector2(-600f, 235f);
    [SerializeField] private float openSlideDuration = 0.2f;
    [SerializeField] private float closeSlideDuration = 0.15f;
    [SerializeField] private bool useUnscaledTimeForSlide = true;

    [Header("Character Change Fade")]
    [SerializeField] private bool useCharacterChangeFade = true;
    [SerializeField] private CanvasGroup contentCanvasGroup;
    [SerializeField] private float characterChangeFadeOutDuration = 0.08f;
    [SerializeField] private float characterChangeFadeInDuration = 0.08f;

    [Header("Close")]
    [SerializeField] private bool closeWhenClickOutside = true;
    [SerializeField] private RectTransform[] keepOpenClickRoots;
    [SerializeField] private bool keepOpenWhenClickTimeline = true;

    [Header("Sound")]
    [SerializeField] private bool playOpenSound = true;
    [SerializeField] private SfxType openSfx = SfxType.SkillListPanelOpen;
    [SerializeField, Range(0f, 1f)] private float openSfxVolume = 1f;
    [SerializeField] private bool playCloseSound = true;
    [SerializeField] private SfxType closeSfx = SfxType.SkillListPanelClose;
    [SerializeField, Range(0f, 1f)] private float closeSfxVolume = 1f;

    private readonly List<RectTransform> runtimeKeepOpenClickRoots = new();
    private readonly List<RectTransform> timelineKeepOpenClickRoots = new();
    private readonly List<SkillListSlotUI> skillSlots = new();
    private SkillListSlotUI selectedSkillSlot;

    private CharacterRuntimeData currentRuntime;
    private CharacterRuntimeData battleExecutionClosedRuntime;
    private bool hasCapturedInitialPosition;
    private bool hasCapturedInitialDetailPosition;
    private Vector2 initialDetailAnchoredPosition;
    private int ignoreOutsideCloseFrame = -1;
    private int renderedActiveSlotIndex = int.MinValue;
    private int renderedReservationVersion = int.MinValue;
    private Coroutine slideRoutine;
    private Coroutine characterChangeFadeRoutine;
    private bool isChangingCharacterContent;

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

        EnsureBattleTimelineController();
        EnsureContentCanvasGroup();

        HideSkillDetail();
        CloseImmediate(true);
    }

    private void OnEnable()
    {
        CaptureInitialPosition();
        CaptureInitialDetailPosition();
    }

    private void Update()
    {
        if (closeWhenClickOutside &&
            IsOpen() &&
            Time.frameCount > ignoreOutsideCloseFrame &&
            WasPointerPressedThisFrame(out Vector2 screenPosition))
        {
            if (!IsScreenPositionInsidePanelOrKeepOpenRoots(screenPosition))
            {
                Close();
                return;
            }
        }

        RefreshIfTimelinePreviewStateChanged();
    }

    private void EnsureBattleTimelineController()
    {
        if (battleTimelineController != null)
            return;

        battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
    }

    private void RefreshIfTimelinePreviewStateChanged()
    {
        if (!IsOpen() || currentRuntime == null || isChangingCharacterContent)
            return;

        EnsureBattleTimelineController();

        int activeSlotIndex = battleTimelineController != null
            ? battleTimelineController.ActiveSlotIndex
            : -1;
        int reservationVersion = battleTimelineController != null
            ? battleTimelineController.ReservationVersion
            : -1;

        if (activeSlotIndex == renderedActiveSlotIndex &&
            reservationVersion == renderedReservationVersion)
        {
            return;
        }

        Refresh();
    }

    public void Open(CharacterRuntimeData runtimeData)
    {
        Open(runtimeData, null);
    }

    public void Open(CharacterRuntimeData runtimeData, RectTransform hudRect)
    {
        bool wasOpenBeforeRequest = IsOpen();

        // 패널 오브젝트 자체가 비활성화된 상태에서는 StartCoroutine을 사용할 수 없으므로
        // 열기 처리의 가장 처음에 이 컴포넌트가 붙은 오브젝트를 먼저 활성화한다.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        bool isChangingCharacter = IsOpen() && currentRuntime != null && currentRuntime != runtimeData;

        StopSlideRoutine();

        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        EnsureBattleTimelineController();
        EnsureContentCanvasGroup();
        RefreshTimelineKeepOpenClickRoots();

        if (isChangingCharacter && useCharacterChangeFade)
        {
            currentRuntime = runtimeData;
            ignoreOutsideCloseFrame = Time.frameCount;
            SetPanelAnchoredPosition(openedAnchoredPosition);

            if (battleTimelineController != null)
                battleTimelineController.SelectCharacter(currentRuntime);

            PlayCharacterChangeFade();
            return;
        }

        StopCharacterChangeFadeRoutine(true);
        SetContentAlpha(1f);

        if (isChangingCharacter)
        {
            Clear();
            HideSkillDetail();
            ClearSkillHoverRangePreview();
        }

        currentRuntime = runtimeData;
        ignoreOutsideCloseFrame = Time.frameCount;

        if (battleTimelineController != null)
            battleTimelineController.SelectCharacter(currentRuntime);

        if (useSlideAnimation)
        {
            SetPanelAnchoredPosition(closedAnchoredPosition);
        }
        else
        {
            ApplyPanelPosition(hudRect);
        }

        Refresh();
        PlayOpenAnimation();

        if (!wasOpenBeforeRequest)
            PlayOpenSound();
    }

    public void Close()
    {
        bool wasOpenBeforeRequest = IsOpen();

        EnsureBattleTimelineController();
        StopCharacterChangeFadeRoutine(true);

        if (battleTimelineController != null)
        {
            battleTimelineController.CancelSkillReservationPreviewFromSkillList(currentRuntime);
            battleTimelineController.ClearCharacterSelectionFromSkillList(currentRuntime);
        }

        currentRuntime = null;
        renderedActiveSlotIndex = int.MinValue;
        renderedReservationVersion = int.MinValue;
        HideSkillDetail();
        ClearSkillHoverRangePreview();
        PlayCloseAnimation(true);

        if (wasOpenBeforeRequest)
            PlayCloseSound();
    }


    public void CloseForBattleExecution()
    {
        bool wasOpenBeforeRequest = IsOpen();

        EnsureBattleTimelineController();
        StopCharacterChangeFadeRoutine(true);

        battleExecutionClosedRuntime = currentRuntime;

        if (battleTimelineController != null)
            battleTimelineController.CancelSkillReservationPreviewFromSkillList(currentRuntime);

        currentRuntime = null;
        renderedActiveSlotIndex = int.MinValue;
        renderedReservationVersion = int.MinValue;
        HideSkillDetail();
        ClearSkillHoverRangePreview();
        PlayCloseAnimation(true);

        if (wasOpenBeforeRequest)
            PlayCloseSound();
    }

    public void ReopenAfterBattleExecution()
    {
        EnsureBattleTimelineController();

        CharacterRuntimeData runtimeData = battleTimelineController != null
            ? battleTimelineController.SelectedCharacter
            : null;

        if (runtimeData == null)
            runtimeData = battleExecutionClosedRuntime;

        if (runtimeData == null)
            return;

        battleExecutionClosedRuntime = null;
        Open(runtimeData);
    }

    private void PlayCharacterChangeFade()
    {
        StopCharacterChangeFadeRoutine(false);

        if (!gameObject.activeInHierarchy)
        {
            Refresh();
            SetContentAlpha(1f);
            return;
        }

        characterChangeFadeRoutine = StartCoroutine(CharacterChangeFadeRoutine());
    }

    private IEnumerator CharacterChangeFadeRoutine()
    {
        isChangingCharacterContent = true;
        HideSkillDetail();
        ClearSkillHoverRangePreview();

        yield return FadeContentAlpha(GetContentAlpha(), 0f, characterChangeFadeOutDuration);

        Refresh();
        SetContentAlpha(0f);

        yield return FadeContentAlpha(0f, 1f, characterChangeFadeInDuration);

        SetContentAlpha(1f);
        isChangingCharacterContent = false;
        characterChangeFadeRoutine = null;
    }

    private IEnumerator FadeContentAlpha(float startAlpha, float targetAlpha, float duration)
    {
        EnsureContentCanvasGroup();

        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            float deltaTime = useUnscaledTimeForSlide ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            contentCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        contentCanvasGroup.alpha = targetAlpha;
    }

    private void StopCharacterChangeFadeRoutine(bool resetAlpha)
    {
        if (characterChangeFadeRoutine != null)
        {
            StopCoroutine(characterChangeFadeRoutine);
            characterChangeFadeRoutine = null;
        }

        isChangingCharacterContent = false;

        if (resetAlpha)
            SetContentAlpha(1f);
    }

    private void EnsureContentCanvasGroup()
    {
        if (contentCanvasGroup != null)
            return;

        if (contentRoot == null)
            return;

        contentCanvasGroup = contentRoot.GetComponent<CanvasGroup>();

        if (contentCanvasGroup == null)
            contentCanvasGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
    }

    private void SetContentAlpha(float alpha)
    {
        EnsureContentCanvasGroup();

        if (contentCanvasGroup == null)
            return;

        contentCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private float GetContentAlpha()
    {
        EnsureContentCanvasGroup();

        if (contentCanvasGroup == null)
            return 1f;

        return contentCanvasGroup.alpha;
    }

    private void PlayOpenAnimation()
    {
        if (!useSlideAnimation)
        {
            SetPanelAnchoredPosition(openedAnchoredPosition);
            return;
        }

        StopSlideRoutine();
        slideRoutine = StartCoroutine(SlidePanel(openedAnchoredPosition, openSlideDuration, false, false));
    }

    private void PlayOpenSound()
    {
        if (!playOpenSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(openSfx, openSfxVolume);
    }

    private void PlayCloseSound()
    {
        if (!playCloseSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(closeSfx, closeSfxVolume);
    }

    private void PlayCloseAnimation(bool clearContentWhenComplete)
    {
        StopSlideRoutine();

        if (!useSlideAnimation || !gameObject.activeInHierarchy)
        {
            CloseImmediate(clearContentWhenComplete);
            return;
        }

        slideRoutine = StartCoroutine(SlidePanel(closedAnchoredPosition, closeSlideDuration, true, clearContentWhenComplete));
    }

    private IEnumerator SlidePanel(Vector2 targetPosition, float duration, bool deactivateWhenComplete, bool clearContentWhenComplete)
    {
        if (panelRect == null)
        {
            if (deactivateWhenComplete)
                CloseImmediate(clearContentWhenComplete);

            slideRoutine = null;
            yield break;
        }

        Vector2 startPosition = panelRect.anchoredPosition;
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            float deltaTime = useUnscaledTimeForSlide ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            panelRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);
            yield return null;
        }

        panelRect.anchoredPosition = targetPosition;
        slideRoutine = null;

        if (deactivateWhenComplete)
            CloseImmediate(clearContentWhenComplete);
    }

    private void StopSlideRoutine()
    {
        if (slideRoutine == null)
            return;

        StopCoroutine(slideRoutine);
        slideRoutine = null;
    }

    private void CloseImmediate(bool clearContent)
    {
        StopSlideRoutine();
        StopCharacterChangeFadeRoutine(true);
        SetPanelAnchoredPosition(closedAnchoredPosition);

        if (clearContent)
            Clear();

        HideSkillDetail();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void SetPanelAnchoredPosition(Vector2 anchoredPosition)
    {
        if (panelRect == null)
            return;

        panelRect.anchoredPosition = anchoredPosition;
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
        UpdateRenderedTimelinePreviewState();
        Clear();

        if (currentRuntime == null)
            return;

        AddSkillSlot(currentRuntime.MoveSkillId, true);
        AddSkillSlot(currentRuntime.AbilitySkillId, true);
        AddSkillSlot(GetEquippedSkillId(2), true);
        AddSkillSlot(GetEquippedSkillId(3), true);
        AddSkillSlot(currentRuntime.UniqueSkillId, true);
    }

    private string GetEquippedSkillId(int index)
    {
        if (currentRuntime == null)
            return string.Empty;

        if (currentRuntime.EquippedSkillIds == null)
            return string.Empty;

        if (index < 0 || index >= currentRuntime.EquippedSkillIds.Length)
            return string.Empty;

        return currentRuntime.EquippedSkillIds[index];
    }

    private void AddSkillSlot(string skillId, bool interactable)
    {
        if (skillSlotPrefab == null || contentRoot == null)
            return;

        SkillListSlotUI slot = Instantiate(skillSlotPrefab, contentRoot);
        skillSlots.Add(slot);
        slot.Setup(this, skillId, interactable, GetPreviewSkillCostValue(skillId), currentRuntime);
    }

    private void UpdateRenderedTimelinePreviewState()
    {
        EnsureBattleTimelineController();

        renderedActiveSlotIndex = battleTimelineController != null
            ? battleTimelineController.ActiveSlotIndex
            : -1;
        renderedReservationVersion = battleTimelineController != null
            ? battleTimelineController.ReservationVersion
            : -1;
    }

    private int GetPreviewSkillCostValue(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return -1;

        if (DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            !DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData))
        {
            return -1;
        }

        EnsureBattleTimelineController();

        if (battleTimelineController == null)
            return skillData.ResourceCostValue;

        return battleTimelineController.GetPreviewReservationCostValue(currentRuntime, skillData);
    }

    public void SelectSkillSlot(SkillListSlotUI selectedSlot)
    {
        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(this);

        selectedSkillSlot = selectedSlot;
        ApplySkillSlotSelectionVisuals();
    }

    public void ResetSelectionState()
    {
        selectedSkillSlot = null;
        ApplySkillSlotSelectionVisuals();
    }

    private void ApplySkillSlotSelectionVisuals()
    {
        for (int i = skillSlots.Count - 1; i >= 0; i--)
        {
            SkillListSlotUI slot = skillSlots[i];

            if (slot == null)
            {
                skillSlots.RemoveAt(i);
                continue;
            }

            slot.SetSelected(slot == selectedSkillSlot);
        }

        SkillListSlotUI[] childSlots = GetComponentsInChildren<SkillListSlotUI>(true);
        for (int i = 0; i < childSlots.Length; i++)
        {
            SkillListSlotUI slot = childSlots[i];
            if (slot != null && !skillSlots.Contains(slot))
                slot.SetSelected(slot == selectedSkillSlot);
        }
    }

    public void SelectSkill(string skillId)
    {
        if (currentRuntime == null)
        {
            ShowBattleWarning("선택된 캐릭터가 없습니다.");
            Debug.LogWarning("[SkillListPanel] 선택된 캐릭터가 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillId))
        {
            ShowBattleWarning("등록된 스킬이 없습니다.");
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
        {
            ShowBattleWarning("스킬 데이터를 찾을 수 없습니다.");
            Debug.LogWarning("[SkillListPanel] SkillDatabase가 없습니다.");
            return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            ShowBattleWarning("스킬 데이터를 찾을 수 없습니다.");
            Debug.LogWarning($"[SkillListPanel] SkillData 없음: {skillId}");
            return;
        }

        EnsureBattleTimelineController();

        if (battleTimelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            Debug.LogWarning("[SkillListPanel] BattleTimelineController가 없습니다.");
            return;
        }

        battleTimelineController.SelectCharacter(currentRuntime);
        battleTimelineController.SelectSkill(skillData);
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
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

    public void ShowSkillHoverRangePreview(SkillMasterData skillData)
    {
        EnsureBattleTimelineController();

        if (battleTimelineController == null)
            return;

        battleTimelineController.ShowSkillHoverRangePreview(currentRuntime, skillData);
    }

    public void ClearSkillHoverRangePreview()
    {
        EnsureBattleTimelineController();

        if (battleTimelineController != null)
            battleTimelineController.ClearSkillHoverRangePreview();
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

        if (contentRoot is RectTransform contentRect && IsScreenPositionInsideRect(contentRect, screenPosition))
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

        if (keepOpenWhenClickTimeline)
        {
            RefreshTimelineKeepOpenClickRoots();

            for (int i = timelineKeepOpenClickRoots.Count - 1; i >= 0; i--)
            {
                RectTransform root = timelineKeepOpenClickRoots[i];

                if (root == null)
                {
                    timelineKeepOpenClickRoots.RemoveAt(i);
                    continue;
                }

                if (IsScreenPositionInsideRect(root, screenPosition))
                    return true;
            }
        }

        return false;
    }

    private void RefreshTimelineKeepOpenClickRoots()
    {
        if (!keepOpenWhenClickTimeline)
            return;

        timelineKeepOpenClickRoots.Clear();

        AddTimelineKeepOpenRoot(battleTimelineController);

        BattleTimelineBarUI[] timelineBars = FindObjectsByType<BattleTimelineBarUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < timelineBars.Length; i++)
            AddTimelineKeepOpenRoot(timelineBars[i]);

        BattleTimelineGroupUI[] timelineGroups = FindObjectsByType<BattleTimelineGroupUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < timelineGroups.Length; i++)
            AddTimelineKeepOpenRoot(timelineGroups[i]);

        ReserveTurnSlotUI[] reserveTurnSlots = FindObjectsByType<ReserveTurnSlotUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < reserveTurnSlots.Length; i++)
            AddTimelineKeepOpenRoot(reserveTurnSlots[i]);
    }

    private void AddTimelineKeepOpenRoot(Component component)
    {
        if (component == null)
            return;

        RectTransform rectTransform = component.GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

        if (!timelineKeepOpenClickRoots.Contains(rectTransform))
            timelineKeepOpenClickRoots.Add(rectTransform);
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

    public bool IsOpen()
    {
        if (panelRoot == null)
            return gameObject.activeInHierarchy;

        return panelRoot.activeInHierarchy;
    }

    private void Clear()
    {
        selectedSkillSlot = null;
        skillSlots.Clear();

        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }
}
