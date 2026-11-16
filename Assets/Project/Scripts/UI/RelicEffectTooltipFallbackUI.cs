using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Relic.Gameplay.Data
{
    [DisallowMultipleComponent]
    public sealed class RelicEffectTooltipFallbackUI : MonoBehaviour
    {
        [Header("Optional Prefab (Lobby Relic Shop)")]
        [Tooltip("Assign the existing UnitStatusEffectTooltipItemUI prefab. If empty, the generated fallback is used.")]
        [SerializeField] private UnitStatusEffectTooltipItemUI effectItemPrefab;
        [Header("Prefab Cursor Follow")]
        [SerializeField, Min(0f)] private float prefabCursorRightGap = 12f;
        [SerializeField] private float prefabCursorTopOffset = 8f;
        [SerializeField, Min(0f)] private float prefabScreenPadding = 8f;
        [SerializeField, Min(0f)] private float prefabFadeInDuration = 0.12f;
        [SerializeField, Min(0f)] private float prefabFadeOutDuration = 0.05f;
        private Canvas prefabOverlayCanvas;
        private RectTransform prefabOverlayRect;
        private UnitStatusEffectTooltipItemUI prefabItem;
        private CanvasGroup prefabItemGroup;
        private Coroutine prefabFadeRoutine;
        private readonly Vector3[] prefabCorners = new Vector3[4];
        public bool HasAssignedPrefab => effectItemPrefab != null;

        private const float CardWidth = 360f;
        private const float CardHeight = 96f;
        private const float DefaultSpacing = 8f;
        private static RelicEffectTooltipFallbackUI instance;

        private readonly List<GameObject> cards = new();
        private RectTransform rootRect;
        private Object owner;
        private float currentSpacing = DefaultSpacing;

        // Use only the component placed on a scene object (e.g. RelicShopPanel).
        // Do not create a separate RelicEffectTooltipFallbackUI GameObject at runtime.
        public static RelicEffectTooltipFallbackUI GetOrCreate()
        {
            if (instance != null && instance.isActiveAndEnabled)
                return instance;

            instance = FindFirstObjectByType<RelicEffectTooltipFallbackUI>();
            return instance;
        }

        private void Awake()
        {
            instance = this;
            rootRect = transform as RectTransform;
        }

        private void OnDestroy()
        {
            ClearPrefabItem();
            if (prefabOverlayCanvas != null && prefabOverlayCanvas.gameObject != gameObject)
                Destroy(prefabOverlayCanvas.gameObject);
            if (instance == this)
                instance = null;
        }

        private void OnDisable()
        {
            if (prefabFadeRoutine != null) StopCoroutine(prefabFadeRoutine);
            prefabFadeRoutine = null;
            ClearPrefabItem();
        }

        private void EnsurePrefabOverlay()
        {
            if (prefabOverlayRect != null) return;
            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas != null && ownCanvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                transform.parent == null)
            {
                prefabOverlayCanvas = ownCanvas;
                prefabOverlayRect = transform as RectTransform;
                return;
            }
            GameObject overlay = new GameObject("RelicEffectTooltipPrefabCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            prefabOverlayCanvas = overlay.GetComponent<Canvas>();
            prefabOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            prefabOverlayCanvas.overrideSorting = true;
            prefabOverlayCanvas.sortingOrder = 32767;
            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            prefabOverlayRect = overlay.transform as RectTransform;
        }

        private void ClearPrefabItem()
        {
            if (prefabItem != null) Destroy(prefabItem.gameObject);
            prefabItem = null;
            prefabItemGroup = null;
        }

        private IEnumerator FadePrefab(float targetAlpha, float duration, bool clear)
        {
            CanvasGroup group = prefabItemGroup;
            if (group == null) yield break;
            float startAlpha = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration && group != null)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (group != null) group.alpha = targetAlpha;
            if (clear) ClearPrefabItem();
            prefabFadeRoutine = null;
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

        // Backup UI when the normal effect tooltip prefab is not available.
        // The reward panel uses this for a single hovered effect next to the cursor.
        public void ShowAtCursor(Object sourceOwner,
            IReadOnlyList<StatusEffectRuntimeData> effects, Vector2 cursorScreenPosition)
        {
            ClearCards();
            owner = sourceOwner;
            if (prefabFadeRoutine != null) StopCoroutine(prefabFadeRoutine);
            prefabFadeRoutine = null;
            ClearPrefabItem();
            if (effects == null || effects.Count == 0)
                return;

            if (effectItemPrefab != null)
            {
                EnsurePrefabOverlay();
                if (prefabOverlayRect == null) return;
                prefabItem = Instantiate(effectItemPrefab, prefabOverlayRect, false);
                prefabItem.Set(effects[0]);
                prefabItem.gameObject.SetActive(true);
                prefabItemGroup = prefabItem.GetComponent<CanvasGroup>();
                if (prefabItemGroup == null)
                    prefabItemGroup = prefabItem.gameObject.AddComponent<CanvasGroup>();
                prefabItemGroup.blocksRaycasts = false;
                prefabItemGroup.interactable = false;
                prefabItemGroup.alpha = prefabFadeInDuration > 0f ? 0f : 1f;
                Canvas.ForceUpdateCanvases();
                UpdateCursorPosition(sourceOwner, cursorScreenPosition);
                if (prefabFadeInDuration > 0f)
                    prefabFadeRoutine = StartCoroutine(FadePrefab(1f, prefabFadeInDuration, false));
                return;
            }

            Vector2 canvasPoint = ScreenToCanvasPoint(cursorScreenPosition);
            float scale = GetComponent<Canvas>()?.scaleFactor ?? 1f;
            Vector2 topLeft = canvasPoint + new Vector2(12f, 8f) * Mathf.Max(0.0001f, scale);
            GameObject card = CreateCard(effects[0]);
            RectTransform rect = card.transform as RectTransform;
            rect.anchoredPosition = topLeft;
            cards.Add(card);
            ClampCardsInsideCanvas();
        }

        // Reposition existing hover cards without rebuilding them or restarting their effect UI.
        public void UpdateCursorPosition(Object sourceOwner, Vector2 cursorScreenPosition)
        {
            if (owner != sourceOwner) return;
            if (prefabItem != null && prefabOverlayRect != null)
            {
                RectTransform rect = prefabItem.transform as RectTransform;
                float prefabCanvasScale = Mathf.Max(0.0001f, prefabOverlayCanvas.scaleFactor);
                Vector2 screenTarget = cursorScreenPosition +
                    new Vector2(prefabCursorRightGap, prefabCursorTopOffset) * prefabCanvasScale;
                if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        prefabOverlayRect, screenTarget, null, out Vector3 target)) return;
                rect.GetWorldCorners(prefabCorners);
                rect.position += target - prefabCorners[1];
                rect.GetWorldCorners(prefabCorners);
                float minX = Mathf.Min(prefabCorners[0].x, prefabCorners[1].x, prefabCorners[2].x, prefabCorners[3].x);
                float maxX = Mathf.Max(prefabCorners[0].x, prefabCorners[1].x, prefabCorners[2].x, prefabCorners[3].x);
                float minY = Mathf.Min(prefabCorners[0].y, prefabCorners[1].y, prefabCorners[2].y, prefabCorners[3].y);
                float maxY = Mathf.Max(prefabCorners[0].y, prefabCorners[1].y, prefabCorners[2].y, prefabCorners[3].y);
                float pad = prefabScreenPadding * prefabCanvasScale;
                float dx = maxX > Screen.width - pad ? Screen.width - pad - maxX :
                    (minX < pad ? pad - minX : 0f);
                float dy = maxY > Screen.height - pad ? Screen.height - pad - maxY :
                    (minY < pad ? pad - minY : 0f);
                rect.position += new Vector3(dx, dy, 0f);
                return;
            }
            if (cards.Count == 0 || cards[0] == null) return;

            RectTransform cardRect = cards[0].transform as RectTransform;
            if (cardRect == null)
                return;

            Vector2 canvasPoint = ScreenToCanvasPoint(cursorScreenPosition);
            float scale = GetComponent<Canvas>()?.scaleFactor ?? 1f;
            cardRect.anchoredPosition = canvasPoint + new Vector2(12f, 8f) * Mathf.Max(0.0001f, scale);
            ClampCardsInsideCanvas();
        }

        public void Hide(Object sourceOwner)
        {
            if (sourceOwner != null && owner != sourceOwner)
                return;
            owner = null;
            ClearCards();
            if (prefabFadeRoutine != null) StopCoroutine(prefabFadeRoutine);
            prefabFadeRoutine = null;
            if (prefabItem == null) return;
            if (prefabFadeOutDuration > 0f)
                prefabFadeRoutine = StartCoroutine(FadePrefab(0f, prefabFadeOutDuration, true));
            else ClearPrefabItem();
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
            description.textWrappingMode = TextWrappingModes.Normal;

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
