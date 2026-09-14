using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ready_Panel을 직접 여닫아야 하는 레거시 버튼용 호환 컴포넌트입니다.
/// 기본 탐사 흐름에서는 BattlePlayButton이 Ready_Panel을 엽니다.
/// Info_Panel은 LobbyInfoPanelOpenButton을 사용합니다.
/// </summary>
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public sealed class LobbyEquipOpenButton : MonoBehaviour
{
    [SerializeField] private LobbyEquipPanelUI readyPanel;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        ResolveReadyPanel();
        RegisterToggleButton();
        BindButton();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        ResolveReadyPanel();
        RegisterToggleButton();
        BindButton();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ToggleReadyPanel);
    }

    public void ToggleReadyPanel()
    {
        LobbyEquipPanelUI resolved = ResolveReadyPanel();
        if (resolved == null)
        {
            Debug.LogWarning("[LobbyEquipOpenButton] LobbyEquipPanelUI를 찾을 수 없습니다.", this);
            return;
        }

        PlayClickSfx();
        resolved.Toggle();
    }

    private void RegisterToggleButton()
    {
        LobbyEquipPanelUI resolved = ResolveReadyPanel();
        if (resolved != null)
            resolved.SetToggleButton(transform as RectTransform);
    }

    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(ToggleReadyPanel);
        button.onClick.AddListener(ToggleReadyPanel);
    }

    private LobbyEquipPanelUI ResolveReadyPanel()
    {
        if (readyPanel != null)
            return readyPanel;

        LobbyEquipPanelUI[] panels = FindObjectsByType<LobbyEquipPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            LobbyEquipPanelUI candidate = panels[i];
            if (candidate != null && candidate.gameObject.name == "Ready_Panel")
            {
                readyPanel = candidate;
                return readyPanel;
            }
        }

        if (panels.Length > 0)
            readyPanel = panels[0];

        return readyPanel;
    }

    private void PlayClickSfx()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolume);
    }
}
