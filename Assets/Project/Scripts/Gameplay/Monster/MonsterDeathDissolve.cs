using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Monster
{
    public sealed class MonsterDeathDissolve : MonoBehaviour
    {
        private static readonly int DissolveProgressId = Shader.PropertyToID("_Dissolve");

        [Header("Dissolve Exclusions")]
        [Tooltip("사망 연출 시 디졸브하지 않고 즉시 비활성화할 자식 오브젝트입니다. 나머지 SpriteRenderer는 기존 디졸브를 사용합니다.")]
        [SerializeField] private GameObject[] objectsToDisable;

        [Header("Death Dissolve")]
        [SerializeField, Min(0f)] private float duration = 0.4f;
        [SerializeField] private Material dissolveMaterial;

        private readonly List<Material> runtimeMaterials = new();
        private Coroutine routine;

        public float Duration => Mathf.Max(0f, duration);

        public void PlayAfter(float delay)
        {
            if (!isActiveAndEnabled || routine != null)
                return;

            routine = StartCoroutine(PlayRoutine(Mathf.Max(0f, delay)));
        }

        private IEnumerator PlayRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            // 먼저 제외 대상으로 지정한 자식 오브젝트만 즉시 숨깁니다.
            // 나머지는 기존과 동일하게 디졸브합니다.
            DisableConfiguredObjects();

            if (Duration <= 0f || !TryApplyDissolveMaterials())
            {
                routine = null;
                yield break;
            }

            float elapsed = 0f;
            SetDissolveProgress(0.5f);
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                SetDissolveProgress(Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(elapsed / Duration)));
                yield return null;
            }

            SetDissolveProgress(1f);
            routine = null;
        }

        private void DisableConfiguredObjects()
        {
            if (objectsToDisable == null)
                return;

            foreach (GameObject target in objectsToDisable)
            {
                if (target == null)
                    continue;

                // 부모 자체를 끄면 이 컴포넌트의 코루틴도 중단되므로 허용하지 않습니다.
                if (target == gameObject || transform.IsChildOf(target.transform))
                {
                    Debug.LogWarning("[MonsterDeathDissolve] 자기 자신 또는 상위 오브젝트는 비활성화 대상에 지정할 수 없습니다.", this);
                    continue;
                }

                target.SetActive(false);
            }
        }

        private bool IsExcludedRenderer(SpriteRenderer renderer)
        {
            if (objectsToDisable == null || renderer == null)
                return false;

            foreach (GameObject target in objectsToDisable)
            {
                if (target != null &&
                    (renderer.gameObject == target || renderer.transform.IsChildOf(target.transform)))
                    return true;
            }

            return false;
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
                if (renderer == null || IsExcludedRenderer(renderer))
                    continue;

                Material runtimeMaterial = new(dissolveMaterial);
                runtimeMaterial.SetFloat(DissolveProgressId, 0.5f);
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
