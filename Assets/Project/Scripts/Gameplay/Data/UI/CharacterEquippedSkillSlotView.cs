using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class CharacterEquippedSkillSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text skillNameText;

        public void SetSkill(string skillId)
        {
            skillNameText.text = string.IsNullOrWhiteSpace(skillId) ? "Empty" : skillId;
        }
    }
}
