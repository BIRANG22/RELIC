using UnityEngine;
using UnityEngine.UI;

public class TitleContinueButton : MonoBehaviour
{
    [SerializeField] private GameObject lockedImage;
    [SerializeField] private string missingContinueMessage = "이어서 할 정보가 없음";

    [Header("Sound")]
    [SerializeField] private bool playClickSound;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    private Button button;
    private bool isContinuing;

    private void Awake()
    {
        AutoBind();
        AddClickListener();
        RefreshLockState();
    }

    private void OnEnable()
    {
        RefreshLockState();
    }

    private void OnDestroy()
    {
        RemoveClickListener();
    }

    public void RefreshLockState()
    {
        bool canContinue = HasBattleContinueSave();

        if (button != null)
        {
            button.interactable = canContinue;
        }

        if (lockedImage != null)
        {
            lockedImage.SetActive(!canContinue);
        }
    }

    public async void OnClickContinue()
    {
        if (isContinuing)
            return;

        PlayClickSound();

        if (SaveSystem.Instance == null ||
            !SaveSystem.Instance.TryLoadBattleContinueProgress())
        {
            RefreshLockState();
            TitleManager.RefreshRunButtonsInScene();
            ShowMissingContinueWarning();
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
        {
            Debug.LogWarning("[TitleContinueButton] GameManager is not ready.");
            ShowMissingContinueWarning();
            return;
        }

        isContinuing = true;
        TitleManager.CloseTitleModePanelsInScene();

        try
        {
            await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
        }
        finally
        {
            isContinuing = false;
        }
    }

    private bool HasBattleContinueSave()
    {
        return SaveSystem.Instance != null &&
               SaveSystem.Instance.HasBattleContinueSave();
    }

    private void ShowMissingContinueWarning()
    {
        TitleWarningUI warningUI = TitleWarningUI.Instance;

        if (warningUI == null)
            warningUI = FindFirstObjectByType<TitleWarningUI>(FindObjectsInactive.Include);

        if (warningUI != null)
        {
            warningUI.Show(missingContinueMessage);
            return;
        }

        Debug.LogWarning($"[TitleContinueButton] {missingContinueMessage}");
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

        if (lockedImage == null)
            lockedImage = FindChildByName("lockedImage");
    }

    private GameObject FindChildByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null || child == transform)
                continue;

            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private void AddClickListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClickContinue);
        button.onClick.AddListener(OnClickContinue);
    }

    private void RemoveClickListener()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClickContinue);
    }
}
