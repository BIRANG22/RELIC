using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTimelineGroupUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Turn Mark")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Image firstIconRootImage;
    [SerializeField] private Image firstIconImage;
    [SerializeField] private Image laterIconRootImage;
    [SerializeField] private Image laterIconImage;

    [Header("Legacy Owner Icon References")]
    [SerializeField] private Image playerIconRootImage;
    [SerializeField] private Image playerIconImage;
    [SerializeField] private Image enemyIconRootImage;
    [SerializeField] private Image enemyIconImage;

    [Header("Order Slots")]
    [SerializeField] private Image[] useSkillIconImages;
    [SerializeField] private TMP_Text[] useSkillValueTexts;

    [Header("Reserved Colors")]
    [SerializeField] private Color playerReservedColor = new Color32(0x0A, 0x46, 0x9E, 0xFF);
    [SerializeField] private Color enemyReservedColor = new Color32(0xDF, 0x4D, 0x56, 0xFF);
    [SerializeField] private Color deadReservationColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    [Header("Empty Use Skill Slots")]
    [SerializeField] private Color emptyUseSkillColor = new Color32(0xFF, 0xFF, 0xFF, 0x05);

    [Header("Selected Turn Mark")]
    [SerializeField] private Transform turnMarkTransform;
    [SerializeField] private Image turnMarkImage;
    [SerializeField] private Color selectedTurnMarkColorA = new Color32(0x00, 0x00, 0x00, 0xFF);
    [SerializeField] private Color selectedTurnMarkColorB = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    [SerializeField] private float selectedTurnMarkScale = 1.2f;
    [SerializeField] private float selectedTurnMarkBreathScale = 0.06f;
    [SerializeField] private float selectedTurnMarkBreathSpeed = 3f;
    [SerializeField] private float selectedTurnMarkColorSpeed = 4f;

    private readonly List<BattleTimelinePreviewEntry> currentEntries = new();
    private readonly List<bool> currentEntryOwnerDeadStates = new();
    private BattleTimelinePreviewEntry firstOwnerEntry;
    private BattleTimelinePreviewEntry laterOwnerEntry;
    private bool firstOwnerDeadState;
    private bool laterOwnerDeadState;

    private BattleTimelineBarUI owner;
    private int slotIndex;
    private bool isActive;
    private bool emptyUseSkillSlotsVisible = false;

    private Vector3 turnMarkNormalScale = Vector3.one;
    private bool hasCachedTurnMarkVisual;
    private Color turnMarkNormalImageColor = Color.white;
    private Sprite turnMarkNormalSprite;
    private Sprite[] useSkillRootNormalSprites;
    private Color[] useSkillRootNormalColors;
    private bool hasCachedTrailingVisualDefaults;

    private string enemyOwnerIconMonsterRuntimeId = "";
    private MonsterUnit hoveredEnemyOwnerIconMonster;
    private GameObject registeredEnemyOwnerIconImageObject;
    private GameObject registeredEnemyOwnerIconRootObject;

    private string playerOwnerIconCharacterId = "";
    private BattleCharacter hoveredPlayerOwnerIconCharacter;
    private GameObject registeredPlayerOwnerIconImageObject;
    private GameObject registeredPlayerOwnerIconRootObject;

    [Header("Owner Icon Hover Scale")]
    [SerializeField] private float ownerIconHoverScaleMultiplier = 1.1f;
    [SerializeField] private float ownerIconHoverScaleDuration = 0.12f;

    private Transform ownerIconScaleTransform;
    private Vector3 ownerIconBaseScale = Vector3.one;
    private Coroutine ownerIconScaleRoutine;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        AutoFindReferences();
        CacheTurnMarkNormalVisual();
        CacheTrailingVisualDefaults();
        ApplyTurnMarkSelectedVisual(false);
    }

    private void Update()
    {
        UpdateTurnMarkSelectedAnimation();
        UpdateDeadReservationVisuals();
    }

    public void Init(BattleTimelineBarUI owner, int slotIndex)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;

        AutoFindReferences();
        CacheTurnMarkNormalVisual();
        CacheTrailingVisualDefaults();
        ApplyTurnMarkSelectedVisual(isActive);
        ApplyTurnText();
    }

    public void SetActiveTimelineSlot(bool active)
    {
        isActive = active;
        ApplyTurnMarkSelectedVisual(active);
    }

    public void SetOwnerIconsVisible(bool visible)
    {
        if (!visible)
        {
            SetOwnerIconImage(firstIconRootImage, firstIconImage, null, false, Color.white);
            SetOwnerIconImage(laterIconRootImage, laterIconImage, null, false, Color.white);
            SetOwnerIconImage(playerIconRootImage, playerIconImage, null, false, playerReservedColor);
            SetOwnerIconImage(enemyIconRootImage, enemyIconImage, null, false, enemyReservedColor);
            ClearEnemyOwnerIconHudHoverTarget();
            ClearPlayerOwnerIconHudHoverTarget();
            return;
        }

        // 실제 표시 여부는 SetTimelineEntries에서 현재 실행 순서에 맞춰 다시 계산합니다.
    }

    public void SetTimelineEntries(IReadOnlyList<BattleTimelinePreviewEntry> entries, int targetSlotIndex)
    {
        Clear();
        slotIndex = targetSlotIndex;
        ApplyTurnText();

        if (entries == null || entries.Count <= 0)
            return;

        BattleTimelinePreviewEntry firstPlayerEntry = null;
        BattleTimelinePreviewEntry firstMonsterEntry = null;
        int firstPlayerIndex = int.MaxValue;
        int firstMonsterIndex = int.MaxValue;

        int visibleIndex = 0;
        int maxOrderCount = 5;

        for (int i = 0; i < entries.Count; i++)
        {
            BattleTimelinePreviewEntry entry = entries[i];

            if (entry == null)
                continue;

            if (visibleIndex >= maxOrderCount)
                break;

            currentEntries.Add(entry);
            bool ownerDead = IsEntryOwnerDead(entry);
            currentEntryOwnerDeadStates.Add(ownerDead);

            if (entry.IsMonster && firstMonsterEntry == null)
            {
                firstMonsterEntry = entry;
                firstMonsterIndex = visibleIndex;
            }
            else if (entry.IsPlayer && firstPlayerEntry == null)
            {
                firstPlayerEntry = entry;
                firstPlayerIndex = visibleIndex;
            }

            Color reservedColor = entry.IsMonster ? enemyReservedColor : playerReservedColor;

            if (useSkillIconImages != null && visibleIndex < useSkillIconImages.Length)
            {
                Image useSkillImage = useSkillIconImages[visibleIndex];

                SetSkillImage(useSkillImage, entry.SkillIcon, true, reservedColor);
                ApplySkillReservationColor(useSkillImage, entry, ownerDead);
                SetSkillValueText(useSkillValueTexts, visibleIndex, entry.SkillValueText);

                if (entry.IsMonster)
                    SetupEnemySkillHoverTarget(useSkillImage, entry);
                else
                    SetupPlayerSkillHoverTarget(useSkillImage, entry);
            }

            visibleIndex++;
        }

        BattleTimelinePreviewEntry firstEntry = null;
        BattleTimelinePreviewEntry laterEntry = null;

        if (firstPlayerEntry != null && firstMonsterEntry != null)
        {
            bool playerFirst = firstPlayerIndex < firstMonsterIndex;
            firstEntry = playerFirst ? firstPlayerEntry : firstMonsterEntry;
            laterEntry = playerFirst ? firstMonsterEntry : firstPlayerEntry;
        }
        else
        {
            firstEntry = firstPlayerEntry ?? firstMonsterEntry;
        }

        firstOwnerEntry = firstEntry;
        laterOwnerEntry = laterEntry;
        firstOwnerDeadState = IsEntryOwnerDead(firstEntry);
        laterOwnerDeadState = IsEntryOwnerDead(laterEntry);

        ApplyOwnerOrderIcon(firstIconRootImage, firstIconImage, firstEntry);
        ApplyOwnerOrderIcon(laterIconRootImage, laterIconImage, laterEntry);

        SetupOwnerIconInteractionTarget(firstIconImage, firstEntry);
        SetupOwnerIconInteractionTarget(laterIconImage, laterEntry);
    }

    public void Clear()
    {
        currentEntries.Clear();
        currentEntryOwnerDeadStates.Clear();
        firstOwnerEntry = null;
        laterOwnerEntry = null;
        firstOwnerDeadState = false;
        laterOwnerDeadState = false;

        SetOwnerIconImage(firstIconRootImage, firstIconImage, null, false, Color.white);
        SetOwnerIconImage(laterIconRootImage, laterIconImage, null, false, Color.white);
        SetOwnerIconImage(playerIconRootImage, playerIconImage, null, false, playerReservedColor);
        SetOwnerIconImage(enemyIconRootImage, enemyIconImage, null, false, enemyReservedColor);
        ClearEnemyOwnerIconHudHoverTarget();
        ClearPlayerOwnerIconHudHoverTarget();

        if (useSkillIconImages != null)
        {
            for (int i = 0; i < useSkillIconImages.Length; i++)
            {
                ClearSkillImage(useSkillIconImages[i]);
            }
        }

        ClearSkillValueTexts(useSkillValueTexts);
    }

    private void ClearSkillImage(Image image)
    {
        if (image == null)
            return;

        image.sprite = null;
        image.color = Color.white;
        image.enabled = false;
        image.gameObject.SetActive(false);
        image.raycastTarget = false;

        GameObject hoverObject = GetSkillHoverObject(image);

        if (hoverObject != null)
        {
            if (emptyUseSkillSlotsVisible)
                ShowEmptyUseSkillSlot(hoverObject);
            else
                HideEmptyUseSkillSlot(hoverObject);
        }

        ClearSkillHoverTarget(image);
    }

    private void SetupOwnerIconInteractionTarget(Image ownerIconImage, BattleTimelinePreviewEntry entry)
    {
        if (entry == null)
            return;

        if (entry.IsMonster)
            SetupEnemyOwnerIconHudHoverTarget(ownerIconImage, entry.MonsterRuntimeId);
        else if (entry.IsPlayer)
            SetupPlayerOwnerIconHudHoverTarget(ownerIconImage, entry.OwnerId);
    }

    private void SetupEnemyOwnerIconHudHoverTarget(Image ownerIconImage, string monsterRuntimeId)
    {
        if (ownerIconImage == null || string.IsNullOrWhiteSpace(monsterRuntimeId))
        {
            ClearEnemyOwnerIconHudHoverTarget(false);
            return;
        }

        if (registeredEnemyOwnerIconImageObject == ownerIconImage.gameObject &&
            enemyOwnerIconMonsterRuntimeId == monsterRuntimeId)
        {
            ownerIconImage.raycastTarget = true;
            return;
        }

        ClearEnemyOwnerIconHudHoverTarget(false);
        enemyOwnerIconMonsterRuntimeId = monsterRuntimeId;

        GameObject imageObject = ownerIconImage.gameObject;
        CacheOwnerIconBaseScale(ownerIconImage.transform);
        GameObject rootObject = ownerIconImage.transform.parent != null
            ? ownerIconImage.transform.parent.gameObject
            : imageObject;

        RegisterEnemyOwnerIconHoverEvents(imageObject, true);

        if (rootObject != imageObject)
            RegisterEnemyOwnerIconHoverEvents(rootObject, false);
    }

    private void ClearEnemyOwnerIconHudHoverTarget(bool clearRegisteredObjects = true)
    {
        HideEnemyOwnerIconHover();
        enemyOwnerIconMonsterRuntimeId = "";

        if (registeredEnemyOwnerIconImageObject != null)
        {
            Image image = registeredEnemyOwnerIconImageObject.GetComponent<Image>();

            if (image != null)
                image.raycastTarget = false;
        }

        if (registeredEnemyOwnerIconRootObject != null)
        {
            Image image = registeredEnemyOwnerIconRootObject.GetComponent<Image>();

            if (image != null)
                image.raycastTarget = false;
        }

        if (clearRegisteredObjects)
        {
            registeredEnemyOwnerIconImageObject = null;
            registeredEnemyOwnerIconRootObject = null;
        }
    }

    private void RegisterEnemyOwnerIconHoverEvents(GameObject targetObject, bool isImageObject)
    {
        if (targetObject == null)
            return;

        if (isImageObject)
            registeredEnemyOwnerIconImageObject = targetObject;
        else
            registeredEnemyOwnerIconRootObject = targetObject;

        Image image = targetObject.GetComponent<Image>();

        if (image == null)
        {
            image = targetObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        image.raycastTarget = true;

        EventTrigger trigger = targetObject.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = targetObject.AddComponent<EventTrigger>();

        RemoveEnemyOwnerIconHoverEvents(trigger);

        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };

        enterEntry.callback.AddListener(_ => ShowEnemyOwnerIconHover());
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };

        exitEntry.callback.AddListener(_ => HideEnemyOwnerIconHover());
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry clickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };

        clickEntry.callback.AddListener(_ => SelectEnemyOwnerIcon());
        trigger.triggers.Add(clickEntry);
    }

    private void RemoveEnemyOwnerIconHoverEvents(EventTrigger trigger)
    {
        if (trigger == null || trigger.triggers == null)
            return;

        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            EventTrigger.Entry entry = trigger.triggers[i];

            if (entry == null)
                continue;

            if (entry.eventID == EventTriggerType.PointerEnter ||
                entry.eventID == EventTriggerType.PointerExit ||
                entry.eventID == EventTriggerType.PointerClick)
            {
                trigger.triggers.RemoveAt(i);
            }
        }
    }

    private void SelectEnemyOwnerIcon()
    {
        MonsterUnit monster = FindEnemyOwnerIconMonster();

        if (monster != null)
            monster.SelectForInfoFromTimeline();
    }

    private void ShowEnemyOwnerIconHover()
    {
        AnimateOwnerIconScale(true);
        ShowEnemyOwnerIconHUD();

        if (hoveredEnemyOwnerIconMonster != null)
        {
            hoveredEnemyOwnerIconMonster.SetTimelineHoverHighlight(true);
            hoveredEnemyOwnerIconMonster.ShowAttackRangePreviewFromTimeline();
        }
    }

    private void HideEnemyOwnerIconHover()
    {
        AnimateOwnerIconScale(false);

        if (hoveredEnemyOwnerIconMonster == null)
            hoveredEnemyOwnerIconMonster = FindEnemyOwnerIconMonster();

        if (hoveredEnemyOwnerIconMonster != null)
        {
            hoveredEnemyOwnerIconMonster.SetTimelineHoverHighlight(false);
            hoveredEnemyOwnerIconMonster.HideAttackRangePreviewFromTimeline();
        }

        HideHoveredEnemyOwnerIconHUD();
    }

    private void ShowEnemyOwnerIconHUD()
    {
        hoveredEnemyOwnerIconMonster = FindEnemyOwnerIconMonster();

        if (hoveredEnemyOwnerIconMonster != null)
            hoveredEnemyOwnerIconMonster.ShowAndRefreshHUD();
    }

    private void HideHoveredEnemyOwnerIconHUD()
    {
        if (hoveredEnemyOwnerIconMonster == null)
            hoveredEnemyOwnerIconMonster = FindEnemyOwnerIconMonster();

        if (hoveredEnemyOwnerIconMonster != null)
            hoveredEnemyOwnerIconMonster.HideHUDIfNotSelected();

        hoveredEnemyOwnerIconMonster = null;
    }

    private MonsterUnit FindEnemyOwnerIconMonster()
    {
        if (string.IsNullOrWhiteSpace(enemyOwnerIconMonsterRuntimeId))
            return null;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.RuntimeId == enemyOwnerIconMonsterRuntimeId)
                return monsters[i];
        }

        return null;
    }

    private void SetupPlayerOwnerIconHudHoverTarget(Image ownerIconImage, string characterId)
    {
        if (ownerIconImage == null || string.IsNullOrWhiteSpace(characterId))
        {
            ClearPlayerOwnerIconHudHoverTarget(false);
            return;
        }

        // 동일한 타임라인 아이콘이 같은 캐릭터를 계속 표시하는 동안에는
        // Hover 상태를 초기화하지 않습니다. 타임라인 갱신은 매 프레임 발생할 수 있으므로,
        // 여기서 Clear를 반복하면 CharacterHUDSlot이 Hover 직후 다시 숨겨집니다.
        if (registeredPlayerOwnerIconImageObject == ownerIconImage.gameObject &&
            playerOwnerIconCharacterId == characterId)
        {
            ownerIconImage.raycastTarget = true;
            return;
        }

        ClearPlayerOwnerIconHudHoverTarget(false);
        playerOwnerIconCharacterId = characterId;

        GameObject imageObject = ownerIconImage.gameObject;
        CacheOwnerIconBaseScale(ownerIconImage.transform);
        GameObject rootObject = ownerIconImage.transform.parent != null
            ? ownerIconImage.transform.parent.gameObject
            : imageObject;

        RegisterPlayerOwnerIconEvents(imageObject, true);

        if (rootObject != imageObject)
            RegisterPlayerOwnerIconEvents(rootObject, false);
    }

    private void ClearPlayerOwnerIconHudHoverTarget(bool clearRegisteredObjects = true)
    {
        HidePlayerOwnerIconHover();
        playerOwnerIconCharacterId = "";

        if (registeredPlayerOwnerIconImageObject != null)
        {
            Image image = registeredPlayerOwnerIconImageObject.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }

        if (registeredPlayerOwnerIconRootObject != null)
        {
            Image image = registeredPlayerOwnerIconRootObject.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }

        if (clearRegisteredObjects)
        {
            registeredPlayerOwnerIconImageObject = null;
            registeredPlayerOwnerIconRootObject = null;
        }
    }

    private void RegisterPlayerOwnerIconEvents(GameObject targetObject, bool isImageObject)
    {
        if (targetObject == null)
            return;

        if (isImageObject)
            registeredPlayerOwnerIconImageObject = targetObject;
        else
            registeredPlayerOwnerIconRootObject = targetObject;

        Image image = targetObject.GetComponent<Image>();
        if (image == null)
        {
            image = targetObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        image.raycastTarget = true;

        EventTrigger trigger = targetObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = targetObject.AddComponent<EventTrigger>();

        RemovePlayerOwnerIconEvents(trigger);

        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener(_ => ShowPlayerOwnerIconHover());
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener(_ => HidePlayerOwnerIconHover());
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry clickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        clickEntry.callback.AddListener(_ => SelectPlayerOwnerIcon());
        trigger.triggers.Add(clickEntry);
    }

    private void RemovePlayerOwnerIconEvents(EventTrigger trigger)
    {
        if (trigger == null || trigger.triggers == null)
            return;

        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            EventTrigger.Entry entry = trigger.triggers[i];
            if (entry == null)
                continue;

            if (entry.eventID == EventTriggerType.PointerEnter ||
                entry.eventID == EventTriggerType.PointerExit ||
                entry.eventID == EventTriggerType.PointerClick)
            {
                trigger.triggers.RemoveAt(i);
            }
        }
    }

    private void ShowPlayerOwnerIconHover()
    {
        AnimateOwnerIconScale(true);
        ShowPlayerOwnerIconHUD();
    }

    private void HidePlayerOwnerIconHover()
    {
        AnimateOwnerIconScale(false);
        HideHoveredPlayerOwnerIconHUD();
    }

    private void ShowPlayerOwnerIconHUD()
    {
        hoveredPlayerOwnerIconCharacter = FindPlayerOwnerIconCharacter();

        if (hoveredPlayerOwnerIconCharacter == null)
            return;

        hoveredPlayerOwnerIconCharacter.SetTimelineHoverHighlight(true);

        BattleCharacterHUDController hudController = Object.FindFirstObjectByType<BattleCharacterHUDController>(FindObjectsInactive.Include);
        if (hudController != null)
            hudController.ShowTimelineIconCharacterHUD(hoveredPlayerOwnerIconCharacter);
    }

    private void HideHoveredPlayerOwnerIconHUD()
    {
        if (hoveredPlayerOwnerIconCharacter == null)
            hoveredPlayerOwnerIconCharacter = FindPlayerOwnerIconCharacter();

        if (hoveredPlayerOwnerIconCharacter != null)
            hoveredPlayerOwnerIconCharacter.SetTimelineHoverHighlight(false);

        BattleCharacterHUDController hudController = Object.FindFirstObjectByType<BattleCharacterHUDController>(FindObjectsInactive.Include);
        if (hudController != null)
            hudController.HideTimelineIconCharacterHUD(hoveredPlayerOwnerIconCharacter);

        hoveredPlayerOwnerIconCharacter = null;
    }

    private void CacheOwnerIconBaseScale(Transform iconTransform)
    {
        if (iconTransform == null)
            return;

        if (ownerIconScaleTransform == iconTransform)
            return;

        ResetOwnerIconScaleImmediate();
        ownerIconScaleTransform = iconTransform;
        ownerIconBaseScale = iconTransform.localScale;
    }

    private void AnimateOwnerIconScale(bool hovered)
    {
        if (ownerIconScaleTransform == null)
            return;

        Vector3 targetScale = hovered
            ? Vector3.Scale(ownerIconBaseScale, Vector3.one * ownerIconHoverScaleMultiplier)
            : ownerIconBaseScale;

        // 전투방 정리 과정에서는 TimelineSlot이 먼저 비활성화될 수 있습니다.
        // 비활성화된 MonoBehaviour에서는 코루틴을 시작할 수 없으므로
        // 이 경우 애니메이션 없이 즉시 목표 스케일로 복원합니다.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ownerIconScaleRoutine = null;

            if (ownerIconScaleTransform != null)
                ownerIconScaleTransform.localScale = targetScale;

            return;
        }

        if (ownerIconScaleRoutine != null)
        {
            StopCoroutine(ownerIconScaleRoutine);
            ownerIconScaleRoutine = null;
        }

        ownerIconScaleRoutine = StartCoroutine(AnimateOwnerIconScale(ownerIconScaleTransform, targetScale));
    }

    private IEnumerator AnimateOwnerIconScale(Transform target, Vector3 targetScale)
    {
        if (target == null)
            yield break;

        Vector3 startScale = target.localScale;
        float duration = Mathf.Max(0f, ownerIconHoverScaleDuration);

        if (duration <= 0f)
        {
            target.localScale = targetScale;
            ownerIconScaleRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            target.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        if (target != null)
            target.localScale = targetScale;

        ownerIconScaleRoutine = null;
    }

    private void ResetOwnerIconScaleImmediate()
    {
        if (ownerIconScaleRoutine != null)
        {
            StopCoroutine(ownerIconScaleRoutine);
            ownerIconScaleRoutine = null;
        }

        if (ownerIconScaleTransform != null)
            ownerIconScaleTransform.localScale = ownerIconBaseScale;
    }

    private void SelectPlayerOwnerIcon()
    {
        BattleCharacter character = FindPlayerOwnerIconCharacter();
        if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
            return;

        BattleRoomLoader roomLoader = Object.FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);
        if (roomLoader != null)
            roomLoader.OnPlayerCharacterClicked(character.RuntimeData);
    }

    private BattleCharacter FindPlayerOwnerIconCharacter()
    {
        if (string.IsNullOrWhiteSpace(playerOwnerIconCharacterId))
            return null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || characters[i].RuntimeData == null)
                continue;

            if (characters[i].RuntimeData.CharacterId == playerOwnerIconCharacterId)
                return characters[i];
        }

        return null;
    }

    private void OnDisable()
    {
        HideHoveredEnemyOwnerIconHUD();
        HideHoveredPlayerOwnerIconHUD();
    }

    private void SetupEnemySkillHoverTarget(Image skillImage, BattleTimelinePreviewEntry entry)
    {
        GameObject hoverObject = GetSkillHoverObject(skillImage);

        if (hoverObject == null)
            return;

        BattleTimelineMonsterHoverTarget monsterHoverTarget =
            hoverObject.GetComponent<BattleTimelineMonsterHoverTarget>();

        if (monsterHoverTarget == null)
            monsterHoverTarget = hoverObject.AddComponent<BattleTimelineMonsterHoverTarget>();

        monsterHoverTarget.SetMonsterRuntimeId(entry != null ? entry.MonsterRuntimeId : "");

        TimelineSkillIconHoverUI skillHoverUI =
            hoverObject.GetComponent<TimelineSkillIconHoverUI>();

        if (skillHoverUI == null)
            skillHoverUI = hoverObject.AddComponent<TimelineSkillIconHoverUI>();

        skillHoverUI.Setup(entry);
        EnsureHoverRaycastTarget(hoverObject);

        if (skillImage != null)
            skillImage.raycastTarget = true;
    }

    private void SetupPlayerSkillHoverTarget(Image skillImage, BattleTimelinePreviewEntry entry)
    {
        GameObject hoverObject = GetSkillHoverObject(skillImage);

        if (hoverObject == null)
            return;

        BattleTimelineCharacterHoverTarget characterHoverTarget =
            hoverObject.GetComponent<BattleTimelineCharacterHoverTarget>();

        if (characterHoverTarget == null)
            characterHoverTarget = hoverObject.AddComponent<BattleTimelineCharacterHoverTarget>();

        characterHoverTarget.SetCharacterId(entry != null ? entry.OwnerId : "");

        TimelineSkillIconHoverUI skillHoverUI =
            hoverObject.GetComponent<TimelineSkillIconHoverUI>();

        if (skillHoverUI == null)
            skillHoverUI = hoverObject.AddComponent<TimelineSkillIconHoverUI>();

        skillHoverUI.Setup(entry);
        EnsureHoverRaycastTarget(hoverObject);

        if (skillImage != null)
            skillImage.raycastTarget = true;
    }

    private void ClearSkillHoverTarget(Image skillImage)
    {
        GameObject hoverObject = GetSkillHoverObject(skillImage);

        ClearSkillHoverComponents(skillImage != null ? skillImage.gameObject : null);

        if (hoverObject != null && (skillImage == null || hoverObject != skillImage.gameObject))
            ClearSkillHoverComponents(hoverObject);
    }

    private void ClearSkillHoverComponents(GameObject target)
    {
        if (target == null)
            return;

        BattleTimelineMonsterHoverTarget monsterHoverTarget =
            target.GetComponent<BattleTimelineMonsterHoverTarget>();

        if (monsterHoverTarget != null)
            monsterHoverTarget.SetMonsterRuntimeId("");

        BattleTimelineCharacterHoverTarget characterHoverTarget =
            target.GetComponent<BattleTimelineCharacterHoverTarget>();

        if (characterHoverTarget != null)
            characterHoverTarget.SetCharacterId("");

        TimelineSkillIconHoverUI skillHoverUI =
            target.GetComponent<TimelineSkillIconHoverUI>();

        if (skillHoverUI != null)
            skillHoverUI.Clear();
    }

    private GameObject GetSkillHoverObject(Image skillImage)
    {
        if (skillImage == null)
            return null;

        if (skillImage.gameObject.name == "Use_skill")
            return skillImage.gameObject;

        Transform parent = skillImage.transform.parent;

        if (parent != null)
            return parent.gameObject;

        return skillImage.gameObject;
    }

    private void EnsureHoverRaycastTarget(GameObject hoverObject)
    {
        if (hoverObject == null)
            return;

        Image image = hoverObject.GetComponent<Image>();

        if (image == null)
        {
            image = hoverObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        image.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnTimelineSlotClicked(slotIndex);
    }

    public void OnOrderClicked(int orderIndex)
    {
        if (orderIndex < 0)
            return;

        if (orderIndex >= currentEntries.Count)
        {
            if (owner != null)
                owner.OnTimelineSlotClicked(slotIndex);

            return;
        }

        BattleTimelinePreviewEntry entry = currentEntries[orderIndex];

        if (entry == null)
        {
            if (owner != null)
                owner.OnTimelineSlotClicked(slotIndex);

            return;
        }

        TimelineReservationHoverPreview.HideCurrent();

        if (entry.IsMonster)
        {
            SelectMonsterOrderSkill(entry);
            return;
        }

        if (owner != null)
            owner.OnEntryClicked(entry);
    }

    private void SelectMonsterOrderSkill(BattleTimelinePreviewEntry entry)
    {
        if (entry == null || !entry.IsMonster || entry.MonsterSkillData == null)
            return;

        MonsterUnit monster = FindMonsterByRuntimeId(entry.MonsterRuntimeId);
        if (monster == null)
            return;

        monster.SelectForInfoFromTimeline();

        BattleCharacterPanelUI panel = Object.FindFirstObjectByType<BattleCharacterPanelUI>(
            FindObjectsInactive.Include);

        if (panel != null)
            panel.SelectMonsterSkillFromTimeline(monster, entry.MonsterSkillData.SkillId);
    }

    private MonsterUnit FindMonsterByRuntimeId(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            return null;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster == null || monster.RuntimeData == null)
                continue;

            if (monster.RuntimeData.RuntimeId == runtimeId)
                return monster;
        }

        return null;
    }

    private void AutoFindReferences()
    {
        if (turnMarkTransform == null)
            turnMarkTransform = FindChildRecursive(transform, "TurnMark");

        if (turnMarkImage == null && turnMarkTransform != null)
            turnMarkImage = turnMarkTransform.GetComponent<Image>();

        if (turnText == null && turnMarkTransform != null)
        {
            Transform turnTextTransform = FindChildRecursive(turnMarkTransform, "Turn_Text");
            if (turnTextTransform != null)
                turnText = turnTextTransform.GetComponent<TMP_Text>();
        }

        if (firstIconRootImage == null)
            firstIconRootImage = FindRootImage("First_Icon");

        if (firstIconImage == null)
            firstIconImage = FindImage("First_Icon", "image");

        if (laterIconRootImage == null)
            laterIconRootImage = FindRootImage("Later_Icon");

        if (laterIconImage == null)
            laterIconImage = FindImage("Later_Icon", "image");

        if (playerIconRootImage == null)
            playerIconRootImage = FindRootImage("Player_Icon");

        if (playerIconImage == null)
            playerIconImage = FindImage("Player_Icon", "image");

        if (enemyIconRootImage == null)
            enemyIconRootImage = FindRootImage("Enemy_Icon");

        if (enemyIconImage == null)
            enemyIconImage = FindImage("Enemy_Icon", "image");

        if (useSkillIconImages == null || useSkillIconImages.Length == 0)
            useSkillIconImages = FindOrderUseSkillImages();

        if (useSkillValueTexts == null || useSkillValueTexts.Length == 0)
            useSkillValueTexts = FindOrderUseSkillTexts();

        EnsureButton();
        SetupTurnMarkClickTarget();
        SetupOrderClickTargets();
    }

    private void CacheTrailingVisualDefaults()
    {
        if (hasCachedTrailingVisualDefaults)
            return;

        if (turnMarkImage != null)
            turnMarkNormalSprite = turnMarkImage.sprite;

        int count = useSkillIconImages != null ? useSkillIconImages.Length : 0;
        useSkillRootNormalSprites = new Sprite[count];
        useSkillRootNormalColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            Image skillIcon = useSkillIconImages[i];
            GameObject root = GetSkillHoverObject(skillIcon);
            Image rootImage = root != null ? root.GetComponent<Image>() : null;

            if (rootImage == null)
                continue;

            useSkillRootNormalSprites[i] = rootImage.sprite;
            useSkillRootNormalColors[i] = rootImage.color;
        }

        hasCachedTrailingVisualDefaults = true;
    }

    public void PrepareTrailingDecorationVisuals()
    {
        AutoFindReferences();
        CacheTurnMarkNormalVisual();
        CacheTrailingVisualDefaults();

        SetActiveTimelineSlot(false);
        SetOwnerIconsVisible(false);

        if (turnMarkTransform != null)
            turnMarkTransform.gameObject.SetActive(true);

        if (turnMarkImage != null)
        {
            if (turnMarkNormalSprite != null)
                turnMarkImage.sprite = turnMarkNormalSprite;

            Color color = turnMarkNormalImageColor;
            color.a = 1f;
            turnMarkImage.color = color;
            turnMarkImage.enabled = true;
            turnMarkImage.raycastTarget = false;
        }

        if (useSkillIconImages != null)
        {
            for (int i = 0; i < useSkillIconImages.Length; i++)
            {
                Image skillIcon = useSkillIconImages[i];
                if (skillIcon == null)
                    continue;

                GameObject root = GetSkillHoverObject(skillIcon);
                if (root != null)
                {
                    Image rootImage = root.GetComponent<Image>();
                    if (rootImage != null)
                    {
                        if (useSkillRootNormalSprites != null && i < useSkillRootNormalSprites.Length &&
                            useSkillRootNormalSprites[i] != null)
                        {
                            rootImage.sprite = useSkillRootNormalSprites[i];
                        }

                        Color color = Color.white;
                        if (useSkillRootNormalColors != null && i < useSkillRootNormalColors.Length)
                            color = useSkillRootNormalColors[i];

                        color.a = 1f;
                        rootImage.color = color;
                        rootImage.enabled = true;
                        rootImage.raycastTarget = false;
                    }

                    // 아직 행동이 등록되지 않은 Next 슬롯의 Order 프레임은 표시하지 않습니다.
                    root.SetActive(false);
                }

                skillIcon.sprite = null;
                skillIcon.color = Color.white;
                skillIcon.enabled = false;
                skillIcon.gameObject.SetActive(false);
                skillIcon.raycastTarget = false;
            }
        }

        ClearSkillValueTexts(useSkillValueTexts);
    }

    private void CacheTurnMarkNormalVisual()
    {
        if (hasCachedTurnMarkVisual)
            return;

        if (turnMarkTransform == null)
            return;

        turnMarkNormalScale = turnMarkTransform.localScale;

        if (turnMarkImage != null)
            turnMarkNormalImageColor = turnMarkImage.color;

        hasCachedTurnMarkVisual = true;
    }

    private void UpdateTurnMarkSelectedAnimation()
    {
        if (turnMarkTransform == null)
            return;

        if (!isActive)
            return;

        float breath = (Mathf.Sin(Time.unscaledTime * selectedTurnMarkBreathSpeed) + 1f) * 0.5f;
        float scale = selectedTurnMarkScale + (breath * selectedTurnMarkBreathScale);
        turnMarkTransform.localScale = turnMarkNormalScale * scale;

        ApplyTurnMarkSelectedBlinkColor();
    }

    private void ApplyTurnMarkSelectedVisual(bool selected)
    {
        CacheTurnMarkNormalVisual();

        if (turnMarkTransform == null)
            return;

        if (selected)
            ApplyTurnMarkSelectedBlinkColor();
        else
        {
            if (turnMarkImage != null)
                turnMarkImage.color = turnMarkNormalImageColor;
        }

        if (!selected)
            turnMarkTransform.localScale = turnMarkNormalScale;
        else
            turnMarkTransform.localScale = turnMarkNormalScale * selectedTurnMarkScale;
    }

    private void ApplyTurnMarkSelectedBlinkColor()
    {
        float t = (Mathf.Sin(Time.unscaledTime * selectedTurnMarkColorSpeed) + 1f) * 0.5f;
        Color blinkColor = Color.Lerp(selectedTurnMarkColorA, selectedTurnMarkColorB, t);

        if (turnMarkImage != null)
            turnMarkImage.color = blinkColor;
    }


    public ReserveTurnSlotUI GetOrCreateReserveTurnSlot(BattleTimelineController controller, int targetSlotIndex)
    {
        ReserveTurnSlotUI slot = GetComponent<ReserveTurnSlotUI>();

        if (slot == null)
            slot = gameObject.AddComponent<ReserveTurnSlotUI>();

        slot.SetAutoBindButtonsInChildren(false);
        slot.Init(controller, targetSlotIndex);
        return slot;
    }

    private void SetupTurnMarkClickTarget()
    {
        if (turnMarkTransform == null)
            return;

        Image image = turnMarkTransform.GetComponent<Image>();
        if (image == null)
        {
            image = turnMarkTransform.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        image.raycastTarget = true;

        Button button = turnMarkTransform.GetComponent<Button>();
        if (button == null)
            button = turnMarkTransform.gameObject.AddComponent<Button>();

        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickTimelineSlot);
    }

    private void EnsureButton()
    {
        Button button = GetComponent<Button>();

        if (button == null)
            button = gameObject.AddComponent<Button>();

        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickTimelineSlot);

        Image image = GetComponent<Image>();

        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        image.raycastTarget = true;
    }

    private void OnClickTimelineSlot()
    {
        if (owner != null)
            owner.OnTimelineSlotClicked(slotIndex);
    }

    private void SetupOrderClickTargets()
    {
        for (int i = 1; i <= 5; i++)
        {
            Transform order = FindChildRecursive(transform, "Order" + i.ToString("00"));

            if (order == null)
                continue;

            int capturedIndex = i - 1;

            Image image = order.GetComponent<Image>();

            if (image == null)
            {
                image = order.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }

            image.raycastTarget = true;

            TimelineOrderClickTarget clickTarget =
                order.GetComponent<TimelineOrderClickTarget>();

            if (clickTarget == null)
                clickTarget = order.gameObject.AddComponent<TimelineOrderClickTarget>();

            clickTarget.Init(this, capturedIndex);

            Button button = order.GetComponent<Button>();

            if (button == null)
                button = order.gameObject.AddComponent<Button>();

            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                OnOrderClicked(capturedIndex);
            });
        }
    }

    private Image[] FindOrderUseSkillImages()
    {
        List<Image> images = new();

        for (int i = 1; i <= 5; i++)
        {
            Transform root = FindOrderUseSkillRoot(i);

            if (root == null)
                continue;

            Image image = FindUseSkillImage(root);

            if (image != null)
                images.Add(image);
        }

        return images.ToArray();
    }

    private TMP_Text[] FindOrderUseSkillTexts()
    {
        List<TMP_Text> texts = new();

        for (int i = 1; i <= 5; i++)
        {
            Transform root = FindOrderUseSkillRoot(i);

            if (root == null)
                continue;

            TMP_Text text = FindUseSkillText(root);

            if (text != null)
                texts.Add(text);
        }

        return texts.ToArray();
    }

    private Transform FindOrderUseSkillRoot(int orderNumber)
    {
        Transform order = FindChildRecursive(transform, "Order" + orderNumber.ToString("00"));

        if (order == null)
            return null;

        return FindChildRecursive(order, "Use_skill");
    }

    private Image FindUseSkillImage(Transform root)
    {
        if (root == null)
            return null;

        Transform imageTransform = FindChildRecursive(root, "Skill_Image");

        if (imageTransform == null)
            imageTransform = FindChildRecursive(root, "image");

        if (imageTransform != null)
        {
            Image childImage = imageTransform.GetComponent<Image>();

            if (childImage != null)
                return childImage;
        }

        return root.GetComponent<Image>();
    }

    private TMP_Text FindUseSkillText(Transform root)
    {
        if (root == null)
            return null;

        Transform textTransform = FindChildRecursive(root, "Value");

        if (textTransform == null)
            textTransform = FindChildRecursive(root, "Text (TMP)");

        if (textTransform == null)
            textTransform = FindChildRecursive(root, "Text");

        if (textTransform == null)
            return null;

        return textTransform.GetComponent<TMP_Text>();
    }

    private Image FindRootImage(string rootName)
    {
        Transform root = FindChildRecursive(transform, rootName);

        if (root == null)
            return null;

        return root.GetComponent<Image>();
    }

    private Image FindImage(string rootName, string imageName)
    {
        Transform root = FindChildRecursive(transform, rootName);

        if (root == null)
            return null;

        Transform imageTransform = FindChildRecursive(root, imageName);

        if (imageTransform == null)
            return null;

        return imageTransform.GetComponent<Image>();
    }

    private void ApplyTurnText()
    {
        if (turnText == null)
            return;

        turnText.text = GetRomanTurnText(slotIndex);
        turnText.gameObject.SetActive(true);
    }

    private static string GetRomanTurnText(int index)
    {
        switch (index)
        {
            case 0: return "I";
            case 1: return "II";
            case 2: return "III";
            case 3: return "IV";
            case 4: return "V";
            default: return string.Empty;
        }
    }

    private void ApplyOwnerOrderIcon(Image rootImage, Image contentImage, BattleTimelinePreviewEntry entry)
    {
        if (entry == null)
        {
            SetOwnerIconImage(rootImage, contentImage, null, false, Color.white);
            return;
        }

        Color color = entry.IsMonster ? enemyReservedColor : playerReservedColor;
        SetOwnerIconImage(rootImage, contentImage, entry.OwnerIcon, true, color);
        ApplyOwnerReservationColor(rootImage, contentImage, entry, IsEntryOwnerDead(entry));
    }

    private void UpdateDeadReservationVisuals()
    {
        int count = Mathf.Min(currentEntries.Count, useSkillIconImages != null ? useSkillIconImages.Length : 0);

        for (int i = 0; i < count; i++)
        {
            BattleTimelinePreviewEntry entry = currentEntries[i];
            bool ownerDead = IsEntryOwnerDead(entry);

            if (i < currentEntryOwnerDeadStates.Count && currentEntryOwnerDeadStates[i] == ownerDead)
                continue;

            while (currentEntryOwnerDeadStates.Count <= i)
                currentEntryOwnerDeadStates.Add(false);

            currentEntryOwnerDeadStates[i] = ownerDead;
            ApplySkillReservationColor(useSkillIconImages[i], entry, ownerDead);
        }

        bool firstDead = IsEntryOwnerDead(firstOwnerEntry);
        if (firstDead != firstOwnerDeadState)
        {
            firstOwnerDeadState = firstDead;
            ApplyOwnerReservationColor(firstIconRootImage, firstIconImage, firstOwnerEntry, firstDead);
        }

        bool laterDead = IsEntryOwnerDead(laterOwnerEntry);
        if (laterDead != laterOwnerDeadState)
        {
            laterOwnerDeadState = laterDead;
            ApplyOwnerReservationColor(laterIconRootImage, laterIconImage, laterOwnerEntry, laterDead);
        }
    }

    private bool IsEntryOwnerDead(BattleTimelinePreviewEntry entry)
    {
        if (entry == null)
            return false;

        if (entry.IsPlayer)
            return entry.CharacterRuntime != null && entry.CharacterRuntime.IsDead;

        if (entry.IsMonster)
            return entry.MonsterRuntime != null && entry.MonsterRuntime.IsDead;

        return false;
    }

    private void ApplySkillReservationColor(
        Image skillImage,
        BattleTimelinePreviewEntry entry,
        bool ownerDead)
    {
        if (skillImage == null || entry == null)
            return;

        Color rootColor = ownerDead
            ? deadReservationColor
            : (entry.IsMonster ? enemyReservedColor : playerReservedColor);
        Color contentColor = ownerDead ? deadReservationColor : Color.white;

        GameObject root = GetSkillHoverObject(skillImage);
        if (root != null)
            SetRootImageColor(root, rootColor);

        skillImage.color = contentColor;
    }

    private void ApplyOwnerReservationColor(
        Image rootImage,
        Image contentImage,
        BattleTimelinePreviewEntry entry,
        bool ownerDead)
    {
        if (entry == null)
            return;

        Color rootColor = ownerDead
            ? deadReservationColor
            : (entry.IsMonster ? enemyReservedColor : playerReservedColor);
        Color contentColor = ownerDead ? deadReservationColor : Color.white;

        if (rootImage != null)
            rootImage.color = rootColor;

        if (contentImage != null)
            contentImage.color = contentColor;
    }

    private void SetImage(Image image, Sprite sprite, bool visible)
    {
        SetImage(image, sprite, visible, Color.white);
    }

    private void SetImage(Image image, Sprite sprite, bool visible, Color color)
    {
        if (image == null)
            return;

        bool show = visible && sprite != null;

        image.sprite = sprite;
        image.color = show ? color : Color.white;
        image.enabled = show;
        image.gameObject.SetActive(show);
        image.raycastTarget = false;
    }

    private void SetOwnerIconImage(
        Image rootImage,
        Image contentImage,
        Sprite sprite,
        bool visible,
        Color reservedColor)
    {
        bool show = visible && sprite != null;

        if (rootImage != null)
        {
            rootImage.color = reservedColor;
            rootImage.enabled = true;
            rootImage.gameObject.SetActive(show);
        }

        if (contentImage == null)
            return;

        contentImage.sprite = sprite;
        contentImage.enabled = show;
        contentImage.gameObject.SetActive(show);
        contentImage.raycastTarget = false;
    }

    private void SetSkillImage(Image image, Sprite sprite, bool visible, Color borderColor)
    {
        if (image == null)
            return;

        bool show = visible && sprite != null;
        GameObject hoverObject = GetSkillHoverObject(image);

        if (hoverObject != null)
        {
            SetRootImageColor(hoverObject, show ? borderColor : emptyUseSkillColor);
            hoverObject.SetActive(true);

            Image rootImage = hoverObject.GetComponent<Image>();

            if (rootImage != null)
                rootImage.raycastTarget = show;
        }

        image.gameObject.SetActive(show);
        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = show;
        image.raycastTarget = show;
    }

    private void ShowEmptyUseSkillSlot(GameObject useSkillRoot)
    {
        if (useSkillRoot == null)
            return;

        useSkillRoot.SetActive(true);

        Image rootImage = useSkillRoot.GetComponent<Image>();

        if (rootImage != null)
        {
            rootImage.color = emptyUseSkillColor;
            rootImage.enabled = true;
            rootImage.raycastTarget = false;
        }
    }

    private void HideEmptyUseSkillSlot(GameObject useSkillRoot)
    {
        if (useSkillRoot == null)
            return;

        useSkillRoot.SetActive(false);
    }

    public void SetEmptyUseSkillSlotsVisible(bool visible)
    {
        emptyUseSkillSlotsVisible = visible;

        if (useSkillIconImages == null)
            return;

        int usedCount = Mathf.Clamp(currentEntries.Count, 0, useSkillIconImages.Length);

        for (int i = usedCount; i < useSkillIconImages.Length; i++)
        {
            Image image = useSkillIconImages[i];

            if (image == null)
                continue;

            GameObject hoverObject = GetSkillHoverObject(image);

            if (hoverObject == null)
                continue;

            if (visible)
                ShowEmptyUseSkillSlot(hoverObject);
            else
                HideEmptyUseSkillSlot(hoverObject);
        }
    }

    private void SetSkillValueText(TMP_Text[] texts, int index, string valueText)
    {
        if (texts == null || index < 0 || index >= texts.Length)
            return;

        TMP_Text text = texts[index];

        if (text == null)
            return;

        bool show = !string.IsNullOrWhiteSpace(valueText);
        text.text = show ? valueText : "";
        text.gameObject.SetActive(show);
    }

    private void ClearSkillValueTexts(TMP_Text[] texts)
    {
        if (texts == null)
            return;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            texts[i].text = "";
            texts[i].gameObject.SetActive(false);
        }
    }

    private void SetRootImageColor(GameObject root, Color color)
    {
        if (root == null)
            return;

        Image image = root.GetComponent<Image>();

        if (image != null)
            image.color = color;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private string GetPath(Transform target)
    {
        if (target == null)
            return "";

        string path = target.name;

        Transform current = target.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
