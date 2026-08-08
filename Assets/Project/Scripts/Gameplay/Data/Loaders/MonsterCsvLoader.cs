using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class MonsterCsvLoader
    {
        public static List<MonsterMasterData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "MonsterMasterData", "MonsterMaster", "Monster");
            ApplyColumnAliases(rows);
            return DataRowMapper.MapList<MonsterMasterData>(rows);
        }

        private static void ApplyColumnAliases(IReadOnlyList<Dictionary<string, string>> rows)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];

                if (row == null)
                    continue;

                DataColumnAliasUtility.CopyAlias(row, "HP", "HP", "Health");
                DataColumnAliasUtility.CopyAlias(row, "MinRemnant", "MinRemnant", "RemnantMin", "MinimumRemnant", "\uCD5C\uC18C\uC794\uC7AC", "\uC794\uC7AC\uCD5C\uC18C");
                DataColumnAliasUtility.CopyAlias(row, "MaxRemnant", "MaxRemnant", "RemnantMax", "MaximumRemnant", "\uCD5C\uB300\uC794\uC7AC", "\uC794\uC7AC\uCD5C\uB300");
                DataColumnAliasUtility.CopyAlias(row, "UniqueItemId", "UniqueItemId", "UniqueItemID", "UniqueItem", "FixedItemId", "FixedItem", "\uACE0\uC720\uC544\uC774\uD15C", "\uACE0\uC720\uC544\uC774\uD15CID", "\uACE0\uC720\uC544\uC774\uD15CId");
                DataColumnAliasUtility.CopyAlias(row, "UniqueItemChance", "UniqueItemChance", "UniqueItemRate", "UniqueItemProbability", "ItemChance", "ItemRate", "\uACE0\uC720\uC544\uC774\uD15C\uD655\uB960", "\uC544\uC774\uD15C\uD655\uB960");
                DataColumnAliasUtility.CopyAlias(row, "RelicChance", "RelicChance", "RelicRate", "RelicProbability", "\uC720\uBB3C\uD655\uB960");
                DataColumnAliasUtility.CopyAlias(row, "AttackRangeId", "AttackRangeId", "AttackRange", "\uACF5\uACA9\uBC94\uC704");
                DataColumnAliasUtility.CopyAlias(row, "SpecialAction1", "SpecialAction1", "\uD2B9\uC218\uD589\uB3D91");
                DataColumnAliasUtility.CopyAlias(row, "SpecialAction2", "SpecialAction2", "\uD2B9\uC218\uD589\uB3D92");
            }
        }
    }
}
