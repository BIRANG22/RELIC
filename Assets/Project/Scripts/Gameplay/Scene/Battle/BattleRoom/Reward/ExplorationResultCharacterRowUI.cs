using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExplorationResultCharacterRowUI : MonoBehaviour
{
    private const string LevelText = "LV";
    private const string LevelUpText = "LV UP";

    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text killCountText;
    [SerializeField] private TMP_Text damageDealtText;
    [SerializeField] private TMP_Text damageTakenText;
    [SerializeField] private TMP_Text buffAppliedText;
    [SerializeField] private TMP_Text defeatCountText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private GameObject levelUpRoot;
    [SerializeField] private GameObject[] unlockRoots;

    private void Awake()
    {
        AutoBindSceneReferences();
    }

    public void Bind(
        BattleRunCharacterStatisticsData statistics,
        Sprite portrait,
        int gainedExperience = 0,
        bool leveledUp = false,
        float experienceProgress = 0f,
        IReadOnlyList<string> unlockTexts = null)
    {
        AutoBindSceneReferences();

        bool hasStatistics = statistics != null;
        gameObject.SetActive(hasStatistics);

        if (!hasStatistics)
            return;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            portraitImage.preserveAspect = true;
        }

        SetText(killCountText, Mathf.Max(0, statistics.KillCount).ToString());
        SetText(damageDealtText, Mathf.Max(0, statistics.DamageDealt).ToString());
        SetText(damageTakenText, Mathf.Max(0, statistics.DamageTaken).ToString());
        SetText(buffAppliedText, Mathf.Max(0, statistics.BuffApplied).ToString());
        SetText(defeatCountText, Mathf.Max(0, statistics.DeathCount).ToString());
        SetExperience(gainedExperience, leveledUp, experienceProgress);
        SetUnlockTexts(unlockTexts);
    }

    public void Clear()
    {
        gameObject.SetActive(false);
    }

    private void SetExperience(
        int gainedExperience,
        bool leveledUp,
        float experienceProgress)
    {
        int safeExperience = Mathf.Max(0, gainedExperience);

        if (experienceSlider != null)
            experienceSlider.value = Mathf.Clamp01(experienceProgress);

        SetText(experienceText, $"+{safeExperience}");

        if (levelUpRoot != null)
            SetLevelUpText(leveledUp);
    }

    private void SetLevelUpText(bool leveledUp)
    {
        levelUpRoot.SetActive(true);

        TMP_Text text = levelUpRoot.GetComponent<TMP_Text>();
        if (text == null)
            text = levelUpRoot.GetComponentInChildren<TMP_Text>(true);

        SetText(text, leveledUp ? LevelUpText : LevelText);
    }

    private void SetUnlockTexts(IReadOnlyList<string> unlockTexts)
    {
        if (unlockRoots == null)
            return;

        int textCount = unlockTexts?.Count ?? 0;
        if (unlockRoots.Length == 1 && textCount > 1)
        {
            SetUnlockRootVisible(unlockRoots[0], true);
            SetUnlockRootText(unlockRoots[0], BuildJoinedUnlockText(unlockTexts));
            return;
        }

        for (int i = 0; i < unlockRoots.Length; i++)
        {
            GameObject root = unlockRoots[i];
            bool visible = root != null && i < textCount && !string.IsNullOrWhiteSpace(unlockTexts[i]);
            SetUnlockRootVisible(root, visible);

            if (visible)
                SetUnlockRootText(root, unlockTexts[i]);
        }
    }

    private static void SetUnlockRootVisible(GameObject root, bool visible)
    {
        if (root != null)
            root.SetActive(visible);
    }

    private static void SetUnlockRootText(GameObject root, string value)
    {
        if (root == null)
            return;

        TMP_Text text = root.GetComponent<TMP_Text>();
        if (text == null)
            text = root.GetComponentInChildren<TMP_Text>(true);

        SetText(text, value);
    }

    private static string BuildJoinedUnlockText(IReadOnlyList<string> unlockTexts)
    {
        if (unlockTexts == null || unlockTexts.Count == 0)
            return string.Empty;

        string result = string.Empty;
        for (int i = 0; i < unlockTexts.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(unlockTexts[i]))
                continue;

            if (result.Length > 0)
                result += "\n";

            result += unlockTexts[i];
        }

        return result;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private void AutoBindSceneReferences()
    {
        portraitImage ??= FindImage("Portrait");
        killCountText ??= FindText("KillCount");
        damageDealtText ??= FindText("DamageDealt");
        damageTakenText ??= FindText("DamageTaken");
        buffAppliedText ??= FindText("BuffApplied");
        defeatCountText ??= FindText("DefeatCount");
        experienceSlider ??= FindComponent<Slider>("ExperienceSlider");
        experienceSlider ??= GetComponentInChildren<Slider>(true);
        experienceText ??= FindText("ExperienceText");
        levelUpRoot ??= FindChild("LevelUp")?.gameObject;

        if (unlockRoots == null || unlockRoots.Length == 0)
        {
            Transform unlockRoot = FindChild("Unlocks");
            if (unlockRoot != null)
            {
                List<GameObject> roots = new(unlockRoot.childCount);
                for (int i = 0; i < unlockRoot.childCount; i++)
                    roots.Add(unlockRoot.GetChild(i).gameObject);

                unlockRoots = roots.ToArray();
            }
        }
    }

    private TMP_Text FindText(string objectName)
    {
        return FindComponent<TMP_Text>(objectName);
    }

    private Image FindImage(string objectName)
    {
        return FindComponent<Image>(objectName);
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        Transform child = FindChild(objectName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindChild(string objectName)
    {
        return FindChildRecursive(transform, objectName);
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == objectName)
                return child;

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
