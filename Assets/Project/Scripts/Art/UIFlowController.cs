using UnityEngine;

public class UIFlowController : MonoBehaviour
{
    [Header("Flow 값을 변경할 머티리얼")]
    [SerializeField] private Material targetMaterial;

    [Header("활성화 여부를 확인할 오브젝트")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("셰이더 Flow 프로퍼티 이름")]
    [SerializeField] private string flowPropertyName = "_noiseflow";

    private Vector4 defaultFlow;
    private bool hasDefaultFlow;

    private void Awake()
    {
        CacheDefaultFlow();
    }

    private void OnEnable()
    {
        if (!hasDefaultFlow)
            CacheDefaultFlow();

        ApplyFlow();
    }

    private void Update()
    {
        ApplyFlow();
    }

    private void OnDisable()
    {
        RestoreDefaultFlow();
    }

    private void OnDestroy()
    {
        RestoreDefaultFlow();
    }

    private void CacheDefaultFlow()
    {
        hasDefaultFlow = false;

        if (targetMaterial == null)
            return;

        if (!targetMaterial.HasProperty(flowPropertyName))
        {
            Debug.LogWarning(
                $"[{nameof(UIFlowController)}] 머티리얼 '{targetMaterial.name}'에 '{flowPropertyName}' 프로퍼티가 없습니다.",
                this
            );
            return;
        }

        defaultFlow = targetMaterial.GetVector(flowPropertyName);
        hasDefaultFlow = true;
    }

    private void ApplyFlow()
    {
        if (targetMaterial == null || !hasDefaultFlow)
            return;

        bool anyObjectActive = false;

        if (targetObjects != null)
        {
            foreach (GameObject targetObject in targetObjects)
            {
                if (targetObject != null && targetObject.activeInHierarchy)
                {
                    anyObjectActive = true;
                    break;
                }
            }
        }

        targetMaterial.SetVector(
            flowPropertyName,
            anyObjectActive ? Vector4.zero : defaultFlow
        );
    }

    private void RestoreDefaultFlow()
    {
        if (targetMaterial == null || !hasDefaultFlow)
            return;

        targetMaterial.SetVector(flowPropertyName, defaultFlow);
    }
}
