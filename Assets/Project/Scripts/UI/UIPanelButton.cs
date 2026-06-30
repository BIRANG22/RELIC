using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UIPanelEffect
{
    None,
    Fade
}

public class UIPanelButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("Panel Active")]
    [SerializeField] private GameObject panelToOpen;
    [SerializeField] private GameObject[] panelsToClose;

    [Header("Panel Move")]
    [SerializeField] private RectTransform panelToMove;
    [SerializeField] private Vector2 moveOffset = new Vector2(300f, 0f);
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private bool toggleMove = true;

    [Header("Button Flip")]
    [SerializeField] private bool flipButtonOnMove = false;
    [SerializeField] private RectTransform flipTarget;

    [Header("Toggle")]
    [SerializeField] private bool toggleIfAlreadyOpen = true;

    [Header("Opened Panel Front Sorting")]
    [SerializeField] private bool bringOpenedPanelToFront = true;
    [SerializeField] private bool forceOpenedPanelCanvasSorting = true;
    [SerializeField] private int openedPanelSortingOrder = 1000;
    [SerializeField] private bool addGraphicRaycasterToOpenedPanel = true;


    [Header("Close On Other Button Hover")]
    [SerializeField] private bool closeOpenedPanelWhenOtherButtonHovered = false;
    [SerializeField] private bool resetMoveWhenClosedByOtherButton = true;

    [Header("Move Together")]
    [SerializeField] private RectTransform[] moveTogetherTargets;

    [Header("Effect")]
    [SerializeField] private UIPanelEffect effect = UIPanelEffect.None;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private SfxType hoverSfx = SfxType.NormalButtonHover;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    private static UIPanelButton currentOpenedPanelOwner;

    public static bool HasCurrentOpenedPanel => currentOpenedPanelOwner != null;

    public static void CloseCurrentOpenedPanel()
    {
        TryCloseCurrentOpenedPanel();
    }

    public static bool TryCloseCurrentOpenedPanel()
    {
        if (currentOpenedPanelOwner == null)
            return false;

        currentOpenedPanelOwner.CloseOwnPanel();
        return true;
    }

    private bool isPlayingEffect;
    private bool isMoved;
    private Vector2 originalPosition;
    private Vector2[] originalTogetherPositions;
    private Coroutine moveCoroutine;
    private int lastClickSoundFrame = -1;

    private void Awake()
    {
        CacheOriginalMovePositions();

        if (flipTarget == null)
            flipTarget = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        if (currentOpenedPanelOwner == this)
            currentOpenedPanelOwner = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CloseCurrentOpenedPanelIfThisIsOtherButton();
        PlayHoverSound();
    }

    public void Execute()
    {
        if (isPlayingEffect)
            return;

        PlayClickSound();

        if (toggleIfAlreadyOpen &&
            panelToOpen != null &&
            panelToOpen.activeSelf)
        {
            CloseOwnPanel();
            return;
        }

        switch (effect)
        {
            case UIPanelEffect.None:
                ExecutePanelTransition();
                break;

            case UIPanelEffect.Fade:
                if (fadeImage == null)
                {
                    Debug.LogWarning("[UIPanelButton] Fade effect selected but Fade Image is not assigned.");
                    ExecutePanelTransition();
                    return;
                }

                StartCoroutine(FadeRoutine());
                break;
        }
    }




    public void ExecuteGiveUpConfirm()
    {
        if (isPlayingEffect)
            return;

        PlayClickSound();

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[UIPanelButton] UIManager.Instance를 찾지 못했습니다.");
            return;
        }

        UIManager.Instance.ShowGiveUpConfirm();
    }

    public void ExecuteQuitConfirm()
    {
        if (isPlayingEffect)
            return;

        PlayClickSound();

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[UIPanelButton] UIManager.Instance를 찾지 못했습니다.");
            return;
        }

        UIManager.Instance.ShowQuitConfirm();
    }

    public void ExecuteCloseCurrentPanel()
    {
        if (isPlayingEffect)
            return;

        PlayClickSound();

        if (TryCloseCurrentOpenedPanel())
            return;

        if (panelToOpen != null && panelToOpen.activeSelf)
            CloseOwnPanel();
    }

    public void MovePanel()
    {
        if (panelToMove == null)
        {
            Debug.LogWarning("[UIPanelButton] Panel To Move is not assigned.");
            return;
        }

        if (isPlayingEffect)
            return;

        PlayClickSound();

        Vector2 targetPosition;

        if (toggleMove)
        {
            targetPosition = isMoved
                ? originalPosition
                : originalPosition + moveOffset;

            isMoved = !isMoved;
        }
        else
        {
            targetPosition = panelToMove.anchoredPosition + moveOffset;
        }

        ApplyButtonFlip();

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveRoutine(targetPosition));
    }

    private void CacheOriginalMovePositions()
    {
        if (panelToMove != null)
            originalPosition = panelToMove.anchoredPosition;

        if (moveTogetherTargets == null)
        {
            originalTogetherPositions = null;
            return;
        }

        originalTogetherPositions = new Vector2[moveTogetherTargets.Length];

        for (int i = 0; i < moveTogetherTargets.Length; i++)
        {
            if (moveTogetherTargets[i] != null)
                originalTogetherPositions[i] = moveTogetherTargets[i].anchoredPosition;
        }
    }

    private void CloseCurrentOpenedPanelIfThisIsOtherButton()
    {
        if (currentOpenedPanelOwner == null)
            return;

        if (currentOpenedPanelOwner == this)
            return;

        if (!currentOpenedPanelOwner.closeOpenedPanelWhenOtherButtonHovered)
            return;

        currentOpenedPanelOwner.CloseOwnPanel();
    }

    private void CloseOwnPanel()
    {
        ResetButtonAnimationsInClosedPanel();

        if (panelToOpen != null)
            panelToOpen.SetActive(false);

        if (currentOpenedPanelOwner == this)
            currentOpenedPanelOwner = null;

        if (resetMoveWhenClosedByOtherButton)
            ResetMoveState();
    }

    private void ResetButtonAnimationsInClosedPanel()
    {
        if (panelToOpen == null)
            return;

        ButtonAnimationCoroutine[] buttonAnimations =
            panelToOpen.GetComponentsInChildren<ButtonAnimationCoroutine>(true);

        for (int i = 0; i < buttonAnimations.Length; i++)
        {
            if (buttonAnimations[i] != null)
                buttonAnimations[i].ForceClearState(false);
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null &&
            eventSystem.currentSelectedGameObject != null &&
            eventSystem.currentSelectedGameObject.transform.IsChildOf(panelToOpen.transform))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    private void ResetMoveState()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (panelToMove != null)
            panelToMove.anchoredPosition = originalPosition;

        if (moveTogetherTargets != null && originalTogetherPositions != null)
        {
            for (int i = 0; i < moveTogetherTargets.Length; i++)
            {
                if (moveTogetherTargets[i] == null || i >= originalTogetherPositions.Length)
                    continue;

                moveTogetherTargets[i].anchoredPosition = originalTogetherPositions[i];
            }
        }

        isMoved = false;
        isPlayingEffect = false;
    }

    private void PlayHoverSound()
    {
        if (!playHoverSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(hoverSfx);
    }

    public void PlayClickSoundOnly()
    {
        PlayClickSound();
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        if (Time.frameCount == lastClickSoundFrame)
            return;

        if (AudioManager.Instance == null)
            return;

        lastClickSoundFrame = Time.frameCount;
        AudioManager.Instance.PlaySfx(clickSfx);
    }

    private void ApplyButtonFlip()
    {
        if (!flipButtonOnMove || flipTarget == null)
            return;

        Vector3 scale = flipTarget.localScale;
        scale.x *= -1f;
        flipTarget.localScale = scale;
    }

    private IEnumerator MoveRoutine(Vector2 targetPosition)
    {
        isPlayingEffect = true;

        Vector2 startPosition = panelToMove.anchoredPosition;
        Vector2 moveDelta = targetPosition - startPosition;

        Vector2[] togetherStartPositions = null;

        if (moveTogetherTargets != null)
        {
            togetherStartPositions = new Vector2[moveTogetherTargets.Length];

            for (int i = 0; i < moveTogetherTargets.Length; i++)
            {
                if (moveTogetherTargets[i] != null)
                    togetherStartPositions[i] = moveTogetherTargets[i].anchoredPosition;
            }
        }

        float time = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);

            panelToMove.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                t
            );

            if (moveTogetherTargets != null)
            {
                for (int i = 0; i < moveTogetherTargets.Length; i++)
                {
                    if (moveTogetherTargets[i] == null)
                        continue;

                    moveTogetherTargets[i].anchoredPosition =
                        Vector2.Lerp(
                            togetherStartPositions[i],
                            togetherStartPositions[i] + moveDelta,
                            t
                        );
                }
            }

            yield return null;
        }

        panelToMove.anchoredPosition = targetPosition;

        if (moveTogetherTargets != null)
        {
            for (int i = 0; i < moveTogetherTargets.Length; i++)
            {
                if (moveTogetherTargets[i] == null)
                    continue;

                moveTogetherTargets[i].anchoredPosition =
                    togetherStartPositions[i] + moveDelta;
            }
        }

        isPlayingEffect = false;
        moveCoroutine = null;
    }

    private void ExecutePanelTransition()
    {
        ClosePanels();

        if (panelToOpen != null)
        {
            TitleManager.CloseTitleModePanelsExceptInScene(panelToOpen);

            panelToOpen.SetActive(true);
            ApplyOpenedPanelFrontSorting(panelToOpen);
            currentOpenedPanelOwner = this;
        }
    }

    private void ApplyOpenedPanelFrontSorting(GameObject openedPanel)
    {
        if (openedPanel == null)
            return;

        if (bringOpenedPanelToFront)
            openedPanel.transform.SetAsLastSibling();

        if (!forceOpenedPanelCanvasSorting)
            return;

        Canvas canvas = openedPanel.GetComponent<Canvas>();
        if (canvas == null)
            canvas = openedPanel.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = openedPanelSortingOrder;

        if (!addGraphicRaycasterToOpenedPanel)
            return;

        GraphicRaycaster raycaster = openedPanel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            openedPanel.AddComponent<GraphicRaycaster>();
    }

    private void ClosePanels()
    {
        if (panelsToClose == null)
            return;

        for (int i = 0; i < panelsToClose.Length; i++)
        {
            if (panelsToClose[i] != null)
                panelsToClose[i].SetActive(false);
        }
    }

    private IEnumerator FadeRoutine()
    {
        isPlayingEffect = true;

        yield return Fade(0f, 1f);

        ExecutePanelTransition();

        yield return Fade(1f, 0f);

        isPlayingEffect = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float time = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);
        Color color = fadeImage.color;

        fadeImage.gameObject.SetActive(true);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);

            color.a = Mathf.Lerp(from, to, t);
            fadeImage.color = color;

            yield return null;
        }

        color.a = to;
        fadeImage.color = color;

        if (Mathf.Approximately(to, 0f))
            fadeImage.gameObject.SetActive(false);
    }
}
