using System.Collections.Generic;
using NUnit.Framework;

public class LocalizationExcelImporterTests
{
    private const string WorkbookPath = "Assets/ExcelSource/Localization.xlsx";

    [Test]
    public void Importer_UsesWorkbookAsSingleSourceOfTruth()
    {
        Assert.That(LocalizationExcelImporter.RemoveMissingEntries, Is.True);
    }

    [Test]
    public void ReadSheet_LoadsCurrentTextLocalizationRow()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");

        Assert.That(rows, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(rows[0], Is.EqualTo(new[]
        {
            "Key",
            "Id",
            "Korean(ko)",
            "English(en)",
            "Chinese (Simplified)(zh-Hans)",
            "Japanese(ja)",
            "Spanish(es)"
        }));
        Assert.That(rows[1], Is.EqualTo(new[]
        {
            "ui_start",
            "329720012800",
            "시작",
            "Start",
            "开始",
            "開始",
            "Inicio"
        }));
    }

    [Test]
    public void ToCsv_EscapesCommaQuoteAndLineBreak()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = new List<IReadOnlyList<string>>
        {
            new[] { "Key", "English(en)" },
            new[] { "ui_test", "Hello, \"Relic\"\nNext" }
        };

        string csv = LocalizationXlsxReader.ToCsv(rows);

        Assert.That(csv, Does.Contain("ui_test,\"Hello, \"\"Relic\"\"\nNext\""));
    }

    [Test]
    public void ToCsv_SkipsEmptyRowsAndPadsShortRowsToHeaderWidth()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = new List<IReadOnlyList<string>>
        {
            new[] { "Key", "Id", "ko", "en", "zh-Hans", "ja", "es" },
            new[] { "", "", "", "", "", "", "" },
            new[] { "ui_short", "0", "테스트" },
            new[] { "", "" }
        };

        string csv = LocalizationXlsxReader.ToCsv(rows);

        Assert.That(
            csv.Replace("\r\n", "\n"),
            Is.EqualTo("Key,Id,ko,en,zh-Hans,ja,es\nui_short,0,테스트,,,,"));
    }

    [Test]
    public void ValidateHeaders_RejectsWorkbookWithoutKeyColumn()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = new List<IReadOnlyList<string>>
        {
            new[] { "Id", "English(en)" }
        };

        Assert.That(
            () => LocalizationXlsxReader.ValidateHeaders(rows),
            Throws.ArgumentException.With.Message.Contains("Key"));
    }
}
