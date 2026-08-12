using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class EventData
    {
        public string EventId;
        public string EventName;
        public string Title;

        public int ChoiceOrder;
        public string ChoiceName;
        public string ChoiceDesc;
        public string ChoiceType;
        public string SelectCondition;

        public string CostType;
        public string CostTarget;
        public string CostValue;
        public string SuccessCondition;

        public string ResultType;
        public string ResultTarget;
        public string ResultValue;
        public string SuccessRate;
        public string FailResult;
        public string NextEventId;
        public string SuccessVisualObjectId;
        public string SuccessVisualActionId;
        public string FailureVisualObjectId;
        public string FailureVisualActionId;
    }

    [Serializable]
    public class EventDefinition
    {
        public string EventId;
        public string EventName;
        public string Title;
        public List<EventData> Choices = new();
    }

    public static class EventIdUtility
    {
        private static readonly Regex CurrentPattern = new(
            @"^Event_?0*(\d+)(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LegacyPattern = new(
            @"^(?:EVT|EVENT)_?0*(\d+)(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Normalize(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return string.Empty;

            string trimmed = eventId.Trim();
            if (trimmed == "0")
                return string.Empty;

            if (TryNormalize(trimmed, CurrentPattern, out string current))
                return current;

            if (TryNormalize(trimmed, LegacyPattern, out string legacy))
                return legacy;

            return trimmed;
        }

        private static bool TryNormalize(string input, Regex pattern, out string normalized)
        {
            normalized = string.Empty;

            Match match = pattern.Match(input);
            if (!match.Success)
                return false;

            if (!int.TryParse(match.Groups[1].Value, out int number))
                return false;

            normalized = $"Event_{number:00}{NormalizeSuffix(match.Groups[2].Value)}";
            return true;
        }

        private static string NormalizeSuffix(string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
                return string.Empty;

            string trimmed = suffix.Trim();
            return trimmed.StartsWith("_", StringComparison.Ordinal)
                ? trimmed
                : "_" + trimmed;
        }
    }
}
