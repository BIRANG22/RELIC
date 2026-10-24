using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ExplorationResultCharacterRowUI : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image informationIcon;
    [SerializeField] private TMP_Text damageDealtText;
    [SerializeField] private TMP_Text damageTakenText;
    [SerializeField] private TMP_Text buffAppliedText;
    [SerializeField] private TMP_Text defeatCountText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private GameObject levelUpRoot;
    [SerializeField] private GameObject[] unlockRoots;
    [Header("Report Card")]
    [SerializeField] private BattleBagItemSlotUI iconSlotPrefab;
    [SerializeField] private RectTransform relicSlotRoot;
    [SerializeField] private RectTransform skillSlotRoot;
    [SerializeField] private RectTransform informationHoverTarget;
    [SerializeField] private RectTransform statisticsOverlayAnchor;
    [SerializeField] private ExplorationResultPanelUI statisticsOverlayController;
    [SerializeField, Min(1)] private int maxRelicSlots = 6;
    [SerializeField, Min(1)] private int maxSkillSlots = 4;
    [Header("Relic Icon Layout")]
    [SerializeField, Min(0.01f)] private float relicSlotScale = 0.46f;
    [SerializeField, Min(0f)] private float relicSlotHorizontalSpacing = 52f;
    [SerializeField, Min(0f)] private float relicSlotVerticalSpacing = 52f;
    [SerializeField, Min(1)] private int relicSlotColumns = 3;
    [Header("Skill Icon Layout")]
    [SerializeField, Min(0.01f)] private float skillSlotScale = 0.62f;
    [SerializeField, Min(0f)] private float skillSlotHorizontalSpacing = 66f;
    [SerializeField, Min(0f)] private float skillSlotVerticalSpacing = 66f;
    [SerializeField, Min(1)] private int skillSlotColumns = 3;

    private readonly List<BattleBagItemSlotUI> relicSlots = new();
    private readonly List<BattleBagItemSlotUI> skillSlots = new();
    private string statisticsTooltipContent = string.Empty;

    private void Awake()
    {
        AutoBindSceneReferences();
    }

    public void Bind(
        BattleRunCharacterStatisticsData statistics,
        Sprite portrait,
        CharacterRuntimeData character,
        int displayLevel,
        int gainedExperience = 0,
        bool leveledUp = false,
        float experienceProgress = 0f,
        IReadOnlyList<string> unlockTexts = null,
        ExplorationResultPanelUI overlayController = null)
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

        if (informationIcon != null)
            informationIcon.enabled = informationIcon.sprite != null;

        statisticsOverlayController = overlayController != null
            ? overlayController
            : statisticsOverlayController;
        statisticsTooltipContent = BuildStatisticsTooltip(statistics);
        statisticsOverlayController?.HideStatisticsOverlay(this);
        SetEquipmentSlots(character);
        SetExperience(displayLevel, gainedExperience, leveledUp, experienceProgress);
        SetUnlockTexts(unlockTexts);
    }

    public void Clear()
    {
        statisticsOverlayController?.HideStatisticsOverlay(this);
        statisticsTooltipContent = string.Empty;
        gameObject.SetActive(false);
    }

    private void SetExperience(
        int level,
        int gainedExperience,
        bool leveledUp,
        float experienceProgress)
    {
        int safeExperience = Mathf.Max(0, gainedExperience);

        if (experienceSlider != null)
            experienceSlider.value = Mathf.Clamp01(experienceProgress);

        SetText(experienceText, $"+{safeExperience}");

        if (levelUpRoot != null)
            SetLevelText(level, leveledUp);
    }

    private void SetLevelText(int level, bool leveledUp)
    {
        levelUpRoot.SetActive(true);

        TMP_Text text = levelUpRoot.GetComponent<TMP_Text>();
        if (text == null)
            text = levelUpRoot.GetComponentInChildren<TMP_Text>(true);

        SetText(text, FormatLevelLabel(level));
    }

    public static string FormatLevelLabel(int level)
    {
        return $"LV.{Mathf.Max(1, level):00}";
    }

    public static string BuildStatisticsTooltip(BattleRunCharacterStatisticsData statistics)
    {
        if (statistics == null)
            return string.Empty;

        return $"{GameLocalization.Get("ui.exploration.result.monster_kills", "몬스터 처치")}  {Mathf.Max(0, statistics.KillCount)}\n" +
               $"{GameLocalization.Get("ui.exploration.result.damage_dealt", "입힌 피해량")}  {Mathf.Max(0, statistics.DamageDealt)}\n" +
               $"{GameLocalization.Get("ui.exploration.result.damage_taken", "받은 피해량")}  {Mathf.Max(0, statistics.DamageTaken)}\n" +
               $"{GameLocalization.Get("ui.exploration.result.buffs_applied", "버프 부여량")}  {Mathf.Max(0, statistics.BuffApplied)}\n" +
               $"{GameLocalization.Get("ui.exploration.result.incapacitated", "전투불능 횟수")}  {Mathf.Max(0, statistics.DeathCount)}";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (informationHoverTarget == null || eventData == null)
            return;

        RectTransform overlaySource = statisticsOverlayAnchor ?? informationHoverTarget;
        statisticsOverlayController?.ShowStatisticsOverlay(
            this,
            statisticsTooltipContent,
            overlaySource);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        statisticsOverlayController?.HideStatisticsOverlay(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (informationHoverTarget == null || eventData == null)
            return;

        RectTransform overlaySource = statisticsOverlayAnchor ?? informationHoverTarget;
        statisticsOverlayController?.ToggleStatisticsOverlay(
            this,
            statisticsTooltipContent,
            overlaySource);
    }

    private void SetEquipmentSlots(CharacterRuntimeData character)
    {
        BindIconSlots(relicSlots, relicSlotRoot, character?.EquippedRelicIds, maxRelicSlots, false);
        BindIconSlots(skillSlots, skillSlotRoot, character?.EquippedSkillIds, maxSkillSlots, true);
    }

    private void BindIconSlots(
        List<BattleBagItemSlotUI> slots,
        RectTransform root,
        IReadOnlyList<string> ids,
        int maxCount,
        bool skill)
    {
        if (root == null || iconSlotPrefab == null)
            return;

        List<string> visibleIds = new();
        if (ids != null)
        {
            for (int i = 0; i < ids.Count && visibleIds.Count < maxCount; i++)
            {
                if (!string.IsNullOrWhiteSpace(ids[i]))
                    visibleIds.Add(ids[i].Trim());
            }
        }

        while (slots.Count < maxCount)
        {
            BattleBagItemSlotUI slot = Instantiate(iconSlotPrefab, root, false);
            slot.name = skill ? $"SkillSlot_{slots.Count}" : $"RelicSlot_{slots.Count}";
            RectTransform rect = slot.RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                int columns = Mathf.Max(1, skill ? skillSlotColumns : relicSlotColumns);
                float horizontalSpacing = skill ? skillSlotHorizontalSpacing : relicSlotHorizontalSpacing;
                float verticalSpacing = skill ? skillSlotVerticalSpacing : relicSlotVerticalSpacing;
                rect.anchoredPosition = new Vector2(
                    (slots.Count % columns) * Mathf.Max(0f, horizontalSpacing),
                    -(slots.Count / columns) * Mathf.Max(0f, verticalSpacing));
                rect.localScale = Vector3.one * Mathf.Max(
                    0.01f,
                    skill ? skillSlotScale : relicSlotScale);
            }
            slots.Add(slot);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            BattleBagItemSlotUI slot = slots[i];
            bool filled = i < visibleIds.Count;
            slot.gameObject.SetActive(true);
            if (!filled)
            {
                slot.SetupDisplayIcon(null);
                continue;
            }

            string id = visibleIds[i];
            slot.SetupDisplayIcon(skill ? ResolveSkillIcon(id) : ResolveRelicIcon(id));
        }
    }

    private static Sprite ResolveRelicIcon(string id)
    {
        if (DataManager.Instance?.RelicIconDatabase != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(id, out Sprite icon))
        {
            return icon;
        }

        return null;
    }

    private static Sprite ResolveSkillIcon(string id)
    {
        if (DataManager.Instance?.SkillIconDatabase != null &&
            DataManager.Instance.SkillIconDatabase.TryGetIcon(id, out Sprite icon))
        {
            return icon;
        }

        return null;
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
        informationIcon ??= FindImage("ExplorationReportInformationIcon");
        damageDealtText ??= FindText("DamageDealt");
        damageTakenText ??= FindText("DamageTaken");
        buffAppliedText ??= FindText("BuffApplied");
        defeatCountText ??= FindText("DefeatCount");
        experienceSlider ??= FindComponent<Slider>("ExperienceSlider");
        experienceSlider ??= GetComponentInChildren<Slider>(true);
        experienceText ??= FindText("ExperienceText");
        levelUpRoot ??= FindChild("LevelUp")?.gameObject;
        informationHoverTarget ??= informationIcon != null ? informationIcon.rectTransform : null;
        statisticsOverlayAnchor ??= FindChild("StatisticsOverlayAnchor") as RectTransform;
        statisticsOverlayController ??= GetComponentInParent<ExplorationResultPanelUI>(true);
        relicSlotRoot ??= damageTakenText != null ? damageTakenText.rectTransform : null;
        skillSlotRoot ??= buffAppliedText != null ? buffAppliedText.rectTransform : null;

        if (damageTakenText != null)
            damageTakenText.enabled = false;
        if (buffAppliedText != null)
            buffAppliedText.enabled = false;
        if (defeatCountText != null)
            defeatCountText.gameObject.SetActive(false);

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
