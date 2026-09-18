using NUnit.Framework;
using UnityEngine;

public sealed class WebGlVfxFallbackTests
{
    [TestCase(RuntimePlatform.WebGLPlayer, true)]
    [TestCase(RuntimePlatform.WindowsPlayer, false)]
    [TestCase(RuntimePlatform.WindowsEditor, false)]
    public void UsesFallback_SelectsOnlyTheWebGlPresentation(
        RuntimePlatform platform,
        bool expected)
    {
        Assert.That(WebGlVfxFallback.UsesFallback(platform), Is.EqualTo(expected));
    }
}
