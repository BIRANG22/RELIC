using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class OptionPanelUITests
{
    private const string OptionPrefabPath = "Assets/Project/PrefabsR/Option.prefab";
    private const string OptionFontGuid = "561f3002d19400a47bae19552e5b0ed5";
    private const long OptionFontFileId = 11400000;
    private const long OptionFontMaterialFileId = -7555710420834941891;

    private readonly List<GameObject> createdObjects = new();
    private bool hadTutorialPreference;
    private int originalTutorialPreference;

    [SetUp]
    public void SetUp()
    {
        hadTutorialPreference = PlayerPrefs.HasKey(TutorialSettings.ShowTutorialPrefsKey);
        originalTutorialPreference = PlayerPrefs.GetInt(TutorialSettings.ShowTutorialPrefsKey, 1);
        PlayerPrefs.DeleteKey(TutorialSettings.ShowTutorialPrefsKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        SaveResultToastUI[] toasts = Object.FindObjectsByType<SaveResultToastUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = toasts.Length - 1; i >= 0; i--)
        {
            if (toasts[i] != null)
                Object.DestroyImmediate(toasts[i].gameObject);
        }

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();

        if (hadTutorialPreference)
            PlayerPrefs.SetInt(TutorialSettings.ShowTutorialPrefsKey, originalTutorialPreference);
        else
            PlayerPrefs.DeleteKey(TutorialSettings.ShowTutorialPrefsKey);

        PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator ShowResolution_OpensResolutionDropdownAfterActivationFrame()
    {
        OptionPanelUI panel = CreateOptionPanel(out GameObject resolutionContent, out TMP_Dropdown dropdown);

        panel.ShowResolution();

        Assert.That(resolutionContent.activeSelf, Is.True);
        Assert.That(dropdown.IsExpanded, Is.False);

        yield return null;

        Assert.That(dropdown.IsExpanded, Is.True);
        Assert.That(
            dropdown.options.ConvertAll(option => option.text),
            Is.EqualTo(ResolutionManager.GetSupportedResolutionLabels()));
    }

    [UnityTest]
    public IEnumerator ShowResolution_RaisesGeneratedDropdownAboveExistingHighSortingCanvas()
    {
        Canvas highSortingCanvas = CreateHighSortingCanvas(32000);
        OptionPanelUI panel = CreateOptionPanel(out _, out TMP_Dropdown dropdown);

        panel.ShowResolution();

        yield return null;
        yield return null;

        Transform dropdownList = dropdown.transform.Find("Dropdown List");
        Assert.That(dropdownList, Is.Not.Null);

        Canvas dropdownListCanvas = dropdownList.GetComponent<Canvas>();
        Assert.That(dropdownListCanvas, Is.Not.Null);
        Assert.That(dropdownListCanvas.sortingOrder, Is.GreaterThan(highSortingCanvas.sortingOrder));
    }

    [UnityTest]
    public IEnumerator SaveProgress_WhenSaveSystemMissing_ShowsCenteredFailureToast()
    {
        OptionPanelUI panel = CreateOptionPanel(out _, out _);

        panel.SaveProgress();

        yield return null;

        TextMeshProUGUI toastText = FindText("저장 실패");
        Assert.That(toastText, Is.Not.Null);

        RectTransform toastRect = toastText.GetComponent<RectTransform>();
        Assert.That(toastRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(toastRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(toastRect.anchoredPosition, Is.EqualTo(Vector2.zero));
    }

    [UnityTest]
    public IEnumerator ShowControl_ActivatesControlContentAndSavesTutorialToggle()
    {
        TutorialSettings.SetShouldShowTutorial(false);
        OptionPanelUI panel = CreateOptionPanel(
            out GameObject soundContent,
            out GameObject languageContent,
            out GameObject resolutionContent,
            out _,
            out GameObject controlContent,
            out Toggle tutorialToggle);

        panel.ShowControl();

        Assert.That(soundContent.activeSelf, Is.False);
        Assert.That(languageContent.activeSelf, Is.False);
        Assert.That(resolutionContent.activeSelf, Is.False);
        Assert.That(controlContent.activeSelf, Is.True);
        Assert.That(tutorialToggle.isOn, Is.False);

        tutorialToggle.isOn = true;
        yield return null;

        Assert.That(TutorialSettings.ShouldShowTutorial, Is.True);
    }

    [UnityTest]
    public IEnumerator ShowSound_ActivatesOnlySoundContent()
    {
        OptionPanelUI panel = CreateOptionPanel(
            out GameObject soundContent,
            out GameObject languageContent,
            out GameObject resolutionContent,
            out _,
            out GameObject controlContent,
            out _);

        panel.ShowSound();
        yield return null;

        Assert.That(soundContent.activeSelf, Is.True);
        Assert.That(languageContent.activeSelf, Is.False);
        Assert.That(resolutionContent.activeSelf, Is.False);
        Assert.That(controlContent.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator ShowLanguage_ActivatesOnlyLanguageContent()
    {
        OptionPanelUI panel = CreateOptionPanel(
            out GameObject soundContent,
            out GameObject languageContent,
            out GameObject resolutionContent,
            out _,
            out GameObject controlContent,
            out _);

        panel.ShowLanguage();
        yield return null;

        Assert.That(soundContent.activeSelf, Is.False);
        Assert.That(languageContent.activeSelf, Is.True);
        Assert.That(resolutionContent.activeSelf, Is.False);
        Assert.That(controlContent.activeSelf, Is.False);
    }

    [Test]
    public void TutorialSettings_DefaultsToShowingTutorialUntilFirstShown()
    {
        Assert.That(TutorialSettings.ShouldShowTutorial, Is.True);
    }

    [Test]
    public void OptionTMPFontEnforcer_ApplyFontsReplacesFontAndMaterial()
    {
        TMP_FontAsset optionFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/DungGeunMo SDF.asset");
        TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        Assert.That(optionFont, Is.Not.Null);
        Assert.That(fallbackFont, Is.Not.Null);
        Assert.That(optionFont.material, Is.Not.Null);

        GameObject root = Track(new GameObject("FontEnforcerRoot"));
        TextMeshProUGUI text = CreateText("사운드", root.transform);
        text.font = fallbackFont;
        text.fontSharedMaterial = fallbackFont.material;

        OptionTMPFontEnforcer enforcer = root.AddComponent<OptionTMPFontEnforcer>();
        SetPrivateField(enforcer, "defaultFontAsset", optionFont);
        SetPrivateField(enforcer, "dropdownFontAsset", optionFont);

        enforcer.ApplyFonts();

        Assert.That(text.font, Is.SameAs(optionFont));
        Assert.That(text.fontSharedMaterial, Is.SameAs(optionFont.material));
    }

    [Test]
    public void OptionPrefab_KoreanTextsUseKoreanFontAndMaterial()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(OptionPrefabPath);

        try
        {
            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.That(texts, Is.Not.Empty);

            foreach (TextMeshProUGUI text in texts)
            {
                if (text == null || !ContainsKorean(text.text))
                    continue;

                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(text.font, out string fontGuid, out long fontFileId), Is.True);
                Assert.That(fontGuid, Is.EqualTo(OptionFontGuid), text.name);
                Assert.That(fontFileId, Is.EqualTo(OptionFontFileId), text.name);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(text.fontSharedMaterial, out string materialGuid, out long materialFileId), Is.True);
                Assert.That(materialGuid, Is.EqualTo(OptionFontGuid), text.name);
                Assert.That(materialFileId, Is.EqualTo(OptionFontMaterialFileId), text.name);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void TutorialSettings_MarkTutorialShownDisablesFutureTutorial()
    {
        TutorialSettings.SetShouldShowTutorial(true);

        TutorialSettings.MarkTutorialShown();

        Assert.That(TutorialSettings.ShouldShowTutorial, Is.False);
        Assert.That(PlayerPrefs.GetInt(TutorialSettings.ShowTutorialPrefsKey), Is.EqualTo(0));
    }

    [Test]
    public void OptionPrefab_ContainsSerializedTutorialToggle()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(OptionPrefabPath);

        try
        {
            OptionPanelUI panel = root.GetComponentInChildren<OptionPanelUI>(true);
            Assert.That(panel, Is.Not.Null);

            var serializedPanel = new SerializedObject(panel);
            GameObject controlContent = serializedPanel.FindProperty("controlContent").objectReferenceValue as GameObject;
            Toggle tutorialToggle = serializedPanel.FindProperty("tutorialToggle").objectReferenceValue as Toggle;

            Assert.That(controlContent, Is.Not.Null);
            Assert.That(tutorialToggle, Is.Not.Null);
            Assert.That(tutorialToggle.transform.IsChildOf(controlContent.transform), Is.True);
            Assert.That(tutorialToggle.targetGraphic, Is.Not.Null);
            Assert.That(tutorialToggle.graphic, Is.Not.Null);
            Assert.That(
                controlContent.transform.Find("TutorialSettingRow/TutorialLabel")
                    ?.GetComponent<TextMeshProUGUI>(),
                Is.Not.Null);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void OptionPrefab_LanguageDropdownControllerIsHostedByOptionRoot()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(OptionPrefabPath);

        try
        {
            OptionPanelUI panel = root.GetComponentInChildren<OptionPanelUI>(true);
            LanguageDropdownUI languageController = root.GetComponentInChildren<LanguageDropdownUI>(true);

            Assert.That(panel, Is.Not.Null);
            Assert.That(languageController, Is.Not.Null);
            Assert.That(languageController.gameObject, Is.SameAs(panel.gameObject));
            Assert.That(languageController.gameObject.activeSelf, Is.True);

            var serializedPanel = new SerializedObject(panel);
            var serializedController = new SerializedObject(languageController);
            GameObject languageContent = serializedPanel.FindProperty("languageContent").objectReferenceValue as GameObject;
            TMP_Dropdown languageDropdown = serializedController.FindProperty("languageDropdown").objectReferenceValue as TMP_Dropdown;

            Assert.That(languageContent, Is.Not.Null);
            Assert.That(languageDropdown, Is.Not.Null);
            Assert.That(languageDropdown.transform.IsChildOf(languageContent.transform), Is.True);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private OptionPanelUI CreateOptionPanel(out GameObject resolutionContent, out TMP_Dropdown dropdown)
    {
        return CreateOptionPanel(
            out _,
            out _,
            out resolutionContent,
            out dropdown,
            out _,
            out _);
    }

    private OptionPanelUI CreateOptionPanel(
        out GameObject soundContent,
        out GameObject languageContent,
        out GameObject resolutionContent,
        out TMP_Dropdown dropdown,
        out GameObject controlContent,
        out Toggle tutorialToggle)
    {
        GameObject root = Track(new GameObject("OptionPanelRoot"));
        root.SetActive(false);

        GameObject canvasObject = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)));
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1920f, 1080f);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        Track(new GameObject("EventSystem", typeof(EventSystem)));

        root.transform.SetParent(canvasObject.transform, false);

        soundContent = new GameObject("SoundContent");
        soundContent.transform.SetParent(root.transform, false);

        languageContent = new GameObject("LanguageContent");
        languageContent.transform.SetParent(root.transform, false);

        resolutionContent = new GameObject("ResolutionContent");
        resolutionContent.transform.SetParent(root.transform, false);

        dropdown = CreateDropdown(resolutionContent.transform);

        controlContent = new GameObject("ControlContent");
        controlContent.transform.SetParent(root.transform, false);
        tutorialToggle = CreateToggle(controlContent.transform);

        OptionPanelUI panel = root.AddComponent<OptionPanelUI>();
        SetPrivateField(panel, "soundContent", soundContent);
        SetPrivateField(panel, "languageContent", languageContent);
        SetPrivateField(panel, "resolutionContent", resolutionContent);
        SetPrivateField(panel, "resolutionDropdown", dropdown);
        SetPrivateField(panel, "controlContent", controlContent);
        SetPrivateField(panel, "tutorialToggle", tutorialToggle);

        root.SetActive(true);
        return panel;
    }

    private TMP_Dropdown CreateDropdown(Transform parent)
    {
        GameObject dropdownObject = new("ResolutionDropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        dropdownObject.transform.SetParent(parent, false);
        RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
        dropdownRect.sizeDelta = new Vector2(320f, 60f);

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();

        TextMeshProUGUI captionText = CreateText("Label", dropdownObject.transform);
        RectTransform captionRect = captionText.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.sizeDelta = Vector2.zero;

        RectTransform template = CreateDropdownTemplate(dropdownObject.transform, out TextMeshProUGUI itemText);
        template.gameObject.SetActive(false);

        dropdown.captionText = captionText;
        dropdown.itemText = itemText;
        dropdown.template = template;

        return dropdown;
    }

    private RectTransform CreateDropdownTemplate(Transform parent, out TextMeshProUGUI itemText)
    {
        GameObject templateObject = new("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        templateObject.transform.SetParent(parent, false);
        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.sizeDelta = new Vector2(0f, 180f);

        GameObject itemObject = new("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        itemObject.transform.SetParent(templateObject.transform, false);
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.sizeDelta = new Vector2(0f, 30f);

        itemText = CreateText("Item Label", itemObject.transform);
        RectTransform itemTextRect = itemText.GetComponent<RectTransform>();
        itemTextRect.anchorMin = Vector2.zero;
        itemTextRect.anchorMax = Vector2.one;
        itemTextRect.sizeDelta = Vector2.zero;

        return templateRect;
    }

    private Toggle CreateToggle(Transform parent)
    {
        GameObject toggleObject = new("TutorialToggle", typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);

        GameObject backgroundObject = new("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(toggleObject.transform, false);
        Image background = backgroundObject.GetComponent<Image>();

        GameObject checkmarkObject = new("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkmarkObject.transform.SetParent(backgroundObject.transform, false);
        Image checkmark = checkmarkObject.GetComponent<Image>();

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        return toggle;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = name;
        return text;
    }

    private Canvas CreateHighSortingCanvas(int sortingOrder)
    {
        GameObject canvasObject = Track(new GameObject("HighSortingCanvas", typeof(RectTransform), typeof(Canvas)));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        return canvas;
    }

    private TextMeshProUGUI FindText(string text)
    {
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].text == text)
                return texts[i];
        }

        return null;
    }

    private GameObject Track(GameObject gameObject)
    {
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static bool ContainsKorean(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c >= '\uac00' && c <= '\ud7a3')
                return true;
        }

        return false;
    }
}
