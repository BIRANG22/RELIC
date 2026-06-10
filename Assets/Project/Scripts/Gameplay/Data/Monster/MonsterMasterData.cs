using System;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterMasterData
    {
        public string MonsterId;
        public string Name;
        public string Grade;
        public int Health;
        public string DropTableId;
        public string PossSkillId01;
        public string PossSkillId02;
        public string PossSkillId03;
        public string PossSkillId04;
        public string PossSkillId05;
        public string PossSkillId06;
        public string PossSkillId07;
        public string PossSkillId08;
        public string PossSkillId09;
        public string PossSkillId10;

        [NonSerialized]
        public GameObject BattlePrefab;
    }
}
