using UnityEngine;
using UnityEngine.UI;

namespace Relic.Gameplay.Data
{
    public class SkillIconButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private string skillId;

        private CharacterEquipmentManager equipmentManager;
        private CharacterSelectionManager selectionManager;

        public void Initialize(CharacterEquipmentManager equipment, CharacterSelectionManager selection)
        {
            equipmentManager = equipment;
            selectionManager = selection;
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (selectionManager == null || equipmentManager == null)
                return;

            equipmentManager.EquipCommon(selectionManager.CurrentCharacterId, 0, skillId);
        }
    }
}
