
/// <summary>
/// [Battle] 스크립트. 역할/설정/변수 용도를 코드 주석으로 확인할 수 있도록 정리했습니다.
/// Unity 연결: MonoBehaviour 스크립트는 Scene/GameObject에 컴포넌트로 부착 후 Inspector 필드를 설정하세요.
/// 데이터 클래스는 엑셀 시트 컬럼과 필드명을 맞춰 DataBootstrap 로딩 파이프라인에서 자동 매핑됩니다.
/// </summary>
namespace Relic.Gameplay.Data
{
    /// <summary>
    /// BattleMonsterBuilder의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public class BattleMonsterBuilder
    {
        public BattleMonsterContext Build(MonsterMasterData master, MonsterStateData state, MonsterPatternData pattern)
        {
            return new BattleMonsterContext
            {
                MonsterId = master.MonsterId,
                Name = master.Name,
                CurrentHealth = state.CurrentHealth > 0 ? state.CurrentHealth : master.Health,
                Pattern = pattern
            };
        }
    }

    /// <summary>
    /// BattleMonsterContext의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public class BattleMonsterContext
    {
        /// <summary>
        /// MonsterId: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public string MonsterId;
        /// <summary>
        /// Name: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public string Name;
        /// <summary>
        /// CurrentHealth: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public int CurrentHealth;
        /// <summary>
        /// Pattern: 엑셀/런타임 데이터에서 이 필드가 담아야 할 값을 저장합니다.
        /// </summary>
        public MonsterPatternData Pattern;
    }
}
