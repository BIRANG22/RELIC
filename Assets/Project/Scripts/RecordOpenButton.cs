using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 타이틀, 로비, 일시정지 메뉴 등 어느 화면에서든 공통 도감을 엽니다.
/// </summary>
public class RecordOpenButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sound")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private SfxType hoverSfx = SfxType.NormalButtonHover;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

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
            Debug.LogWarning("[RecordOpenButton] UIManager를 찾지 못해 도감을 열 수 없습니다.");
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
