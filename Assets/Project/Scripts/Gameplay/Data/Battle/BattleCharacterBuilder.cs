
/// <summary>
/// [Battle] 스크립트. 역할/설정/변수 용도를 코드 주석으로 확인할 수 있도록 정리했습니다.
/// Unity 연결: MonoBehaviour 스크립트는 Scene/GameObject에 컴포넌트로 부착 후 Inspector 필드를 설정하세요.
/// 데이터 클래스는 엑셀 시트 컬럼과 필드명을 맞춰 DataBootstrap 로딩 파이프라인에서 자동 매핑됩니다.
/// </summary>
namespace Relic.Gameplay.Data
{
    /// <summary>
    /// BattleCharacterBuilder의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public class BattleCharacterBuilder
    {
        public BattleCharacterContext Build(CharacterMasterData master, CharacterGrowthData growth, CharacterEquipmentData equipment, CharacterStateData state)
        {
            return new BattleCharacterContext
            {
                CharacterId = master.CharacterId,
                Name = master.Name,
                MaxHealth = master.MaxHealth,
                CurrentHealth = state.CurrentHealth > 0 ? state.CurrentHealth : master.MaxHealth,
                CurrentStamina = state.CurrentStamina > 0 ? state.CurrentStamina : master.MaxStamina,
                SkillLoadout = equipment.SkillLoadout
            };
        }
    }

    /// <summary>
    /// BattleCharacterContext의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public class BattleCharacterContext
    {
        /// <summary>
        /// CharacterId: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public string CharacterId;
        /// <summary>
        /// Name: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public string Name;
        /// <summary>
        /// MaxHealth: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public int MaxHealth;
        /// <summary>
        /// CurrentHealth: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public int CurrentHealth;
        /// <summary>
        /// CurrentStamina: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public int CurrentStamina;
        /// <summary>
        /// SkillLoadout: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public CharacterSkillLoadout SkillLoadout;
    }
}
