using System.IO;
using NUnit.Framework;

public sealed class UIBackgroundBlurShaderTests
{
    private const string ShaderPath = "Assets/Project/Shaders/DustiumBackgroundBlur.shader";

    [Test]
    public void BlurSourceTexture_IsDeclaredAsAMaterialProperty()
    {
        string shader = File.ReadAllText(ShaderPath);

        Assert.That(shader, Does.Contain("_UIBlurSourceTexture (\"Blur Source\", 2D)"));
    }

    [Test]
    public void SharedBlurManager_UsesRendererFeatureSourceTextureOnly()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("UIBackgroundBlurRendererFeature.SourceTexture"));
        Assert.That(manager, Does.Not.Contain("UIBlurBackgroundCaptureManager"));
        Assert.That(manager, Does.Not.Contain("CaptureBackgroundNow"));
        Assert.That(manager, Does.Not.Contain("capturedTexture"));
        Assert.That(manager, Does.Not.Contain("canvas.enabled = false"));
    }

    [Test]
    public void SharedBlurManager_UsesBuildIncludedResourcesMaterialBeforeShaderFind()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");
        string material = File.ReadAllText("Assets/Project/Resources/UI/DustiumBackgroundBlur.mat");

        Assert.That(manager, Does.Contain("Resources.Load<Material>(MaterialResourcePath)"));
        Assert.That(manager, Does.Contain("Shader.Find(ShaderName)"));
        Assert.That(manager.IndexOf("Resources.Load<Material>(MaterialResourcePath)"),
            Is.LessThan(manager.IndexOf("Shader.Find(ShaderName)")));
        Assert.That(manager, Does.Contain("Possible build shader stripping"));
        Assert.That(material, Does.Contain("m_Shader: {fileID: 4800000, guid: ba44f26dad75d30409e27201c722a7e1, type: 3}"));
    }

    [Test]
    public void SharedBlurManager_LogsBuildTextureDiagnosticsOnce()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("textureDiagnosticsLogged"));
        Assert.That(manager, Does.Contain("_UIBlurSourceTexture is null when blur is requested"));
        Assert.That(manager, Does.Contain("_UIBlurUiTexture is not created when blur is requested"));
        Assert.That(manager, Does.Contain("SupportsRenderTextureFormat(RenderTextureFormat.ARGB32)"));
        Assert.That(manager, Does.Contain("DescribeRenderTexture(uiBlurTexture)"));
    }

    [Test]
    public void SharedBlurManager_UsesCurrentScreenSizeForUiBlurTexture()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("GetUiBlurTextureSize()"));
        Assert.That(manager, Does.Contain("Screen.width"));
        Assert.That(manager, Does.Contain("Screen.height"));
        Assert.That(manager, Does.Contain("uiBlurTexture.width == targetSize.x"));
        Assert.That(manager, Does.Contain("uiBlurTexture.height == targetSize.y"));
        Assert.That(manager, Does.Not.Contain("UIBlurTextureWidth"));
        Assert.That(manager, Does.Not.Contain("UIBlurTextureHeight"));
    }

    [Test]
    public void SharedBlurManager_CopiesSourceCanvasScalerForReplicaCanvas()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("SyncReplicaCanvasScaler(topRequester)"));
        Assert.That(manager, Does.Contain("FindSourceCanvasScaler(topRequester)"));
        Assert.That(manager, Does.Contain("replicaScaler.referenceResolution = sourceScaler.referenceResolution"));
        Assert.That(manager, Does.Contain("replicaScaler.screenMatchMode = sourceScaler.screenMatchMode"));
        Assert.That(manager, Does.Contain("replicaScaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight"));
    }

    [Test]
    public void SharedBlurManager_ScalesBlurRadiusByRenderHeightBeforeSendingToShader()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("ReferenceBlurHeight = 1080f"));
        Assert.That(manager, Does.Contain("GetResolutionScaledBlurRadius(requester.BlurRadius, sourceTexture)"));
        Assert.That(manager, Does.Contain("return blurRadius * (renderHeight / ReferenceBlurHeight)"));
        Assert.That(manager.IndexOf("if (sourceTexture != null && sourceTexture.height > 0)"),
            Is.LessThan(manager.IndexOf("if (uiBlurTexture != null && uiBlurTexture.height > 0)")));
        Assert.That(manager, Does.Not.Contain("requester.BlurRadius ="));
    }

    [Test]
    public void BlurReplicaSource_CopiesTmpMeshLayoutProperties()
    {
        string replicaSource = File.ReadAllText("Assets/Project/Scripts/UIBlurReplicaSource.cs");

        Assert.That(replicaSource, Does.Contain("replica.margin = source.margin"));
        Assert.That(replicaSource, Does.Contain("replica.characterSpacing = source.characterSpacing"));
        Assert.That(replicaSource, Does.Contain("replica.wordSpacing = source.wordSpacing"));
        Assert.That(replicaSource, Does.Contain("replica.lineSpacing = source.lineSpacing"));
        Assert.That(replicaSource, Does.Contain("replica.paragraphSpacing = source.paragraphSpacing"));
        Assert.That(replicaSource, Does.Contain("replica.richText = source.richText"));
        Assert.That(replicaSource, Does.Contain("replica.ForceMeshUpdate(true, true)"));
    }

    [Test]
    public void BlurBackground_SupportsAdditiveRuntimeRootsWithoutBreakingSetReplacement()
    {
        string blurBackground = File.ReadAllText("Assets/Project/Scripts/UIBlurBackground.cs");

        Assert.That(blurBackground, Does.Contain("public void SetRuntimeBlurredUiRoots"));
        Assert.That(blurBackground, Does.Contain("runtimeBlurredUiRoots.Clear();"));
        Assert.That(blurBackground, Does.Contain("public void AddRuntimeBlurredUiRoot(GameObject root)"));
        Assert.That(blurBackground, Does.Contain("private bool AddRuntimeBlurredUiRootInternal(GameObject root)"));
        Assert.That(blurBackground, Does.Contain("blurredUiRoots.Contains(root) || runtimeBlurredUiRoots.Contains(root)"));
    }

    [Test]
    public void LobbyMenu_AddsSettingButtonAndQuestRootsForMenuBlur()
    {
        string menu = File.ReadAllText("Assets/Project/Scripts/LobbyMenuController.cs");
        string quest = File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Lobby/Quest/LobbyQuestManager.cs");

        Assert.That(menu, Does.Contain("SettingButtonObjectName = \"SettingButton\""));
        Assert.That(menu, Does.Contain("ConfigureMenuBlurRoots();"));
        Assert.That(menu, Does.Contain("blurBackground.AddRuntimeBlurredUiRoot(settingButton);"));
        Assert.That(menu, Does.Contain("LobbyQuestManager.Instance?.ConfigureQuestPanelBlur(blurBackground);"));
        Assert.That(quest, Does.Contain("blurBackground.AddRuntimeBlurredUiRoot(questPanel.gameObject);"));
        Assert.That(quest, Does.Contain("UIBlurBackgroundManager.MarkReplicaDirty();"));
    }

    [Test]
    public void SettingWarning_StaysAboveSharedBlurAndSharpLobbyPanels()
    {
        string warning = File.ReadAllText("Assets/Project/Scripts/UI/Lobby/SettingWarningUI.cs");

        Assert.That(warning, Does.Contain("MinimumTopSortingOrder = 11000"));
        Assert.That(warning, Does.Contain("Mathf.Max(topSortingOrder, MinimumTopSortingOrder)"));
    }

    [Test]
    public void SharedBlurManager_DoesNotBlockInputAndPausesCameraWhileARequesterIsActive()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("public static bool IsInputBlocked"));
        Assert.That(manager, Does.Contain("CameraMouseParallaxController.BeginUiPanelPause();"));
        Assert.That(manager, Does.Contain("CameraMouseParallaxController.EndUiPanelPause();"));
        Assert.That(manager, Does.Contain("public static bool IsInputBlocked => false;"));
        Assert.That(manager, Does.Contain("sharedBackground.raycastTarget = false;"));
        Assert.That(manager, Does.Not.Contain("FindObjectsByType<Canvas>"));
        Assert.That(manager, Does.Not.Contain("AddComponent<Canvas>"));
        Assert.That(manager, Does.Not.Contain("AddComponent<GraphicRaycaster>"));
    }

    [Test]
    public void PanelButton_DoesNotUseSharedBlurModalStateForInputBlocking()
    {
        string panelButton = File.ReadAllText("Assets/Project/Scripts/UI/UIPanelButton.cs");

        Assert.That(panelButton, Does.Not.Contain("UIBlurBackgroundManager.IsInputBlocked"));
    }

    [Test]
    public void BlurPanelOpeners_DoNotOverridePresentationSortingForConfiguredBlurPanels()
    {
        string menu = File.ReadAllText("Assets/Project/Scripts/LobbyMenuController.cs");
        string erosion = File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyErosionMirrorButton.cs");
        string panelButton = File.ReadAllText("Assets/Project/Scripts/UI/UIPanelButton.cs");

        Assert.That(menu, Does.Not.Contain("ApplyCanvasSorting"));
        Assert.That(menu, Does.Not.Contain("AddComponent<Canvas>"));
        Assert.That(erosion, Does.Contain("panel.GetComponent<UIBlurBackground>() != null"));
        Assert.That(panelButton, Does.Contain("openedPanel.GetComponent<UIBlurBackground>() != null"));
    }

    [Test]
    public void PanelButton_AllowsMenuRootControlsWhileSharedBlurIsOpen()
    {
        string panelButton = File.ReadAllText("Assets/Project/Scripts/UI/UIPanelButton.cs");

        Assert.That(panelButton, Does.Contain("DefaultMenuRootObjectName = \"MenuRoot\""));
        Assert.That(panelButton, Does.Contain("IsInsideMenuRoot()"));
    }

    [Test]
    public void PanelButton_AllowsLobbyMenuRootControlsWhileSharedBlurIsOpen()
    {
        string panelButton = File.ReadAllText("Assets/Project/Scripts/UI/UIPanelButton.cs");

        Assert.That(panelButton, Does.Contain("DefaultLobbyMenuRootObjectName = \"Setting_upper\""));
        Assert.That(panelButton, Does.Contain("IsInsideLobbyMenuRoot()"));
    }
}
