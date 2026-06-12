using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class CharacterCsvLoader
    {
        public static List<CharacterMasterData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "CharacterData", "Character");

            var validRows = rows
                .Where(row =>
                    row.TryGetValue("CharacterId", out var id) &&
                    !string.IsNullOrWhiteSpace(id))
                .ToList();


            var list = DataRowMapper.MapList<CharacterMasterData>(validRows);

            for (int i = 0; i < list.Count; i++)
            {
                CharacterMasterData c = list[i];

                Debug.Log(
                    $"[CharacterCsvLoader] {c.CharacterId} / " +
                    $"Passive1:{c.PassiveSkill1} / " +
                    $"Unique1:{c.UniqueSkill1} / " +
                    $"Character1:{c.CharacterSkill1} / " +
                    $"Character2:{c.CharacterSkill2} / " +
                    $"Common1:{c.CommonSkill1} / " +
                    $"Common2:{c.CommonSkill2}"
                );
            }

            return list;
        }
    }
}