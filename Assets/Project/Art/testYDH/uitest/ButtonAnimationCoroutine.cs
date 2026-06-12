using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ButtonAnimationCoroutine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [System.Serializable]
    public class ClickColorTarget
    {
        public Graphic target;
        public Color normalColor = Color.white;
        public Color clickedColor = Color.white;
    }

    [Header("Hover - Button Content")]
    [SerializeField] private RectTransform buttonContent;
    [SerializeField] private Vector3 hoverButtonMoveOffset = new Vector3(20f, 0f, 0f);
    [SerializeField] private Vector3 hoverButtonScale = new Vector3(1.1f, 1.1f, 1f);
    [SerializeField] private float hoverDuration = 0.2f;

    [Header("Hover - Background Rotation")]
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private RectTransform secondBackgroundImage;
    [SerializeField] private float backgroundHoverRotationZOffset = 0f;
    [SerializeField] private float secondBackgroundHoverRotationZOffset = 0f;

    [Header("Click - Color")]
    [SerializeField] private bool changeColorOnClick = true;
    [SerializeField] private ClickColorTarget[] clickColorTargets;

    [Header("Click - Button Content Move")]
    [SerializeField] private bool moveButtonContentOnClick = true;
    [FormerlySerializedAs("clickMoveOffset")]
    [SerializeField] private Vector3 clickButtonMoveOffset = new Vector3(50f, 0f, 0f);
    [FormerlySerializedAs("clickMoveDuration")]
    [SerializeField] private float clickDuration = 0.2f;

    [Header("Click - Background Rotation")]
    [FormerlySerializedAs("clickRotationZOffset")]
    [SerializeField] private float backgroundClickRotationZOffset = 0f;
    [SerializeField] private float secondBackgroundClickRotationZOffset = 0f;

    [Header("Click State Option")]
    [SerializeField] private bool toggleClickState = true;
    [SerializeField] private bool keepHoverStateWhileClicked = true;
    [SerializeField] private bool clearClickedStateWhenAnotherButtonHovered = true;

    private static ButtonAnimationCoroutine currentClickedButton;

    private Vector3 buttonOriginPosition;
    private Vector3 buttonOriginScale;
    private Vector3 backgroundOriginPosition;
    private Vector3 secondBackgroundOriginPosition;
    private Quaternion backgroundOriginRotation;
    private Quaternion secondBackgroundOriginRotation;

    private Coroutine visualCoroutine;
    private bool isClicked;
    private bool isHovering;

    private void Start()
    {
        CacheOriginStates();
        ApplyClickColorState(false);
        SetToCurrentVisualStateImmediately();
    }

    private void OnDisable()
    {
        if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }
    }

    private void OnDestroy()
    {
        if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ClearOtherClickedButton();

        isHovering = true;
        AnimateToCurrentVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        AnimateToCurrentVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ClearOtherClickedButton();

        if (toggleClickState)
        {
            isClicked = !isClicked;
        }
        else
        {
            isClicked = true;
        }

        if (isClicked)
        {
            currentClickedButton = this;
        }
        else if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }

        ApplyClickColorState(isClicked);
        AnimateToCurrentVisualState();
    }

    public void SetClicked(bool clicked)
    {
        if (clicked)
        {
            ClearOtherClickedButton();
            currentClickedButton = this;
        }
        else if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }

        isClicked = clicked;
        ApplyClickColorState(isClicked);
        SetToCurrentVisualStateImmediately();
    }

    public void ClearState()
    {
        ForceClearState(true);
    }

    private void CacheOriginStates()
    {
        if (buttonContent != null)
        {
            buttonOriginPosition = buttonContent.anchoredPosition;
            buttonOriginScale = buttonContent.localScale;
        }

        if (backgroundImage != null)
        {
            backgroundOriginPosition = backgroundImage.anchoredPosition;
            backgroundOriginRotation = backgroundImage.localRotation;
        }

        if (secondBackgroundImage != null)
        {
            secondBackgroundOriginPosition = secondBackgroundImage.anchoredPosition;
            secondBackgroundOriginRotation = secondBackgroundImage.localRotation;
        }
    }

    private void ClearOtherClickedButton()
    {
        if (currentClickedButton == null || currentClickedButton == this)
        {
            return;
        }

        if (!clearClickedStateWhenAnotherButtonHovered)
        {
            return;
        }

        currentClickedButton.ForceClearState(true);
        currentClickedButton = null;
    }

    private void ForceClearState(bool animate)
    {
        isClicked = false;
        isHovering = false;
        ApplyClickColorState(false);

        if (animate)
        {
            AnimateToCurrentVisualState();
        }
        else
        {
            SetToCurrentVisualStateImmediately();
        }
    }

    private void StopVisualAnimation()
    {
        if (visualCoroutine != null)
        {
            StopCoroutine(visualCoroutine);
            visualCoroutine = null;
        }
    }

    private void AnimateToCurrentVisualState()
    {
        StopVisualAnimation();

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        visualCoroutine = StartCoroutine(AnimateVisualState());
    }

    private void SetToCurrentVisualStateImmediately()
    {
        StopVisualAnimation();

        GetCurrentTargets(
            out Vector3 buttonTargetPosition,
            out Vector3 buttonTargetScale,
            out Quaternion backgroundTargetRotation,
            out Quaternion secondBackgroundTargetRotation
        );

        if (buttonContent != null)
        {
            buttonContent.anchoredPosition = buttonTargetPosition;
            buttonContent.localScale = buttonTargetScale;
        }

        if (backgroundImage != null)
        {
            backgroundImage.anchoredPosition = backgroundOriginPosition;
            backgroundImage.localRotation = backgroundTargetRotation;
        }

        if (secondBackgroundImage != null)
        {
            secondBackgroundImage.anchoredPosition = secondBackgroundOriginPosition;
            secondBackgroundImage.localRotation = secondBackgroundTargetRotation;
        }
    }

    private IEnumerator AnimateVisualState()
    {
        GetCurrentTargets(
            out Vector3 buttonEndPosition,
            out Vector3 buttonEndScale,
            out Quaternion backgroundEndRotation,
            out Quaternion secondBackgroundEndRotation
        );

        Vector3 buttonStartPosition = buttonContent != null ? buttonContent.anchoredPosition : Vector3.zero;
        Vector3 buttonStartScale = buttonContent != null ? buttonContent.localScale : Vector3.one;

        Quaternion backgroundStartRotation = backgroundImage != null ? backgroundImage.localRotation : Quaternion.identity;
        Quaternion secondBackgroundStartRotation = secondBackgroundImage != null ? secondBackgroundImage.localRotation : Quaternion.identity;

        float totalDuration = Mathf.Max(0.01f, Mathf.Max(hoverDuration, clickDuration));
        float elapsedTime = 0f;

        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / totalDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            if (buttonContent != null)
            {
                buttonContent.anchoredPosition = Vector3.Lerp(buttonStartPosition, buttonEndPosition, t);
                buttonContent.localScale = Vector3.Lerp(buttonStartScale, buttonEndScale, t);
            }

            if (backgroundImage != null)
            {
                backgroundImage.anchoredPosition = backgroundOriginPosition;
                backgroundImage.localRotation = Quaternion.Slerp(backgroundStartRotation, backgroundEndRotation, t);
            }

            if (secondBackgroundImage != null)
            {
                secondBackgroundImage.anchoredPosition = secondBackgroundOriginPosition;
                secondBackgroundImage.localRotation = Quaternion.Slerp(secondBackgroundStartRotation, secondBackgroundEndRotation, t);
            }

            yield return null;
        }

        if (buttonContent != null)
        {
            buttonContent.anchoredPosition = buttonEndPosition;
            buttonContent.localScale = buttonEndScale;
        }

        if (backgroundImage != null)
        {
            backgroundImage.anchoredPosition = backgroundOriginPosition;
            backgroundImage.localRotation = backgroundEndRotation;
        }

        if (secondBackgroundImage != null)
        {
            secondBackgroundImage.anchoredPosition = secondBackgroundOriginPosition;
            secondBackgroundImage.localRotation = secondBackgroundEndRotation;
        }

        visualCoroutine = null;
    }

    private void GetCurrentTargets(
        out Vector3 buttonTargetPosition,
        out Vector3 buttonTargetScale,
        out Quaternion backgroundTargetRotation,
        out Quaternion secondBackgroundTargetRotation)
    {
        bool hoverVisual = ShouldUseHoverVisual();

        buttonTargetPosition = buttonOriginPosition;
        buttonTargetScale = buttonOriginScale;

        if (hoverVisual)
        {
            buttonTargetPosition += hoverButtonMoveOffset;
            buttonTargetScale = hoverButtonScale;
        }

        if (isClicked && moveButtonContentOnClick)
        {
            buttonTargetPosition += clickButtonMoveOffset;
        }

        backgroundTargetRotation = backgroundOriginRotation;
        secondBackgroundTargetRotation = secondBackgroundOriginRotation;

        if (hoverVisual)
        {
            backgroundTargetRotation *= Quaternion.Euler(0f, 0f, backgroundHoverRotationZOffset);
            secondBackgroundTargetRotation *= Quaternion.Euler(0f, 0f, secondBackgroundHoverRotationZOffset);
        }

        if (isClicked)
        {
            backgroundTargetRotation *= Quaternion.Euler(0f, 0f, backgroundClickRotationZOffset);
            secondBackgroundTargetRotation *= Quaternion.Euler(0f, 0f, secondBackgroundClickRotationZOffset);
        }
    }

    private bool ShouldUseHoverVisual()
    {
        return isHovering || (keepHoverStateWhileClicked && isClicked);
    }

    private void ApplyClickColorState(bool clicked)
    {
        if (!changeColorOnClick || clickColorTargets == null)
        {
            return;
        }

        for (int i = 0; i < clickColorTargets.Length; i++)
        {
            ClickColorTarget colorTarget = clickColorTargets[i];

            if (colorTarget == null || colorTarget.target == null)
            {
                continue;
            }

            colorTarget.target.color = clicked ? colorTarget.clickedColor : colorTarget.normalColor;
        }
    }
}
