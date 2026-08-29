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
    [Header("���� ȭ�� ����")]

    [Tooltip("�� ������Ʈ�� Ȱ��ȭ�Ǿ� �ִ� ���� ��ư ������ �۵����� �ʽ��ϴ�.")]
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


    [Header("Button Sound")]

    [SerializeField]
    private bool playClickSound = true;

    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string clickSoundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float clickSoundVolume = 1f;


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

    [Tooltip("��ư ������ �������� �����Ǿ� �ִ� ������ ���� �����Դϴ�. ��� ������ Protected Background Image �Ǵ� ���� ������Ʈ�� ����մϴ�.")]
    [SerializeField]
    private RectTransform hoverHitArea;

    [Tooltip("��ư �𼭸����� ȣ���� �ݺ����� �ʵ��� ���� ���� ������ �߰��ϴ� ���� �ȼ��Դϴ�.")]
    [SerializeField]
    private float hoverExitPadding = 4f;

    [Tooltip("��ư �̵� �� �߻��ϴ� �������� ������ ��Ż�� �����ϴ� �ð��Դϴ�.")]
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

        // StartImage�� ���� �����ٸ� ���� ���� ����� ���¸� ��� �ʱ�ȭ�մϴ�.
        if (isBlocked && !wasBlocked)
        {
            ForceClearState(false);
        }

        wasBlocked = isBlocked;

        // ��ư ������ �����̴���, �����Ͱ� ������ �� ������ ���� ������
        // ������ ����� �������� ȣ�� ���¸� �����մϴ�.
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
    /// StartImage�� ���� Ȱ��ȭ�Ǿ� �ִ��� Ȯ���մϴ�.
    /// </summary>
    private bool IsBlockedByStartImage()
    {
        return startImageObject != null &&
               startImageObject.activeInHierarchy;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        // StartImage�� ���� ������ ȣ�� �Է��� �����մϴ�.
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

        // ������ ��ư �׷����� �����Ϳ��� �����ٴ� �̺�Ʈ�� �����ϰ�,
        // �����Ͱ� ó�� �������� �� ������ ���� ������ ��� ��쿡�� �����մϴ�.
        Vector2 pointerPosition = eventData != null
            ? eventData.position
            : (Vector2)Input.mousePosition;

        if (hasCachedHoverScreenRect &&
            cachedHoverScreenRect.Contains(pointerPosition))
        {
            pointerOutsideTime = 0f;
            return;
        }

        // ��ư �׷����� �̵��ϸ鼭 �߻��� �������� Exit �̺�Ʈ�����δ�
        // ȣ���� ��� �������� �ʽ��ϴ�. Update���� ���� ���� �ۿ�
        // hoverExitDelay �̻� �ӹ� ��쿡�� ���� ��Ż�� ó���մϴ�.
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
        // StartImage�� ���� ������ Ŭ�� ������ �������� �ʽ��ϴ�.
        if (IsBlockedByStartImage())
        {
            return;
        }

        PlayClickSound();

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


    private void PlayClickSound()
    {
        if (!playClickSound)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(clickSoundId))
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                $"[{nameof(ButtonAnimationCoroutine)}] AudioManager.Instance를 찾지 못했습니다. Object: {name}",
                this
            );
            return;
        }

        AudioManager.Instance.PlaySfx(
            clickSoundId,
            Mathf.Clamp01(clickSoundVolume)
        );
    }


    public void ForceSetClickedState(bool clicked, bool animate)
    {
        isClicked = clicked;
        isPointerInside = false;
        hasCachedHoverScreenRect = false;
        pointerOutsideTime = 0f;

        if (clicked)
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
        // StartImage�� ���� ������ ��� ��ư ������ �����մϴ�.
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

        // ���°� ������ �ٲ� ���� �ڷ�ƾ�� �ߴ��ϰ� �ٽ� ������ �ʽ��ϴ�.
        // ���� ���� �ڷ�ƾ�� �� ������ �ֽ� ��ǥ���� ���󰩴ϴ�.
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
            // ���� �� StartImage�� �ٽ� ������ ��� ���� ���·� �����մϴ�.
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
                // �������� ������ ��쿡�� ��ġ�� �����մϴ�.
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
    /// ��ư �̵� �������� ������ ��쿡�� ��ġ�� �����մϴ�.
    /// </summary>
    private bool UsesButtonPositionAnimation()
    {
        return hoverButtonMoveOffset.sqrMagnitude >
               0.000001f;
    }


    /// <summary>
    /// ���� ��� �̵� �������� ������ ��쿡�� ��ġ�� �����մϴ�.
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