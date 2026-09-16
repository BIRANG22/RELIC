using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public sealed class MonsterDeathDissolve : MonoBehaviour
    {
        private static readonly int DissolveProgressId = Shader.PropertyToID("_DissolveProgress");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");

        [Header("Death Dissolve")]
        [SerializeField, Min(0f)] private float duration = 0.4f;
        [SerializeField] private Material dissolveMaterial;
        [SerializeField, ColorUsage(true, true)] private Color edgeColor = new(0.5f, 0.15f, 1f, 1f);
        [SerializeField, Range(0.001f, 0.25f)] private float edgeWidth = 0.06f;
        [SerializeField, Range(1f, 64f)] private float noiseScale = 16f;

        private readonly List<Material> runtimeMaterials = new();
        private Coroutine routine;

        public float Duration => Mathf.Max(0f, duration);

        public void PlayAfter(float delay)
        {
            if (!isActiveAndEnabled || routine != null || Duration <= 0f)
                return;

            routine = StartCoroutine(PlayRoutine(Mathf.Max(0f, delay)));
        }

        private IEnumerator PlayRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (!TryApplyDissolveMaterials())
            {
                routine = null;
                yield break;
            }

            float elapsed = 0f;
            SetDissolveProgress(0f);
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                SetDissolveProgress(Mathf.Clamp01(elapsed / Duration));
                yield return null;
            }

            SetDissolveProgress(1f);
            routine = null;
        }

        private bool TryApplyDissolveMaterials()
        {
            if (dissolveMaterial == null)
                return false;

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length <= 0)
                return false;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Material runtimeMaterial = new(dissolveMaterial);
                runtimeMaterial.SetFloat(DissolveProgressId, 0f);
                runtimeMaterial.SetColor(EdgeColorId, edgeColor);
                runtimeMaterial.SetFloat(EdgeWidthId, edgeWidth);
                runtimeMaterial.SetFloat(NoiseScaleId, noiseScale);
                renderer.material = runtimeMaterial;
                runtimeMaterials.Add(runtimeMaterial);
            }

            return runtimeMaterials.Count > 0;
        }

        private void SetDissolveProgress(float progress)
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                    runtimeMaterials[i].SetFloat(DissolveProgressId, progress);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                    Destroy(runtimeMaterials[i]);
            }

            runtimeMaterials.Clear();
        }
    }
}
