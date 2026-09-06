using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Character Icon Database")]
    public class CharacterIconDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterIconEntry> entries = new();

        private Dictionary<string, CharacterIconEntry> map;

        public void Initialize()
        {
            map = new Dictionary<string, CharacterIconEntry>();

            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.CharacterId))
                    continue;

                map[entry.CharacterId] = entry;
            }
        }

        public bool TryGetIcon(string characterId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            icon = entry.Icon;
            return icon != null;
        }

        public bool TryGetTimelineIcon(string characterId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            icon = entry.TimelineIcon;

            if (icon == null)
                icon = entry.Icon;

            return icon != null;
        }

        public bool TryGetPortrait(string characterId, out Sprite portrait)
        {
            portrait = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            portrait = entry.Portrait;

            if (portrait == null)
                portrait = entry.Icon;

            return portrait != null;
        }

        public bool TryGetSideImage(string characterId, out Sprite sideImage)
        {
            sideImage = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            sideImage = entry.SideImage;
            return sideImage != null;
        }

        public bool TryGetHUDPortraitImage(string characterId, out Sprite portrait)
        {
            portrait = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            portrait = entry.HUDPortraitImage;

            if (portrait == null)
                portrait = entry.SideImage;

            return portrait != null;
        }

        public bool TryGetHUDSelectedPortraitImage(string characterId, out Sprite portrait)
        {
            portrait = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            portrait = entry.HUDSelectedPortraitImage;

            if (portrait == null)
                portrait = entry.HUDPortraitImage;

            if (portrait == null)
                portrait = entry.SideImage;

            return portrait != null;
        }

        [Obsolete("Use TryGetSideImage instead.")]
        public bool TryGetCharBackImage(string characterId, out Sprite charBackImage)
        {
            return TryGetSideImage(characterId, out charBackImage);
        }

        public bool TryGetMark(string characterId, out Sprite mark)
        {
            mark = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            mark = entry.Mark;
            return mark != null;
        }

        public bool TryGetMark2(string characterId, out Sprite mark2)
        {
            mark2 = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            mark2 = entry.Mark2;
            return mark2 != null;
        }
    }

    [Serializable]
    public class CharacterIconEntry
    {
        public string CharacterId;
        public Sprite Icon;
        public Sprite Portrait;
        public Sprite TimelineIcon;
        [FormerlySerializedAs("CharBackImage")]
        public Sprite SideImage;

        [Header("Battle HUD Portraits")]
        public Sprite HUDPortraitImage;
        public Sprite HUDSelectedPortraitImage;

        public Sprite Mark;
        public Sprite Mark2;
    }
}
