using System.Collections;
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

    [Header("Panel")]
    [SerializeField] private bool closePanelAfterSelect = true;
    [SerializeField] private GameObject panelToCloseAfterSelect;

    [Header("Delay")]
    [SerializeField] private float clickActionDelay = 0.2f;

    private Coroutine selectCoroutine;
    private bool isProcessing;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        RefreshLockState();

        if (button != null)
            button.onClick.RemoveListener(OnClickSelectChapter);
    }

    private void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();

        RefreshLockState();
    }

    private void OnDisable()
    {
        if (selectCoroutine != null)
        {
            StopCoroutine(selectCoroutine);
            selectCoroutine = null;
        }

        isProcessing = false;
    }

    public void OnClickSelectChapter()
    {
        if (isProcessing)
            return;

        if (isLocked)
        {
            Debug.Log("[MapChapterSelectButton] Locked stage.");
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.NormalButtonClick);

        ApplySelectedStageLabel();
        SaveSelectedMapRuntimeData();

        if (clickActionDelay <= 0f)
        {
            ClosePanelAfterSelect();
            return;
        }

        isProcessing = true;
        selectCoroutine = StartCoroutine(ClosePanelAfterDelay());
    }

    private IEnumerator ClosePanelAfterDelay()
    {
        yield return new WaitForSecondsRealtime(clickActionDelay);

        ClosePanelAfterSelect();

        isProcessing = false;
        selectCoroutine = null;
    }

    private void ClosePanelAfterSelect()
    {
        if (closePanelAfterSelect && panelToCloseAfterSelect != null)
            panelToCloseAfterSelect.SetActive(false);
    }

    private IEnumerator SelectChapterAfterDelay()
    {
        yield return new WaitForSecondsRealtime(clickActionDelay);

        SelectChapterNow();

        isProcessing = false;
        selectCoroutine = null;
    }

    private void SelectChapterNow()
    {
        ApplySelectedStageLabel();
        SaveSelectedMapRuntimeData();

        if (closePanelAfterSelect && panelToCloseAfterSelect != null)
            panelToCloseAfterSelect.SetActive(false);
    }

    private void ApplySelectedStageLabel()
    {
        if (targetStageText == null)
            return;

        string label = overrideStageLabel;

        if (string.IsNullOrWhiteSpace(label) && sourceStageText != null)
            label = sourceStageText.text;

        if (string.IsNullOrWhiteSpace(label))
            label = startStage;

        targetStageText.text = label;
    }

    private void SaveSelectedMapRuntimeData()
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

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        RefreshLockState();
    }

    private void RefreshLockState()
    {
        if (lockMark != null)
            lockMark.SetActive(isLocked);

        if (button != null)
            button.interactable = !isLocked;
    }
}
