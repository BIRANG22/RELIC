using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattlePlayButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Option")]
    [SerializeField] private bool checkMapSelected = true;
    [SerializeField] private bool checkPartyExists = true;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickPlay);
            button.onClick.AddListener(OnClickPlay);
        }
    }

    public async void OnClickPlay()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[BattlePlayButton] DataManager is null.");
            return;
        }

        if (checkMapSelected && !IsMapSelected())
        {
            Debug.LogWarning("[BattlePlayButton] 선택된 챕터/스테이지가 없습니다.");
            return;
        }

        if (checkPartyExists && !DataManager.Instance.PartyRuntimeStore.HasAnyCharacter)
        {
            Debug.LogWarning("[BattlePlayButton] 파티에 캐릭터가 없습니다.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[BattlePlayButton] GameManager is null.");
            return;
        }

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
    }

    private bool IsMapSelected()
    {
        MapRuntimeData mapData = DataManager.Instance.MapRuntimeStore.Get();

        if (mapData == null)
            return false;

        if (string.IsNullOrWhiteSpace(mapData.SelectedChapterId))
            return false;

        if (string.IsNullOrWhiteSpace(mapData.CurrentStage))
            return false;

        return true;
    }
}