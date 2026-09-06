using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class TitleAbandonBattleButton : MonoBehaviour
{
    [Header("Confirm")]
    [SerializeField] private string confirmMessage = "정말 포기하시겠습니까?";
    [SerializeField] private string confirmYesText = "예";
    [SerializeField] private string confirmNoText = "아니오";

    [Header("Warning")]
    [SerializeField] private string missingRunMessage = "포기할 탐사 정보가 없음";
    [SerializeField] private string completeMessage = "진행 중인 탐사를 포기했습니다.";

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    private Button button;
    private bool isProcessing;

    private void Awake()
    {
        AutoBind();
        AddClickListener();
        RefreshInteractable();
    }

    private void OnEnable()
    {
        RefreshInteractable();
    }

    private void OnDestroy()
    {
        RemoveClickListener();
    }

    public void RefreshInteractable()
    {
        if (button == null)
            AutoBind();

        if (button != null)
            button.interactable = HasBattleContinueSave();
    }

    public void OnClickAbandonBattle()
    {
        if (isProcessing)
            return;

        PlayClickSound();

        if (!HasBattleContinueSave())
        {
            RefreshInteractable();
            TitleManager.RefreshRunButtonsInScene();
            ShowWarning(missingRunMessage);
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[TitleAbandonBattleButton] UIManager is not ready.");
            return;
        }

        UIManager.Instance.ShowConfirmDialog(
            confirmMessage,
            confirmYesText,
            confirmNoText,
            ConfirmAbandonBattle,
            UIManager.Instance.HideConfirmDialog);
    }

    private void ConfirmAbandonBattle()
    {
        if (isProcessing)
            return;

        if (UIManager.Instance != null)
            UIManager.Instance.HideConfirmDialog();

        if (!HasBattleContinueSave())
        {
            RefreshInteractable();
            TitleManager.RefreshRunButtonsInScene();
            ShowWarning(missingRunMessage);
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[TitleAbandonBattleButton] DataManager is not ready.");
            ShowWarning(missingRunMessage);
            return;
        }

        isProcessing = true;

        try
        {
            BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);
            SaveSystem.Instance?.DeleteSaveFile();
            TitleManager.RefreshRunButtonsInScene();
            ShowWarning(completeMessage);
        }
        finally
        {
            isProcessing = false;
            RefreshInteractable();
        }
    }

    private bool HasBattleContinueSave()
    {
        return SaveSystem.Instance != null && SaveSystem.Instance.HasBattleContinueSave();
    }

    private void ShowWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        TitleWarningUI warningUI = TitleWarningUI.Instance;
        if (warningUI == null)
            warningUI = FindFirstObjectByType<TitleWarningUI>(FindObjectsInactive.Include);

        if (warningUI != null)
        {
            warningUI.Show(message);
            return;
        }

        Debug.LogWarning($"[TitleAbandonBattleButton] {message}");
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx);
    }

    private void AutoBind()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void AddClickListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClickAbandonBattle);
        button.onClick.AddListener(OnClickAbandonBattle);
    }

    private void RemoveClickListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClickAbandonBattle);
    }
}
