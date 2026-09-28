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
        if (nodeNameText != null) nodeNameText.text = GameLocalization.Get("battle.node_info");
        if (nodeInfoText != null) nodeInfoText.text = GameLocalization.Get("battle.node_hover_hint");
        if (nodeIconImage != null)
        {
            nodeIconImage.sprite = null;
            nodeIconImage.enabled = false;
        }
    }

    public static BattleMapNodeInfoCopy ResolveCopy(string nodeType) => nodeType switch
    {
        "Start" => new(GameLocalization.Get("ui_start"), GameLocalization.Get("battle.node_start_description")),
        "Rest" => new(GameLocalization.Get("lobby.rest"), GameLocalization.Get("battle.node_rest_description")),
        "Special" => new(GameLocalization.Get("battle.event"), GameLocalization.Get("battle.node_event_description")),
        "Common" => new(GameLocalization.Get("battle.battle"), GameLocalization.Get("battle.node_common_description")),
        "Elite" => new(GameLocalization.Get("battle.elite"), GameLocalization.Get("battle.node_elite_description")),
        "Boss" => new(GameLocalization.Get("battle.boss"), GameLocalization.Get("battle.node_boss_description")),
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
