using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LanguageDropdownUI : MonoBehaviour
{
    [Header("Dropdown")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Language Preview Objects")]
    [SerializeField] private GameObject krObject;
    [SerializeField] private GameObject enObject;
    [SerializeField] private GameObject scObject;
    [SerializeField] private GameObject jpObject;
    [SerializeField] private GameObject spObject;

    [Header("Dropdown Sorting")]
    [SerializeField] private bool bringDropdownListToFront = true;
    [SerializeField] private int dropdownSortingOrderOffset = 50;

    private const string SaveKey = "SelectedLanguage";
    private const string DropdownListObjectName = "Dropdown List";
    private const string BlockerObjectName = "Blocker";

    private readonly List<string> localeCodes = new()
    {
        "ko",
        "en",
        "zh-Hans",
        "ja",
        "es"
    };

    private bool isInitialized;

    private async void Start()
    {
        await LocalizationSettings.InitializationOperation.Task;

        SetupDropdownOptions();
        SyncDropdownFromCurrentLocale();
        RefreshLanguagePreview(languageDropdown.value);
        AttachDropdownFrontGuard();

        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }
    }

    private void SetupDropdownOptions()
    {
        if (languageDropdown == null)
        {
            Debug.LogError("[LanguageDropdownUI] languageDropdown is null.");
            return;
        }

        languageDropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>
        {
            new("한국어"),
            new("English"),
            new("中文"),
            new("日本語"),
            new("Español")
        };

        languageDropdown.AddOptions(options);
    }

    private void SyncDropdownFromCurrentLocale()
    {
        string currentCode = LocalizationSettings.SelectedLocale.Identifier.Code;
        int index = localeCodes.IndexOf(currentCode);

        if (index < 0)
        {
            index = 0;
        }

        languageDropdown.SetValueWithoutNotify(index);
    }

    private void OnLanguageChanged(int index)
    {
        if (!isInitialized)
            return;

        if (index < 0 || index >= localeCodes.Count)
        {
            Debug.LogWarning($"[LanguageDropdownUI] Invalid dropdown index: {index}");
            return;
        }

        string localeCode = localeCodes[index];
        ApplyLanguage(localeCode);
        RefreshLanguagePreview(index);
    }

    private async void ApplyLanguage(string localeCode)
    {
        await LocalizationSettings.InitializationOperation.Task;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                PlayerPrefs.SetString(SaveKey, localeCode);
                PlayerPrefs.Save();

                Debug.Log($"[LanguageDropdownUI] Language changed: {localeCode}");
                return;
            }
        }

        Debug.LogWarning($"[LanguageDropdownUI] Locale not found: {localeCode}");
    }

    private void RefreshLanguagePreview(int index)
    {
        if (krObject != null) krObject.SetActive(index == 0);
        if (enObject != null) enObject.SetActive(index == 1);
        if (scObject != null) scObject.SetActive(index == 2);
        if (jpObject != null) jpObject.SetActive(index == 3);
        if (spObject != null) spObject.SetActive(index == 4);
    }

    private void AttachDropdownFrontGuard()
    {
        if (!bringDropdownListToFront || languageDropdown == null)
            return;

        DropdownFrontGuard guard = languageDropdown.GetComponent<DropdownFrontGuard>();
        if (guard == null)
            guard = languageDropdown.gameObject.AddComponent<DropdownFrontGuard>();

        guard.Configure(dropdownSortingOrderOffset);
    }

    private sealed class DropdownFrontGuard : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        private int sortingOrderOffset = 50;
        private Coroutine bringToFrontCoroutine;

        public void Configure(int offset)
        {
            sortingOrderOffset = Mathf.Max(1, offset);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            BringOptionCanvasToFront();
            ScheduleBringDropdownListToFront();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            BringOptionCanvasToFront();
            ScheduleBringDropdownListToFront();
        }

        private void ScheduleBringDropdownListToFront()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (bringToFrontCoroutine != null)
                StopCoroutine(bringToFrontCoroutine);

            bringToFrontCoroutine = StartCoroutine(BringDropdownListToFrontRoutine());
        }

        private IEnumerator BringDropdownListToFrontRoutine()
        {
            yield return null;
            BringDropdownGeneratedObjectsToFront();

            yield return null;
            BringDropdownGeneratedObjectsToFront();

            bringToFrontCoroutine = null;
        }

        private void BringOptionCanvasToFront()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
                return;

            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = GetHighestCanvasSortingOrder(parentCanvas) + sortingOrderOffset;

            if (parentCanvas.GetComponent<GraphicRaycaster>() == null)
                parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private void BringDropdownGeneratedObjectsToFront()
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
}
