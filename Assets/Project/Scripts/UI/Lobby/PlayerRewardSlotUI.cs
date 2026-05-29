using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRewardSlotUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text rewardNameText;
    [SerializeField] private TMP_Text stateText;

    [Header("Images")]
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject reachedMarkObj;

    [Header("Visual")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Color unlockedTextColor = Color.white;
    [SerializeField] private Color lockedTextColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private float unlockedAlpha = 1f;
    [SerializeField] private float lockedAlpha = 0.45f;

    private int rewardLevel;
    private bool isUnlocked;

    public int RewardLevel => rewardLevel;
    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetReward(int level, Sprite icon, string rewardName, bool unlocked)
    {
        rewardLevel = level;
        isUnlocked = unlocked;

        if (levelText != null)
            levelText.text = "LV. " + level;

        if (rewardNameText != null)
            rewardNameText.text = string.IsNullOrWhiteSpace(rewardName) ? "Unknown Reward" : rewardName;

        if (stateText != null)
            stateText.text = unlocked ? "해금" : "잠금";

        RefreshIcon(icon, unlocked);
        RefreshLockState(unlocked);
        RefreshCanvasGroup(unlocked);
        RefreshTextColors(unlocked);
    }

    private void RefreshIcon(Sprite icon, bool unlocked)
    {
        if (rewardIconImage == null)
            return;

        rewardIconImage.sprite = icon;
        rewardIconImage.enabled = icon != null;
        rewardIconImage.color = unlocked
            ? Color.white
            : new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    private void RefreshLockState(bool unlocked)
    {
        if (lockObj != null)
            lockObj.SetActive(!unlocked);

        if (reachedMarkObj != null)
            reachedMarkObj.SetActive(unlocked);
    }

    private void RefreshCanvasGroup(bool unlocked)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = unlocked ? unlockedAlpha : lockedAlpha;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    private void RefreshTextColors(bool unlocked)
    {
        Color color = unlocked ? unlockedTextColor : lockedTextColor;

        if (levelText != null)
            levelText.color = color;

        if (rewardNameText != null)
            rewardNameText.color = color;

        if (stateText != null)
            stateText.color = color;
    }
}