using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingWarningUI : MonoBehaviour
{
    public static SettingWarningUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private RectTransform moveTarget;
    [SerializeField] private Image backgroundImage;

    [Header("Timing")]
    [SerializeField] private float showDuration = 1.2f;
    [SerializeField] private float fadeInTime = 0.08f;
    [SerializeField] private float fadeOutTime = 0.25f;

    [Header("Motion")]
    [SerializeField] private bool useMoveEffect = true;
    [SerializeField] private Vector2 startOffset = new Vector2(0f, -20f);
    [SerializeField] private Vector2 endOffset = Vector2.zero;

    [Header("Scale")]
    [SerializeField] private bool useScalePop = true;
    [SerializeField] private Vector3 startScale = new Vector3(0.92f, 0.92f, 1f);
    [SerializeField] private Vector3 endScale = Vector3.one;

    [Header("Color")]
    [SerializeField] private Color normalBackgroundColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color warningTextColor = Color.white;

    [Header("Sort Order")]
    [SerializeField] private bool forceTopSorting = true;
    [SerializeField] private int topSortingOrder = 9000;
    [SerializeField] private bool setAsLastSiblingOnShow = true;

    [Header("Warning Text - General")]
    [SerializeField] private string dataUnavailableMessage = "데이터를 불러올 수 없습니다.";
    [SerializeField] private string noCharacterSelectedMessage = "선택된 캐릭터가 없습니다.";
    [SerializeField] private string characterDataNotFoundMessage = "캐릭터 데이터를 찾을 수 없습니다.";
    [Tooltip("{0} 위치에 캐릭터 ID가 들어갑니다.")]
    [SerializeField] private string characterDataNotFoundWithIdMessage = "캐릭터 데이터를 찾을 수 없습니다: {0}";
    [SerializeField] private string selectCharacterFirstMessage = "캐릭터를 먼저 선택해야 합니다.";
    [SerializeField] private string noAvailableCharacterMessage = "선택할 수 있는 캐릭터가 없습니다.";
    [SerializeField] private string maxLevelMessage = "최대 레벨입니다.";

    [Header("Warning Text - Skill")]
    [SerializeField] private string skillSlotNotConnectedMessage = "스킬 슬롯이 연결되지 않았습니다.";
    [SerializeField] private string selectSkillSlotFirstMessage = "스킬을 장착할 슬롯을 먼저 선택하세요.";
    [SerializeField] private string noSkillSelectedMessage = "선택된 스킬이 없습니다.";
    [Tooltip("레벨이 부족한 기억을 클릭했을 때 표시할 문구입니다. {0} 위치에 필요한 레벨이 들어갑니다.")]
    [SerializeField] private string skillMemoryUnlockLevelMessage = "캐릭터 Lv.{0}에 해금됩니다.";
    [Tooltip("일반 스킬 레벨 잠금 경고입니다. {0} 위치에 필요한 레벨이 들어갑니다.")]
    [SerializeField] private string skillLockedLevelMessage = "아직 잠겨있는 스킬입니다. 필요 레벨: LV. {0}";
    [SerializeField] private string skillNotAvailableForSlotMessage = "이 슬롯에 장착할 수 없는 스킬입니다.";

    [Header("Warning Text - Fragment")]
    [SerializeField] private string noRuneSelectedMessage = "선택된 파편이 없습니다.";
    [SerializeField] private string runeNotAvailableMessage = "현재 캐릭터가 사용할 수 없는 파편입니다.";
    [SerializeField] private string noEmptyRuneSlotMessage = "비어있는 파편 슬롯이 없습니다.";
    [SerializeField] private string sharedRuneLevelRequiredMessage = "플레이어 또는 계정 레벨 조건이 필요한 공용 파편입니다.";
    [Tooltip("{0} 위치에 필요한 캐릭터 레벨이 들어갑니다.")]
    [SerializeField] private string characterRuneUnlockLevelMessage = "캐릭터 LV.{0}에 해금되는 전용 파편입니다.";
    [SerializeField] private string noRuneToUnequipMessage = "해체할 파편이 없습니다.";
    [SerializeField] private string runeNotEquippedMessage = "장착 중인 파편이 아닙니다.";
    [SerializeField] private string runeSlotNotConnectedMessage = "파편 슬롯이 연결되지 않았습니다.";
    [SerializeField] private string runeSlotLockedMessage = "아직 잠겨있는 파편 슬롯입니다.";
    [Tooltip("{0} 위치에 필요한 캐릭터 레벨이 들어갑니다.")]
    [SerializeField] private string runeSlotUnlockLevelMessage = "캐릭터 Lv.{0}에 해금되는 파편 슬롯입니다.";
    [SerializeField] private string sharedRuneLockedMessage = "아직 잠겨있는 공용 파편입니다.";
    [SerializeField] private string insufficientBlueDustiumMessage = "블루 더스티움이 부족합니다.";

    private Canvas sortingCanvas;
    private Vector2 baseAnchoredPosition;
    private float timer;
    private bool isShowing;

    private void Awake()
    {
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (moveTarget == null)
            moveTarget = transform as RectTransform;

        if (moveTarget != null)
            baseAnchoredPosition = moveTarget.anchoredPosition;

        EnsureTopSorting();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool ShowMessage(string message)
    {
        SettingWarningUI warningUI = ResolveInstance();

        if (warningUI == null)
            return false;

        warningUI.Show(message);
        return true;
    }

    public static string GetSkillMemoryUnlockLevelMessage(int requiredLevel)
    {
        SettingWarningUI warningUI = ResolveInstance();
        string format = warningUI != null
            ? warningUI.skillMemoryUnlockLevelMessage
            : "캐릭터 Lv.{0}에 해금됩니다.";

        if (string.IsNullOrWhiteSpace(format))
            format = "캐릭터 Lv.{0}에 해금됩니다.";

        try
        {
            return string.Format(format, Mathf.Max(0, requiredLevel));
        }
        catch (System.FormatException)
        {
            return format;
        }
    }

    public static string GetRuneSlotUnlockLevelMessage(int requiredLevel)
    {
        SettingWarningUI warningUI = ResolveInstance();
        string format = warningUI != null
            ? warningUI.runeSlotUnlockLevelMessage
            : "캐릭터 Lv.{0}에 해금되는 파편 슬롯입니다.";

        if (string.IsNullOrWhiteSpace(format))
            format = "캐릭터 Lv.{0}에 해금되는 파편 슬롯입니다.";

        // 기존 프리팹/씬에 직렬화되어 남아 있는 예전 용어도 런타임에서 정규화합니다.
        format = format.Replace("룬", "파편");

        try
        {
            return string.Format(format, Mathf.Max(0, requiredLevel));
        }
        catch (System.FormatException)
        {
            return format;
        }
    }

    public static string GetInsufficientBlueDustiumMessage()
    {
        SettingWarningUI warningUI = ResolveInstance();
        return warningUI != null && !string.IsNullOrWhiteSpace(warningUI.insufficientBlueDustiumMessage)
            ? warningUI.insufficientBlueDustiumMessage
            : "블루 더스티움이 부족합니다.";
    }

    private static SettingWarningUI ResolveInstance()
    {
        if (Instance != null)
            return Instance;

        return FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        EnsureTopSorting();

        if (moveTarget != null && !isShowing)
            baseAnchoredPosition = moveTarget.anchoredPosition;
    }

    private void Update()
    {
        if (!isShowing)
            return;

        timer += Time.unscaledDeltaTime;

        SetAlpha(CalculateAlpha(timer));
        UpdateMotion(timer);
        UpdateScale(timer);

        float totalDuration = fadeInTime + showDuration + fadeOutTime;

        if (timer >= totalDuration)
            HideImmediate();
    }

    public void Show(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        message = ResolveInspectorMessage(message);

        if (moveTarget != null)
            baseAnchoredPosition = moveTarget.anchoredPosition - endOffset;

        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = warningTextColor;
        }

        if (backgroundImage != null)
            backgroundImage.color = normalBackgroundColor;

        gameObject.SetActive(true);
        BringToFront();

        timer = 0f;
        isShowing = true;

        SetAlpha(0f);

        if (moveTarget != null)
        {
            if (useMoveEffect)
                moveTarget.anchoredPosition = baseAnchoredPosition + startOffset;

            if (useScalePop)
                moveTarget.localScale = startScale;
        }
    }

    private string ResolveInspectorMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        string trimmed = message.Trim();

        if (trimmed == "블루 더스티움이 부족합니다.")
            return GetConfiguredText(insufficientBlueDustiumMessage, trimmed);

        if (trimmed == "데이터를 불러올 수 없습니다." ||
            trimmed == "데이터를 사용할 수 없습니다." ||
            trimmed == "DataManager가 없습니다.")
            return GetConfiguredText(dataUnavailableMessage, trimmed);

        if (trimmed == "선택된 캐릭터가 없습니다.")
            return GetConfiguredText(noCharacterSelectedMessage, trimmed);

        if (trimmed == "캐릭터 데이터를 찾을 수 없습니다." ||
            trimmed.StartsWith("캐릭터 마스터 데이터를 찾을 수 없습니다:") ||
            trimmed.StartsWith("캐릭터 런타임 데이터를 찾을 수 없습니다:"))
            return GetConfiguredText(characterDataNotFoundMessage, trimmed);

        if (trimmed.StartsWith("캐릭터 데이터를 찾을 수 없습니다:"))
        {
            string value = trimmed.Substring("캐릭터 데이터를 찾을 수 없습니다:".Length).Trim();
            return FormatConfiguredText(characterDataNotFoundWithIdMessage, trimmed, value);
        }

        if (trimmed == "캐릭터를 먼저 선택해야 합니다.")
            return GetConfiguredText(selectCharacterFirstMessage, trimmed);

        if (trimmed == "선택할 수 있는 캐릭터가 없습니다.")
            return GetConfiguredText(noAvailableCharacterMessage, trimmed);

        if (trimmed == "최대 레벨입니다.")
            return GetConfiguredText(maxLevelMessage, trimmed);

        if (trimmed == "스킬 슬롯이 연결되지 않았습니다.")
            return GetConfiguredText(skillSlotNotConnectedMessage, trimmed);

        if (trimmed == "스킬을 장착할 슬롯을 먼저 선택하세요.")
            return GetConfiguredText(selectSkillSlotFirstMessage, trimmed);

        if (trimmed == "선택된 스킬이 없습니다.")
            return GetConfiguredText(noSkillSelectedMessage, trimmed);

        if (trimmed.StartsWith("아직 잠겨있는 스킬입니다. 필요 레벨:"))
        {
            int requiredLevel = ExtractLastInteger(trimmed);
            return FormatConfiguredText(skillLockedLevelMessage, trimmed, requiredLevel);
        }

        if (trimmed == "이 슬롯에 장착할 수 없는 스킬입니다.")
            return GetConfiguredText(skillNotAvailableForSlotMessage, trimmed);

        if (trimmed == "선택된 파편이 없습니다.")
            return GetConfiguredText(noRuneSelectedMessage, trimmed);

        if (trimmed == "현재 캐릭터가 사용할 수 없는 파편입니다.")
            return GetConfiguredText(runeNotAvailableMessage, trimmed);

        if (trimmed == "비어있는 파편 슬롯이 없습니다.")
            return GetConfiguredText(noEmptyRuneSlotMessage, trimmed);

        if (trimmed == "플레이어 또는 계정 레벨 조건이 필요한 공용 파편입니다.")
            return GetConfiguredText(sharedRuneLevelRequiredMessage, trimmed);

        if (trimmed.StartsWith("캐릭터 LV.") && trimmed.Contains("해금되는 전용룬"))
        {
            int requiredLevel = ExtractLastInteger(trimmed);
            return FormatConfiguredText(characterRuneUnlockLevelMessage, trimmed, requiredLevel);
        }

        if (trimmed == "해체할 파편이 없습니다.")
            return GetConfiguredText(noRuneToUnequipMessage, trimmed);

        if (trimmed == "장착 중인 파편이 아닙니다.")
            return GetConfiguredText(runeNotEquippedMessage, trimmed);

        if (trimmed == "파편 슬롯이 연결되지 않았습니다.")
            return GetConfiguredText(runeSlotNotConnectedMessage, trimmed);

        if (trimmed.StartsWith("캐릭터 Lv.") && trimmed.Contains("해금되는 룬 슬롯"))
        {
            int requiredLevel = ExtractLastInteger(trimmed);
            return FormatConfiguredText(runeSlotUnlockLevelMessage, trimmed, requiredLevel);
        }

        if (trimmed == "아직 잠겨있는 파편 슬롯입니다.")
            return GetConfiguredText(runeSlotLockedMessage, trimmed);

        if (trimmed == "아직 잠겨있는 공용 파편입니다.")
            return GetConfiguredText(sharedRuneLockedMessage, trimmed);

        return message;
    }

    private static string NormalizeLegacyTermText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        string normalized = text.Replace("룬", "파편");
        normalized = normalized.Replace("해제할 파편이 없습니다.", "해체할 파편이 없습니다.");
        return normalized;
    }

    private static string GetConfiguredText(string configured, string fallback)
    {
        string text = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
        return NormalizeLegacyTermText(text);
    }

    private static string FormatConfiguredText(string configured, string fallback, params object[] args)
    {
        string format = NormalizeLegacyTermText(string.IsNullOrWhiteSpace(configured) ? fallback : configured);

        try
        {
            return string.Format(format, args);
        }
        catch (System.FormatException)
        {
            return format;
        }
    }

    private static int ExtractLastInteger(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int value = 0;
        int multiplier = 1;
        bool foundDigit = false;

        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];
            if (char.IsDigit(c))
            {
                foundDigit = true;
                value += (c - '0') * multiplier;
                multiplier *= 10;
                continue;
            }

            if (foundDigit)
                break;
        }

        return value;
    }

    public void HideImmediate()
    {
        isShowing = false;
        timer = 0f;

        SetAlpha(0f);

        if (moveTarget != null)
        {
            if (useMoveEffect)
                moveTarget.anchoredPosition = baseAnchoredPosition + endOffset;

            if (useScalePop)
                moveTarget.localScale = endScale;
        }

        gameObject.SetActive(false);
    }

    private float CalculateAlpha(float time)
    {
        if (time <= fadeInTime)
        {
            if (fadeInTime <= 0f)
                return 1f;

            return Mathf.Clamp01(time / fadeInTime);
        }

        if (time <= fadeInTime + showDuration)
            return 1f;

        float fadeOutElapsed = time - fadeInTime - showDuration;

        if (fadeOutTime <= 0f)
            return 0f;

        return 1f - Mathf.Clamp01(fadeOutElapsed / fadeOutTime);
    }

    private void UpdateMotion(float time)
    {
        if (!useMoveEffect)
            return;

        if (moveTarget == null)
            return;

        float t = fadeInTime <= 0f ? 1f : Mathf.Clamp01(time / fadeInTime);
        t = EaseOutBack(t);

        moveTarget.anchoredPosition = Vector2.LerpUnclamped(
            baseAnchoredPosition + startOffset,
            baseAnchoredPosition + endOffset,
            t
        );
    }

    private void UpdateScale(float time)
    {
        if (!useScalePop)
            return;

        if (moveTarget == null)
            return;

        float t = fadeInTime <= 0f ? 1f : Mathf.Clamp01(time / fadeInTime);
        t = EaseOutBack(t);

        moveTarget.localScale = Vector3.LerpUnclamped(
            startScale,
            endScale,
            t
        );
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void BringToFront()
    {
        EnsureTopSorting();

        if (setAsLastSiblingOnShow)
            transform.SetAsLastSibling();
    }

    private void EnsureTopSorting()
    {
        if (!forceTopSorting)
            return;

        if (sortingCanvas == null)
            sortingCanvas = GetComponent<Canvas>();

        if (sortingCanvas == null)
            sortingCanvas = gameObject.AddComponent<Canvas>();

        sortingCanvas.overrideSorting = true;
        sortingCanvas.sortingOrder = topSortingOrder;
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}