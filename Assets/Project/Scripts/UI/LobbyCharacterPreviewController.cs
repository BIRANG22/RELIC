using UnityEngine;

public class LobbyCharacterPreviewController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject lobbyMainPanel;
    [SerializeField] private GameObject characterSettingPanel;

    [Header("Preview Root")]
    [SerializeField] private GameObject previewRoot;

    [Header("Character Preview Objects")]
    [SerializeField] private GameObject character1Preview;
    [SerializeField] private GameObject character2Preview;
    [SerializeField] private GameObject character3Preview;

    [Header("Settings")]
    [SerializeField] private int defaultCharacterIndex = 0;

    private int currentCharacterIndex;

    private void Awake()
    {
        currentCharacterIndex = Mathf.Clamp(defaultCharacterIndex, 0, 2);
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        RefreshByPanelState();
    }

    public void ShowCharacter(int characterIndex)
    {
        currentCharacterIndex = Mathf.Clamp(characterIndex, 0, 2);
        Refresh();
    }

    public void ShowCharacter1()
    {
        ShowCharacter(0);
    }

    public void ShowCharacter2()
    {
        ShowCharacter(1);
    }

    public void ShowCharacter3()
    {
        ShowCharacter(2);
    }

    public void Refresh()
    {
        bool shouldShow = IsCharacterSettingPanelOpen();

        SetActiveIfNeeded(previewRoot, shouldShow);

        if (!shouldShow)
        {
            SetActiveIfNeeded(character1Preview, false);
            SetActiveIfNeeded(character2Preview, false);
            SetActiveIfNeeded(character3Preview, false);
            return;
        }

        SetActiveIfNeeded(character1Preview, currentCharacterIndex == 0);
        SetActiveIfNeeded(character2Preview, currentCharacterIndex == 1);
        SetActiveIfNeeded(character3Preview, currentCharacterIndex == 2);
    }

    private void RefreshByPanelState()
    {
        bool shouldShow = IsCharacterSettingPanelOpen();

        if (previewRoot != null && previewRoot.activeSelf != shouldShow)
            Refresh();
    }

    private bool IsCharacterSettingPanelOpen()
    {
        if (characterSettingPanel == null)
            return false;

        if (!characterSettingPanel.activeInHierarchy)
            return false;

        if (lobbyMainPanel != null && lobbyMainPanel.activeInHierarchy)
            return false;

        return true;
    }

    private static void SetActiveIfNeeded(GameObject target, bool active)
    {
        if (target == null)
            return;

        if (target.activeSelf != active)
            target.SetActive(active);
    }
}