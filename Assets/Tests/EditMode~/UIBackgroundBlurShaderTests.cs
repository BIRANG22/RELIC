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
    public void SharedBlurManager_ReappliesTheActiveRequesterSettingsEveryFrame()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("Apply(activeRequester);"));
    }

    [Test]
    public void SharedBlurManager_BlocksInputAndPausesCameraWhileARequesterIsActive()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(manager, Does.Contain("public static bool IsInputBlocked"));
        Assert.That(manager, Does.Contain("CameraMouseParallaxController.BeginUiPanelPause();"));
        Assert.That(manager, Does.Contain("CameraMouseParallaxController.EndUiPanelPause();"));
        Assert.That(manager, Does.Contain("sharedBackground.raycastTarget = true;"));
    }

    [Test]
    public void PanelButton_UsesSharedBlurModalStateForWorldClickBlocking()
    {
        string panelButton = File.ReadAllText("Assets/Project/Scripts/UI/UIPanelButton.cs");

        Assert.That(panelButton, Does.Contain("UIBlurBackgroundManager.IsInputBlocked"));
        Assert.That(panelButton, Does.Contain("IsRequesterPanelObject"));
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
