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

    [Header("Warning UI")]
    [SerializeField] private SettingWarningUI warningUI;
    [SerializeField] private string mapNotSelectedMessage = "스테이지를 선택해야 합니다.";
    [SerializeField] private string partyEmptyMessage = "캐릭터를 편성해야 합니다.";
    [SerializeField] private string dataManagerMissingMessage = "데이터 매니저가 없습니다.";
    [SerializeField] private string gameManagerMissingMessage = "게임 매니저가 없습니다.";

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    private bool isProcessing;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        FindWarningUIIfMissing();
    }

    private void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public async void OnClickPlay()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        if (button != null)
            button.interactable = false;

        try
        {
            PlayClickSound();

            if (DataManager.Instance == null)
            {
                ShowWarning(dataManagerMissingMessage);
                Debug.LogWarning("[BattlePlayButton] DataManager is null.");
                return;
            }

            if (checkMapSelected && !IsMapSelected())
            {
                ShowWarning(mapNotSelectedMessage);
                Debug.LogWarning("[BattlePlayButton] 선택된 챕터/스테이지가 없습니다.");
                return;
            }

            if (checkPartyExists && !DataManager.Instance.PartyRuntimeStore.HasAnyCharacter)
            {
                ShowWarning(partyEmptyMessage);
                Debug.LogWarning("[BattlePlayButton] 파티에 캐릭터가 없습니다.");
                return;
            }

            if (GameManager.Instance == null)
            {
                ShowWarning(gameManagerMissingMessage);
                Debug.LogWarning("[BattlePlayButton] GameManager is null.");
                return;
            }

            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
        }
        finally
        {
            isProcessing = false;

            if (button != null)
                button.interactable = true;
        }
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx);
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

        Debug.LogWarning($"[BattlePlayButton] Warning UI is missing. Message: {message}");
    }

    private void FindWarningUIIfMissing()
    {
        if (warningUI != null)
            return;

        warningUI = FindFirstObjectByType<SettingWarningUI>(FindObjectsInactive.Include);
    }

    private bool IsMapSelected()
    {
        MapRuntimeData mapData = DataManager.Instance.MapRuntimeStore.Get();

        return mapData != null &&
               !string.IsNullOrWhiteSpace(mapData.SelectedChapterId) &&
               !string.IsNullOrWhiteSpace(mapData.CurrentStage);
    }
}
