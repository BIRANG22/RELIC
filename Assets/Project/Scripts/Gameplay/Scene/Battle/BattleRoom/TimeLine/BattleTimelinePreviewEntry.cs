using Relic.Gameplay.Data;
using UnityEngine;

public class BattleTimelinePreviewEntry
{
    public int SlotIndex;
    public int OrderIndex;

    public CharacterRuntimeData UserRuntime;
    public SkillMasterData SkillData;

    public string CharacterId => UserRuntime != null ? UserRuntime.CharacterId : "";
    public string SkillId => SkillData != null ? SkillData.SkillId : "";

    public Sprite OwnerIcon => GetCharacterIcon(CharacterId);
    public Sprite SkillIcon => GetSkillIcon(SkillId);

    public static BattleTimelinePreviewEntry CreatePlayer(
        int slotIndex,
        int orderIndex,
        BattleReservedCommand command)
    {
        if (command == null)
            return null;

        return new BattleTimelinePreviewEntry
        {
            SlotIndex = slotIndex,
            OrderIndex = orderIndex,
            UserRuntime = command.UserRuntime,
            SkillData = command.SkillData
        };
    }

    private static Sprite GetCharacterIcon(string characterId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out Sprite icon))
            return icon;

        return null;
    }

    private static Sprite GetSkillIcon(string skillId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out Sprite icon))
            return icon;

        return null;
    }
}