using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyQuestGate : MonoBehaviour
{
    [SerializeField]
    private LobbyTutorialProgress requiredProgress =
        LobbyTutorialProgress.WaitingForSetup;
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

    public bool UpdateButtonInteractable
    {
        get => updateButtonInteractable;
        set
        {
            updateButtonInteractable = value;

            if (!updateButtonInteractable && button != null)
            {
                // 잠겨 있어도 클릭 이벤트를 받아 경고 UI를 띄우는 기능에서 사용합니다.
                button.interactable = true;
                return;
            }

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
        string message = GameLocalization.Get(LocalizationKeys.Warning.QuestMustComplete);
        if (SettingWarningUI.ShowMessage(message))
            return;

        Debug.LogWarning($"[LobbyQuestGate] {message}", this);
    }
}
