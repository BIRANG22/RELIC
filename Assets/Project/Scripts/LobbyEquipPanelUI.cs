using System.Collections;
using UnityEngine;

/// <summary>
/// 로비의 장비 관리 Equip_panel 전용 컨트롤러입니다.
/// 패널 오브젝트 자체는 항상 활성 상태로 유지하고 Equip/Charter의 위치로 열림/닫힘을 표현합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyEquipPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slide Targets")]
    [SerializeField] private RectTransform equipRect;
    [SerializeField] private RectTransform charterRect;

    [Header("Slide Position")]
    [SerializeField] private float equipStartX = -1350f;
    [SerializeField] private float equipEndX = -450f;
    [SerializeField] private float charterStartX = 1350f;
    [SerializeField] private float charterEndX = 450f;

    [Header("Slide Animation")]
    [SerializeField, Min(0f)] private float slideDuration = 0.35f;
    [SerializeField]
    private AnimationCurve slideCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Close Input")]
    [SerializeField] private bool closeOnOutsideClick = true;

    [Header("Opened Panel")]
    [SerializeField] private bool bringToFront = true;

    private Coroutine slideAnimationCoroutine;
    private RectTransform toggleButtonRect;
    private bool isOpen;
    private bool isClosing;
    private int lastToggleFrame = -1;

    public bool IsOpen => isOpen && !isClosing;

    private void Awake()
    {
        ResolvePanelRoot();
        ResolveSlideTargets();
        ResetSlidePositions();
        isOpen = false;
        isClosing = false;
    }

    private void OnEnable()
    {
        // Equip_panel은 StoragePanel처럼 항상 활성화된 상태를 유지합니다.
        // 다시 활성화된 경우에도 닫힌 위치에서 시작합니다.
        if (!isOpen && !isClosing)
            ResetSlidePositions();
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == lastToggleFrame)
            return;

        if (!closeOnOutsideClick || !Input.GetMouseButtonDown(0))
            return;

        Vector2 pointerPosition = Input.mousePosition;
        if (IsPointerInsideOpenArea(pointerPosition))
            return;

        Close();
    }

    private void OnDisable()
    {
        StopSlideAnimation();
        isOpen = false;
        isClosing = false;
        ResetSlidePositions();
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDestroy()
    {
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    /// <summary>
    /// Equip 버튼 자신의 RectTransform을 등록합니다.
    /// 버튼 클릭을 패널 바깥 클릭으로 오인하지 않도록 사용합니다.
    /// </summary>
    public void SetToggleButton(RectTransform buttonRect)
    {
        toggleButtonRect = buttonRect;
    }

    /// <summary>
    /// Equip 버튼에서 호출합니다.
    /// 닫혀 있으면 열고, 열려 있으면 시작 위치로 슬라이드 아웃합니다.
    /// </summary>
    public void Toggle()
    {
        lastToggleFrame = Time.frameCount;

        if (isOpen && !isClosing)
            Close();
        else
            Open();
    }

    public void Open()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
        {
            Debug.LogWarning("[LobbyEquipPanelUI] Equip_panel을 찾을 수 없습니다.", this);
            return;
        }

        if (isOpen && !isClosing)
            return;

        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        TitleManager.CloseTitleModePanelsExceptInScene(root);

        if (!root.activeSelf)
            root.SetActive(true);

        if (bringToFront)
            root.transform.SetAsLastSibling();

        ResolveSlideTargets();
        StopSlideAnimation();

        // 닫힘 도중 다시 열면 현재 위치에서 자연스럽게 열리도록 합니다.
        isClosing = false;
        isOpen = true;
        LobbyPositionModalInputBlocker.Block(this);
        slideAnimationCoroutine = StartCoroutine(PlaySlideAnimation(true));
    }

    public void Close()
    {
        if (!isOpen || isClosing)
            return;

        ResolveSlideTargets();
        StopSlideAnimation();
        isClosing = true;
        isOpen = false;
        slideAnimationCoroutine = StartCoroutine(PlaySlideAnimation(false));
    }

    private IEnumerator PlaySlideAnimation(bool opening)
    {
        ResolveSlideTargets();

        float equipFromX = equipRect != null ? equipRect.anchoredPosition.x : (opening ? equipStartX : equipEndX);
        float charterFromX = charterRect != null ? charterRect.anchoredPosition.x : (opening ? charterStartX : charterEndX);
        float equipToX = opening ? equipEndX : equipStartX;
        float charterToX = opening ? charterEndX : charterStartX;

        if (slideDuration <= 0f)
        {
            SetAnchoredX(equipRect, equipToX);
            SetAnchoredX(charterRect, charterToX);
            FinishSlide(opening);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / slideDuration);
            float curveValue = slideCurve != null ? slideCurve.Evaluate(normalized) : normalized;

            SetAnchoredX(equipRect, Mathf.LerpUnclamped(equipFromX, equipToX, curveValue));
            SetAnchoredX(charterRect, Mathf.LerpUnclamped(charterFromX, charterToX, curveValue));
            yield return null;
        }

        SetAnchoredX(equipRect, equipToX);
        SetAnchoredX(charterRect, charterToX);
        FinishSlide(opening);
    }

    private void FinishSlide(bool opening)
    {
        slideAnimationCoroutine = null;

        if (opening)
        {
            isOpen = true;
            isClosing = false;
            return;
        }

        FinishClose();
    }

    private void FinishClose()
    {
        slideAnimationCoroutine = null;
        isOpen = false;
        isClosing = false;
        LobbyPositionModalInputBlocker.Unblock(this);

        // Equip_panel 자체는 비활성화하지 않습니다.
        // 닫힘 상태는 Equip=-1350, Charter=1350 위치로만 표현합니다.
    }

    private bool IsPointerInsideOpenArea(Vector2 screenPosition)
    {
        if (ContainsScreenPoint(equipRect, screenPosition))
            return true;

        if (ContainsScreenPoint(charterRect, screenPosition))
            return true;

        if (ContainsScreenPoint(toggleButtonRect, screenPosition))
            return true;

        return false;
    }

    private static bool ContainsScreenPoint(RectTransform rect, Vector2 screenPosition)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    private void StopSlideAnimation()
    {
        if (slideAnimationCoroutine == null)
            return;

        StopCoroutine(slideAnimationCoroutine);
        slideAnimationCoroutine = null;
    }

    private void ResetSlidePositions()
    {
        ResolveSlideTargets();
        SetAnchoredX(equipRect, equipStartX);
        SetAnchoredX(charterRect, charterStartX);
    }

    private void ResolveSlideTargets()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
            return;

        if (equipRect == null)
        {
            Transform equip = FindChildRecursive(root.transform, "Equip");
            if (equip != null)
                equipRect = equip as RectTransform;
        }

        if (charterRect == null)
        {
            Transform charter = FindChildRecursive(root.transform, "Charter");
            if (charter != null)
                charterRect = charter as RectTransform;
        }
    }

    private GameObject ResolvePanelRoot()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        return panelRoot;
    }

    private static void SetAnchoredX(RectTransform target, float x)
    {
        if (target == null)
            return;

        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }
}
