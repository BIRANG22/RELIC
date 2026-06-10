using System.Collections.Generic;
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
    [SerializeField] private GameObject[] playerMarkObjects;
    [SerializeField] private GameObject[] enemyMarkObjects;

    [Header("Active Visual")]
    [SerializeField] private Transform activeScaleTarget;
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 activeScale = new Vector3(1.12f, 1.12f, 1f);

    private readonly List<BattleTimelinePreviewEntry> currentEntries = new();

    private BattleTimelineBarUI owner;
    private int slotIndex;
    private bool isActive;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        AutoFindReferences();
    }

    private void Update()
    {
        if (activeScaleTarget == null)
            return;

        Vector3 targetScale = isActive ? activeScale : normalScale;

        activeScaleTarget.localScale = Vector3.Lerp(
            activeScaleTarget.localScale,
            targetScale,
            Time.unscaledDeltaTime * 12f
        );
    }

    public void Init(BattleTimelineBarUI owner, int slotIndex)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;

        AutoFindReferences();
    }

    public void SetActiveTimelineSlot(bool active)
    {
        isActive = active;
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
                if (enemySkillIconImages != null &&
                    visibleIndex < enemySkillIconImages.Length)
                {
                    SetSkillImage(enemySkillIconImages[visibleIndex], entry.SkillIcon, true);
                }

                if (enemyMarkObjects != null &&
                    visibleIndex < enemyMarkObjects.Length &&
                    enemyMarkObjects[visibleIndex] != null)
                {
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
                    SetSkillImage(playerSkillIconImages[visibleIndex], entry.SkillIcon, true);

                    TimelineSkillIconHoverUI hoverUI =
                        playerSkillIconImages[visibleIndex].GetComponentInParent<TimelineSkillIconHoverUI>();

                    if (hoverUI != null)
                        hoverUI.Setup(entry.PlayerCommand);
                }

                if (playerMarkObjects != null &&
                    visibleIndex < playerMarkObjects.Length &&
                    playerMarkObjects[visibleIndex] != null)
                {
                    playerMarkObjects[visibleIndex].SetActive(true);
                }
            }

            visibleIndex++;
        }

        SetImage(playerIconImage, firstPlayerIcon, firstPlayerIcon != null);
        SetImage(enemyIconImage, firstEnemyIcon, firstEnemyIcon != null);
    }

    public void Clear()
    {
        currentEntries.Clear();

        SetImage(playerIconImage, null, false);
        SetImage(enemyIconImage, null, false);

        if (playerSkillIconImages != null)
        {
            for (int i = 0; i < playerSkillIconImages.Length; i++)
            {
                SetImage(playerSkillIconImages[i], null, false);

                if (playerSkillIconImages[i] != null &&
                    playerSkillIconImages[i].transform.parent != null)
                {
                    playerSkillIconImages[i].transform.parent.gameObject.SetActive(false);
                }
            }
        }

        if (enemySkillIconImages != null)
        {
            for (int i = 0; i < enemySkillIconImages.Length; i++)
            {
                SetImage(enemySkillIconImages[i], null, false);

                if (enemySkillIconImages[i] != null &&
                    enemySkillIconImages[i].transform.parent != null)
                {
                    enemySkillIconImages[i].transform.parent.gameObject.SetActive(false);
                }
            }
        }

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
            parent.gameObject.SetActive(false);
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
        if (activeScaleTarget == null)
            activeScaleTarget = transform;

        if (playerIconImage == null)
            playerIconImage = FindImage("Player_Icon", "image");

        if (enemyIconImage == null)
            enemyIconImage = FindImage("Enemy_Icon", "image");

        if (playerSkillIconImages == null || playerSkillIconImages.Length == 0)
            playerSkillIconImages = FindOrderImages("Player_Skill", "Skill_Image");

        if (enemySkillIconImages == null || enemySkillIconImages.Length == 0)
            enemySkillIconImages = FindOrderImages("Enemy_Skill", "Skill_Image");

        if (playerMarkObjects == null || playerMarkObjects.Length == 0)
            playerMarkObjects = FindOrderObjects("Player_Mark");

        if (enemyMarkObjects == null || enemyMarkObjects.Length == 0)
            enemyMarkObjects = FindOrderObjects("Enemy_Mark");

        EnsureButton();
        SetupOrderClickTargets();
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
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = visible && sprite != null;
        image.gameObject.SetActive(visible && sprite != null);
        image.raycastTarget = false;
    }

    private void SetSkillImage(Image image, Sprite sprite, bool visible)
    {
        if (image == null)
            return;

        //Debug.Log($"[SetSkillImage] Path:{GetPath(image.transform)} / Sprite:{sprite}");

        bool show = visible && sprite != null;

        Transform parent = image.transform.parent;

        if (parent != null)
            parent.gameObject.SetActive(show);

        image.gameObject.SetActive(show);
        image.sprite = sprite;
        image.enabled = show;
        image.raycastTarget = false;
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
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