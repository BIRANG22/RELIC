using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GridEffectSpriteDatabase",
    menuName = "Relic/Data/Grid Effect Sprite Database"
)]
public class GridEffectSpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string gridEffectId;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Sprite> spriteMap;

    public void Initialize()
    {
        spriteMap = new Dictionary<string, Sprite>();

        foreach (Entry entry in entries)
        {
            if (entry == null)
                continue;

            string gridEffectId = entry.gridEffectId?.Trim();

            if (string.IsNullOrWhiteSpace(gridEffectId))
                continue;

            if (entry.sprite == null)
            {
                Debug.LogWarning($"[GridEffectSpriteDatabase] Sprite 없음: {gridEffectId}");
                continue;
            }

            if (spriteMap.ContainsKey(gridEffectId))
            {
                Debug.LogWarning($"[GridEffectSpriteDatabase] 중복 GridEffectID: {gridEffectId}");
                continue;
            }

            spriteMap.Add(gridEffectId, entry.sprite);
        }
    }

    public bool TryGetSprite(string gridEffectId, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrWhiteSpace(gridEffectId))
            return false;

        if (spriteMap == null)
            Initialize();

        return spriteMap.TryGetValue(gridEffectId.Trim(), out sprite);
    }
}
