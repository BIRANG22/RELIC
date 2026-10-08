using System;
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

    private BattleRewardData reward;
    private Action completed;
    private bool processing;
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
        Transform content = FindChild(transform, "Skill_content");
        if (content == null) { reward = null; completed = null; return false; }
        List<SkillMasterData> offers = BuildOffers(skill);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Transform card = FindChild(content, "Skill0" + (i + 1));
            offeredSkills[i] = i < offers.Count ? offers[i] : null;
            characterIds[i] = null;
            if (card == null) continue;
            SkillMasterData choice = offeredSkills[i];
            card.gameObject.SetActive(choice != null);
            if (choice == null) continue;
            characterIds[i] = ResolveOwner(choice);
            BindCard(card, choice.CharacterId, choice, data);
            Button button = card.GetComponent<Button>();
            if (button == null) button = card.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            buttons[i] = button;
            button.onClick.AddListener(index == 0 ? SelectFirst : index == 1 ? SelectSecond : SelectThird);
            button.interactable = !string.IsNullOrEmpty(characterIds[i]) &&
                BattleRewardEquipSelectionPolicy.CanEquipRewardSkill(choice, characterIds[i]) &&
                HasFreeSlot(characterIds[i]);
            Transform select = FindChild(card, "Select");
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
        Transform content = FindChild(transform, "Char_content");
        if (content == null) return;
        DataManager dm = DataManager.Instance;
        for (int i = 0; i < 3; i++)
        {
            Transform card = FindChild(content, "Char" + (i + 1));
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
                characterName = GameDataLocalization.CharacterName(master);
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

    private void BindCard(Transform card, string characterId, SkillMasterData skill, BattleRewardData data)
    {
        Image line = GetImage(card, "Background/Line2");
        if (line != null) line.color = RarityColor(skill.Rarity);
        Image portrait = GetImage(card, "Character/Mask/Icon");
        Sprite side = null;
        if (!string.IsNullOrWhiteSpace(characterId))
            DataManager.Instance.CharacterIconDatabase?.TryGetSideImage(characterId, out side);
        SetSprite(portrait, side);
        string typeName = skill.TimelineNotation == TimelineActionType.Move ? "이동" :
            skill.SkillType == SkillType.Buff ? "버프" :
            skill.SkillType == SkillType.Debuff ? "디버프" :
            skill.SkillType == SkillType.Attack ? "공격" : "패시브";
        SetText(card, "Type/Type_text", typeName);
        Sprite icon = skill.Icon;
        if (icon == null && DataManager.Instance.SkillIconDatabase != null)
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);
        SetSprite(GetImage(card, "Icon/Icon"), icon);
        SetText(card, "Name", GameDataLocalization.SkillName(skill));
        SetText(card, "Detail", GameDataLocalization.SkillDetails(skill));
        Image range = GetImage(card, "Range");
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
        if (value != null) { value.text = skill.ResourceCostValue.ToString(); value.color = color; }
    }

    private List<SkillMasterData> BuildOffers(SkillMasterData first)
    {
        List<SkillMasterData> result = new List<SkillMasterData>();
        if (first == null) return result;
        result.Add(first);
        var dm = DataManager.Instance;
        if (dm?.SkillDatabase == null) return result;
        List<SkillMasterData> candidates = new List<SkillMasterData>();
        List<SkillMasterData> all = dm.SkillDatabase.GetAll();
        HashSet<string> blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        blocked.Add(first.SkillId.Trim());
        BattleRuntimeData battle = dm.BattleRuntimeStore?.GetOrCreate();
        if (battle?.AcquiredSkillIds != null)
            foreach (string id in battle.AcquiredSkillIds)
                if (!string.IsNullOrWhiteSpace(id)) blocked.Add(id.Trim());
        if (dm.CharacterRuntimeStore != null)
        {
            var runtimes = dm.CharacterRuntimeStore.GetAll();
            foreach (CharacterRuntimeData character in runtimes.Values)
            {
                if (character?.EquippedSkillIds == null) continue;
                foreach (string id in character.EquippedSkillIds)
                    if (!string.IsNullOrWhiteSpace(id)) blocked.Add(id.Trim());
            }
        }
        foreach (SkillMasterData candidate in all)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.SkillId) ||
                candidate.Category != Category.Core || candidate.Rarity != first.Rarity ||
                !SkillRarityUtility.IsBaseSkillVariant(candidate.SkillId) ||
                blocked.Contains(candidate.SkillId.Trim()) || ResolveOwner(candidate) == null ||
                !SkillRewardPoolPolicy.IsAllowed(candidate.SkillId, dm.SkillRewardPoolDatabase)) continue;
            candidates.Add(candidate);
        }
        while (result.Count < 3 && candidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return result;
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
    private static Transform FindChild(Transform parent, string path) { return parent != null ? parent.Find(path) : null; }
    private static Image GetImage(Transform root, string path) { var t = FindChild(root, path); return t != null ? t.GetComponent<Image>() : null; }
    private static TMP_Text GetText(Transform root, string path) { var t = FindChild(root, path); return t != null ? t.GetComponent<TMP_Text>() : null; }
    private static void SetText(Transform root, string path, string text) { var t = GetText(root, path); if (t != null) t.text = text ?? string.Empty; }
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
