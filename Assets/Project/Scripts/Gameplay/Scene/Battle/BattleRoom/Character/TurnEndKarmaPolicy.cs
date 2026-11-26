using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public sealed class TurnEndKarmaPolicy
{
    private readonly Dictionary<string, HashSet<int>> slotsByCharacter = new(StringComparer.Ordinal);
    public void Record(string characterId, int slotIndex, bool eligible)
    {
        if (!eligible || string.IsNullOrWhiteSpace(characterId) || slotIndex < 0) return;
        if (!slotsByCharacter.TryGetValue(characterId, out HashSet<int> slots))
            slotsByCharacter.Add(characterId, slots = new HashSet<int>());
        slots.Add(slotIndex);
    }
    public int GetHalfUnitGain(string characterId) =>
        characterId != null && slotsByCharacter.TryGetValue(characterId, out HashSet<int> slots)
            ? Math.Min(5, slots.Count) : 0;
    public IEnumerable<KeyValuePair<string, int>> GetRecordedGains()
    {
        foreach (KeyValuePair<string, HashSet<int>> pair in slotsByCharacter)
            yield return new KeyValuePair<string, int>(pair.Key, Math.Min(5, pair.Value.Count));
    }
    public static bool IsMemoryCategory(Category category) =>
        category != Category.Move && category != Category.Passive;
    public static TurnEndKarmaPolicy FromExecutedCommands(
        IEnumerable<PlayerReservedCommand> executedCommands)
    {
        TurnEndKarmaPolicy policy = new();
        if (executedCommands == null)
            return policy;

        foreach (PlayerReservedCommand command in executedCommands)
        {
            if (command?.SkillData == null)
                continue;

            bool valid = command.ExecutionResult != null &&
                         command.ExecutionResult.WasValidExecution;
            policy.Record(
                command.CharacterId,
                command.TimelineSlotIndex,
                valid && IsMemoryCategory(command.SkillData.Category));
        }
        return policy;
    }
    public void Clear() => slotsByCharacter.Clear();
}
