using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Plays the coin flip and automatically continues after revealing the result.
/// The result callback is responsible for closing the presenter and advancing the event.
/// </summary>
public sealed class EventCoinFlipPresenter : MonoBehaviour
{
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite headsSprite;
    [SerializeField] private Sprite tailsSprite;
    [SerializeField] private Button confirmButton;
    [Header("Prediction Buttons")]
    [SerializeField] private Button headsButton;
    [SerializeField] private Button tailsButton;
    [SerializeField] private TMP_Text detailText;
    [SerializeField, Min(0.01f)] private float flipDuration = 0.8f;
    [SerializeField, Min(1)] private int flipCount = 9;
    [SerializeField, Min(0f)] private float resultHoldDuration = 0.15f;

    [Header("Panel Fade Out")]
    [SerializeField, Min(0f)] private float panelFadeOutDuration = 0.2f;

    private Coroutine routine;
    private Action confirmed;
    private Action<bool, bool> predictionCompleted;
    private bool actualHeads;
    private bool awaitingPrediction;

    private void Awake()
    {
        // Confirmation is optional. The coin flip now completes automatically.
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
        if (headsButton == null) headsButton = FindButton("HeadsButton");
        if (tailsButton == null) tailsButton = FindButton("TailsButton");
        if (detailText == null)
        {
            Transform child = FindChild(transform, "Detailtext");
            if (child != null) detailText = child.GetComponent<TMP_Text>();
        }
        if (headsButton != null) headsButton.onClick.AddListener(ChooseHeads);
        if (tailsButton != null) tailsButton.onClick.AddListener(ChooseTails);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
        if (headsButton != null) headsButton.onClick.RemoveListener(ChooseHeads);
        if (tailsButton != null) tailsButton.onClick.RemoveListener(ChooseTails);
    }

    // The player chooses a side inside this panel before the flip begins.
    public void ShowPrediction(bool coinHeads, Action<bool, bool> onFinished)
    {
        if (coinImage == null || headsSprite == null || tailsSprite == null ||
            headsButton == null || tailsButton == null)
        {
            Debug.LogError("[EventCoinFlipPresenter] Coin image, sprites or prediction buttons are missing.", this);
            return;
        }

        if (routine != null) { StopCoroutine(routine); routine = null; }
        confirmed = null;
        predictionCompleted = onFinished;
        awaitingPrediction = true;
        actualHeads = coinHeads;
        gameObject.SetActive(true);
        RestorePanelAlpha();
        transform.SetAsLastSibling();
        if (detailText != null) detailText.text = string.Empty;
        if (headsButton != null) headsButton.gameObject.SetActive(true);
        if (tailsButton != null) tailsButton.gameObject.SetActive(true);
        SetPredictionButtonsInteractable(true);
        if (confirmButton != null) confirmButton.interactable = false;
        EnsurePredictionInput();
    }

    private void LateUpdate()
    {
        // Some UI transitions or Animator clips can restore a disabled Button state.
        // While waiting for the player's choice, both prediction buttons must stay usable.
        if (awaitingPrediction && routine == null)
        {
            if (headsButton != null && tailsButton != null &&
                (!headsButton.interactable || !tailsButton.interactable))
                SetPredictionButtonsInteractable(true);
        }
    }

    private void SetPredictionButtonsInteractable(bool enabled)
    {
        if (headsButton != null) headsButton.interactable = enabled;
        if (tailsButton != null) tailsButton.interactable = enabled;
    }

    private void EnsurePredictionInput()
    {
        // The choice list is faded out before this panel opens. Its CanvasGroup
        // must not block raycasts on the separate coin prediction panel.
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = true;
    }

    private void ChooseHeads() => ChoosePrediction(true);
    private void ChooseTails() => ChoosePrediction(false);

    private void ChoosePrediction(bool predictedHeads)
    {
        if (predictionCompleted == null || routine != null) return;
        awaitingPrediction = false;
        SetPredictionButtonsInteractable(false);
        routine = StartCoroutine(AnimatePrediction(predictedHeads));
    }

    private IEnumerator AnimatePrediction(bool predictedHeads)
    {
        Sprite result = actualHeads ? headsSprite : tailsSprite;
        int count = Mathf.Max(1, flipCount);
        float interval = Mathf.Max(0.01f, flipDuration) / count;
        for (int i = 0; i < count; i++)
        {
            coinImage.sprite = (i & 1) == 0 ? headsSprite : tailsSprite;
            yield return new WaitForSecondsRealtime(interval);
        }
        coinImage.sprite = result;
        if (detailText != null)
            detailText.text = GameLocalization.Get(predictedHeads == actualHeads
                ? "event.dice.judgement_success" : "event.dice.judgement_failure");
        if (resultHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(resultHoldDuration);
        routine = null;
        Action<bool, bool> callback = predictionCompleted;
        predictionCompleted = null;
        callback?.Invoke(actualHeads, predictedHeads);
    }

    // Legacy coin flow remains supported for other event callers.
    public void Show(bool heads, Action onConfirmed)
    {
        if (coinImage == null || headsSprite == null || tailsSprite == null)
        {
            Debug.LogError("[EventCoinFlipPresenter] Coin image or heads/tails sprites are not configured.", this);
            return;
        }
        if (routine != null) { StopCoroutine(routine); routine = null; }
        predictionCompleted = null;
        awaitingPrediction = false;
        confirmed = onConfirmed;
        gameObject.SetActive(true);
        RestorePanelAlpha();
        transform.SetAsLastSibling();
        if (headsButton != null) headsButton.gameObject.SetActive(false);
        if (tailsButton != null) tailsButton.gameObject.SetActive(false);
        if (detailText != null) detailText.text = string.Empty;
        if (confirmButton != null) confirmButton.interactable = false;
        routine = StartCoroutine(Animate(heads));
    }

    private Button FindButton(string name)
    {
        Transform child = FindChild(transform, name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private IEnumerator Animate(bool heads)
    {
        Sprite result = heads ? headsSprite : tailsSprite;
        int count = Mathf.Max(1, flipCount);
        float interval = Mathf.Max(0.01f, flipDuration) / count;
        for (int i = 0; i < count; i++)
        {
            coinImage.sprite = (i & 1) == 0 ? headsSprite : tailsSprite;
            yield return new WaitForSecondsRealtime(interval);
        }

        coinImage.sprite = result;
        if (resultHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(resultHoldDuration);

        routine = null;
        Confirm();
    }

    private void Confirm()
    {
        // The callback calls Hide() and then resolves the choice.
        Action action = confirmed;
        confirmed = null;
        if (confirmButton != null)
            confirmButton.interactable = false;
        action?.Invoke();
    }

    // Called by EventRoomController after the world result visual has played.
    // The caller owns this coroutine so that disabling the presenter cannot
    // interrupt the remaining event progression.
    public IEnumerator FadeOutAndHide()
    {
        if (!gameObject.activeInHierarchy)
            yield break;

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        group.interactable = false;
        group.blocksRaycasts = false;
        float startAlpha = group.alpha;
        float duration = Mathf.Max(0f, panelFadeOutDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        group.alpha = 0f;
        Hide();
    }

    private void RestorePanelAlpha()
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = 1f;
    }

    public void Hide()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        confirmed = null;
        predictionCompleted = null;
        awaitingPrediction = false;
        gameObject.SetActive(false);
    }
}
