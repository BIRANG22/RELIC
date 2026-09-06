using UnityEngine;

/// <summary>
/// 기존 SelectButton 호환용 스크립트입니다.
/// 현재는 캐릭터 버튼을 누르는 즉시 파티 편성이 반영되므로 사용하지 않습니다.
/// SelectButton 오브젝트와 이 컴포넌트는 제거해도 됩니다.
/// </summary>
public class CharacterConfirmButton : MonoBehaviour
{
    public void Execute()
    {
        // 캐릭터 편성은 CharPick에서 즉시 처리하므로 별도의 확정 동작이 필요하지 않습니다.
    }
}
