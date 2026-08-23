namespace Relic.Gameplay.Data
{
    public enum QuestActionId
    {
        None = 0,
        OpenCharacterSetting = 1
    }

    public readonly struct QuestActionGateResult
    {
        public QuestActionGateResult(bool allowed, string blockedReason)
        {
            Allowed = allowed;
            BlockedReason = blockedReason ?? string.Empty;
        }

        public bool Allowed { get; }
        public string BlockedReason { get; }
    }

    public readonly struct QuestDisplayState
    {
        public QuestDisplayState(bool visible, string questId, string text)
        {
            Visible = visible;
            QuestId = questId ?? string.Empty;
            Text = text ?? string.Empty;
        }

        public bool Visible { get; }
        public string QuestId { get; }
        public string Text { get; }
    }

    public class QuestManager
    {
        public const string DefaultTutorialQuestId = "tutorial.open_character_setting";
        public const string CharacterSettingSystemId = "system.character_setting";

        private const string DefaultTutorialQuestText = "파티편성 및 캐릭터 세팅을 먼저 확인하세요.";
        private const string DefaultBlockedReason = "현재 퀘스트를 먼저 진행해야 합니다.";

        private LobbyRuntimeData lobby;

        public void Initialize(LobbyRuntimeData lobbyRuntime)
        {
            lobby = lobbyRuntime ?? new LobbyRuntimeData();
            Normalize(lobby);

            if (string.IsNullOrWhiteSpace(lobby.ActiveQuestId) &&
                !Contains(lobby.CompletedQuestIds, DefaultTutorialQuestId))
            {
                lobby.ActiveQuestId = DefaultTutorialQuestId;
            }
        }

        public QuestActionGateResult CanPerformAction(QuestActionId actionId)
        {
            EnsureInitialized();

            if (actionId == QuestActionId.OpenCharacterSetting &&
                IsDefaultTutorialQuestActive() &&
                !Contains(lobby.UnlockedSystemIds, CharacterSettingSystemId))
            {
                return new QuestActionGateResult(false, DefaultBlockedReason);
            }

            return new QuestActionGateResult(true, string.Empty);
        }

        public void MarkActionCompleted(QuestActionId actionId)
        {
            EnsureInitialized();

            if (actionId != QuestActionId.OpenCharacterSetting)
                return;

            AddUnique(lobby.UnlockedSystemIds, CharacterSettingSystemId);

            if (IsDefaultTutorialQuestActive())
            {
                AddUnique(lobby.CompletedQuestIds, DefaultTutorialQuestId);
                lobby.ActiveQuestId = string.Empty;
            }
        }

        public QuestDisplayState GetCurrentDisplayState()
        {
            EnsureInitialized();

            return string.Equals(lobby.ActiveQuestId, DefaultTutorialQuestId, System.StringComparison.Ordinal)
                ? new QuestDisplayState(true, DefaultTutorialQuestId, DefaultTutorialQuestText)
                : new QuestDisplayState(false, string.Empty, string.Empty);
        }

        public void AcceptQuest(string questId)
        {
            EnsureInitialized();

            if (!string.IsNullOrWhiteSpace(questId))
                lobby.ActiveQuestId = questId.Trim();
        }

        public void CompleteQuest(string questId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(questId))
                return;

            string normalizedId = questId.Trim();
            AddUnique(lobby.CompletedQuestIds, normalizedId);

            if (string.Equals(lobby.ActiveQuestId, normalizedId, System.StringComparison.Ordinal))
                lobby.ActiveQuestId = string.Empty;
        }

        public bool IsCompleted(string questId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(questId) &&
                   Contains(lobby.CompletedQuestIds, questId.Trim());
        }

        private bool IsDefaultTutorialQuestActive()
        {
            return string.Equals(lobby.ActiveQuestId, DefaultTutorialQuestId, System.StringComparison.Ordinal);
        }

        private void EnsureInitialized()
        {
            if (lobby == null)
                Initialize(new LobbyRuntimeData());
        }

        private static void Normalize(LobbyRuntimeData value)
        {
            value.CompletedQuestIds ??= new System.Collections.Generic.List<string>();
            value.UnlockedSystemIds ??= new System.Collections.Generic.List<string>();
        }

        private static void AddUnique(System.Collections.Generic.List<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value) || Contains(values, value))
                return;

            values.Add(value);
        }

        private static bool Contains(System.Collections.Generic.List<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value))
                return false;

            string normalizedValue = value.Trim();
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], normalizedValue, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
