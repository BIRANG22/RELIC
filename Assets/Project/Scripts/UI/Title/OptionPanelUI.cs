using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionPanelUI : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] private GameObject soundContent;
    [SerializeField] private GameObject languageContent;
    [SerializeField] private GameObject resolutionContent;

    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Resolution Dropdown Sorting")]
    [SerializeField] private bool bringResolutionDropdownListToFront = true;
    [SerializeField] private int resolutionDropdownSortingOrderOffset = 50;

    [Header("Save Toast")]
    [SerializeField] private string saveSuccessMessage = "저장되었습니다.";
    [SerializeField] private string saveFailedMessage = "저장 실패";
    [SerializeField] private float saveToastDuration = 1.4f;
    [SerializeField] private int saveToastSortingOrder = 32100;

    private bool isResolutionDropdownReady;
    private Coroutine openResolutionDropdownCoroutine;
    private TMPDropdownFrontGuard resolutionDropdownFrontGuard;

    private void OnEnable()
    {
        SetupResolutionDropdown();
        ShowSound();
    }

    private void OnDisable()
    {
        CancelScheduledResolutionDropdown();
    }

    private void OnDestroy()
    {
        CancelScheduledResolutionDropdown();

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
    }

    public void ShowSound()
    {
        CancelScheduledResolutionDropdown();

        SetContentActive(soundContent, true);
        SetContentActive(languageContent, false);
        SetContentActive(resolutionContent, false);
    }

    public void ShowLanguage()
    {
        CancelScheduledResolutionDropdown();

        SetContentActive(soundContent, false);
        SetContentActive(languageContent, true);
        SetContentActive(resolutionContent, false);
    }

    public void ShowResolution()
    {
        SetupResolutionDropdown();

        SetContentActive(soundContent, false);
        SetContentActive(languageContent, false);
        SetContentActive(resolutionContent, true);

        ScheduleOpenResolutionDropdown();
    }

    public void SaveProgress()
    {
        bool saved = false;

        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[OptionPanelUI] SaveSystem is not ready. Progress was not saved.");
        }
        else
        {
            saved = SaveSystem.Instance.SaveCurrentProgress();
        }

        SaveResultToastUI.Show(
            saved ? saveSuccessMessage : saveFailedMessage,
            saveToastDuration,
            saveToastSortingOrder);
    }

    private void SetupResolutionDropdown()
    {
        TMP_Dropdown contentDropdown = resolutionContent != null
            ? resolutionContent.GetComponentInChildren<TMP_Dropdown>(true)
            : null;

        if (contentDropdown != null && resolutionDropdown != contentDropdown)
        {
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

            resolutionDropdown = contentDropdown;
        }

        if (resolutionDropdown == null)
            return;

        isResolutionDropdownReady = false;

        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        resolutionDropdown.ClearOptions();

        List<string> labels = ResolutionManager.GetSupportedResolutionLabels();
        var options = new List<TMP_Dropdown.OptionData>(labels.Count);

        for (int i = 0; i < labels.Count; i++)
            options.Add(new TMP_Dropdown.OptionData(labels[i]));

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(ResolutionManager.CurrentResolutionIndex);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        AttachResolutionDropdownFrontGuard();

        isResolutionDropdownReady = true;
    }

    private void OnResolutionChanged(int index)
    {
        if (!isResolutionDropdownReady)
            return;

        ResolutionManager.ApplyResolution(index, true);
    }

    private void OpenResolutionDropdown()
    {
        if (resolutionDropdown == null)
            return;

        if (!resolutionDropdown.gameObject.activeInHierarchy)
            return;

        resolutionDropdown.Show();
        resolutionDropdownFrontGuard?.BringOpenedDropdownToFront();
    }

    private void AttachResolutionDropdownFrontGuard()
    {
        resolutionDropdownFrontGuard = null;

        if (!bringResolutionDropdownListToFront || resolutionDropdown == null)
            return;

        resolutionDropdownFrontGuard = resolutionDropdown.GetComponent<TMPDropdownFrontGuard>();
        if (resolutionDropdownFrontGuard == null)
            resolutionDropdownFrontGuard = resolutionDropdown.gameObject.AddComponent<TMPDropdownFrontGuard>();

        resolutionDropdownFrontGuard.Configure(resolutionDropdownSortingOrderOffset);
    }

    private void ScheduleOpenResolutionDropdown()
    {
        CancelScheduledResolutionDropdown();

        if (!isActiveAndEnabled)
            return;

        openResolutionDropdownCoroutine = StartCoroutine(OpenResolutionDropdownNextFrame());
    }

    private IEnumerator OpenResolutionDropdownNextFrame()
    {
        yield return null;

        openResolutionDropdownCoroutine = null;
        OpenResolutionDropdown();
    }

    private void CancelScheduledResolutionDropdown()
    {
        if (openResolutionDropdownCoroutine == null)
            return;

        StopCoroutine(openResolutionDropdownCoroutine);
        openResolutionDropdownCoroutine = null;
    }

    private static void SetContentActive(GameObject content, bool active)
    {
        if (content != null)
            content.SetActive(active);
    }
}

