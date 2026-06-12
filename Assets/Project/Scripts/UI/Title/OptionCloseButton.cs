using UnityEngine;

public class OptionCloseButton : MonoBehaviour
{
    [SerializeField] private bool playClickSound = true;

    public void CloseOption()
    {
        if (playClickSound)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        UIManager.Instance.HideOption();
    }
}