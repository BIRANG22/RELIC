using System.IO;
using NUnit.Framework;

public sealed class UIBlurReplicaSourceTests
{
    [Test]
    public void Manager_UsesSeparateUiBlurTextureWithoutChangingWorldBlurFeature()
    {
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");
        string feature = File.ReadAllText("Assets/Project/Scripts/Rendering/UIBackgroundBlurRendererFeature.cs");

        Assert.That(manager, Does.Contain("_UIBlurUiTexture"));
        Assert.That(manager, Does.Contain("BlurReplicaRoot"));
        Assert.That(manager, Does.Contain("UIBlurCamera"));
        Assert.That(manager, Does.Contain("RenderTextureDescriptor"));
        Assert.That(manager, Does.Contain("depthBufferBits = 24"));
        Assert.That(manager, Does.Not.Contain("SetBlurred"));
        Assert.That(manager, Does.Not.Contain("SetSharp"));
        Assert.That(manager, Does.Not.Contain("CanvasState"));
        Assert.That(feature, Does.Contain("_UIBlurSourceTexture"));
        Assert.That(feature, Does.Contain("camera.targetTexture != null"));
    }

    [Test]
    public void ReplicaSource_DoesNotCreateInputOrMutateOriginalCanvasPresentation()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/UIBlurReplicaSource.cs");
        string manager = File.ReadAllText("Assets/Project/Scripts/UIBlurBackgroundManager.cs");

        Assert.That(source, Does.Not.Contain("GraphicRaycaster"));
        Assert.That(source, Does.Contain("raycastTarget = false"));
        Assert.That(source, Does.Contain("Setting_upper"));
        Assert.That(source, Does.Contain("CanvasRenderer"));
        Assert.That(source, Does.Contain(".cull"));
        Assert.That(source, Does.Contain("TMP_SubMeshUI"));
        Assert.That(source, Does.Contain("CanvasRendererCullState"));
        Assert.That(source, Does.Contain("RestoreOriginalRendering"));
        Assert.That(manager, Does.Not.Contain("canvas.renderMode = RenderMode.ScreenSpaceCamera"));
        Assert.That(manager, Does.Not.Contain("canvas.worldCamera ="));
        Assert.That(manager, Does.Not.Contain("canvas.sortingOrder ="));
        Assert.That(manager, Does.Not.Contain("canvas.enabled = false"));
    }

    [Test]
    public void ReplicaSource_MapsReplicaRectsFromScreenSpaceAndCopiesMasks()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/UIBlurReplicaSource.cs");

        Assert.That(source, Does.Contain("GetWorldCorners"));
        Assert.That(source, Does.Contain("WorldToScreenPoint"));
        Assert.That(source, Does.Contain("ScreenPointToLocalPointInRectangle"));
        Assert.That(source, Does.Contain("bool isRoot"));
        Assert.That(source, Does.Contain("SyncLocalLayout"));
        Assert.That(source, Does.Contain("replica.anchorMin = source.anchorMin"));
        Assert.That(source, Does.Contain("replica.offsetMin = source.offsetMin"));
        Assert.That(source, Does.Contain("replica.offsetMax = source.offsetMax"));
        Assert.That(source, Does.Contain("Mask"));
        Assert.That(source, Does.Contain("RectMask2D"));
        Assert.That(source, Does.Contain("showMaskGraphic"));
        Assert.That(source, Does.Contain("softness"));
    }

    [Test]
    public void BlurShader_CompositesUiTextureByAlphaWithoutLeakingTransparentBlack()
    {
        string shader = File.ReadAllText("Assets/Project/Shaders/DustiumBackgroundBlur.shader");

        Assert.That(shader, Does.Contain("float uiAlpha = saturate(ui.a)"));
        Assert.That(shader, Does.Contain("world.rgb = lerp(world.rgb, uiRgb, uiAlpha)"));
        Assert.That(shader, Does.Not.Contain("lerp(world.rgb, ui.rgb, ui.a)"));
    }

    [Test]
    public void ReplicaSource_UsesTransparentMaterialPathForAdditiveRenderTextureRawImages()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/UIBlurReplicaSource.cs");
        string additiveShader = File.ReadAllText("Assets/Project/Shaders/UIVfxRenderTextureAdditive.shader");

        Assert.That(additiveShader, Does.Contain("Blend One One"));
        Assert.That(additiveShader, Does.Contain("color.a = 1"));
        Assert.That(source, Does.Contain("source.texture is RenderTexture"));
        Assert.That(source, Does.Contain("VFX RenderTexture Additive"));
        Assert.That(source, Does.Contain("replica.material = null"));
    }

    [Test]
    public void RelicOfferButton_MarksBlurReplicaDirtyWhenVisibleStateChanges()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicOfferButtonUI.cs");

        Assert.That(source, Does.Contain("UIBlurBackgroundManager.MarkReplicaDirty();"));
        Assert.That(source, Does.Contain("rarityRingProxyImage.texture = rarityRingRenderTexture"));
    }
}
