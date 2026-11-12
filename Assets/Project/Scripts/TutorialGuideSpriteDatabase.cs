using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assign preview sprites by tutorial IndexId and Page in a Unity asset.
/// All text and page ordering remain in the TutorialGuide worksheet.
/// </summary>
[CreateAssetMenu(fileName = "TutorialGuideSpriteDatabase", menuName = "Dustium/Tutorial Guide Sprite Database")]
public sealed class TutorialGuideSpriteDatabase : ScriptableObject
{
    [Serializable]
    public sealed class PageSprite
    {
        [Min(1)] public int page = 1;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class IndexSprites
    {
        public string indexId = "Index01";
        public List<PageSprite> pages = new List<PageSprite>();
    }

    [SerializeField] private List<IndexSprites> indexes = new List<IndexSprites>();

    public Sprite GetSprite(string indexId, int page)
    {
        if (string.IsNullOrWhiteSpace(indexId) || page < 1) return null;
        foreach (IndexSprites entry in indexes)
        {
            if (entry == null || !string.Equals(entry.indexId?.Trim(), indexId.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;
            if (entry.pages == null) return null;
            foreach (PageSprite item in entry.pages)
                if (item != null && item.page == page)
                    return item.sprite;
            return null;
        }
        return null;
    }
}
