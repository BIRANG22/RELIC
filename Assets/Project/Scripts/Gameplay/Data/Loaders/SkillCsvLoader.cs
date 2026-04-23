using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class SkillCsvLoader
    {
        public static List<PassiveSkillData> LoadPassive(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<PassiveSkillData>(ExcelSheetSelector.GetSheet(workbook, "PassiveSkillData", "PassiveSkill"));

        public static List<UniqueSkillData> LoadUnique(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<UniqueSkillData>(ExcelSheetSelector.GetSheet(workbook, "UniqueSkillData", "UniqueSkill"));

        public static List<CommonSkillData> LoadCommon(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<CommonSkillData>(ExcelSheetSelector.GetSheet(workbook, "CommonSkillData", "CommonSkill"));

        public static List<EssenceSkillData> LoadEssence(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<EssenceSkillData>(ExcelSheetSelector.GetSheet(workbook, "EssenceSkillData", "EssenceSkill"));

        public static List<SkillRangeData> LoadRange(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<SkillRangeData>(ExcelSheetSelector.GetSheet(workbook, "SkillRangeData", "SkillRange"));
    }
}
