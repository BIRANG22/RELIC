using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// BattleCharacterPanel 안에서 선택된 몬스터의 핵심 정보와 보유 스킬을 표시합니다.
/// MonsterInfo / SkillList / SkillInfo 하위 UI를 이름으로 자동 연결합니다.
/// </summary>
public class BattleMonsterInfoPanelUI : MonoBehaviour
{
    private const int MonsterSkillSlotCount = 8;
    private const int MonsterSkillEffectSlotCount = 3;

    [Header("Monster Info")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    [Header("HP Info")]
    [SerializeField] private Image hpIconImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpFillImage;

    [Header("Armor Info")]
    [SerializeField] private GameObject armorObject;
    [SerializeField] private Image armorIconImage;
    [SerializeField] private TMP_Text armorText;

    [Header("Action Range")]
    [Tooltip("ActionRange/RangeIcon 이미지입니다. 몬스터의 AttackRangeId에 해당하는 범위 이미지를 표시합니다.")]
    [SerializeField] private Image actionRangeImage;

    [Header("Special Actions")]
    [Tooltip("SpecialAction/Effect01 텍스트입니다.")]
    [SerializeField] private TMP_Text specialAction1Text;
    [Tooltip("SpecialAction/Effect02 텍스트입니다.")]
    [SerializeField] private TMP_Text specialAction2Text;

    [Header("Status Effects")]
    [SerializeField] private RectTransform statusEffectListRoot;
    [SerializeField] private StatusEffectIcon statusEffectIconPrefab;

    [Header("Portrait")]
    [Tooltip("MonsterIconDatabase에서 일반 Icon을 찾지 못했을 때만 월드 스프라이트를 예비 초상화로 사용합니다.")]
    [SerializeField] private bool useWorldSpriteAsPortraitFallback = true;

    [Header("Monster Skill List")]
    [Tooltip("SkillList 오브젝트입니다. 비어 있으면 자동으로 찾습니다.")]
    [SerializeField] private Transform skillListRoot;
    [SerializeField] private Button[] skillButtons = new Button[MonsterSkillSlotCount];
    [SerializeField] private Image[] skillBackgroundImages = new Image[MonsterSkillSlotCount];
    [SerializeField] private Image[] skillHoverBackgroundImages = new Image[MonsterSkillSlotCount];
    [SerializeField] private TMP_Text[] skillNameTexts = new TMP_Text[MonsterSkillSlotCount];
    [SerializeField] private Color inactiveSkillColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color selectedSkillColor = new Color32(0xDF, 0x4E, 0x56, 0xFF);

    [Header("Monster Skill Icon Database")]
    [SerializeField] private MonsterSkillIconDatabase monsterSkillIconDatabase;

    [Header("Monster Skill Info")]
    [SerializeField] private Transform skillInfoRoot;
    [SerializeField] private Image skillInfoIconImage;
    [SerializeField] private Image skillInfoRangeImage;
    [SerializeField] private TMP_Text skillInfoNameText;
    [SerializeField] private TMP_Text skillInfoTypeText;
    [SerializeField] private TMP_Text skillInfoDetailsText;

    [Header("Monster Skill Effects")]
    [SerializeField] private GameObject[] skillEffectRoots = new GameObject[MonsterSkillEffectSlotCount];
    [SerializeField] private TMP_Text[] skillEffectNameTexts = new TMP_Text[MonsterSkillEffectSlotCount];
    [SerializeField] private TMP_Text[] skillEffectValueTexts = new TMP_Text[MonsterSkillEffectSlotCount];

    private readonly List<StatusEffectIcon> spawnedStatusEffectIcons = new();
    private readonly MonsterSkillData[] displayedSkills = new MonsterSkillData[MonsterSkillSlotCount];
    private readonly UnityAction[] skillClickActions = new UnityAction[MonsterSkillSlotCount];
    private readonly Color[] originalSkillBackgroundColors = new Color[MonsterSkillSlotCount];
    private readonly Color[] originalSkillNameColors = new Color[MonsterSkillSlotCount];
    private readonly bool[] originalSkillColorsCached = new bool[MonsterSkillSlotCount];

    private MonsterUnit boundMonster;
    private MonsterRuntimeData boundRuntime;
    private int selectedSkillIndex = -1;
    private bool skillListenersRegistered;

    private int lastHp = int.MinValue;
    private int lastMaxHp = int.MinValue;
    private int lastArmor = int.MinValue;
    private int lastStatusHash = int.MinValue;

    private void Awake()
    {
        EnsureArraySizes();
        ResolveReferences();
        RegisterSkillButtonListeners();
        RegisterSkillHoverEvents();
        ClearSkillListAndInfo();
    }

    private void OnEnable()
    {
        EnsureArraySizes();
        ResolveReferences();
        RegisterSkillButtonListeners();
        RegisterSkillHoverEvents();
        Refresh(true);
    }

    private void OnDisable()
    {
        ClearStatusEffects();
        boundMonster = null;
        boundRuntime = null;
        selectedSkillIndex = -1;
        ResetCachedValues();
        ClearSkillListAndInfo();
    }

    private void OnDestroy()
    {
        UnregisterSkillButtonListeners();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureArraySizes();
    }
#endif

    private void Update()
    {
        Refresh(false);
    }

    public void ConfigureStatusEffectPrefab(StatusEffectIcon prefab)
    {
        if (prefab != null)
            statusEffectIconPrefab = prefab;
    }

    public void Bind(MonsterUnit monster)
    {
        boundMonster = monster;
        boundRuntime = monster != null ? monster.RuntimeData : null;
        selectedSkillIndex = -1;

        ResetCachedValues();
        EnsureArraySizes();
        ResolveReferences();
        RegisterSkillButtonListeners();
        RegisterSkillHoverEvents();
        Refresh(true);
    }

    public void Clear()
    {
        boundMonster = null;
        boundRuntime = null;
        selectedSkillIndex = -1;
        ResetCachedValues();

        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        if (nameText != null)
            nameText.text = string.Empty;

        if (hpText != null)
            hpText.text = string.Empty;

        if (hpFillImage != null)
            hpFillImage.fillAmount = 0f;

        if (armorText != null)
            armorText.text = string.Empty;

        if (armorObject != null)
            armorObject.SetActive(false);

        SetImage(actionRangeImage, null);

        if (specialAction1Text != null)
            specialAction1Text.text = string.Empty;

        if (specialAction2Text != null)
            specialAction2Text.text = string.Empty;

        ClearStatusEffects();
        ClearSkillListAndInfo();
    }

    private void Refresh(bool force)
    {
        if (boundMonster != null)
            boundRuntime = boundMonster.RuntimeData;

        if (boundRuntime == null)
            return;

        if (force)
        {
            if (nameText != null)
                nameText.text = boundRuntime.GetDisplayName();

            RefreshPortrait();
            RefreshActionRangeAndSpecialActions();
            RefreshMonsterSkillList();
        }

        if (force || lastHp != boundRuntime.CurrentHP || lastMaxHp != boundRuntime.MaxHP)
        {
            if (hpText != null)
                hpText.text = $"{Mathf.Max(0, boundRuntime.CurrentHP)}/{Mathf.Max(0, boundRuntime.MaxHP)}";

            if (hpFillImage != null)
                hpFillImage.fillAmount = Mathf.Clamp01(boundRuntime.GetHPPercent());

            lastHp = boundRuntime.CurrentHP;
            lastMaxHp = boundRuntime.MaxHP;
        }

        if (force || lastArmor != boundRuntime.CurrentShield)
        {
            if (armorText != null)
                armorText.text = Mathf.Max(0, boundRuntime.CurrentShield).ToString();

            if (armorObject != null)
                armorObject.SetActive(boundRuntime.CurrentShield > 0);

            lastArmor = boundRuntime.CurrentShield;
        }

        int statusHash = CalculateStatusHash(boundRuntime.StatusEffects);
        if (force || statusHash != lastStatusHash)
        {
            RebuildStatusEffects(boundRuntime.StatusEffects);
            lastStatusHash = statusHash;
        }
    }

    private void RefreshPortrait()
    {
        if (portraitImage == null)
            return;

        Sprite portrait = null;

        if (boundRuntime != null &&
            !string.IsNullOrWhiteSpace(boundRuntime.MonsterId) &&
            DataManager.Instance != null &&
            DataManager.Instance.MonsterIconDatabase != null)
        {
            DataManager.Instance.MonsterIconDatabase.TryGetIcon(
                boundRuntime.MonsterId,
                out portrait
            );
        }

        if (portrait == null && useWorldSpriteAsPortraitFallback && boundMonster != null)
        {
            SpriteRenderer renderer = boundMonster.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null)
                portrait = renderer.sprite;
        }

        portraitImage.sprite = portrait;
        portraitImage.enabled = portrait != null;
        portraitImage.preserveAspect = true;
    }


