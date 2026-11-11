using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Relic.Gameplay.Data
{
    [DisallowMultipleComponent]
    public sealed class RelicEffectTooltipText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Vector2 tooltipOffset = new(24f, 0f);
        [SerializeField, Min(0f)] private float verticalSpacing;

        private TMP_Text targetText;
        private UnitStatusEffectTooltipUI tooltip;
        private RelicEffectTooltipFallbackUI fallbackTooltip;
        private readonly List<StatusEffectRuntimeData> effects = new();
        private bool pointerInside;

        public void Bind(TMP_Text text)
        {
            targetText = text;
            enabled = targetText != null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            ShowEffects();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            HideEffects();
        }

        private void Update()
        {
            if (targetText == null)
                targetText = GetComponent<TMP_Text>();
            if (targetText == null)
                return;

            Canvas canvas = targetText.canvas;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(
                targetText.rectTransform,
                Input.mousePosition,
                camera);
            if (inside == pointerInside)
                return;

            pointerInside = inside;
            if (pointerInside)
                ShowEffects();
            else
                HideEffects();
        }

        private void ShowEffects()
        {
            if (targetText == null)
                targetText = GetComponent<TMP_Text>();
            if (targetText == null)
                return;

            effects.Clear();
            DataManager manager = DataManager.Instance;
            foreach (string id in RelicDescriptionFormatter.ExtractEffectIds(targetText.text))
            {
                if (manager?.EffectDatabase != null &&
                    manager.EffectDatabase.TryGet(id, out EffectMasterData data) && data != null)
                    effects.Add(new StatusEffectRuntimeData(id, 1));
            }

            if (effects.Count == 0)
                return;

            tooltip ??= UnitStatusEffectTooltipUI.GetOrCreate();
            if (tooltip != null)
            {
                fallbackTooltip?.Hide(this);
                tooltip.ShowSkillEffectList(
                    this,
                    effects,
                    targetText.rectTransform,
                    null,
                    tooltipOffset,
                    verticalSpacing);
                return;
            }

            fallbackTooltip ??= RelicEffectTooltipFallbackUI.GetOrCreate();
            fallbackTooltip?.Show(
                this,
                effects,
                targetText.rectTransform,
                tooltipOffset,
                verticalSpacing);
        }

        private void OnDisable()
        {
            pointerInside = false;
            HideEffects();
        }

        private void HideEffects()
        {
            tooltip?.HideSkillEffectList(this);
            fallbackTooltip?.Hide(this);
        }
    }
}
