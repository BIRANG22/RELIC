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
    [Header("World Sprite")]
    [SerializeField] private bool allowSpriteRendererClick = true;
    [SerializeField] private bool ignoreSpriteClickWhenMoved = true;

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

    [Header("Panel Image Fade")]
    [SerializeField] private bool fadePanelImageOnChange = false;
    [SerializeField] private Image panelFadeImage;
    [SerializeField, Range(0f, 1f)] private float openedPanelAlpha = 230f / 255f;
    [SerializeField] private float panelFadeDuration = 0.2f;

    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private SfxType hoverSfx = SfxType.NormalButtonHover;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    [Header("Panel Open Close Sound")]
    [SerializeField] private bool playPanelOpenCloseSound = false;
    [SerializeField] private SfxType panelOpenSfx = SfxType.BagOpen;
    [SerializeField] private SfxType panelCloseSfx = SfxType.BagClose;
    [SerializeField, Range(0f, 1f)] private float panelOpenCloseSfxVolume = 1f;

    private const string DefaultMenuPanelObjectName = "MenuPanel";
    private const string DefaultMenuButtonObjectName = "MenuButton";
    private const string DefaultBattlePlayerHudRootName = "PlayerHUD_Root";

    private static UIPanelButton currentOpenedPanelOwner;

    public static bool HasCurrentOpenedPanel => currentOpenedPanelOwner != null;
    public static bool IsMenuPanelOpen => IsMenuPanelActiveInScene();

    public static bool IsMenuPanelActiveInScene()
    {
        GameObject menuPanel = FindMenuPanelInScene();
        return menuPanel != null && menuPanel.activeInHierarchy;
    }

    public static GameObject FindMenuPanelInScene()
    {
        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate == null)
                continue;

            if (candidate.name == DefaultMenuPanelObjectName)
                return candidate;
        }

        return null;
    }

    private bool isPlayingEffect;
    private bool isMoved;
    private Vector2 originalPosition;
    private Vector2[] originalTogetherPositions;
    private Coroutine moveCoroutine;
    private Coroutine panelImageFadeCoroutine;
    private int lastClickSoundFrame = -1;
    private float originalPanelAlpha = 1f;
    private bool usesBattlePlayerHudOpenOrigin;

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

    public static void ClearCurrentOpenedPanelIfPanel(GameObject closedPanel)
    {
        if (currentOpenedPanelOwner == null || closedPanel == null)
            return;

        if (currentOpenedPanelOwner.panelToOpen == closedPanel)
        {
            currentOpenedPanelOwner = null;
            return;
        }

        if (currentOpenedPanelOwner.panelToOpen != null &&
            currentOpenedPanelOwner.panelToOpen.transform.IsChildOf(closedPanel.transform))
        {
            currentOpenedPanelOwner = null;
        }
    }

    public void ConfigurePanelMove(RectTransform targetPanel, Vector2 offset)
    {
        panelToOpen = null;
        panelsToClose = System.Array.Empty<GameObject>();
        panelToMove = targetPanel;
        moveOffset = offset;
        toggleMove = true;
        moveTogetherTargets = System.Array.Empty<RectTransform>();
    }

    private void Awake()
    {
        CacheOriginalMovePositions();
        ApplyBattlePlayerHudInitialOpenState();

        if (flipTarget == null)
            flipTarget = GetComponent<RectTransform>();

        if (panelFadeImage != null)
            originalPanelAlpha = panelFadeImage.color.a;
    }

    private void OnDisable()
    {
        if (currentOpenedPanelOwner == this)
            currentOpenedPanelOwner = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        // 마우스 호버는 사운드만 재생합니다.
        // 패널 안의 새로하기, 이어하기, 방 생성, 입장 버튼에 마우스를 올렸을 때
        // 열린 모드 패널이 접히는 현상을 막기 위해 호버에서는 패널을 닫지 않습니다.
        PlayHoverSound();
    }

    public void Execute()
    {
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        if (isPlayingEffect)
            return;

        bool willOpen = panelToOpen == null || !panelToOpen.activeSelf;

        if (playPanelOpenCloseSound)
            PlayPanelOpenCloseSound(willOpen);
        else
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
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

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
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

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
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

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
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        if (panelToMove == null)
        {
            Debug.LogWarning("[UIPanelButton] Panel To Move is not assigned.");
            return;
        }

        if (isPlayingEffect)
            return;

        Vector2 targetPosition;
        bool willOpen = true;

        if (toggleMove)
        {
            SyncMoveStateFromCurrentPosition();

            willOpen = !isMoved;

            targetPosition = willOpen
                ? GetOpenedPosition()
                : GetClosedPosition();

            isMoved = willOpen;
        }
        else
        {
            targetPosition = panelToMove.anchoredPosition + moveOffset;
        }

        if (playPanelOpenCloseSound)
            PlayPanelOpenCloseSound(willOpen);
        else
            PlayClickSound();

        ApplyButtonFlip();

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveRoutine(targetPosition));

        FadePanelImageTo(willOpen ? openedPanelAlpha : originalPanelAlpha);
    }

    private bool ShouldBlockInteractionByOpenMenuPanel()
    {
        if (!IsMenuPanelActiveInScene())
            return false;

        if (IsMenuPanelButton())
            return false;

        if (IsInsideOpenMenuPanel())
            return false;

        return true;
    }

    private bool IsMenuPanelButton()
    {
        return gameObject != null && gameObject.name == DefaultMenuButtonObjectName;
    }

    private bool IsInsideOpenMenuPanel()
    {
        GameObject menuPanel = FindMenuPanelInScene();

        if (menuPanel == null)
            return false;

        Transform target = transform;
        return target == menuPanel.transform || target.IsChildOf(menuPanel.transform);
    }

    private bool IsMenuPanelTarget()
    {
        return panelToOpen != null && panelToOpen.name == DefaultMenuPanelObjectName;
    }

    private void CacheOriginalMovePositions()
    {
        if (panelToMove != null)
        {
            originalPosition = panelToMove.anchoredPosition;
            usesBattlePlayerHudOpenOrigin = string.Equals(
                panelToMove.name,
                DefaultBattlePlayerHudRootName,
                System.StringComparison.Ordinal
            );
        }

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

    /// <summary>
    /// 전투방의 PlayerHUD_Root는 프리팹에 저장된 위치를 열린 위치로 사용합니다.
    /// 따라서 입장 시 위치를 이동하지 않고 열린 상태로만 초기화합니다.
    /// </summary>
    private void ApplyBattlePlayerHudInitialOpenState()
    {
        if (panelToMove == null || !usesBattlePlayerHudOpenOrigin)
            return;

        panelToMove.anchoredPosition = GetOpenedPosition();

        if (moveTogetherTargets != null && originalTogetherPositions != null)
        {
            for (int i = 0; i < moveTogetherTargets.Length; i++)
            {
                if (moveTogetherTargets[i] == null ||
                    i >= originalTogetherPositions.Length)
                {
                    continue;
                }

                moveTogetherTargets[i].anchoredPosition =
                    GetOpenedTogetherPosition(i);
            }
        }

        isMoved = true;
    }

    private Vector2 GetClosedPosition()
    {
        return usesBattlePlayerHudOpenOrigin
            ? originalPosition + moveOffset
            : originalPosition;
    }

    private Vector2 GetOpenedPosition()
    {
        return usesBattlePlayerHudOpenOrigin
            ? originalPosition
            : originalPosition + moveOffset;
    }

    private Vector2 GetClosedTogetherPosition(int index)
    {
        Vector2 position = originalTogetherPositions[index];
        return usesBattlePlayerHudOpenOrigin
            ? position + moveOffset
            : position;
    }

    private Vector2 GetOpenedTogetherPosition(int index)
    {
        Vector2 position = originalTogetherPositions[index];
        return usesBattlePlayerHudOpenOrigin
            ? position
            : position + moveOffset;
    }

    private void SyncMoveStateFromCurrentPosition()
    {
        if (panelToMove == null)
            return;

        Vector2 closedPosition = GetClosedPosition();
        Vector2 openedPosition = GetOpenedPosition();
        Vector2 currentPosition = panelToMove.anchoredPosition;

        float distanceToClosed = Vector2.SqrMagnitude(currentPosition - closedPosition);
        float distanceToOpened = Vector2.SqrMagnitude(currentPosition - openedPosition);

        isMoved = distanceToOpened < distanceToClosed;
    }

    private void CloseCurrentOpenedPanelIfThisIsOtherButton()
    {
        if (currentOpenedPanelOwner == null)
            return;

        if (currentOpenedPanelOwner == this)
            return;

        if (!currentOpenedPanelOwner.closeOpenedPanelWhenOtherButtonHovered)
            return;

        GameObject openedPanel = currentOpenedPanelOwner.panelToOpen;

        // 현재 열린 패널 안에 있는 버튼에 마우스를 올린 경우에는
        // 다른 메뉴 버튼으로 취급하지 않습니다.
        // 튜토리얼, 새로하기, 이어하기, 방 생성, 참여 버튼을
        // 조작할 때 펼쳐진 버튼들이 다시 접히는 것을 방지합니다.
        if (openedPanel != null)
        {
            Transform openedPanelTransform = openedPanel.transform;
            Transform hoveredButtonTransform = transform;

            if (hoveredButtonTransform == openedPanelTransform ||
                hoveredButtonTransform.IsChildOf(openedPanelTransform))
            {
                return;
            }
        }

        currentOpenedPanelOwner.CloseOwnPanel();
    }

    private void CloseOwnPanel()
    {
        if (panelToOpen != null)
        {
            TitleModePanelSpreadAnimator titleModeAnimator =
                panelToOpen.GetComponent<TitleModePanelSpreadAnimator>();

            if (titleModeAnimator != null)
            {
                // 버튼 상태 초기화가 RectTransform 위치를 먼저 바꾸면
                // 닫힘 애니메이션이 시작되기 전에 버튼이 접힌 위치로 순간 이동합니다.
                // 따라서 펼침 패널은 닫힘 연출이 끝난 뒤 버튼 상태를 초기화합니다.
                titleModeAnimator.Close(ResetButtonAnimationsInClosedPanel);
            }
            else
            {
                ResetButtonAnimationsInClosedPanel();
                panelToOpen.SetActive(false);
            }
        }
        else
        {
            ResetButtonAnimationsInClosedPanel();
        }

        if (currentOpenedPanelOwner == this)
            currentOpenedPanelOwner = null;

        FadePanelImageTo(originalPanelAlpha);

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
            panelToMove.anchoredPosition = GetClosedPosition();

        if (moveTogetherTargets != null && originalTogetherPositions != null)
        {
            for (int i = 0; i < moveTogetherTargets.Length; i++)
            {
                if (moveTogetherTargets[i] == null || i >= originalTogetherPositions.Length)
                    continue;

                moveTogetherTargets[i].anchoredPosition =
                    GetClosedTogetherPosition(i);
            }
        }

        isMoved = false;
        isPlayingEffect = false;
        FadePanelImageTo(originalPanelAlpha);
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

    private void PlayPanelOpenCloseSound(bool willOpen)
    {
        if (!playPanelOpenCloseSound)
            return;

        if (AudioManager.Instance == null)
            return;

        SfxType targetSfx = willOpen ? panelOpenSfx : panelCloseSfx;
        AudioManager.Instance.PlaySfx(targetSfx, panelOpenCloseSfxVolume);
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

            panelToMove.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

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
        bool isMenuPanelTarget = IsMenuPanelTarget();

        if (!isMenuPanelTarget)
            ClosePanels();

        if (panelToOpen != null)
        {
            TitleManager.CloseTitleModePanelsExceptInScene(panelToOpen);

            panelToOpen.SetActive(true);

            TitleModePanelSpreadAnimator titleModeAnimator =
                panelToOpen.GetComponent<TitleModePanelSpreadAnimator>();
            if (titleModeAnimator != null)
                titleModeAnimator.Open();

            ApplyOpenedPanelFrontSorting(panelToOpen);
            currentOpenedPanelOwner = this;
            FadePanelImageTo(openedPanelAlpha);
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
        canvas.sortingOrder = openedPanel.name == DefaultMenuPanelObjectName
            ? Mathf.Max(openedPanelSortingOrder, 10000)
            : openedPanelSortingOrder;

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
            if (panelsToClose[i] == null)
                continue;

            TitleModePanelSpreadAnimator titleModeAnimator =
                panelsToClose[i].GetComponent<TitleModePanelSpreadAnimator>();

            if (titleModeAnimator != null)
                titleModeAnimator.Close();
            else
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

    private void FadePanelImageTo(float targetAlpha)
    {
        if (!fadePanelImageOnChange || panelFadeImage == null)
            return;

        if (panelImageFadeCoroutine != null)
            StopCoroutine(panelImageFadeCoroutine);

        panelImageFadeCoroutine = StartCoroutine(PanelImageFadeRoutine(targetAlpha));
    }

    private IEnumerator PanelImageFadeRoutine(float targetAlpha)
    {
        Color color = panelFadeImage.color;
        float startAlpha = color.a;

        float time = 0f;
        float duration = Mathf.Max(0.01f, panelFadeDuration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            panelFadeImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        panelFadeImage.color = color;

        panelImageFadeCoroutine = null;
    }

    private void OnMouseUpAsButton()
    {
        if (ShouldBlockInteractionByOpenMenuPanel())
            return;

        if (!allowSpriteRendererClick)
            return;

        if (ignoreSpriteClickWhenMoved && panelToMove != null && isMoved)
            return;

        if (panelToMove != null)
            MovePanel();
        else
            Execute();
    }
}
