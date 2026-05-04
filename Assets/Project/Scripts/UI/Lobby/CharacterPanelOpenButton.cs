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
    [SerializeField] private List<CharacterPanelEntry> characterPanels = new();

    [Header("Effect")]
    [SerializeField] private UIPanelEffect effect = UIPanelEffect.None;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Option")]
    [SerializeField] private bool playClickSound = true;

    private static GameObject currentDetailPanel;

    private bool isPlayingEffect;
    private GameObject pendingTargetPanel;

    public void Execute()
    {
        if (isPlayingEffect)
            return;

        if (playClickSound && AudioManager.Instance != null)
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

        if (effect == UIPanelEffect.Fade && fadeImage != null)
            StartCoroutine(FadeRoutine());
        else
            OpenTargetPanel();
    }

    private GameObject GetPanelByCharacter(CharacterType characterType)
    {
        foreach (var entry in characterPanels)
        {
            if (entry.characterType == characterType)
                return entry.targetPanel;
        }

        return null;
    }

    private void OpenTargetPanel()
    {
        if (pendingTargetPanel == null)
            return;

        if (currentDetailPanel != null && currentDetailPanel != pendingTargetPanel)
            currentDetailPanel.SetActive(false);

        pendingTargetPanel.SetActive(true);
        currentDetailPanel = pendingTargetPanel;

        Debug.Log($"[CharacterPanelOpenButton] Open: {pendingTargetPanel.name}");
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
            fadeImage.gameObject.SetActive(false);
    }
}