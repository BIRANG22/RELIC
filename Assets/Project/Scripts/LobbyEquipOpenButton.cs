using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens and closes Equip_panel from the lobby Setting/Equip button.
/// Does not depend on world object interactions.
/// </summary>
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public sealed class LobbyEquipOpenButton : MonoBehaviour
{
    [Header("Equip Panel")]
    [SerializeField] private LobbyEquipPanelUI equipPanel;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        ResolveEquipPanel();
        RegisterToggleButton();
        BindButton();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        ResolveEquipPanel();
        RegisterToggleButton();
        BindButton();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ToggleEquipPanel);
    }

    public void ToggleEquipPanel()
    {
        LobbyEquipPanelUI resolved = ResolveEquipPanel();
        if (resolved == null)
        {
            Debug.LogWarning("[LobbyEquipOpenButton] LobbyEquipPanelUI is missing.", this);
            return;
        }

        PlayClickSfx();
        resolved.Toggle();
    }

    private void RegisterToggleButton()
    {
        LobbyEquipPanelUI resolved = ResolveEquipPanel();
        if (resolved == null)
            return;

        resolved.SetToggleButton(transform as RectTransform);
    }

    private void BindButton()
    {
        Button resolvedButton = button;
        if (resolvedButton == null)
            return;

        resolvedButton.onClick.RemoveListener(ToggleEquipPanel);
        resolvedButton.onClick.AddListener(ToggleEquipPanel);
    }

    private LobbyEquipPanelUI ResolveEquipPanel()
    {
        if (equipPanel != null)
            return equipPanel;

        LobbyEquipPanelUI[] panels = FindObjectsByType<LobbyEquipPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            LobbyEquipPanelUI candidate = panels[i];
            if (candidate == null)
                continue;

            if (candidate.gameObject.name == "Equip_panel")
            {
                equipPanel = candidate;
                return equipPanel;
            }
        }

        if (panels.Length > 0)
            equipPanel = panels[0];

        return equipPanel;
    }

    private void PlayClickSfx()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolume);
    }
}
