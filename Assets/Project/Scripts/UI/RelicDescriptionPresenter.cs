using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class RelicDescriptionPresenter
    {
        public static void Set(TMP_Text target, string description)
        {
            if (target == null)
                return;

            if (target.GetComponent<RelicDescriptionTextMarker>() == null)
                target.gameObject.AddComponent<RelicDescriptionTextMarker>();

            string formatted = RelicDescriptionFormatter.Format(description);
            target.text = formatted;

            RelicEffectTooltipAutoBinder.TryBind(target);
        }
    }
}
