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
        StartVisualAnimationIfNeeded();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;

        if (isClicked && keepClickedStateWhenPointerExit && usePersistentClickedState)
        {
            StartVisualAnimationIfNeeded();
            return;
        }

        isClicked = false;

        if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }

        StartVisualAnimationIfNeeded();
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

        StartVisualAnimationIfNeeded();
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
            StartVisualAnimationIfNeeded();
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
        {
            return;
        }

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

    private void StartVisualAnimationIfNeeded()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ApplyVisualStateImmediately();
            return;
        }

        // 상태가 빠르게 바뀌어도 기존 코루틴을 중단하고 다시 만들지 않는다.
        // 실행 중인 하나의 코루틴이 매 프레임 최신 목표 위치를 따라가므로
        // 호버를 빠르게 반복해도 중간 위치가 새로운 원점처럼 누적되지 않는다.
        if (visualCoroutine == null)
        {
            visualCoroutine = StartCoroutine(AnimateVisualState());
        }
    }

    private void StopVisualAnimation()
    {
        if (visualCoroutine == null)
        {
            return;
        }

        StopCoroutine(visualCoroutine);
        visualCoroutine = null;
    }

    private IEnumerator AnimateVisualState()
    {
        while (true)
        {
            float buttonT = GetFrameT(hoverDuration);
            float backgroundT = GetFrameT(changingBackgroundDuration);

            if (buttonContent != null)
            {
                // 이동 오프셋이 0인 버튼은 외부 패널 애니메이션이 정한 위치를 유지합니다.
                // 싱글/멀티 패널처럼 펼침 스크립트가 위치를 제어하는 경우
                // 이 스크립트가 저장해 둔 -500 위치로 되돌리지 않도록 합니다.
                if (UsesButtonPositionAnimation())
                {
                    buttonContent.anchoredPosition = Vector3.Lerp(
                        buttonContent.anchoredPosition,
                        GetTargetButtonPosition(),
                        buttonT);
                }

                buttonContent.localScale = Vector3.Lerp(
                    buttonContent.localScale,
                    GetTargetButtonScale(),
                    buttonT);
            }

            if (changingBackgroundImage != null)
            {
                if (UsesChangingBackgroundPositionAnimation())
                {
                    changingBackgroundImage.anchoredPosition = Vector3.Lerp(
                        changingBackgroundImage.anchoredPosition,
                        GetTargetChangingBackgroundPosition(),
                        backgroundT);
                }

                changingBackgroundImage.localRotation = Quaternion.Slerp(
                    changingBackgroundImage.localRotation,
                    GetTargetChangingBackgroundRotation(),
                    backgroundT);
            }

            if (changingBackgroundGraphic != null)
            {
                changingBackgroundGraphic.color = Color.Lerp(
                    changingBackgroundGraphic.color,
                    GetTargetChangingBackgroundColor(),
                    backgroundT);
            }

            if (HasReachedCurrentTarget())
            {
                ApplyVisualStateImmediately();
                visualCoroutine = null;
                yield break;
            }

            yield return null;
        }
    }

    private static float GetFrameT(float duration)
    {
        if (duration <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime / duration);
    }

    private bool HasReachedCurrentTarget()
    {
        const float positionThreshold = 0.01f;
        const float scaleThreshold = 0.0001f;
        const float rotationThreshold = 0.05f;
        const float colorThreshold = 0.0001f;

        if (buttonContent != null)
        {
            if (UsesButtonPositionAnimation() &&
                (buttonContent.anchoredPosition - (Vector2)GetTargetButtonPosition()).sqrMagnitude > positionThreshold * positionThreshold)
            {
                return false;
            }

            if ((buttonContent.localScale - GetTargetButtonScale()).sqrMagnitude > scaleThreshold)
            {
                return false;
            }
        }

        if (changingBackgroundImage != null)
        {
            if (UsesChangingBackgroundPositionAnimation() &&
                (changingBackgroundImage.anchoredPosition - (Vector2)GetTargetChangingBackgroundPosition()).sqrMagnitude > positionThreshold * positionThreshold)
            {
                return false;
            }

            if (Quaternion.Angle(changingBackgroundImage.localRotation, GetTargetChangingBackgroundRotation()) > rotationThreshold)
            {
                return false;
            }
        }

        if (changingBackgroundGraphic != null)
        {
            Color difference = changingBackgroundGraphic.color - GetTargetChangingBackgroundColor();
            if (difference.r * difference.r + difference.g * difference.g + difference.b * difference.b + difference.a * difference.a > colorThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyVisualStateImmediately()
    {
        if (buttonContent != null)
        {
            if (UsesButtonPositionAnimation())
            {
                buttonContent.anchoredPosition = GetTargetButtonPosition();
            }

            buttonContent.localScale = GetTargetButtonScale();
        }

        if (changingBackgroundImage != null)
        {
            if (UsesChangingBackgroundPositionAnimation())
            {
                changingBackgroundImage.anchoredPosition = GetTargetChangingBackgroundPosition();
            }

            changingBackgroundImage.localRotation = GetTargetChangingBackgroundRotation();
        }

        if (changingBackgroundGraphic != null)
        {
            changingBackgroundGraphic.color = GetTargetChangingBackgroundColor();
        }
    }


    /// <summary>
    /// 버튼 이동 오프셋이 실제로 설정된 경우에만 버튼 위치를 제어합니다.
    /// 값이 0이면 다른 패널 애니메이션이 관리하는 위치를 건드리지 않습니다.
    /// </summary>
    private bool UsesButtonPositionAnimation()
    {
        return hoverButtonMoveOffset.sqrMagnitude > 0.000001f;
    }

    /// <summary>
    /// 변경 배경의 이동 오프셋이 설정된 경우에만 위치를 제어합니다.
    /// </summary>
    private bool UsesChangingBackgroundPositionAnimation()
    {
        return hoverChangingBackgroundMoveOffset.sqrMagnitude > 0.000001f;
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
