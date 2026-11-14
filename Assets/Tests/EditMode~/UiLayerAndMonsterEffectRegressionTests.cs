using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public sealed class UiLayerAndMonsterEffectRegressionTests
{
    [Test]
    public void TutorialUiSortingPolicy_KeepsMenuAndModalAboveForegroundTooltip()
    {
        int tooltipOrder = TutorialFocusGuideRunner.GetForegroundTooltipSortingOrder(
            TutorialFocusGuideRunner.DefaultOverlaySortingOrder);
        int rewardTooltipOrder = TutorialFocusGuideRunner.GetForegroundTooltipSortingOrder(32500);

        Assert.That(tooltipOrder, Is.EqualTo(29930));
        Assert.That(rewardTooltipOrder, Is.EqualTo(32630));
        Assert.That(rewardTooltipOrder,
            Is.LessThan(BattleMenuEscapeInputController.DefaultTutorialMenuRootSortingOrder));
        Assert.That(tooltipOrder, Is.LessThan(BattleMenuEscapeInputController.DefaultTutorialMenuRootSortingOrder));
        Assert.That(BattleMenuEscapeInputController.DefaultTutorialMenuRootSortingOrder,
            Is.LessThan(BattleMenuEscapeInputController.DefaultTutorialMenuPanelSortingOrder));
        Assert.That(BattleMenuEscapeInputController.DefaultTutorialMenuPanelSortingOrder,
            Is.LessThan(BattleSettingPanelUI.GetModalSortingOrder(
                BattleMenuEscapeInputController.DefaultTutorialMenuPanelSortingOrder)));
    }

    [Test]
    public void MenuModalSortingPolicy_InheritsOpenMenuLayerAndSortsAboveMenu()
    {
        BattleMenuEscapeInputController.ModalSortingState state =
            BattleMenuEscapeInputController.ResolveModalSortingState(
                menuSortingLayerId: 17,
                menuSortingOrder: 32701,
                fallbackSortingLayerId: 0,
                fallbackSortingOrder: 30000,
                offset: 10);

        Assert.That(state.SortingLayerId, Is.EqualTo(17));
        Assert.That(state.SortingOrder, Is.EqualTo(32711));
    }

    [Test]
    public void OpenMenu_UsesRaycastBlockerWithoutDisablingBattleButtons()
    {
        GameObject root = new("MenuRoot", typeof(RectTransform), typeof(Canvas));
        GameObject blockerObject = new("Image", typeof(RectTransform), typeof(Image));
        GameObject menu = new("MenuPanel", typeof(RectTransform), typeof(Canvas));
        GameObject battleButtonObject = new("BattleButton", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            blockerObject.transform.SetParent(root.transform, false);
            menu.transform.SetParent(root.transform, false);
            menu.SetActive(true);

            BattleMenuEscapeInputController controller = root.AddComponent<BattleMenuEscapeInputController>();
            SetPrivateField(controller, "menuPanel", menu);
            SetPrivateField(controller, "menuRootTransform", root.transform);
            SetPrivateField(controller, "menuRootCanvas", root.GetComponent<Canvas>());
            SetPrivateField(controller, "menuRootBlockerGraphic", blockerObject.GetComponent<Image>());

            MethodInfo refresh = typeof(BattleMenuEscapeInputController).GetMethod(
                "RefreshPauseState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(refresh, Is.Not.Null);
            refresh.Invoke(controller, null);

            Assert.That(blockerObject.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(battleButtonObject.GetComponent<Button>().interactable, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(battleButtonObject);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ModalSortingPolicy_WithoutOpenMenuRestoresFallbackLayer()
    {
        GameObject modalObject = new("Modal", typeof(RectTransform), typeof(Canvas));
        try
        {
            Canvas modal = modalObject.GetComponent<Canvas>();
            modal.sortingLayerID = 17;
            modal.sortingOrder = 32711;

            BattleMenuEscapeInputController.ApplyModalSorting(
                modal,
                fallbackSortingLayerId: 0,
                fallbackSortingOrder: 30000,
                offset: 10);

            Assert.That(modal.sortingLayerID, Is.EqualTo(0));
            Assert.That(modal.sortingOrder, Is.EqualTo(30000));
        }
        finally
        {
            Object.DestroyImmediate(modalObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    [Test]
    public void TutorialForegroundRegistry_EnforcementDoesNotDiscoverNewTooltipRoots()
    {
        GameObject runnerRoot = new("Runner", typeof(TutorialFocusGuideRunner));
        GameObject registered = new("TooltipPanel", typeof(RectTransform));
        GameObject unregistered = null;
        try
        {
            TutorialFocusGuideRunner runner = runnerRoot.GetComponent<TutorialFocusGuideRunner>();
            runner.RefreshForegroundTooltipRegistry();
            Canvas registeredCanvas = registered.GetComponent<Canvas>();
            Assert.That(registeredCanvas, Is.Not.Null);

            registeredCanvas.sortingOrder = 0;
            unregistered = new GameObject("TooltipUI", typeof(RectTransform));

            runner.EnforceRegisteredForegroundTooltipLayers();

            Assert.That(registeredCanvas.sortingOrder, Is.EqualTo(29930));
            Assert.That(unregistered.GetComponent<Canvas>(), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(unregistered);
            Object.DestroyImmediate(registered);
            Object.DestroyImmediate(runnerRoot);
        }
    }

    [Test]
    public void TutorialFocusTooltipPolicy_RecognizesStatusAndGridEffectTooltips()
    {
        GameObject status = new("Status", typeof(RectTransform), typeof(UnitStatusEffectTooltipUI));
        GameObject grid = new("Grid", typeof(RectTransform), typeof(GridEffectTooltipUI));
        try
        {
            Assert.That(TutorialFocusGuideRunner.IsForegroundTooltip(status.transform), Is.True);
            Assert.That(TutorialFocusGuideRunner.IsForegroundTooltip(grid.transform), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(status);
            Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void RefreshBrightnessPolicy_ExcludesButtonTargetGraphic()
    {
        GameObject root = new("Refresh", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject child = new("Icon", typeof(RectTransform), typeof(Image));
        try
        {
            child.transform.SetParent(root.transform, false);
            Image target = root.GetComponent<Image>();
            Image icon = child.GetComponent<Image>();

            Assert.That(LobbyRelicRefreshButtonUI.ShouldApplyBrightness(icon, target), Is.True);
            Assert.That(LobbyRelicRefreshButtonUI.ShouldApplyBrightness(target, target), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RefreshButtonRestingColor_DoesNotUseSelectedColor()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.black;
        colors.highlightedColor = Color.gray;
        colors.selectedColor = Color.white;

        Assert.That(LobbyRelicRefreshButtonUI.GetRestingColor(colors, false), Is.EqualTo(Color.black));
        Assert.That(LobbyRelicRefreshButtonUI.GetRestingColor(colors, true), Is.EqualTo(Color.gray));
    }

    [Test]
    public void RefreshButtonSelectionColors_UsesNormalColorForSelectedState()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.black;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.red;

        ColorBlock normalized = LobbyRelicRefreshButtonUI.NormalizeSelectionColors(colors);

        Assert.That(normalized.selectedColor, Is.EqualTo(Color.black));
        Assert.That(normalized.pressedColor, Is.EqualTo(Color.white));
    }

    [TestCase("TutorialMode")]
    [TestCase("__TutorialFocusTooltipForeground")]
    [TestCase("__RelicTutorialDialogueOverlay")]
    public void MenuBlurCollector_IncludesTutorialPresentationRoots(string objectName)
    {
        Assert.That(BattleUIBlurRootCollector.IsTutorialPresentationRootName(objectName), Is.True);
    }

    [Test]
    public void MonsterInnateEffectDescription_UnderlinesReferencedStatusEffect()
    {
        EffectDatabase effects = new();
        effects.Initialize(new[]
        {
            new EffectMasterData { EffectId = "E_WebCreation", Name = "사냥실" },
            new EffectMasterData { EffectId = "E_Spider_Web", Name = "거미줄" }
        });

        string result = MonsterInnateEffectDescriptionFormatter.Format(
            "턴 시작 시 거미줄을 생성한다.", "E_WebCreation", effects, null);

        Assert.That(result, Does.Contain(
            "<link=\"effecticon:E_Spider_Web\"><u>거미줄</u></link>"));
    }

    [Test]
    public void MonsterInfoStatusDetail_UsesInnateEffectFormatter()
    {
        EffectMasterData webCreation = new()
        {
            EffectId = "E_WebCreation",
            Name = "사냥실",
            ToolTip = "턴 시작 시 거미줄을 생성한다."
        };
        EffectDatabase effects = new();
        effects.Initialize(new[]
        {
            webCreation,
            new EffectMasterData { EffectId = "E_Spider_Web", Name = "거미줄", Mark = 1 }
        });
        StatusEffectRuntimeData status = new("E_WebCreation", 1)
        {
            IsMonsterInnateTrait = true
        };

        string result = MonsterInfoStatusItemUI.FormatDetailText(status, webCreation, effects, null);

        Assert.That(result, Does.Contain("<u>거미줄</u>"));
    }

    [Test]
    public void MonsterInfoStatusDetail_FormatsReferencedEffectWithoutInnateFlag()
    {
        EffectMasterData webCreation = new()
        {
            EffectId = "E_WebCreation",
            Name = "사냥실",
            ToolTip = "턴 시작 시 거미줄을 생성한다."
        };
        EffectDatabase effects = new();
        effects.Initialize(new[]
        {
            webCreation,
            new EffectMasterData { EffectId = "E_Spider_Web", Name = "거미줄", Mark = 1 }
        });
        StatusEffectRuntimeData status = new("E_WebCreation", 1);

        string result = MonsterInfoStatusItemUI.FormatDetailText(status, webCreation, effects, null);

        Assert.That(result, Does.Contain("<u>거미줄</u>"));
    }

    [Test]
    public void MonsterInnateEffectDescription_UnderlinesReferencedGridEffect()
    {
        EffectDatabase effects = new();
        effects.Initialize(new[]
        {
            new EffectMasterData { EffectId = "E_Trail", Name = "흔적" }
        });
        GridEffectDatabase grids = new();
        grids.Initialize(new[]
        {
            new GridEffectData { GridEffectID = "GR_Residue", Name = "점액" }
        });

        string result = MonsterInnateEffectDescriptionFormatter.Format(
            "이동 시 기존 위치에 점액을 남긴다.", "E_Trail", effects, grids);

        Assert.That(result, Does.Contain("<u>점액</u>"));
    }
}
