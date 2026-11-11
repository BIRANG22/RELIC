using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

public class RelicEffectTooltipTextTests
{
    private GameObject targetObject;

    [TearDown]
    public void TearDown()
    {
        if (targetObject != null)
            Object.DestroyImmediate(targetObject);

        RelicEffectTooltipFallbackUI fallback =
            Object.FindFirstObjectByType<RelicEffectTooltipFallbackUI>(FindObjectsInactive.Include);
        if (fallback != null)
            Object.DestroyImmediate(fallback.gameObject);
    }

    [Test]
    public void Set_DescriptionWithEffect_BindsFormattedTextAndHoverComponent()
    {
        targetObject = new GameObject("RelicDescription", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text text = targetObject.GetComponent<TMP_Text>();

        RelicDescriptionPresenter.Set(text, "{E_Vulnerable}을 부여한다.");

        Assert.That(text.text, Does.Contain("<link=\"effecticon:E_Vulnerable\"><u>"));
        Assert.That(targetObject.GetComponent<RelicEffectTooltipText>(), Is.Not.Null);
        Assert.That(text.richText, Is.True);
        Assert.That(text.raycastTarget, Is.True);
    }

    [Test]
    public void Set_PlainDescription_DoesNotRequireHoverComponent()
    {
        targetObject = new GameObject("RelicDescription", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text text = targetObject.GetComponent<TMP_Text>();

        RelicDescriptionPresenter.Set(text, "효과 토큰이 없는 설명");

        Assert.That(text.text, Is.EqualTo("효과 토큰이 없는 설명"));
        Assert.That(targetObject.GetComponent<RelicEffectTooltipText>(), Is.Null);
    }

    [Test]
    public void RefreshExistingBinding_SkillEffectText_DoesNotAddRelicHoverHandler()
    {
        targetObject = new GameObject("SkillDescription", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text text = targetObject.GetComponent<TMP_Text>();
        text.text = "<link=\"effecticon:E_Vulnerable\">취약</link>";

        RelicEffectTooltipAutoBinder.RefreshExistingBinding(text);

        Assert.That(targetObject.GetComponent<RelicEffectTooltipText>(), Is.Null);
    }

    [Test]
    public void GetOrCreate_WithoutSceneStatusTooltip_CreatesRelicEffectFallback()
    {
        RelicEffectTooltipFallbackUI fallback = RelicEffectTooltipFallbackUI.GetOrCreate();

        Assert.That(fallback, Is.Not.Null);
        Assert.That(fallback.GetComponentInParent<Canvas>(), Is.Not.Null);
    }

    [Test]
    public void Show_UsesHoveredRelicAnchorInsteadOfMouseEntryPosition()
    {
        targetObject = new GameObject("RelicDescription", typeof(RectTransform));
        RectTransform anchor = targetObject.GetComponent<RectTransform>();
        RelicEffectTooltipFallbackUI fallback = RelicEffectTooltipFallbackUI.GetOrCreate();
        var effects = new List<StatusEffectRuntimeData>
        {
            new("E_Vulnerable", 1)
        };

        fallback.Show(targetObject, effects, anchor, new Vector2(24f, 0f), 0f);
        RectTransform firstCard = fallback.transform.Find("Effect") as RectTransform;
        Vector2 firstPosition = firstCard.anchoredPosition;

        fallback.Show(targetObject, effects, anchor, new Vector2(24f, 0f), 0f);
        firstCard = fallback.transform.Find("Effect") as RectTransform;

        Assert.That(firstCard.anchoredPosition, Is.EqualTo(firstPosition));
    }
}
