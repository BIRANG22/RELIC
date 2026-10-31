#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

public static class LocalizationWorkbookKeyRenamer
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    private static XDocument LoadEntry(ZipArchiveEntry entry)
    {
        if (entry == null) throw new InvalidOperationException("XLSX entry is missing.");
        using (Stream stream = entry.Open())
            return XDocument.Load(stream);
    }

    private static string WorksheetPath(ZipArchive zip)
    {
        var workbook = LoadEntry(zip.GetEntry("xl/workbook.xml"));
        var sheet = workbook.Descendants(Ns + "sheet")
            .FirstOrDefault(s => (string)s.Attribute("name") == "Text")
            ?? throw new InvalidOperationException("Text worksheet is missing.");
        string relationshipId = (string)sheet.Attribute(RelationshipNs + "id");
        var relationships = LoadEntry(zip.GetEntry("xl/_rels/workbook.xml.rels"));
        string target = (string)relationships.Descendants(PackageNs + "Relationship")
            .First(r => (string)r.Attribute("Id") == relationshipId).Attribute("Target");
        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("Text worksheet relationship target is missing.");
        string path = target.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/') : "xl/" + target.TrimStart('/');
        // Some workbooks express the target relative to xl/ with ./ segments.
        return path.Replace("xl/./", "xl/");
    }

    private static string[] ReadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        return entry == null ? Array.Empty<string>() : LoadEntry(entry)
            .Descendants(Ns + "si")
            .Select(si => string.Concat(si.Descendants(Ns + "t").Select(t => (string)t)))
            .ToArray();
    }

    private static string CellValue(XElement cell, string[] strings)
    {
        if ((string)cell.Attribute("t") == "s")
        {
            string indexText = (string)cell.Element(Ns + "v");
            return int.TryParse(indexText, out int index) && index >= 0 && index < strings.Length
                ? strings[index] : "";
        }
        var textNodes = cell.Element(Ns + "is")?.Descendants(Ns + "t");
        string inlineText = textNodes == null ? "" : string.Concat(textNodes.Select(t => (string)t));
        return inlineText.Length > 0 ? inlineText : (string)cell.Element(Ns + "v") ?? "";
    }

    private static string ColumnOf(XElement cell)
    {
        string address = (string)cell.Attribute("r") ?? "";
        return new string(address.TakeWhile(char.IsLetter).ToArray());
    }

    private static (List<XElement> rows, string keyColumn) GetRowsAndKeyColumn(XDocument sheet, string[] sharedStrings)
    {
        var rows = sheet.Descendants(Ns + "sheetData").Elements(Ns + "row").ToList();
        if (rows.Count == 0) throw new InvalidOperationException("Text worksheet has no rows.");
        var header = rows[0].Elements(Ns + "c")
            .FirstOrDefault(c => CellValue(c, sharedStrings) == "Key")
            ?? throw new InvalidOperationException("Key header is missing in Text worksheet.");
        return (rows, ColumnOf(header));
    }

    public static IReadOnlyList<string> ReadKeys(string path)
    {
        using (var zip = ZipFile.OpenRead(path))
        {
            var sheet = LoadEntry(zip.GetEntry(WorksheetPath(zip)));
            var strings = ReadSharedStrings(zip);
            var info = GetRowsAndKeyColumn(sheet, strings);
            return info.rows.Skip(1)
                .Select(row => row.Elements(Ns + "c")
                    .FirstOrDefault(c => ColumnOf(c) == info.keyColumn))
                .Where(c => c != null)
                .Select(c => CellValue(c, strings))
                .Where(key => !string.IsNullOrWhiteSpace(key)).ToList();
        }
    }

    public static void RenameKeys(string path, IDictionary<string, string> mapping)
    {
        if (mapping == null || mapping.Count == 0) return;
        if (!File.Exists(path)) throw new FileNotFoundException("Localization workbook was not found.", path);

        string temp = path + ".rename.tmp";
        if (File.Exists(temp)) File.Delete(temp);
        File.Copy(path, temp);
        try
        {
            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Update))
            {
                string sheetPath = WorksheetPath(zip);
                var part = zip.GetEntry(sheetPath)
                    ?? throw new InvalidOperationException("Missing worksheet: " + sheetPath);
                var document = LoadEntry(part); // Close read stream BEFORE deleting the entry.
                var strings = ReadSharedStrings(zip);
                var info = GetRowsAndKeyColumn(document, strings);

                int replaced = 0;
                foreach (var row in info.rows.Skip(1))
                {
                    var cell = row.Elements(Ns + "c")
                        .FirstOrDefault(c => ColumnOf(c) == info.keyColumn);
                    if (cell == null || !mapping.TryGetValue(CellValue(cell, strings), out string next))
                        continue;
                    cell.RemoveNodes();
                    cell.SetAttributeValue("t", "inlineStr");
                    cell.Add(new XElement(Ns + "is", new XElement(Ns + "t", next)));
                    replaced++;
                }
                if (replaced != mapping.Count)
                    throw new InvalidOperationException($"Workbook matched {replaced} of {mapping.Count} keys. Nothing was committed.");

                string entryName = part.FullName;
                part.Delete();
                using (Stream output = zip.CreateEntry(entryName, CompressionLevel.Optimal).Open())
                    document.Save(output);
            }

            // Validate the finished archive before replacing the original workbook.
            using (var check = ZipFile.OpenRead(temp))
            {
                if (check.GetEntry("xl/workbook.xml") == null || check.GetEntry("[Content_Types].xml") == null)
                    throw new InvalidOperationException("Temporary XLSX failed validation.");
            }
            File.Copy(temp, path, true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
#endif
