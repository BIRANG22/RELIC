using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Relic.Gameplay.Data
{
    [DisallowMultipleComponent]
    public sealed class RelicEffectTooltipFallbackUI : MonoBehaviour
    {
        private const float CardWidth = 360f;
        private const float CardHeight = 96f;
        private const float DefaultSpacing = 8f;
        private static RelicEffectTooltipFallbackUI instance;

        private readonly List<GameObject> cards = new();
        private RectTransform rootRect;
        private Object owner;
        private float currentSpacing = DefaultSpacing;

        public static RelicEffectTooltipFallbackUI GetOrCreate()
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<RelicEffectTooltipFallbackUI>(FindObjectsInactive.Include);
            if (instance != null)
                return instance;

            GameObject root = new(
                "RelicEffectTooltipFallbackUI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(RelicEffectTooltipFallbackUI));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
            raycaster.enabled = false;
            instance = root.GetComponent<RelicEffectTooltipFallbackUI>();
            return instance;
        }

        private void Awake()
        {
            instance = this;
            rootRect = transform as RectTransform;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void Show(
            Object sourceOwner,
            IReadOnlyList<StatusEffectRuntimeData> effects,
            RectTransform hoveredRelicAnchor,
            Vector2 offset,
            float verticalSpacing)
        {
            ClearCards();
            owner = sourceOwner;
            if (effects == null || effects.Count == 0 || hoveredRelicAnchor == null)
                return;

            float spacing = verticalSpacing >= 0f ? verticalSpacing : DefaultSpacing;
            currentSpacing = spacing;
            Canvas.ForceUpdateCanvases();
            Vector2 canvasPoint = ScreenToCanvasPoint(GetAnchorTopRightScreenPoint(hoveredRelicAnchor));
            float scale = GetComponent<Canvas>()?.scaleFactor ?? 1f;
            Vector2 firstTopLeft = canvasPoint + offset * Mathf.Max(0.0001f, scale);

            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectRuntimeData effect = effects[i];
                if (effect == null)
                    continue;

                GameObject card = CreateCard(effect);
                RectTransform rect = card.transform as RectTransform;
                rect.anchoredPosition = firstTopLeft + new Vector2(0f, -i * (CardHeight + spacing));
                cards.Add(card);
            }

            ClampCardsInsideCanvas();
        }

        public void Hide(Object sourceOwner)
        {
            if (sourceOwner != null && owner != sourceOwner)
                return;
            owner = null;
            ClearCards();
        }

        private GameObject CreateCard(StatusEffectRuntimeData effect)
        {
            GameObject card = new("Effect", typeof(RectTransform), typeof(Image), typeof(UnitStatusEffectTooltipItemUI));
            card.transform.SetParent(transform, false);
            RectTransform cardRect = card.transform as RectTransform;
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.zero;
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            Image background = card.GetComponent<Image>();
            background.color = new Color(0.045f, 0.055f, 0.075f, 0.96f);
            background.raycastTarget = false;

            Image icon = CreateIcon(cardRect);
            TMP_Text title = CreateText(cardRect, "Title", 58f, -10f, 290f, 28f, 20f, FontStyles.Bold);
            TMP_Text description = CreateText(cardRect, "Description", 58f, -38f, 290f, 48f, 15f, FontStyles.Normal);
            description.enableWordWrapping = true;

            UnitStatusEffectTooltipItemUI item = card.GetComponent<UnitStatusEffectTooltipItemUI>();
            item.BindFallbackReferences(background, icon, title, description);
            item.Set(effect);
            return card;
        }

        private static Image CreateIcon(RectTransform parent)
        {
            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -12f);
            rect.sizeDelta = new Vector2(38f, 38f);
            Image image = iconObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(
            RectTransform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            float fontSize,
            FontStyles style)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            return text;
        }

        private Vector2 ScreenToCanvasPoint(Vector2 screenPosition)
        {
            float scale = GetComponent<Canvas>()?.scaleFactor ?? 1f;
            return screenPosition / Mathf.Max(0.0001f, scale);
        }

        private static Vector2 GetAnchorTopRightScreenPoint(RectTransform anchor)
        {
            Canvas anchorCanvas = anchor.GetComponentInParent<Canvas>();
            Camera camera = anchorCanvas != null && anchorCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? anchorCanvas.worldCamera
                : null;
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        }

        private void ClampCardsInsideCanvas()
        {
            if (rootRect == null || cards.Count == 0)
                return;

            RectTransform first = cards[0].transform as RectTransform;
            float totalHeight = cards.Count * CardHeight + Mathf.Max(0, cards.Count - 1) * currentSpacing;
            Vector2 position = first.anchoredPosition;
            position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, rootRect.rect.width - CardWidth));
            position.y = Mathf.Clamp(position.y, totalHeight, rootRect.rect.height);
            Vector2 correction = position - first.anchoredPosition;
            foreach (GameObject card in cards)
            {
                RectTransform rect = card.transform as RectTransform;
                rect.anchoredPosition += correction;
            }
        }

        private void ClearCards()
        {
            foreach (GameObject card in cards)
                if (card != null) Destroy(card);
            cards.Clear();
        }
    }
}
