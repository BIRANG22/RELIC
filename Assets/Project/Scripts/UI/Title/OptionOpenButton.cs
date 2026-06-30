using UnityEngine;
using UnityEngine.EventSystems;

public class OptionOpenButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private SfxType hoverSfx = SfxType.NormalButtonHover;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSound();
    }

    public void OpenOption()
    {
        PlayClickSound();

        UIManager.Instance.ShowOption();
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
