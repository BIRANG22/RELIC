using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 선택된 버튼의 테두리 이미지에 빛이 순환하는 머티리얼을 적용합니다.
/// 버튼이 EventSystem에서 선택되거나 기본 선택 버튼으로 지정되면 Glow를 켜고,
/// 다른 버튼으로 선택이 이동하면 자동으로 끕니다.
/// </summary>
[DisallowMultipleComponent]
public class SelectedButtonBorderGlow : MonoBehaviour,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Glow Target")]
    [Tooltip("선택 상태에서 빛이 도는 테두리 Image입니다.")]
    [SerializeField] private Image glowImage;

    [Tooltip("선택되지 않은 상태에서는 Glow Image 오브젝트를 비활성화합니다.")]
    [SerializeField] private bool hideWhenUnselected = true;

    [Header("Selection")]
    [Tooltip("버튼을 클릭하면 EventSystem의 선택 대상으로 지정합니다.")]
    [SerializeField] private bool selectOnClick = true;

    [Tooltip("같은 선택 그룹으로 사용할 부모입니다. 비워두면 이 오브젝트의 부모를 사용합니다.")]
    [SerializeField] private Transform selectionGroupRoot;

    [Header("Initial State")]
    [Tooltip("패널이 활성화될 때 이 버튼을 기본 선택 상태로 표시합니다. 프리셋 버튼에만 체크하세요.")]
    [SerializeField] private bool selectedOnEnable;

    [SerializeField] private bool selected;

    [Header("Runtime Material Values")]
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField, Range(-5f, 5f)] private float flowSpeed = 0.5f;
    [SerializeField, Range(0.01f, 0.5f)] private float glowWidth = 0.5f;
    [SerializeField, Range(0f, 8f)] private float glowStrength = 4f;
    [SerializeField, Range(0f, 2f)] private float baseBrightness = 2f;

    private Material runtimeMaterial;

    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");
    private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
    private static readonly int BaseBrightnessId = Shader.PropertyToID("_BaseBrightness");

    private void Awake()
    {
        EnsureRuntimeMaterial();
        ApplyMaterialValues();
        ApplySelectedState();
    }

    private Coroutine initializeSelectionCoroutine;

    private void OnEnable()
    {
        EnsureRuntimeMaterial();
        ApplyMaterialValues();

        selected = false;
        ApplySelectedState();

        if (initializeSelectionCoroutine != null)
            StopCoroutine(initializeSelectionCoroutine);

        initializeSelectionCoroutine = StartCoroutine(InitializeSelectionNextFrame());
    }

    private IEnumerator InitializeSelectionNextFrame()
    {
        // CharacterSettingPanel이 켜지는 프레임에는 기존 선택 UI 초기화가 아직 끝나지 않을 수 있습니다.
        // 한 프레임 뒤 같은 그룹의 기본 버튼을 찾아 실제 선택 상태와 Glow를 맞춥니다.
        yield return null;

        initializeSelectionCoroutine = null;

        if (!isActiveAndEnabled)
            yield break;

        Transform root = selectionGroupRoot != null ? selectionGroupRoot : transform.parent;
        if (root == null)
        {
            SelectExclusive();
            yield break;
        }

        SelectedButtonBorderGlow[] group =
            root.GetComponentsInChildren<SelectedButtonBorderGlow>(true);

        SelectedButtonBorderGlow current = null;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            GameObject currentObject = EventSystem.current.currentSelectedGameObject;
            for (int i = 0; i < group.Length; i++)
            {
                if (group[i] != null && group[i].gameObject == currentObject)
                {
                    current = group[i];
                    break;
                }
            }
        }

        SelectedButtonBorderGlow defaultGlow = FindDefaultGlow(group);
        SelectedButtonBorderGlow target = current != null ? current : defaultGlow;

        if (target == null)
            yield break;

        if (target != this)
        {
            SetSelected(false);
            yield break;
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);

        SelectExclusive();
    }

    private static SelectedButtonBorderGlow FindDefaultGlow(SelectedButtonBorderGlow[] group)
    {
        if (group == null || group.Length == 0)
            return null;

        // 명시적으로 기본 선택을 지정한 버튼이 있으면 가장 우선합니다.
        for (int i = 0; i < group.Length; i++)
        {
            SelectedButtonBorderGlow glow = group[i];
            if (glow != null && glow.isActiveAndEnabled && glow.selectedOnEnable)
                return glow;
        }

        // 별도 지정이 없으면 Hierarchy상 가장 앞의 활성 버튼을 기본 선택으로 사용합니다.
        SelectedButtonBorderGlow first = null;
        int firstSiblingIndex = int.MaxValue;

        for (int i = 0; i < group.Length; i++)
        {
            SelectedButtonBorderGlow glow = group[i];
            if (glow == null || !glow.isActiveAndEnabled)
                continue;

            int siblingIndex = glow.transform.GetSiblingIndex();
            if (first == null || siblingIndex < firstSiblingIndex)
            {
                first = glow;
                firstSiblingIndex = siblingIndex;
            }
        }

        return first;
    }

    private void OnDisable()
    {
        if (initializeSelectionCoroutine != null)
        {
            StopCoroutine(initializeSelectionCoroutine);
            initializeSelectionCoroutine = null;
        }

        selected = false;
        ApplySelectedState();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        ApplyMaterialValues();
        ApplySelectedState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!selectOnClick)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
        else
            SelectExclusive();
    }

    public void OnSelect(BaseEventData eventData)
    {
        SelectExclusive();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetSelected(false);
    }

    public void SetSelected(bool value)
    {
        selected = value;
        ApplySelectedState();
    }

    public void Select()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
        else
            SelectExclusive();
    }

    public void Deselect()
    {
        SetSelected(false);
    }

    private void SelectExclusive()
    {
        Transform root = selectionGroupRoot != null ? selectionGroupRoot : transform.parent;

        if (root != null)
        {
            SelectedButtonBorderGlow[] group =
                root.GetComponentsInChildren<SelectedButtonBorderGlow>(true);

            for (int i = 0; i < group.Length; i++)
            {
                SelectedButtonBorderGlow glow = group[i];
                if (glow != null && glow != this)
                    glow.SetSelected(false);
            }
        }

        SetSelected(true);
    }

    private void EnsureRuntimeMaterial()
    {
        if (glowImage == null || runtimeMaterial != null)
            return;

        Material source = glowImage.material;
        if (source == null)
            return;

        runtimeMaterial = new Material(source)
        {
            name = source.name + " (Runtime)"
        };

        glowImage.material = runtimeMaterial;
    }

    private void ApplyMaterialValues()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetColor(GlowColorId, glowColor);
        runtimeMaterial.SetFloat(SpeedId, flowSpeed);
        runtimeMaterial.SetFloat(GlowWidthId, glowWidth);
        runtimeMaterial.SetFloat(GlowStrengthId, glowStrength);
        runtimeMaterial.SetFloat(BaseBrightnessId, baseBrightness);
    }

    private void ApplySelectedState()
    {
        if (glowImage == null)
            return;

        if (hideWhenUnselected)
        {
            if (glowImage.gameObject.activeSelf != selected)
                glowImage.gameObject.SetActive(selected);
        }
        else
        {
            glowImage.enabled = selected;
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeMaterial);
        else
            DestroyImmediate(runtimeMaterial);
    }
}
