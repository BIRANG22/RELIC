using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 모드 패널이 열릴 때 버튼을 한 지점에서 각자의 위치로 펼치고,
/// 닫힐 때는 다시 시작 지점으로 모은 뒤 패널을 비활성화합니다.
/// </summary>
public class TitleModePanelSpreadAnimator : MonoBehaviour
{
    [Header("펼쳐질 버튼")]
    [Tooltip("왼쪽부터 순서대로 등록합니다. 각 버튼의 현재 위치가 펼쳐진 최종 위치로 저장됩니다.")]
    [SerializeField] private RectTransform[] buttons;

    [Header("시작 위치")]
    [Tooltip("모든 버튼이 펼쳐지기 전에 모여 있을 위치입니다.")]
    [SerializeField] private Vector2 collapsedPosition = new Vector2(-500f, -150f);

    [Header("애니메이션")]
    [Min(0.01f)]
    [SerializeField] private float openDuration = 0.35f;

    [Min(0.01f)]
    [SerializeField] private float closeDuration = 0.25f;

    [Tooltip("버튼마다 애니메이션 시작 시간을 조금씩 늦춥니다.")]
    [Min(0f)]
    [SerializeField] private float buttonInterval = 0.05f;

    [Tooltip("켜면 자연스럽게 감속하며 펼쳐지고 닫힙니다.")]
    [SerializeField] private bool useSmoothStep = true;

    [Header("전환 중 검정 처리")]
    [Tooltip("열고 닫히는 동안만 검정색으로 보일 배경 이미지를 등록합니다. 전환이 끝나면 원래 색상으로 돌아갑니다.")]
    [SerializeField] private Graphic[] transitionBackgroundGraphics;

    private Vector2[] openedPositions;
    private CanvasGroup[] buttonCanvasGroups;
    private Selectable[] buttonSelectables;
    private Coroutine animationCoroutine;
    private bool positionsCached;
    private bool isClosing;
    private Action closeCompleted;
    private Color[] originalTransitionColors;

    private void Awake()
    {
        CacheButtonData();
        CacheTransitionBackgroundColors();
    }

    private void OnEnable()
    {
        Open();
    }

    /// <summary>
    /// 패널을 활성화하고 펼침 애니메이션을 재생합니다.
    /// </summary>
    public void Open()
    {
        CacheButtonData();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        StopCurrentAnimation();
        isClosing = false;
        animationCoroutine = StartCoroutine(PlayAnimation(true));
    }

    /// <summary>
    /// 닫힘 애니메이션을 재생한 뒤 패널을 비활성화합니다.
    /// </summary>
    public void Close()
    {
        Close(null);
    }

    /// <summary>
    /// 닫힘 애니메이션이 끝난 뒤 실행할 동작을 함께 등록합니다.
    /// 버튼 상태 초기화처럼 위치를 바꿀 수 있는 처리는 반드시 닫힘 완료 후 실행해야 합니다.
    /// </summary>
    public void Close(Action onClosed)
    {
        if (!gameObject.activeSelf)
        {
            onClosed?.Invoke();
            return;
        }

        if (isClosing)
        {
            closeCompleted += onClosed;
            return;
        }

        CacheButtonData();
        StopCurrentAnimation();

        closeCompleted = onClosed;
        isClosing = true;
        animationCoroutine = StartCoroutine(PlayAnimation(false));
    }

    /// <summary>
    /// 애니메이션 없이 즉시 접힌 상태로 만들고 패널을 비활성화합니다.
    /// </summary>
    public void CloseImmediately()
    {
        StopCurrentAnimation();
        CacheButtonData();
        ApplyCollapsedState();
        RestoreTransitionBackgroundColors();
        isClosing = false;

        Action completed = closeCompleted;
        closeCompleted = null;
        completed?.Invoke();

        gameObject.SetActive(false);
    }

    private void CacheButtonData()
    {
        if (positionsCached && openedPositions != null && openedPositions.Length == GetButtonCount())
        {
            return;
        }

        int count = GetButtonCount();
        openedPositions = new Vector2[count];
        buttonCanvasGroups = new CanvasGroup[count];
        buttonSelectables = new Selectable[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform button = buttons[i];
            if (button == null)
            {
                continue;
            }

            openedPositions[i] = button.anchoredPosition;

            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }

            buttonCanvasGroups[i] = canvasGroup;
            buttonSelectables[i] = button.GetComponent<Selectable>();
        }

