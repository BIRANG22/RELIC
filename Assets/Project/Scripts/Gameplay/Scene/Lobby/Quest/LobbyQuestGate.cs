using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyQuestGate : MonoBehaviour
{
    [SerializeField] private LobbyTutorialProgress requiredProgress =
        LobbyTutorialProgress.WaitingForSetup;
    [SerializeField] private string lockedMessage = "현재 퀘스트를 먼저 완료해야 합니다.";
    [SerializeField] private Button button;
    [SerializeField] private bool updateButtonInteractable = true;

    public LobbyTutorialProgress RequiredProgress
    {
        get => requiredProgress;
        set
        {
            requiredProgress = value;
            Refresh();
        }
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public bool CanExecute()
    {
        LobbyQuestManager manager = LobbyQuestManager.Instance;
        return manager == null || manager.CanUseFeature(requiredProgress);
    }

    public bool TryConsume()
    {
        if (CanExecute())
            return true;

        ShowLockedWarning();
        Refresh();
        return false;
    }

    public void Refresh()
    {
        if (updateButtonInteractable && button != null)
            button.interactable = CanExecute();
    }

    public void ShowLockedWarning()
    {
        if (string.IsNullOrWhiteSpace(lockedMessage))
            return;

        if (SettingWarningUI.Instance != null)
        {
            SettingWarningUI.Instance.Show(lockedMessage);
            return;
        }

        Debug.LogWarning($"[LobbyQuestGate] {lockedMessage}", this);
    }
}
