using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 선택된 버튼의 테두리 Glow 이미지를 관리합니다.
/// 다른 Glow 버튼을 선택했을 때만 이전 Glow가 꺼지며,
/// 일반 UI 버튼을 눌러 EventSystem 포커스가 이동해도 선택된 Glow는 유지됩니다.
/// </summary>
[DisallowMultipleComponent]
public class SelectedButtonBorderGlow : MonoBehaviour,
    IPointerClickHandler,
    ISelectHandler
{
    [Header("Glow Target")]
    [Tooltip("선택 상태에서 표시할 빛나는 테두리 Image입니다.")]
    [SerializeField] private Image glowImage;

    [Tooltip("선택되지 않은 상태에서는 Glow Image 오브젝트를 비활성화합니다.")]
    [SerializeField] private bool hideWhenUnselected = true;

    [Header("Selection")]
    [Tooltip("버튼을 클릭하면 EventSystem의 선택 대상으로 설정합니다.")]
    [SerializeField] private bool selectOnClick = true;

    [Tooltip("같은 선택 그룹으로 사용할 부모입니다. 비워두면 이 오브젝트의 부모를 사용합니다.")]
    [SerializeField] private Transform selectionGroupRoot;

    [Header("Initial State")]
    [Tooltip("패널이 활성화될 때 이 버튼을 기본 선택 상태로 표시합니다. 기본 버튼에서만 체크하세요.")]
    [SerializeField] private bool selectedOnEnable;

    [SerializeField] private bool selected;

    [Header("Runtime Material Values")]
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField, Range(-5f, 5f)] private float flowSpeed = 0.5f;
    [SerializeField, Range(0.01f, 0.5f)] private float glowWidth = 0.5f;
    [SerializeField, Range(0f, 8f)] private float glowStrength = 4f;
    [SerializeField, Range(0f, 2f)] private float baseBrightness = 2f;

    [Header("TurnMark Line Mode")]
    [Tooltip("TurnMark에서는 선택 상태를 변경하지 않으며 GlowImage에 설정한 머티리얼과 값을 그대로 사용합니다.")]
    [SerializeField] private bool turnMarkLineMode;

    private bool IsTurnMarkLineMode => turnMarkLineMode ||
        (transform.name == "Line" && transform.parent != null && transform.parent.name == "TurnMark");

    private Material runtimeMaterial;

    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");
    private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
    private static readonly int BaseBrightnessId = Shader.PropertyToID("_BaseBrightness");

    private void Awake()
    {
        if (!IsTurnMarkLineMode)
            EnsureRuntimeMaterial();

        if (!IsTurnMarkLineMode)
            ApplyMaterialValues();
        ApplySelectedState();
    }

    private Coroutine initializeSelectionCoroutine;

    private void OnEnable()
    {
        if (!IsTurnMarkLineMode)
            EnsureRuntimeMaterial();

        if (!IsTurnMarkLineMode)
            ApplyMaterialValues();

        if (IsTurnMarkLineMode)
        {
            // BattleTimelineGroupUI가 Line 활성화를 관리합니다. 머티리얼은 변경하지 않습니다.
            if (glowImage != null)
                glowImage.enabled = true;
            return;
        }

        selected = false;
        ApplySelectedState();

        if (initializeSelectionCoroutine != null)
            StopCoroutine(initializeSelectionCoroutine);

        initializeSelectionCoroutine = StartCoroutine(InitializeSelectionNextFrame());
    }

    private IEnumerator InitializeSelectionNextFrame()
    {
        // CharacterSettingPanel이 켜진 첫 프레임에는 다른 선택 UI 초기화가 아직 끝나지 않을 수 있습니다.
        // 한 프레임 뒤 같은 선택 그룹의 기본 버튼을 찾아 현재 선택 상태와 Glow를 동기화합니다.
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
                if (group[i] != null && !group[i].IsTurnMarkLineMode && group[i].gameObject == currentObject)
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

        // 인스펙터에서 기본 선택으로 지정한 버튼을 가장 먼저 우선합니다.
        for (int i = 0; i < group.Length; i++)
        {
            SelectedButtonBorderGlow glow = group[i];
            if (glow != null && !glow.IsTurnMarkLineMode && glow.isActiveAndEnabled && glow.selectedOnEnable)
                return glow;
        }

        // 별도 지정이 없으면 Hierarchy에서 가장 앞의 활성 버튼을 기본 선택으로 사용합니다.
        SelectedButtonBorderGlow first = null;
        int firstSiblingIndex = int.MaxValue;

        for (int i = 0; i < group.Length; i++)
        {
            SelectedButtonBorderGlow glow = group[i];
            if (glow == null || glow.IsTurnMarkLineMode || !glow.isActiveAndEnabled)
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

        if (IsTurnMarkLineMode)
            return;

        selected = false;
        ApplySelectedState();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (!IsTurnMarkLineMode)
        {
            ApplyMaterialValues();
            ApplySelectedState();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsTurnMarkLineMode)
            return;

        if (!selectOnClick)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
        else
            SelectExclusive();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (IsTurnMarkLineMode)
            return;
        SelectExclusive();
    }

    // EventSystem의 Deselect는 탭 선택 해제를 의미하지 않습니다.
    // 다른 일반 버튼으로 포커스가 이동해도 현재 선택된 Glow는 유지합니다.
    // 다른 Glow 버튼이 선택될 때 SelectExclusive()가 이전 Glow를 해제합니다.

    public void SetSelected(bool value)
    {
        if (IsTurnMarkLineMode)
            return;

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
        if (IsTurnMarkLineMode)
            return;

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
        // TurnMark의 GlowImage는 Inspector에서 지정한 Glow_Material을
        // 그대로 사용합니다. 공용 머티리얼의 속성 또한 덮어쓰지 않습니다.
        if (IsTurnMarkLineMode || runtimeMaterial == null)
            return;

        if (runtimeMaterial.HasProperty(GlowColorId))
            runtimeMaterial.SetColor(GlowColorId, glowColor);
        if (runtimeMaterial.HasProperty(SpeedId))
            runtimeMaterial.SetFloat(SpeedId, flowSpeed);
        if (runtimeMaterial.HasProperty(GlowWidthId))
            runtimeMaterial.SetFloat(GlowWidthId, glowWidth);
        if (runtimeMaterial.HasProperty(GlowStrengthId))
            runtimeMaterial.SetFloat(GlowStrengthId, glowStrength);
        if (runtimeMaterial.HasProperty(BaseBrightnessId))
            runtimeMaterial.SetFloat(BaseBrightnessId, baseBrightness);
    }

    private void ApplySelectedState()
    {
        if (glowImage == null)
            return;

        // TurnMark 모드는 부모 Line의 활성화만 따릅니다.
        if (IsTurnMarkLineMode)
        {
            glowImage.enabled = true;
            return;
        }

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
