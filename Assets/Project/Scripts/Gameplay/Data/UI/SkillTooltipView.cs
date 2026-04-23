using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class SkillTooltipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private TMP_Text rangeText;

        public void Bind(CommonSkillData skill, SkillRangeData range)
        {
            titleText.text = skill?.Name ?? string.Empty;
            costText.text = $"{skill?.CostResource} {skill?.CostValue}";
            effectText.text = skill == null ? string.Empty : $"Effects: {skill.Effects.Count}";
            rangeText.text = range?.RangeCategory ?? string.Empty;
        }
    }
}
