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

    private bool isPlayingEffect;
    private bool isMoved;
    private Vector2 originalPosition;
    private Coroutine moveCoroutine;
    private int lastClickSoundFrame = -1;

    private void Awake()
    {
        if (panelToMove != null)
            originalPosition = panelToMove.anchoredPosition;

        if (flipTarget == null)
            flipTarget = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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
            panelToOpen.SetActive(false);
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

    private void PlayHoverSound()
    {
        if (!playHoverSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(hoverSfx);
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

        while (time < moveDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / moveDuration);

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
            panelToOpen.SetActive(true);
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
        Color color = fadeImage.color;

        fadeImage.gameObject.SetActive(true);

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / fadeDuration);

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
