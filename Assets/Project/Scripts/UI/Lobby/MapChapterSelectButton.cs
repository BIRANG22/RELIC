using Relic.Gameplay.Data;
using UnityEngine;

public class MapChapterSelectButton : MonoBehaviour
{
    [Header("Chapter")]
    [SerializeField] private string chapterId;

    [Header("Start")]
    [SerializeField] private string startStage;

    public async void OnClickSelectChapter()
    {
        AudioManager.Instance.PlaySfx(SfxType.Click);

        DataManager.Instance.MapRuntimeStore.Set(new MapRuntimeData
        {
            SelectedChapterId = chapterId,
            CurrentStage = startStage,
            IsRunInitialized = true
        });

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
    }
}