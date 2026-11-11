using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Relic.Gameplay.Data
{
    public static class RelicDescriptionFormatter
    {
        private static readonly Regex EffectLinkContentRegex = new(
            @"(<link\s*=\s*""effecticon:[^""]+""[^>]*>)(?!<u>)(.*?)(</link>)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public static string Format(string source)
        {
            return Format(source, null);
        }

        public static string Format(string source, string relicEffectIds)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source ?? string.Empty;

            string linked = SkillDescriptionFormatter.ReplaceEffectTokens(source);
            linked = EffectLinkContentRegex.Replace(linked, "$1<u>$2</u>$3");
            linked = Regex.Replace(linked, @"</link><space=[^>]+>", "</link>");

            foreach (string effectId in ResolveTooltipEffectIds(relicEffectIds))
            {
                if (linked.Contains($"effecticon:{effectId}", StringComparison.OrdinalIgnoreCase))
                    continue;

                DataManager manager = DataManager.Instance;
                if (manager?.EffectDatabase == null ||
                    !manager.EffectDatabase.TryGet(effectId, out EffectMasterData effect) || effect == null)
                    continue;

                string displayName = GameDataLocalization.EffectName(effect);
                if (string.IsNullOrWhiteSpace(displayName) || !linked.Contains(displayName))
                    continue;

                string tagged = $"<link=\"effecticon:{effectId}\"><u>{displayName}</u></link>";
                linked = linked.Replace(displayName, tagged);
            }

            return linked;
        }

        public static List<string> ExtractEffectIds(string formattedDescription)
        {
            return SkillDescriptionFormatter.ExtractLinkedEffectIds(formattedDescription);
        }

        public static List<string> ResolveTooltipEffectIds(string relicEffectIds)
        {
            var result = new List<string>();
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(relicEffectIds))
                return result;

            var mappings = new (string Marker, string StatusId)[]
            {
                ("Vulnerable", "E_Vulnerable"),
                ("Weaken", "E_Weaken"),
                ("Poison", "E_Poison"),
                ("Bleed", "E_Bleed"),
                ("Armor", "E_Armor"),
                ("Smite", "E_Smite"),
                ("Boost", "E_Boost"),
                ("Swift", "E_Swift"),
                ("Lifesteal", "E_Lifesteal")
            };

            foreach (string rawId in relicEffectIds.Split(';'))
            {
                string id = rawId.Trim();
                if (id.Length == 0)
                    continue;
                foreach ((string marker, string statusId) in mappings)
                {
                    if (id.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0 || !added.Add(statusId))
                        continue;
                    result.Add(statusId);
                    break;
                }
            }
            return result;
        }
    }
}
