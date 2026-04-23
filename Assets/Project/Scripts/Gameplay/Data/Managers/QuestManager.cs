using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class QuestManager
    {
        private readonly HashSet<string> accepted = new();
        private readonly HashSet<string> completed = new();

        public void AcceptQuest(string questId) => accepted.Add(questId);
        public void CompleteQuest(string questId) { if (accepted.Contains(questId)) completed.Add(questId); }
        public bool IsCompleted(string questId) => completed.Contains(questId);
    }
}
