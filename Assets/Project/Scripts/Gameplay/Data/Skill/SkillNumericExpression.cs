using System;
using System.Globalization;

namespace Relic.Gameplay.Data
{
    public readonly struct SkillNumericExpression
    {
        public int UnitValue { get; }
        public bool UsesX { get; }

        private SkillNumericExpression(int unitValue, bool usesX)
        {
            UnitValue = unitValue;
            UsesX = usesX;
        }

        public static bool TryParse(string source, out SkillNumericExpression expression)
        {
            expression = default;
            if (string.IsNullOrWhiteSpace(source))
                return false;

            string normalized = source.Trim();
            bool usesX = normalized.EndsWith("X", StringComparison.OrdinalIgnoreCase);
            string numberText = usesX
                ? normalized.Substring(0, normalized.Length - 1).Trim()
                : normalized;
            if (usesX && string.IsNullOrEmpty(numberText))
                numberText = "1";
            if (!int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return false;
            if ((usesX && value <= 0) || (!usesX && value < 0))
                return false;

            expression = new SkillNumericExpression(value, usesX);
            return true;
        }

        public int ResolveMaximumX(int availableResource)
        {
            return UsesX && UnitValue > 0 ? Math.Max(0, availableResource) / UnitValue : 0;
        }

        public int Resolve(int availableResource, int resolvedX)
        {
            return UsesX ? UnitValue * Math.Max(0, resolvedX) : UnitValue;
        }
    }
}
