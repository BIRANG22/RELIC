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
        RefreshLockState();

        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnValidate()
    {
        RefreshLockState();
    }

    public async void OnClickSelectChapter()
    {
        if (isLocked)
        {
            Debug.Log("[MapChapterSelectButton] 잠긴 스테이지입니다.");
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        DataManager.Instance.MapRuntimeStore.Set(new MapRuntimeData
        {
            SelectedChapterId = chapterId,
            CurrentStage = startStage,
            IsRunInitialized = true
        });

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
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