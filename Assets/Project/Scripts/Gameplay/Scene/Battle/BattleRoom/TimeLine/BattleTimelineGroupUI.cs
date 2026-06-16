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
    [SerializeField] private Image[] playerSkillIconImages;
    [SerializeField] private Image[] enemySkillIconImages;
    [SerializeField] private TMP_Text[] playerSkillValueTexts;
    [SerializeField] private TMP_Text[] enemySkillValueTexts;
    [SerializeField] private GameObject[] playerMarkObjects;
    [SerializeField] private GameObject[] enemyMarkObjects;

    [Header("Reserved Colors")]
    [SerializeField] private Color playerReservedColor = new Color32(0x0A, 0x46, 0x9E, 0xFF);
    [SerializeField] private Color enemyReservedColor = new Color32(0xDF, 0x4D, 0x56, 0xFF);

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

            if (entry.IsMonster)
            {
                if (firstEnemyIcon == null)
                    firstEnemyIcon = entry.OwnerIcon;

                if (enemySkillIconImages != null &&
                    visibleIndex < enemySkillIconImages.Length)
                {
                    SetSkillImage(enemySkillIconImages[visibleIndex], entry.SkillIcon, true, enemyReservedColor);
                    SetSkillValueText(enemySkillValueTexts, visibleIndex, entry.SkillValueText);

                    SetupEnemySkillHoverTarget(enemySkillIconImages[visibleIndex], entry);
                }

                if (enemyMarkObjects != null &&
                    visibleIndex < enemyMarkObjects.Length &&
                    enemyMarkObjects[visibleIndex] != null)
                {
                    SetRootImageColor(enemyMarkObjects[visibleIndex], enemyReservedColor);
                    enemyMarkObjects[visibleIndex].SetActive(true);
                }
            }
            else
            {
                if (firstPlayerIcon == null)
                    firstPlayerIcon = entry.OwnerIcon;

                if (playerSkillIconImages != null &&
                    visibleIndex < playerSkillIconImages.Length)
                {
                    SetSkillImage(playerSkillIconImages[visibleIndex], entry.SkillIcon, true, playerReservedColor);
                    SetSkillValueText(playerSkillValueTexts, visibleIndex, entry.SkillValueText);

                    SetupPlayerSkillHoverTarget(playerSkillIconImages[visibleIndex], entry);
                }

                if (playerMarkObjects != null &&
                    visibleIndex < playerMarkObjects.Length &&
                    playerMarkObjects[visibleIndex] != null)
                {
                    SetRootImageColor(playerMarkObjects[visibleIndex], playerReservedColor);
                    playerMarkObjects[visibleIndex].SetActive(true);
                }
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

        if (playerSkillIconImages != null)
        {
            for (int i = 0; i < playerSkillIconImages.Length; i++)
            {
                ClearSkillImage(playerSkillIconImages[i]);
            }
        }

        if (enemySkillIconImages != null)
        {
            for (int i = 0; i < enemySkillIconImages.Length; i++)
            {
                ClearSkillImage(enemySkillIconImages[i]);
            }
        }

        ClearSkillValueTexts(playerSkillValueTexts);
        ClearSkillValueTexts(enemySkillValueTexts);

        SetObjectsActive(playerMarkObjects, false);
        SetObjectsActive(enemyMarkObjects, false);
    }

    private void ClearSkillImage(Image image)
    {
        if (image == null)
            return;

        image.sprite = null;
        image.enabled = false;
        image.gameObject.SetActive(false);

        Transform parent = image.transform.parent;

        if (parent != null)
        {
            SetRootImageColor(parent.gameObject, Color.white);
            parent.gameObject.SetActive(false);
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

        if (owner != null)
            owner.OnOrderClicked(slotIndex, orderIndex);
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

        if (playerSkillIconImages == null || playerSkillIconImages.Length == 0)
            playerSkillIconImages = FindOrderImages("Player_Skill", "Skill_Image");

        if (enemySkillIconImages == null || enemySkillIconImages.Length == 0)
            enemySkillIconImages = FindOrderImages("Enemy_Skill", "Skill_Image");

        if (playerSkillValueTexts == null || playerSkillValueTexts.Length == 0)
            playerSkillValueTexts = FindOrderTexts("Player_Skill", "Text (TMP)");

        if (enemySkillValueTexts == null || enemySkillValueTexts.Length == 0)
            enemySkillValueTexts = FindOrderTexts("Enemy_Skill", "Text (TMP)");

        if (playerMarkObjects == null || playerMarkObjects.Length == 0)
            playerMarkObjects = FindOrderObjects("Player_Mark");

        if (enemyMarkObjects == null || enemyMarkObjects.Length == 0)
            enemyMarkObjects = FindOrderObjects("Enemy_Mark");

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

    private Image[] FindOrderImages(string rootName, string imageName)
    {
        List<Image> images = new();

        for (int i = 1; i <= 5; i++)
        {
            Transform order = FindChildRecursive(transform, "Order" + i.ToString("00"));

            if (order == null)
                continue;

            Transform root = FindChildRecursive(order, rootName);

            if (root == null)
                continue;

            Transform imageTransform = FindChildRecursive(root, imageName);

            if (imageTransform == null)
                continue;

            Image image = imageTransform.GetComponent<Image>();

            if (image != null)
                images.Add(image);
        }

        return images.ToArray();
    }

    private TMP_Text[] FindOrderTexts(string rootName, string textName)
    {
        List<TMP_Text> texts = new();

        for (int i = 1; i <= 5; i++)
        {
            Transform order = FindChildRecursive(transform, "Order" + i.ToString("00"));

            if (order == null)
                continue;

            Transform root = FindChildRecursive(order, rootName);

            if (root == null)
                continue;

            Transform textTransform = FindChildRecursive(root, textName);

            if (textTransform == null)
                textTransform = FindChildRecursive(root, "Text");

            if (textTransform == null)
                continue;

            TMP_Text text = textTransform.GetComponent<TMP_Text>();

            if (text != null)
                texts.Add(text);
        }

        return texts.ToArray();
    }

    private GameObject[] FindOrderObjects(string objectName)
    {
        List<GameObject> objects = new();

        for (int i = 1; i <= 5; i++)
        {
            Transform order = FindChildRecursive(transform, "Order" + i.ToString("00"));

            if (order == null)
                continue;

            Transform found = FindChildRecursive(order, objectName);

            if (found != null)
                objects.Add(found.gameObject);
        }

        return objects.ToArray();
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
        Transform parent = image.transform.parent;

        if (parent != null)
        {
            SetRootImageColor(parent.gameObject, show ? borderColor : Color.white);
            parent.gameObject.SetActive(show);
        }

        image.gameObject.SetActive(show);
        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = show;
        image.raycastTarget = true;
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

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                if (!active)
                    SetRootImageColor(objects[i], Color.white);

                objects[i].SetActive(active);
            }
        }
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