using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the resolved coin side and waits for the user to confirm it.
/// Assign separate heads/tails sprites and the confirm button in Inspector.
/// </summary>
public sealed class EventCoinFlipPresenter : MonoBehaviour
{
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite headsSprite;
    [SerializeField] private Sprite tailsSprite;
    [SerializeField] private Button confirmButton;
    [SerializeField, Min(0.01f)] private float flipDuration = 0.8f;
    [SerializeField, Min(1)] private int flipCount = 9;

    private Coroutine routine;
    private Action confirmed;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
    }

    public void Show(bool heads, Action onConfirmed)
    {
        if (coinImage == null || headsSprite == null || tailsSprite == null || confirmButton == null)
        {
            Debug.LogError("[EventCoinFlipPresenter] Coin image, sprites, or confirm button are not configured.", this);
            return;
        }
        if (routine != null) StopCoroutine(routine);
        confirmed = onConfirmed;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        confirmButton.interactable = false;
        routine = StartCoroutine(Animate(heads));
    }

    private IEnumerator Animate(bool heads)
    {
        Sprite result = heads ? headsSprite : tailsSprite;
        for (int i = 0; i < flipCount; i++)
        {
            coinImage.sprite = i % 2 == 0 ? headsSprite : tailsSprite;
            yield return new WaitForSecondsRealtime(flipDuration / flipCount);
        }
        coinImage.sprite = result;
        confirmButton.interactable = true;
        routine = null;
    }

    private void Confirm()
    {
        if (confirmButton == null || !confirmButton.interactable) return;
        confirmButton.interactable = false;
        Action action = confirmed;
        confirmed = null;
        action?.Invoke();
    }

    public void Hide()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        confirmed = null;
        gameObject.SetActive(false);
    }
}
