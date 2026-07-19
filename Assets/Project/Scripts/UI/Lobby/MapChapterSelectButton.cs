using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private bool allowLockedButtonClick = true;

    [Header("Locked Warning")]
    [SerializeField] private SettingWarningUI warningUI;
    [SerializeField] private string lockedMessage = "아직 잠겨있는 스테이지입니다.";

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

    [Header("Direct Selection")]
    [SerializeField] private bool enterBattleOnDirectSelect;
    [SerializeField] private UnityEvent directSelectionSucceeded;

    [Header("Data Manager")]
    [Tooltip("DataManager가 없는 상태에서 스테이지 선택 저장을 시도했을 때 경고 로그를 출력합니다. 로비씬 단독 테스트 중 로그가 반복되면 꺼둘 수 있습니다.")]
    [SerializeField] private bool logDataManagerMissingWarning = false;

    private Coroutine selectCoroutine;
    private bool isProcessing;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        FindWarningUIIfMissing();
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
        LobbyStageButtonCarousel carousel = GetComponentInParent<LobbyStageButtonCarousel>();

        if (carousel != null)
        {
            if (button == null)
                button = GetComponent<Button>();

            if (carousel.HandleStageButtonClick(button))
                return;
        }

        if (enterBattleOnDirectSelect)
        {
            if (TrySelectChapter(true, false, 0f))
                directSelectionSucceeded?.Invoke();

            return;
        }

        TrySelectChapter(true, closePanelAfterSelect, clickActionDelay);
    }

    public bool SelectChapterForCarousel()
    {
        return TrySelectChapter(false, false, 0f, true);
    }

    private bool TrySelectChapter(bool playSound, bool closeAfterSelect, float delay)
    {
        return TrySelectChapter(playSound, closeAfterSelect, delay, false);
    }

    private bool TrySelectChapter(bool playSound, bool closeAfterSelect, float delay, bool suppressLockedWarning)
    {
        if (isProcessing)
            return false;

        if (isLocked)
        {
            if (!suppressLockedWarning)
            {
                ShowWarning(lockedMessage);
                Debug.Log("[MapChapterSelectButton] Locked stage.");
            }

            return false;
        }

        if (playSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.NormalButtonClick);

        ApplySelectedStageLabel();
        SaveSelectedMapRuntimeData();

        if (!closeAfterSelect)
            return true;

        if (delay <= 0f)
        {
            ClosePanelAfterSelect();
            return true;
        }

        isProcessing = true;
        selectCoroutine = StartCoroutine(ClosePanelAfterDelay(delay));
        return true;
    }

    private IEnumerator ClosePanelAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

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
            if (logDataManagerMissingWarning)
                Debug.LogWarning("[MapChapterSelectButton] DataManager가 없어서 스테이지 선택값을 저장하지 못했습니다. 로비씬을 단독 실행 중이라면 Bootstrap 씬에서 시작하거나 DataManager가 포함된 Bootstrap 오브젝트를 먼저 로드해야 합니다.");

            return;
        }

        if (DataManager.Instance.MapRuntimeStore == null)
        {
            if (logDataManagerMissingWarning)
                Debug.LogWarning("[MapChapterSelectButton] MapRuntimeStore가 없어서 스테이지 선택값을 저장하지 못했습니다.");

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

    public bool IsLocked()
    {
        return isLocked;
    }

    private void RefreshLockState()
    {
        if (lockMark != null)
            lockMark.SetActive(isLocked);

        if (button != null)
            button.interactable = !isLocked || allowLockedButtonClick;
    }

    private void ShowWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        FindWarningUIIfMissing();

        if (warningUI != null)
        {
            warningUI.Show(message);
            return;
        }

        if (SettingWarningUI.Instance != null)
        {
            SettingWarningUI.Instance.Show(message);
            return;
        }

        Debug.LogWarning($"[MapChapterSelectButton] Warning UI is missing. Message: {message}");
    }

    private void FindWarningUIIfMissing()
    {
        if (warningUI != null)
            return;

        warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);
    }
}
