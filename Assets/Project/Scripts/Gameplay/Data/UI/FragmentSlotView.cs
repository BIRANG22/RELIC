using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class FragmentSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text stateText;

        public void SetFragment(string fragmentId)
        {
            stateText.text = string.IsNullOrWhiteSpace(fragmentId) ? "Unequipped" : fragmentId;
        }
    }
}
