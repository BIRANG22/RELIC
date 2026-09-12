using System.Collections.Generic;
using NUnit.Framework;

public class LocalizationWorkbookWriterTests
{
    [Test]
    public void ApplySourceUpdatesToRows_UpdatesKoreanPreservesIdAndClearsOtherTranslations()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = new List<IReadOnlyList<string>>
        {
            new[] { "Key", "Id", "Korean(ko)", "English(en)", "Japanese(ja)" },
            new[] { "data.rune.rune_51.effect_description", "42", "기존 효과", "Old effect", "古い効果" },
            new[] { "ui.record.title", "43", "기록서", "Record", "記録書" },
        };

        IReadOnlyList<IReadOnlyList<string>> updated = LocalizationWorkbookWriter.ApplySourceUpdatesToRows(
            rows,
            new[] { new LocalizationWorkbookEntry("data.rune.rune_51.effect_description", "변경된 효과") });

        Assert.That(updated[1], Is.EqualTo(new[]
        {
            "data.rune.rune_51.effect_description", "42", "변경된 효과", string.Empty, string.Empty,
        }));
        Assert.That(updated[2], Is.EqualTo(rows[2]));
    }

    [Test]
    public void RemoveEntriesFromRows_RemovesOnlyExplicitlyUnusedKeys()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = new List<IReadOnlyList<string>>
        {
            new[] { "Key", "Id", "Korean(ko)" },
            new[] { "ui.used", "1", "사용 중" },
            new[] { "ui.unused", "2", "미사용" },
        };

        IReadOnlyList<IReadOnlyList<string>> remaining = LocalizationWorkbookWriter.RemoveEntriesFromRows(
            rows,
            new[] { "ui.unused" });

        Assert.That(remaining, Is.EqualTo(new IReadOnlyList<string>[]
        {
            new[] { "Key", "Id", "Korean(ko)" },
            new[] { "ui.used", "1", "사용 중" },
        }));
    }
}
