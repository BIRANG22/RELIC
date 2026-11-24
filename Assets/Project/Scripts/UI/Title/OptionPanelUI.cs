using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class OptionPanelUI : MonoBehaviour
{
    private const string SoundContentName = "SoundContent";
    private const string ResolutionContentName = "ResolutionContent";

    private const string WindowedLabelKey = "ui.option.display_mode.windowed";
    private const string BorderlessLabelKey = "ui.option.display_mode.borderless";
    private const string FullscreenLabelKey = "ui.option.display_mode.fullscreen";
    private const string ControlContentName = "ControlContent";

    [Header("Tab Buttons")]
    [SerializeField] private Button gameButton;
    [SerializeField] private Button graphicButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button controlButton;

    [Header("Contents")]
    [SerializeField] private GameObject gameContent;
    [SerializeField] private GameObject soundContent;
    [SerializeField] private GameObject resolutionContent;
    [SerializeField] private GameObject controlContent;

    [Header("Resolution")]
    [SerializeField] private Button resolutionPreviousButton;
    [SerializeField] private Button resolutionNextButton;
    [SerializeField] private TMP_Text resolutionValueText;
    [Tooltip("Font database for resolution numbers. Assign the existing LocaleFontDatabase asset if it is outside Resources.")]
    [SerializeField] private LocaleFontDatabase resolutionFontDatabase;

    [Header("Display Mode Arrows")]
    [SerializeField] private Button displayModePreviousButton;
    [SerializeField] private Button displayModeNextButton;
    [SerializeField] private TMP_Text displayModeValueText;
    [SerializeField] private bool useLocalizedDisplayModeNames = true;
    [SerializeField] private string windowedName = "창화면";
    [SerializeField] private string borderlessName = "테두리 없는 화면";
    [SerializeField] private string fullscreenName = "전체화면";

    [Header("Battle Presentation Speed")]
    [SerializeField] private Button battlePresentationSpeedPreviousButton;
    [SerializeField] private Button battlePresentationSpeedNextButton;
    [SerializeField] private TextMeshProUGUI battlePresentationSpeedLabel;

    [Header("Tutorial")]
    [SerializeField] private Button tutorialPreviousButton;
    [SerializeField] private Button tutorialNextButton;
    [SerializeField] private TMP_Text tutorialValueText;
    [SerializeField] private string tutorialYesKey = "common.yes";
    [SerializeField] private string tutorialNoKey = "common.no";

    [Header("Language Arrows")]
    [SerializeField] private Button languagePreviousButton;
    [SerializeField] private Button languageNextButton;
    [SerializeField] private TMP_Text languageValueText;
    private const string LanguagePreferenceKey = "OptionPanelUI.LanguageCode";
    private bool languageReady;
    private int languageInitVersion;

    [Header("Save Toast")]
    [SerializeField] private float saveToastDuration = 1.4f;
    [SerializeField] private int saveToastSortingOrder = 32100;


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (resolutionFontDatabase != null)
            return;

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LocaleFontDatabase");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            LocaleFontDatabase candidate = UnityEditor.AssetDatabase.LoadAssetAtPath<LocaleFontDatabase>(path);
            if (candidate == null || candidate.CommonNumberFont == null)
                continue;
            resolutionFontDatabase = candidate;
            break;
        }
    }
