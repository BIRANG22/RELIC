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

            Debug.Log($"[CharacterCsvLoader] rows={rows.Count}, validRows={validRows.Count}");

            var list = DataRowMapper.MapList<CharacterMasterData>(validRows);

            Debug.Log($"[CharacterCsvLoader] mapped count={list.Count}");

            return list;
        }
    }
}