using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapChapterSelectButton : MonoBehaviour
{
    [Header("Chapter")]
    [SerializeField] private string chapterId;

    [Header("Start")]
    [SerializeField] private string startStage;

    [Header("Lock")]
    [SerializeField] private bool isLocked;
    [SerializeField] private GameObject lockMark;

    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Selected Stage Label")]
    [SerializeField] private TMP_Text sourceStageText;
    [SerializeField] private TMP_Text targetStageText;
    [SerializeField] private string overrideStageLabel;
    [SerializeField] private bool useStartStageWhenLabelIsEmpty = true;

    [Header("Panel")]
    [SerializeField] private bool closePanelAfterSelect = true;
    [SerializeField] private GameObject panelToCloseAfterSelect;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (sourceStageText == null)
            sourceStageText = GetComponentInChildren<TMP_Text>(true);

        RefreshLockState();

        if (button != null)
            button.onClick.RemoveListener(OnClickSelectChapter);
    }

    private void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (sourceStageText == null)
            sourceStageText = GetComponentInChildren<TMP_Text>(true);

        RefreshLockState();
    }

    public void OnClickSelectChapter()
    {
        if (isLocked)
        {
            Debug.Log("[MapChapterSelectButton] This stage is locked.");
            return;
        }

        PlayClickSound();
        ApplySelectedStageLabel();
        ClosePanelIfNeeded();
        SaveSelectedChapter();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        RefreshLockState();
    }

    private void ApplySelectedStageLabel()
    {
        if (targetStageText == null)
            return;

        string label = GetSelectedStageLabel();
        targetStageText.text = label;
    }

    private string GetSelectedStageLabel()
    {
        if (!string.IsNullOrWhiteSpace(overrideStageLabel))
            return overrideStageLabel;

        if (sourceStageText != null && !string.IsNullOrWhiteSpace(sourceStageText.text))
            return sourceStageText.text;

        if (useStartStageWhenLabelIsEmpty && !string.IsNullOrWhiteSpace(startStage))
            return startStage;

        return targetStageText != null ? targetStageText.text : string.Empty;
    }

    private void ClosePanelIfNeeded()
    {
        if (!closePanelAfterSelect)
            return;

        if (panelToCloseAfterSelect != null)
            panelToCloseAfterSelect.SetActive(false);
    }

    private void SaveSelectedChapter()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[MapChapterSelectButton] DataManager is null.");
            return;
        }

        DataManager.Instance.MapRuntimeStore.Set(new MapRuntimeData
        {
            SelectedChapterId = chapterId,
            CurrentStage = startStage,
            CurrentMapId = "",
            CurrentSceneName = SceneName.Battle,
            IsRunInitialized = false
        });
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx);
    }

    private void RefreshLockState()
    {
        if (lockMark != null)
            lockMark.SetActive(isLocked);

        if (button != null)
            button.interactable = !isLocked;
    }
}
