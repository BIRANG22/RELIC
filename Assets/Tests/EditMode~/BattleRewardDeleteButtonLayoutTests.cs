using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

public sealed class BattleRewardDeleteButtonLayoutTests
{
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void ExtractContent_UsesExpandedSpacingForEnglishLabel()
    {
        string scene = File.ReadAllText(BattleScenePath);
        string iconRect = GetObjectBlock(scene, "224", "140502669391084342");
        string valueRect = GetObjectBlock(scene, "224", "2735283493251876970");

        Assert.That(iconRect, Does.Contain("m_AnchoredPosition: {x: 85, y: 0}"));
        Assert.That(valueRect, Does.Contain("m_AnchoredPosition: {x: 157, y: 0}"));
    }

    [Test]
    public void HoverDecorations_StayOutsideExpandedButtonContent()
    {
        string scene = File.ReadAllText(BattleScenePath);
        string deleteLeftDecoration = GetObjectBlock(scene, "224", "1401483366274886949");
        string deleteRightDecoration = GetObjectBlock(scene, "224", "645521735666116966");
        string confirmLeftDecoration = GetObjectBlock(scene, "224", "5574267222777024718");
        string confirmRightDecoration = GetObjectBlock(scene, "224", "1777111073961849848");
        string deleteHoverComponent = GetObjectBlock(scene, "114", "8513905844557255480");
        string confirmHoverComponent = GetObjectBlock(scene, "114", "8604865590445466366");

        Assert.That(deleteLeftDecoration, Does.Contain("m_AnchoredPosition: {x: -125, y: 0}"));
        Assert.That(deleteRightDecoration, Does.Contain("m_AnchoredPosition: {x: 125, y: 0}"));
        Assert.That(confirmLeftDecoration, Does.Contain("m_AnchoredPosition: {x: -125, y: 0}"));
        Assert.That(confirmRightDecoration, Does.Contain("m_AnchoredPosition: {x: 125, y: 0}"));

        foreach (string hoverComponent in new[] { deleteHoverComponent, confirmHoverComponent })
        {
            Assert.That(hoverComponent, Does.Contain("hoverAnchoredPosition: {x: -135, y: 0}"));
            Assert.That(hoverComponent, Does.Contain("hoverAnchoredPosition: {x: 135, y: 0}"));
        }
    }

    private static string GetObjectBlock(string yaml, string classId, string fileId)
    {
        Match match = Regex.Match(
            yaml,
            $@"(?ms)^--- !u!{classId} &{fileId}\r?\n.*?(?=^--- !u!|\z)");

        Assert.That(match.Success, Is.True, $"Serialized object {fileId} was not found.");
        return match.Value;
    }
}
