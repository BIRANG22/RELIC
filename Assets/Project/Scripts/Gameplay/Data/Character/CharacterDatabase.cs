using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] private List<CharacterData> characters = new();

    private Dictionary<string, CharacterData> cachedMap;

    public IReadOnlyList<CharacterData> Characters => characters;

    public CharacterData GetById(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        BuildCacheIfNeeded();

        cachedMap.TryGetValue(characterId, out CharacterData result);
        return result;
    }

    private void BuildCacheIfNeeded()
    {
        if (cachedMap != null)
            return;

        cachedMap = new Dictionary<string, CharacterData>();

        foreach (CharacterData data in characters)
        {
            if (data == null)
                continue;

            if (string.IsNullOrWhiteSpace(data.CharacterId))
            {
                Debug.LogWarning("[CharacterDatabase] CharacterData has empty ID.");
                continue;
            }

            if (cachedMap.ContainsKey(data.CharacterId))
            {
                Debug.LogWarning($"[CharacterDatabase] Duplicate character ID: {data.CharacterId}");
                continue;
            }

            cachedMap.Add(data.CharacterId, data);
        }
    }
}