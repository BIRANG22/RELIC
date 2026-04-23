using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum UIPanelAction
{
    Open,
    Close,
    Toggle
}

public enum UIPanelEffect
{
    None,
    Fade
}

public class UIPanelButton : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private UIPanelAction action = UIPanelAction.Open;

    [Header("Effect")]
    [SerializeField] private UIPanelEffect effect = UIPanelEffect.None;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Option")]
    [SerializeField] private bool playClickSound = true;

    private bool isPlayingEffect = false;

    public void Execute()
    {
        if (isPlayingEffect)
            return;

        if (playClickSound)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        if (targetPanel == null)
        {
            Debug.LogWarning("[UIPanelButton] Target Panel is not assigned.");
            return;
        }

        switch (effect)
        {
            case UIPanelEffect.None:
                ExecutePanelAction();
                break;

            case UIPanelEffect.Fade:
                if (fadeImage == null)
                {
                    Debug.LogWarning("[UIPanelButton] Fade effect selected but Fade Image is not assigned.");
                    ExecutePanelAction();
                    return;
                }

                StartCoroutine(FadeRoutine());
                break;
        }
    }

    private void ExecutePanelAction()
    {
        switch (action)
        {
            case UIPanelAction.Open:
                targetPanel.SetActive(true);
                break;

            case UIPanelAction.Close:
                targetPanel.SetActive(false);
                break;

            case UIPanelAction.Toggle:
                targetPanel.SetActive(!targetPanel.activeSelf);
                break;
        }
    }

    private IEnumerator FadeRoutine()
    {
        isPlayingEffect = true;

        yield return Fade(0f, 1f);

        ExecutePanelAction();

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
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
}