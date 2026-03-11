using UnityEngine;
using UnityEngine.UI;

public class BoardSlotView : MonoBehaviour
{
    public RectTransform rectTransform;

    public RectTransform frameRoot;
    public UIQuadWarp frameWarp;
    public Image frameImage;

    public RectTransform iconRoot;
    public UIQuadWarp iconWarp;
    public Image iconImage;

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
    }
}