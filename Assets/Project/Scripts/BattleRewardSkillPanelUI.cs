﻿using System;
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

    [Header("Card Image Dissolve")]
    [Tooltip("Delay before SkillPanel1 dissolve starts (seconds).")]
    [SerializeField, Min(0f)] private float dissolveDelay = 0f;
    [Tooltip("Additional delay from SkillPanel1 start to SkillPanel2 start (seconds).")]
    [SerializeField, Min(0f)] private float skillPanel2DissolveDelay = 0.5f;
    [Tooltip("Additional delay from SkillPanel2 start to SkillPanel3 start (seconds).")]
    [SerializeField, Min(0f)] private float skillPanel3DissolveDelay = 0.5f;
    [SerializeField, Min(0f)] private float dissolveDuration = 2f;

    [Header("Skill Details Reveal")]
    [SerializeField, Min(0f)] private float skillRevealDelay = 1f;

    [Header("Skill Card Scale")]
    [SerializeField, Min(0.01f)] private float normalScale = 1f;
    [SerializeField, Min(0.01f)] private float hoverScale = 1.1f;
    [SerializeField, Min(0.01f)] private float clickedScale = 1.15f;
    [SerializeField, Min(0f)] private float scaleTransitionSpeed = 12f;

    [Header("Equipped Skill VFX")]
    [Tooltip("VFX prefab played over the actual equipped skill icon in Char_content.")]
    [SerializeField] private GameObject equippedSkillVfxPrefab;
    [Tooltip("Local position offset from the equipped skill icon.")]
    [SerializeField] private Vector3 equippedSkillVfxOffset = Vector3.zero;
    [Tooltip("Local scale applied to the spawned VFX.")]
    [SerializeField] private Vector3 equippedSkillVfxScale = Vector3.one;

    [Header("Rarity VFX Prefabs")]
    [SerializeField] private GameObject commonVfxPrefab;
    [SerializeField] private GameObject rareVfxPrefab;
    [SerializeField] private GameObject epicVfxPrefab;
    [SerializeField] private GameObject uniqueVfxPrefab;


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
        [Tooltip("Parent for this card's rarity VFX. Defaults to the card root.")]
        public Transform rarityVfxRoot;

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
    private int pendingReplacementCard = -1;
    private string pendingReplacementCharacterId;
    private readonly List<RewardSkillEquippedSlotClick> replacementSlotHandlers = new List<RewardSkillEquippedSlotClick>();
    [Header("Equip Close Delay")]
    [SerializeField, Min(0f)] private float equipCloseDelay = 1f;
    private Coroutine equipCloseCoroutine;
    [Header("Skill Panel Close Fade")]
    [Tooltip("Duration for the whole Skill_panel to fade out before it is disabled.")]
    [SerializeField, Min(0f)] private float skillPanelCloseFadeDuration = 0.5f;
    private CanvasGroup skillPanelCloseCanvasGroup;

    [Header("Unselected Card Mask Dissolve")]
    [Tooltip("Duration for the two unselected cards to dissolve after selecting a reward.")]
    [SerializeField, Min(0f)] private float unselectedCardDissolveDuration = 0.6f;

    [Header("Selected Card Fade Out")]
    [Tooltip("Time for the selected card contents to fade out after selection.")]
    [SerializeField, Min(0f)] private float selectedCardFadeDuration = 0.6f;
    private Coroutine selectedCardFadeCoroutine;
    private readonly CanvasGroup[] selectedCardFadeGroups = new CanvasGroup[3];

    private Coroutine selectionDissolveCoroutine;
    private readonly CardUIStencilDissolveMaskController[] selectionDissolveMasks = new CardUIStencilDissolveMaskController[3];
    private GameObject activeEquippedSkillVfx;
    private readonly Transform[] scaleCardRoots = new Transform[3];
    private readonly Vector3[] baseCardScales = new Vector3[3];
    private readonly bool[] cardHovered = new bool[3];
    private readonly bool[] cardInteractionReady = new bool[3];
    private int clickedCardIndex = -1;
    private readonly Button[] buttons = new Button[3];
    private readonly string[] characterIds = new string[3];
    private readonly SkillMasterData[] offeredSkills = new SkillMasterData[3];
    private readonly GameObject[] activeRarityVfx = new GameObject[3];
    private readonly Coroutine[] pendingRarityVfx = new Coroutine[3];
    private readonly Coroutine[] pendingSkillReveal = new Coroutine[3];
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveEdgeColorId = Shader.PropertyToID("_DissolveEdgeColor");
    private readonly Material[] cardDissolveMaterials = new Material[3];
    private readonly Image[] cardDissolveImages = new Image[3];
    private readonly Material[] originalCardMaterials = new Material[3];
    private readonly Coroutine[] cardDissolveCoroutines = new Coroutine[3];


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
        CancelReplacementSelection();
        if (equipCloseCoroutine != null)
        {
            StopCoroutine(equipCloseCoroutine);
            equipCloseCoroutine = null;
        }
        StopSelectedCardFade();
        ResetSkillPanelCloseFade();
        if (selectionDissolveCoroutine != null)
        {
            StopCoroutine(selectionDissolveCoroutine);
            selectionDissolveCoroutine = null;
        }
        Transform content = skillContent != null ? skillContent : FindChild(transform, "Skill_content");
        if (content == null) { reward = null; completed = null; return false; }
        ResetAllCardScales();
        ClearEquippedSkillVfx();
        clickedCardIndex = -1;
        List<SkillMasterData> offers = BuildOffers(skill, data);
        Debug.Log($"[BattleRewardSkillPanelUI] Reward rarity: {skill.Rarity}, offers: {offers.Count} / {string.Join(", ", offers.ConvertAll(x => x.SkillId))}");
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Transform card = GetSkillCardRoot(content, i);
            offeredSkills[i] = i < offers.Count ? offers[i] : null;
            characterIds[i] = null;
            ClearRarityVfx(i);
            ReleaseCardDissolve(i);
            StopSkillReveal(i);
            if (card == null) continue;
            scaleCardRoots[i] = card;
            Transform fadeRoot = FindChild(card, "Mask") ?? card;
            CanvasGroup fadeGroup = fadeRoot.GetComponent<CanvasGroup>();
            if (fadeGroup == null) fadeGroup = fadeRoot.gameObject.AddComponent<CanvasGroup>();
            selectedCardFadeGroups[i] = fadeGroup;
            fadeGroup.alpha = 1f;
            selectionDissolveMasks[i] = FindSelectionDissolveMask(card);
            if (selectionDissolveMasks[i] != null)
                selectionDissolveMasks[i].ResetDissolve();
            baseCardScales[i] = card.localScale;
            cardHovered[i] = false;
            cardInteractionReady[i] = false;
            SetCardAudioEnabled(card, false);
            card.localScale = baseCardScales[i] * normalScale;
            Transform skillRoot = FindChild(card, "Skill");
            if (skillRoot != null) skillRoot.gameObject.SetActive(false);
            card.gameObject.SetActive(false);
            ClearCard(card);
            SkillMasterData choice = offeredSkills[i];
            if (choice == null) continue;
            characterIds[i] = ResolveOwner(choice);
            BindCard(card, string.IsNullOrWhiteSpace(choice.CharacterId) || string.Equals(choice.CharacterId, "ALL", StringComparison.OrdinalIgnoreCase) ? characterIds[i] : choice.CharacterId, choice, data);
            // Assign the sprite and initialize the material before the card becomes visible.
            PrepareCardDissolve(i, card, choice.Rarity);
            card.gameObject.SetActive(true);
            Button button = card.GetComponent<Button>();
            if (button == null) button = card.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            buttons[i] = button;
            button.onClick.AddListener(index == 0 ? SelectFirst : index == 1 ? SelectSecond : SelectThird);
            button.interactable = cardInteractionReady[i] && !string.IsNullOrEmpty(characterIds[i]) &&
                BattleRewardEquipSelectionPolicy.CanEquipRewardSkill(choice, characterIds[i]) &&
                true;
            Transform select = FindChild(card, "Mask/Skill/Select") ?? FindChild(card, "Skill/Select") ?? FindChild(card, "Select");
            if (select == null && SkillRef(i)?.select != null) select = SkillRef(i).select.transform;
            if (select != null)
            {
                select.gameObject.SetActive(false);
                foreach (Graphic graphic in select.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                RewardSkillCardHover hover = card.GetComponent<RewardSkillCardHover>();
                if (hover == null) hover = card.gameObject.AddComponent<RewardSkillCardHover>();
                hover.Configure(select.gameObject, this, i);
            }
        }
        BindPartyContent();
        BattleUIBlurRootCollector.ConfigureForPanel(gameObject);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        for (int i = 0; i < 3; i++)
        {
            if (offeredSkills[i] == null) continue;
            Transform card = GetSkillCardRoot(content, i);
            if (card == null || !card.gameObject.activeInHierarchy) continue;
            float startDelay = GetCardDissolveStartDelay(i);
            if (startDelay <= 0f)
                ShowRarityVfx(i, card, offeredSkills[i].Rarity);
            else
                pendingRarityVfx[i] = StartCoroutine(ShowRarityVfxAfterDissolveDelay(i, card, offeredSkills[i].Rarity, startDelay));
        }
        for (int i = 0; i < 3; i++)
        {
            if (offeredSkills[i] == null) continue;
            Transform card = GetSkillCardRoot(content, i);
            if (card == null) continue;
            Transform skillRoot = FindChild(card, "Skill");
            if (skillRoot == null) continue;
            // Skill information reveal is timed independently from the card dissolve sequence.
            float revealDelay = Mathf.Max(0f, skillRevealDelay);
            if (revealDelay <= 0f) skillRoot.gameObject.SetActive(true);
            else pendingSkillReveal[i] = StartCoroutine(RevealSkillAfterDelay(i, skillRoot, revealDelay));
        }
        for (int i = 0; i < 3; i++)
        {
            if (offeredSkills[i] == null) continue;
            SetCardDissolveAmount(i, 0f);
            if (cardDissolveMaterials[i] != null && cardDissolveImages[i] != null && cardDissolveImages[i].isActiveAndEnabled)
                cardDissolveCoroutines[i] = StartCoroutine(AnimateCardDissolve(i, GetCardDissolveStartDelay(i)));
            else
            {
                Debug.LogWarning($"[BattleRewardSkillPanelUI] No dissolve material for SkillPanel{i + 1}; enabling interaction after its configured timing.", this);
                cardDissolveCoroutines[i] = StartCoroutine(UnlockAfterConfiguredTime(i));
            }
        }
        return true;
    }

    // 파티의 현재 장착 기억을 Char_content에 표시합니다. 이 영역은 정보 표시 전용입니다.
    // Prefer the matching card in the actual Skill_content hierarchy. Inspector refs are fallback.
    private Transform GetSkillCardRoot(Transform content, int index)
    {
        if (content == null || index < 0 || index >= 3) return null;
        Transform card = FindChild(content, "SkillPanel" + (index + 1));
        if (card == null) card = FindChild(content, "Skill0" + (index + 1));
        if (card == null) card = SkillRef(index)?.root;
        return card;
    }

    private float GetCardDissolveStartDelay(int index)
    {
        float delay = Mathf.Max(0f, dissolveDelay);
        if (index >= 1) delay += Mathf.Max(0f, skillPanel2DissolveDelay);
        if (index >= 2) delay += Mathf.Max(0f, skillPanel3DissolveDelay);
        return delay;
    }

    private IEnumerator RevealSkillAfterDelay(int index, Transform skillRoot, float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        pendingSkillReveal[index] = null;
        if (reward != null && skillRoot != null && gameObject.activeInHierarchy &&
            skillRoot.parent != null && skillRoot.parent.gameObject.activeInHierarchy)
            skillRoot.gameObject.SetActive(true);
    }

    private void StopSkillReveal(int index)
    {
        if (pendingSkillReveal[index] != null)
        {
            StopCoroutine(pendingSkillReveal[index]);
            pendingSkillReveal[index] = null;
        }
    }

    public void SetCardHovered(int index, bool hovered)
    {
        if (index < 0 || index >= cardHovered.Length) return;
        cardHovered[index] = cardInteractionReady[index] && hovered;
    }

    private void LateUpdate()
    {
        if (reward == null) return;
        float blend = scaleTransitionSpeed <= 0f
            ? 1f : 1f - Mathf.Exp(-scaleTransitionSpeed * Time.unscaledDeltaTime);
        for (int i = 0; i < scaleCardRoots.Length; i++)
        {
            Transform card = scaleCardRoots[i];
            if (card == null || offeredSkills[i] == null) continue;
            float multiplier = !cardInteractionReady[i] ? normalScale
                : clickedCardIndex == i ? clickedScale
                : (clickedCardIndex >= 0 ? normalScale
                : (cardHovered[i] ? hoverScale : normalScale));
            card.localScale = Vector3.Lerp(card.localScale, baseCardScales[i] * multiplier, blend);
        }
    }

    private void ResetAllCardScales()
    {
        for (int i = 0; i < scaleCardRoots.Length; i++)
        {
            if (scaleCardRoots[i] != null)
                scaleCardRoots[i].localScale = baseCardScales[i];
            SetCardAudioEnabled(scaleCardRoots[i], false);
            scaleCardRoots[i] = null;
            cardHovered[i] = false;
            cardInteractionReady[i] = false;
        }
        clickedCardIndex = -1;
    }

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
            {
                dm.CharacterIconDatabase.TryGetIcon(characterId, out portrait);
            }
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
                RewardSkillEquippedSlotClick click = skillSlot.GetComponent<RewardSkillEquippedSlotClick>();
                if (click == null) click = skillSlot.gameObject.AddComponent<RewardSkillEquippedSlotClick>();
                click.Configure(this, characterId, runtimeIndex, !string.IsNullOrWhiteSpace(skillId));
                if (!replacementSlotHandlers.Contains(click)) replacementSlotHandlers.Add(click);
                // Ensure the slot itself can receive UI raycasts even if the icon has none.
                Image hitArea = skillSlot.GetComponent<Image>();
                if (hitArea == null)
                {
                    hitArea = skillSlot.gameObject.AddComponent<Image>();
                    hitArea.color = new Color(1f, 1f, 1f, 0f);
                }
                hitArea.raycastTarget = true;
            }
        }
    }

    private void SelectFirst() { Select(0); }
    private void SelectSecond() { Select(1); }
    private void SelectThird() { Select(2); }

    private void Select(int index)
    {
        if (processing || reward == null || index < 0 || index >= characterIds.Length || !cardInteractionReady[index]) return;
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
        if (target < 0)
        {
            // All active slots are occupied. Wait for the player to click an
            // equipped slot belonging to the selected skill's character.
            pendingReplacementCard = index;
            pendingReplacementCharacterId = id;
            clickedCardIndex = index;
            // While waiting for the equipped slot choice, dissolve only the other cards.
            // The selected card remains visible until replacement is actually confirmed.
            PlaySelectionCardEffects(index, false);
            RefreshReplacementSlotHovers();
            return;
        }
        ConfirmSkillSelection(index, id, skill.SkillId.Trim(), character, target);
    }

    internal bool CanShowReplacementHover(string characterId, int equippedIndex)
    {
        return reward != null && !processing && pendingReplacementCard >= 0 &&
            equippedIndex >= 1 && equippedIndex <= 3 &&
            string.Equals(characterId, pendingReplacementCharacterId, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshReplacementSlotHovers()
    {
        foreach (RewardSkillEquippedSlotClick handler in replacementSlotHandlers)
            if (handler != null) handler.RefreshHover();
    }

    internal void OnEquippedSlotClicked(string characterId, int equippedIndex)
    {
        if (processing || reward == null || pendingReplacementCard < 0 ||
            !string.Equals(characterId, pendingReplacementCharacterId, StringComparison.OrdinalIgnoreCase) ||
            equippedIndex < 1 || equippedIndex > 3) return;
        int cardIndex = pendingReplacementCard;
        SkillMasterData skill = offeredSkills[cardIndex];
        DataManager dm = DataManager.Instance;
        if (skill == null || dm?.CharacterRuntimeStore == null ||
            !dm.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData character) || character == null ||
            !BattleRewardEquipSelectionPolicy.CanEquipRewardSkill(skill, characterId)) return;
        SkillInventoryEquipService.EnsureEquippedSkillArray(character);
        if (character.EquippedSkillIds == null || equippedIndex >= character.EquippedSkillIds.Length) return;
        // Replacement is restricted to an existing equipped skill, not a passive slot.
        if (string.IsNullOrWhiteSpace(character.EquippedSkillIds[equippedIndex])) return;
        ConfirmSkillSelection(cardIndex, characterId, skill.SkillId.Trim(), character, equippedIndex);
    }

    private void ConfirmSkillSelection(int index, string id, string skillId, CharacterRuntimeData character, int target)
    {
        // Replacement mode already started the unselected-card dissolve.
        // Fade the selected card only after the player confirms the replacement slot.
        bool isReplacement = pendingReplacementCard == index;
        pendingReplacementCard = -1;
        pendingReplacementCharacterId = null;
        RefreshReplacementSlotHovers();
        processing = true;
        if (isReplacement)
            StartSelectedCardFade(index);
        else
            PlaySelectionCardEffects(index, true);
        EquipSelectedSkill(id, skillId, character, target);
    }

    private void PlaySelectionCardEffects(int selectedIndex, bool fadeSelectedCard)
    {
        clickedCardIndex = selectedIndex;
        if (scaleCardRoots[selectedIndex] != null)
            scaleCardRoots[selectedIndex].localScale = baseCardScales[selectedIndex] * clickedScale;

        // Keep all three SkillPanel roots active to avoid LayoutGroup repositioning.
        foreach (Button choiceButton in buttons)
            if (choiceButton != null) choiceButton.interactable = false;
        for (int i = 0; i < scaleCardRoots.Length; i++)
        {
            if (i == selectedIndex || scaleCardRoots[i] == null) continue;
            cardHovered[i] = false;
            SetCardAudioEnabled(scaleCardRoots[i], false);
        }
        if (fadeSelectedCard)
            StartSelectedCardFade(selectedIndex);
        if (selectionDissolveCoroutine != null)
            StopCoroutine(selectionDissolveCoroutine);
        selectionDissolveCoroutine = StartCoroutine(DissolveUnselectedCards(selectedIndex));
    }

    private void StartSelectedCardFade(int index)
    {
        if (selectedCardFadeCoroutine != null)
        {
            StopCoroutine(selectedCardFadeCoroutine);
            selectedCardFadeCoroutine = null;
        }
        selectedCardFadeCoroutine = StartCoroutine(FadeSelectedCard(index));
    }

    private IEnumerator FadeSelectedCard(int index)
    {
        CanvasGroup group = selectedCardFadeGroups[index];
        if (group == null) yield break;
        group.alpha = 1f;
        float duration = Mathf.Max(0f, selectedCardFadeDuration);
        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration && reward != null)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }
        if (reward != null && group != null) group.alpha = 0f;
        selectedCardFadeCoroutine = null;
    }

    private void StopSelectedCardFade()
    {
        if (selectedCardFadeCoroutine != null)
        {
            StopCoroutine(selectedCardFadeCoroutine);
            selectedCardFadeCoroutine = null;
        }
        for (int i = 0; i < selectedCardFadeGroups.Length; i++)
        {
            if (selectedCardFadeGroups[i] != null) selectedCardFadeGroups[i].alpha = 1f;
            selectedCardFadeGroups[i] = null;
        }
    }

    private static CardUIStencilDissolveMaskController FindSelectionDissolveMask(Transform card)
    {
        if (card == null) return null;
        Transform mask = card.Find("Mask");
        if (mask != null)
        {
            CardUIStencilDissolveMaskController direct = mask.GetComponent<CardUIStencilDissolveMaskController>();
            if (direct != null) return direct;
        }
        return card.GetComponentInChildren<CardUIStencilDissolveMaskController>(true);
    }

    // Rebind each unselected card's own Mask each time a choice is made.
    // Never touch the selected Mask; it must remain visible until the slot is replaced.
    private CardUIStencilDissolveMaskController GetUnselectedDissolveMask(int index)
    {
        if (index < 0 || index >= scaleCardRoots.Length) return null;
        Transform card = scaleCardRoots[index];
        if (card == null) return null;
        Transform maskRoot = card.Find("Mask");
        CardUIStencilDissolveMaskController controller = maskRoot != null
            ? maskRoot.GetComponent<CardUIStencilDissolveMaskController>() : null;
        if (controller == null)
            controller = FindSelectionDissolveMask(card);
        selectionDissolveMasks[index] = controller;
        if (controller == null)
            Debug.LogWarning($"[BattleRewardSkillPanelUI] Missing CardUIStencilDissolveMaskController on {card.name}/Mask.", card);
        return controller;
    }

    private IEnumerator DissolveUnselectedCards(int selectedIndex)
    {
        // Only the two unselected Mask controllers dissolve. Never animate their alpha.
        // The Mask Image must use Dustium/UI/Card Stencil Dissolve Mask.
        // Its material uses procedural noise by default, so no texture is required.
        CardUIStencilDissolveMaskController[] targets = new CardUIStencilDissolveMaskController[3];
        for (int i = 0; i < targets.Length; i++)
        {
            if (i == selectedIndex || scaleCardRoots[i] == null ||
                !scaleCardRoots[i].gameObject.activeInHierarchy) continue;

            targets[i] = GetUnselectedDissolveMask(i);
            if (targets[i] != null)
            {
                targets[i].enabled = true;
                targets[i].ResetDissolve();
            }

            // Do not fade or disable the Mask root: that would bypass the dissolve
            // and would move remaining panels if a LayoutGroup is in use.
            CanvasGroup maskGroup = selectedCardFadeGroups[i];
            if (maskGroup != null)
            {
                maskGroup.alpha = 1f;
                maskGroup.interactable = false;
                maskGroup.blocksRaycasts = false;
            }
            ClearRarityVfx(i);
        }

        float duration = Mathf.Max(0f, unselectedCardDissolveDuration);
        float elapsed = 0f;
        while (elapsed < duration && reward != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float amount = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < targets.Length; i++)
                if (i != selectedIndex && targets[i] != null)
                    targets[i].SetDissolveAmount(amount);
            yield return null;
        }

        if (reward == null) yield break;
        for (int i = 0; i < targets.Length; i++)
            if (i != selectedIndex && targets[i] != null)
                targets[i].SetDissolveAmount(1f);
        selectionDissolveCoroutine = null;
    }

    private void EquipSelectedSkill(string characterId, string skillId,
        CharacterRuntimeData character, int targetSlot)
    {
        if (reward == null || !gameObject.activeInHierarchy) return;
        var dm = DataManager.Instance;
        if (dm == null || character == null) return;
        character.EquippedSkillIds[targetSlot] = skillId;
        dm.CharacterRuntimeStore.AddOrUpdate(character);
        BattleRuntimeData runtime = dm.BattleRuntimeStore.GetOrCreate();
        runtime.AcquiredSkillIds ??= new List<string>();
        if (!runtime.AcquiredSkillIds.Contains(skillId)) runtime.AcquiredSkillIds.Add(skillId);
        dm.BattleRuntimeStore.Set(runtime);
        RecordDiscoveryService.RegisterSkill(dm, skillId);
        BindPartyContent();
        // Equip VFX plays as soon as the skill appears in the target slot.
        PlayEquippedSkillVfx(characterId, targetSlot);
        equipCloseCoroutine = StartCoroutine(CloseAfterEquippedPreview());
    }

    private void PlayEquippedSkillVfx(string characterId, int equippedIndex)
    {
        ClearEquippedSkillVfx();
        if (equippedSkillVfxPrefab == null || string.IsNullOrWhiteSpace(characterId)) return;

        Transform content = charContent != null ? charContent : FindChild(transform, "Char_content");
        DataManager dm = DataManager.Instance;
        if (content == null || dm?.PartyRuntimeStore == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (!string.Equals(dm.PartyRuntimeStore.GetCharacterId(i), characterId, StringComparison.OrdinalIgnoreCase))
                continue;

            Transform partyCard = PartyRef(i)?.root != null ? PartyRef(i).root : FindChild(content, "Char" + (i + 1));
            if (partyCard == null) return;
            Transform skillSlot = FindChild(partyCard, "Skill/Skill0" + equippedIndex);
            if (skillSlot == null) return;

            // Center the effect on the icon; fall back to its slot if no icon exists.
            Transform parent = FindChild(skillSlot, "Icon") ?? skillSlot;
            activeEquippedSkillVfx = Instantiate(equippedSkillVfxPrefab, parent, false);
            activeEquippedSkillVfx.name = equippedSkillVfxPrefab.name + "_EquipVFX";
            activeEquippedSkillVfx.transform.localPosition = equippedSkillVfxOffset;
            activeEquippedSkillVfx.transform.localRotation = Quaternion.identity;
            activeEquippedSkillVfx.transform.localScale = equippedSkillVfxScale;
            activeEquippedSkillVfx.SetActive(true);
            return;
        }
    }

    private void ClearEquippedSkillVfx()
    {
        if (activeEquippedSkillVfx == null) return;
        activeEquippedSkillVfx.SetActive(false);
        Destroy(activeEquippedSkillVfx);
        activeEquippedSkillVfx = null;
    }

    private IEnumerator CloseAfterEquippedPreview()
    {
        // Wait until the selected-card fade, other-card dissolves, and equipped preview finish.
        float previewDelay = Mathf.Max(0f, equipCloseDelay, unselectedCardDissolveDuration, selectedCardFadeDuration);
        if (previewDelay > 0f)
            yield return new WaitForSecondsRealtime(previewDelay);

        CanvasGroup group = GetSkillPanelCloseCanvasGroup();
        float duration = Mathf.Max(0f, skillPanelCloseFadeDuration);
        if (group != null && duration > 0f)
        {
            float startAlpha = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = 0f;
        }
        equipCloseCoroutine = null;
        Finish();
    }

    private CanvasGroup GetSkillPanelCloseCanvasGroup()
    {
        if (skillPanelCloseCanvasGroup == null)
        {
            skillPanelCloseCanvasGroup = GetComponent<CanvasGroup>();
            if (skillPanelCloseCanvasGroup == null)
                skillPanelCloseCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        return skillPanelCloseCanvasGroup;
    }

    private void ResetSkillPanelCloseFade()
    {
        CanvasGroup group = GetSkillPanelCloseCanvasGroup();
        if (group != null) group.alpha = 1f;
    }

    public void DiscardReward() { if (reward != null && !processing) { processing = true; Finish(); } }

    private void CancelReplacementSelection()
    {
        pendingReplacementCard = -1;
        pendingReplacementCharacterId = null;
        foreach (RewardSkillEquippedSlotClick handler in replacementSlotHandlers)
            if (handler != null) handler.Configure(null, null, -1, false);
        replacementSlotHandlers.Clear();
    }

    private void Finish()
    {
        CancelReplacementSelection();
        ResetSkillPanelCloseFade();
        StopSelectedCardFade();
        if (selectionDissolveCoroutine != null)
        {
            StopCoroutine(selectionDissolveCoroutine);
            selectionDissolveCoroutine = null;
        }
        for (int i = 0; i < selectionDissolveMasks.Length; i++)
            if (selectionDissolveMasks[i] != null) selectionDissolveMasks[i].ResetDissolve();
        ClearEquippedSkillVfx();
        ResetAllCardScales();
        for (int i = 0; i < activeRarityVfx.Length; i++)
        {
            ClearRarityVfx(i);
            ReleaseCardDissolve(i);
            StopSkillReveal(i);
        }
        Action callback = completed;
        reward = null;
        completed = null;
        gameObject.SetActive(false);
        processing = false;
        callback?.Invoke();
    }

    public bool IsCardInteractionReady(int index)
    {
        return index >= 0 && index < cardInteractionReady.Length &&
               reward != null && !processing && cardInteractionReady[index];
    }

    private static void SetCardAudioEnabled(Transform card, bool allow)
    {
        if (card == null) return;
        foreach (DBAudioSource audio in card.GetComponentsInChildren<DBAudioSource>(true))
            audio.SetInteractionEnabled(allow);
    }

    private IEnumerator UnlockAfterConfiguredTime(int index)
    {
        float delay = GetCardDissolveStartDelay(index) + Mathf.Max(0f, dissolveDuration);
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        cardDissolveCoroutines[index] = null;
        UnlockCardInteraction(index);
    }

    private void UnlockCardInteraction(int index)
    {
        if (index < 0 || index >= cardInteractionReady.Length || reward == null || processing) return;
        Transform card = scaleCardRoots[index];
        if (card == null || !card.gameObject.activeInHierarchy) return;
        cardInteractionReady[index] = true;
        SetCardAudioEnabled(card, true);
        if (buttons[index] != null)
            buttons[index].interactable = !string.IsNullOrEmpty(characterIds[index]) &&
                offeredSkills[index] != null &&
                BattleRewardEquipSelectionPolicy.CanEquipRewardSkill(offeredSkills[index], characterIds[index]) &&
                true;
    }

    private void PrepareCardDissolve(int index, Transform card, SkillRarity rarity)
    {
        Image target = GetImage(card, "Card/Cardimage");
        if (target == null || target.material == null) return;
        Material source = target.material;
        if (!source.HasProperty(DissolveAmountId))
        {
            Debug.LogWarning($"[BattleRewardSkillPanelUI] Character/Icon material has no _DissolveAmount: {card.name}", target);
            return;
        }
        originalCardMaterials[index] = source;
        Material instance = new Material(source);
        instance.name = source.name + " (Reward Card " + (index + 1) + ")";
        instance.SetFloat(DissolveAmountId, 0f);
        if (instance.HasProperty(DissolveEdgeColorId))
            instance.SetColor(DissolveEdgeColorId, RarityColor(rarity));
        cardDissolveMaterials[index] = instance;
        cardDissolveImages[index] = target;
        target.material = instance;
        target.SetMaterialDirty();
        SetCardDissolveAmount(index, 0f);
    }

    // Unity UI Mask creates a separate stencil material for child graphics.
    // Update that rendered material as well as the Image's own material.
    private void SetCardDissolveAmount(int index, float amount)
    {
        if (index < 0 || index >= cardDissolveMaterials.Length) return;
        Material instance = cardDissolveMaterials[index];
        if (instance == null) return;

        amount = Mathf.Clamp01(amount);
        instance.SetFloat(DissolveAmountId, amount);

        Image image = cardDissolveImages[index];
        if (image == null || !image.isActiveAndEnabled) return;

        // materialForRendering contains the stencil variant generated by Mask.
        Material rendered = image.materialForRendering;
        if (rendered != null && rendered != instance && rendered.HasProperty(DissolveAmountId))
            rendered.SetFloat(DissolveAmountId, amount);
    }

    private IEnumerator AnimateCardDissolve(int index, float startDelay)
    {
        if (cardDissolveMaterials[index] == null) yield break;
        SetCardDissolveAmount(index, 0f);
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);
        float duration = Mathf.Max(0f, dissolveDuration);
        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration && cardDissolveMaterials[index] != null)
            {
                elapsed += Time.unscaledDeltaTime;
                SetCardDissolveAmount(index, elapsed / duration);
                yield return null;
            }
        }
        SetCardDissolveAmount(index, 1f);
        cardDissolveCoroutines[index] = null;
        UnlockCardInteraction(index);
    }

    private void ReleaseCardDissolve(int index)
    {
        if (cardDissolveCoroutines[index] != null)
        {
            StopCoroutine(cardDissolveCoroutines[index]);
            cardDissolveCoroutines[index] = null;
        }
        if (cardDissolveImages[index] != null && cardDissolveMaterials[index] != null &&
            cardDissolveImages[index].material == cardDissolveMaterials[index])
        {
            cardDissolveImages[index].material = originalCardMaterials[index];
            cardDissolveImages[index].SetMaterialDirty();
        }
        if (cardDissolveMaterials[index] != null)
            Destroy(cardDissolveMaterials[index]);
        cardDissolveMaterials[index] = null;
        cardDissolveImages[index] = null;
        originalCardMaterials[index] = null;
    }

    private void OnDestroy()
    {
        StopSelectedCardFade();
        ClearEquippedSkillVfx();
        ResetAllCardScales();
        for (int i = 0; i < cardDissolveMaterials.Length; i++)
        {
            ClearRarityVfx(i);
            ReleaseCardDissolve(i);
            StopSkillReveal(i);
        }
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
        SetSprite(GetImage(card, "Card/Cardimage"), null);
    }

    private void BindCard(Transform card, string characterId, SkillMasterData skill, BattleRewardData data)
    {
        Image line = GetImage(card, "Background/Line2");
        if (line != null) line.color = RarityColor(skill.Rarity);
        Image portrait = GetImage(card, "Card/Cardimage");
        Sprite side = null;
        if (!string.IsNullOrWhiteSpace(characterId))
        {
            var iconDatabase = DataManager.Instance.CharacterIconDatabase;
            if (iconDatabase != null)
            {
                iconDatabase.TryGetCardImage(characterId, out side);
                if (side == null)
                    iconDatabase.TryGetSideImage(characterId, out side);
            }
        }
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

    private GameObject GetRarityVfxPrefab(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Rare: return rareVfxPrefab;
            case SkillRarity.Epic: return epicVfxPrefab;
            case SkillRarity.Unique: return uniqueVfxPrefab;
            default: return commonVfxPrefab;
        }
    }

    private IEnumerator ShowRarityVfxAfterDissolveDelay(int index, Transform card, SkillRarity rarity, float startDelay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, startDelay));
        pendingRarityVfx[index] = null;
        if (reward != null && card != null && card.gameObject.activeInHierarchy && gameObject.activeInHierarchy)
            ShowRarityVfx(index, card, rarity);
    }

    private void ShowRarityVfx(int index, Transform card, SkillRarity rarity)
    {
        ClearRarityVfx(index);
        GameObject prefab = GetRarityVfxPrefab(rarity);
        if (prefab == null || card == null) return;

        Transform parent = SkillRef(index)?.rarityVfxRoot;
        if (parent == null || (parent != card && !parent.IsChildOf(card)))
            parent = card;

        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = prefab.name + "_RarityVFX";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        activeRarityVfx[index] = instance;
        instance.SetActive(true);
    }

    private void ClearRarityVfx(int index)
    {
        if (index < 0 || index >= activeRarityVfx.Length) return;
        if (pendingRarityVfx[index] != null)
        {
            StopCoroutine(pendingRarityVfx[index]);
            pendingRarityVfx[index] = null;
        }
        if (activeRarityVfx[index] == null) return;
        activeRarityVfx[index].SetActive(false);
        Destroy(activeRarityVfx[index]);
        activeRarityVfx[index] = null;
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

    private static Transform FindChild(Transform parent, string path)
    {
        if (parent == null || string.IsNullOrEmpty(path)) return null;
        Transform found = parent.Find(path);
        if (found != null) return found;
        // SkillPanel now contains Mask/Skill and Mask/Card.
        Transform mask = parent.Find("Mask");
        if (mask == null) return null;
        if (path == "Select") return mask.Find("Skill/Select");
        if (path == "Skill" || path == "Card" || path.StartsWith("Skill/") || path.StartsWith("Card/"))
            return mask.Find(path);
        return null;
    }

    private Image GetImage(Transform root, string path)
    {
        if (root != null)
        {
            Transform skill = FindChild(root, "Skill");
            Transform cardImage = FindChild(root, "Card/Cardimage");
            if (skill != null || cardImage != null)
            {
                Transform nested = path == "Card/Cardimage" ? cardImage : FindChild(root, "Skill/" + path);
                // Both Skill/Icon (Image) and Skill/Icon/Icon are supported.
                if (nested == null && path == "Icon/Icon") nested = FindChild(root, "Skill/Icon");
                if (nested != null)
                {
                    Image image = nested.GetComponent<Image>();
                    if (image != null) return image;
                }
            }
        }
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
                    case "Character/Icon": if (s.characterSideIcon != null) return s.characterSideIcon; break;
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
        if (root != null && FindChild(root, "Skill") != null)
        {
            Transform nested = FindChild(root, "Skill/" + path);
            if (nested != null)
            {
                TMP_Text label = nested.GetComponent<TMP_Text>();
                return label != null ? label : nested.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
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
    private BattleRewardSkillPanelUI panel;
    private int cardIndex = -1;

    public void Configure(GameObject target, BattleRewardSkillPanelUI owner, int index)
    {
        selection = target;
        panel = owner;
        cardIndex = index;
        if (selection != null) selection.SetActive(false);
        if (panel != null) panel.SetCardHovered(cardIndex, false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (panel == null || !panel.IsCardInteractionReady(cardIndex)) return;
        if (selection != null) selection.SetActive(true);
        panel.SetCardHovered(cardIndex, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selection != null) selection.SetActive(false);
        if (panel != null) panel.SetCardHovered(cardIndex, false);
    }

    private void OnDisable()
    {
        if (selection != null) selection.SetActive(false);
        if (panel != null) panel.SetCardHovered(cardIndex, false);
    }
}

// Click an already equipped active memory in Char_content to replace it.
// Passive slot (index 0) is never selectable.
public sealed class RewardSkillEquippedSlotClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private BattleRewardSkillPanelUI panel;
    private string characterId;
    private int equippedIndex = -1;
    private bool occupied;
    private bool pointerInside;
    private Transform hoverRoot;
    private TMP_Text hoverText;
    private CanvasGroup hoverCanvasGroup;

    public void Configure(BattleRewardSkillPanelUI owner, string id, int index, bool hasSkill)
    {
        panel = owner;
        characterId = id;
        equippedIndex = index;
        occupied = hasSkill;
        hoverRoot = transform.Find("Hover");
        hoverText = hoverRoot != null ? hoverRoot.Find("Text")?.GetComponent<TMP_Text>() : null;
        if (hoverText == null && hoverRoot != null)
            hoverText = hoverRoot.GetComponentInChildren<TMP_Text>(true);
        if (hoverRoot != null)
        {
            hoverCanvasGroup = hoverRoot.GetComponent<CanvasGroup>();
            if (hoverCanvasGroup == null)
                hoverCanvasGroup = hoverRoot.gameObject.AddComponent<CanvasGroup>();
            hoverCanvasGroup.interactable = false;
            hoverCanvasGroup.blocksRaycasts = false;
            // A label inside Hover must never swallow the slot's click/hover.
            foreach (Graphic graphic in hoverRoot.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            hoverRoot.gameObject.SetActive(true);
        }
        RefreshHover();
    }

    public void RefreshHover()
    {
        bool visible = pointerInside && occupied && panel != null &&
            panel.CanShowReplacementHover(characterId, equippedIndex);
        if (hoverText != null) hoverText.text = visible ? "교체" : string.Empty;
        if (hoverCanvasGroup != null) hoverCanvasGroup.alpha = visible ? 1f : 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        RefreshHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        RefreshHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && occupied)
            panel?.OnEquippedSlotClicked(characterId, equippedIndex);
    }

    private void OnDisable()
    {
        pointerInside = false;
        RefreshHover();
    }
}
