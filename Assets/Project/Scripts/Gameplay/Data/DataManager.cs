using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// [DataManager.cs] 스크립트. 역할/설정/변수 용도를 코드 주석으로 확인할 수 있도록 정리했습니다.
/// Unity 연결: MonoBehaviour 스크립트는 Scene/GameObject에 컴포넌트로 부착 후 Inspector 필드를 설정하세요.
/// 데이터 클래스는 엑셀 시트 컬럼과 필드명을 맞춰 DataBootstrap 로딩 파이프라인에서 자동 매핑됩니다.
/// </summary>
public class DataManager : Singleton<DataManager>
{
    //private Dictionary<int, CardData> cardTable = new();

    public void Initialize()
    {
        LoadHardCodedData();
    }

    private void LoadHardCodedData()
    {
        //cardTable[1] = new CardData
        //{
        //    Id = 1,
        //    Name = "Slash",
        //    SPCost = 2,
        //    Damage = 10
        //};

        //cardTable[2] = new CardData
        //{
        //    Id = 2,
        //    Name = "FireBall",
        //    SPCost = 3,
        //    Damage = 15
        //};
    }

    //public CardData GetCard(int id)
    //{
    //    return cardTable.TryGetValue(id, out var data) ? data : null;
    //}
}
