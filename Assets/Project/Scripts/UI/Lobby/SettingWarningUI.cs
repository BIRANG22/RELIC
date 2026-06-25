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

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool ShowMessage(string message)
    {
        SettingWarningUI warningUI = Instance;

        if (warningUI == null)
            warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);

        if (warningUI == null)
            return false;

        warningUI.Show(message);
        return true;
    }

    private void OnEnable()
    {
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

    private void SetAlpha(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}