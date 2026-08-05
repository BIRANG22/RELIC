using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMapPartyInfoPresenterTests
{
    [Test]
    public void Render_ShowsHpAndIconInPartyOrderAndHidesEmptySlot()
    {
        GameObject root = CreateCharacterInfoHierarchy();
        Sprite icon1 = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f), Vector2.zero);

        try
        {
            BattleMapPartyInfoPresenter presenter = root.AddComponent<BattleMapPartyInfoPresenter>();
            presenter.Render(new List<CharacterRuntimeData>
            {
                new() { CharacterId = "C1", CurrentHP = 35, MaxHP = 50 },
                null,
                new() { CharacterId = "C3", CurrentHP = 0, MaxHP = 80 }
            }, id => id == "C1" ? icon1 : null);

            Assert.That(root.transform.Find("Character1/HpInfo/Text (TMP)")
                .GetComponent<TMP_Text>().text, Is.EqualTo("35/50"));
            Assert.That(root.transform.Find("Character1/Icon").GetComponent<Image>().sprite,
                Is.SameAs(icon1));
            Assert.That(root.transform.Find("Character1/HpBar/Fill").GetComponent<Image>().fillAmount,
                Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(root.transform.Find("Character2").gameObject.activeSelf, Is.False);
            Assert.That(root.transform.Find("Character3").gameObject.activeSelf, Is.True);
            Assert.That(root.transform.Find("Character3/HpInfo/Text (TMP)")
                .GetComponent<TMP_Text>().text, Is.EqualTo("0/80"));
            Assert.That(root.transform.Find("Character3/HpBar/Fill").GetComponent<Image>().fillAmount,
                Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(icon1);
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateCharacterInfoHierarchy()
    {
        GameObject root = new("CharacterInfo");
        for (int i = 1; i <= 3; i++)
        {
            GameObject character = new($"Character{i}");
            character.transform.SetParent(root.transform);
            GameObject hpInfo = new("HpInfo");
            hpInfo.transform.SetParent(character.transform);
            GameObject text = new("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(hpInfo.transform);
            GameObject icon = new("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(character.transform);
            GameObject hpBar = new("HpBar", typeof(RectTransform), typeof(Image));
            hpBar.transform.SetParent(character.transform);
            GameObject fill = new("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(hpBar.transform);
        }

        return root;
    }
}
