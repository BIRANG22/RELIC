using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class EventChoiceData
    {
        public string EventId;
        public int ChoiceOrder;
        public string ChoiceText;
        public string ChoiceType;
        public string SelectCondition;
        public string CostType;
        public string CostTarget;
        public int CostValue;
        public string ResultType;
        public string ResultTarget;
        public int ResultValue;
        public float SuccessRate;
        public string FailResult;
        public string NextEventId;
    }
}
