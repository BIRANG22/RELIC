using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 전투/이벤트 기억 보상 전용 패널. 카드 1~3은 서로 다른 기억 후보입니다.
public sealed class BattleRewardSkillPanelUI : MonoBehaviour
{
    [SerializeField] private Color commonColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    [SerializeField] private Color rareColor = new Color32(0x52, 0x86, 0xC7, 0xFF);
    [SerializeField] private Color epicColor = new Color32(0xA0, 0x72, 0xC2, 0xFF);
    [SerializeField] private Color uniqueColor = new Color32(0xCD, 0xA4, 0x52, 0xFF);
    [SerializeField] private Color hpColor = new Color32(0xD2, 0x66, 0x66, 0xFF);
    [SerializeField] private Color mpColor = new Color32(0x62, 0x9B, 0xDA, 0xFF);
    [SerializeField] private Color karmaColor = Color.white;
    [SerializeField] private Sprite hpIcon;
    [SerializeField] private Sprite mpIcon;
    [SerializeField] private Sprite karmaIcon;

    [Serializable]
    private sealed class SkillCardReferences
    {
        public Transform root;
        public Image rarityLine;
        public TMP_Text typeText;
        public Image skillIcon;
        public TMP_Text skillName;
        public Image rangeIcon;
        public TMP_Text detailText;
        public Image resourceIcon;
        public TMP_Text resourceValue;
        public Image characterSideIcon;
        public GameObject select;
    }

    [Serializable]
    private sealed class PartyCardReferences
    {
        public Transform root;
        public Image characterIcon;
        public TMP_Text characterName;
        public Image skill01Icon;
        public Image skill02Icon;
        public Image skill03Icon;
    }

    [Header("Skill_content - inspector references")]
    [SerializeField] private Transform skillContent;
    [SerializeField] private SkillCardReferences[] skillCards = new SkillCardReferences[3];
    [Header("Char_content - inspector references")]
    [SerializeField] private Transform charContent;
    [SerializeField] private PartyCardReferences[] partyCards = new PartyCardReferences[3];

    private BattleRewardData reward;
    private Action completed;
    private bool processing;
    [Header("Equip Close Delay")]
    [SerializeField, Min(0f)] private float equipCloseDelay = 1f;
    private Coroutine equipCloseCoroutine;
    private readonly Button[] buttons = new Button[3];
    private readonly string[] characterIds = new string[3];
    private readonly SkillMasterData[] offeredSkills = new SkillMasterData[3];

    public static BattleRewardSkillPanelUI FindPanel()
    {
        return UnityEngine.Object.FindFirstObjectByType<BattleRewardSkillPanelUI>(FindObjectsInactive.Include);
    }

