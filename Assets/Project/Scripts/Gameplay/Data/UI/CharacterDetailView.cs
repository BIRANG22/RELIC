using TMPro;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class CharacterDetailView : MonoBehaviour
    {
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text resourceText;
        [SerializeField] private TMP_Text moveLevelText;

        public void Bind(CharacterMasterData master)
        {
            healthText.text = $"HP {master.MaxHealth}";
            resourceText.text = $"{master.ResourceType} {master.MaxResource}";
            moveLevelText.text = $"Move Lv.{master.MoveLevel}";
        }
    }
}
