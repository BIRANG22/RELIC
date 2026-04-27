using UnityEngine;

public class OptionOpenButton : MonoBehaviour
{
    [SerializeField] private bool playClickSound = true;

    public void OpenOption()
    {
        if (playClickSound)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        UIManager.Instance.ShowOption();
    }
}