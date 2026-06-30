using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimationCoroutine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Hover - Button Content")]
    [SerializeField] private RectTransform buttonContent;
    [SerializeField] private Vector3 hoverButtonMoveOffset = Vector3.zero;
    [SerializeField] private Vector3 hoverButtonScale = Vector3.one;
    [SerializeField] private float hoverDuration = 0.2f;

    [Header("Protected Images")]
    [SerializeField] private Graphic protectedBackgroundImage;
    [SerializeField] private Graphic protectedButtonImage;

    [Header("Changing Background")]
    [SerializeField] private RectTransform changingBackgroundImage;
    [SerializeField] private Graphic changingBackgroundGraphic;
    [SerializeField] private Vector3 hoverChangingBackgroundMoveOffset = Vector3.zero;
    [SerializeField] private float hoverChangingBackgroundRotationZOffset = 0f;
    [SerializeField] private float changingBackgroundDuration = 0.2f;

    [Header("Changing Background Color")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color clickedColor = Color.white;

    [Header("Click State")]
    [SerializeField] private bool toggleClickState = true;
    [SerializeField] private bool keepClickedStateWhenPointerExit = true;
    [SerializeField] private bool usePersistentClickedState = false;
    [SerializeField] private bool clearClickedStateWhenAnotherButtonHovered = true;

    private static ButtonAnimationCoroutine currentClickedButton;

    private Vector3 originButtonPosition;
    private Vector3 originButtonScale;
    private Vector3 originChangingBackgroundPosition;
    private Quaternion originChangingBackgroundRotation;

    private bool isPointerInside;
    private bool isClicked;
    private bool hasCachedOriginValues;

    private Coroutine visualCoroutine;

    private void Awake()
    {
        CacheOriginValuesIfNeeded();
        ForceClearState(false);
    }

    private void OnEnable()
    {
        CacheOriginValuesIfNeeded();
        ForceClearState(false);
    }

    private void OnDisable()
    {
        ForceClearState(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (clearClickedStateWhenAnotherButtonHovered)
        {
            ClearOtherClickedButton();
        }

        isPointerInside = true;
        AnimateToCurrentVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;

        if (isClicked && keepClickedStateWhenPointerExit && usePersistentClickedState)
        {
            ApplyVisualStateImmediately();
            return;
        }

        isClicked = false;

        if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }

        AnimateToCurrentVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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
            if (currentClickedButton != null && currentClickedButton != this)
            {
                currentClickedButton.ForceClearState(false);
            }

            currentClickedButton = this;
        }
        else if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }

        AnimateToCurrentVisualState();
    }

    public void ForceClearState(bool animate)
    {
        isClicked = false;
        isPointerInside = false;

        if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }

        if (animate)
        {
            AnimateToCurrentVisualState();
        }
        else
        {
            StopVisualAnimation();
            ApplyVisualStateImmediately();
        }
    }

    private void ClearOtherClickedButton()
    {
        if (currentClickedButton == null || currentClickedButton == this)
        {
            return;
        }

        currentClickedButton.ForceClearState(true);
    }

    private void CacheOriginValuesIfNeeded()
    {
        if (hasCachedOriginValues)
            return;

        CacheOriginValues();
        hasCachedOriginValues = true;
    }

    private void CacheOriginValues()
    {
        if (buttonContent != null)
        {
            originButtonPosition = buttonContent.anchoredPosition;
            originButtonScale = buttonContent.localScale;
        }

        if (changingBackgroundImage != null)
        {
            originChangingBackgroundPosition = changingBackgroundImage.anchoredPosition;
            originChangingBackgroundRotation = changingBackgroundImage.localRotation;
        }

        if (changingBackgroundGraphic == null && changingBackgroundImage != null)
        {
            changingBackgroundGraphic = changingBackgroundImage.GetComponent<Graphic>();
        }
    }

    private void AnimateToCurrentVisualState()
    {
        StopVisualAnimation();

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ApplyVisualStateImmediately();
            return;
        }

        visualCoroutine = StartCoroutine(AnimateVisualState());
    }

    private void StopVisualAnimation()
    {
        if (visualCoroutine != null)
        {
            StopCoroutine(visualCoroutine);
            visualCoroutine = null;
        }
    }

    private IEnumerator AnimateVisualState()
    {
        Vector3 startButtonPosition = buttonContent != null ? buttonContent.anchoredPosition : Vector3.zero;
        Vector3 startButtonScale = buttonContent != null ? buttonContent.localScale : Vector3.one;
        Vector3 startChangingBackgroundPosition = changingBackgroundImage != null ? changingBackgroundImage.anchoredPosition : Vector3.zero;
        Quaternion startChangingBackgroundRotation = changingBackgroundImage != null ? changingBackgroundImage.localRotation : Quaternion.identity;
        Color startColor = changingBackgroundGraphic != null ? changingBackgroundGraphic.color : Color.white;

        Vector3 targetButtonPosition = GetTargetButtonPosition();
        Vector3 targetButtonScale = GetTargetButtonScale();
        Vector3 targetChangingBackgroundPosition = GetTargetChangingBackgroundPosition();
        Quaternion targetChangingBackgroundRotation = GetTargetChangingBackgroundRotation();
        Color targetColor = GetTargetChangingBackgroundColor();

        float safeDuration = Mathf.Max(0.01f, Mathf.Max(hoverDuration, changingBackgroundDuration));
        float elapsedTime = 0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / safeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            if (buttonContent != null)
            {
                buttonContent.anchoredPosition = Vector3.Lerp(startButtonPosition, targetButtonPosition, t);
                buttonContent.localScale = Vector3.Lerp(startButtonScale, targetButtonScale, t);
            }

            if (changingBackgroundImage != null)
            {
                changingBackgroundImage.anchoredPosition = Vector3.Lerp(startChangingBackgroundPosition, targetChangingBackgroundPosition, t);
                changingBackgroundImage.localRotation = Quaternion.Slerp(startChangingBackgroundRotation, targetChangingBackgroundRotation, t);
            }

            if (changingBackgroundGraphic != null)
            {
                changingBackgroundGraphic.color = Color.Lerp(startColor, targetColor, t);
            }

            yield return null;
        }

        ApplyVisualStateImmediately();
        visualCoroutine = null;
    }

    private void ApplyVisualStateImmediately()
    {
        if (buttonContent != null)
        {
            buttonContent.anchoredPosition = GetTargetButtonPosition();
            buttonContent.localScale = GetTargetButtonScale();
        }

        if (changingBackgroundImage != null)
        {
            changingBackgroundImage.anchoredPosition = GetTargetChangingBackgroundPosition();
            changingBackgroundImage.localRotation = GetTargetChangingBackgroundRotation();
        }

        if (changingBackgroundGraphic != null)
        {
            changingBackgroundGraphic.color = GetTargetChangingBackgroundColor();
        }
    }

    private Vector3 GetTargetButtonPosition()
    {
        if (isPointerInside || isClicked)
        {
            return originButtonPosition + hoverButtonMoveOffset;
        }

        return originButtonPosition;
    }

    private Vector3 GetTargetButtonScale()
    {
        if (isPointerInside || isClicked)
        {
            return hoverButtonScale;
        }

        return originButtonScale;
    }

    private Vector3 GetTargetChangingBackgroundPosition()
    {
        if (isPointerInside || isClicked)
        {
            return originChangingBackgroundPosition + hoverChangingBackgroundMoveOffset;
        }

        return originChangingBackgroundPosition;
    }

    private Quaternion GetTargetChangingBackgroundRotation()
    {
        if (isPointerInside)
        {
            return originChangingBackgroundRotation * Quaternion.Euler(0f, 0f, hoverChangingBackgroundRotationZOffset);
        }

        return originChangingBackgroundRotation;
    }

    private Color GetTargetChangingBackgroundColor()
    {
        if (isClicked)
        {
            return clickedColor;
        }

        if (isPointerInside)
        {
            return hoverColor;
        }

        return normalColor;
    }
}
