using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// Resources/Data/GameData를 읽어 각 데이터 시트 Dictionary로 변환합니다.
    /// .xlsx/.bytes 형식의 엑셀 워크북과, 하나의 GameData.csv 안에 '# SheetName' 섹션으로 나눈 CSV 형식을 모두 지원합니다.
    /// CSV 섹션은 0번 행 = 한글 설명, 1번 행 = 영어 필드명, 2번 행부터 데이터로 처리합니다.
    /// </summary>
    public static class ExcelWorkbookReader
    {
        public static Dictionary<string, List<Dictionary<string, string>>> Read(byte[] dataBytes)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
            if (dataBytes == null || dataBytes.Length == 0)
                return result;

            if (IsZipWorkbook(dataBytes))
                return ReadExcelWorkbook(dataBytes);

            return ReadSectionedCsv(dataBytes);
        }

        private static bool IsZipWorkbook(byte[] dataBytes)
        {
            return dataBytes.Length >= 4
                && dataBytes[0] == 0x50
                && dataBytes[1] == 0x4B
                && dataBytes[2] == 0x03
                && dataBytes[3] == 0x04;
        }

        private static Dictionary<string, List<Dictionary<string, string>>> ReadExcelWorkbook(byte[] excelBytes)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

            using var stream = new MemoryStream(excelBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var sharedStrings = ReadSharedStrings(archive);
            var workbookEntry = archive.GetEntry("xl/workbook.xml");
            var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");

            if (workbookEntry == null || relsEntry == null)
                return result;

            var workbook = XDocument.Load(workbookEntry.Open());
            var rels = XDocument.Load(relsEntry.Open());

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace pkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

            var relMap = rels.Descendants(pkgRelNs + "Relationship")
                .Where(x => x.Attribute("Id") != null && x.Attribute("Target") != null)
                .ToDictionary(
                    x => x.Attribute("Id")!.Value,
                    x => x.Attribute("Target")!.Value,
                    StringComparer.OrdinalIgnoreCase);

            var sheets = workbook.Descendants(ns + "sheet");
            foreach (var sheet in sheets)
            {
                var name = sheet.Attribute("name")?.Value;
                var relId = sheet.Attribute(relNs + "id")?.Value;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relId))
                    continue;

                if (!relMap.TryGetValue(relId, out var target))
                    continue;

                var normalized = NormalizeWorkbookRelationshipTarget(target);

                var sheetEntry = archive.GetEntry(normalized);
                if (sheetEntry == null)
                    continue;

                result[name] = ReadSheetRows(sheetEntry.Open(), sharedStrings);
            }

            return result;
        }

        private static string NormalizeWorkbookRelationshipTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return string.Empty;

            var normalized = target.Replace('\\', '/').Trim();

            if (normalized.StartsWith("/", StringComparison.Ordinal))
                return normalized.TrimStart('/');

            if (normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                return normalized;

            return "xl/" + normalized.TrimStart('/');
        }

        private static Dictionary<string, List<Dictionary<string, string>>> ReadSectionedCsv(byte[] csvBytes)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
            var text = DecodeCsvText(csvBytes);
            var sectionRows = new Dictionary<string, List<List<string>>>(StringComparer.OrdinalIgnoreCase);
            string currentSectionName = null;

            foreach (var values in ParseCsvRecords(text))
            {
                if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
                    continue;

                var firstValue = values[0]?.Trim() ?? string.Empty;
                if (firstValue.StartsWith("#", StringComparison.Ordinal))
                {
                    currentSectionName = firstValue.Substring(1).Trim();
                    if (!string.IsNullOrWhiteSpace(currentSectionName) && !sectionRows.ContainsKey(currentSectionName))
                        sectionRows[currentSectionName] = new List<List<string>>();

                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentSectionName))
                    continue;

                sectionRows[currentSectionName].Add(values);
            }

            foreach (var pair in sectionRows)
                result[pair.Key] = ConvertCsvRowsToDictionaries(pair.Value);

            return result;
        }

        private static List<List<string>> ParseCsvRecords(string text)
        {
            var records = new List<List<string>>();
            var values = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                if ((c == '\r' || c == '\n') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    values.Add(current.ToString());
                    current.Clear();
                    records.Add(values);
                    values = new List<string>();
                    continue;
                }

                if (c == '\r' && inQuotes)
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    current.Append('\n');
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0 || values.Count > 0)
            {
                values.Add(current.ToString());
                records.Add(values);
            }

            return records;
        }

        private static string DecodeCsvText(byte[] csvBytes)
        {
            if (csvBytes.Length >= 3 && csvBytes[0] == 0xEF && csvBytes[1] == 0xBB && csvBytes[2] == 0xBF)
                return Encoding.UTF8.GetString(csvBytes, 3, csvBytes.Length - 3);

            return Encoding.UTF8.GetString(csvBytes);
        }

        private static List<Dictionary<string, string>> ConvertCsvRowsToDictionaries(List<List<string>> csvRows)
        {
            var rows = new List<Dictionary<string, string>>();

            // 최소 2행은 있어야 함
            // 0번 행 = 한글 설명
            // 1번 행 = 영어 필드명
            if (csvRows == null || csvRows.Count < 2)
                return rows;

            var headerRowIndex = 1;
            var dataStartRowIndex = 2;
            var headers = csvRows[headerRowIndex];

            for (var i = dataStartRowIndex; i < csvRows.Count; i++)
            {
                var values = csvRows[i];
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (var h = 0; h < headers.Count; h++)
                {
                    var key = headers[h]?.Trim();
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    row[key] = h < values.Count ? values[h]?.Trim() ?? string.Empty : string.Empty;
                }

                var isEmptyRow = row.Values.All(v => string.IsNullOrWhiteSpace(v));
                if (!isEmptyRow)
                    rows.Add(row);
            }

            return rows;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            if (line == null)
                return values;

            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());
            return values;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var list = new List<string>();
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return list;

            var doc = XDocument.Load(entry.Open());
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            foreach (var si in doc.Descendants(ns + "si"))
            {
                var text = string.Concat(si.Descendants(ns + "t").Select(x => x.Value));
                list.Add(text);
            }

            return list;
        }

        private static List<Dictionary<string, string>> ReadSheetRows(Stream stream, List<string> sharedStrings)
        {
            var rows = new List<Dictionary<string, string>>();
            var doc = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            var rowNodes = doc.Descendants(ns + "row").ToList();

            // 최소 2행은 있어야 함
            // 0번 행 = 한글 설명
            // 1번 행 = 영어 필드명
            if (rowNodes.Count < 2)
                return rows;

            var headerRowIndex = 1;
            var dataStartRowIndex = 2;

            var headers = ReadCellValues(rowNodes[headerRowIndex], sharedStrings);

            for (var i = dataStartRowIndex; i < rowNodes.Count; i++)
            {
                var values = ReadCellValues(rowNodes[i], sharedStrings);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (var h = 0; h < headers.Count; h++)
                {
                    var key = headers[h]?.Trim();

                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    row[key] = h < values.Count ? values[h] : string.Empty;
                }

                bool isEmptyRow = row.Values.All(v => string.IsNullOrWhiteSpace(v));

                if (!isEmptyRow)
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        private static List<string> ReadCellValues(XElement rowNode, List<string> sharedStrings)
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var values = new List<string>();
            var nextColumnIndex = 0;

            foreach (var cell in rowNode.Elements(ns + "c"))
            {
                int columnIndex;
                if (!TryGetColumnIndex(cell.Attribute("r")?.Value, out columnIndex))
                    columnIndex = nextColumnIndex;

                while (values.Count < columnIndex)
                    values.Add(string.Empty);

                var type = cell.Attribute("t")?.Value;
                var valueNode = cell.Element(ns + "v");
                var inlineNode = cell.Element(ns + "is");
                var raw = valueNode?.Value ??
                    (inlineNode != null
                        ? string.Concat(inlineNode.Descendants(ns + "t").Select(x => x.Value))
                        : string.Empty);

                string value;

                if (type == "s" && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                    value = sharedStrings[sharedIndex];
                else
                    value = raw;

                if (values.Count == columnIndex)
                    values.Add(value);
                else
                    values[columnIndex] = value;

                nextColumnIndex = columnIndex + 1;
            }

            return values;
        }

        private static bool TryGetColumnIndex(string cellReference, out int columnIndex)
        {
            columnIndex = 0;

            if (string.IsNullOrWhiteSpace(cellReference))
                return false;

            var foundColumn = false;
            for (var i = 0; i < cellReference.Length; i++)
            {
                var c = char.ToUpperInvariant(cellReference[i]);
                if (c < 'A' || c > 'Z')
                    break;

                foundColumn = true;
                columnIndex = columnIndex * 26 + (c - 'A' + 1);
            }

            if (!foundColumn)
                return false;

            columnIndex -= 1;
            return true;
        }
    }
}
