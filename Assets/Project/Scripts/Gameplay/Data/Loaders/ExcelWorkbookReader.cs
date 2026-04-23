using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;


/// <summary>
/// [Loaders] 스크립트. 역할/설정/변수 용도를 코드 주석으로 확인할 수 있도록 정리했습니다.
/// Unity 연결: MonoBehaviour 스크립트는 Scene/GameObject에 컴포넌트로 부착 후 Inspector 필드를 설정하세요.
/// 데이터 클래스는 엑셀 시트 컬럼과 필드명을 맞춰 DataBootstrap 로딩 파이프라인에서 자동 매핑됩니다.
/// </summary>
namespace Relic.Gameplay.Data
{
    /// <summary>
    /// ExcelWorkbookReader의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public static class ExcelWorkbookReader
    {
        public static Dictionary<string, List<Dictionary<string, string>>> Read(byte[] excelBytes)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
            if (excelBytes == null || excelBytes.Length == 0)
                return result;

            using var stream = new MemoryStream(excelBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var sharedStrings = ReadSharedStrings(archive);
            var workbook = XDocument.Load(archive.GetEntry("xl/workbook.xml")?.Open() ?? Stream.Null);
            var rels = XDocument.Load(archive.GetEntry("xl/_rels/workbook.xml.rels")?.Open() ?? Stream.Null);

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

                var normalized = target.Replace('\\', '/');
                if (!normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                    normalized = "xl/" + normalized.TrimStart('/');

                var sheetEntry = archive.GetEntry(normalized);
                if (sheetEntry == null)
                    continue;

                result[name] = ReadSheetRows(sheetEntry.Open(), sharedStrings);
            }

            return result;
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
            if (rowNodes.Count == 0)
                return rows;

            var headers = ReadCellValues(rowNodes[0], sharedStrings);
            for (var i = 1; i < rowNodes.Count; i++)
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

                if (row.Count > 0)
                    rows.Add(row);
            }

            return rows;
        }

        private static List<string> ReadCellValues(XElement rowNode, List<string> sharedStrings)
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var values = new List<string>();

            foreach (var cell in rowNode.Elements(ns + "c"))
            {
                var type = cell.Attribute("t")?.Value;
                var valueNode = cell.Element(ns + "v");
                var inlineNode = cell.Element(ns + "is")?.Element(ns + "t");
                var raw = valueNode?.Value ?? inlineNode?.Value ?? string.Empty;

                if (type == "s" && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                    values.Add(sharedStrings[sharedIndex]);
                else
                    values.Add(raw);
            }

            return values;
        }
    }
}
