using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class ExcelWorkbookReaderTests
{
    [Test]
    public void ReadSectionedCsv_PreservesQuotedMultilineCell()
    {
        const string csv =
            "# Character\n" +
            "캐릭터 ID,소개\n" +
            "CharacterId,introduction\n" +
            "Char_01,\"첫째 줄\n둘째 \"\"강조\"\" 줄\"\n";

        var workbook = ExcelWorkbookReader.Read(Encoding.UTF8.GetBytes(csv));

        Assert.That(workbook["Character"], Has.Count.EqualTo(1));
        Assert.That(workbook["Character"][0]["CharacterId"], Is.EqualTo("Char_01"));
        Assert.That(workbook["Character"][0]["introduction"], Is.EqualTo("첫째 줄\n둘째 \"강조\" 줄"));
    }

    [Test]
    public void CharacterLoader_PreservesColumnsWhenXlsxDataRowOmitsMiddleCell()
    {
        var workbook = ExcelWorkbookReader.Read(BuildSparseCharacterWorkbook());

        var characters = CharacterCsvLoader.Load(workbook);

        Assert.That(characters, Has.Count.EqualTo(1));

        CharacterMasterData character = characters[0];
        Assert.That(character.CharacterId, Is.EqualTo("Char_Sparse"));
        Assert.That(character.MaxHP, Is.EqualTo(108));
        Assert.That(character.MaxCost, Is.EqualTo(8));
        Assert.That(character.CostRecovery, Is.EqualTo(4));
        Assert.That(character.MaxResource, Is.EqualTo(0));
        Assert.That(character.ResourceType, Is.EqualTo(ResourceType.Rage));
        Assert.That(character.ResourceTrigger, Is.EqualTo(ResourceTrigger.OnDamaged));
        Assert.That(character.MoveValue, Is.EqualTo(32));
    }

    private static byte[] BuildSparseCharacterWorkbook()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                "xl/workbook.xml",
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""
          xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Character"" sheetId=""1"" r:id=""rId1"" />
  </sheets>
</workbook>");

            WriteEntry(
                archive,
                "xl/_rels/workbook.xml.rels",
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1""
                Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet""
                Target=""/xl/worksheets/sheet1.xml"" />
</Relationships>");

            string sheetXml =
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <sheetData>" +
                Row(1,
                    CellData.For("A1", "Character ID"),
                    CellData.For("B1", "Name"),
                    CellData.For("C1", "Max HP"),
                    CellData.For("D1", "Max Cost"),
                    CellData.For("E1", "Cost Recovery"),
                    CellData.For("F1", "Max Resource"),
                    CellData.For("G1", "Resource Type"),
                    CellData.For("H1", "Resource Trigger"),
                    CellData.For("I1", "Move Value")) +
                Row(2,
                    CellData.For("A2", "CharacterId"),
                    CellData.For("B2", "Name"),
                    CellData.For("C2", "MaxHP"),
                    CellData.For("D2", "MaxCost"),
                    CellData.For("E2", "CostRecovery"),
                    CellData.For("F2", "MaxResource"),
                    CellData.For("G2", "ResourceType"),
                    CellData.For("H2", "ResourceTrigger"),
                    CellData.For("I2", "MoveValue")) +
                Row(3,
                    CellData.For("A3", "Char_Sparse"),
                    CellData.For("B3", "Sparse Character"),
                    CellData.For("C3", "108"),
                    CellData.For("D3", "8"),
                    CellData.For("E3", "4"),
                    CellData.For("G3", "Rage"),
                    CellData.For("H3", "OnDamaged"),
                    CellData.For("I3", "32")) +
                @"  </sheetData>
</worksheet>";

            WriteEntry(archive, "xl/worksheets/sheet1.xml", sheetXml);
        }

        return stream.ToArray();
    }

    private static string Row(int rowIndex, params CellData[] cells)
    {
        var builder = new StringBuilder();
        builder.Append("<row r=\"").Append(rowIndex).Append("\">");

        foreach (var cell in cells)
            builder.Append(Cell(cell.Reference, cell.Value));

        builder.Append("</row>");
        return builder.ToString();
    }

    private readonly struct CellData
    {
        public readonly string Reference;
        public readonly string Value;

        private CellData(string reference, string value)
        {
            Reference = reference;
            Value = value;
        }

        public static CellData For(string reference, string value)
        {
            return new CellData(reference, value);
        }
    }

    private static string Cell(string reference, string value)
    {
        return "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>" +
               SecurityElement.Escape(value) +
               "</t></is></c>";
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);

        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
