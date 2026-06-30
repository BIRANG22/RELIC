using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyStageButtonCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Stage Buttons")]
    [SerializeField] private Button[] stageButtons = new Button[3];
    [SerializeField] private bool autoBindStageButtons = true;
    [SerializeField] private Transform stageButtonRoot;
    [SerializeField] private bool selectFirstAvailableOnEnable = true;
    [Tooltip("끝 스테이지에서 같은 방향으로 한 번 더 넘기면 반대쪽 스테이지로 이어지게 합니다.")]
    [SerializeField] private bool wrapSelection = true;

    [Header("Navigation Buttons")]
    [Tooltip("이전 스테이지를 중앙으로 불러오는 버튼입니다. 비워두면 사용하지 않습니다.")]
    [SerializeField] private Button previousStageNavigationButton;
    [Tooltip("다음 스테이지를 중앙으로 불러오는 버튼입니다. 비워두면 사용하지 않습니다.")]
    [SerializeField] private Button nextStageNavigationButton;
    [Tooltip("연결된 이전/다음 버튼에 클릭 이벤트를 자동으로 등록합니다.")]
    [SerializeField] private bool bindNavigationButtonClicks = true;

    [Header("Layout")]
    [Tooltip("선택된 스테이지 버튼이 위치할 중앙 좌표입니다.")]
    [SerializeField] private Vector2 centerPosition = Vector2.zero;
    [Tooltip("선택된 버튼의 이전 스테이지가 작아져서 왼쪽 뒤에 보이는 위치입니다.")]
    [SerializeField] private Vector2 previousPosition = new Vector2(-210f, 0f);
    [Tooltip("선택된 버튼의 다음 스테이지가 작아져서 오른쪽 뒤에 보이는 위치입니다.")]
    [SerializeField] private Vector2 nextPosition = new Vector2(210f, 0f);
    [Tooltip("중앙에 있는 선택 버튼의 스케일입니다.")]
    [SerializeField] private float centerScale = 0.9f;
    [Tooltip("양쪽 뒤에 보이는 버튼의 스케일입니다.")]
    [SerializeField] private float sideScale = 0.7f;
    [SerializeField] private float hiddenScale = 0.45f;
    [SerializeField] private float centerRotation = 0f;
    [SerializeField] private float previousRotation = 0f;
    [SerializeField] private float nextRotation = 0f;


    [Header("Centered Text")]
    [Tooltip("각 스테이지 버튼에 표시할 텍스트입니다. 비워두면 스테이지 버튼 자식에서 TMP_Text를 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text[] stageTexts = new TMP_Text[3];
    [Tooltip("스테이지별 표시 이름입니다. 기본값은 폐허, 수로, 성역입니다.")]
    [SerializeField] private string[] stageDisplayNames = { "폐허", "수로", "성역" };
    [Tooltip("중앙에 온 스테이지 버튼의 텍스트만 켜고, 양쪽 버튼의 텍스트는 끕니다.")]
    [SerializeField] private bool showTextOnlyOnCenteredButton = true;
    [Tooltip("시작 시 Stage Display Names 값을 텍스트 컴포넌트에 자동으로 넣습니다.")]
    [SerializeField] private bool applyStageDisplayNamesToTexts = true;
    [Tooltip("Stage Texts가 비어 있을 때 각 스테이지 버튼의 자식 TMP_Text를 자동으로 찾아 연결합니다.")]
    [SerializeField] private bool autoBindStageTexts = true;


    [Header("Stage Front Image Color")]
    [Tooltip("각 스테이지 버튼의 Button_Front 자식 이미지입니다. 비워두면 Button_Front 이름의 자식에서 Image를 자동으로 찾습니다.")]
    [SerializeField] private Image[] stageFrontImages = new Image[3];
    [Tooltip("중앙에 온 버튼의 Button_Front 이미지 색상입니다.")]
    [SerializeField] private Color centeredFrontImageColor = Color.white;
    [Tooltip("중앙이 아닌 버튼의 Button_Front 이미지 색상입니다.")]
    [SerializeField] private Color sideFrontImageColor = new Color32(0x52, 0x52, 0x52, 0xFF);
    [Tooltip("Stage Front Images가 비어 있을 때 각 스테이지 버튼에서 Button_Front 이미지를 자동으로 찾습니다.")]
    [SerializeField] private bool autoBindStageFrontImages = true;

    [Header("Animation")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Selection")]
    [Tooltip("중앙으로 온 스테이지를 즉시 선택 상태로 저장합니다. PlayButton을 누르면 이 스테이지로 시작됩니다.")]
    [SerializeField] private bool applyStageSelectionWhenCentered = true;
    [SerializeField] private bool sendPointerHoverToCenteredButton = true;
    [SerializeField] private bool onlyCenteredButtonInteractable = false;
    [Tooltip("잠겨있는 스테이지 버튼도 캐러셀 위치 이동과 중앙 이동 대상에 포함합니다. 잠금 여부는 실제 선택 저장 단계에서 처리합니다.")]
    [SerializeField] private bool includeLockedButtonsInCarousel = true;

    [Header("Mouse Drag")]
    [Tooltip("마우스를 누른 상태로 좌우로 끌었을 때 스테이지를 넘깁니다.")]
    [SerializeField] private bool enableMouseDrag = true;
    [SerializeField] private float dragThreshold = 70f;
    [SerializeField] private bool invertDragDirection = false;

    private RectTransform[] rects;
    private Coroutine animationCoroutine;
    private int currentIndex = -1;
    private Vector2 dragStartPosition;
    private bool isDragging;

    public int CurrentIndex => currentIndex;

    public MapChapterSelectButton CurrentChapterButton => GetCurrentChapterButton();

    public bool IsCurrentStageLocked()
    {
        MapChapterSelectButton chapterButton = GetCurrentChapterButton();
        return chapterButton != null && chapterButton.IsLocked();
    }

    private MapChapterSelectButton GetCurrentChapterButton()
    {
        if (!IsIndexInRange(currentIndex))
            return null;

        return GetChapterButton(stageButtons[currentIndex]);
    }

    private void Awake()
    {
        AutoBindStageButtonsIfNeeded();
        CacheRects();
        AutoBindStageTextsIfNeeded();
        AutoBindStageFrontImagesIfNeeded();
        ApplyStageDisplayNames();
        BindNavigationButtons();
        RefreshNavigationButtonInteractable();
    }

    private void OnEnable()
    {
        AutoBindStageButtonsIfNeeded();
        CacheRects();
        AutoBindStageTextsIfNeeded();
        AutoBindStageFrontImagesIfNeeded();
        ApplyStageDisplayNames();
        BindNavigationButtons();

        if (selectFirstAvailableOnEnable && currentIndex < 0)
            currentIndex = GetFirstAvailableIndex();

        ApplyLayout(true);
        ApplyCenteredTextVisibility();
        ApplyStageFrontImageColors();
        ApplyCenteredSelection();
    }

    private void OnDisable()
    {
        isDragging = false;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        UnbindNavigationButtons();
    }

    private void OnDestroy()
    {
        UnbindNavigationButtons();
    }

    public void ShowPreviousStage()
    {
        MoveSelection(-1);
    }

    public void ShowNextStage()
    {
        MoveSelection(1);
    }

    public int MoveSelection(int direction)
    {
        if (stageButtons == null || stageButtons.Length <= 0)
            return -1;

        AutoBindStageButtonsIfNeeded();
        CacheRects();
        AutoBindStageTextsIfNeeded();
        AutoBindStageFrontImagesIfNeeded();
        ApplyStageDisplayNames();

        int nextIndex = FindNextAvailableIndex(currentIndex, direction);

        if (nextIndex < 0)
            return currentIndex;

        SetSelection(nextIndex, false);
        return currentIndex;
    }

    public void SetSelection(int index, bool instant)
    {
        AutoBindStageButtonsIfNeeded();
        CacheRects();
        AutoBindStageTextsIfNeeded();
        AutoBindStageFrontImagesIfNeeded();
        ApplyStageDisplayNames();

        if (!IsButtonUsable(index))
            index = GetFirstAvailableIndex();

        if (index < 0)
            return;

        currentIndex = index;
        ApplyLayout(instant);
        ApplyCenteredTextVisibility();
        ApplyStageFrontImageColors();
        ApplyCenteredSelection();
        RefreshNavigationButtonInteractable();
    }

    public bool HandleStageButtonClick(Button clickedButton)
    {
        int index = GetButtonIndex(clickedButton);

        if (!IsButtonUsable(index))
            return false;

        SetSelection(index, false);
        return true;
    }

    public bool IsCenteredButton(Button button)
    {
        int index = GetButtonIndex(button);
        return index >= 0 && index == currentIndex;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableMouseDrag)
            return;

        isDragging = true;
        dragStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableMouseDrag || !isDragging)
            return;

        float deltaX = eventData.position.x - dragStartPosition.x;

        if (Mathf.Abs(deltaX) < dragThreshold)
            return;

        int direction = deltaX > 0f ? -1 : 1;

        if (invertDragDirection)
            direction *= -1;

        MoveSelection(direction);
        dragStartPosition = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void BindNavigationButtons()
    {
        if (!bindNavigationButtonClicks)
            return;

        UnbindNavigationButtons();

        if (previousStageNavigationButton != null)
            previousStageNavigationButton.onClick.AddListener(ShowPreviousStage);

        if (nextStageNavigationButton != null)
            nextStageNavigationButton.onClick.AddListener(ShowNextStage);
    }

    private void UnbindNavigationButtons()
    {
        if (previousStageNavigationButton != null)
            previousStageNavigationButton.onClick.RemoveListener(ShowPreviousStage);

        if (nextStageNavigationButton != null)
            nextStageNavigationButton.onClick.RemoveListener(ShowNextStage);
    }

    private void RefreshNavigationButtonInteractable()
    {
        if (wrapSelection)
        {
            if (previousStageNavigationButton != null)
                previousStageNavigationButton.interactable = true;

            if (nextStageNavigationButton != null)
                nextStageNavigationButton.interactable = true;

            return;
        }

        if (previousStageNavigationButton != null)
            previousStageNavigationButton.interactable = FindNextAvailableIndex(currentIndex, -1) >= 0;

        if (nextStageNavigationButton != null)
            nextStageNavigationButton.interactable = FindNextAvailableIndex(currentIndex, 1) >= 0;
    }

    private void AutoBindStageButtonsIfNeeded()
    {
        if (!autoBindStageButtons)
            return;

        bool hasAnyButton = false;

        if (stageButtons != null)
        {
            for (int i = 0; i < stageButtons.Length; i++)
            {
                if (stageButtons[i] != null)
                {
                    hasAnyButton = true;
                    break;
                }
            }
        }

        if (hasAnyButton)
            return;

        Transform root = stageButtonRoot != null ? stageButtonRoot : transform;
        MapChapterSelectButton[] chapterButtons = root.GetComponentsInChildren<MapChapterSelectButton>(true);

        if (chapterButtons != null && chapterButtons.Length > 0)
        {
            stageButtons = new Button[chapterButtons.Length];

            for (int i = 0; i < chapterButtons.Length; i++)
                stageButtons[i] = chapterButtons[i].GetComponent<Button>();

            return;
        }

        stageButtons = root.GetComponentsInChildren<Button>(true);
    }

    private void CacheRects()
    {
        if (stageButtons == null)
        {
            rects = null;
            return;
        }

        if (rects != null && rects.Length == stageButtons.Length)
            return;

        rects = new RectTransform[stageButtons.Length];

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] != null)
                rects[i] = stageButtons[i].GetComponent<RectTransform>();
        }
    }

    private void ApplyLayout(bool instant)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (instant || moveDuration <= 0f || !gameObject.activeInHierarchy)
        {
            ApplyLayoutImmediate();
            return;
        }

        animationCoroutine = StartCoroutine(AnimateLayout());
    }

    private void ApplyLayoutImmediate()
    {
        if (stageButtons == null || rects == null)
            return;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            ApplyRectState(i, 1f);
            ApplyButtonInteractable(i);
        }

        BringSideButtonsToBack();
        BringCenteredButtonToFront();
        ApplyCenteredTextVisibility();
        ApplyStageFrontImageColors();
        RefreshNavigationButtonInteractable();
    }

    private IEnumerator AnimateLayout()
    {
        if (stageButtons == null || rects == null)
            yield break;

        Vector2[] startPositions = new Vector2[rects.Length];
        Vector3[] startScales = new Vector3[rects.Length];
        Quaternion[] startRotations = new Quaternion[rects.Length];

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];

            if (rect == null)
                continue;

            startPositions[i] = rect.anchoredPosition;
            startScales[i] = rect.localScale;
            startRotations[i] = rect.localRotation;
            ApplyButtonInteractable(i);
        }

        BringSideButtonsToBack();
        BringCenteredButtonToFront();

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (elapsed < duration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            for (int i = 0; i < rects.Length; i++)
                ApplyRectState(i, curvedT, startPositions, startScales, startRotations);

            yield return null;
        }

        for (int i = 0; i < rects.Length; i++)
            ApplyRectState(i, 1f);

        BringSideButtonsToBack();
        BringCenteredButtonToFront();
        ApplyCenteredTextVisibility();
        ApplyStageFrontImageColors();
        RefreshNavigationButtonInteractable();
        animationCoroutine = null;
    }

    private void ApplyRectState(int index, float t)
    {
        ApplyRectState(index, t, null, null, null);
    }

    private void ApplyRectState(int index, float t, Vector2[] startPositions, Vector3[] startScales, Quaternion[] startRotations)
    {
        if (rects == null || index < 0 || index >= rects.Length)
            return;

        RectTransform rect = rects[index];

        if (rect == null)
            return;

        Vector2 targetPosition;
        Vector3 targetScale;
        Quaternion targetRotation;
        GetTargetState(index, out targetPosition, out targetScale, out targetRotation);

        if (startPositions == null || startScales == null || startRotations == null)
        {
            rect.anchoredPosition = targetPosition;
            rect.localScale = targetScale;
            rect.localRotation = targetRotation;
            return;
        }

        rect.anchoredPosition = Vector2.LerpUnclamped(startPositions[index], targetPosition, t);
        rect.localScale = Vector3.LerpUnclamped(startScales[index], targetScale, t);
        rect.localRotation = Quaternion.LerpUnclamped(startRotations[index], targetRotation, t);
    }

    private void GetTargetState(int index, out Vector2 position, out Vector3 scale, out Quaternion rotation)
    {
        int relation = GetRelationToCurrent(index);

        if (relation == 0)
        {
            position = centerPosition;
            scale = Vector3.one * centerScale;
            rotation = Quaternion.Euler(0f, 0f, centerRotation);
            return;
        }

        if (relation < 0)
        {
            position = previousPosition;
            scale = Vector3.one * sideScale;
            rotation = Quaternion.Euler(0f, 0f, previousRotation);
            return;
        }

        if (relation == 1)
        {
            position = nextPosition;
            scale = Vector3.one * sideScale;
            rotation = Quaternion.Euler(0f, 0f, nextRotation);
            return;
        }

        position = centerPosition;
        scale = Vector3.one * hiddenScale;
        rotation = Quaternion.identity;
    }

    private int GetRelationToCurrent(int index)
    {
        if (index == currentIndex)
            return 0;

        int previousIndex = FindNextAvailableIndex(currentIndex, -1);
        int nextIndex = FindNextAvailableIndex(currentIndex, 1);

        if (index == previousIndex)
            return -1;

        if (index == nextIndex)
            return 1;

        return 2;
    }

    private void BringSideButtonsToBack()
    {
        if (stageButtons == null)
            return;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (i == currentIndex || stageButtons[i] == null)
                continue;

            stageButtons[i].transform.SetAsFirstSibling();
        }
    }

    private void BringCenteredButtonToFront()
    {
        if (!IsIndexInRange(currentIndex) || stageButtons[currentIndex] == null)
            return;

        stageButtons[currentIndex].transform.SetAsLastSibling();
    }

    private void ApplyButtonInteractable(int index)
    {
        if (!onlyCenteredButtonInteractable)
            return;

        if (!IsIndexInRange(index) || stageButtons[index] == null)
            return;

        stageButtons[index].interactable = index == currentIndex;
    }


    private void AutoBindStageTextsIfNeeded()
    {
        if (!autoBindStageTexts || stageButtons == null)
            return;

        bool hasAnyText = false;

        if (stageTexts != null)
        {
            for (int i = 0; i < stageTexts.Length; i++)
            {
                if (stageTexts[i] != null)
                {
                    hasAnyText = true;
                    break;
                }
            }
        }

        if (hasAnyText)
            return;

        stageTexts = new TMP_Text[stageButtons.Length];

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null)
                continue;

            stageTexts[i] = stageButtons[i].GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void ApplyStageDisplayNames()
    {
        if (!applyStageDisplayNamesToTexts || stageTexts == null)
            return;

        for (int i = 0; i < stageTexts.Length; i++)
        {
            if (stageTexts[i] == null)
                continue;

            if (stageDisplayNames != null && i < stageDisplayNames.Length && !string.IsNullOrEmpty(stageDisplayNames[i]))
                stageTexts[i].text = stageDisplayNames[i];
        }
    }

    private void ApplyCenteredTextVisibility()
    {
        if (!showTextOnlyOnCenteredButton || stageTexts == null)
            return;

        for (int i = 0; i < stageTexts.Length; i++)
        {
            if (stageTexts[i] == null)
                continue;

            stageTexts[i].gameObject.SetActive(i == currentIndex);
        }
    }



    private void AutoBindStageFrontImagesIfNeeded()
    {
        if (!autoBindStageFrontImages || stageButtons == null)
            return;

        bool hasAnyImage = false;

        if (stageFrontImages != null)
        {
            for (int i = 0; i < stageFrontImages.Length; i++)
            {
                if (stageFrontImages[i] != null)
                {
                    hasAnyImage = true;
                    break;
                }
            }
        }

        if (hasAnyImage)
            return;

        stageFrontImages = new Image[stageButtons.Length];

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null)
                continue;

            Transform frontTransform = FindChildRecursive(stageButtons[i].transform, "Button_Front");

            if (frontTransform == null)
                continue;

            Transform frontImageTransform = FindChildRecursive(frontTransform, "Image");
            Image frontImage = frontImageTransform != null ? frontImageTransform.GetComponent<Image>() : null;

            if (frontImage == null)
                frontImage = frontTransform.GetComponent<Image>();

            if (frontImage != null)
                stageFrontImages[i] = frontImage;
        }
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private void ApplyStageFrontImageColors()
    {
        if (stageFrontImages == null)
            return;

        for (int i = 0; i < stageFrontImages.Length; i++)
        {
            Image image = stageFrontImages[i];

            if (image == null)
                continue;

            image.color = i == currentIndex ? centeredFrontImageColor : sideFrontImageColor;
        }
    }


    private void ApplyCenteredSelection()
    {
        if (!IsIndexInRange(currentIndex))
            return;

        Button button = stageButtons[currentIndex];

        if (button == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);

        if (sendPointerHoverToCenteredButton)
            SendPointerEnter(button.gameObject);

        if (!applyStageSelectionWhenCentered)
            return;

        MapChapterSelectButton chapterButton = GetChapterButton(button);

        if (chapterButton != null)
            chapterButton.SelectChapterForCarousel();
    }

    private int FindNextAvailableIndex(int startIndex, int direction)
    {
        if (stageButtons == null || stageButtons.Length <= 0)
            return -1;

        int count = stageButtons.Length;
        int normalizedDirection = direction >= 0 ? 1 : -1;
        int index = startIndex;

        if (!IsIndexInRange(index))
            index = normalizedDirection > 0 ? -1 : count;

        for (int i = 0; i < count; i++)
        {
            index += normalizedDirection;

            if (wrapSelection)
            {
                if (index < 0)
                    index = count - 1;
                else if (index >= count)
                    index = 0;
            }
            else if (index < 0 || index >= count)
            {
                return -1;
            }

            if (IsButtonUsable(index))
                return index;
        }

        return -1;
    }

    private int GetFirstAvailableIndex()
    {
        if (stageButtons == null)
            return -1;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (IsButtonUsable(i))
                return i;
        }

        return -1;
    }

    private bool IsButtonUsable(int index)
    {
        if (!IsIndexInRange(index))
            return false;

        Button button = stageButtons[index];

        if (button == null)
            return false;

        if (!button.gameObject.activeInHierarchy)
            return false;

        if (!includeLockedButtonsInCarousel)
        {
            MapChapterSelectButton chapterButton = GetChapterButton(button);

            if (chapterButton != null && chapterButton.IsLocked())
                return false;
        }

        return true;
    }

    private bool IsIndexInRange(int index)
    {
        return stageButtons != null && index >= 0 && index < stageButtons.Length;
    }

    private int GetButtonIndex(Button button)
    {
        if (button == null || stageButtons == null)
            return -1;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == button)
                return i;
        }

        return -1;
    }

    private MapChapterSelectButton GetChapterButton(Button button)
    {
        if (button == null)
            return null;

        MapChapterSelectButton chapterButton = button.GetComponent<MapChapterSelectButton>();

        if (chapterButton == null)
            chapterButton = button.GetComponentInParent<MapChapterSelectButton>();

        if (chapterButton == null)
            chapterButton = button.GetComponentInChildren<MapChapterSelectButton>(true);

        return chapterButton;
    }

    private void SendPointerEnter(GameObject targetObject)
    {
        if (targetObject == null || EventSystem.current == null)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = Input.mousePosition
        };

        ExecuteEvents.Execute(targetObject, pointerData, ExecuteEvents.pointerEnterHandler);
    }
}
