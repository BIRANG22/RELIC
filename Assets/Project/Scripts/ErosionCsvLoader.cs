using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// GameData의 Erosion 시트를 ErosionData 목록으로 변환합니다.
    /// </summary>
    public static class ErosionCsvLoader
    {
        public static List<ErosionData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            return DataRowMapper.MapList<ErosionData>(
                ExcelSheetSelector.GetSheet(workbook, "Erosion")
            );
        }
    }
}
