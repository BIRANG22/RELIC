using System;
using System.Collections.Generic;
using UnityEngine;

public static class TestVfxWorkbenchUtility
{
    public static int WrapIndex(int index, int count)
    {
        if (count <= 0)
            return 0;

        if (index < 0)
            return count - 1;

        if (index >= count)
            return 0;

        return index;
    }

    public static TEnum NextEnumValue<TEnum>(TEnum current)
        where TEnum : struct
    {
        return CycleEnumValue(current, 1);
    }

    public static TEnum PreviousEnumValue<TEnum>(TEnum current)
        where TEnum : struct
    {
        return CycleEnumValue(current, -1);
    }

    public static int RebuildFilteredLabelIndexes(
        IReadOnlyList<string> labels,
        string search,
        List<int> resultIndexes,
        int maxResultCount)
    {
        resultIndexes?.Clear();

        if (labels == null)
            return 0;

        string trimmedSearch = string.IsNullOrWhiteSpace(search)
            ? string.Empty
            : search.Trim();
        int visibleLimit = maxResultCount > 0 ? maxResultCount : int.MaxValue;
        int totalMatches = 0;

        for (int i = 0; i < labels.Count; i++)
        {
            if (!MatchesLabel(labels[i], trimmedSearch))
                continue;

            if (resultIndexes != null && resultIndexes.Count < visibleLimit)
                resultIndexes.Add(i);

            totalMatches++;
        }

        return totalMatches;
    }

    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null || layer < 0)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public static void ApplyDirectRendererSorting(
        GameObject vfx,
        string sortingLayerName,
        float sortingWorldY,
        float yMultiplier,
        int sortingOrderOffset)
    {
        if (vfx == null)
            return;

        Renderer[] renderers = vfx.GetComponentsInChildren<Renderer>(true);
        int baseOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(
            sortingWorldY,
            Mathf.Max(0.01f, yMultiplier),
            sortingOrderOffset);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            int prefabOrderOffset = renderer.sortingOrder;

            if (!string.IsNullOrWhiteSpace(sortingLayerName))
                renderer.sortingLayerName = sortingLayerName;

            renderer.sortingOrder = baseOrder + prefabOrderOffset;
        }
    }

    public static void RestartParticles(GameObject vfx)
    {
        if (vfx == null)
            return;

        ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }
    }

    public static void ApplyFlip(GameObject vfx, VfxFlipType flipType)
    {
        if (vfx == null)
            return;

        switch (flipType)
        {
            case VfxFlipType.None:
                break;

            case VfxFlipType.RotationY180:
                AddLocalRotationY(vfx.transform, 180f);
                break;

            case VfxFlipType.ParticleRendererFlipY:
                FlipParticleRendererY(vfx);
                break;
        }
    }

    public static void ApplyTransformOverrides(GameObject vfx, TestVfxSpawnSettings settings)
    {
        if (vfx == null || settings == null)
            return;

        vfx.transform.localRotation = Quaternion.Euler(settings.RotationEuler);
        Vector3 multiplier = settings.SafeScaleMultiplier();
        Vector3 scale = vfx.transform.localScale;
        vfx.transform.localScale = new Vector3(
            scale.x * multiplier.x,
            scale.y * multiplier.y,
            scale.z * multiplier.z);
    }

    private static void AddLocalRotationY(Transform target, float amount)
    {
        if (target == null)
            return;

        Vector3 euler = target.localEulerAngles;
        euler.y += amount;
        target.localEulerAngles = euler;
    }

    private static bool MatchesLabel(string label, string trimmedSearch)
    {
        if (string.IsNullOrEmpty(trimmedSearch))
            return true;

        return label != null &&
               label.IndexOf(trimmedSearch, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void FlipParticleRendererY(GameObject vfx)
    {
        ParticleSystemRenderer[] renderers =
            vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            ParticleSystemRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Vector3 flip = renderer.flip;
            flip.y = 1f - flip.y;
            renderer.flip = flip;
        }
    }

    private static TEnum CycleEnumValue<TEnum>(TEnum current, int direction)
        where TEnum : struct
    {
        Type enumType = typeof(TEnum);
        if (!enumType.IsEnum)
            return current;

        Array values = Enum.GetValues(enumType);
        if (values.Length == 0)
            return current;

        int currentIndex = Array.IndexOf(values, current);
        int nextIndex = WrapIndex(currentIndex + direction, values.Length);
        return (TEnum)values.GetValue(nextIndex);
    }
}
