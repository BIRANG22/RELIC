using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class ReportedBugfixBatchTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void BattleEntry_AlwaysClaimsBgmOwnership(bool hasUnclearedCurrentNode)
    {
        Assert.That(BattleState.ShouldPlayDefaultBattleBgm(hasUnclearedCurrentNode), Is.True);
    }

    [TestCase(BgmState.Stage1TutorialBattle)]
    [TestCase(BgmState.ArabellaPhase2)]
    public void LongRunningBattleBgm_IsConfiguredToLoop(BgmState state)
    {
        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(
            "Assets/DB/SoundDatabase.asset");

        BgmData entry = database.BgmEntries.First(value => value.state == state);
        Assert.That(entry.mainClip.loop, Is.True, state.ToString());
    }

    [Test]
    public void MonsterStatusName_RemainsPlainAndDisplaysActualStack()
    {
        Assert.That(
            MonsterInfoStatusItemUI.FormatDisplayName("거미줄", "E_Spider_Web", 1, false),
            Is.EqualTo("거미줄 1"));
        Assert.That(
            MonsterInfoStatusItemUI.FormatDisplayName("점액", "E_Strike", 2, false),
            Is.EqualTo("점액 2"));
        Assert.That(
            MonsterInfoStatusItemUI.FormatDisplayName("특성", "E_Test", 1, true),
            Is.EqualTo("특성 ∞"));
    }

    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void WorldHudFollowPolicy_RequiresActiveLivingTarget(
        bool targetActive,
        bool runtimeAlive,
        bool expected)
    {
        Assert.That(WorldFollowHUD.ShouldDisplay(targetActive, runtimeAlive), Is.EqualTo(expected));
    }

    [Test]
    public void RefreshButton_ClearsLatchedClickVisualAfterRequest()
    {
        string source = System.IO.File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicRefreshButtonUI.cs");
        StringAssert.Contains("ResetClickVisualState", source);
        StringAssert.Contains("WaitForEndOfFrame", source);
        StringAssert.Contains("button.OnPointerExit", source);
    }

    [Test]
    public void TimelineMonsterHover_ShowsAndHidesTheSameMonsterHud()
    {
        string source = System.IO.File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineMonsterHoverTarget.cs");
        StringAssert.Contains("ShowTemporaryHUD", source);
        StringAssert.Contains("HideTemporaryHUD", source);
    }

    [Test]
    public void ArabellaIntro_PreservesCameraPositionDuringDialogueSequence()
    {
        string prefab = System.IO.File.ReadAllText(
            "Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab");
        int sequence = prefab.IndexOf("  sequenceSteps:", System.StringComparison.Ordinal);
        int end = prefab.IndexOf("  useBattleEffectSlide:", sequence, System.StringComparison.Ordinal);
        string block = prefab.Substring(sequence, end - sequence).Replace("\r\n", "\n");

        StringAssert.DoesNotContain("    action: 0\n", block);
        StringAssert.DoesNotContain("    action: 1\n", block);
    }

    [Test]
    public void ArabellaWebPresentation_DoesNotFlipTowardTemporaryWebTarget()
    {
        string source = System.IO.File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleStatusEffectService.cs");
        int method = source.IndexOf(
            "private static IEnumerator PlayArabellaWebCreationRoutine",
            System.StringComparison.Ordinal);
        int next = source.IndexOf(
            "private static IEnumerator PlayNocturnPortalPresentationsRoutine",
            method,
            System.StringComparison.Ordinal);
        string block = source.Substring(method, next - method);

        StringAssert.DoesNotContain("FaceByWorldTarget", block);
        StringAssert.DoesNotContain("UnityEngine.Random", block);
        StringAssert.Contains("BattleRandom.Range", block);
    }

    [Test]
    public void TimelineSkillPopup_RendersAboveTutorialFocusOverlay()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/PrefabsR/TimelinePopupPrefab.prefab");

        Assert.That(prefab, Is.Not.Null);
        TutorialForegroundTooltipCanvas sorting =
            prefab.GetComponent<TutorialForegroundTooltipCanvas>();
        Assert.That(sorting, Is.Not.Null);
        Assert.That(sorting.TutorialSortingOrder, Is.EqualTo(29930));
    }

    [Test]
    public void TutorialMenu_SortsAboveEveryTutorialLayer()
    {
        int rewardTooltipOrder =
            TutorialFocusGuideRunner.GetForegroundTooltipSortingOrder(32500);

        Assert.That(rewardTooltipOrder,
            Is.LessThan(BattleMenuEscapeInputController.DefaultTutorialMenuRootSortingOrder));
        Assert.That(BattleMenuEscapeInputController.DefaultTutorialMenuRootSortingOrder,
            Is.LessThan(BattleMenuEscapeInputController.DefaultTutorialMenuPanelSortingOrder));
    }

    [Test]
    public void SpiderWeb_StatusValueIsOneWhileMoveCostMultiplierRemainsTwo()
    {
        Assert.That(Relic.Gameplay.Battle.BattleGridEffectService.SpiderWebStatusStack, Is.EqualTo(1));
        Assert.That(BattleEquipmentEffectService.SpiderWebCostMultiplier, Is.EqualTo(2));
    }

    [Test]
    public void ActiveMonsterPanel_UnderlinesOnlyLinkedEffectsInSkillDescription()
    {
        const string linked = "턴 시작 시 <link=\"effecticon:E_Spider_Web\">거미줄</link>을 설치한다.";
        string formatted = BattleMonsterInfoPanelUI.FormatMonsterEffectDescription(linked, null);

        Assert.That(formatted, Does.Contain(
            "<link=\"effecticon:E_Spider_Web\"><u>거미줄</u></link>"));
    }
}
