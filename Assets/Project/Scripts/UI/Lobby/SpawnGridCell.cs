using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class SpawnGridCell : MonoBehaviour
{
    [SerializeField] private Button button;

    [Header("Slot Order Icon Object")]
    [FormerlySerializedAs("slotOrderIconImage")]
    [FormerlySerializedAs("characterIconImage")]
    [SerializeField] private Transform slotOrderIconParent;

    [SerializeField] private GameObject selectedObject;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color selectedColor = Color.blue;

    private Image cellImage;
    private SpawnGridPanel owner;
    private int gridIndex;
    private GameObject currentSlotOrderIconSource;
    private GameObject currentSlotOrderIconInstance;

    public void Init(SpawnGridPanel panel, int index)
    {
        cellImage = GetComponent<Image>();

        owner = panel;
        gridIndex = index;

        if (slotOrderIconParent == null)
            slotOrderIconParent = transform;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Execute);
        }

        Refresh();
    }

    public void Execute()
    {
        if (owner == null)
            return;

        owner.OnClickCell(gridIndex);
    }

    public void Refresh()
    {
        int partySlotIndex = GetPartySlotIndexOnThisGrid();
        bool hasPartySlot = partySlotIndex >= 0;
        bool isSelected = owner != null && owner.IsSelectedGrid(gridIndex);

        if (cellImage != null)
        {
            if (isSelected)
                cellImage.color = selectedColor;
            else if (hasPartySlot)
                cellImage.color = occupiedColor;
            else
                cellImage.color = normalColor;
        }

        RefreshSlotOrderIconObject(partySlotIndex, hasPartySlot);

        if (selectedObject != null)
            selectedObject.SetActive(owner != null && owner.IsSelectedGrid(gridIndex));
    }

    private void RefreshSlotOrderIconObject(int partySlotIndex, bool hasPartySlot)
    {
        GameObject iconObject = null;

        if (hasPartySlot && owner != null)
            owner.TryGetPartySlotOrderIconObject(partySlotIndex, out iconObject);

        if (!hasPartySlot || iconObject == null)
        {
            ClearSlotOrderIconObject();
            return;
        }

        if (currentSlotOrderIconInstance != null && currentSlotOrderIconSource == iconObject)
        {
            currentSlotOrderIconInstance.SetActive(true);
            return;
        }

        ClearSlotOrderIconObject();

        Transform parent = slotOrderIconParent != null ? slotOrderIconParent : transform;
        currentSlotOrderIconSource = iconObject;
        currentSlotOrderIconInstance = Instantiate(iconObject, parent);
        currentSlotOrderIconInstance.name = iconObject.name;
        currentSlotOrderIconInstance.SetActive(true);

        RectTransform rectTransform = currentSlotOrderIconInstance.transform as RectTransform;

        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            currentSlotOrderIconInstance.transform.localPosition = Vector3.zero;
            currentSlotOrderIconInstance.transform.localRotation = Quaternion.identity;
            currentSlotOrderIconInstance.transform.localScale = Vector3.one;
        }
    }

    private void ClearSlotOrderIconObject()
    {
        currentSlotOrderIconSource = null;

        if (currentSlotOrderIconInstance == null)
            return;

        Destroy(currentSlotOrderIconInstance);
        currentSlotOrderIconInstance = null;
    }

    private int GetPartySlotIndexOnThisGrid()
    {
        if (DataManager.Instance == null)
            return -1;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (partyStore.GetSpawnGridIndex(i) == gridIndex)
                return i;
        }

        return -1;
    }
}
