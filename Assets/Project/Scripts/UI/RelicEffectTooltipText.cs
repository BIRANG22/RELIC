using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Relic.Gameplay.Data
{
    [DisallowMultipleComponent]
    public sealed class RelicEffectTooltipText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TMP_Text targetText;
        private UnitStatusEffectTooltipUI tooltip;
        private RelicEffectTooltipFallbackUI fallbackTooltip;
        private string hoveredEffectId;
        private readonly List<StatusEffectRuntimeData> hoveredEffect = new(1);

        public void Bind(TMP_Text text)
        {
            targetText = text;
            enabled = targetText != null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            RefreshRewardEffectHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hoveredEffectId = null;
            HideEffects();
        }

        private void Update()
        {
            if (targetText == null)
                targetText = GetComponent<TMP_Text>();
            if (targetText == null)
                return;

            RefreshRewardEffectHover();
        }

        // Battle reward panels own their cursor offsets; other panels retain their existing settings.
        private void ResolveBattleRewardCursorOffsets(out float rightGap, out float topOffset)
        {
            rightGap = 12f;
            topOffset = float.NaN;
            BattleRewardRelicPanelUI relicPanel = GetComponentInParent<BattleRewardRelicPanelUI>();
            if (relicPanel != null)
            {
                rightGap = relicPanel.RewardEffectCursorRightGap;
                topOffset = relicPanel.RewardEffectCursorTopOffset;
                return;
            }
            BattleRewardSkillPanelUI skillPanel = GetComponentInParent<BattleRewardSkillPanelUI>();
            if (skillPanel != null)
            {
                rightGap = skillPanel.RewardEffectCursorRightGap;
                topOffset = skillPanel.RewardEffectCursorTopOffset;
                return;
            }
            RestRoomShopPanel shopPanel = GetComponentInParent<RestRoomShopPanel>();
            if (shopPanel != null)
            {
                rightGap = shopPanel.ShopEffectCursorRightGap;
                topOffset = shopPanel.ShopEffectCursorTopOffset;
            }
        }

        // Relic descriptions (battle reward and lobby shop) show ONLY the effect link underneath the mouse.
        // A TMP text rectangle may contain multiple effects and unrelated text.
        private void RefreshRewardEffectHover()
        {
            if (targetText == null || !isActiveAndEnabled || !targetText.isActiveAndEnabled)
            {
                ClearRewardEffectHover();
                return;
            }

            // A reward skill card may remain active for layout while fading out.
            // Do not allow its invisible Detail text to keep receiving link hover.
            if (!IsEffectTextVisibleAndInteractive())
            {
                ClearRewardEffectHover();
                return;
            }

            Canvas canvas = targetText.canvas;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            Vector2 cursor = Input.mousePosition;
            if (!RectTransformUtility.RectangleContainsScreenPoint(targetText.rectTransform, cursor, camera))
            {
                ClearRewardEffectHover();
                return;
            }

            // FindIntersectingLink tests actual rendered glyphs, not the entire text box.
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(targetText, cursor, camera);
            string effectId = null;
            if (linkIndex >= 0 && linkIndex < targetText.textInfo.linkCount)
            {
                string linkId = targetText.textInfo.linkInfo[linkIndex].GetLinkID();
                const string prefix = "effecticon:";
                if (!string.IsNullOrEmpty(linkId) &&
                    linkId.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    effectId = linkId.Substring(prefix.Length);
            }

            if (string.Equals(hoveredEffectId, effectId, System.StringComparison.OrdinalIgnoreCase))
            {
                // Keep the visible tooltip beside the pointer even while hovering the same link.
                if (!string.IsNullOrEmpty(effectId))
                {
                    tooltip?.UpdateCursorRightPosition(this, cursor);
                    fallbackTooltip?.UpdateCursorPosition(this, cursor);
                }
                return;
            }

            ClearRewardEffectHover();
            if (string.IsNullOrEmpty(effectId))
                return;

            DataManager manager = DataManager.Instance;
            if (manager?.EffectDatabase == null ||
                !manager.EffectDatabase.TryGet(effectId, out EffectMasterData effectData) || effectData == null)
                return;

            hoveredEffectId = effectId;
            hoveredEffect.Clear();
            hoveredEffect.Add(new StatusEffectRuntimeData(effectId, 1));
            // Prefer the component explicitly attached to RelicShopPanel.
            // Also support a separately placed scene component without creating one.
            fallbackTooltip = GetComponentInParent<RelicEffectTooltipFallbackUI>();
            if (fallbackTooltip == null)
                fallbackTooltip = RelicEffectTooltipFallbackUI.GetOrCreate();
            if (fallbackTooltip != null && fallbackTooltip.isActiveAndEnabled && fallbackTooltip.HasAssignedPrefab)
            {
                tooltip?.Hide(this);
                fallbackTooltip?.Hide(this);
                fallbackTooltip.ShowAtCursor(this, hoveredEffect, cursor);
                return;
            }

            tooltip ??= UnitStatusEffectTooltipUI.GetOrCreate();
            if (tooltip != null)
            {
                tooltip.HideSkillEffectList(this);
                fallbackTooltip?.Hide(this);
                ResolveBattleRewardCursorOffsets(out float rightGap, out float topOffset);
                tooltip.ShowCursorRight(this, hoveredEffect, cursor, rightGap, topOffset);
                return;
            }

            // No automatic FallbackUI component creation. An existing scene component is required.
            fallbackTooltip ??= RelicEffectTooltipFallbackUI.GetOrCreate();
            if (fallbackTooltip != null && fallbackTooltip.isActiveAndEnabled)
                fallbackTooltip.ShowAtCursor(this, hoveredEffect, cursor);
        }

        private bool IsEffectTextVisibleAndInteractive()
        {
            // Shop goods stay active behind the relic reward panel. This component
            // checks TMP links via Input.mousePosition, bypassing EventSystem UI
            // occlusion, so explicitly suppress shop link hover while the reward
            // panel is open. Keep the reward panel's own effect links interactive.
            RestRoomShopPanel shopPanel = targetText.GetComponentInParent<RestRoomShopPanel>();
            if (shopPanel != null)
            {
                BattleRewardRelicPanelUI rewardRelicPanel =
                    UnityEngine.Object.FindFirstObjectByType<BattleRewardRelicPanelUI>(
                        FindObjectsInactive.Exclude);
                if (rewardRelicPanel != null && rewardRelicPanel.isActiveAndEnabled &&
                    rewardRelicPanel.gameObject.activeInHierarchy)
                    return false;
            }

            // CanvasGroup alpha and raycast blocking can change without OnDisable.
            // In particular, the selected Skill_panel card stays active to avoid
            // shifting the other cards while its contents fade away.
            Transform node = targetText.transform;
            while (node != null)
            {
                if (!node.gameObject.activeInHierarchy)
                    return false;

                CanvasGroup group = node.GetComponent<CanvasGroup>();
                if (group != null && (group.alpha <= 0.001f || !group.blocksRaycasts))
                    return false;
                node = node.parent;
            }

            BattleRewardSkillPanelUI skillPanel = targetText.GetComponentInParent<BattleRewardSkillPanelUI>();
            if (skillPanel != null)
            {
                // A card's interaction is locked as soon as the player chooses
                // a memory, even if its fade-out has not yet finished.
                node = targetText.transform;
                while (node != null && node != skillPanel.transform)
                {
                    string name = node.name;
                    if (name.StartsWith("SkillPanel", System.StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(name.Substring("SkillPanel".Length), out int panelNumber) &&
                        panelNumber >= 1 && panelNumber <= 3)
                        return skillPanel.IsCardInteractionReady(panelNumber - 1);
                    if (name.StartsWith("Skill0", System.StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(name.Substring("Skill0".Length), out int cardNumber) &&
                        cardNumber >= 1 && cardNumber <= 3)
                        return skillPanel.IsCardInteractionReady(cardNumber - 1);
                    node = node.parent;
                }
            }
            return true;
        }

        private void ClearRewardEffectHover()
        {
            if (hoveredEffectId == null)
                return;
            hoveredEffectId = null;
            HideEffects();
        }

        private void OnDisable()
        {
            hoveredEffectId = null;
            HideEffects();
        }

        private void HideEffects()
        {
            tooltip?.HideSkillEffectList(this);
            tooltip?.Hide(this);
            fallbackTooltip?.Hide(this);
        }
    }
}
