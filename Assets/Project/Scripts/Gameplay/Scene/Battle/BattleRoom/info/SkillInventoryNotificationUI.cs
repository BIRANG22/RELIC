using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillInventoryNotificationUI : MonoBehaviour
{
    private const string DefaultInventoryButtonName = "InventoryButton";

    [SerializeField] private GameObject notificationObject;

    private Button boundButton;

    private void Awake()
    {
        ResolveNotificationObjectIfNeeded();
        BindButtonIfNeeded();
    }

    private void OnEnable()
    {
        ResolveNotificationObjectIfNeeded();
        BindButtonIfNeeded();
    }

    private void OnDestroy()
    {
        if (boundButton != null)
            boundButton.onClick.RemoveListener(ClearNotice);
    }

    public void ShowNotice()
    {
        SetNoticeVisible(true);
    }

    public void ClearNotice()
    {
        SetNoticeVisible(false);
    }

    public static void ShowNewSkillNotice()
    {
        SkillInventoryNotificationUI notifier = FindOrCreateInventoryButtonNotifier();

        if (notifier != null)
            notifier.ShowNotice();
    }

    public static void ClearNewSkillNotice()
    {
        SkillInventoryNotificationUI notifier = FindOrCreateInventoryButtonNotifier();

        if (notifier != null)
            notifier.ClearNotice();
    }

    private static SkillInventoryNotificationUI FindOrCreateInventoryButtonNotifier()
    {
        SkillInventoryNotificationUI notifier = FindFirstObjectByType<SkillInventoryNotificationUI>(
            FindObjectsInactive.Include);

        if (notifier != null)
            return notifier;

        GameObject inventoryButton = GameObject.Find(DefaultInventoryButtonName);

        if (inventoryButton == null)
            return null;

        notifier = inventoryButton.GetComponent<SkillInventoryNotificationUI>();

        if (notifier == null)
            notifier = inventoryButton.AddComponent<SkillInventoryNotificationUI>();

        return notifier;
    }

    private void BindButtonIfNeeded()
    {
        Button button = GetComponent<Button>();

        if (button == null)
            return;

        if (boundButton != null && boundButton != button)
            boundButton.onClick.RemoveListener(ClearNotice);

        boundButton = button;
        boundButton.onClick.RemoveListener(ClearNotice);
        boundButton.onClick.AddListener(ClearNotice);
    }

    private void ResolveNotificationObjectIfNeeded()
    {
        if (notificationObject != null)
            return;

        TMP_Text tmpText = GetComponentInChildren<TMP_Text>(true);

        if (tmpText != null && tmpText.gameObject != gameObject)
        {
            notificationObject = tmpText.gameObject;
            return;
        }

        Text legacyText = GetComponentInChildren<Text>(true);

        if (legacyText != null && legacyText.gameObject != gameObject)
            notificationObject = legacyText.gameObject;
    }

    private void SetNoticeVisible(bool visible)
    {
        ResolveNotificationObjectIfNeeded();

        if (notificationObject != null)
            notificationObject.SetActive(visible);
    }
}
