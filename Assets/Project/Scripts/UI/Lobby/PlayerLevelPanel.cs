using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class PlayerLevelPanel : MonoBehaviour
{
    [Header("Reward Panel Root")]
    [SerializeField] private GameObject rewardPanelRoot;
    [SerializeField] private Button closeButton;

    [Header("Objects Active With Reward Panel")]
    [SerializeField] private GameObject[] activeWithRewardPanel;

    [Header("Top Info - Always Visible")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;

    [Header("Reward List")]
    [SerializeField] private Transform rewardContentRoot;
    [SerializeField] private PlayerRewardSlotUI rewardSlotPrefab;
    [SerializeField] private bool rebuildOnOpen = true;

    [Header("Level Test Buttons")]
    [SerializeField] private Button levelMinusButton;
    [SerializeField] private Button levelPlusButton;

    [Header("Common Rune Reward")]
    [SerializeField] private string[] commonRuneRewardIds;

    private bool isBuilt;

    private PlayerRuntimeStore PlayerStore
    {
        get
        {
            if (DataManager.Instance == null)
                return null;

            return DataManager.Instance.PlayerRuntimeStore;
        }
    }

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (levelMinusButton != null)
        {
            levelMinusButton.onClick.RemoveListener(OnClickLevelMinus);
            levelMinusButton.onClick.AddListener(OnClickLevelMinus);
        }

        if (levelPlusButton != null)
        {
            levelPlusButton.onClick.RemoveListener(OnClickLevelPlus);
            levelPlusButton.onClick.AddListener(OnClickLevelPlus);
        }

        Close();
        RefreshTopInfo();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (levelMinusButton != null)
            levelMinusButton.onClick.RemoveListener(OnClickLevelMinus);

        if (levelPlusButton != null)
            levelPlusButton.onClick.RemoveListener(OnClickLevelPlus);
    }

    public void Open()
    {
        SetRewardPanelActive(true);
        Refresh();
    }

    public void Close()
    {
        SetRewardPanelActive(false);
        RefreshTopInfo();
    }

    public void Toggle()
    {
        bool isOpen = rewardPanelRoot != null && rewardPanelRoot.activeSelf;

        if (isOpen)
            Close();
        else
            Open();
    }

    private void SetRewardPanelActive(bool active)
    {
        if (rewardPanelRoot != null)
            rewardPanelRoot.SetActive(active);

        if (activeWithRewardPanel != null)
        {
            for (int i = 0; i < activeWithRewardPanel.Length; i++)
            {
                if (activeWithRewardPanel[i] != null)
                    activeWithRewardPanel[i].SetActive(active);
            }
        }
    }

    public void Refresh()
    {
        RefreshTopInfo();

        if (rewardPanelRoot != null && rewardPanelRoot.activeSelf)
        {
            if (rebuildOnOpen || !isBuilt)
                BuildRewardSlots();
            else
                RefreshRewardSlots();

            RefreshLevelTestButtons();
        }
    }

    private void RefreshTopInfo()
    {
        PlayerRuntimeStore store = PlayerStore;

        if (store == null)
        {
            if (levelText != null)
                levelText.text = "LV. -";

            if (expText != null)
                expText.text = "EXP -";

            return;
        }

        int playerLevel = store.Level;
        int currentTotalExp = store.TotalExp;
        int nextTotalExp = store.GetTotalExpToNextLevel();

        if (levelText != null)
            levelText.text = "LV. " + playerLevel;

        if (expText != null)
        {
            if (store.IsMaxLevel())
                expText.text = "EXP MAX";
            else
                expText.text = "EXP " + currentTotalExp + " / " + nextTotalExp;
        }
    }

    private void RefreshLevelTestButtons()
    {
        PlayerRuntimeStore store = PlayerStore;

        if (store == null)
            return;

        if (levelMinusButton != null)
            levelMinusButton.interactable = store.Level > 1;

        if (levelPlusButton != null)
            levelPlusButton.interactable = store.Level < store.MaxLevelValue;
    }

    private void BuildRewardSlots()
    {
        if (rewardContentRoot == null || rewardSlotPrefab == null)
            return;

        ClearRewardSlots();

        int rewardCount = commonRuneRewardIds != null ? commonRuneRewardIds.Length : 0;

        for (int i = 0; i < rewardCount; i++)
        {
            int rewardLevel = GetCommonRuneUnlockLevel(i);

            PlayerRewardSlotUI slot = Instantiate(rewardSlotPrefab, rewardContentRoot);

            if (slot == null)
                continue;

            SetupRewardSlot(slot, rewardLevel, i);
        }

        isBuilt = true;

        Canvas.ForceUpdateCanvases();
    }

    private void RefreshRewardSlots()
    {
        if (rewardContentRoot == null)
            return;

        int rewardIndex = 0;

        for (int i = 0; i < rewardContentRoot.childCount; i++)
        {
            PlayerRewardSlotUI slot = rewardContentRoot.GetChild(i).GetComponent<PlayerRewardSlotUI>();

            if (slot == null)
                continue;

            int rewardLevel = GetCommonRuneUnlockLevel(rewardIndex);
            SetupRewardSlot(slot, rewardLevel, rewardIndex);
            rewardIndex++;
        }

        Canvas.ForceUpdateCanvases();
    }

    private void SetupRewardSlot(PlayerRewardSlotUI slot, int rewardLevel, int commonRuneIndex)
    {
        PlayerRuntimeStore store = PlayerStore;

        int currentPlayerLevel = store != null ? store.Level : 1;
        bool unlocked = currentPlayerLevel >= rewardLevel;

        RuneData runeData = GetCommonRune(commonRuneIndex);

        Sprite icon = GetRuneIcon(runeData);
        string rewardName = runeData != null ? runeData.Name : "°ø¿ë ·é " + (commonRuneIndex + 1);

        slot.SetReward(
            rewardLevel,
            icon,
            rewardName,
            unlocked
        );
    }

    private RuneData GetCommonRune(int index)
    {
        if (commonRuneRewardIds == null)
            return null;

        if (index < 0 || index >= commonRuneRewardIds.Length)
            return null;

        string runeId = commonRuneRewardIds[index];

        if (string.IsNullOrWhiteSpace(runeId))
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.RuneDatabase == null)
            return null;

        if (DataManager.Instance.RuneDatabase.TryGet(runeId, out var runeData))
            return runeData;

        return null;
    }

    private Sprite GetRuneIcon(RuneData runeData)
    {
        if (runeData == null)
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.RuneIconDatabase == null)
            return null;

        if (DataManager.Instance.RuneIconDatabase.TryGetIcon(runeData.RuneId, out var icon))
            return icon;

        return null;
    }

    private int GetCommonRuneUnlockLevel(int rewardIndex)
    {
        switch (rewardIndex)
        {
            case 0:
                return 2;
            case 1:
                return 4;
            case 2:
                return 6;
            case 3:
                return 8;
            case 4:
                return 10;
            case 5:
                return 12;
            case 6:
                return 14;
            case 7:
                return 16;
            case 8:
                return 18;
            case 9:
                return 20;
            default:
                return 20;
        }
    }

    private void ClearRewardSlots()
    {
        if (rewardContentRoot == null)
            return;

        for (int i = rewardContentRoot.childCount - 1; i >= 0; i--)
            Destroy(rewardContentRoot.GetChild(i).gameObject);

        isBuilt = false;
    }

    private void OnClickLevelMinus()
    {
        PlayerRuntimeStore store = PlayerStore;

        if (store == null)
            return;

        store.AddLevelForTest(-1);
        Refresh();
    }

    private void OnClickLevelPlus()
    {
        PlayerRuntimeStore store = PlayerStore;

        if (store == null)
            return;

        store.AddLevelForTest(1);
        Refresh();
    }
}