    private void RefreshActionRangeAndSpecialActions()
    {
        ResolveActionRangeAndSpecialActionReferences();

        object monsterMasterData = ResolveBoundMonsterMasterData();
        if (monsterMasterData == null)
        {
            SetImage(actionRangeImage, null);

            if (specialAction1Text != null)
                specialAction1Text.text = string.Empty;

            if (specialAction2Text != null)
                specialAction2Text.text = string.Empty;

            return;
        }

        string attackRangeId = GetStringMemberValue(monsterMasterData, "AttackRangeId");
        if (IsEmptyDataValue(attackRangeId))
            attackRangeId = GetStringMemberValue(monsterMasterData, "AttackRange");

        SetImage(actionRangeImage, ResolveRangeIcon(attackRangeId));

        if (specialAction1Text != null)
            specialAction1Text.text = NormalizeDisplayText(GetStringMemberValue(monsterMasterData, "SpecialAction1"));

        if (specialAction2Text != null)
            specialAction2Text.text = NormalizeDisplayText(GetStringMemberValue(monsterMasterData, "SpecialAction2"));
    }

    private void RefreshMonsterSkillList()
    {
        EnsureArraySizes();
        ResolveSkillListReferences();

        bool selectedSkillStillExists = false;
        int firstValidSkillIndex = -1;

        for (int i = 0; i < MonsterSkillSlotCount; i++)
        {
            string skillId = ResolvePossessedMonsterSkillId(i);
            MonsterSkillData skillData = ResolveMonsterSkillData(skillId);
            displayedSkills[i] = skillData;

            bool hasSkill = skillData != null;
            Button button = skillButtons[i];
            TMP_Text skillNameText = skillNameTexts[i];

            if (button != null)
            {
                button.gameObject.SetActive(true);
                button.interactable = hasSkill;
            }

            ApplySkillSlotVisualState(i, hasSkill);

            if (!hasSkill)
                continue;

            if (firstValidSkillIndex < 0)
                firstValidSkillIndex = i;

            if (selectedSkillIndex == i)
                selectedSkillStillExists = true;
        }

        if (selectedSkillStillExists &&
            selectedSkillIndex >= 0 &&
            selectedSkillIndex < displayedSkills.Length)
        {
            RefreshSkillSelectionVisuals();
            ShowMonsterSkillInfo(displayedSkills[selectedSkillIndex]);
            return;
        }

        selectedSkillIndex = firstValidSkillIndex;
        RefreshSkillSelectionVisuals();

        if (selectedSkillIndex >= 0)
            ShowMonsterSkillInfo(displayedSkills[selectedSkillIndex]);
        else
            ClearMonsterSkillInfo();
    }

