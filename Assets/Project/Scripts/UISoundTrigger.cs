using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 오브젝트의 호버 및 클릭 사운드만 담당하는 독립 컴포넌트입니다.
/// 다른 버튼 애니메이션이나 선택 상태에는 관여하지 않습니다.
/// </summary>
public class UISoundTrigger : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [Header("Hover Sound")]
    [Tooltip("마우스가 이 UI 영역에 들어왔을 때 호버 사운드를 재생합니다.")]
    [SerializeField] private bool playHoverSound = false;

    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string hoverSoundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float hoverSoundVolume = 1f;

    [Header("Click Sound")]
    [Tooltip("마우스 왼쪽 버튼을 누르는 순간 클릭 사운드를 재생합니다.")]
    [SerializeField] private bool playClickSound = true;

    [SerializeField, SoundId(SoundCategory.Sfx)]
    private string clickSoundId = AudioIds.Sfx.NormalButtonClick;

    [SerializeField, Range(0f, 1f)]
    private float clickSoundVolume = 1f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound)
            return;

        PlaySfx(hoverSoundId, hoverSoundVolume);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!playClickSound || eventData.button != PointerEventData.InputButton.Left)
            return;

        PlaySfx(clickSoundId, clickSoundVolume);
    }

    private void PlaySfx(string soundId, float volume)
    {
        if (string.IsNullOrWhiteSpace(soundId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning($"[{nameof(UISoundTrigger)}] AudioManager.Instance를 찾지 못했습니다. Object: {name}", this);
            return;
        }

        audioManager.PlaySfx(soundId, Mathf.Clamp01(volume));
    }
}