#endif

    private void OnEnable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady += OnLocaleTableReady;

        AutoFindReferences();
        BindArrowSelectors();
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLanguageChanged;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLanguageChanged;
        InitializeLanguageSelector();
        RefreshResolutionLabel();
        RefreshDisplayModeLabels();
        RefreshOptionHeadingLabels();
        RefreshOptionHeadingLabelsWhenReady();
        BindTabButtons();
        RestoreActiveTab();
        SyncTutorialToggleFromSettings();
        RefreshBattlePresentationSpeedLabel();
    }

    private void OnDisable()
    {
        RestoreTabColors();
        languageInitVersion++;
        languageReady = false;
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLanguageChanged;
        UnbindTabButtons();
        UnbindArrowSelectors();
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
    }

    private void OnDestroy()
    {
        UnbindArrowSelectors();
        UnbindTabButtons();
    }

    private void BindTabButtons()
    {
        UnbindTabButtons();
        Transform buttons = transform.Find("Buttons");
        if (buttons == null)
            buttons = FindChildByName(transform, "Buttons");

        if (gameButton == null) gameButton = FindTabButton(buttons, "Game");
        if (graphicButton == null) graphicButton = FindTabButton(buttons, "Graphic");
        if (audioButton == null) audioButton = FindTabButton(buttons, "Audio");
        if (controlButton == null) controlButton = FindTabButton(buttons, "Control");

        if (gameButton != null) gameButton.onClick.AddListener(ShowGame);
        if (graphicButton != null) graphicButton.onClick.AddListener(ShowResolution);
        if (audioButton != null) audioButton.onClick.AddListener(ShowSound);
        if (controlButton != null) controlButton.onClick.AddListener(ShowControl);
    }

    private void UnbindTabButtons()
    {
        if (gameButton != null) gameButton.onClick.RemoveListener(ShowGame);
        if (graphicButton != null) graphicButton.onClick.RemoveListener(ShowResolution);
        if (audioButton != null) audioButton.onClick.RemoveListener(ShowSound);
        if (controlButton != null) controlButton.onClick.RemoveListener(ShowControl);
    }

    private static Button FindTabButton(Transform root, string name)
    {
        Transform child = FindChildByName(root, name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    public void ShowGame()
    {
        ShowSingleContent(gameContent);
        SyncTutorialToggleFromSettings();
        RefreshLanguageLabel();
    }

    public void ShowSound() => ShowSingleContent(soundContent);
    public void ShowLanguage() => ShowGame();
    public void ShowResolution()
    {
        ShowSingleContent(resolutionContent);
        RefreshDisplayModeLabels();
        RefreshResolutionLabel();
    }
    public void ShowControl() => ShowSingleContent(controlContent);

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
            GameLocalization.Get(saved
                ? LocalizationKeys.SystemMessage.SaveSuccess
                : LocalizationKeys.SystemMessage.SaveFailed),
            saveToastDuration,
            saveToastSortingOrder);
    }

    /// <summary>
    /// Option 프리팹의 전투 배속 버튼 UnityEvent에서 호출됩니다.
    /// 전투 연출용 로컬 속도만 다음 단계로 순환합니다.
    /// </summary>
    public void CycleBattlePresentationSpeed() => NextBattlePresentationSpeed();
    public void PreviousBattlePresentationSpeed() => ChangeBattlePresentationSpeed(-1);
    public void NextBattlePresentationSpeed() => ChangeBattlePresentationSpeed(1);

    private void ChangeBattlePresentationSpeed(int direction)
    {
        int count = BattlePresentationSpeedSettings.OptionCount;
        if (count <= 0) return;
        int index = (BattlePresentationSpeedSettings.CurrentIndex + direction + count) % count;
        BattlePresentationSpeedSettings.SetSelectedIndex(index);
        RefreshBattlePresentationSpeedLabel();
    }

    private void AutoFindReferences()
    {
        if (gameContent == null)
            gameContent = FindChildGameObject("GameContent");
        if (soundContent == null)
            soundContent = FindChildGameObject(SoundContentName);

        if (resolutionContent == null)
            resolutionContent = FindChildGameObject(ResolutionContentName);

        if (controlContent == null)
            controlContent = FindChildGameObject(ControlContentName);

    }

    private async void InitializeLanguageSelector()
    {
        int version = ++languageInitVersion;
        languageReady = false;
        SetLanguageButtonsInteractable(false);
        try
        {
            await LocalizationSettings.InitializationOperation.Task;
            if (!this || !isActiveAndEnabled || version != languageInitVersion) return;
            languageReady = true;
            string savedCode = PlayerPrefs.GetString(LanguagePreferenceKey, string.Empty);
            if (!string.IsNullOrEmpty(savedCode))
            {
                Locale saved = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(savedCode));
                if (saved != null && LocalizationSettings.SelectedLocale != saved)
                    LocalizationSettings.SelectedLocale = saved;
            }
            RefreshLanguageLabel();
        }
        catch (System.Exception e)
        {
            if (this && version == languageInitVersion)
                Debug.LogWarning("[OptionPanelUI] Language initialization failed: " + e.Message, this);
        }
    }

    public void PreviousLanguage() => CycleLanguage(-1);
    public void NextLanguage() => CycleLanguage(1);

    private void CycleLanguage(int direction)
    {
        if (!languageReady) return;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count < 2) return;
        int index = -1;
        for (int i = 0; i < locales.Count; i++)
            if (locales[i] == LocalizationSettings.SelectedLocale) { index = i; break; }
        int next = index < 0 ? (direction < 0 ? locales.Count - 1 : 0)
            : (index + direction + locales.Count) % locales.Count;
        Locale selected = locales[next];
        if (selected == null) return;
        LocalizationSettings.SelectedLocale = selected;
        PlayerPrefs.SetString(LanguagePreferenceKey, selected.Identifier.Code);
        PlayerPrefs.Save();
        RefreshLanguageLabel();
    }

    private void OnSelectedLanguageChanged(Locale locale)
    {
        if (languageReady && locale != null)
        {
            PlayerPrefs.SetString(LanguagePreferenceKey, locale.Identifier.Code);
            PlayerPrefs.Save();
        }
        RefreshLanguageLabel();
    }

    private void SetLanguageButtonsInteractable(bool value)
    {
        if (languagePreviousButton != null) languagePreviousButton.interactable = value;
        if (languageNextButton != null) languageNextButton.interactable = value;
    }

    private void RefreshLanguageLabel()
    {
        if (!languageReady) return;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        SetLanguageButtonsInteractable(locales != null && locales.Count > 1);
        if (languageValueText == null) return;
        Locale locale = LocalizationSettings.SelectedLocale;
        LocalizedTMPText.ProtectDynamicOwnership(languageValueText);
        languageValueText.text = locale == null ? string.Empty : GetLanguageDisplayName(locale);
        LocaleTMPFontGuard.Prepare(languageValueText);
        languageValueText.ForceMeshUpdate();
    }

    private static string GetLanguageDisplayName(Locale locale)
    {
        switch (locale.Identifier.Code.ToLowerInvariant())
        {
            case "ko": case "ko-kr": return "한국어";
            case "en": case "en-us": case "en-gb": return "English";
            case "ja": case "ja-jp": return "日本語";
            case "zh-hans": case "zh-cn": return "简体中文";
            case "zh-hant": case "zh-tw": case "zh-hk": return "繁體中文";
            case "es": case "es-es": return "Español";
            default: return locale.LocaleName;
        }
    }

    public void PreviousResolution() => CycleResolution(-1);
    public void NextResolution() => CycleResolution(1);

    private void CycleResolution(int direction)
    {
        List<string> labels = ResolutionManager.GetSupportedResolutionLabels();
        if (labels == null || labels.Count == 0) return;
        int index = Mathf.Clamp(ResolutionManager.CurrentResolutionIndex, 0, labels.Count - 1);
        int next = (index + direction + labels.Count) % labels.Count;
        if (next != index)
            ResolutionManager.ApplyResolution(next, true);
        RefreshResolutionLabel();
    }

    private void RefreshResolutionLabel()
    {
        List<string> labels = ResolutionManager.GetSupportedResolutionLabels();
        int count = labels != null ? labels.Count : 0;
        if (resolutionPreviousButton != null) resolutionPreviousButton.interactable = count > 1;
        if (resolutionNextButton != null) resolutionNextButton.interactable = count > 1;
        if (resolutionValueText == null) return;
        LocalizedTMPText.ProtectDynamicOwnership(resolutionValueText);
        resolutionValueText.text = count > 0
            ? labels[Mathf.Clamp(ResolutionManager.CurrentResolutionIndex, 0, count - 1)]
            : string.Empty;
        LocaleTMPFontGuard.Prepare(resolutionValueText);
        LocaleFontDatabase fontDatabase = resolutionFontDatabase != null
            ? resolutionFontDatabase : LocaleFontDatabaseProvider.LoadDefault();
        if (fontDatabase != null && fontDatabase.CommonNumberFont != null)
            resolutionValueText.font = fontDatabase.CommonNumberFont;
        resolutionValueText.ForceMeshUpdate();
    }

    private void BindArrowSelectors()
    {
        UnbindArrowSelectors();
        Transform language = FindChildByName(gameContent != null ? gameContent.transform : transform, "LanguageSelector");
        if (languagePreviousButton == null) languagePreviousButton = FindTabButton(language, "LeftButton");
        if (languageNextButton == null) languageNextButton = FindTabButton(language, "RightButton");
        if (languageValueText == null) languageValueText = FindSelectorText(language);
        if (languagePreviousButton != null) languagePreviousButton.onClick.AddListener(PreviousLanguage);
        if (languageNextButton != null && languageNextButton != languagePreviousButton) languageNextButton.onClick.AddListener(NextLanguage);
        Transform speed = FindChildByName(transform, "BattleSpeedSelector");
        if (battlePresentationSpeedPreviousButton == null) battlePresentationSpeedPreviousButton = FindTabButton(speed, "LeftButton");
        if (battlePresentationSpeedNextButton == null) battlePresentationSpeedNextButton = FindTabButton(speed, "RightButton");
        if (speed != null)
        {
            TextMeshProUGUI value = FindSelectorText(speed) as TextMeshProUGUI;
            if (value != null && (battlePresentationSpeedLabel == null || !battlePresentationSpeedLabel.transform.IsChildOf(speed)))
                battlePresentationSpeedLabel = value;
        }
        if (battlePresentationSpeedPreviousButton != null) battlePresentationSpeedPreviousButton.onClick.AddListener(PreviousBattlePresentationSpeed);
        if (battlePresentationSpeedNextButton != null && battlePresentationSpeedNextButton != battlePresentationSpeedPreviousButton) battlePresentationSpeedNextButton.onClick.AddListener(NextBattlePresentationSpeed);
        Transform resolution = FindChildByName(resolutionContent != null ? resolutionContent.transform : transform, "ResolutionSelector");
        if (resolutionPreviousButton == null) resolutionPreviousButton = FindTabButton(resolution, "LeftButton");
        if (resolutionNextButton == null) resolutionNextButton = FindTabButton(resolution, "RightButton");
        if (resolutionValueText == null) resolutionValueText = FindSelectorText(resolution);
        if (resolutionPreviousButton != null) resolutionPreviousButton.onClick.AddListener(PreviousResolution);
        if (resolutionNextButton != null) resolutionNextButton.onClick.AddListener(NextResolution);
        Transform mode = FindChildByName(transform, "DisplayModeSelector");
        Transform tutorial = FindChildByName(transform, "TutorialSelector");
        if (displayModePreviousButton == null) displayModePreviousButton = FindTabButton(mode, "LeftButton");
        if (displayModeNextButton == null) displayModeNextButton = FindTabButton(mode, "RightButton");
        if (displayModeValueText == null) displayModeValueText = FindSelectorText(mode);
        if (tutorialPreviousButton == null) tutorialPreviousButton = FindTabButton(tutorial, "LeftButton");
        if (tutorialNextButton == null) tutorialNextButton = FindTabButton(tutorial, "RightButton");
        if (tutorialValueText == null) tutorialValueText = FindSelectorText(tutorial);
        if (displayModePreviousButton != null) displayModePreviousButton.onClick.AddListener(PreviousDisplayMode);
        if (displayModeNextButton != null) displayModeNextButton.onClick.AddListener(NextDisplayMode);
        if (tutorialPreviousButton != null) tutorialPreviousButton.onClick.AddListener(CycleTutorial);
        if (tutorialNextButton != null && tutorialNextButton != tutorialPreviousButton) tutorialNextButton.onClick.AddListener(CycleTutorial);
    }

    private static TMP_Text FindSelectorText(Transform root)
    {
        Transform child = FindChildByName(root, "ValueText");
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private void UnbindArrowSelectors()
    {
        if (languagePreviousButton != null) languagePreviousButton.onClick.RemoveListener(PreviousLanguage);
        if (languageNextButton != null) languageNextButton.onClick.RemoveListener(NextLanguage);
        if (battlePresentationSpeedPreviousButton != null) battlePresentationSpeedPreviousButton.onClick.RemoveListener(PreviousBattlePresentationSpeed);
        if (battlePresentationSpeedNextButton != null) battlePresentationSpeedNextButton.onClick.RemoveListener(NextBattlePresentationSpeed);
        if (resolutionPreviousButton != null) resolutionPreviousButton.onClick.RemoveListener(PreviousResolution);
        if (resolutionNextButton != null) resolutionNextButton.onClick.RemoveListener(NextResolution);
        if (displayModePreviousButton != null) displayModePreviousButton.onClick.RemoveListener(PreviousDisplayMode);
        if (displayModeNextButton != null) displayModeNextButton.onClick.RemoveListener(NextDisplayMode);
        if (tutorialPreviousButton != null) tutorialPreviousButton.onClick.RemoveListener(CycleTutorial);
        if (tutorialNextButton != null) tutorialNextButton.onClick.RemoveListener(CycleTutorial);
    }

    private static readonly ResolutionManager.DisplayMode[] DisplayModes =
    {
        ResolutionManager.DisplayMode.Windowed,
        ResolutionManager.DisplayMode.BorderlessFullscreen,
        ResolutionManager.DisplayMode.ExclusiveFullscreen
    };

    public void PreviousDisplayMode() => CycleDisplayMode(-1);
    public void NextDisplayMode() => CycleDisplayMode(1);

    private void CycleDisplayMode(int direction)
    {
        int index = System.Array.IndexOf(DisplayModes, ResolutionManager.CurrentDisplayMode);
        if (index < 0) index = 0;
        index = (index + direction + DisplayModes.Length) % DisplayModes.Length;
        ResolutionManager.SetDisplayMode(DisplayModes[index], true);
        RefreshDisplayModeLabels();
        RefreshResolutionLabel();
    }

    public void CycleTutorial()
    {
        TutorialSettings.SetShouldPlayTutorial(!TutorialSettings.ShouldPlayTutorial);
        SyncTutorialToggleFromSettings();
    }

    private void RefreshDisplayModeLabels()
    {
        switch (ResolutionManager.CurrentDisplayMode)
        {
            case ResolutionManager.DisplayMode.BorderlessFullscreen:
                SetDisplayModeName(BorderlessLabelKey, borderlessName);
                break;
            case ResolutionManager.DisplayMode.ExclusiveFullscreen:
                SetDisplayModeName(FullscreenLabelKey, fullscreenName);
                break;
            default:
                SetDisplayModeName(WindowedLabelKey, windowedName);
                break;
        }
    }

    private void SetDisplayModeName(string key, string name)
    {
        if (useLocalizedDisplayModeNames)
        {
            SetSelectorLabel(displayModeValueText, key, name);
            return;
        }
        if (displayModeValueText == null) return;
        LocalizedTMPText.ProtectDynamicOwnership(displayModeValueText);
        displayModeValueText.text = name;
        LocaleTMPFontGuard.Prepare(displayModeValueText);
        displayModeValueText.ForceMeshUpdate();
    }

    private static void SetSelectorLabel(TMP_Text label, string key, string fallback)
    {
        if (label == null) return;
        LocalizedTMPText.ProtectDynamicOwnership(label);
        string value = GameLocalization.Get(key, fallback);
        label.text = string.IsNullOrWhiteSpace(value) || value == key || value == "Untranslated" || value == "미번역" || value == "未翻译" || value == "未翻訳" || value == "Sin traducir" ? fallback : value;
        LocaleTMPFontGuard.Prepare(label);
        label.ForceMeshUpdate();
    }

    private void OnLocaleTableReady(UnityEngine.Localization.Locale _)
    {
        RefreshLanguageLabel();
        RefreshDisplayModeLabels();
        RefreshResolutionLabel();
        SyncTutorialToggleFromSettings();
        RefreshOptionHeadingLabels();
    }

    // Option prefab header labels may retain an old missing key (and display "Untranslated").
    // The panel owns these four headings; dropdown captions and other dynamic values stay untouched.
    private void RefreshOptionHeadingLabels()
    {
        SetOptionHeading("Language_text", "ui.option.header.language", "언어");
        SetOptionHeading("LanguageText", "ui.option.header.language", "언어");
        SetOptionHeading("Resolution_text", "ui.option.header.resolution", "해상도");
        SetOptionHeading("ResolutionText", "ui.option.header.resolution", "해상도");
        SetOptionHeading("Sound_text", "ui.option.header.sound", "사운드");
        SetOptionHeading("SoundText", "ui.option.header.sound", "사운드");
        SetOptionHeading("Control_text", "ui.option.header.control", "조작");
        SetOptionHeading("ControlText", "ui.option.header.control", "조작");
        SetOptionHeading("Tutorial_text", "ui.tutorialpanel.titletext.74198686", "튜토리얼");
        SetOptionHeading("TutorialText", "ui.tutorialpanel.titletext.74198686", "튜토리얼");
        // The instantiated Option prefab uses Back/Name/TutorialLabel.
        SetOptionHeading("TutorialLabel", "ui.tutorialpanel.titletext.74198686", "튜토리얼");
    }


    private void SetOptionHeading(string objectName, string key, string koreanSource)
    {
        Transform child = FindChildByName(transform, objectName);
        if (child == null)
            return;

        TMP_Text label = child.GetComponent<TMP_Text>();
        if (label == null)
            return;

        LocalizedTMPText.ProtectDynamicOwnership(label);
        // Never leave the visible placeholder on an otherwise functional option panel.
        string translation = GameLocalization.Get(key, koreanSource);
        if (string.IsNullOrWhiteSpace(translation) ||
            translation == "Untranslated" || translation == "미번역" ||
            translation == "未翻译" || translation == "未翻訳" || translation == "Sin traducir")
            translation = koreanSource;
        label.text = translation;
        LocaleTMPFontGuard.Prepare(label);
        label.ForceMeshUpdate();
    }

    private async void RefreshOptionHeadingLabelsWhenReady()
    {
        try
        {
            await UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation.Task;
            if (!this || !isActiveAndEnabled)
                return;
            UnityEngine.Localization.Locale locale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
            if (locale != null)
                await UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase
                    .GetTableAsync(GameLocalization.TableName, locale).Task;
        }
        catch (System.Exception)
        {
            // Preserve the Korean source until the requested locale table becomes available.
        }

        if (this && isActiveAndEnabled)
        {
            RefreshOptionHeadingLabels();
            RefreshDisplayModeLabels();
            RefreshResolutionLabel();
            SyncTutorialToggleFromSettings();
        }
    }


    private void SyncTutorialToggleFromSettings()
    {
        bool enabled = TutorialSettings.ShouldPlayTutorial;
        SetSelectorLabel(tutorialValueText, enabled ? tutorialYesKey : tutorialNoKey, enabled ? "예" : "아니오");
    }

    private void RefreshBattlePresentationSpeedLabel()
    {
        if (battlePresentationSpeedLabel == null)
        {
            Transform speedButton = FindChildByName(transform, "BattleSpeedButton");
            if (speedButton != null)
                battlePresentationSpeedLabel = speedButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (battlePresentationSpeedLabel != null)
        {
            LocalizedTMPText.ProtectDynamicOwnership(battlePresentationSpeedLabel);
            battlePresentationSpeedLabel.text = $"{BattlePresentationSpeedSettings.CurrentDisplayMultiplier:0.0}X";
        }
        else
            Debug.LogWarning("[BattlePresentationSpeed] Speed label reference is missing.", this);
    }


    private void RestoreActiveTab()
    {
        // activeSelf preserves the tab state even while the option panel is closed.
        if (gameContent != null && gameContent.activeSelf) ShowGame();
        else if (resolutionContent != null && resolutionContent.activeSelf) ShowResolution();
        else if (soundContent != null && soundContent.activeSelf) ShowSound();
        else if (controlContent != null && controlContent.activeSelf) ShowControl();
        else if (gameContent != null) ShowGame();
        else if (resolutionContent != null) ShowResolution();
        else if (soundContent != null) ShowSound();
        else if (controlContent != null) ShowControl();
    }

    private readonly Dictionary<Button, ColorBlock> originalTabColors = new Dictionary<Button, ColorBlock>();

    private void SelectTabForContent(GameObject content)
    {
        ApplyTabColor(gameButton, content == gameContent);
        ApplyTabColor(graphicButton, content == resolutionContent);
        ApplyTabColor(audioButton, content == soundContent);
        ApplyTabColor(controlButton, content == controlContent);
    }

    private void ApplyTabColor(Button button, bool active)
    {
        if (button == null) return;
        if (!originalTabColors.TryGetValue(button, out ColorBlock original))
        {
            original = button.colors;
            originalTabColors.Add(button, original);
        }

        ColorBlock colors = original;
        // The active tab stays selected visually regardless of EventSystem focus.
        // Inactive tabs must not show the selected tint just because they have focus.
        colors.selectedColor = active ? original.selectedColor : original.normalColor;
        if (active)
        {
            colors.normalColor = original.selectedColor;
            colors.highlightedColor = original.selectedColor;
            colors.pressedColor = original.selectedColor;
        }
        button.colors = colors;
    }

    private void RestoreTabColors()
    {
        foreach (var entry in originalTabColors)
            if (entry.Key != null) entry.Key.colors = entry.Value;
        originalTabColors.Clear();
    }

    private void ShowSingleContent(GameObject activeContent)
    {
        if (activeContent == null)
            return;

        SetContentActive(gameContent, gameContent == activeContent);
        SetContentActive(resolutionContent, resolutionContent == activeContent);
        SetContentActive(soundContent, soundContent == activeContent);
        SetContentActive(controlContent, controlContent == activeContent);
        SelectTabForContent(activeContent);
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

