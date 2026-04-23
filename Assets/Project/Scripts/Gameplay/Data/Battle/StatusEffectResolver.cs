using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
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
