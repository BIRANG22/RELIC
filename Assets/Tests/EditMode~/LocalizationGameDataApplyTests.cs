using System;
using System.IO;
using NUnit.Framework;

public sealed class LocalizationGameDataApplyTests
{
    [Test]
    public void ConvertOrThrow_ExportsLatestSkillValuesFromGameDataWorkbook()
    {
        string sourcePath = Path.Combine(
            Path.GetTempPath(),
            $"relic-gamedata-{Guid.NewGuid():N}.xlsx");
        string outputPath = Path.Combine(
            Path.GetTempPath(),
            $"relic-gamedata-{Guid.NewGuid():N}.csv");

        try
        {
            File.Copy("Assets/ExcelSource/GameData.xlsx", sourcePath);
            using FileStream openWorkbook = File.Open(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            ExcelToBytesConverter.ConvertOrThrow(
                sourcePath,
                outputPath);

            string[] matchingRows = Array.FindAll(
                File.ReadAllLines(outputPath),
                line => line.StartsWith("S_Unique_05,", StringComparison.Ordinal));
            Assert.That(matchingRows, Has.Length.EqualTo(1));

            string[] columns = matchingRows[0].Split(',');
            Assert.That(columns[9], Is.EqualTo("E_Grit;E_Charge"));
            Assert.That(columns[10], Is.EqualTo("1;2"));
            Assert.That(columns[15], Does.Contain("{ValueRate2}"));
        }
        finally
        {
            if (File.Exists(sourcePath))
                File.Delete(sourcePath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Test]
    public void ConvertOrThrow_WhenSourceIsMissing_PropagatesFailure()
    {
        string outputPath = Path.Combine(
            Path.GetTempPath(),
            $"relic-gamedata-{Guid.NewGuid():N}.csv");

        Assert.Throws<FileNotFoundException>(() =>
            ExcelToBytesConverter.ConvertOrThrow(
                "Assets/ExcelSource/does-not-exist.xlsx",
                outputPath));
    }

    [Test]
    public void ConvertOrThrow_WhenConversionFails_PreservesExistingRuntimeFile()
    {
        string sourcePath = Path.Combine(
            Path.GetTempPath(),
            $"relic-invalid-gamedata-{Guid.NewGuid():N}.xlsx");
        string outputPath = Path.Combine(
            Path.GetTempPath(),
            $"relic-existing-runtime-{Guid.NewGuid():N}.csv");

        try
        {
            File.WriteAllText(sourcePath, "not an xlsx workbook");
            File.WriteAllText(outputPath, "existing runtime data");

            Assert.Throws<InvalidOperationException>(() =>
                ExcelToBytesConverter.ConvertOrThrow(sourcePath, outputPath));
            Assert.That(File.ReadAllText(outputPath), Is.EqualTo("existing runtime data"));
        }
        finally
        {
            if (File.Exists(sourcePath))
                File.Delete(sourcePath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
