using NUnit.Framework;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMapNodeInfoPresenterTests
{
    [TestCase("Start", "시작", "새로운 탐사를 시작하는 출발점입니다.")]
    [TestCase("Rest", "휴식", "상처를 회복하고 전열을 가다듬습니다.")]
    [TestCase("Special", "사건", "예측할 수 없는 사건과 마주칩니다.")]
    [TestCase("Common", "전투", "적을 물리치고 앞으로 나아갑니다.")]
    [TestCase("Elite", "정예", "강력한 적을 넘어 값진 보상을 노립니다.")]
    [TestCase("Boss", "보스", "탐사의 끝을 지키는 우두머리와 결전합니다.")]
    public void ResolveCopy_ReturnsLocalizedNameAndOneLineDescription(
        string nodeType,
        string expectedName,
        string expectedDescription)
    {
        BattleMapNodeInfoCopy copy = BattleMapNodeInfoPresenter.ResolveCopy(nodeType);

        Assert.That(copy.Name, Is.EqualTo(expectedName));
        Assert.That(copy.Description, Is.EqualTo(expectedDescription));
    }

    [Test]
    public void ResetToDefault_KeepsPanelActiveAndShowsGuidance()
    {
        GameObject root = new("Node_Info");
        try
        {
            TMP_Text name = CreateText(root.transform, "Node_Name");
            Image icon = new GameObject("Node_Icon").AddComponent<Image>();
            icon.transform.SetParent(root.transform, false);
            TMP_Text info = CreateText(root.transform, "Node_Info");
            BattleMapNodeInfoPresenter presenter = root.AddComponent<BattleMapNodeInfoPresenter>();

            presenter.ResetToDefault();

            Assert.That(root.activeSelf, Is.True);
            Assert.That(name.text, Is.EqualTo("노드 정보"));
            Assert.That(info.text, Is.EqualTo("노드에 마우스를 올려 정보를 확인하세요."));
            Assert.That(icon.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void HoverExit_KeepsMostRecentlyDisplayedNodeInfo()
    {
        GameObject panelObject = new("MapPanel");
        try
        {
            GameObject root = new("Node_Info");
            root.transform.SetParent(panelObject.transform, false);
            TMP_Text name = CreateText(root.transform, "Node_Name");
            Image icon = new GameObject("Node_Icon").AddComponent<Image>();
            icon.transform.SetParent(root.transform, false);
            TMP_Text info = CreateText(root.transform, "Node_Info");
            root.AddComponent<BattleMapNodeInfoPresenter>();
            BattleMapPanel panel = panelObject.AddComponent<BattleMapPanel>();
            MethodInfo hovered = typeof(BattleMapPanel).GetMethod(
                "OnNodeHovered", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo exited = typeof(BattleMapPanel).GetMethod(
                "OnNodeHoverExited", BindingFlags.Instance | BindingFlags.NonPublic);

            hovered.Invoke(panel, new object[]
            {
                new Relic.Gameplay.Data.GeneratedMapNodeData { Type = "Boss" }, null
            });
            exited.Invoke(panel, null);

            Assert.That(name.text, Is.EqualTo("보스"));
            Assert.That(info.text, Is.EqualTo("탐사의 끝을 지키는 우두머리와 결전합니다."));
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    private static TMP_Text CreateText(Transform parent, string name)
    {
        TextMeshProUGUI text = new GameObject(name).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        return text;
    }
}
