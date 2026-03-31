using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSelectionData
{
    public string selectedCharacterId;
    public List<string> equippedSkillIds = new();
    public List<string> equippedItemIds = new();

    public void Clear()
    {
        selectedCharacterId = string.Empty;
        equippedSkillIds.Clear();
        equippedItemIds.Clear();
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(selectedCharacterId);
    }
}