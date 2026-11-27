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
    [SerializeField] private RectTransform control;

    [Header("Shortcut Layout")]
    [SerializeField, Min(0f)] private float shortcutLabelWidth = 150f;
    [SerializeField, Min(0f)] private float shortcutButtonWidth = 200f;
    [SerializeField, Min(1f)] private float shortcutRowHeight = 80f;
    [SerializeField] private float shortcutRowSpacing = 0f;
    [SerializeField] private Vector2 shortcutHighlightSize = new Vector2(425f, 50f);
    [SerializeField] private Vector2 shortcutResetButtonPosition = new Vector2(0f, 75f);
    [SerializeField] private Vector2 shortcutResetButtonSize = new Vector2(260f, 60f);

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

    [Header("Background Audio")]
    [SerializeField] private Button backgroundAudioPreviousButton;
    [SerializeField] private Button backgroundAudioNextButton;
    [SerializeField] private TMP_Text backgroundAudioValueText;
    [SerializeField] private string backgroundAudioOnKey = "ui.option.background_audio.on";
    [SerializeField] private string backgroundAudioOffKey = "ui.option.background_audio.off";

    [Header("Language Arrows")]
    [SerializeField] private Button languagePreviousButton;
    [SerializeField] private Button languageNextButton;
    [SerializeField] private TMP_Text languageValueText;
    [Tooltip("Language cycling order, using locale codes.")]
    [SerializeField] private string[] languageOrder = { "ko", "en", "zh-Hans", "zh-Hant", "ja", "es" };
    private bool languageReady;
    private int languageInitVersion;

    [Header("Save Toast")]
    [SerializeField] private float saveToastDuration = 1.4f;
    [SerializeField] private int saveToastSortingOrder = 32100;

    private readonly Dictionary<BattleShortcutAction, Button> shortcutButtons = new Dictionary<BattleShortcutAction, Button>();
    private readonly Dictionary<BattleShortcutAction, TMP_Text> shortcutKeyTexts = new Dictionary<BattleShortcutAction, TMP_Text>();
    private readonly Dictionary<BattleShortcutAction, TMP_Text> shortcutNameTexts = new Dictionary<BattleShortcutAction, TMP_Text>();
    private TMP_Text shortcutResetText;
    private RectTransform shortcutResetRect;
    private BattleShortcutAction? capturingShortcut;


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
        EnsureControlContent();
        RefreshShortcutHoverSound();
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
        RefreshTabLabels();
        RefreshShortcutRows();
        BattleKeyboardShortcutBindings.Changed -= OnShortcutChanged;
        BattleKeyboardShortcutBindings.Changed += OnShortcutChanged;
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
        BattleKeyboardShortcutBindings.Changed -= OnShortcutChanged;
        capturingShortcut = null;
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

    public void ShowSound()
    {
        ShowSingleContent(soundContent);
        RefreshBackgroundAudioLabel();
    }

    public void CycleBackgroundAudio()
    {
        AudioManager.SetBackgroundAudioEnabled(!AudioManager.BackgroundAudioEnabled);
        RefreshBackgroundAudioLabel();
    }

    private void RefreshBackgroundAudioLabel()
    {
        bool enabled = AudioManager.BackgroundAudioEnabled;
        SetSelectorLabel(backgroundAudioValueText, enabled ? backgroundAudioOnKey : backgroundAudioOffKey,
            enabled ? LocalizedOptionText("켜기", "On", "オン", "开启", "開啟", "Activado")
                    : LocalizedOptionText("끄기", "Off", "オフ", "关闭", "關閉", "Desactivado"));
    }
    public void ShowLanguage() => ShowGame();
    public void ShowResolution()
    {
        ShowSingleContent(resolutionContent);
        RefreshDisplayModeLabels();
        RefreshResolutionLabel();
    }
    public void ShowControl() => ShowSingleContent(controlContent);

    private void Update()
    {
        if (!capturingShortcut.HasValue)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            capturingShortcut = null;
            RefreshShortcutRows();
            return;
        }

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (key == KeyCode.None || (int)key >= (int)KeyCode.Mouse0 || !Input.GetKeyDown(key))
                continue;

            BattleKeyboardShortcutBindings.Set(capturingShortcut.Value, key);
            capturingShortcut = null;
            RefreshShortcutRows();
            return;
        }
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
            soundContent = FindChildGameObject("AudioContent") ?? FindChildGameObject(SoundContentName);

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
        var locales = GetOrderedLanguageLocales();
        if (locales.Count < 2) return;
        int index = -1;
        for (int i = 0; i < locales.Count; i++)
            if (locales[i] == LocalizationSettings.SelectedLocale) { index = i; break; }
        int next = index < 0 ? (direction < 0 ? locales.Count - 1 : 0)
            : (index + direction + locales.Count) % locales.Count;
        Locale selected = locales[next];
        if (selected == null) return;
        LocalizationSettings.SelectedLocale = selected;
        PlayerPrefs.SetString(GameLocalization.SelectedLanguagePreferenceKey, selected.Identifier.Code);
        PlayerPrefs.Save();
        RefreshLanguageLabel();
    }

    private List<Locale> GetOrderedLanguageLocales()
    {
        var result = new List<Locale>();
        var available = LocalizationSettings.AvailableLocales.Locales;
        if (available == null) return result;
        if (languageOrder != null)
        {
            foreach (string code in languageOrder)
            {
                if (string.IsNullOrWhiteSpace(code)) continue;
                foreach (Locale locale in available)
                {
                    if (locale != null && string.Equals(locale.Identifier.Code, code.Trim(), System.StringComparison.OrdinalIgnoreCase) && !result.Contains(locale))
                        result.Add(locale);
                }
            }
        }
        foreach (Locale locale in available)
            if (locale != null && !result.Contains(locale)) result.Add(locale);
        return result;
    }

    private void OnSelectedLanguageChanged(Locale _) => RefreshLanguageLabel();

    private void SetLanguageButtonsInteractable(bool value)
    {
        if (languagePreviousButton != null) languagePreviousButton.interactable = value;
        if (languageNextButton != null) languageNextButton.interactable = value;
    }

    private void RefreshLanguageLabel()
    {
        if (!languageReady) return;
        SetLanguageButtonsInteractable(GetOrderedLanguageLocales().Count > 1);
        if (languageValueText == null) return;
        Locale locale = LocalizationSettings.SelectedLocale;
        languageValueText.text = locale == null ? string.Empty : GetLanguageDisplayName(locale);
        PrepareOptionLocalizedText(languageValueText);
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
        Transform audioRoot = soundContent != null ? soundContent.transform : transform;
        Transform backgroundAudio = FindChildByName(audioRoot, "BackgroundAudio");
        if (backgroundAudioPreviousButton == null) backgroundAudioPreviousButton = FindTabButton(backgroundAudio, "LeftButton");
        if (backgroundAudioNextButton == null) backgroundAudioNextButton = FindTabButton(backgroundAudio, "RightButton");
        if (backgroundAudioValueText == null) backgroundAudioValueText = FindSelectorText(backgroundAudio);
        if (backgroundAudioPreviousButton != null) backgroundAudioPreviousButton.onClick.AddListener(CycleBackgroundAudio);
        if (backgroundAudioNextButton != null && backgroundAudioNextButton != backgroundAudioPreviousButton)
            backgroundAudioNextButton.onClick.AddListener(CycleBackgroundAudio);
        RefreshBackgroundAudioLabel();
        Transform gameRoot = gameContent != null ? gameContent.transform : transform;
        Transform language = FindChildByName(gameRoot, "LanguageContent") ?? FindChildByName(gameRoot, "LanguageSelector");
        if (languagePreviousButton == null) languagePreviousButton = FindTabButton(language, "LeftButton");
        if (languageNextButton == null) languageNextButton = FindTabButton(language, "RightButton");
        if (languageValueText == null) languageValueText = FindSelectorText(language);
        ProtectLanguageValueOwnership();
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

    private void ProtectLanguageValueOwnership()
    {
        if (languageValueText == null) return;
        // LocalizationIgnore alone permits a dynamic writer to remember "한국어"
        // and replay its translation on reopen. This value is already a locale name.
        if (languageValueText.GetComponent<LocalizationAutoBindingIgnore>() == null)
            languageValueText.gameObject.AddComponent<LocalizationAutoBindingIgnore>();
        LocalizedTMPText.ProtectDynamicOwnership(languageValueText);
        DynamicLocalizedTMPText dynamicWriter = languageValueText.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicWriter != null)
            dynamicWriter.enabled = false;
    }

    private static TMP_Text FindSelectorText(Transform root)
    {
        Transform child = FindChildByName(root, "ValueText");
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private void UnbindArrowSelectors()
    {
        if (backgroundAudioPreviousButton != null) backgroundAudioPreviousButton.onClick.RemoveListener(CycleBackgroundAudio);
        if (backgroundAudioNextButton != null) backgroundAudioNextButton.onClick.RemoveListener(CycleBackgroundAudio);
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
        PrepareOptionLocalizedText(displayModeValueText);
        displayModeValueText.ForceMeshUpdate();
    }

    private static void SetSelectorLabel(TMP_Text label, string key, string fallback)
    {
        if (label == null) return;
        LocalizedTMPText.ProtectDynamicOwnership(label);
        string value = GameLocalization.Get(key, fallback);
        label.text = string.IsNullOrWhiteSpace(value) || value == key || value == "Untranslated" || value == "미번역" || value == "未翻译" || value == "未翻訳" || value == "Sin traducir" ? fallback : value;
        PrepareOptionLocalizedText(label);
        label.ForceMeshUpdate();
    }

    private void OnLocaleTableReady(UnityEngine.Localization.Locale _)
    {
        RefreshLanguageLabel();
        RefreshDisplayModeLabels();
        RefreshResolutionLabel();
        SyncTutorialToggleFromSettings();
        RefreshBackgroundAudioLabel();
        RefreshOptionHeadingLabels();
        RefreshTabLabels();
        RefreshShortcutRows();
    }

    // Option prefab header labels may retain an old missing key (and display "Untranslated").
    // The panel owns these four headings; dropdown captions and other dynamic values stay untouched.
    private void RefreshOptionHeadingLabels()
    {
        SetOptionHeading("BackgroundAudioLabel", "ui.option.background_audio.label",
            LocalizedOptionText("백그라운드 재생", "Background Audio", "バックグラウンド再生", "后台播放", "背景播放", "Audio en segundo plano"));
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
        PrepareOptionLocalizedText(label);
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
            RefreshBackgroundAudioLabel();
            RefreshTabLabels();
            RefreshShortcutRows();
        }
    }

    private void EnsureControlContent()
    {
        if (controlContent == null)
        {
            controlContent = FindChildGameObject(ControlContentName);
            if (controlContent == null)
            {
                Debug.LogWarning("[OptionPanelUI] Assign ControlContent or add it to the Option prefab.", this);
                return;
            }
        }

        if (control == null)
            control = controlContent.transform.Find("Control") as RectTransform;
        if (control == null || control.parent != controlContent.transform)
        {
            Debug.LogWarning("[OptionPanelUI] Assign Control or add Control directly under ControlContent.", this);
            return;
        }

        if (shortcutButtons.Count > 0)
        {
            ApplyShortcutResetLayout();
            return;
        }

        TMP_Text fontSource = controlButton != null ? controlButton.GetComponentInChildren<TMP_Text>(true) : null;
        BattleShortcutAction[] actions = (BattleShortcutAction[])System.Enum.GetValues(typeof(BattleShortcutAction));
        float labelWidth = Mathf.Max(0f, shortcutLabelWidth);
        float buttonWidth = Mathf.Max(0f, shortcutButtonWidth);
        float rowHeight = Mathf.Max(1f, shortcutRowHeight);
        float spacing = Mathf.Max(0f, shortcutRowSpacing);
        float rowWidth = labelWidth + spacing + buttonWidth;
        for (int i = 0; i < actions.Length; i++)
        {
            BattleShortcutAction action = actions[i];
            var rowObject = new GameObject(action + "Row", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(control, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(rowWidth, rowHeight);
            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.preferredWidth = rowWidth;
            rowLayout.preferredHeight = rowHeight;
            rowLayout.minWidth = rowWidth;
            rowLayout.minHeight = rowHeight;
            rowLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup rowGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = spacing;
            rowGroup.childAlignment = TextAnchor.MiddleLeft;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = false;

            var nameObject = new GameObject(action + "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObject.transform.SetParent(rowRect, false);
            LayoutElement nameLayout = nameObject.GetComponent<LayoutElement>();
            nameLayout.minWidth = labelWidth;
            nameLayout.preferredWidth = labelWidth;
            nameLayout.flexibleWidth = 0f;
            nameLayout.minHeight = rowHeight;
            nameLayout.preferredHeight = rowHeight;
            nameLayout.flexibleHeight = 0f;
            TextMeshProUGUI nameText = nameObject.GetComponent<TextMeshProUGUI>();
            CopyTextStyle(fontSource, nameText, 24f);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 12f;
            nameText.fontSizeMax = 24f;
            shortcutNameTexts[action] = nameText;

            var buttonObject = new GameObject(action + "KeyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(rowRect, false);
            LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
            buttonLayout.minWidth = buttonWidth;
            buttonLayout.preferredWidth = buttonWidth;
            buttonLayout.minHeight = rowHeight;
            buttonLayout.preferredHeight = rowHeight;
            buttonLayout.flexibleHeight = 0f;
            buttonLayout.flexibleWidth = 0f;
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
            Button button = buttonObject.GetComponent<Button>();
            BattleShortcutAction capturedAction = action;
            button.onClick.AddListener(() => BeginShortcutCapture(capturedAction));
            shortcutButtons[action] = button;

            var keyObject = new GameObject("KeyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            keyObject.transform.SetParent(buttonObject.transform, false);
            RectTransform keyRect = keyObject.GetComponent<RectTransform>();
            keyRect.anchorMin = Vector2.zero;
            keyRect.anchorMax = Vector2.one;
            keyRect.offsetMin = keyRect.offsetMax = Vector2.zero;
            TextMeshProUGUI keyText = keyObject.GetComponent<TextMeshProUGUI>();
            CopyTextStyle(fontSource, keyText, 24f);
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.textWrappingMode = TextWrappingModes.NoWrap;
            keyText.enableAutoSizing = true;
            keyText.fontSizeMin = 12f;
            keyText.fontSizeMax = 24f;
            shortcutKeyTexts[action] = keyText;

            // Reuse the same hover highlight as the other option tabs.
            var hover = rowObject.AddComponent<OptionHoverItem>();
            hover.SetHighlightSize(shortcutHighlightSize);
        }

        var resetObject = new GameObject("ResetShortcuts", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        resetObject.transform.SetParent(controlContent.transform, false);
        resetObject.GetComponent<LayoutElement>().ignoreLayout = true;
        shortcutResetRect = resetObject.GetComponent<RectTransform>();
        ApplyShortcutResetLayout();
        resetObject.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        resetObject.GetComponent<Button>().onClick.AddListener(BattleKeyboardShortcutBindings.ResetAll);
        shortcutResetText = CreateChildText(resetObject.transform, fontSource, "ResetText", 25f);
        resetObject.AddComponent<OptionHoverItem>();
    }

    private void RefreshShortcutHoverSound()
    {
        if (controlContent == null) return;

        string inheritedSfxId = null;
        // Read the configured sound even from inactive option tabs.
        foreach (GameObject content in new[] { gameContent, resolutionContent, soundContent })
        {
            if (content == null || content == controlContent) continue;
            foreach (OptionHoverItem source in content.GetComponentsInChildren<OptionHoverItem>(true))
            {
                if (string.IsNullOrWhiteSpace(source.HoverSfxId)) continue;
                inheritedSfxId = source.HoverSfxId;
                break;
            }
            if (!string.IsNullOrWhiteSpace(inheritedSfxId)) break;
        }

        foreach (OptionHoverItem item in controlContent.GetComponentsInChildren<OptionHoverItem>(true))
            item.SetHoverSfxId(inheritedSfxId);
    }

    private void ApplyShortcutResetLayout()
    {
        if (shortcutResetRect == null) return;
        shortcutResetRect.anchorMin = shortcutResetRect.anchorMax = new Vector2(0.5f, 0f);
        shortcutResetRect.pivot = new Vector2(0.5f, 0f);
        shortcutResetRect.sizeDelta = shortcutResetButtonSize;
        shortcutResetRect.anchoredPosition = shortcutResetButtonPosition;
    }

    private static TMP_Text CreateChildText(Transform parent, TMP_Text source, string name, float size)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        CopyTextStyle(source, text, size);
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static void CopyTextStyle(TMP_Text source, TMP_Text target, float size)
    {
        if (source != null)
        {
            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
            target.color = source.color;
        }
        else target.color = Color.white;
        target.fontSize = size;
        target.raycastTarget = false;
        LocaleTMPFontGuard.Prepare(target);
    }

    private void BeginShortcutCapture(BattleShortcutAction action)
    {
        capturingShortcut = action;
        RefreshShortcutRows();
    }

    private void OnShortcutChanged(BattleShortcutAction _) => RefreshShortcutRows();

    private void RefreshShortcutRows()
    {
        if (shortcutResetText != null)
        {
            shortcutResetText.text = LocalizedOptionText("기본값으로 초기화", "Reset to defaults", "デフォルトに戻す", "恢复默认", "恢復預設", "Restablecer");
            PrepareOptionLocalizedText(shortcutResetText);
        }

        foreach (BattleShortcutAction action in System.Enum.GetValues(typeof(BattleShortcutAction)))
        {
            if (shortcutNameTexts.TryGetValue(action, out TMP_Text nameText))
            {
                nameText.text = GetShortcutActionName(action);
                PrepareOptionLocalizedText(nameText);
            }
            if (!shortcutKeyTexts.TryGetValue(action, out TMP_Text keyText))
                continue;

            bool duplicate = BattleKeyboardShortcutBindings.IsDuplicate(action);
            keyText.text = capturingShortcut == action
                ? LocalizedOptionText("키 입력...", "Press a key...", "キーを入力...", "按下按键...", "按下按鍵...", "Pulsa una tecla...")
                : BattleKeyboardShortcutBindings.GetDisplayName(action);
            if (capturingShortcut == action)
                PrepareOptionLocalizedText(keyText);
            else
                LocaleTMPFontGuard.Prepare(keyText);
            keyText.color = duplicate ? new Color(1f, 0.22f, 0.22f) : Color.white;
            if (shortcutButtons.TryGetValue(action, out Button button))
                button.GetComponent<Image>().color = duplicate
                    ? new Color(0.48f, 0.05f, 0.05f, 0.98f)
                    : new Color(0.15f, 0.15f, 0.18f, 0.95f);
        }
    }

    private void RefreshTabLabels()
    {
        SetButtonText(gameButton, LocalizedOptionText("게임", "Game", "ゲーム", "游戏", "遊戲", "Juego"));
        SetButtonText(graphicButton, LocalizedOptionText("그래픽", "Graphics", "グラフィック", "图形", "圖形", "Gráficos"));
        SetButtonText(audioButton, LocalizedOptionText("오디오", "Audio", "オーディオ", "音频", "音訊", "Audio"));
        SetButtonText(controlButton, LocalizedOptionText("조작", "Controls", "操作", "控制", "控制", "Controles"));
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null) return;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        LocalizedTMPText.ProtectDynamicOwnership(text);
        text.text = value;
        PrepareOptionLocalizedText(text);
    }

    private static string GetShortcutActionName(BattleShortcutAction action)
    {
        switch (action)
        {
            case BattleShortcutAction.Move: return LocalizedOptionText("이동", "Move", "移動", "移动", "移動", "Mover");
            case BattleShortcutAction.Flip: return LocalizedOptionText("뒤집기", "Flip", "反転", "翻转", "翻轉", "Voltear");
            case BattleShortcutAction.Compound: return LocalizedOptionText("조합", "Combine", "合成", "合成", "合成", "Combinar");
            case BattleShortcutAction.Undo: return LocalizedOptionText("실행 취소", "Undo", "元に戻す", "撤销", "復原", "Deshacer");
            case BattleShortcutAction.Redo: return LocalizedOptionText("다시 실행", "Redo", "やり直す", "重做", "重做", "Rehacer");
            case BattleShortcutAction.Skill01: return LocalizedOptionText("스킬 1", "Skill 1", "スキル 1", "技能 1", "技能 1", "Habilidad 1");
            case BattleShortcutAction.Skill02: return LocalizedOptionText("스킬 2", "Skill 2", "スキル 2", "技能 2", "技能 2", "Habilidad 2");
            case BattleShortcutAction.Skill03: return LocalizedOptionText("스킬 3", "Skill 3", "スキル 3", "技能 3", "技能 3", "Habilidad 3");
            case BattleShortcutAction.Skill04: return LocalizedOptionText("스킬 4", "Skill 4", "スキル 4", "技能 4", "技能 4", "Habilidad 4");
            case BattleShortcutAction.CharacterCycle: return LocalizedOptionText("캐릭터 순환", "Cycle character", "キャラ切替", "切换角色", "切換角色", "Cambiar personaje");
            case BattleShortcutAction.TimelineSlot01: return LocalizedOptionText("타임라인 1", "Timeline 1", "タイムライン 1", "时间轴 1", "時間軸 1", "Cronología 1");
            case BattleShortcutAction.TimelineSlot02: return LocalizedOptionText("타임라인 2", "Timeline 2", "タイムライン 2", "时间轴 2", "時間軸 2", "Cronología 2");
            case BattleShortcutAction.TimelineSlot03: return LocalizedOptionText("타임라인 3", "Timeline 3", "タイムライン 3", "时间轴 3", "時間軸 3", "Cronología 3");
            case BattleShortcutAction.TimelineSlot04: return LocalizedOptionText("타임라인 4", "Timeline 4", "タイムライン 4", "时间轴 4", "時間軸 4", "Cronología 4");
            case BattleShortcutAction.TimelineSlot05: return LocalizedOptionText("타임라인 5", "Timeline 5", "タイムライン 5", "时间轴 5", "時間軸 5", "Cronología 5");
            default: return LocalizedOptionText("턴 실행", "Execute turn", "ターン実行", "执行回合", "執行回合", "Ejecutar turno");
        }
    }

    private static void PrepareOptionLocalizedText(TMP_Text text)
    {
        if (text == null) return;
        Locale locale = LocalizationSettings.SelectedLocale;
        if (locale != null)
            LocaleTMPFontGuard.PrepareLocalized(text, locale);
    }

    private static string LocalizedOptionText(string ko, string en, string ja, string zhHans, string zhHant, string es)
    {
        Locale locale = LocalizationSettings.SelectedLocale;
        string code = locale == null ? "ko" : locale.Identifier.Code.ToLowerInvariant();
        if (code.StartsWith("en")) return en;
        if (code.StartsWith("ja")) return ja;
        if (code.Contains("hant") || code == "zh-tw" || code == "zh-hk") return zhHant;
        if (code.StartsWith("zh")) return zhHans;
        if (code.StartsWith("es")) return es;
        return ko;
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