    public bool Open(BattleRewardData data, Action onCompleted)
    {
        if (data == null || data.Type != BattleRewardType.Skill || processing || reward != null || DataManager.Instance == null)
            return false;
        if (!DataManager.Instance.SkillDatabase.TryGet(data.RewardId, out SkillMasterData skill) || skill == null)
            return false;
        reward = data;
        completed = onCompleted;
        processing = false;
        if (equipCloseCoroutine != null)
        {
            StopCoroutine(equipCloseCoroutine);
            equipCloseCoroutine = null;
        }
        Transform content = skillContent != null ? skillContent : FindChild(transform, "Skill_content");
        if (content == null) { reward = null; completed = null; return false; }
        List<SkillMasterData> offers = BuildOffers(skill, data);
        Debug.Log($"[BattleRewardSkillPanelUI] Reward rarity: {skill.Rarity}, offers: {offers.Count} / {string.Join(", ", offers.ConvertAll(x => x.SkillId))}");
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Transform card = SkillRef(i)?.root != null ? SkillRef(i).root : FindChild(content, "Skill0" + (i + 1));
            offeredSkills[i] = i < offers.Count ? offers[i] : null;
            characterIds[i] = null;
            if (card == null) continue;
            ClearCard(card);
            SkillMasterData choice = offeredSkills[i];
            card.gameObject.SetActive(choice != null);
            if (choice == null) continue;
            characterIds[i] = ResolveOwner(choice);
            BindCard(card, string.IsNullOrWhiteSpace(choice.CharacterId) || string.Equals(choice.CharacterId, "ALL", StringComparison.OrdinalIgnoreCase) ? characterIds[i] : choice.CharacterId, choice, data);
            Button button = card.GetComponent<Button>();
            if (button == null) button = card.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            buttons[i] = button;
            button.onClick.AddListener(index == 0 ? SelectFirst : index == 1 ? SelectSecond : SelectThird);
            button.interactable = !string.IsNullOrEmpty(characterIds[i]) &&
                BattleRewardEquipSelectionPolicy.CanEquipRewardSkill(choice, characterIds[i]) &&
                HasFreeSlot(characterIds[i]);
            Transform select = SkillRef(i)?.select != null ? SkillRef(i).select.transform : FindChild(card, "Select");
            if (select != null)
            {
                select.gameObject.SetActive(false);
                foreach (Graphic graphic in select.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                RewardSkillCardHover hover = card.GetComponent<RewardSkillCardHover>();
                if (hover == null) hover = card.gameObject.AddComponent<RewardSkillCardHover>();
                hover.Configure(select.gameObject);
            }
        }
        BindPartyContent();
        BattleUIBlurRootCollector.ConfigureForPanel(gameObject);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        return true;
    }

