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
            button.onClick.AddListener(OnClickSelectChapter);
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
            IsRunInitialized = true
        });

        Debug.Log(
            "[MapChapterSelectButton] Chapter Selected / " +
            "Chapter: " + chapterId +
            " / StartStage: " + startStage
        );
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