        positionsCached = true;
    }

    private IEnumerator PlayAnimation(bool opening)
    {
        int count = GetButtonCount();
        float duration = Mathf.Max(0.01f, opening ? openDuration : closeDuration);
        float interval = Mathf.Max(0f, buttonInterval);
        float totalDuration = duration + interval * Mathf.Max(0, count - 1);

        Vector2[] startPositions = new Vector2[count];
        float[] startAlphas = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            if (opening)
            {
                buttons[i].anchoredPosition = collapsedPosition;
                SetAlpha(i, 0f);
            }

            startPositions[i] = buttons[i].anchoredPosition;
            startAlphas[i] = GetAlpha(i);
            SetInteractable(i, false);
        }

        Color[] transitionStartColors = CaptureCurrentTransitionColors();
        if (opening)
        {
            ApplyTransitionColorProgress(0f, true, transitionStartColors);
        }

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float transitionProgress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, totalDuration));
            if (useSmoothStep)
            {
                transitionProgress = transitionProgress * transitionProgress * (3f - 2f * transitionProgress);
            }
            ApplyTransitionColorProgress(transitionProgress, opening, transitionStartColors);

            for (int i = 0; i < count; i++)
            {
                RectTransform button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                int animationOrder = opening ? i : count - 1 - i;
                float delay = interval * animationOrder;
                float t = Mathf.Clamp01((elapsed - delay) / duration);

                if (useSmoothStep)
                {
                    t = t * t * (3f - 2f * t);
                }

                Vector2 targetPosition = opening ? openedPositions[i] : collapsedPosition;
                float targetAlpha = opening ? 1f : 0f;

                button.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], targetPosition, t);
                SetAlpha(i, Mathf.Lerp(startAlphas[i], targetAlpha, t));
            }

            yield return null;
        }

        if (opening)
        {
            ApplyOpenedState();
            ApplyTransitionColorProgress(1f, true, transitionStartColors);
            isClosing = false;
        }
        else
        {
            ApplyCollapsedState();
            ApplyTransitionColorProgress(1f, false, transitionStartColors);
            isClosing = false;
            animationCoroutine = null;

            Action completed = closeCompleted;
            closeCompleted = null;
            completed?.Invoke();

            gameObject.SetActive(false);
            yield break;
        }

        animationCoroutine = null;
    }

    private void ApplyOpenedState()
    {
        int count = GetButtonCount();

        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            buttons[i].anchoredPosition = openedPositions[i];
            SetAlpha(i, 1f);
            SetInteractable(i, true);
        }
    }

    private void ApplyCollapsedState()
    {
        int count = GetButtonCount();

        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            buttons[i].anchoredPosition = collapsedPosition;
            SetAlpha(i, 0f);
            SetInteractable(i, false);
        }
    }

    private void StopCurrentAnimation()
    {
        if (animationCoroutine == null)
        {
            return;
        }

        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
    }

    private int GetButtonCount()
    {
        return buttons == null ? 0 : buttons.Length;
    }

    private float GetAlpha(int index)
    {
        if (buttonCanvasGroups == null || index < 0 || index >= buttonCanvasGroups.Length || buttonCanvasGroups[index] == null)
        {
            return 1f;
        }

        return buttonCanvasGroups[index].alpha;
    }

    private void SetAlpha(int index, float alpha)
    {
        if (buttonCanvasGroups == null || index < 0 || index >= buttonCanvasGroups.Length || buttonCanvasGroups[index] == null)
        {
            return;
        }

        buttonCanvasGroups[index].alpha = alpha;
        buttonCanvasGroups[index].blocksRaycasts = alpha >= 0.999f && !isClosing;
        buttonCanvasGroups[index].interactable = alpha >= 0.999f && !isClosing;
    }

    private void SetInteractable(int index, bool interactable)
    {
        if (buttonSelectables != null && index >= 0 && index < buttonSelectables.Length && buttonSelectables[index] != null)
        {
            buttonSelectables[index].interactable = interactable;
        }

        if (buttonCanvasGroups != null && index >= 0 && index < buttonCanvasGroups.Length && buttonCanvasGroups[index] != null)
        {
            buttonCanvasGroups[index].blocksRaycasts = interactable;
            buttonCanvasGroups[index].interactable = interactable;
        }
    }

    private void CacheTransitionBackgroundColors()
    {
        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        originalTransitionColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic != null)
            {
                originalTransitionColors[i] = graphic.color;
            }
        }
    }

    private Color[] CaptureCurrentTransitionColors()
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        Color[] colors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            colors[i] = graphic != null ? graphic.color : Color.white;
        }

        return colors;
    }

    /// <summary>
    /// 열릴 때는 검정에서 원래 색상으로, 닫힐 때는 현재 색상에서 검정으로 자연스럽게 보간합니다.
    /// </summary>
    private void ApplyTransitionColorProgress(float progress, bool opening, Color[] startColors)
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color original = originalTransitionColors[i];
            Color black = new Color(0f, 0f, 0f, original.a);

            if (opening)
            {
                graphic.color = Color.Lerp(black, original, progress);
            }
            else
            {
                Color start = startColors != null && i < startColors.Length ? startColors[i] : original;
                Color targetBlack = new Color(0f, 0f, 0f, start.a);
                graphic.color = Color.Lerp(start, targetBlack, progress);
            }
        }
    }

    private void SetTransitionBackgroundsBlack()
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color current = graphic.color;
            graphic.color = new Color(0f, 0f, 0f, current.a);
        }
    }

    private void RestoreTransitionBackgroundColors()
    {
        CacheTransitionBackgroundColorsIfNeeded();

        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        for (int i = 0; i < count; i++)
        {
            Graphic graphic = transitionBackgroundGraphics[i];
            if (graphic != null)
            {
                graphic.color = originalTransitionColors[i];
            }
        }
    }

    private void CacheTransitionBackgroundColorsIfNeeded()
    {
        int count = transitionBackgroundGraphics == null ? 0 : transitionBackgroundGraphics.Length;
        if (originalTransitionColors == null || originalTransitionColors.Length != count)
        {
            CacheTransitionBackgroundColors();
        }
    }

}
