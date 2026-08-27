using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Opens the record screen from title, lobby, pause menu, and other shared screens.
/// </summary>
public class RecordOpenButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string hoverSfx = AudioIds.Sfx.NormalButtonHover;
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHoverSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(hoverSfx);
    }

    public void OpenRecord()
    {
        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(clickSfx);

        UIManager uiManager = GetUIManager();
        if (uiManager == null)
        {
            Debug.LogWarning("[RecordOpenButton] UIManager is missing. Cannot open record.");
            return;
        }

        uiManager.ShowRecord();
    }

    private UIManager GetUIManager()
    {
        if (UIManager.Instance != null)
            return UIManager.Instance;

        return FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
    }
}
