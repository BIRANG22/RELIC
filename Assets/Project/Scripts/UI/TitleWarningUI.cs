using TMPro;
using UnityEngine;

public class TitleWarningUI : MonoBehaviour
{
    public static TitleWarningUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private RectTransform moveTarget;

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

        // 첫 Show() 요청이 비활성 오브젝트를 활성화하면서 Awake()를 실행할 수 있으므로
        // 여기서는 즉시 비활성화하지 않고 숨김 상태만 초기화합니다.
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
    }

    private void Start()
    {
        // 씬 시작 시 별도의 표시 요청이 없었다면 기존처럼 비활성 상태로 정리합니다.
        // 비활성 오브젝트에서 Show()가 먼저 호출된 경우에는 isShowing이 true이므로 유지됩니다.
        if (!isShowing)
            HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
            messageText.text = message;

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

    public void ShowDefaultMessage()
    {
        Show("아직 준비되지 않았습니다.");
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
        if (!useMoveEffect || moveTarget == null)
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
        if (!useScalePop || moveTarget == null)
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
