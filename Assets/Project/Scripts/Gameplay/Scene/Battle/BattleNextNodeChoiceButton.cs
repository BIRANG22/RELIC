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
    private AnimationClip pendingClip;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
            iconImage = transform.Find("NodeIcon")?.GetComponent<Image>();

        if (animator == null && iconImage != null)
            animator = iconImage.GetComponent<Animator>();

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }
    }

    private void OnEnable()
    {
        TryPlayPendingClip();
    }

    private void Update()
    {
        if (pendingClip != null)
            TryPlayPendingClip();
    }

    private bool EnsureOverrideController()
    {
        if (animator == null)
            return false;

        if (!animator.isActiveAndEnabled || !animator.gameObject.activeInHierarchy)
            return false;

        if (overrideController != null && originalClip != null)
        {
            if (animator.runtimeAnimatorController != overrideController)
                animator.runtimeAnimatorController = overrideController;

            return true;
        }

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null)
            return false;

        overrideController = new AnimatorOverrideController(controller);

        AnimationClip[] clips = overrideController.animationClips;
        if (clips == null || clips.Length == 0)
        {
            overrideController = null;
            return false;
        }

        originalClip = clips[0];
        animator.runtimeAnimatorController = overrideController;
        return true;
    }

    public void ConfigureGeneratedUi(Image generatedIcon)
    {
        iconImage = generatedIcon;
        animator = iconImage != null ? iconImage.GetComponent<Animator>() : null;

        overrideController = null;
        originalClip = null;

        TryPlayPendingClip();
    }

    public void Bind(
        GeneratedMapNodeData node,
        Action<int> selectionCallback)
    {
        nodeIndex = node != null ? node.NodeIndex : -1;
        onSelected = selectionCallback;

        gameObject.SetActive(node != null);

        if (node == null)
        {
            pendingClip = null;
            return;
        }

        pendingClip = GetNodeAnimationClip(node);
        TryPlayPendingClip();
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

    private void TryPlayPendingClip()
    {
        if (pendingClip == null || animator == null)
            return;

        if (!animator.isActiveAndEnabled || !animator.gameObject.activeInHierarchy)
            return;

        if (!EnsureOverrideController() || originalClip == null)
            return;

        AnimationClip clip = pendingClip;
        pendingClip = null;

        overrideController[originalClip] = clip;

        animator.Rebind();
        animator.Update(0f);
        animator.Play(0, 0, 0f);
    }

    public void Clear()
    {
        nodeIndex = -1;
        onSelected = null;
        pendingClip = null;

        gameObject.SetActive(false);
    }

    public void Select()
    {
        if (nodeIndex < 0 || UIPanelButton.IsMenuPanelOpen)
            return;

        onSelected?.Invoke(nodeIndex);
    }
}