    // 파티의 현재 장착 기억을 Char_content에 표시합니다. 이 영역은 정보 표시 전용입니다.
    private void BindPartyContent()
    {
        Transform content = charContent != null ? charContent : FindChild(transform, "Char_content");
        if (content == null) return;
        DataManager dm = DataManager.Instance;
        for (int i = 0; i < 3; i++)
        {
            Transform card = PartyRef(i)?.root != null ? PartyRef(i).root : FindChild(content, "Char" + (i + 1));
            if (card == null) continue;
            string characterId = dm?.PartyRuntimeStore?.GetCharacterId(i);
            CharacterRuntimeData character = null;
            bool hasCharacter = !string.IsNullOrWhiteSpace(characterId) &&
                dm?.CharacterRuntimeStore != null &&
                dm.CharacterRuntimeStore.TryGet(characterId, out character) && character != null;
            card.gameObject.SetActive(hasCharacter);
            if (!hasCharacter) continue;

            Sprite portrait = null;
            if (dm.CharacterIconDatabase != null)
                dm.CharacterIconDatabase.TryGetIcon(characterId, out portrait);
            if (portrait == null && dm.CharacterDatabase != null &&
                dm.CharacterDatabase.TryGet(characterId, out CharacterMasterData portraitData))
                portrait = portraitData.Icon;
            SetSprite(GetImage(card, "Icon/Mask/Image"), portrait);

            string characterName = characterId;
            if (dm.CharacterDatabase != null &&
                dm.CharacterDatabase.TryGet(characterId, out CharacterMasterData master))
            {
                string localizedName = GameDataLocalization.CharacterName(master);
                if (!string.IsNullOrWhiteSpace(localizedName)) characterName = localizedName;
                else if (!string.IsNullOrWhiteSpace(master.Name)) characterName = master.Name;
            }
            SetText(card, "Name", characterName);

            SkillInventoryEquipService.EnsureEquippedSkillArray(character);
            for (int slot = 0; slot < 3; slot++)
            {
                Transform skillSlot = FindChild(card, "Skill/Skill0" + (slot + 1));
                if (skillSlot == null) continue;
                // 장착 기억의 런타임 인덱스는 1~3이고, 0은 패시브 슬롯입니다.
                int runtimeIndex = slot + 1;
                string skillId = character.EquippedSkillIds != null && runtimeIndex < character.EquippedSkillIds.Length
                    ? character.EquippedSkillIds[runtimeIndex] : null;
                Sprite skillIcon = null;
                if (!string.IsNullOrWhiteSpace(skillId))
                {
                    if (dm.SkillIconDatabase != null)
                        dm.SkillIconDatabase.TryGetIcon(skillId.Trim(), out skillIcon);
                    if (skillIcon == null && dm.SkillDatabase != null &&
                        dm.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData masterSkill))
                        skillIcon = masterSkill.Icon;
                }
                SetSprite(GetImage(skillSlot, "Icon"), skillIcon);
            }
        }
    }

    private void SelectFirst() { Select(0); }
    private void SelectSecond() { Select(1); }
    private void SelectThird() { Select(2); }

    private void Select(int index)
    {
        if (processing || reward == null || index < 0 || index >= characterIds.Length) return;
        string id = characterIds[index];
        SkillMasterData skill = offeredSkills[index];
        if (string.IsNullOrWhiteSpace(id) || skill == null) return;
        var dm = DataManager.Instance;
        if (dm == null || !BattleRewardEquipSelectionPolicy.CanEquipRewardSkill(skill, id)) return;
        if (!dm.CharacterRuntimeStore.TryGet(id, out CharacterRuntimeData character) || character == null) return;
        SkillInventoryEquipService.EnsureEquippedSkillArray(character);
        // 임의의 장착 기억 교체를 피하기 위해 비어 있는 슬롯에만 장착합니다.
        int target = -1;
        for (int i = 1; i <= 3 && i < character.EquippedSkillIds.Length; i++)
            if (string.IsNullOrWhiteSpace(character.EquippedSkillIds[i])) { target = i; break; }
        if (target < 0) return;
        processing = true;
        character.EquippedSkillIds[target] = skill.SkillId.Trim();
        dm.CharacterRuntimeStore.AddOrUpdate(character);
        BattleRuntimeData runtime = dm.BattleRuntimeStore.GetOrCreate();
        runtime.AcquiredSkillIds ??= new List<string>();
        if (!runtime.AcquiredSkillIds.Contains(skill.SkillId)) runtime.AcquiredSkillIds.Add(skill.SkillId);
        dm.BattleRuntimeStore.Set(runtime);
        RecordDiscoveryService.RegisterSkill(dm, skill.SkillId);
        BindPartyContent(); // Refresh equipped skill icons while the panel is still visible.
        foreach (Button choiceButton in buttons)
            if (choiceButton != null) choiceButton.interactable = false;
        equipCloseCoroutine = StartCoroutine(CloseAfterEquippedPreview());
    }

    private IEnumerator CloseAfterEquippedPreview()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, equipCloseDelay));
        equipCloseCoroutine = null;
        Finish();
    }

    public void DiscardReward() { if (reward != null && !processing) { processing = true; Finish(); } }

    private void Finish()
    {
        Action callback = completed;
        reward = null;
        completed = null;
        gameObject.SetActive(false);
        processing = false;
        callback?.Invoke();
    }

    private void ClearCard(Transform card)
    {
        if (card == null) return;
        SetText(card, "Type/Type_text", string.Empty);
        SetText(card, "Name", string.Empty);
        SetText(card, "Detail", string.Empty);
        SetText(card, "Resources/Value", string.Empty);
        SetSprite(GetImage(card, "Icon/Icon"), null);
        SetSprite(GetImage(card, "Range"), null);
        SetSprite(GetImage(card, "Resources/Icon"), null);
        SetSprite(GetImage(card, "Character/Mask/Icon"), null);
    }

    private void BindCard(Transform card, string characterId, SkillMasterData skill, BattleRewardData data)
    {
        Image line = GetImage(card, "Background/Line2");
        if (line != null) line.color = RarityColor(skill.Rarity);
        Image portrait = GetImage(card, "Character/Mask/Icon");
        Sprite side = null;
        if (!string.IsNullOrWhiteSpace(characterId))
            DataManager.Instance.CharacterIconDatabase?.TryGetSideImage(characterId, out side);
        SetSprite(portrait, side);
        string typeName = skill.TimelineNotation == TimelineActionType.Move
            ? GameLocalization.Get("common.move")
            : skill.Category == Category.Passive
                ? GameLocalization.Get("common.passive")
                : skill.SkillType == SkillType.Buff
                    ? GameLocalization.Get("common.buff")
                    : skill.SkillType == SkillType.Debuff
                        ? GameLocalization.Get("common.debuff")
                        : skill.SkillType == SkillType.Attack
                            ? GameLocalization.Get("common.attack")
                            : GameLocalization.Get("common.passive");
        SetText(card, "Type/Type_text", typeName);
        Sprite icon = null;
        if (DataManager.Instance.SkillIconDatabase != null)
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);
        if (icon == null) icon = skill.Icon;
        SetSprite(GetImage(card, "Icon/Icon"), icon);
        SetText(card, "Name", GameDataLocalization.SkillName(skill));
        SetDetailText(card, skill);
        Image range = GetImage(card, "Range") ?? GetImage(card, "Range/Icon");
        Sprite rangeIcon = null;
        if (!string.IsNullOrWhiteSpace(skill.RangeId) && DataManager.Instance.SkillRangeIconDatabase != null)
            DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(skill.RangeId.Trim(), out rangeIcon);
        SetSprite(range, rangeIcon);
        Image cost = GetImage(card, "Resources/Icon");
        TMP_Text value = GetText(card, "Resources/Value");
        Color color = skill.ReferenceResource == ReferenceResource.HP ? hpColor :
            skill.ReferenceResource == ReferenceResource.UniqueResource ? karmaColor : mpColor;
        Sprite resource = skill.ReferenceResource == ReferenceResource.HP ? hpIcon :
            skill.ReferenceResource == ReferenceResource.UniqueResource ? karmaIcon : mpIcon;
        if (resource == null)
        {
            BattleCharacterPanelUI hud = UnityEngine.Object.FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Include);
            if (hud != null) resource = hud.GetResourceIcon(skill.ReferenceResource);
        }
        SetSprite(cost, resource);
        if (cost != null) cost.color = color;
        if (value != null) { SetDynamicText(value, skill.ResourceCostValue.ToString()); value.color = color; }
    }

    // Collect eligible memories from all registered party members, then draw three unique IDs.
    // The event reward source carries the selection filter; a random memory is not
    // restricted to the rarity of the placeholder skill selected by the event system.
    private List<SkillMasterData> BuildOffers(SkillMasterData rewardSkill, BattleRewardData rewardData)
    {
        List<SkillMasterData> offers = new List<SkillMasterData>();
        DataManager dm = DataManager.Instance;
        if (dm?.SkillDatabase == null || dm.PartyRuntimeStore == null) return offers;

        HashSet<string> party = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < 3; i++)
        {
            string id = dm.PartyRuntimeStore.GetCharacterId(i)?.Trim();
            if (!string.IsNullOrWhiteSpace(id) && dm.CharacterRuntimeStore != null &&
                dm.CharacterRuntimeStore.TryGet(id, out CharacterRuntimeData character) && character != null)
                party.Add(id);
        }
        if (party.Count == 0) return offers;

        HashSet<string> acquired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        BattleRuntimeData runtime = dm.BattleRuntimeStore?.GetOrCreate();
        if (runtime?.AcquiredSkillIds != null)
            foreach (string id in runtime.AcquiredSkillIds) BlockSkillAndVariant(acquired, id);
        if (runtime?.SkillInventoryIds != null)
            foreach (string id in runtime.SkillInventoryIds) BlockSkillAndVariant(acquired, id);
        if (dm.CharacterRuntimeStore != null)
            foreach (CharacterRuntimeData character in dm.CharacterRuntimeStore.GetAll().Values)
            {
                if (character == null) continue;
                if (character.EquippedSkillIds != null)
                    foreach (string id in character.EquippedSkillIds) BlockSkillAndVariant(acquired, id);
            }

        string source = rewardData?.SourceKey ?? string.Empty;
        bool allRarities = source.Contains("|AnyRarity|") || source.Contains("|Attack|") ||
            source.Contains("|Buff|") || source.Contains("|Debuff|");
        bool commonRare = source.Contains("|CommonToRare|");
        SkillType? typeFilter = source.Contains("|Attack|") ? SkillType.Attack :
            source.Contains("|Buff|") ? SkillType.Buff :
            source.Contains("|Debuff|") ? SkillType.Debuff : (SkillType?)null;

        List<SkillMasterData> candidates = new List<SkillMasterData>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (SkillMasterData skill in dm.SkillDatabase.GetAll())
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId) || skill.Category != Category.Core ||
                !SkillRarityUtility.IsCoreDropRarity(skill.Rarity) ||
                !SkillRarityUtility.IsBaseSkillVariant(skill.SkillId) ||
                acquired.Contains(skill.SkillId.Trim()) || !seen.Add(skill.SkillId.Trim())) continue;
            if (typeFilter.HasValue && skill.SkillType != typeFilter.Value) continue;
            if (commonRare)
            {
                if (skill.Rarity != SkillRarity.Common && skill.Rarity != SkillRarity.Rare) continue;
            }
            else if (!allRarities && skill.Rarity != rewardSkill.Rarity) continue;

            string owner = skill.CharacterId?.Trim();
            if (!string.Equals(owner, "ALL", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(owner) || !party.Contains(owner))) continue;
            if (ResolveOwner(skill) == null) continue;
            candidates.Add(skill);
        }
        Debug.Log($"[BattleRewardSkillPanelUI] Source={source}, Rarity={rewardSkill.Rarity}, Party={string.Join(",", party)}, Pool={candidates.Count}");
        while (offers.Count < 3 && candidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            offers.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return offers;
    }

    private static void BlockSkillAndVariant(HashSet<string> blocked, string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId)) return;
        string id = skillId.Trim();
        blocked.Add(id);
        if (SkillRarityUtility.TryGetPairedVariantId(id, out string paired)) blocked.Add(paired);
    }

    private string ResolveOwner(SkillMasterData skill)
    {
        if (skill == null) return null;
        var dm = DataManager.Instance;
        if (dm?.PartyRuntimeStore == null || dm.CharacterRuntimeStore == null) return null;
        for (int i = 0; i < 3; i++)
        {
            string id = dm.PartyRuntimeStore.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(id) || !SkillOwnershipPolicy.CanEquip(skill, id)) continue;
            if (dm.CharacterRuntimeStore.TryGet(id, out CharacterRuntimeData character) && character != null)
                return id;
        }
        return null;
    }

    private static bool HasFreeSlot(string characterId)
    {
        var dm = DataManager.Instance;
        if (dm?.CharacterRuntimeStore == null ||
            !dm.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData character) || character == null)
            return false;
        SkillInventoryEquipService.EnsureEquippedSkillArray(character);
        for (int i = 1; i <= 3 && i < character.EquippedSkillIds.Length; i++)
            if (string.IsNullOrWhiteSpace(character.EquippedSkillIds[i])) return true;
        return false;
    }

    private Color RarityColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Rare: return rareColor;
            case SkillRarity.Epic: return epicColor;
            case SkillRarity.Unique: return uniqueColor;
            default: return commonColor;
        }
    }
    private SkillCardReferences SkillRef(int index) =>
        skillCards != null && index >= 0 && index < skillCards.Length ? skillCards[index] : null;

    private PartyCardReferences PartyRef(int index) =>
        partyCards != null && index >= 0 && index < partyCards.Length ? partyCards[index] : null;

    private static Transform FindChild(Transform parent, string path) => parent != null ? parent.Find(path) : null;

    private Image GetImage(Transform root, string path)
    {
        for (int i = 0; i < 3; i++)
        {
            SkillCardReferences s = SkillRef(i);
            if (s != null && s.root == root)
            {
                switch (path)
                {
                    case "Background/Line2": if (s.rarityLine != null) return s.rarityLine; break;
                    case "Icon/Icon": if (s.skillIcon != null) return s.skillIcon; break;
                    case "Range": case "Range/Icon": if (s.rangeIcon != null) return s.rangeIcon; break;
                    case "Resources/Icon": if (s.resourceIcon != null) return s.resourceIcon; break;
                    case "Character/Mask/Icon": if (s.characterSideIcon != null) return s.characterSideIcon; break;
                }
            }
            PartyCardReferences p = PartyRef(i);
            if (p != null && p.root == root && path == "Icon/Mask/Image" && p.characterIcon != null)
                return p.characterIcon;
            if (p != null && p.root != null && root != null && root.IsChildOf(p.root) && path == "Icon")
            {
                Transform slots = p.root.Find("Skill");
                if (slots != null)
                {
                    if (root == slots.Find("Skill01") && p.skill01Icon != null) return p.skill01Icon;
                    if (root == slots.Find("Skill02") && p.skill02Icon != null) return p.skill02Icon;
                    if (root == slots.Find("Skill03") && p.skill03Icon != null) return p.skill03Icon;
                }
            }
        }
        Transform t = FindChild(root, path);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private TMP_Text GetText(Transform root, string path)
    {
        for (int i = 0; i < 3; i++)
        {
            SkillCardReferences s = SkillRef(i);
            if (s != null && s.root == root)
            {
                switch (path)
                {
                    case "Type/Type_text": if (s.typeText != null) return s.typeText; break;
                    case "Name": if (s.skillName != null) return s.skillName; break;
                    case "Detail": if (s.detailText != null) return s.detailText; break;
                    case "Resources/Value": if (s.resourceValue != null) return s.resourceValue; break;
                }
            }
            PartyCardReferences p = PartyRef(i);
            if (p != null && p.root == root && path == "Name" && p.characterName != null)
                return p.characterName;
        }
        Transform target = FindChild(root, path);
        if (target == null) return null;
        TMP_Text direct = target.GetComponent<TMP_Text>();
        return direct != null ? direct : target.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void SetDetailText(Transform card, SkillMasterData skill)
    {
        TMP_Text target = GetText(card, "Detail");
        if (target == null) return;
        DisableStaticLocalization(target);
        SkillEffectInlineIconUtility.SetText(target,
            skill != null ? GameDataLocalization.SkillDetails(skill) : string.Empty);
    }

    private void SetText(Transform root, string path, string text)
    {
        TMP_Text label = GetText(root, path);
        if (label != null) SetDynamicText(label, text);
        else Debug.LogWarning($"[BattleRewardSkillPanelUI] Missing TMP_Text: {root?.name}/{path}", root);
    }
    // Dynamic reward text is controlled by this panel, not by prefab localization keys.
    private static void DisableStaticLocalization(TMP_Text label)
    {
        if (label == null) return;
        LocalizedTMPText localization = label.GetComponent<LocalizedTMPText>();
        if (localization != null && localization.enabled)
            localization.enabled = false;
    }

    private static void SetDynamicText(TMP_Text label, string value)
    {
        if (label == null) return;
        DisableStaticLocalization(label);
        label.text = value ?? string.Empty;
    }

    private static void SetSprite(Image image, Sprite sprite) { if (image == null) return; image.sprite = sprite; image.enabled = sprite != null; }
}

// 카드의 호버 표시만 담당합니다. 보상 선택은 Button.onClick에서만 실행됩니다.
public sealed class RewardSkillCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject selection;
    public void Configure(GameObject target) { selection = target; if (selection != null) selection.SetActive(false); }
    public void OnPointerEnter(PointerEventData eventData) { if (selection != null) selection.SetActive(true); }
    public void OnPointerExit(PointerEventData eventData) { if (selection != null) selection.SetActive(false); }
    private void OnDisable() { if (selection != null) selection.SetActive(false); }
}
