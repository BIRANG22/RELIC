using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// SkillDescriptionFormatter가 생성한 effecticon 링크 위치에 상태 효과 아이콘을 표시합니다.
    /// Sprite는 공용 StatusEffectIconDatabase에서 자동으로 불러옵니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillEffectInlineIconRenderer : MonoBehaviour
    {
        private const string LinkPrefix = "effecticon:";
        private const float GlobalEffectIconScale = 1.6f;
        internal const string DefaultEffectIconSpacingTag = "<space=1em>";
        internal const string GlobalEffectIconSpacingTag = "<space=1.6em>";
        private TMP_Text targetText;
        private readonly List<Image> spawnedIcons = new();
        private string lastText = null;
        private bool refreshRequested;
        private bool underlineIcons;

        public void SetUnderlineIcons(bool enabled)
        {
            if (underlineIcons == enabled) return;
            underlineIcons = enabled;
            RequestRefresh();
        }

        public void Bind(TMP_Text text)
        {
            targetText = text;
            RequestRefresh();
        }

        public void RequestRefresh()
        {
            refreshRequested = true;
        }

        private void OnEnable()
        {
            refreshRequested = true;
        }

        private void OnDisable()
        {
            ClearIcons();
            lastText = null;
        }

        private void LateUpdate()
        {
            if (targetText == null)
                targetText = GetComponent<TMP_Text>();

            if (targetText == null)
                return;

            string current = targetText.text ?? string.Empty;
            if (!refreshRequested && string.Equals(lastText, current, StringComparison.Ordinal))
                return;

            refreshRequested = false;
            lastText = current;
            RebuildIcons();
        }

        private void RebuildIcons()
        {
            ClearIcons();

            if (targetText == null || string.IsNullOrEmpty(targetText.text) || !targetText.text.Contains(LinkPrefix))
                return;

            DataManager dataManager = DataManager.Instance;
            if (dataManager?.StatusEffectIconDatabase == null)
                return;

            targetText.ForceMeshUpdate();
            TMP_TextInfo textInfo = targetText.textInfo;
            if (textInfo == null || textInfo.linkCount <= 0)
                return;

            for (int i = 0; i < textInfo.linkCount; i++)
            {
                TMP_LinkInfo link = textInfo.linkInfo[i];
                string linkId = link.GetLinkID();
                if (string.IsNullOrWhiteSpace(linkId) || !linkId.StartsWith(LinkPrefix, StringComparison.Ordinal))
                    continue;

                string effectId = linkId.Substring(LinkPrefix.Length);
                if (string.IsNullOrWhiteSpace(effectId) ||
                    !dataManager.StatusEffectIconDatabase.TryGetIcon(effectId, out Sprite sprite) ||
                    sprite == null)
                    continue;

                int lastIndex = link.linkTextfirstCharacterIndex + link.linkTextLength - 1;
                if (lastIndex < 0 || lastIndex >= textInfo.characterCount)
                    continue;

                TMP_CharacterInfo character = textInfo.characterInfo[lastIndex];
                float height = Mathf.Max(1f, character.ascender - character.descender);
                float size = height * 0.9f * GlobalEffectIconScale;
                Vector3 position = new Vector3(
                    character.topRight.x + size * 0.58f,
                    (character.ascender + character.descender) * 0.5f,
                    0f);

                GameObject iconObject = new GameObject($"EffectIcon_{effectId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(targetText.rectTransform, false);

                RectTransform rect = iconObject.GetComponent<RectTransform>();
                rect.anchorMin = targetText.rectTransform.pivot;
                rect.anchorMax = targetText.rectTransform.pivot;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                rect.localPosition = position;
                rect.localScale = Vector3.one;

                Image image = iconObject.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                spawnedIcons.Add(image);

            }
        }

        private void ClearIcons()
        {
            for (int i = spawnedIcons.Count - 1; i >= 0; i--)
            {
                Image image = spawnedIcons[i];
                if (image != null)
                    Destroy(image.gameObject);
            }
            spawnedIcons.Clear();
        }
    }

    public static class SkillEffectInlineIconUtility
    {
        public static void SetText(TMP_Text target, string text)
        {
            if (target == null)
                return;

            target.richText = true;

            string formattedText = text ?? string.Empty;
            if (formattedText.Contains("effecticon:", StringComparison.Ordinal))
            {
                // 커진 인라인 효과 아이콘 크기에 맞춰 텍스트 간격도 함께 확장합니다.
                // 일반 설명과 다른 UI에서도 효과 아이콘과 글자가 겹치지 않도록 간격을 조정합니다.
                formattedText = formattedText.Replace(
                    SkillEffectInlineIconRenderer.DefaultEffectIconSpacingTag,
                    SkillEffectInlineIconRenderer.GlobalEffectIconSpacingTag,
                    StringComparison.Ordinal);
            }

            target.text = formattedText;

            SkillEffectInlineIconRenderer renderer = target.GetComponent<SkillEffectInlineIconRenderer>();
            if (renderer == null)
                renderer = target.gameObject.AddComponent<SkillEffectInlineIconRenderer>();

            renderer.Bind(target);
        }
    }
}
