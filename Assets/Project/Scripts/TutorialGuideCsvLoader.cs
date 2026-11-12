using System.Collections.Generic;
namespace Relic.Gameplay.Data
{
    public static class TutorialGuideCsvLoader
    {
        public static List<TutorialGuideData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            return DataRowMapper.MapList<TutorialGuideData>(ExcelSheetSelector.GetSheet(workbook, "TutorialGuide"));
        }
    }
}
