using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class RelicEffectTooltipAutoBinder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        public static void TryBind(TMP_Text text)
        {
            if (text == null)
                return;

            bool hasEffects = RelicDescriptionFormatter.ExtractEffectIds(text.text).Count > 0;
            RelicEffectTooltipText hover = text.GetComponent<RelicEffectTooltipText>();
            if (!hasEffects)
            {
                if (hover != null)
                    hover.enabled = false;
                return;
            }

            if (hover == null)
                hover = text.gameObject.AddComponent<RelicEffectTooltipText>();
            text.richText = true;
            text.raycastTarget = true;
            hover.Bind(text);
        }

        public static void RefreshExistingBinding(TMP_Text text)
        {
            if (text == null || text.GetComponent<RelicDescriptionTextMarker>() == null)
                return;

            TryBind(text);
        }

        private static void OnTextChanged(Object changedObject)
        {
            if (changedObject is TMP_Text text && (text.text ?? string.Empty).Contains("effecticon:"))
                RefreshExistingBinding(text);
        }
    }

    [DisallowMultipleComponent]
    public sealed class RelicDescriptionTextMarker : MonoBehaviour
    {
    }
}
