using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 인트로 화면에서 일정 시간 동안 누르고 있으면 전체 인트로를 스킵하는 버튼입니다.
/// 홀드 중에는 Fill Image의 fillAmount로 진행도를 표시할 수 있습니다.
/// </summary>
public class IntroHoldSkipButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private static readonly HashSet<IntroHoldSkipButton> ActiveButtons = new HashSet<IntroHoldSkipButton>();

    [Header("인트로 스킵")]
    [Tooltip("비워두면 IntroSequenceController.Instance를 사용합니다.")]
    [SerializeField] private IntroSequenceController introSequenceController;

    [Min(0.1f)]
    [Tooltip("스킵이 실행될 때까지 버튼을 누르고 있어야 하는 시간입니다.")]
    [SerializeField] private float holdDuration = 1.5f;

    [Header("홀드 진행 표시")]
    [Tooltip("홀드 진행도를 표시할 Image입니다. Image Type을 Filled로 설정하면 fillAmount가 0에서 1까지 증가합니다.")]
    [SerializeField] private Image fillImage;

    [Tooltip("버튼을 놓거나 영역 밖으로 나갔을 때 진행도를 즉시 0으로 되돌립니다.")]
    [SerializeField] private bool resetProgressOnCancel = true;

    private RectTransform cachedRectTransform;
    private Canvas cachedCanvas;
    private bool isPointerHolding;
    private bool isSpaceHolding;
    private bool skipTriggered;
    private float holdElapsed;

    private void Awake()
    {
        CacheReferences();
        SetProgress(0f);
    }

    private void OnEnable()
    {
        CacheReferences();
        ActiveButtons.Add(this);
        ResetHold();
    }

    private void OnDisable()
    {
        ActiveButtons.Remove(this);
        ResetHold();
    }

    private void Update()
    {
        UpdateSpaceHoldState();

        if ((!isPointerHolding && !isSpaceHolding) || skipTriggered)
            return;

        IntroSequenceController controller = ResolveController();
        if (controller == null || !controller.IsPlaying)
        {
            ResetHold();
            return;
        }

        holdElapsed += Time.unscaledDeltaTime;
        float duration = Mathf.Max(0.1f, holdDuration);
        float progress = Mathf.Clamp01(holdElapsed / duration);
        SetProgress(progress);

        if (holdElapsed < duration)
            return;

        skipTriggered = true;
        isPointerHolding = false;
        isSpaceHolding = false;
        SetProgress(1f);
        controller.SkipIntro();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IntroSequenceController controller = ResolveController();
        if (controller == null || !controller.IsPlaying)
            return;

        skipTriggered = false;
        if (!isSpaceHolding)
        {
            holdElapsed = 0f;
            SetProgress(0f);
        }

        isPointerHolding = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerHolding = false;
        CancelHoldIfNoInput();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerHolding = false;
        CancelHoldIfNoInput();
    }

    private void UpdateSpaceHoldState()
    {
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        if (spacePressed == isSpaceHolding)
            return;

        isSpaceHolding = spacePressed;

        if (isSpaceHolding)
        {
            if (!isPointerHolding && !skipTriggered)
            {
                holdElapsed = 0f;
                SetProgress(0f);
            }

            return;
        }

        CancelHoldIfNoInput();
    }

    private void CancelHoldIfNoInput()
    {
        if (skipTriggered || isPointerHolding || isSpaceHolding)
            return;

        holdElapsed = 0f;

        if (resetProgressOnCancel)
            SetProgress(0f);
    }

    private void ResetHold()
    {
        isPointerHolding = false;
        isSpaceHolding = false;
        skipTriggered = false;
        holdElapsed = 0f;
        SetProgress(0f);
    }

    private void SetProgress(float value)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(value);
    }

    private IntroSequenceController ResolveController()
    {
        if (introSequenceController != null)
            return introSequenceController;

        return IntroSequenceController.Instance;
    }

    private void CacheReferences()
    {
        if (cachedRectTransform == null)
            cachedRectTransform = transform as RectTransform;

        if (cachedCanvas == null)
            cachedCanvas = GetComponentInParent<Canvas>();
    }

    private Camera ResolveEventCamera()
    {
        if (cachedCanvas == null || cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return cachedCanvas.worldCamera;
    }

    /// <summary>
    /// IntroSequenceController의 일반 마우스 진행 입력과 스킵 버튼 입력이 동시에 처리되지 않도록
    /// 현재 포인터가 활성화된 스킵 버튼 영역 안에 있는지 확인합니다.
    /// </summary>
    public static bool IsPointerOverSkipButton(Vector2 screenPosition)
    {
        ActiveButtons.RemoveWhere(button => button == null);

        foreach (IntroHoldSkipButton button in ActiveButtons)
        {
            if (button == null || !button.isActiveAndEnabled || !button.gameObject.activeInHierarchy)
                continue;

            button.CacheReferences();
            if (button.cachedRectTransform == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                    button.cachedRectTransform,
                    screenPosition,
                    button.ResolveEventCamera()))
            {
                return true;
            }
        }

        return false;
    }
}
