using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class CompoundCsvLoader
    {
        public static List<CompoundData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "Compound");
            var list = DataRowMapper.MapList<CompoundData>(rows);

            foreach (CompoundData data in list)
            {
                if (data == null)
                    continue;

                data.CompoundId = data.CompoundId?.Trim();
                data.FragmentId = data.CompoundId;
                data.TargetType = data.TargetType?.Trim();
                data.MaterialId1 = data.MaterialId1?.Trim();
                data.MaterialId2 = data.MaterialId2?.Trim();
                data.MaterialId3 = data.MaterialId3?.Trim();
            }

            return list
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.CompoundId))
                .ToList();
        }
    }
}
