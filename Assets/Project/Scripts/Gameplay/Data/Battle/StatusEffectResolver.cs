using System.Collections.Generic;


/// <summary>
/// [Battle] 스크립트. 역할/설정/변수 용도를 코드 주석으로 확인할 수 있도록 정리했습니다.
/// Unity 연결: MonoBehaviour 스크립트는 Scene/GameObject에 컴포넌트로 부착 후 Inspector 필드를 설정하세요.
/// 데이터 클래스는 엑셀 시트 컬럼과 필드명을 맞춰 DataBootstrap 로딩 파이프라인에서 자동 매핑됩니다.
/// </summary>
namespace Relic.Gameplay.Data
{
    /// <summary>
    /// StatusEffectResolver의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public class StatusEffectResolver
    {
        public void Apply(List<StatusEffectInstanceData> list, StatusEffectInstanceData incoming)
        {
            var exist = list.Find(x => x.StatusEffectId == incoming.StatusEffectId);
            if (exist == null)
            {
                list.Add(incoming);
                return;
            }

            exist.StackCount += incoming.StackCount;
            exist.Value += incoming.Value;
            exist.RemainingTurn = incoming.RemainingTurn > exist.RemainingTurn ? incoming.RemainingTurn : exist.RemainingTurn;
        }

        public void EndTurn(List<StatusEffectInstanceData> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                list[i].RemainingTurn--;
                if (list[i].RemainingTurn <= 0)
                    list.RemoveAt(i);
            }
        }
    }
}
