using UnityEngine;
using UnityEngine.EventSystems;

public class OptionCloseButton : MonoBehaviour, IPointerEnterHandler
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

    public void CloseOption()
    {
        PlayClickSound();

        UIManager.Instance.HideOption();
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
