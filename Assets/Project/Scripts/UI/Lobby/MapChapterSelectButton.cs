using Relic.Gameplay.Data;
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

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        RefreshLockState();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickSelectChapter);
        }
    }

    private void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();

        RefreshLockState();
    }

    public void OnClickSelectChapter()
    {
        if (isLocked)
        {
            Debug.Log("[MapChapterSelectButton] 잠긴 스테이지입니다.");
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[MapChapterSelectButton] DataManager is null.");
            return;
        }

        DataManager.Instance.MapRuntimeStore.Set(new MapRuntimeData
        {
            SelectedChapterId = chapterId,
            CurrentStage = startStage,
            CurrentMapId = "",//넣은 위치 id 부터 시작
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