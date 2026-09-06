using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleButtonImage : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;

    [Header("Button Images")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    private bool isSelected;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        Refresh();
    }

    private void Update()
    {
        if (!isSelected)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    transform as RectTransform,
                    Input.mousePosition,
                    null))
            {
                SetSelected(false);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        SetSelected(!isSelected);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        Refresh();
    }

    private void Refresh()
    {
        if (targetImage == null)
            return;

        targetImage.sprite = isSelected ? selectedSprite : normalSprite;
    }
}
