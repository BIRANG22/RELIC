using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct BattleMapNodeInfoCopy
{
    public string Name { get; }
    public string Description { get; }
    public BattleMapNodeInfoCopy(string name, string description) { Name = name; Description = description; }
}

public class BattleMapNodeInfoPresenter : MonoBehaviour
{
    private const string DefaultName = "노드 정보";
    private const string DefaultDescription = "노드에 마우스를 올려 정보를 확인하세요.";

    [SerializeField] private TMP_Text nodeNameText;
    [SerializeField] private Image nodeIconImage;
    [SerializeField] private TMP_Text nodeInfoText;

    private void Awake()
    {
        ResolveReferences();
        DisableRaycastTargets();
        ResetToDefault();
    }

    private void OnEnable()
    {
        ResolveReferences();
        DisableRaycastTargets();
        ResetToDefault();
    }

    public void Show(GeneratedMapNodeData node, Sprite icon)
    {
        if (node == null) { ResetToDefault(); return; }

        ResolveReferences();
        BattleMapNodeInfoCopy copy = ResolveCopy(node.Type);
        if (nodeNameText != null) nodeNameText.text = copy.Name;
        if (nodeInfoText != null) nodeInfoText.text = copy.Description;
        if (nodeIconImage != null)
        {
            nodeIconImage.sprite = icon;
            nodeIconImage.enabled = icon != null;
            nodeIconImage.preserveAspect = true;
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void ResetToDefault()
    {
        ResolveReferences();
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (nodeNameText != null) nodeNameText.text = DefaultName;
        if (nodeInfoText != null) nodeInfoText.text = DefaultDescription;
        if (nodeIconImage != null)
        {
            nodeIconImage.sprite = null;
            nodeIconImage.enabled = false;
        }
    }

    public static BattleMapNodeInfoCopy ResolveCopy(string nodeType) => nodeType switch
    {
        "Start" => new("시작", "새로운 탐사를 시작하는 출발점입니다."),
        "Rest" => new("휴식", "상처를 회복하고 전열을 가다듬습니다."),
        "Special" => new("사건", "예측할 수 없는 사건과 마주칩니다."),
        "Common" => new("전투", "적을 물리치고 앞으로 나아갑니다."),
        "Elite" => new("정예", "강력한 적을 넘어 값진 보상을 노립니다."),
        "Boss" => new("보스", "탐사의 끝을 지키는 우두머리와 결전합니다."),
        _ => new(nodeType ?? string.Empty, string.Empty)
    };

    private void ResolveReferences()
    {
        if (nodeNameText == null) nodeNameText = transform.Find("Node_Name")?.GetComponent<TMP_Text>();
        if (nodeIconImage == null) nodeIconImage = transform.Find("Node_Icon")?.GetComponent<Image>();
        if (nodeInfoText == null) nodeInfoText = transform.Find("Node_Info")?.GetComponent<TMP_Text>();
    }

    private void DisableRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }
}