public sealed class SaveResultToastUI : MonoBehaviour
{
    private const string ToastObjectName = "Save Result Toast";
    private const string TextObjectName = "Message";
    private const int DefaultSortingOrder = 32100;

    private TextMeshProUGUI messageText;
    private Coroutine hideCoroutine;

    public static void Show(string message, float duration, int sortingOrder)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        SaveResultToastUI toast = FindFirstObjectByType<SaveResultToastUI>(FindObjectsInactive.Include);
        if (toast == null)
            toast = Create(Mathf.Max(DefaultSortingOrder, sortingOrder));
        else
            toast.ConfigureCanvas(Mathf.Max(DefaultSortingOrder, sortingOrder));

        toast.ShowMessage(message, duration);
    }

    private static SaveResultToastUI Create(int sortingOrder)
    {
        var toastObject = new GameObject(
            ToastObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        SaveResultToastUI toast = toastObject.AddComponent<SaveResultToastUI>();
        toast.ConfigureCanvas(sortingOrder);
        toast.EnsureMessageText();
        return toast;
    }

    private void ConfigureCanvas(int sortingOrder)
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;
    }

    private void ShowMessage(string message, float duration)
    {
        EnsureMessageText();
        messageText.text = message;
        gameObject.SetActive(true);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDelay(Mathf.Max(0.1f, duration)));
    }

    private IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        hideCoroutine = null;
        Destroy(gameObject);
    }

    private void EnsureMessageText()
    {
        if (messageText != null)
            return;

        Transform existing = transform.Find(TextObjectName);
        if (existing != null)
            messageText = existing.GetComponent<TextMeshProUGUI>();

        if (messageText == null)
            messageText = CreateMessageText();
    }

    private TextMeshProUGUI CreateMessageText()
    {
        var textObject = new GameObject(
            TextObjectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(520f, 96f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }
}

public sealed class TMPDropdownFrontGuard : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private const string DropdownListObjectName = "Dropdown List";
    private const string BlockerObjectName = "Blocker";

    private int sortingOrderOffset = 50;
    private Coroutine bringToFrontCoroutine;

    public void Configure(int offset)
    {
        sortingOrderOffset = Mathf.Max(1, offset);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PrepareForDropdownOpen();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PrepareForDropdownOpen();
    }

    public void PrepareForDropdownOpen()
    {
        BringOwnerCanvasToFront();
        ScheduleGeneratedObjectsToFront();
    }

    public void BringOpenedDropdownToFront()
    {
        BringOwnerCanvasToFront();
        BringGeneratedObjectsToFront();
        ScheduleGeneratedObjectsToFront();
    }

    private void ScheduleGeneratedObjectsToFront()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (bringToFrontCoroutine != null)
            StopCoroutine(bringToFrontCoroutine);

        bringToFrontCoroutine = StartCoroutine(BringGeneratedObjectsToFrontRoutine());
    }

    private IEnumerator BringGeneratedObjectsToFrontRoutine()
    {
        yield return null;
        BringGeneratedObjectsToFront();

        yield return null;
        BringGeneratedObjectsToFront();

        bringToFrontCoroutine = null;
    }

    private void BringOwnerCanvasToFront()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            return;

        parentCanvas.overrideSorting = true;
        parentCanvas.sortingOrder = GetHighestCanvasSortingOrder(parentCanvas) + sortingOrderOffset;

        if (parentCanvas.GetComponent<GraphicRaycaster>() == null)
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void BringGeneratedObjectsToFront()
    {
        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int baseOrder = GetHighestCanvasSortingOrder(null) + sortingOrderOffset;

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject target = objects[i];
            if (target == null)
                continue;

            if (target.name != DropdownListObjectName && target.name != BlockerObjectName)
                continue;

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null)
                canvas = target.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = target.name == DropdownListObjectName ? baseOrder + 1 : baseOrder;

            if (target.GetComponent<GraphicRaycaster>() == null)
                target.AddComponent<GraphicRaycaster>();
        }
    }

    private int GetHighestCanvasSortingOrder(Canvas excludedCanvas)
    {
        int highestOrder = 0;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            if (canvas == excludedCanvas)
                continue;

            if (!canvas.gameObject.activeInHierarchy)
                continue;

            if (canvas.sortingOrder > highestOrder)
                highestOrder = canvas.sortingOrder;
        }

        return highestOrder;
    }
}
