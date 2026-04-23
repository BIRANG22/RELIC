using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class RuneSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text stateText;

        public void SetRune(string runeId)
        {
            stateText.text = string.IsNullOrWhiteSpace(runeId) ? "Unequipped" : runeId;
        }
    }
}
