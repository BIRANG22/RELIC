using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TimelineSkillHoverPresentationTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [TestCase(0, 2, 0.5f, 0.5f)]
    [TestCase(1, 2, 0.5f, 0.5f)]
    [TestCase(2, 2, 0.5f, 1f)]
    [TestCase(0, -1, 0.5f, 1f)]
    [TestCase(1, -1, 0.5f, 0.5f)]
    public void OwnerIconAlpha_HoveredOwnerTemporarilyBecomesOpaque(
        int queueIndex,
        int hoveredIndex,
        float queuedAlpha,
        float expected)
    {
        Assert.That(
            BattleTimelineGroupUI.ResolveOwnerIconAlpha(queueIndex, hoveredIndex, queuedAlpha),
            Is.EqualTo(expected));
    }

    [Test]
    public void OwnerIconDrawOrder_PutsHoveredOwnerLastAndKeepsOthersRelativeOrder()
    {
        Assert.That(
            BattleTimelineGroupUI.ResolveOwnerIconDrawOrder(4, 2),
            Is.EqualTo(new[] { 3, 1, 0, 2 }));
        Assert.That(
            BattleTimelineGroupUI.ResolveOwnerIconDrawOrder(4, -1),
            Is.EqualTo(new[] { 3, 2, 1, 0 }));
    }

    [Test]
    public void HoveredOwnerIndex_UsesOrderIndexForNonConsecutiveSameOwnerGroups()
    {
        string[] ownerKeys = { "player:A", "player:B", "player:A" };
        IReadOnlyList<int>[] orderGroups =
        {
            new[] { 0 },
            new[] { 1 },
            new[] { 2 }
        };

        Assert.That(
            BattleTimelineGroupUI.ResolveHoveredOwnerIconIndex(
                ownerKeys,
                orderGroups,
                "player:A",
                2),
            Is.EqualTo(2));
    }

    [Test]
    public void Popup_ShowMonsterEntry_BindsOnlyNameAndEffectDescription()
    {
        CreatePopupView(out TMP_Text nameText, out TMP_Text descriptionText, out Image rangeIcon);
        TimelineSkillHoverPopupUI popup = root.AddComponent<TimelineSkillHoverPopupUI>();
        BattleTimelinePreviewEntry entry = new()
        {
            IsMonster = true,
            MonsterSkillData = new MonsterSkillData
            {
                Name = "강습",
                EffectDesc = "적에게 피해를 줍니다.",
                TimelineNotation = TimelineActionType.Attack
            }
        };

        popup.Show(entry, null);

        Assert.That(nameText.text, Is.EqualTo("강습"));
        Assert.That(descriptionText.text, Is.EqualTo("적에게 피해를 줍니다."));
        Assert.That(rangeIcon.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void PopupView_Set_HidesLegacyRangeIcon()
    {
        TimelineSkillHoverPopupView view = CreatePopupView(
            out TMP_Text nameText,
            out TMP_Text descriptionText,
            out Image rangeIcon);
        Sprite sprite = Sprite.Create(
            new Texture2D(2, 2),
            new Rect(0, 0, 2, 2),
            new Vector2(0.5f, 0.5f));

        rangeIcon.sprite = sprite;
        view.Set("기억 이름", "행동 상세 설명");

        Assert.That(nameText.text, Is.EqualTo("기억 이름"));
        Assert.That(descriptionText.text, Is.EqualTo("행동 상세 설명"));
        Assert.That(descriptionText.gameObject.activeSelf, Is.True);
        Assert.That(rangeIcon.sprite, Is.Null);
        Assert.That(rangeIcon.gameObject.activeSelf, Is.False);

        Object.DestroyImmediate(sprite.texture);
        Object.DestroyImmediate(sprite);
    }

    [Test]
    public void PopupView_Set_HidesEmptyOptionalDetails()
    {
        TimelineSkillHoverPopupView view = CreatePopupView(
            out TMP_Text nameText,
            out TMP_Text descriptionText,
            out Image rangeIcon);

        view.Set("이동", "");

        Assert.That(nameText.text, Is.EqualTo("이동"));
        Assert.That(descriptionText.gameObject.activeSelf, Is.False);
        Assert.That(rangeIcon.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void PopupView_Set_UsesExistingNameTextAsDescriptionFallback()
    {
        root = new GameObject("Popup", typeof(RectTransform));
        TimelineSkillHoverPopupView view = root.AddComponent<TimelineSkillHoverPopupView>();
        GameObject nameObject = new GameObject("NameText", typeof(RectTransform), typeof(CanvasRenderer));
        nameObject.transform.SetParent(root.transform, false);
        TMP_Text nameText = nameObject.AddComponent<TextMeshProUGUI>();

        view.Set("기억 이름", "행동 상세 설명");

        Assert.That(nameText.text, Is.EqualTo("기억 이름\n행동 상세 설명"));
    }

    private TimelineSkillHoverPopupView CreatePopupView(
        out TMP_Text nameText,
        out TMP_Text descriptionText,
        out Image rangeIcon)
    {
        root = new GameObject("Popup", typeof(RectTransform));
        TimelineSkillHoverPopupView view = root.AddComponent<TimelineSkillHoverPopupView>();

        GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer));
        nameObject.transform.SetParent(root.transform, false);
        nameText = nameObject.AddComponent<TextMeshProUGUI>();

        GameObject descriptionObject = new GameObject("Description", typeof(RectTransform), typeof(CanvasRenderer));
        descriptionObject.transform.SetParent(root.transform, false);
        descriptionText = descriptionObject.AddComponent<TextMeshProUGUI>();

        GameObject rangeObject = new GameObject("RangeIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rangeObject.transform.SetParent(root.transform, false);
        rangeIcon = rangeObject.GetComponent<Image>();

        return view;
    }
}
