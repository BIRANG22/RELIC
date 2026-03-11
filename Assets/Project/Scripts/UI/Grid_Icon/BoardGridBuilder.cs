using UnityEngine;
using UnityEngine.UI;

public class BoardGridBuilder : MonoBehaviour
{
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 2;
    [SerializeField] private Vector2 slotSize = new Vector2(100f, 100f);

    [Header("Sprites")]
    [SerializeField] private Sprite defaultFrameSprite;
    [SerializeField] private Sprite defaultIconSprite;

    [ContextMenu("Build 6x2 Slots")]
    public void Build()
    {
        if (slotsRoot == null)
        {
            Debug.LogWarning("SlotsRoot is null.");
            return;
        }

        ClearChildren();

        PerspectiveBoardController controller = GetComponent<PerspectiveBoardController>();
        if (controller == null)
            controller = GetComponentInParent<PerspectiveBoardController>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject slotGO = new GameObject(
                    $"Slot_{col}_{row}",
                    typeof(RectTransform),
                    typeof(BoardSlotView)
                );
                slotGO.transform.SetParent(slotsRoot, false);

                RectTransform slotRect = slotGO.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = slotSize;
                slotRect.anchoredPosition = Vector2.zero;
                slotRect.localScale = Vector3.one;
                slotRect.localRotation = Quaternion.identity;

                GameObject frameGO = new GameObject(
                    "Frame",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(UIQuadWarp),
                    typeof(BoardSlotHover)
                );
                frameGO.transform.SetParent(slotGO.transform, false);

                RectTransform frameRect = frameGO.GetComponent<RectTransform>();
                frameRect.anchorMin = new Vector2(0.5f, 0.5f);
                frameRect.anchorMax = new Vector2(0.5f, 0.5f);
                frameRect.pivot = new Vector2(0.5f, 0.5f);
                frameRect.sizeDelta = slotSize;
                frameRect.anchoredPosition = Vector2.zero;
                frameRect.localScale = Vector3.one;
                frameRect.localRotation = Quaternion.identity;

                Image frameImage = frameGO.GetComponent<Image>();
                frameImage.sprite = defaultFrameSprite;
                frameImage.type = Image.Type.Simple;
                frameImage.preserveAspect = false;
                frameImage.raycastTarget = true;

                UIQuadWarp frameWarp = frameGO.GetComponent<UIQuadWarp>();

                GameObject iconGO = new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(UIQuadWarp)
                );
                iconGO.transform.SetParent(slotGO.transform, false);

                RectTransform iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = slotSize;
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.localScale = Vector3.one;
                iconRect.localRotation = Quaternion.identity;

                Image iconImage = iconGO.GetComponent<Image>();
                iconImage.sprite = defaultIconSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                UIQuadWarp iconWarp = iconGO.GetComponent<UIQuadWarp>();

                BoardSlotView slotView = slotGO.GetComponent<BoardSlotView>();
                slotView.rectTransform = slotRect;
                slotView.frameRoot = frameRect;
                slotView.frameWarp = frameWarp;
                slotView.frameImage = frameImage;
                slotView.iconRoot = iconRect;
                slotView.iconWarp = iconWarp;
                slotView.iconImage = iconImage;

                BoardSlotHover hover = frameGO.GetComponent<BoardSlotHover>();
                if (hover != null)
                {
                    int slotIndex = row * columns + col;
                    hover.Setup(controller, slotIndex);
                }
            }
        }

        if (controller != null)
        {
            controller.AutoCollectSlots();
            controller.ApplyLayout();
        }
    }

    private void ClearChildren()
    {
        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = slotsRoot.GetChild(i);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }
}