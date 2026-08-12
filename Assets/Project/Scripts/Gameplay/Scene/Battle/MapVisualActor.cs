using System;
using System.Collections.Generic;
using UnityEngine;

public class MapVisualActor : MonoBehaviour
{
    [SerializeField] private string visualObjectId;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform vfxRoot;
    [SerializeField] private List<MapVisualActionEntry> actions = new();

    private string runtimeVisualObjectId;

    public string VisualObjectId
    {
        get
        {
            string runtimeId = NormalizeId(runtimeVisualObjectId);
            return !string.IsNullOrEmpty(runtimeId)
                ? runtimeId
                : NormalizeId(visualObjectId);
        }
    }

    public void SetRuntimeVisualObjectId(string id)
    {
        runtimeVisualObjectId = NormalizeId(id);
    }

    public bool TryPlayAction(string actionId)
    {
        actionId = NormalizeId(actionId);

        if (string.IsNullOrEmpty(actionId) || actions == null)
            return false;

        bool matched = false;

        for (int i = 0; i < actions.Count; i++)
        {
            MapVisualActionEntry action = actions[i];
            if (action == null || !action.Matches(actionId))
                continue;

            action.Play(this);
            matched = true;
        }

        return matched;
    }

    internal Animator ResolveAnimator()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        return animator;
    }

    internal Transform ResolveVfxRoot()
    {
        return vfxRoot != null ? vfxRoot : transform;
    }

    internal static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}

[Serializable]
public sealed class MapVisualActionEntry
{
    public string ActionId;

    [Header("Animator")]
    public string AnimatorTrigger;

    [Header("VFX")]
    public GameObject VfxPrefab;
    public Transform VfxRootOverride;
    public Vector3 VfxLocalPosition;
    public Vector3 VfxLocalEulerAngles;
    public Vector3 VfxLocalScale = Vector3.one;
    public float VfxLifetime;

    [Header("Sprite")]
    public SpriteRenderer TintTarget;
    public bool ApplyTint;
    public Color TintColor = Color.white;

    [Header("Transform")]
    public Transform ScaleTarget;
    public bool ApplyLocalScale;
    public Vector3 LocalScale = Vector3.one;

    [Header("Activation")]
    public GameObject ActiveTarget;
    public bool ApplyActiveState;
    public bool ActiveState = true;

    public bool Matches(string actionId)
    {
        return string.Equals(
            MapVisualActor.NormalizeId(ActionId),
            MapVisualActor.NormalizeId(actionId),
            StringComparison.Ordinal);
    }

    internal void Play(MapVisualActor owner)
    {
        PlayAnimator(owner);
        SpawnVfx(owner);
        ApplySpriteTint();
        ApplyTransformScale();
        ApplyActivation();
    }

    private void PlayAnimator(MapVisualActor owner)
    {
        if (string.IsNullOrWhiteSpace(AnimatorTrigger))
            return;

        Animator resolvedAnimator = owner != null ? owner.ResolveAnimator() : null;
        if (resolvedAnimator != null)
            resolvedAnimator.SetTrigger(AnimatorTrigger.Trim());
    }

    private void SpawnVfx(MapVisualActor owner)
    {
        if (VfxPrefab == null)
            return;

        Transform parent = VfxRootOverride != null
            ? VfxRootOverride
            : owner != null
                ? owner.ResolveVfxRoot()
                : null;

        GameObject instance = UnityEngine.Object.Instantiate(VfxPrefab, parent, false);
        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = VfxLocalPosition;
        instanceTransform.localEulerAngles = VfxLocalEulerAngles;
        instanceTransform.localScale = VfxLocalScale;

        if (VfxLifetime > 0f)
            UnityEngine.Object.Destroy(instance, VfxLifetime);
    }

    private void ApplySpriteTint()
    {
        if (!ApplyTint || TintTarget == null)
            return;

        TintTarget.color = TintColor;
    }

    private void ApplyTransformScale()
    {
        if (!ApplyLocalScale || ScaleTarget == null)
            return;

        ScaleTarget.localScale = LocalScale;
    }

    private void ApplyActivation()
    {
        if (!ApplyActiveState || ActiveTarget == null)
            return;

        ActiveTarget.SetActive(ActiveState);
    }
}
