using System.Collections.Generic;
using UnityEngine;

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