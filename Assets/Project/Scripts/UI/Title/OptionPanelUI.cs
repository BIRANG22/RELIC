using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionPanelUI : MonoBehaviour
{
    private const string SoundContentName = "SoundContent";
    private const string LanguageContentName = "LanguageContent";
    private const string ResolutionContentName = "ResolutionContent";
    private const string ControlContentName = "ControlContent";

    [Header("Contents")]
    [SerializeField] private GameObject soundContent;
    [SerializeField] private GameObject languageContent;
    [SerializeField] private GameObject resolutionContent;
    [SerializeField] private GameObject controlContent;

    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Tutorial")]
    [SerializeField] private Toggle tutorialToggle;

    [Header("Resolution Template Sorting")]
    [SerializeField] private int resolutionDropdownSortingOrderOffset = 50;

    [Header("Language Template Sorting")]
    [SerializeField] private int languageDropdownSortingOrderOffset = 50;

    [Header("Save Toast")]
    [SerializeField] private string saveSuccessMessage = "저장되었습니다.";
    [SerializeField] private string saveFailedMessage = "저장 실패";
    [SerializeField] private float saveToastDuration = 1.4f;
    [SerializeField] private int saveToastSortingOrder = 32100;

    private bool isResolutionDropdownReady;

    private void OnEnable()
    {
        AutoFindReferences();
        SetupLanguageDropdown();
        SetupResolutionDropdown();
        SetupTutorialToggle();
        ShowAllContents();
    }

    private void OnDestroy()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

        if (tutorialToggle != null)
            tutorialToggle.onValueChanged.RemoveListener(OnTutorialToggleChanged);
    }

    private void ShowAllContents()
    {
        SetContentActive(soundContent, true);
        SetContentActive(languageContent, true);
        SetContentActive(resolutionContent, true);
        SetContentActive(controlContent, true);

        SyncTutorialToggleFromSettings();
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

    private void AutoFindReferences()
    {
        if (soundContent == null)
            soundContent = FindChildGameObject(SoundContentName);

        if (languageContent == null)
            languageContent = FindChildGameObject(LanguageContentName);

        if (resolutionContent == null)
            resolutionContent = FindChildGameObject(ResolutionContentName);

        if (controlContent == null)
            controlContent = FindChildGameObject(ControlContentName);
    }

    private void SetupLanguageDropdown()
    {
        TMP_Dropdown contentDropdown = languageContent != null
            ? languageContent.GetComponentInChildren<TMP_Dropdown>(true)
            : null;

        DirectTemplateDropdown.Attach(contentDropdown)
            ?.Configure(languageDropdownSortingOrderOffset);
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
        DirectTemplateDropdown.Attach(resolutionDropdown)?.Configure(resolutionDropdownSortingOrderOffset);

        isResolutionDropdownReady = true;
    }

    private void SetupTutorialToggle()
    {
        if (controlContent == null)
            controlContent = FindChildGameObject(ControlContentName);

        if (tutorialToggle == null && controlContent != null)
            tutorialToggle = controlContent.GetComponentInChildren<Toggle>(true);

        if (tutorialToggle == null)
        {
            Debug.LogWarning(
                "[OptionPanelUI] TutorialToggle is not assigned in the Option prefab.",
                this);
            return;
        }

        tutorialToggle.onValueChanged.RemoveListener(OnTutorialToggleChanged);
        tutorialToggle.SetIsOnWithoutNotify(TutorialSettings.ShouldShowTutorial);
        tutorialToggle.onValueChanged.AddListener(OnTutorialToggleChanged);
    }

    private void OnTutorialToggleChanged(bool shouldShowTutorial)
    {
        TutorialSettings.SetShouldShowTutorial(shouldShowTutorial);
    }

    private void SyncTutorialToggleFromSettings()
    {
        if (tutorialToggle == null)
            return;

        tutorialToggle.SetIsOnWithoutNotify(TutorialSettings.ShouldShowTutorial);
    }

    private void OnResolutionChanged(int index)
    {
        if (!isResolutionDropdownReady)
            return;

        ResolutionManager.ApplyResolution(index, true);
    }

    private GameObject FindChildGameObject(string childName)
    {
        Transform child = FindChildByName(transform, childName);
        return child != null ? child.gameObject : null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != root && child.name == childName)
                return child;
        }

        return null;
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

