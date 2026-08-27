using UnityEngine;
using UnityEngine.EventSystems;

public class OptionOpenButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string hoverSfx = AudioIds.Sfx.NormalButtonHover;
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSound();
    }

    public void OpenOption()
    {
        PlayClickSound();
        TitleManager.CloseTitleModePanelsInScene();

        UIManager uiManager = GetUIManager();
        if (uiManager == null)
        {
            Debug.LogWarning("[OptionOpenButton] UIManager is not found. Option panel cannot be opened.");
            return;
        }

        uiManager.ShowOption();
    }

    private UIManager GetUIManager()
    {
        if (UIManager.Instance != null)
        {
            return UIManager.Instance;
        }

        UIManager uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiManager != null)
        {
            return uiManager;
        }

        return null;
    }

    private void PlayHoverSound()
    {
        if (!playHoverSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(hoverSfx);
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(clickSfx);
    }
}