    public bool SelectSkillById(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        EnsureArraySizes();
        ResolveReferences();
        RefreshMonsterSkillList();

        for (int i = 0; i < displayedSkills.Length; i++)
        {
            MonsterSkillData skillData = displayedSkills[i];
            if (skillData == null || !string.Equals(skillData.SkillId, skillId, StringComparison.Ordinal))
                continue;

            selectedSkillIndex = i;
            RefreshSkillSelectionVisuals();
            ShowMonsterSkillInfo(skillData);
            return true;
        }

        return false;
    }

    private void HandleSkillButtonClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= displayedSkills.Length)
            return;

        MonsterSkillData skillData = displayedSkills[slotIndex];
        if (skillData == null)
            return;

        selectedSkillIndex = slotIndex;
        RefreshSkillSelectionVisuals();
        ShowMonsterSkillInfo(skillData);
    }

    private void ShowMonsterSkillInfo(MonsterSkillData skillData)
    {
        if (skillData == null)
        {
            ClearMonsterSkillInfo();
            return;
        }

        ResolveSkillInfoReferences();

        SetImage(skillInfoIconImage, ResolveMonsterSkillIcon(skillData));
        SetImage(skillInfoRangeImage, ResolveRangeIcon(skillData.RangeId));

        if (skillInfoNameText != null)
        {
            skillInfoNameText.text = !string.IsNullOrWhiteSpace(skillData.Name)
                ? skillData.Name
                : skillData.SkillId;
        }

        if (skillInfoTypeText != null)
            skillInfoTypeText.text = GetStringMemberValue(skillData, "SkillType");

        if (skillInfoDetailsText != null)
            skillInfoDetailsText.text = FormatMonsterEffectDescription(skillData.EffectDesc, skillData);

        RefreshMonsterSkillEffects(skillData);
    }

    private static string FormatMonsterEffectDescription(string description, MonsterSkillData skillData)
    {
        return MonsterSkillDescriptionFormatter.Format(description, skillData);
    }

    private void RefreshMonsterSkillEffects(MonsterSkillData skillData)
    {
        EnsureArraySizes();
        ResolveSkillEffectReferences();

        string[] effectIds = SplitSemicolonValues(skillData != null ? skillData.EffectIds : null);
        string[] values = SplitSemicolonValues(skillData != null ? skillData.ValueRate : null);

        for (int i = 0; i < MonsterSkillEffectSlotCount; i++)
        {
            bool hasEffect = i < effectIds.Length && !IsEmptyDataValue(effectIds[i]);

            if (skillEffectRoots[i] != null)
                skillEffectRoots[i].SetActive(hasEffect);

            if (!hasEffect)
            {
                if (skillEffectNameTexts[i] != null)
                    skillEffectNameTexts[i].text = string.Empty;

                if (skillEffectValueTexts[i] != null)
                    skillEffectValueTexts[i].text = string.Empty;

                continue;
            }

            string effectId = effectIds[i].Trim();
            string value = i < values.Length ? values[i].Trim() : string.Empty;

            if (skillEffectNameTexts[i] != null)
                skillEffectNameTexts[i].text = GetMonsterEffectDisplayName(effectId);

            if (skillEffectValueTexts[i] != null)
                skillEffectValueTexts[i].text = IsEmptyDataValue(value) ? string.Empty : value;
        }
    }

    private string ResolvePossessedMonsterSkillId(int slotIndex)
    {
        if (boundRuntime == null || DataManager.Instance == null)
            return string.Empty;

        if (slotIndex < 0 || slotIndex >= MonsterSkillSlotCount)
            return string.Empty;

        object monsterMasterData = ResolveBoundMonsterMasterData();
        if (monsterMasterData == null)
            return string.Empty;

        string memberName = $"PossSkillId{slotIndex + 1:00}";
        string skillId = GetStringMemberValue(monsterMasterData, memberName);

        return IsEmptyDataValue(skillId) ? string.Empty : skillId.Trim();
    }

    private object ResolveBoundMonsterMasterData()
    {
        if (boundRuntime == null ||
            string.IsNullOrWhiteSpace(boundRuntime.MonsterId) ||
            DataManager.Instance == null)
        {
            return null;
        }

        object monsterDatabase = GetMemberValue(DataManager.Instance, "MonsterDatabase");
        if (monsterDatabase == null)
            return null;

        return InvokeDatabaseGet(monsterDatabase, boundRuntime.MonsterId);
    }

    private static object InvokeDatabaseGet(object database, string id)
    {
        if (database == null || string.IsNullOrWhiteSpace(id))
            return null;

        Type databaseType = database.GetType();
        MethodInfo getMethod = databaseType.GetMethod(
            "Get",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null
        );

        if (getMethod != null)
        {
            try
            {
                return getMethod.Invoke(database, new object[] { id });
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        MethodInfo[] methods = databaseType.GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method.Name != "TryGet")
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(string))
                continue;

            object[] args = { id, null };

            try
            {
                object result = method.Invoke(database, args);
                if (result is bool success && success)
                    return args[1];
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        return null;
    }

    private static MonsterSkillData ResolveMonsterSkillData(string skillId)
    {
        if (IsEmptyDataValue(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.MonsterSkillDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.MonsterSkillDatabase.Get(skillId.Trim());
    }

    private Sprite ResolveMonsterSkillIcon(MonsterSkillData skillData)
    {
        if (skillData == null || monsterSkillIconDatabase == null)
            return null;

        string iconKey = GetStringMemberValue(skillData, "SkillIcon");
        if (IsEmptyDataValue(iconKey))
            return null;

        return monsterSkillIconDatabase.TryGetIcon(iconKey.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    private static Sprite ResolveRangeIcon(string rangeId)
    {
        if (IsEmptyDataValue(rangeId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillRangeIconDatabase == null)
        {
            return null;
        }

        DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId.Trim(), out Sprite icon);
        return icon;
    }

    private static string GetMonsterEffectDisplayName(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return string.Empty;

        switch (effectId.Trim())
        {
            case "E_Move":
                return "이동";
            case "E_Strike":
            case "E_Pierce":
                return "피해";
            case "E_Knockback":
                return "밀어냄";
            case "E_Grab":
                return "끌어당김";
            case "E_Grudge":
                return "원한";
            case "E_Corrosion":
                return "침식";
            case "E_Spawn_Spider_Egg":
                return "거미알 생성";
            case "E_Spawn_Spider_Web":
                return "거미줄 생성";
            case "E_Barrier":
                return "장막";
            default:
                return effectId.Trim();
        }
    }

    private void ClearSkillListAndInfo()
    {
        EnsureArraySizes();
        ResolveSkillListReferences();

        for (int i = 0; i < MonsterSkillSlotCount; i++)
        {
            displayedSkills[i] = null;

            if (skillButtons[i] != null)
            {
                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].interactable = false;
            }

            ApplySkillSlotVisualState(i, false);
        }

        selectedSkillIndex = -1;
        ClearMonsterSkillInfo();
    }

    private void ClearMonsterSkillInfo()
    {
        ResolveSkillInfoReferences();
        ResolveSkillEffectReferences();

        SetImage(skillInfoIconImage, null);
        SetImage(skillInfoRangeImage, null);

        if (skillInfoNameText != null)
            skillInfoNameText.text = string.Empty;

        if (skillInfoTypeText != null)
            skillInfoTypeText.text = string.Empty;

        if (skillInfoDetailsText != null)
            skillInfoDetailsText.text = string.Empty;

        for (int i = 0; i < MonsterSkillEffectSlotCount; i++)
        {
            if (skillEffectRoots[i] != null)
                skillEffectRoots[i].SetActive(false);

            if (skillEffectNameTexts[i] != null)
                skillEffectNameTexts[i].text = string.Empty;

            if (skillEffectValueTexts[i] != null)
                skillEffectValueTexts[i].text = string.Empty;
        }
    }

    private void RegisterSkillHoverEvents()
    {
        ResolveSkillListReferences();

        for (int i = 0; i < MonsterSkillSlotCount; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            trigger.triggers ??= new List<EventTrigger.Entry>();

            int capturedIndex = i;
            AddOrReplacePointerEvent(
                trigger,
                EventTriggerType.PointerEnter,
                data => HandleSkillPointerEnter(capturedIndex));
            AddOrReplacePointerEvent(
                trigger,
                EventTriggerType.PointerExit,
                data => HandleSkillPointerExit(capturedIndex));
        }
    }

    private static void AddOrReplacePointerEvent(
        EventTrigger trigger,
        EventTriggerType eventType,
        UnityAction<BaseEventData> callback)
    {
        if (trigger == null || callback == null)
            return;

        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            EventTrigger.Entry existing = trigger.triggers[i];
            if (existing != null && existing.eventID == eventType)
                trigger.triggers.RemoveAt(i);
        }

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private void HandleSkillPointerEnter(int slotIndex)
    {
        if (!IsUsableSkillSlot(slotIndex))
            return;

        Image hoverBackground = skillHoverBackgroundImages[slotIndex];
        if (hoverBackground != null)
            hoverBackground.gameObject.SetActive(true);

        Image background = skillBackgroundImages[slotIndex];
        if (background != null)
            background.color = selectedSkillColor;
    }

    private void HandleSkillPointerExit(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MonsterSkillSlotCount)
            return;

        Image hoverBackground = skillHoverBackgroundImages[slotIndex];
        if (hoverBackground != null)
            hoverBackground.gameObject.SetActive(false);

        ApplySkillSlotVisualState(slotIndex, displayedSkills[slotIndex] != null);
    }

    private bool IsUsableSkillSlot(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < MonsterSkillSlotCount &&
               displayedSkills[slotIndex] != null &&
               skillButtons[slotIndex] != null &&
               skillButtons[slotIndex].interactable;
    }

    private void RefreshSkillSelectionVisuals()
    {
        for (int i = 0; i < MonsterSkillSlotCount; i++)
            ApplySkillSlotVisualState(i, displayedSkills[i] != null);
    }

    private void RegisterSkillButtonListeners()
    {
        if (skillListenersRegistered)
            return;

        ResolveSkillListReferences();

        for (int i = 0; i < MonsterSkillSlotCount; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;

            int capturedIndex = i;
            skillClickActions[i] = () => HandleSkillButtonClicked(capturedIndex);
            button.onClick.AddListener(skillClickActions[i]);
        }

        skillListenersRegistered = true;
    }

    private void UnregisterSkillButtonListeners()
    {
        if (!skillListenersRegistered)
            return;

        for (int i = 0; i < MonsterSkillSlotCount; i++)
        {
            if (skillButtons[i] != null && skillClickActions[i] != null)
                skillButtons[i].onClick.RemoveListener(skillClickActions[i]);

            skillClickActions[i] = null;
        }

        skillListenersRegistered = false;
    }

    private void RebuildStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        ClearStatusEffects();

        if (statusEffectListRoot == null || statusEffectIconPrefab == null || statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData statusEffect = statusEffects[i];
            if (statusEffect == null || !statusEffect.IsValid())
                continue;

            StatusEffectIcon icon = Instantiate(statusEffectIconPrefab, statusEffectListRoot);
            icon.gameObject.name = $"StatusEffect_{statusEffect.EffectId}";
            icon.transform.localScale = Vector3.one;
            icon.Set(statusEffect);
            spawnedStatusEffectIcons.Add(icon);
        }
    }

    private void ClearStatusEffects()
    {
        for (int i = 0; i < spawnedStatusEffectIcons.Count; i++)
        {
            StatusEffectIcon icon = spawnedStatusEffectIcons[i];
            if (icon != null)
                Destroy(icon.gameObject);
        }

        spawnedStatusEffectIcons.Clear();
    }

    private void ResolveReferences()
    {
        if (portraitImage == null)
            portraitImage = FindImage("Portrait");

        if (nameText == null)
            nameText = FindText("Name");

        if (hpText == null)
            hpText = FindText("HpInfo");

        if (armorText == null)
            armorText = FindText("ArmorInfo");

        ResolveActionRangeAndSpecialActionReferences();

        if (statusEffectListRoot == null)
        {
            Transform statusRoot = FindChildRecursive(transform, "StatusEffectList");
            if (statusRoot != null)
                statusEffectListRoot = statusRoot as RectTransform;
        }

        ResolveSkillListReferences();
        ResolveSkillInfoReferences();
        ResolveSkillEffectReferences();
    }

    private void ResolveActionRangeAndSpecialActionReferences()
    {
        Transform actionRangeRoot = FindChildRecursive(transform, "ActionRange");
        if (actionRangeImage == null && actionRangeRoot != null)
            actionRangeImage = FindImageUnder(actionRangeRoot, "RangeIcon");

        Transform specialActionRoot = FindChildRecursive(transform, "SpecialAction");
        if (specialActionRoot == null)
            return;

        if (specialAction1Text == null)
            specialAction1Text = FindTextUnder(specialActionRoot, "Effect01");

        if (specialAction2Text == null)
            specialAction2Text = FindTextUnder(specialActionRoot, "Effect02");
    }

    private void ResolveSkillListReferences()
    {
        EnsureArraySizes();

        if (skillListRoot == null)
            skillListRoot = FindChildRecursive(transform, "SkillList");

        if (skillListRoot == null)
            return;

        for (int i = 0; i < MonsterSkillSlotCount; i++)
        {
            string slotName = $"Skill{i + 1:00}";
            Transform slot = FindChildRecursive(skillListRoot, slotName);

            if (slot == null)
                continue;

            if (skillButtons[i] == null)
                skillButtons[i] = slot.GetComponent<Button>();

            if (skillBackgroundImages[i] == null)
            {
                Transform backgroundTransform = FindChildRecursive(slot, "Skill_Background");
                if (backgroundTransform != null)
                    skillBackgroundImages[i] = backgroundTransform.GetComponent<Image>();
            }

            if (skillHoverBackgroundImages[i] == null)
            {
                Transform hoverBackgroundTransform = FindChildRecursive(slot, "Skill_Background2");
                if (hoverBackgroundTransform != null)
                    skillHoverBackgroundImages[i] = hoverBackgroundTransform.GetComponent<Image>();
            }


            if (skillNameTexts[i] == null)
            {
                Transform nameTransform = FindChildRecursive(slot, "Skill_Name");
                if (nameTransform != null)
                {
                    skillNameTexts[i] = nameTransform.GetComponent<TMP_Text>();
                    if (skillNameTexts[i] == null)
                        skillNameTexts[i] = nameTransform.GetComponentInChildren<TMP_Text>(true);
                }
            }

            CacheOriginalSkillSlotColors(i);
        }
    }

    private void CacheOriginalSkillSlotColors(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MonsterSkillSlotCount || originalSkillColorsCached[slotIndex])
            return;

        Image background = skillBackgroundImages[slotIndex];
        TMP_Text name = skillNameTexts[slotIndex];

        if (background == null && name == null)
            return;

        if (background != null)
            originalSkillBackgroundColors[slotIndex] = background.color;

        if (name != null)
            originalSkillNameColors[slotIndex] = name.color;

        originalSkillColorsCached[slotIndex] = true;
    }

    private void ApplySkillSlotVisualState(int slotIndex, bool enabled)
    {
        if (slotIndex < 0 || slotIndex >= MonsterSkillSlotCount)
            return;

        CacheOriginalSkillSlotColors(slotIndex);

        Image background = skillBackgroundImages[slotIndex];
        TMP_Text name = skillNameTexts[slotIndex];

        Image hoverBackground = skillHoverBackgroundImages[slotIndex];
        if (hoverBackground != null)
            hoverBackground.gameObject.SetActive(false);

        if (background != null)
        {
            if (!enabled)
                background.color = inactiveSkillColor;
            else if (selectedSkillIndex == slotIndex)
                background.color = selectedSkillColor;
            else if (originalSkillColorsCached[slotIndex])
                background.color = originalSkillBackgroundColors[slotIndex];
        }

        if (name != null)
        {
            name.color = enabled && originalSkillColorsCached[slotIndex]
                ? originalSkillNameColors[slotIndex]
                : inactiveSkillColor;
        }
    }

    private void ResolveSkillInfoReferences()
    {
        if (skillInfoRoot == null)
            skillInfoRoot = FindChildRecursive(transform, "SkillInfo");

        if (skillInfoRoot == null)
            return;

        if (skillInfoIconImage == null)
            skillInfoIconImage = FindImageUnder(skillInfoRoot, "Skill_Icon");

        if (skillInfoRangeImage == null)
            skillInfoRangeImage = FindImageUnder(skillInfoRoot, "Skill_Range");

        if (skillInfoNameText == null)
            skillInfoNameText = FindTextUnder(skillInfoRoot, "Skill_Name");

        if (skillInfoTypeText == null)
            skillInfoTypeText = FindTextUnder(skillInfoRoot, "Skill_Type");

        if (skillInfoDetailsText == null)
            skillInfoDetailsText = FindTextUnder(skillInfoRoot, "Skill_Details");
    }

    private void ResolveSkillEffectReferences()
    {
        EnsureArraySizes();

        Transform effectContainer = null;
        if (skillInfoRoot != null)
            effectContainer = FindChildRecursive(skillInfoRoot, "Skill_Effect");

        if (effectContainer == null)
            effectContainer = FindChildRecursive(transform, "Skill_Effect");

        if (effectContainer == null)
            return;

        for (int i = 0; i < MonsterSkillEffectSlotCount; i++)
        {
            Transform effectSlot = ResolveEffectSlotTransform(effectContainer, i);
            if (effectSlot == null)
                continue;

            if (skillEffectRoots[i] == null)
                skillEffectRoots[i] = effectSlot.gameObject;

            if (skillEffectNameTexts[i] == null)
            {
                skillEffectNameTexts[i] = FindTextByCandidateNames(
                    effectSlot,
                    "Effect_Name",
                    "EffectName",
                    "Name",
                    "Effect"
                );
            }

            if (skillEffectValueTexts[i] == null)
            {
                skillEffectValueTexts[i] = FindTextByCandidateNames(
                    effectSlot,
                    "Effect_Value",
                    "EffectValue",
                    "Value",
                    "ValueText"
                );
            }

            if (skillEffectNameTexts[i] == null || skillEffectValueTexts[i] == null)
            {
                TMP_Text[] texts = effectSlot.GetComponentsInChildren<TMP_Text>(true);

                if (skillEffectNameTexts[i] == null && texts.Length > 0)
                    skillEffectNameTexts[i] = texts[0];

                if (skillEffectValueTexts[i] == null && texts.Length > 1)
                    skillEffectValueTexts[i] = texts[1];
            }
        }
    }

    private static Transform ResolveEffectSlotTransform(Transform effectContainer, int slotIndex)
    {
        if (effectContainer == null)
            return null;

        string number2 = (slotIndex + 1).ToString("00");
        string number1 = (slotIndex + 1).ToString();
        string[] candidates =
        {
            "Effect" + number2,
            "Effect" + number1,
            "SkillEffect" + number2,
            "SkillEffect" + number1,
            "Skill_Effect" + number2,
            "Skill_Effect" + number1
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            Transform found = FindChildRecursive(effectContainer, candidates[i]);
            if (found != null)
                return found;
        }

        return slotIndex < effectContainer.childCount
            ? effectContainer.GetChild(slotIndex)
            : null;
    }

    private Image FindImage(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        if (target == null)
            return null;

        Image image = target.GetComponent<Image>();
        return image != null ? image : target.GetComponentInChildren<Image>(true);
    }

    private TMP_Text FindText(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        if (target == null)
            return null;

        TMP_Text text = target.GetComponent<TMP_Text>();
        return text != null ? text : target.GetComponentInChildren<TMP_Text>(true);
    }

    private static Image FindImageUnder(Transform root, string objectName)
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null)
            return null;

        Image image = target.GetComponent<Image>();
        return image != null ? image : target.GetComponentInChildren<Image>(true);
    }

    private static TMP_Text FindTextUnder(Transform root, string objectName)
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null)
            return null;

        TMP_Text text = target.GetComponent<TMP_Text>();
        return text != null ? text : target.GetComponentInChildren<TMP_Text>(true);
    }

    private static TMP_Text FindTextByCandidateNames(Transform root, params string[] names)
    {
        if (root == null || names == null)
            return null;

        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildRecursive(root, names[i]);
            if (target == null)
                continue;

            TMP_Text text = target.GetComponent<TMP_Text>();
            if (text == null)
                text = target.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
                return text;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            if (child.name == objectName)
                return child;

            Transform nested = FindChildRecursive(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private static object GetMemberValue(object target, string memberName)
    {
        if (target == null || string.IsNullOrWhiteSpace(memberName))
            return null;

        Type type = target.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        PropertyInfo property = type.GetProperty(memberName, flags);
        if (property != null)
            return property.GetValue(target);

        FieldInfo field = type.GetField(memberName, flags);
        return field != null ? field.GetValue(target) : null;
    }

    private static string GetStringMemberValue(object target, string memberName)
    {
        object value = GetMemberValue(target, memberName);
        return value != null ? value.ToString() : string.Empty;
    }

    private static string NormalizeDisplayText(string value)
    {
        return IsEmptyDataValue(value) ? string.Empty : value.Trim();
    }

    private static string[] SplitSemicolonValues(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw.Split(';');
    }

    private static bool IsEmptyDataValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim() == "0";
    }

    private void EnsureArraySizes()
    {
        if (skillButtons == null || skillButtons.Length != MonsterSkillSlotCount)
            Array.Resize(ref skillButtons, MonsterSkillSlotCount);

        if (skillBackgroundImages == null || skillBackgroundImages.Length != MonsterSkillSlotCount)
            Array.Resize(ref skillBackgroundImages, MonsterSkillSlotCount);

        if (skillHoverBackgroundImages == null || skillHoverBackgroundImages.Length != MonsterSkillSlotCount)
            Array.Resize(ref skillHoverBackgroundImages, MonsterSkillSlotCount);

        if (skillNameTexts == null || skillNameTexts.Length != MonsterSkillSlotCount)
            Array.Resize(ref skillNameTexts, MonsterSkillSlotCount);

        if (skillEffectRoots == null || skillEffectRoots.Length != MonsterSkillEffectSlotCount)
            Array.Resize(ref skillEffectRoots, MonsterSkillEffectSlotCount);

        if (skillEffectNameTexts == null || skillEffectNameTexts.Length != MonsterSkillEffectSlotCount)
            Array.Resize(ref skillEffectNameTexts, MonsterSkillEffectSlotCount);

        if (skillEffectValueTexts == null || skillEffectValueTexts.Length != MonsterSkillEffectSlotCount)
            Array.Resize(ref skillEffectValueTexts, MonsterSkillEffectSlotCount);
    }

    private static int CalculateStatusHash(List<StatusEffectRuntimeData> statusEffects)
    {
        if (statusEffects == null)
            return 0;

        unchecked
        {
            int hash = 17;
            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffectRuntimeData status = statusEffects[i];
                if (status == null)
                    continue;

                hash = hash * 31 + (status.EffectId != null ? status.EffectId.GetHashCode() : 0);
                hash = hash * 31 + status.Stack;
                hash = hash * 31 + status.TurnCount;
            }

            return hash;
        }
    }

    private void ResetCachedValues()
    {
        lastHp = int.MinValue;
        lastMaxHp = int.MinValue;
        lastArmor = int.MinValue;
        lastStatusHash = int.MinValue;
    }
}
