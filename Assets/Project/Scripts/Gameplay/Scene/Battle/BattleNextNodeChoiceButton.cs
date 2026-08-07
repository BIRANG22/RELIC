using System;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattleNextNodeChoiceButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [SerializeField] private AnimationClip eventClip;
    [SerializeField] private AnimationClip restClip;
    [SerializeField] private AnimationClip battleClip;
    [SerializeField] private AnimationClip eliteBattleClip;
    [SerializeField] private AnimationClip bossBattleClip;

    private int nodeIndex = -1;
    private Action<int> onSelected;

    private AnimatorOverrideController overrideController;
    private AnimationClip originalClip;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
            iconImage = transform.Find("NodeIcon")?.GetComponent<Image>();

        if (animator == null && iconImage != null)
            animator = iconImage.GetComponent<Animator>();

        SetupOverrideController();

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }
    }

    private void SetupOverrideController()
    {
        if (animator == null)
            return;

        RuntimeAnimatorController controller =
            animator.runtimeAnimatorController;

        if (controller == null)
            return;

        overrideController =
            new AnimatorOverrideController(controller);

        AnimationClip[] clips = overrideController.animationClips;

        if (clips == null || clips.Length == 0)
            return;

        originalClip = clips[0];

        animator.runtimeAnimatorController = overrideController;
    }

    public void ConfigureGeneratedUi(Image generatedIcon)
    {
        iconImage = generatedIcon;

        if (iconImage != null)
            animator = iconImage.GetComponent<Animator>();

        SetupOverrideController();
    }

    public void Bind(
        GeneratedMapNodeData node,
        Action<int> selectionCallback)
    {
        nodeIndex = node != null ? node.NodeIndex : -1;
        onSelected = selectionCallback;

        gameObject.SetActive(node != null);

        if (node == null)
            return;

        AnimationClip clip = GetNodeAnimationClip(node);

        PlayClip(clip);
    }

    private AnimationClip GetNodeAnimationClip(
        GeneratedMapNodeData node)
    {
        if (node == null)
            return null;

        return node.Type switch
        {
            "Special" => eventClip,
            "Rest" => restClip,
            "Common" => battleClip,
            "Elite" => eliteBattleClip,
            "Boss" => bossBattleClip,
            _ => null
        };
    }

    private void PlayClip(AnimationClip clip)
    {
        if (animator == null ||
            overrideController == null ||
            originalClip == null ||
            clip == null)
        {
            return;
        }

        overrideController[originalClip] = clip;

        animator.runtimeAnimatorController = overrideController;

        animator.Rebind();
        animator.Update(0f);

        animator.Play(0, 0, 0f);
    }

    public void Clear()
    {
        nodeIndex = -1;
        onSelected = null;

        gameObject.SetActive(false);
    }

    public void Select()
    {
        if (nodeIndex < 0 || UIPanelButton.IsMenuPanelOpen)
            return;

        onSelected?.Invoke(nodeIndex);
    }
}