using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 버튼에 마우스를 올리거나 클릭했을 때 위치/크기 애니메이션을 적용하고,
/// 호버/클릭 SFX를 AudioManager의 DB ID로 재생합니다.
/// </summary>
public class ButtonAnimationCoroutine :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("시작 화면 차단")]
    [Tooltip("이 오브젝트가 활성화되어 있는 동안 버튼 입력이 동작하지 않습니다.")]
    [SerializeField] private GameObject startImageObject;

    [Header("Hover - Button Content")]
    [Tooltip("호버/클릭 상태에서 이동하거나 확대할 버튼 영역입니다.")]
    [SerializeField] private RectTransform buttonContent;

    [Tooltip("호버/클릭 상태에서 원래 위치로부터 이동할 값입니다.")]
    [SerializeField] private Vector3 hoverButtonMoveOffset = Vector3.zero;

    [Tooltip("호버/클릭 상태에서 적용할 크기입니다.")]
    [SerializeField] private Vector3 hoverButtonScale = Vector3.one;

    [Tooltip("버튼 위치/크기가 목표 값으로 변경되는 속도입니다.")]
    [SerializeField] private float hoverDuration = 0.2f;

    [Header("Hover Sound")]
    [Tooltip("마우스가 버튼 영역에 들어왔을 때 호버 사운드를 재생합니다.")]
    [SerializeField] private bool playHoverSound = false;

    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string hoverSoundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float hoverSoundVolume = 1f;

    [Header("Click Sound")]
    [Tooltip("버튼을 클릭했을 때 클릭 사운드를 재생합니다.")]
    [SerializeField] private bool playClickSound = true;

    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string clickSoundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float clickSoundVolume = 1f;

    [Header("Click State")]
    [Tooltip("같은 버튼을 다시 클릭하면 선택 상태를 해제합니다.")]
    [SerializeField] private bool toggleClickState = true;

    [Tooltip("포인터가 버튼 밖으로 나가도 클릭 상태를 유지할지 결정합니다.")]
    [SerializeField] private bool keepClickedStateWhenPointerExit = true;

    [Tooltip("클릭된 상태를 지속적으로 유지할지 결정합니다.")]
    [SerializeField] private bool usePersistentClickedState = false;

    [Tooltip("다른 버튼에 호버했을 때 기존 클릭 상태를 해제합니다.")]
    [SerializeField] private bool clearClickedStateWhenAnotherButtonHovered = true;

    [Header("Hover Hit Area")]
    [Tooltip("호버 판정에 사용할 영역입니다. 비워두면 이 스크립트가 붙은 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform hoverHitArea;

    [Tooltip("호버 영역 바깥으로 판정하기 전에 추가로 허용할 픽셀 범위입니다.")]
    [SerializeField] private float hoverExitPadding = 4f;

    [Tooltip("포인터가 호버 영역 밖으로 나간 뒤 실제로 호버를 해제하기까지의 지연 시간입니다.")]
    [SerializeField] private float hoverExitDelay = 0.05f;

    private static ButtonAnimationCoroutine currentClickedButton;

    private Vector3 originButtonPosition;
    private Vector3 originButtonScale;

    private bool isPointerInside;
    private bool isClicked;
    private bool hasCachedOriginValues;
    private bool wasBlocked;
    private bool interactionEnabled = true;

    private Coroutine visualCoroutine;

    private Rect cachedHoverScreenRect;
    private bool hasCachedHoverScreenRect;
    private float pointerOutsideTime;

    private void Awake()
    {
        ResolveHoverHitArea();
        CacheOriginValuesIfNeeded();

        wasBlocked = IsInteractionBlocked();
        ForceClearState(false);
    }

    private void OnEnable()
    {
        ResolveHoverHitArea();
        CacheOriginValuesIfNeeded();

        wasBlocked = IsInteractionBlocked();
        ForceClearState(false);
    }

    private void Update()
    {
        CacheOriginValuesIfNeeded();

        bool isBlocked = IsInteractionBlocked();

        // StartImage가 새로 활성화되면 현재 버튼 상태를 즉시 초기화합니다.
        if (isBlocked && !wasBlocked)
        {
            ForceClearState(false);
        }

        wasBlocked = isBlocked;

        // PointerExit 이벤트만 사용하면 움직이는 UI에서 호버가 잘못 풀릴 수 있으므로
        // 실제 마우스 위치가 저장된 호버 영역 밖에 있는지도 함께 확인합니다.
        if (!isBlocked && isPointerInside && hasCachedHoverScreenRect)
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
    /// 외부 비활성 상태 또는 StartImage 때문에 버튼 입력이 막혀 있는지 확인합니다.
    /// </summary>
    private bool IsInteractionBlocked()
    {
        return !interactionEnabled ||
               (startImageObject != null && startImageObject.activeInHierarchy);
    }

    /// <summary>
    /// 외부 UI에서 Hover/Click 연출과 SFX를 포함한 입력 반응을 켜거나 끕니다.
    /// 비활성화할 때 현재 Hover/Click 상태도 즉시 원래 상태로 되돌립니다.
    /// </summary>
    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;

        if (!interactionEnabled)
        {
            ForceClearState(false);
            wasBlocked = true;
            return;
        }

        wasBlocked = IsInteractionBlocked();
        ForceClearState(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsInteractionBlocked())
        {
            return;
        }

        CacheHoverScreenRect(eventData);
        pointerOutsideTime = 0f;

        if (clearClickedStateWhenAnotherButtonHovered)
        {
            ClearOtherClickedButton();
        }

        // 이미 호버 상태일 때 자식 UI의 이벤트 때문에 PointerEnter가 다시 들어오는 경우
        // 사운드가 중복 재생되지 않도록 실제 진입 순간에만 재생합니다.
        bool wasPointerInside = isPointerInside;
        isPointerInside = true;

        if (!wasPointerInside)
        {
            PlayHoverSound();
        }

        StartVisualAnimationIfNeeded();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsInteractionBlocked())
        {
            return;
        }

        Vector2 pointerPosition = eventData != null
            ? eventData.position
            : (Vector2)Input.mousePosition;

        // 포인터가 실제 호버 판정 영역 안에 있으면 Exit 이벤트가 발생해도 유지합니다.
        if (hasCachedHoverScreenRect &&
            cachedHoverScreenRect.Contains(pointerPosition))
        {
            pointerOutsideTime = 0f;
            return;
        }

        // Update에서 hoverExitDelay만큼 실제 영역 밖에 머물렀는지 확인한 뒤 해제합니다.
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

        hoverHitArea = transform as RectTransform;
    }

    private RectTransform GetHoverHitArea()
    {
        ResolveHoverHitArea();
        return hoverHitArea;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsInteractionBlocked())
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

    /// <summary>
    /// 인스펙터에서 지정한 호버 SFX를 재생합니다.
    /// </summary>
    private void PlayHoverSound()
    {
        PlaySound(
            playHoverSound,
            hoverSoundId,
            hoverSoundVolume
        );
    }

    /// <summary>
    /// 인스펙터에서 지정한 클릭 SFX를 재생합니다.
    /// </summary>
    private void PlayClickSound()
    {
        PlaySound(
            playClickSound,
            clickSoundId,
            clickSoundVolume
        );
    }

    /// <summary>
    /// AudioManager에 등록된 SFX ID를 지정한 볼륨으로 재생합니다.
    /// </summary>
    private void PlaySound(bool enabled, string soundId, float volume)
    {
        if (!enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(soundId))
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
            soundId,
            Mathf.Clamp01(volume)
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

        if (animate && !IsInteractionBlocked())
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

        if (animate && !IsInteractionBlocked())
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
        if (IsInteractionBlocked())
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
        if (hasCachedOriginValues || buttonContent == null)
        {
            return;
        }

        // 선택지 등장 연출 중에는 ButtonContent의 Scale이 0일 수 있습니다.
        // 이 값을 원래 Scale로 저장하면 이후 Hover 상태를 초기화할 때
        // 선택지가 다시 Scale 0으로 돌아가 보이지 않게 됩니다.
        if (buttonContent.localScale.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        CacheOriginValues();
        hasCachedOriginValues = true;
    }

    private void CacheOriginValues()
    {
        if (buttonContent == null)
        {
            return;
        }

        originButtonPosition = buttonContent.anchoredPosition;
        originButtonScale = buttonContent.localScale;
    }

    private void StartVisualAnimationIfNeeded()
    {
        if (IsInteractionBlocked())
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

        // 진행 중인 코루틴이 있다면 중단하지 않고 최신 목표 상태를 계속 따라갑니다.
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
            if (IsInteractionBlocked())
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

            float buttonT = GetFrameT(hoverDuration);

            if (buttonContent != null)
            {
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

        return true;
    }

    private void ApplyVisualStateImmediately()
    {
        if (buttonContent == null)
        {
            return;
        }

        CacheOriginValuesIfNeeded();
        if (!hasCachedOriginValues)
        {
            // 등장 애니메이션이 실제 Scale을 만든 뒤 Update에서 원본값을 캐시합니다.
            // 그 전에는 현재 RectTransform 값을 건드리지 않습니다.
            return;
        }

        if (UsesButtonPositionAnimation())
        {
            buttonContent.anchoredPosition =
                GetTargetButtonPosition();
        }

        buttonContent.localScale =
            GetTargetButtonScale();
    }

    /// <summary>
    /// 버튼 이동 오프셋이 설정된 경우에만 위치 애니메이션을 사용합니다.
    /// </summary>
    private bool UsesButtonPositionAnimation()
    {
        return hoverButtonMoveOffset.sqrMagnitude >
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
}
