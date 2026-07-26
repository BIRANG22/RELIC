using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimationCoroutine :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("시작 화면 차단")]

    [Tooltip("이 오브젝트가 활성화되어 있는 동안 버튼 연출이 작동하지 않습니다.")]
    [SerializeField] private GameObject startImageObject;


    [Header("Hover - Button Content")]

    [SerializeField] private RectTransform buttonContent;

    [SerializeField]
    private Vector3 hoverButtonMoveOffset = Vector3.zero;

    [SerializeField]
    private Vector3 hoverButtonScale = Vector3.one;

    [SerializeField]
    private float hoverDuration = 0.2f;


    [Header("Protected Images")]

    [SerializeField]
    private Graphic protectedBackgroundImage;

    [SerializeField]
    private Graphic protectedButtonImage;


    [Header("Changing Background")]

    [SerializeField]
    private RectTransform changingBackgroundImage;

    [SerializeField]
    private Graphic changingBackgroundGraphic;

    [SerializeField]
    private Vector3 hoverChangingBackgroundMoveOffset = Vector3.zero;

    [SerializeField]
    private float hoverChangingBackgroundRotationZOffset = 0f;

    [SerializeField]
    private float changingBackgroundDuration = 0.2f;


    [Header("Changing Background Color")]

    [SerializeField]
    private Color normalColor = Color.white;

    [SerializeField]
    private Color hoverColor = Color.white;

    [SerializeField]
    private Color clickedColor = Color.white;


    [Header("Click State")]

    [SerializeField]
    private bool toggleClickState = true;

    [SerializeField]
    private bool keepClickedStateWhenPointerExit = true;

    [SerializeField]
    private bool usePersistentClickedState = false;

    [SerializeField]
    private bool clearClickedStateWhenAnotherButtonHovered = true;


    [Header("Hover Hit Area")]

    [Tooltip("버튼 내용이 움직여도 고정되어 있는 포인터 판정 영역입니다. 비어 있으면 Protected Background Image 또는 현재 오브젝트를 사용합니다.")]
    [SerializeField]
    private RectTransform hoverHitArea;

    [Tooltip("버튼 모서리에서 호버가 반복되지 않도록 고정 판정 영역에 추가하는 여유 픽셀입니다.")]
    [SerializeField]
    private float hoverExitPadding = 4f;

    [Tooltip("버튼 이동 중 발생하는 순간적인 포인터 이탈을 무시하는 시간입니다.")]
    [SerializeField]
    private float hoverExitDelay = 0.05f;


    private static ButtonAnimationCoroutine currentClickedButton;

    private Vector3 originButtonPosition;
    private Vector3 originButtonScale;

    private Vector3 originChangingBackgroundPosition;
    private Quaternion originChangingBackgroundRotation;

    private bool isPointerInside;
    private bool isClicked;
    private bool hasCachedOriginValues;

    private bool wasBlocked;

    private Coroutine visualCoroutine;

    private Rect cachedHoverScreenRect;
    private bool hasCachedHoverScreenRect;
    private float pointerOutsideTime;


    private void Awake()
    {
        ResolveHoverHitArea();
        CacheOriginValuesIfNeeded();

        wasBlocked = IsBlockedByStartImage();

        ForceClearState(false);
    }


    private void OnEnable()
    {
        ResolveHoverHitArea();
        CacheOriginValuesIfNeeded();

        wasBlocked = IsBlockedByStartImage();

        ForceClearState(false);
    }


    private void Update()
    {
        bool isBlocked = IsBlockedByStartImage();

        // StartImage가 새로 켜졌다면 진행 중인 연출과 상태를 즉시 초기화합니다.
        if (isBlocked && !wasBlocked)
        {
            ForceClearState(false);
        }

        wasBlocked = isBlocked;

        // 버튼 내용이 움직이더라도, 포인터가 들어왔을 때 저장한 고정 영역을
        // 완전히 벗어나기 전까지는 호버 상태를 유지합니다.
        if (!isBlocked &&
            isPointerInside &&
            hasCachedHoverScreenRect)
        {
            if (cachedHoverScreenRect.Contains(Input.mousePosition))
            {
                pointerOutsideTime = 0f;
            }
            else
            {
                pointerOutsideTime += Time.unscaledDeltaTime;

                if (pointerOutsideTime >= Mathf.Max(0f, hoverExitDelay))
                {
                    ApplyPointerExitState();
                }
            }
        }
    }


    private void OnDisable()
    {
        ForceClearState(false);
    }


    /// <summary>
    /// StartImage가 현재 활성화되어 있는지 확인합니다.
    /// </summary>
    private bool IsBlockedByStartImage()
    {
        return startImageObject != null &&
               startImageObject.activeInHierarchy;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        // StartImage가 켜져 있으면 호버 입력을 무시합니다.
        if (IsBlockedByStartImage())
        {
            return;
        }

        CacheHoverScreenRect(eventData);
        pointerOutsideTime = 0f;

        if (clearClickedStateWhenAnotherButtonHovered)
        {
            ClearOtherClickedButton();
        }

        isPointerInside = true;

        StartVisualAnimationIfNeeded();
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsBlockedByStartImage())
        {
            return;
        }

        // 움직인 버튼 그래픽이 포인터에서 빠졌다는 이벤트는 무시하고,
        // 포인터가 처음 진입했을 때 저장한 고정 영역을 벗어난 경우에만 해제합니다.
        Vector2 pointerPosition = eventData != null
            ? eventData.position
            : (Vector2)Input.mousePosition;

        if (hasCachedHoverScreenRect &&
            cachedHoverScreenRect.Contains(pointerPosition))
        {
            pointerOutsideTime = 0f;
            return;
        }

        // 버튼 그래픽이 이동하면서 발생한 순간적인 Exit 이벤트만으로는
        // 호버를 즉시 해제하지 않습니다. Update에서 고정 영역 밖에
        // hoverExitDelay 이상 머문 경우에만 실제 이탈로 처리합니다.
        pointerOutsideTime = 0f;
    }


    private void CacheHoverScreenRect(PointerEventData eventData)
    {
        RectTransform hitArea = GetHoverHitArea();

        if (hitArea == null)
        {
            hasCachedHoverScreenRect = false;
            return;
        }

        Camera eventCamera = eventData != null
            ? eventData.enterEventCamera
            : null;

        Vector3[] corners = new Vector3[4];
        hitArea.GetWorldCorners(corners);

        Vector2 first = RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            corners[0]
        );

        float minX = first.x;
        float maxX = first.x;
        float minY = first.y;
        float maxY = first.y;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(
                    eventCamera,
                    corners[i]
                );

            minX = Mathf.Min(minX, screenPoint.x);
            maxX = Mathf.Max(maxX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        float padding = Mathf.Max(0f, hoverExitPadding);

        cachedHoverScreenRect = Rect.MinMaxRect(
            minX - padding,
            minY - padding,
            maxX + padding,
            maxY + padding
        );

        hasCachedHoverScreenRect = true;
    }


    private void ApplyPointerExitState()
    {
        isPointerInside = false;
        hasCachedHoverScreenRect = false;
        pointerOutsideTime = 0f;

        if (isClicked &&
            keepClickedStateWhenPointerExit &&
            usePersistentClickedState)
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


    private void ResolveHoverHitArea()
    {
        if (hoverHitArea != null)
        {
            return;
        }

        if (protectedBackgroundImage != null)
        {
            hoverHitArea = protectedBackgroundImage.rectTransform;
            return;
        }

        hoverHitArea = transform as RectTransform;
    }


    private RectTransform GetHoverHitArea()
    {
        ResolveHoverHitArea();
        return hoverHitArea;
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        // StartImage가 켜져 있으면 클릭 연출을 실행하지 않습니다.
        if (IsBlockedByStartImage())
        {
            return;
        }

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
            if (currentClickedButton != null &&
                currentClickedButton != this)
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
        hasCachedHoverScreenRect = false;
        pointerOutsideTime = 0f;

        if (currentClickedButton == this)
        {
            currentClickedButton = null;
        }

        if (animate && !IsBlockedByStartImage())
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
        if (IsBlockedByStartImage())
        {
            return;
        }

        if (currentClickedButton == null ||
            currentClickedButton == this)
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
            originButtonPosition =
                buttonContent.anchoredPosition;

            originButtonScale =
                buttonContent.localScale;
        }

        if (changingBackgroundImage != null)
        {
            originChangingBackgroundPosition =
                changingBackgroundImage.anchoredPosition;

            originChangingBackgroundRotation =
                changingBackgroundImage.localRotation;
        }

        if (changingBackgroundGraphic == null &&
            changingBackgroundImage != null)
        {
            changingBackgroundGraphic =
                changingBackgroundImage.GetComponent<Graphic>();
        }
    }


    private void StartVisualAnimationIfNeeded()
    {
        // StartImage가 켜져 있으면 모든 버튼 연출을 차단합니다.
        if (IsBlockedByStartImage())
        {
            StopVisualAnimation();
            ApplyVisualStateImmediately();

            return;
        }

        if (!isActiveAndEnabled ||
            !gameObject.activeInHierarchy)
        {
            ApplyVisualStateImmediately();

            return;
        }

        // 상태가 빠르게 바뀌어도 기존 코루틴을 중단하고 다시 만들지 않습니다.
        // 실행 중인 코루틴이 매 프레임 최신 목표값을 따라갑니다.
        if (visualCoroutine == null)
        {
            visualCoroutine =
                StartCoroutine(AnimateVisualState());
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
            // 연출 중 StartImage가 다시 켜지면 즉시 원래 상태로 복구합니다.
            if (IsBlockedByStartImage())
            {
                isPointerInside = false;
                isClicked = false;

                if (currentClickedButton == this)
                {
                    currentClickedButton = null;
                }

                visualCoroutine = null;

                ApplyVisualStateImmediately();

                yield break;
            }

            float buttonT =
                GetFrameT(hoverDuration);

            float backgroundT =
                GetFrameT(changingBackgroundDuration);


            if (buttonContent != null)
            {
                // 오프셋이 설정된 경우에만 위치를 변경합니다.
                if (UsesButtonPositionAnimation())
                {
                    buttonContent.anchoredPosition =
                        Vector3.Lerp(
                            buttonContent.anchoredPosition,
                            GetTargetButtonPosition(),
                            buttonT
                        );
                }

                buttonContent.localScale =
                    Vector3.Lerp(
                        buttonContent.localScale,
                        GetTargetButtonScale(),
                        buttonT
                    );
            }


            if (changingBackgroundImage != null)
            {
                if (UsesChangingBackgroundPositionAnimation())
                {
                    changingBackgroundImage.anchoredPosition =
                        Vector3.Lerp(
                            changingBackgroundImage.anchoredPosition,
                            GetTargetChangingBackgroundPosition(),
                            backgroundT
                        );
                }

                changingBackgroundImage.localRotation =
                    Quaternion.Slerp(
                        changingBackgroundImage.localRotation,
                        GetTargetChangingBackgroundRotation(),
                        backgroundT
                    );
            }


            if (changingBackgroundGraphic != null)
            {
                changingBackgroundGraphic.color =
                    Color.Lerp(
                        changingBackgroundGraphic.color,
                        GetTargetChangingBackgroundColor(),
                        backgroundT
                    );
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

        return 1f -
               Mathf.Exp(
                   -8f *
                   Time.unscaledDeltaTime /
                   duration
               );
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
                (buttonContent.anchoredPosition -
                 (Vector2)GetTargetButtonPosition()).sqrMagnitude >
                positionThreshold * positionThreshold)
            {
                return false;
            }

            if ((buttonContent.localScale -
                 GetTargetButtonScale()).sqrMagnitude >
                scaleThreshold)
            {
                return false;
            }
        }


        if (changingBackgroundImage != null)
        {
            if (UsesChangingBackgroundPositionAnimation() &&
                (changingBackgroundImage.anchoredPosition -
                 (Vector2)GetTargetChangingBackgroundPosition()).sqrMagnitude >
                positionThreshold * positionThreshold)
            {
                return false;
            }

            if (Quaternion.Angle(
                    changingBackgroundImage.localRotation,
                    GetTargetChangingBackgroundRotation()
                ) > rotationThreshold)
            {
                return false;
            }
        }


        if (changingBackgroundGraphic != null)
        {
            Color difference =
                changingBackgroundGraphic.color -
                GetTargetChangingBackgroundColor();

            float colorDifference =
                difference.r * difference.r +
                difference.g * difference.g +
                difference.b * difference.b +
                difference.a * difference.a;

            if (colorDifference > colorThreshold)
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
                buttonContent.anchoredPosition =
                    GetTargetButtonPosition();
            }

            buttonContent.localScale =
                GetTargetButtonScale();
        }


        if (changingBackgroundImage != null)
        {
            if (UsesChangingBackgroundPositionAnimation())
            {
                changingBackgroundImage.anchoredPosition =
                    GetTargetChangingBackgroundPosition();
            }

            changingBackgroundImage.localRotation =
                GetTargetChangingBackgroundRotation();
        }


        if (changingBackgroundGraphic != null)
        {
            changingBackgroundGraphic.color =
                GetTargetChangingBackgroundColor();
        }
    }


    /// <summary>
    /// 버튼 이동 오프셋이 설정된 경우에만 위치를 제어합니다.
    /// </summary>
    private bool UsesButtonPositionAnimation()
    {
        return hoverButtonMoveOffset.sqrMagnitude >
               0.000001f;
    }


    /// <summary>
    /// 변경 배경 이동 오프셋이 설정된 경우에만 위치를 제어합니다.
    /// </summary>
    private bool UsesChangingBackgroundPositionAnimation()
    {
        return hoverChangingBackgroundMoveOffset.sqrMagnitude >
               0.000001f;
    }


    private Vector3 GetTargetButtonPosition()
    {
        if (isPointerInside || isClicked)
        {
            return originButtonPosition +
                   hoverButtonMoveOffset;
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
            return originChangingBackgroundPosition +
                   hoverChangingBackgroundMoveOffset;
        }

        return originChangingBackgroundPosition;
    }


    private Quaternion GetTargetChangingBackgroundRotation()
    {
        if (isPointerInside)
        {
            return originChangingBackgroundRotation *
                   Quaternion.Euler(
                       0f,
                       0f,
                       hoverChangingBackgroundRotationZOffset
                   );
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