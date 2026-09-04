using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class UIBackgroundBlurRendererFeature : ScriptableRendererFeature
{
    private const string SourceTextureName = "_UIBlurSourceTexture";
    private UIBackgroundBlurPass pass;
    public static Texture SourceTexture { get; private set; }

    public override void Create() => pass = new UIBackgroundBlurPass();

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
        SourceTexture = null;
    }

    private sealed class UIBackgroundBlurPass : ScriptableRenderPass
    {
        private sealed class GlobalTexturePassData { }
        private static readonly int SourceTextureId = Shader.PropertyToID(SourceTextureName);
        private RTHandle sourceTexture;

        public UIBackgroundBlurPass() => renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

#pragma warning disable 618, 672
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor descriptor)
        {
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref sourceTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: SourceTextureName);
            SourceTexture = sourceTexture;
        }

        [System.Obsolete("Compatibility mode only. RenderGraph uses RecordRenderGraph.")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(SourceTextureName);
            Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, sourceTexture);
            cmd.SetGlobalTexture(SourceTextureId, sourceTexture.nameID);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore 618, 672

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData camera = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor descriptor = camera.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref sourceTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: SourceTextureName);
            SourceTexture = sourceTexture;
            TextureHandle input = resources.activeColorTexture;
            TextureHandle output = graph.ImportTexture(sourceTexture);
            if (input.IsValid() && output.IsValid())
            {
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(input, output, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0), SourceTextureName);
                resources.cameraColor = output;
                using IRasterRenderGraphBuilder builder = graph.AddRasterRenderPass<GlobalTexturePassData>("Set UI Blur Source", out _);
                builder.UseTexture(output, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(output, SourceTextureId);
                builder.SetRenderFunc(static (GlobalTexturePassData _, RasterGraphContext _) => { });
            }
        }

        public void Dispose() => sourceTexture?.Release();
    }
}
