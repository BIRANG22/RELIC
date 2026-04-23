using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterPanelEntry
{
    public CharacterType characterType;
    public GameObject targetPanel;
}

public class CharacterPanelOpenButton : MonoBehaviour
{
    [Header("Character Panels")]
    [SerializeField] private List<CharacterPanelEntry> characterPanels = new List<CharacterPanelEntry>();

    [Header("Effect")]
    [SerializeField] private UIPanelEffect effect = UIPanelEffect.None;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Option")]
    [SerializeField] private bool playClickSound = true;

    private bool isPlayingEffect = false;
    private GameObject pendingTargetPanel;

    public void Execute()
    {
        if (isPlayingEffect)
            return;

        if (playClickSound)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharacterPanelOpenButton] CharacterSelectionState instance is missing.");
            return;
        }

        CharacterType currentCharacter = CharacterSelectionState.Instance.CurrentCharacter;

        if (currentCharacter == CharacterType.None)
        {
            Debug.LogWarning("[CharacterPanelOpenButton] No character selected.");
            return;
        }

        pendingTargetPanel = GetPanelByCharacter(currentCharacter);

        if (pendingTargetPanel == null)
        {
            Debug.LogWarning($"[CharacterPanelOpenButton] No panel assigned for character: {currentCharacter}");
            return;
        }

        switch (effect)
        {
            case UIPanelEffect.None:
                OpenTargetPanel();
                break;

            case UIPanelEffect.Fade:
                if (fadeImage == null)
                {
                    Debug.LogWarning("[CharacterPanelOpenButton] Fade image is missing.");
                    OpenTargetPanel();
                    return;
                }

                StartCoroutine(FadeRoutine());
                break;
        }
    }

    private GameObject GetPanelByCharacter(CharacterType characterType)
    {
        for (int i = 0; i < characterPanels.Count; i++)
        {
            if (characterPanels[i].characterType == characterType)
                return characterPanels[i].targetPanel;
        }

        return null;
    }

    private void OpenTargetPanel()
    {
        CharacterSelectionState.Instance.OpenPanel(pendingTargetPanel);
    }

    private IEnumerator FadeRoutine()
    {
        isPlayingEffect = true;

        yield return Fade(0f, 1f);

        OpenTargetPanel();

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