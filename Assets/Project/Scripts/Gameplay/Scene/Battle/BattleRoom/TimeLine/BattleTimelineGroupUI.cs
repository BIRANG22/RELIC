using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTimelineGroupUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Turn Mark")]
    [SerializeField] private Image playerIconImage;
    [SerializeField] private Image enemyIconImage;

    [Header("Order Slots")]
    [SerializeField] private Image[] useSkillIconImages;
    [SerializeField] private TMP_Text[] useSkillValueTexts;

    [Header("Reserved Colors")]
    [SerializeField] private Color playerReservedColor = new Color32(0x0A, 0x46, 0x9E, 0xFF);
    [SerializeField] private Color enemyReservedColor = new Color32(0xDF, 0x4D, 0x56, 0xFF);

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

    private BattleTimelineBarUI owner;
    private int slotIndex;
    private bool isActive;
    private bool emptyUseSkillSlotsVisible = true;

    private Vector3 turnMarkNormalScale = Vector3.one;
    private bool hasCachedTurnMarkVisual;
    private Color turnMarkNormalImageColor = Color.white;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        AutoFindReferences();
        CacheTurnMarkNormalVisual();
        ApplyTurnMarkSelectedVisual(false);
    }

    private void Update()
    {
        UpdateTurnMarkSelectedAnimation();
    }

    public void Init(BattleTimelineBarUI owner, int slotIndex)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;

        AutoFindReferences();
        CacheTurnMarkNormalVisual();
        ApplyTurnMarkSelectedVisual(isActive);
    }

    public void SetActiveTimelineSlot(bool active)
    {
        isActive = active;
        ApplyTurnMarkSelectedVisual(active);
    }

    public void SetTimelineEntries(IReadOnlyList<BattleTimelinePreviewEntry> entries, int targetSlotIndex)
    {
        Clear();

        if (entries == null || entries.Count <= 0)
            return;

        Sprite firstPlayerIcon = null;
        Sprite firstEnemyIcon = null;

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

            bool isMonster = entry.IsMonster;
            Color reservedColor = isMonster ? enemyReservedColor : playerReservedColor;

            if (isMonster)
            {
                if (firstEnemyIcon == null)
                    firstEnemyIcon = entry.OwnerIcon;
            }
            else
            {
                if (firstPlayerIcon == null)
                    firstPlayerIcon = entry.OwnerIcon;
            }

            if (useSkillIconImages != null && visibleIndex < useSkillIconImages.Length)
            {
                Image useSkillImage = useSkillIconImages[visibleIndex];

                SetSkillImage(useSkillImage, entry.SkillIcon, true, reservedColor);
                SetSkillValueText(useSkillValueTexts, visibleIndex, entry.SkillValueText);

                if (isMonster)
                    SetupEnemySkillHoverTarget(useSkillImage, entry);
                else
                    SetupPlayerSkillHoverTarget(useSkillImage, entry);
            }

            visibleIndex++;
        }

        SetOwnerIconImage(playerIconImage, firstPlayerIcon, firstPlayerIcon != null, playerReservedColor);
        SetOwnerIconImage(enemyIconImage, firstEnemyIcon, firstEnemyIcon != null, enemyReservedColor);
    }

    public void Clear()
    {
        currentEntries.Clear();

        SetOwnerIconImage(playerIconImage, null, false, Color.white);
        SetOwnerIconImage(enemyIconImage, null, false, Color.white);

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
        if (orderIndex < 0 || orderIndex >= currentEntries.Count)
            return;

        BattleTimelinePreviewEntry entry = currentEntries[orderIndex];

        if (entry == null)
            return;

        TimelineReservationHoverPreview.HideCurrent();

        if (owner != null)
            owner.OnEntryClicked(entry);
    }

    private void AutoFindReferences()
    {
        if (turnMarkTransform == null)
            turnMarkTransform = FindChildRecursive(transform, "TurnMark");

        if (turnMarkImage == null && turnMarkTransform != null)
            turnMarkImage = turnMarkTransform.GetComponent<Image>();

        if (playerIconImage == null)
            playerIconImage = FindImage("Player_Icon", "image");

        if (enemyIconImage == null)
            enemyIconImage = FindImage("Enemy_Icon", "image");

        if (useSkillIconImages == null || useSkillIconImages.Length == 0)
            useSkillIconImages = FindOrderUseSkillImages();

        if (useSkillValueTexts == null || useSkillValueTexts.Length == 0)
            useSkillValueTexts = FindOrderUseSkillTexts();

        EnsureButton();
        SetupOrderClickTargets();
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

        Transform textTransform = FindChildRecursive(root, "Text (TMP)");

        if (textTransform == null)
            textTransform = FindChildRecursive(root, "Text");

        if (textTransform == null)
            return null;

        return textTransform.GetComponent<TMP_Text>();
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

    private void SetOwnerIconImage(Image image, Sprite sprite, bool visible, Color borderColor)
    {
        if (image == null)
            return;

        bool show = visible && sprite != null;
        Transform parent = image.transform.parent;

        if (parent != null)
        {
            SetRootImageColor(parent.gameObject, show ? borderColor : Color.white);
            parent.gameObject.SetActive(show);
        }

        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = show;
        image.gameObject.SetActive(show);
        image.raycastTarget = false;
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
