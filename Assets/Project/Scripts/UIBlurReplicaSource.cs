using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBlurReplicaSource
{
    private const string SettingUpperName = "Setting_upper";

    private readonly Transform sourceRoot;
    private readonly RectTransform replicaRoot;
    private readonly List<GraphicPair> graphics = new();
    private readonly List<TransformPair> transforms = new();
    private readonly List<MaskSync> masks = new();
    private bool originalRenderingHidden;

    public UIBlurReplicaSource(Transform sourceRoot, RectTransform parent, Camera replicaCamera, int layer)
    {
        this.sourceRoot = sourceRoot;
        replicaRoot = CloneTransformTree(sourceRoot, parent, replicaCamera, layer, true);
        SyncNow();
    }

    public GameObject GameObject => replicaRoot != null ? replicaRoot.gameObject : null;

    public void SetVisible(bool visible)
    {
        if (replicaRoot != null && replicaRoot.gameObject.activeSelf != visible)
            replicaRoot.gameObject.SetActive(visible);

        SetOriginalRenderingHidden(visible);
    }

    public void SyncNow()
    {
        if (sourceRoot == null || replicaRoot == null)
            return;

        for (int i = 0; i < transforms.Count; i++)
            transforms[i].Sync();

        for (int i = 0; i < graphics.Count; i++)
            graphics[i].Sync();

        for (int i = 0; i < masks.Count; i++)
            masks[i].Sync();
    }

    public void Destroy()
    {
        RestoreOriginalRendering();

        if (replicaRoot == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(replicaRoot.gameObject);
        else
            Object.DestroyImmediate(replicaRoot.gameObject);
    }

    private RectTransform CloneTransformTree(Transform source, RectTransform parent, Camera replicaCamera, int layer, bool isRoot)
    {
        if (source == null || source.name == SettingUpperName)
            return null;

        GameObject replica = new($"{source.name}_BlurReplica", typeof(RectTransform), typeof(CanvasRenderer));
        replica.layer = layer;
        RectTransform replicaRect = replica.GetComponent<RectTransform>();
        replicaRect.SetParent(parent, false);

        RectTransform sourceRect = source as RectTransform;
        if (sourceRect != null)
            transforms.Add(new TransformPair(source, sourceRect, replicaRect, replicaCamera, isRoot));

        CloneSupportedGraphic(source, replica);
        CloneSupportedMasks(source, replica);

        for (int i = 0; i < source.childCount; i++)
            CloneTransformTree(source.GetChild(i), replicaRect, replicaCamera, layer, false);

        return replicaRect;
    }

    private void CloneSupportedGraphic(Transform source, GameObject replica)
    {
        Image sourceImage = source.GetComponent<Image>();
        if (sourceImage != null)
        {
            Image replicaImage = replica.AddComponent<Image>();
            replicaImage.raycastTarget = false;
            graphics.Add(new ImagePair(sourceImage, replicaImage));
            return;
        }

        RawImage sourceRawImage = source.GetComponent<RawImage>();
        if (sourceRawImage != null)
        {
            RawImage replicaRawImage = replica.AddComponent<RawImage>();
            replicaRawImage.raycastTarget = false;
            graphics.Add(new RawImagePair(sourceRawImage, replicaRawImage));
            return;
        }

        TMP_Text sourceText = source.GetComponent<TMP_Text>();
        if (sourceText != null)
        {
            TMP_Text replicaText = replica.AddComponent<TextMeshProUGUI>();
            replicaText.raycastTarget = false;
            graphics.Add(new TmpTextPair(sourceText, replicaText));
        }
    }

    private void CloneSupportedMasks(Transform source, GameObject replica)
    {
        Mask sourceMask = source.GetComponent<Mask>();
        if (sourceMask != null)
        {
            Mask replicaMask = replica.AddComponent<Mask>();
            masks.Add(new MaskComponentSync(sourceMask, replicaMask));
        }

        RectMask2D sourceRectMask = source.GetComponent<RectMask2D>();
        if (sourceRectMask != null)
        {
            RectMask2D replicaRectMask = replica.AddComponent<RectMask2D>();
            masks.Add(new RectMaskPair(sourceRectMask, replicaRectMask));
        }
    }

    private void SetOriginalRenderingHidden(bool hidden)
    {
        if (originalRenderingHidden == hidden)
            return;

        for (int i = 0; i < graphics.Count; i++)
            graphics[i].SetOriginalRenderingHidden(hidden);

        originalRenderingHidden = hidden;
    }

    private void RestoreOriginalRendering()
    {
        SetOriginalRenderingHidden(false);
    }

    private readonly struct TransformPair
    {
        private readonly Transform sourceTransform;
        private readonly RectTransform source;
        private readonly RectTransform replica;
        private readonly RectTransform replicaParent;
        private readonly Camera replicaCamera;
        private readonly bool isRoot;
        private readonly Vector3[] worldCorners;

        public TransformPair(Transform sourceTransform, RectTransform source, RectTransform replica, Camera replicaCamera, bool isRoot)
        {
            this.sourceTransform = sourceTransform;
            this.source = source;
            this.replica = replica;
            replicaParent = replica != null ? replica.parent as RectTransform : null;
            this.replicaCamera = replicaCamera;
            this.isRoot = isRoot;
            worldCorners = new Vector3[4];
        }

        public void Sync()
        {
            if (sourceTransform == null || source == null || replica == null || replicaParent == null)
                return;

            replica.gameObject.SetActive(sourceTransform.gameObject.activeInHierarchy);
            if (!isRoot)
            {
                SyncLocalLayout();
                return;
            }

            source.GetWorldCorners(worldCorners);

            Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
            Camera sourceCamera = sourceCanvas == null || sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : sourceCanvas.worldCamera;

            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldCorners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(replicaParent, screen, replicaCamera, out Vector2 local))
                    continue;

                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            if (min.x == float.MaxValue || max.x == float.MinValue)
                return;

            replica.anchorMin = new Vector2(0.5f, 0.5f);
            replica.anchorMax = new Vector2(0.5f, 0.5f);
            replica.pivot = source.pivot;
            Vector2 size = max - min;
            replica.anchoredPosition = min + Vector2.Scale(size, source.pivot);
            replica.sizeDelta = size;
            replica.localRotation = source.localRotation;
            replica.localScale = source.localScale;
        }

        private void SyncLocalLayout()
        {
            replica.anchorMin = source.anchorMin;
            replica.anchorMax = source.anchorMax;
            replica.pivot = source.pivot;
            replica.anchoredPosition = source.anchoredPosition;
            replica.sizeDelta = source.sizeDelta;
            replica.offsetMin = source.offsetMin;
            replica.offsetMax = source.offsetMax;
            replica.localRotation = source.localRotation;
            replica.localScale = source.localScale;
        }
    }

    private abstract class GraphicPair
    {
        protected readonly Graphic Source;
        protected readonly Graphic Replica;
        private readonly CanvasRenderer sourceRenderer;
        private bool sourceRendererHidden;

        protected GraphicPair(Graphic source, Graphic replica)
        {
            Source = source;
            Replica = replica;
            sourceRenderer = source != null ? source.canvasRenderer : null;
        }

        public virtual void Sync()
        {
            if (Source == null || Replica == null)
                return;

            Replica.enabled = Source.enabled && Source.gameObject.activeInHierarchy;
            Color color = Source.color;
            color.a *= GetEffectiveCanvasGroupAlpha(Source.transform);
            Replica.color = color;
            Replica.material = Source.material;
            Replica.raycastTarget = false;
        }

        public virtual void SetOriginalRenderingHidden(bool hidden)
        {
            if (sourceRenderer == null || sourceRendererHidden == hidden)
                return;

            if (hidden)
                CanvasRendererHideRegistry.Acquire(sourceRenderer);
            else
                CanvasRendererHideRegistry.Release(sourceRenderer);

            sourceRendererHidden = hidden;
        }

        private static float GetEffectiveCanvasGroupAlpha(Transform transform)
        {
            float alpha = 1f;
            Transform current = transform;

            while (current != null)
            {
                CanvasGroup[] groups = current.GetComponents<CanvasGroup>();
                bool ignoreParents = false;

                for (int i = 0; i < groups.Length; i++)
                {
                    CanvasGroup group = groups[i];
                    if (group == null || !group.enabled)
                        continue;

                    alpha *= group.alpha;
                    if (group.ignoreParentGroups)
                        ignoreParents = true;
                }

                if (alpha <= 0f || ignoreParents)
                    break;

                current = current.parent;
            }

            return Mathf.Clamp01(alpha);
        }
    }

    private sealed class ImagePair : GraphicPair
    {
        private readonly Image source;
        private readonly Image replica;

        public ImagePair(Image source, Image replica) : base(source, replica)
        {
            this.source = source;
            this.replica = replica;
        }

        public override void Sync()
        {
            base.Sync();
            if (source == null || replica == null)
                return;

            replica.sprite = source.sprite;
            replica.type = source.type;
            replica.preserveAspect = source.preserveAspect;
            replica.fillMethod = source.fillMethod;
            replica.fillOrigin = source.fillOrigin;
            replica.fillClockwise = source.fillClockwise;
            replica.fillAmount = source.fillAmount;
            replica.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }
    }

    private sealed class RawImagePair : GraphicPair
    {
        private readonly RawImage source;
        private readonly RawImage replica;

        public RawImagePair(RawImage source, RawImage replica) : base(source, replica)
        {
            this.source = source;
            this.replica = replica;
        }

        public override void Sync()
        {
            base.Sync();
            if (source == null || replica == null)
                return;

            replica.texture = source.texture;
            replica.uvRect = source.uvRect;
            // VFX RenderTexture Additive materials can write opaque black into the UI blur RT.
            if (source.texture is RenderTexture)
                replica.material = null;
        }
    }

    private sealed class TmpTextPair : GraphicPair
    {
        private readonly TMP_Text source;
        private readonly TMP_Text replica;
        private readonly List<CanvasRenderer> subMeshRenderers = new();
        private readonly List<CanvasRenderer> hiddenSubMeshRenderers = new();
        private bool subMeshesHidden;

        public TmpTextPair(TMP_Text source, TMP_Text replica) : base(source, replica)
        {
            this.source = source;
            this.replica = replica;
            CacheSubMeshRenderers(source);
        }

        public override void Sync()
        {
            base.Sync();
            if (source == null || replica == null)
                return;

            replica.text = source.text;
            replica.font = source.font;
            replica.fontSharedMaterial = source.fontSharedMaterial;
            replica.fontSize = source.fontSize;
            replica.fontStyle = source.fontStyle;
            replica.alignment = source.alignment;
            replica.enableAutoSizing = source.enableAutoSizing;
            replica.fontSizeMin = source.fontSizeMin;
            replica.fontSizeMax = source.fontSizeMax;
            replica.overflowMode = source.overflowMode;
            replica.textWrappingMode = source.textWrappingMode;
            replica.margin = source.margin;
            replica.characterSpacing = source.characterSpacing;
            replica.wordSpacing = source.wordSpacing;
            replica.lineSpacing = source.lineSpacing;
            replica.paragraphSpacing = source.paragraphSpacing;
            replica.richText = source.richText;
            replica.extraPadding = source.extraPadding;
            replica.parseCtrlCharacters = source.parseCtrlCharacters;
            replica.isRightToLeftText = source.isRightToLeftText;
            replica.horizontalMapping = source.horizontalMapping;
            replica.verticalMapping = source.verticalMapping;
            replica.geometrySortingOrder = source.geometrySortingOrder;
            replica.ForceMeshUpdate(true, true);
        }

        public override void SetOriginalRenderingHidden(bool hidden)
        {
            base.SetOriginalRenderingHidden(hidden);
            CacheSubMeshRenderers(source);

            if (hidden)
            {
                for (int i = 0; i < subMeshRenderers.Count; i++)
                {
                    CanvasRenderer renderer = subMeshRenderers[i];
                    if (renderer == null || hiddenSubMeshRenderers.Contains(renderer))
                        continue;

                    CanvasRendererHideRegistry.Acquire(renderer);
                    hiddenSubMeshRenderers.Add(renderer);
                }
            }
            else
            {
                for (int i = hiddenSubMeshRenderers.Count - 1; i >= 0; i--)
                {
                    CanvasRenderer renderer = hiddenSubMeshRenderers[i];
                    if (renderer != null)
                        CanvasRendererHideRegistry.Release(renderer);
                }

                hiddenSubMeshRenderers.Clear();
            }

            subMeshesHidden = hidden;
        }

        private void CacheSubMeshRenderers(TMP_Text text)
        {
            if (text == null)
                return;

            for (int i = subMeshRenderers.Count - 1; i >= 0; i--)
            {
                if (subMeshRenderers[i] == null)
                    subMeshRenderers.RemoveAt(i);
            }

            TMP_SubMeshUI[] subMeshes = text.GetComponentsInChildren<TMP_SubMeshUI>(true);
            for (int i = 0; i < subMeshes.Length; i++)
            {
                TMP_SubMeshUI subMesh = subMeshes[i];
                if (subMesh == null)
                    continue;

                CanvasRenderer renderer = subMesh.canvasRenderer;
                if (renderer == null || subMeshRenderers.Contains(renderer))
                    continue;

                subMeshRenderers.Add(renderer);
                if (subMeshesHidden && !hiddenSubMeshRenderers.Contains(renderer))
                {
                    CanvasRendererHideRegistry.Acquire(renderer);
                    hiddenSubMeshRenderers.Add(renderer);
                }
            }
        }
    }

    private static class CanvasRendererHideRegistry
    {
        private sealed class Entry
        {
            public int Count;
            public bool OriginalCull;
        }

        private static readonly Dictionary<CanvasRenderer, Entry> entries = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            entries.Clear();
        }

        public static void Acquire(CanvasRenderer renderer)
        {
            if (renderer == null)
                return;

            if (!entries.TryGetValue(renderer, out Entry entry))
            {
                entry = new Entry
                {
                    Count = 0,
                    OriginalCull = renderer.cull
                };
                entries.Add(renderer, entry);
            }

            entry.Count++;
            renderer.cull = true;
        }

        public static void Release(CanvasRenderer renderer)
        {
            if (renderer == null || !entries.TryGetValue(renderer, out Entry entry))
                return;

            entry.Count--;
            if (entry.Count > 0)
            {
                renderer.cull = true;
                return;
            }

            renderer.cull = entry.OriginalCull;
            entries.Remove(renderer);
        }
    }

    private abstract class MaskSync
    {
        public abstract void Sync();
    }

    private sealed class MaskComponentSync : MaskSync
    {
        private readonly Mask source;
        private readonly Mask replica;

        public MaskComponentSync(Mask source, Mask replica)
        {
            this.source = source;
            this.replica = replica;
        }

        public override void Sync()
        {
            if (source == null || replica == null)
                return;

            replica.enabled = source.enabled;
            replica.showMaskGraphic = source.showMaskGraphic;
        }
    }

    private sealed class RectMaskPair : MaskSync
    {
        private readonly RectMask2D source;
        private readonly RectMask2D replica;

        public RectMaskPair(RectMask2D source, RectMask2D replica)
        {
            this.source = source;
            this.replica = replica;
        }

        public override void Sync()
        {
            if (source == null || replica == null)
                return;

            replica.enabled = source.enabled;
            replica.padding = source.padding;
            replica.softness = source.softness;
        }
    }
}
